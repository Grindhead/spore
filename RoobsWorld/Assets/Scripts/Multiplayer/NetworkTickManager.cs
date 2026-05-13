using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Drives the multiplayer game-state tick loop.
    ///
    /// Host (20 Hz):
    ///   Gathers all <see cref="Organism"/>, <see cref="FoodItem"/>, and player positions,
    ///   serializes them into a <see cref="SessionNetState"/> snapshot, and broadcasts to
    ///   all connected clients via <see cref="INetworkTransport"/>.
    ///
    /// Client (every Update):
    ///   Drains inbound packets from the transport, applies host snapshots to
    ///   <see cref="RemotePlayerCell"/> ghosts and NPC organism positions, and
    ///   sends local input to the host.
    ///
    /// Offline / single-player:
    ///   When no session is active, this manager is a no-op with zero overhead.
    ///
    /// Attach to the CellStage GameObject. Activated by <see cref="Activate"/>,
    /// deactivated by <see cref="Deactivate"/> (called from CellStage.OnEnter/OnExit).
    ///
    /// Packet wire format: [1-byte type][payload bytes]
    ///   0x01 = SessionNetState  (host → clients, 20 Hz)
    ///   0x02 = RemoteInputPacket (client → host, every frame)
    /// </summary>
    public sealed class NetworkTickManager : MonoBehaviour
    {
        // ---- packet type tags ----
        public const byte TagSession = 0x01;
        public const byte TagInput   = 0x02;
        public const byte TagVoice   = 0x03;

        /// <summary>Host broadcast rate (seconds between snapshots).</summary>
        public const float BroadcastInterval = 0.05f; // 20 Hz

        private const float RemoteInputMoveSpeed = 7.5f;
        private const float InputSendInterval = BroadcastInterval;
        private const float ObjectCacheRefreshInterval = 0.5f;
        private const float InputChangeEpsilon = 0.0001f;

        // ---- runtime state ----

        private MultiplayerSessionManager            _session;
        private INetworkTransport                    _transport;
        private PlayerCell                           _localPlayer;

        private uint  _tick;
        private float _broadcastTimer;
        private float _inputSendTimer;
        private float _objectCacheTimer;
        private bool _objectCachesReady;
        private Vector2 _lastSentMove = new(float.NaN, float.NaN);
        private bool _lastSentEatInput;

        private readonly Dictionary<int, RemotePlayerCell> _remotePlayers  = new();
        private readonly Dictionary<int, uint> _lastRemoteInputTicks = new();
        private readonly Dictionary<EntityId, Organism>         _orgById        = new();
        private readonly Dictionary<EntityId, uint> _organismGenomeChecksums = new();
        private readonly List<Organism> _organismCache = new(128);
        private FoodItem[] _foodCache = Array.Empty<FoodItem>();
        private ProximityVoiceChat _voiceChat;

        // ---- activation ----

        /// <summary>
        /// Starts the tick loop. Call from <c>CellStage.OnEnter()</c> after the session is available.
        /// Does nothing when <paramref name="session"/> state is Offline (single-player safety).
        /// </summary>
        public void Activate(MultiplayerSessionManager session, INetworkTransport transport, PlayerCell localPlayer)
        {
            _session     = session;
            _transport   = transport;
            _localPlayer = localPlayer;

            EventBus.Subscribe<MultiplayerPlayerJoinedEvent>(OnRemotePlayerJoined);
            EventBus.Subscribe<MultiplayerPlayerLeftEvent>(OnRemotePlayerLeft);

            if (_session?.RemotePlayers != null)
            {
                foreach (var player in _session.RemotePlayers)
                    EnsureRemotePlayerGhost(player.PlayerId, player.DisplayName);
            }

            _voiceChat = gameObject.GetComponent<ProximityVoiceChat>()
                ?? gameObject.AddComponent<ProximityVoiceChat>();
            _voiceChat.Activate(
                _session,
                _transport,
                _localPlayer != null ? _localPlayer.transform : null,
                GetRemotePlayerTransform);

            if (_transport is WebSocketNetworkTransport wsTransport)
                _ = wsTransport.SendJoinLobbyAsync("cell");
        }

        /// <summary>Stops the tick loop and destroys all remote player ghosts.</summary>
        public void Deactivate()
        {
            EventBus.Unsubscribe<MultiplayerPlayerJoinedEvent>(OnRemotePlayerJoined);
            EventBus.Unsubscribe<MultiplayerPlayerLeftEvent>(OnRemotePlayerLeft);

            foreach (var cell in _remotePlayers.Values)
                if (cell != null) Destroy(cell.gameObject);
            _remotePlayers.Clear();
            _lastRemoteInputTicks.Clear();
            _orgById.Clear();
            _organismGenomeChecksums.Clear();
            _organismCache.Clear();
            _foodCache = Array.Empty<FoodItem>();
            _objectCachesReady = false;
            _lastSentMove = new Vector2(float.NaN, float.NaN);
            _lastSentEatInput = false;
            _voiceChat?.Deactivate();

            if (_transport is WebSocketNetworkTransport wsTransport)
                _ = wsTransport.SendJoinLobbyAsync(MultiplayerSessionManager.DefaultLobbyId);

            _session   = null;
            _transport = null;
        }

        // ---- Unity loop ----

        private void Update()
        {
            if (_session == null || _transport == null) return;
            if (_session.State == SessionState.Offline)  return;

            // Drain all inbound packets this frame
            byte[] raw;
            while ((raw = _transport.TryReceive(out int fromId)) != null)
                HandlePacket(fromId, raw);

            // Host broadcasts state at fixed interval
            if (_session.IsHost)
            {
                _broadcastTimer += Time.unscaledDeltaTime;
                if (_broadcastTimer >= BroadcastInterval)
                {
                    _broadcastTimer = 0f;
                    BroadcastState();
                }
            }
            else if (_session.State == SessionState.Connected)
            {
                SendLocalInput(Time.unscaledDeltaTime);
            }
        }

        // ---- host: build and send state ----

        private void BroadcastState()
        {
            _tick++;
            var state   = BuildSessionState(_tick);
            var payload = NetworkSerializer.Serialize(state);
            _transport.Broadcast(Prepend(TagSession, payload));
        }

        internal SessionNetState BuildSessionState(uint tick)
        {
            // ---- players ----
            var players = new List<PlayerNetState>();

            // Local player
            if (_localPlayer != null)
            {
                var pos = _localPlayer.transform.position;
                players.Add(new PlayerNetState
                {
                    PlayerId       = _session.LocalPlayerId,
                    PosX           = pos.x,
                    PosY           = pos.z,
                    VelX           = 0f,
                    VelY           = 0f,
                    GenomeChecksum = GenomeChecksum.Compute(_localPlayer.PlayerGenome),
                    Energy         = 0f,
                    Mass           = _localPlayer.CurrentMass,
                    IsAlive        = !_localPlayer.IsDead
                });
            }

            // Remote player ghosts — re-broadcast their last known positions
            foreach (var kv in _remotePlayers)
            {
                if (kv.Value == null) continue;
                var pos = kv.Value.transform.position;
                players.Add(new PlayerNetState
                {
                    PlayerId = kv.Key,
                    PosX     = pos.x,
                    PosY     = pos.z,
                    Mass     = kv.Value.LastKnownMass,
                    IsAlive  = kv.Value.IsAliveSnapshot
                });
            }

            // ---- organisms ----
            RefreshObjectCachesIfNeeded();
            var orgStates = new List<OrganismNetState>(_organismCache.Count);
            for (int i = _organismCache.Count - 1; i >= 0; i--)
            {
                var o   = _organismCache[i];
                if (o == null)
                {
                    _organismCache.RemoveAt(i);
                    continue;
                }

                var pos = o.transform.position;
                var id  = o.gameObject.GetEntityId();
                orgStates.Add(new OrganismNetState
                {
                    OrgId          = id,
                    PosX           = pos.x,
                    PosY           = pos.z,
                    GenomeChecksum = GetOrganismGenomeChecksum(id, o.Genome),
                    SpeciesId      = o.SpeciesId,
                    IsAlive        = !o.IsDead
                });
            }
            orgStates.Reverse();

            // ---- food ----
            var foodStates = new List<FoodNetState>(_foodCache.Length);
            bool foodCacheHadDestroyedItems = false;
            for (int i = 0; i < _foodCache.Length; i++)
            {
                var f   = _foodCache[i];
                if (f == null)
                {
                    foodCacheHadDestroyedItems = true;
                    continue;
                }

                var pos = f.transform.position;
                foodStates.Add(new FoodNetState
                {
                    FoodId   = f.FoodManagerId,
                    PosX     = pos.x,
                    PosY     = pos.z,
                    FoodType = f.Type,
                    Exists   = f.gameObject.activeSelf
                });
            }
            if (foodCacheHadDestroyedItems)
                _foodCache = foodStates.Count == 0
                    ? Array.Empty<FoodItem>()
                    : Array.FindAll(_foodCache, f => f != null);

            return new SessionNetState
            {
                Tick      = tick,
                Players   = players.ToArray(),
                Organisms = orgStates.ToArray(),
                Foods     = foodStates.ToArray()
            };
        }

        // ---- client: send input ----

        private void SendLocalInput(float deltaTime)
        {
            if (_localPlayer == null) return;

            Vector2 move = ReadMoveInput();
            bool eatInput = false;
            _inputSendTimer += deltaTime;

            bool changed = (move - _lastSentMove).sqrMagnitude > InputChangeEpsilon
                || eatInput != _lastSentEatInput;
            if (!changed && _inputSendTimer < InputSendInterval)
                return;

            _inputSendTimer = 0f;
            _lastSentMove = move;
            _lastSentEatInput = eatInput;

            var pkt = new RemoteInputPacket
            {
                PlayerId = _session.LocalPlayerId,
                MoveX    = move.x,
                MoveY    = move.y,
                EatInput = eatInput,
                Mass     = _localPlayer.CurrentMass,
                Tick     = _tick
            };

            var payload = NetworkSerializer.Serialize(pkt);
            _transport.Send(_session.HostPlayerId, Prepend(TagInput, payload));
        }

        // ---- client: apply received state ----

        private void HandlePacket(int fromId, byte[] raw)
        {
            if (raw == null || raw.Length < 2)
            {
                Debug.LogError($"[NetworkTick] Invalid packet shape from player {fromId}: expected tag plus payload, got length {(raw == null ? 0 : raw.Length)}.");
                return;
            }

            byte tag = raw[0];
            int payloadOffset = 1;
            int payloadCount = raw.Length - payloadOffset;

            if (tag == TagSession)
            {
                SessionNetState state;
                try { state = NetworkSerializer.DeserializeSession(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid SessionNetState packet from player {fromId}: {ex.Message}");
                    return;
                }

                ApplySessionState(state);
            }
            else if (tag == TagVoice)
            {
                VoiceChatPacket voice;
                try { voice = NetworkSerializer.DeserializeVoice(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid VoiceChatPacket from player {fromId}: {ex.Message}");
                    return;
                }

                _voiceChat?.Receive(voice);
            }
            else if (tag == TagInput)
            {
                if (!_session.IsHost) return;
                RemoteInputPacket input;
                try { input = NetworkSerializer.DeserializeInput(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid RemoteInputPacket from player {fromId}: {ex.Message}");
                    return;
                }

                ApplyRemoteInput(fromId, input);
                return;
                // Host receives client input — reserved for future input-driven movement
                // (clients currently move their own player locally; host reconciles on next broadcast)
            }
            else
            {
                Debug.LogError($"[NetworkTick] Invalid packet tag from player {fromId}: 0x{tag:X2}.");
            }
        }

        private void ApplyRemoteInput(int fromId, in RemoteInputPacket input)
        {
            int playerId = input.PlayerId;
            if (playerId < 0) return;
            if (fromId >= 0 && playerId != fromId) return;
            if (playerId == _session.LocalPlayerId) return;
            bool hasLastTick = _lastRemoteInputTicks.TryGetValue(playerId, out uint lastAcceptedTick);
            if (!ShouldAcceptRemoteInputTick(hasLastTick, lastAcceptedTick, input.Tick))
                return;

            _lastRemoteInputTicks[playerId] = input.Tick;

            var move = new Vector2(input.MoveX, input.MoveY);
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            EnsureRemotePlayerGhost(playerId);
            if (!_remotePlayers.TryGetValue(playerId, out var ghost) || ghost == null)
                return;

            float localMass = _localPlayer != null ? _localPlayer.CurrentMass : -1f;
            ghost.ApplyMassSnapshot(input.Mass, localMass);

            if (move.sqrMagnitude <= 0.0001f)
                return;

            float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.05f);
            float moveSpeed = ComputeRemoteInputMoveSpeed(RemoteInputMoveSpeed, input.Mass);
            var delta = new Vector3(move.x, 0f, move.y) * (moveSpeed * dt);
            ghost.MoveAuthoritative(delta);
        }

        private void ApplySessionState(in SessionNetState state)
        {
            if (state.Tick > _tick)
                _tick = state.Tick;

            // Apply remote player positions
            if (state.Players != null)
            {
                foreach (var ps in state.Players)
                {
                    if (ps.PlayerId == _session.LocalPlayerId) continue; // skip self
                    EnsureRemotePlayerGhost(ps.PlayerId);
                    if (_remotePlayers.TryGetValue(ps.PlayerId, out var ghost) && ghost != null)
                    {
                        ghost.SetDisplayName(GetRemotePlayerDisplayName(ps.PlayerId));
                        float localMass = _localPlayer != null ? _localPlayer.CurrentMass : -1f;
                        ghost.ApplyState(ps, localMass);
                    }
                }
            }

            // Apply organism positions (snap — no lerp for NPCs; they have their own AI locally)
            // Only applied on clients where the host is authoritative.
            if (!_session.IsHost && state.Organisms != null)
            {
                // Rebuild id→Organism map lazily when count changes
                if (_orgById.Count != state.Organisms.Length)
                    RebuildOrgMap();

                foreach (var os in state.Organisms)
                {
                    if (_orgById.TryGetValue(os.OrgId, out var org) && org != null && !org.IsDead)
                    {
                        var target = new Vector3(os.PosX, org.transform.position.y, os.PosY);
                        org.transform.position = Vector3.Lerp(
                            org.transform.position, target, 10f * Time.deltaTime);
                    }
                }
            }
        }

        private void RebuildOrgMap()
        {
            _orgById.Clear();
            RefreshObjectCaches(force: true);
            foreach (var o in _organismCache)
                _orgById[o.gameObject.GetEntityId()] = o;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 move = Vector2.zero;
            if (InputManager.HasInstance)
                move = InputManager.Instance.MoveInput;

            if (move.sqrMagnitude <= 0.0001f)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                }
            }

            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        private void RefreshObjectCachesIfNeeded()
        {
            _objectCacheTimer += BroadcastInterval;
            if (!_objectCachesReady || _objectCacheTimer >= ObjectCacheRefreshInterval)
                RefreshObjectCaches(force: false);
        }

        private void RefreshObjectCaches(bool force)
        {
            if (!force && _objectCachesReady && _objectCacheTimer < ObjectCacheRefreshInterval)
                return;

            _objectCacheTimer = 0f;
            _objectCachesReady = true;
            Organism.CopyActiveInstances(_organismCache);
            _foodCache = FindObjectsByType<FoodItem>(FindObjectsInactive.Exclude);
            _organismGenomeChecksums.Clear();

            for (int i = _organismCache.Count - 1; i >= 0; i--)
            {
                if (_organismCache[i] == null)
                    _organismCache.RemoveAt(i);
            }
        }

        private uint GetOrganismGenomeChecksum(EntityId id, Genome genome)
        {
            if (_organismGenomeChecksums.TryGetValue(id, out uint checksum))
                return checksum;

            checksum = GenomeChecksum.Compute(genome);
            _organismGenomeChecksums[id] = checksum;
            return checksum;
        }

        // ---- event handlers ----

        private void OnRemotePlayerJoined(MultiplayerPlayerJoinedEvent e)
        {
            EnsureRemotePlayerGhost(e.PlayerId);
        }

        private void EnsureRemotePlayerGhost(int playerId, string displayName = null)
        {
            if (playerId == _session?.LocalPlayerId) return; // ignore self
            string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? GetRemotePlayerDisplayName(playerId)
                : displayName;

            if (_remotePlayers.TryGetValue(playerId, out var existing) && existing != null)
            {
                existing.SetDisplayName(resolvedDisplayName);
                return;
            }
            if (_remotePlayers.ContainsKey(playerId)) return;

            // Spawn ghost slightly offset so it doesn't overlap local player
            var spawnPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f));
            var ghost = RemotePlayerCell.Create(playerId, spawnPos, resolvedDisplayName);
            _remotePlayers[playerId] = ghost;
            Debug.Log($"[NetworkTick] Remote player {playerId} joined — ghost spawned");
        }

        private string GetRemotePlayerDisplayName(int playerId)
        {
            if (_session?.RemotePlayers != null)
            {
                foreach (var player in _session.RemotePlayers)
                {
                    if (player != null && player.PlayerId == playerId)
                        return player.DisplayName;
                }
            }

            return RemotePlayerCell.NormalizeDisplayName(playerId, null);
        }

        private Transform GetRemotePlayerTransform(int playerId)
        {
            return _remotePlayers.TryGetValue(playerId, out var ghost) && ghost != null
                ? ghost.transform
                : null;
        }

        private void OnRemotePlayerLeft(MultiplayerPlayerLeftEvent e)
        {
            if (_remotePlayers.TryGetValue(e.PlayerId, out var ghost))
            {
                if (ghost != null) Destroy(ghost.gameObject);
                _remotePlayers.Remove(e.PlayerId);
                _lastRemoteInputTicks.Remove(e.PlayerId);
                Debug.Log($"[NetworkTick] Remote player {e.PlayerId} left — ghost destroyed");
            }
        }

        // ---- packet helpers ----

        /// <summary>Prepends a single type-tag byte to a payload.</summary>
        public static byte[] Prepend(byte tag, byte[] payload)
        {
            var result = new byte[payload.Length + 1];
            result[0] = tag;
            Buffer.BlockCopy(payload, 0, result, 1, payload.Length);
            return result;
        }

        /// <summary>Returns the payload portion of a tagged packet (everything after byte 0).</summary>
        public static byte[] StripTag(byte[] packet)
        {
            if (packet == null || packet.Length <= 1) return Array.Empty<byte>();
            var result = new byte[packet.Length - 1];
            Buffer.BlockCopy(packet, 1, result, 0, result.Length);
            return result;
        }

        /// <summary>Returns the type tag byte from a packet without copying.</summary>
        public static byte GetTag(byte[] packet) => packet != null && packet.Length > 0 ? packet[0] : (byte)0;

        public static bool ShouldAcceptRemoteInputTick(bool hasLastAcceptedTick, uint lastAcceptedTick, uint incomingTick)
        {
            return !hasLastAcceptedTick || incomingTick >= lastAcceptedTick;
        }

        public static float ComputeRemoteInputMoveSpeed(float baseSpeed, float remoteMass)
        {
            return Mathf.Max(0f, baseSpeed) * PlayerCell.ComputeSizeSpeedPenalty(remoteMass);
        }
    }
}
