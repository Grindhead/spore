using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Creature;

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
        public const byte TagStageTransition = 0x04;
        public const byte TagHeartbeat = 0x05;
        public const byte TagRecoveryRequest = 0x06;

        /// <summary>Host broadcast rate (seconds between snapshots).</summary>
        public const float BroadcastInterval = 0.05f; // 20 Hz

        private const float RemoteInputMoveSpeed = 7.5f;
        private const float InputSendInterval = BroadcastInterval;
        private const float ObjectCacheRefreshInterval = 0.5f;
        private const float InputChangeEpsilon = 0.0001f;
        private const float RecoveryRequestRetryInterval = 1.25f;
        private const int MaxCreatureDnaSnapshotChars = 12000;
        public const uint RemoteCreatureInputStaleTicks = 120u;
        public const uint CreatureActionIntentTtlTicks = 6u;
        public const uint RemoteCreatureActionResolveCooldownTicks = 10u;
        public const float RemoteCreatureAttackRange = 2.8f;
        public const float RemoteCreatureAbilityRadius = 3.2f;
        public const float RemoteCreatureFallbackAttackDamage = 6f;
        public const float RemoteCreatureFallbackAbilityDamage = 8f;

        // ---- runtime state ----

        private MultiplayerSessionManager            _session;
        private INetworkTransport                    _transport;
        private WebSocketNetworkTransport            _webSocketTransport;
        private PlayerCell                           _localPlayer;
        private CreatureInstance                     _localCreature;

        private uint  _tick;
        private float _broadcastTimer;
        private float _inputSendTimer;
        private float _objectCacheTimer;
        private bool _objectCachesReady;
        private Vector2 _lastSentMove = new(float.NaN, float.NaN);
        private bool _lastSentEatInput;

        private readonly Dictionary<int, RemotePlayerCell> _remotePlayers  = new();
        private readonly Dictionary<int, uint> _lastRemoteInputTicks = new();
        private readonly Dictionary<int, uint> _lastRemoteCreatureInputHostTicks = new();
        private readonly Dictionary<int, uint> _lastRemoteCreatureActionHostTicks = new();
        private readonly Dictionary<int, uint> _lastResolvedRemoteCreatureActionSourceTicks = new();
        private readonly Dictionary<int, uint> _lastResolvedRemoteCreatureActionHostTicks = new();
        private readonly Dictionary<int, CreaturePlayerActionNetState> _remoteCreatureActions = new();
        private readonly Dictionary<EntityId, Organism>         _orgById        = new();
        private readonly Dictionary<EntityId, uint> _organismGenomeChecksums = new();
        private readonly List<Organism> _organismCache = new(128);
        private readonly List<int> _localFoodIds = new(256);
        private readonly List<int> _localPartPickupIds = new(64);
        private readonly HashSet<int> _hostFoodIds = new();
        private readonly HashSet<int> _hostPartPickupIds = new();
        private readonly HashSet<string> _hostCreatureRivalIds = new();
        private readonly HashSet<int> _hostCreaturePlayerIds = new();
        private readonly List<string> _localCreatureRivalIds = new(32);
        private readonly List<int> _localCreaturePlayerIds = new(16);
        private readonly Dictionary<int, RemotePlayerCreature> _remoteCreaturePlayers = new();
        private FoodItem[] _foodCache = Array.Empty<FoodItem>();
        private CellPartPickup[] _partPickupCache = Array.Empty<CellPartPickup>();
        private FoodManager _foodManagerCache;
        private CellStage _cellStageCache;
        private CreatureStage _creatureStageCache;
        private ProximityVoiceChat _voiceChat;
        private float _heartbeatTimer;
        private float _recoveryRequestTimer = RecoveryRequestRetryInterval;
        private float _lastAuthoritativeSnapshotAgeSeconds;
        private int _packetsSentForQuality;
        private int _packetsReceivedForQuality;
        private int _reconnectAttempts;
        private float _lastPingMs;
        private float _rttTrendMs;
        private uint _heartbeatSequence;
        private NetworkQualitySnapshot _qualitySnapshot;

        public NetworkQualitySnapshot CurrentQuality => _qualitySnapshot;

        // ---- activation ----

        /// <summary>
        /// Starts the tick loop. Call from <c>CellStage.OnEnter()</c> after the session is available.
        /// Does nothing when <paramref name="session"/> state is Offline (single-player safety).
        /// </summary>
        public void Activate(
            MultiplayerSessionManager session,
            INetworkTransport transport,
            PlayerCell localPlayer,
            CreatureInstance localCreature = null)
        {
            _session     = session;
            _transport   = transport;
            _localPlayer = localPlayer;
            _localCreature = localCreature;

            EventBus.Subscribe<MultiplayerPlayerJoinedEvent>(OnRemotePlayerJoined);
            EventBus.Subscribe<MultiplayerPlayerLeftEvent>(OnRemotePlayerLeft);
            EventBus.Subscribe<CellEvolutionCompleteEvent>(OnCellEvolutionComplete);

            if (_session?.RemotePlayers != null)
            {
                foreach (var player in _session.RemotePlayers)
                {
                    if (_localPlayer != null)
                        EnsureRemotePlayerGhost(player.PlayerId, player.DisplayName);
                }
            }

            _voiceChat = gameObject.GetComponent<ProximityVoiceChat>()
                ?? gameObject.AddComponent<ProximityVoiceChat>();
            _voiceChat.Activate(
                _session,
                _transport,
                ResolveLocalVoiceTransform(),
                GetRemotePlayerTransform);

            AttachWebSocketTransport(_transport as WebSocketNetworkTransport);
            if (_webSocketTransport != null)
                _ = _webSocketTransport.SendJoinLobbyAsync("cell");
        }

        /// <summary>Stops the tick loop and destroys all remote player ghosts.</summary>
        public void Deactivate()
        {
            EventBus.Unsubscribe<MultiplayerPlayerJoinedEvent>(OnRemotePlayerJoined);
            EventBus.Unsubscribe<MultiplayerPlayerLeftEvent>(OnRemotePlayerLeft);
            EventBus.Unsubscribe<CellEvolutionCompleteEvent>(OnCellEvolutionComplete);

            foreach (var cell in _remotePlayers.Values)
                if (cell != null) DestroyNetworkObject(cell.gameObject);
            foreach (var creature in _remoteCreaturePlayers.Values)
                if (creature != null) DestroyNetworkObject(creature.gameObject);
            _remotePlayers.Clear();
            _remoteCreaturePlayers.Clear();
            _lastRemoteInputTicks.Clear();
            _lastRemoteCreatureInputHostTicks.Clear();
            _lastRemoteCreatureActionHostTicks.Clear();
            _lastResolvedRemoteCreatureActionSourceTicks.Clear();
            _lastResolvedRemoteCreatureActionHostTicks.Clear();
            _remoteCreatureActions.Clear();
            _orgById.Clear();
            _organismGenomeChecksums.Clear();
            _organismCache.Clear();
            _localFoodIds.Clear();
            _localPartPickupIds.Clear();
            _localCreatureRivalIds.Clear();
            _localCreaturePlayerIds.Clear();
            _hostFoodIds.Clear();
            _hostPartPickupIds.Clear();
            _hostCreatureRivalIds.Clear();
            _hostCreaturePlayerIds.Clear();
            _foodCache = Array.Empty<FoodItem>();
            _partPickupCache = Array.Empty<CellPartPickup>();
            _foodManagerCache = null;
            _cellStageCache = null;
            _creatureStageCache = null;
            _objectCachesReady = false;
            _lastSentMove = new Vector2(float.NaN, float.NaN);
            _lastSentEatInput = false;
            _localCreature = null;
            _heartbeatTimer = 0f;
            _recoveryRequestTimer = RecoveryRequestRetryInterval;
            _lastAuthoritativeSnapshotAgeSeconds = 0f;
            _packetsSentForQuality = 0;
            _packetsReceivedForQuality = 0;
            _reconnectAttempts = 0;
            _lastPingMs = 0f;
            _rttTrendMs = 0f;
            _heartbeatSequence = 0;
            _qualitySnapshot = default;
            _voiceChat?.Deactivate();

            if (_webSocketTransport != null)
                _ = _webSocketTransport.SendJoinLobbyAsync(MultiplayerSessionManager.DefaultLobbyId);
            DetachWebSocketTransport();

            _session   = null;
            _transport = null;
        }

        // ---- Unity loop ----

        private void Update()
        {
            if (_session == null || _transport == null) return;
            if (_session.State == SessionState.Offline)  return;

            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (_session.IsHost)
                _lastAuthoritativeSnapshotAgeSeconds = 0f;
            else
                _lastAuthoritativeSnapshotAgeSeconds += deltaTime;

            // Drain all inbound packets this frame
            byte[] raw;
            while ((raw = _transport.TryReceive(out int fromId)) != null)
                HandlePacket(fromId, raw);

            UpdateNetworkQuality();
            SendHeartbeatIfDue(deltaTime);
            SendRecoveryRequestIfNeeded(deltaTime);

            // Host broadcasts state at fixed interval
            if (_session.IsHost)
            {
                _broadcastTimer += deltaTime;
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

            var pickupStates = new List<PartPickupNetState>(_partPickupCache.Length);
            bool pickupCacheHadDestroyedItems = false;
            for (int i = 0; i < _partPickupCache.Length; i++)
            {
                var pickup = _partPickupCache[i];
                if (pickup == null)
                {
                    pickupCacheHadDestroyedItems = true;
                    continue;
                }

                var pos = pickup.transform.position;
                pickupStates.Add(new PartPickupNetState
                {
                    PickupId = pickup.PickupId,
                    PosX = pos.x,
                    PosY = pos.z,
                    PartType = pickup.PartType,
                    Exists = pickup.gameObject.activeSelf
                });
            }
            if (pickupCacheHadDestroyedItems)
                _partPickupCache = pickupStates.Count == 0
                    ? Array.Empty<CellPartPickup>()
                    : Array.FindAll(_partPickupCache, p => p != null);

            return new SessionNetState
            {
                Tick      = tick,
                Players   = players.ToArray(),
                Organisms = orgStates.ToArray(),
                Foods     = foodStates.ToArray(),
                PartPickups = pickupStates.ToArray(),
                CreaturePlayers = BuildCreaturePlayerStates(tick),
                CreatureRivals = BuildCreatureRivalStates(),
                CreatureObjective = BuildCreatureObjectiveState(),
                CreatureWorldProgression = BuildCreatureWorldProgressionState(),
                CreaturePlayerActions = BuildCreaturePlayerActionStates(tick)
            };
        }

        private CreaturePlayerNetState[] BuildCreaturePlayerStates(uint hostTick)
        {
            CleanupStaleRemoteCreaturePlayers(hostTick);
            var states = new List<CreaturePlayerNetState>(_remoteCreaturePlayers.Count + 1);
            var creature = _localCreature;
            if (creature == null)
                creature = FindLocalCreature();
            _localCreature = creature;

            if (creature != null)
            {
                var controller = creature.GetComponent<CreaturePlayerController>();
                states.Add(new CreaturePlayerNetState
                {
                    PlayerId = _session != null ? _session.LocalPlayerId : -1,
                    CreatureId = creature.DNA?.CreatureId ?? string.Empty,
                    CreatureDNAJson = BuildCreatureDnaSnapshot(creature.DNA),
                    PosX = creature.transform.position.x,
                    PosY = creature.transform.position.z,
                    RotY = creature.transform.eulerAngles.y,
                    Health = creature.CurrentHealth,
                    MaxHealth = creature.MaxHealth,
                    Stamina = controller != null ? controller.CurrentStamina : 0f,
                    IsAlive = creature.IsAlive
                });
            }

            foreach (var pair in _remoteCreaturePlayers)
            {
                var remote = pair.Value;
                if (remote != null)
                    states.Add(remote.CreateSnapshot());
            }

            return states.Count == 0 ? Array.Empty<CreaturePlayerNetState>() : states.ToArray();
        }

        private void CleanupStaleRemoteCreaturePlayers(uint hostTick)
        {
            _localCreaturePlayerIds.Clear();
            foreach (var pair in _remoteCreaturePlayers)
            {
                if (pair.Value == null)
                {
                    _localCreaturePlayerIds.Add(pair.Key);
                    continue;
                }

                if (_lastRemoteCreatureInputHostTicks.TryGetValue(pair.Key, out uint lastInputHostTick) &&
                    IsRemoteCreatureInputStale(hostTick, lastInputHostTick))
                {
                    _localCreaturePlayerIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _localCreaturePlayerIds.Count; i++)
                RemoveRemoteCreaturePlayer(_localCreaturePlayerIds[i]);
        }

        private static string BuildCreatureDnaSnapshot(CreatureDNA dna)
        {
            if (dna == null)
                return string.Empty;

            string json = dna.ToJson();
            if (string.IsNullOrWhiteSpace(json) || json.Length > MaxCreatureDnaSnapshotChars)
                return string.Empty;
            return json;
        }

        private CreaturePlayerActionNetState[] BuildCreaturePlayerActionStates(uint hostTick)
        {
            var actions = new List<CreaturePlayerActionNetState>(_remoteCreaturePlayers.Count + 1);
            var creature = _localCreature;
            if (creature == null)
                creature = FindLocalCreature();

            if (creature != null && _session != null)
            {
                actions.Add(new CreaturePlayerActionNetState
                {
                    PlayerId = _session.LocalPlayerId,
                    ActionFlags = ReadCreatureActionFlags(),
                    SourceTick = hostTick
                });
            }

            foreach (var pair in _remoteCreaturePlayers)
            {
                int playerId = pair.Key;
                if (_remoteCreatureActions.TryGetValue(playerId, out var action) &&
                    _lastRemoteCreatureActionHostTicks.TryGetValue(playerId, out uint actionHostTick) &&
                    !IsCreatureActionIntentExpired(hostTick, actionHostTick))
                {
                    action.PlayerId = playerId;
                    actions.Add(action);
                }
                else
                {
                    _remoteCreatureActions.Remove(playerId);
                    _lastRemoteCreatureActionHostTicks.Remove(playerId);
                    pair.Value?.ApplyActionFlags(CreatureInputActionFlags.None);
                    actions.Add(new CreaturePlayerActionNetState
                    {
                        PlayerId = playerId,
                        ActionFlags = CreatureInputActionFlags.None,
                        SourceTick = hostTick
                    });
                }
            }

            return actions.Count == 0 ? Array.Empty<CreaturePlayerActionNetState>() : actions.ToArray();
        }

        private CreatureRivalNetState[] BuildCreatureRivalStates()
        {
            var rivals = FindObjectsByType<CreatureRivalAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (rivals == null || rivals.Length == 0)
                return Array.Empty<CreatureRivalNetState>();

            var states = new List<CreatureRivalNetState>(rivals.Length);
            for (int i = 0; i < rivals.Length; i++)
            {
                var ai = rivals[i];
                if (ai == null)
                    continue;

                var creature = ai.GetComponent<CreatureInstance>();
                if (creature == null || creature.DNA == null)
                    continue;

                states.Add(new CreatureRivalNetState
                {
                    CreatureId = creature.DNA.CreatureId,
                    Archetype = (int)ai.Archetype,
                    IsAlpha = ai.IsAlpha,
                    PosX = creature.transform.position.x,
                    PosY = creature.transform.position.z,
                    RotY = creature.transform.eulerAngles.y,
                    Health = creature.CurrentHealth,
                    MaxHealth = creature.MaxHealth,
                    Exists = creature.gameObject.activeSelf,
                    IsAlive = creature.IsAlive
                });
            }

            return states.ToArray();
        }

        private CreatureObjectiveNetState BuildCreatureObjectiveState()
        {
            var creatureStage = _creatureStageCache;
            if (creatureStage == null)
                creatureStage = FindAnyObjectByType<CreatureStage>();
            _creatureStageCache = creatureStage;
            return creatureStage != null ? creatureStage.CreateNetworkObjectiveSnapshot() : default;
        }

        private CreatureWorldProgressionNetState BuildCreatureWorldProgressionState()
        {
            var creatureStage = _creatureStageCache;
            if (creatureStage == null)
                creatureStage = FindAnyObjectByType<CreatureStage>();
            _creatureStageCache = creatureStage;
            return creatureStage != null ? creatureStage.CreateNetworkWorldProgressionSnapshot() : default;
        }

        // ---- client: send input ----

        private void SendLocalInput(float deltaTime)
        {
            var localCreature = ResolveLocalCreature();
            if (_localPlayer == null && localCreature == null) return;

            Vector2 move = ReadMoveInput();
            bool eatInput = false;
            _inputSendTimer += deltaTime;

            bool hasCreatureInput = localCreature != null;
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
                Mass     = _localPlayer != null ? _localPlayer.CurrentMass : 0f,
                Tick     = _tick,
                HasCreatureInput = hasCreatureInput
            };
            if (hasCreatureInput)
            {
                FillCreatureInputSnapshot(ref pkt, localCreature);
                pkt.CreatureActionFlags = ReadCreatureActionFlags();
            }

            var payload = NetworkSerializer.Serialize(pkt);
            _transport.Send(_session.HostPlayerId, Prepend(TagInput, payload));
        }

        private static void FillCreatureInputSnapshot(ref RemoteInputPacket pkt, CreatureInstance creature)
        {
            if (creature == null)
                return;

            var controller = creature.GetComponent<CreaturePlayerController>();
            pkt.CreatureId = creature.DNA?.CreatureId ?? string.Empty;
            pkt.CreatureDNAJson = BuildCreatureDnaSnapshot(creature.DNA);
            pkt.CreaturePosX = creature.transform.position.x;
            pkt.CreaturePosY = creature.transform.position.z;
            pkt.CreatureRotY = creature.transform.eulerAngles.y;
            pkt.CreatureHealth = creature.CurrentHealth;
            pkt.CreatureMaxHealth = creature.MaxHealth;
            pkt.CreatureStamina = controller != null ? controller.CurrentStamina : 0f;
            pkt.CreatureIsAlive = creature.IsAlive;
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
            else if (tag == TagStageTransition)
            {
                StageTransitionNetState transition;
                try { transition = NetworkSerializer.DeserializeStageTransition(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid StageTransitionNetState packet from player {fromId}: {ex.Message}");
                    return;
                }

                ApplyStageTransition(fromId, transition);
            }
            else if (tag == TagHeartbeat)
            {
                NetworkHeartbeatPacket heartbeat;
                try { heartbeat = NetworkSerializer.DeserializeHeartbeat(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid NetworkHeartbeatPacket from player {fromId}: {ex.Message}");
                    return;
                }

                HandleHeartbeat(fromId, heartbeat);
            }
            else if (tag == TagRecoveryRequest)
            {
                if (!_session.IsHost) return;
                NetworkRecoveryRequestPacket request;
                try { request = NetworkSerializer.DeserializeRecoveryRequest(raw, payloadOffset, payloadCount); }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkTick] Invalid NetworkRecoveryRequestPacket from player {fromId}: {ex.Message}");
                    return;
                }

                HandleRecoveryRequest(fromId, request);
            }
            else
            {
                Debug.LogError($"[NetworkTick] Invalid packet tag from player {fromId}: 0x{tag:X2}.");
            }
        }

        private void OnCellEvolutionComplete(CellEvolutionCompleteEvent evt)
        {
            if (_session == null || _transport == null || !_session.IsHost)
                return;

            BroadcastStageTransition(GameStageType.Creature);
        }

        private void BroadcastStageTransition(GameStageType targetStage)
        {
            if (!StageManager.IsStageSupported(targetStage))
                return;

            var pkt = new StageTransitionNetState
            {
                Tick = _tick,
                RequestingPlayerId = _session != null ? _session.LocalPlayerId : -1,
                TargetStage = targetStage,
                TransitionId = _tick == 0 ? 1u : _tick
            };
            _transport.Broadcast(Prepend(TagStageTransition, NetworkSerializer.Serialize(pkt)));
        }

        private void ApplyStageTransition(int fromId, in StageTransitionNetState transition)
        {
            if (!StageManager.HasInstance)
                return;

            if (!ShouldApplyStageTransition(
                    _session != null && _session.IsHost,
                    fromId,
                    _session != null ? _session.HostPlayerId : -1,
                    StageManager.Instance.CurrentStageType,
                    transition.TargetStage,
                    StageManager.Instance.IsTransitioning))
            {
                return;
            }

            StageManager.Instance.TransitionTo(transition.TargetStage);
        }

        private void SendHeartbeatIfDue(float deltaTime)
        {
            if (_session == null || _transport == null || _session.State == SessionState.Offline)
                return;

            _heartbeatTimer += Mathf.Max(0f, deltaTime);
            if (_heartbeatTimer < NetworkResilienceRules.DefaultHeartbeatIntervalSeconds)
                return;

            _heartbeatTimer = 0f;
            var heartbeat = new NetworkHeartbeatPacket
            {
                PlayerId = _session.LocalPlayerId,
                Sequence = ++_heartbeatSequence,
                SentAtSeconds = Time.realtimeSinceStartupAsDouble,
                IsAck = false
            };

            byte[] packet = Prepend(TagHeartbeat, NetworkSerializer.Serialize(heartbeat));
            if (_session.IsHost)
                _transport.Broadcast(packet);
            else
                _transport.Send(_session.HostPlayerId, packet);
            _packetsSentForQuality++;
        }

        private void HandleHeartbeat(int fromId, in NetworkHeartbeatPacket heartbeat)
        {
            if (heartbeat.IsAck)
            {
                float ping = (float)Math.Max(0d, (Time.realtimeSinceStartupAsDouble - heartbeat.SentAtSeconds) * 1000d);
                _lastPingMs = ping;
                _rttTrendMs = NetworkResilienceRules.SmoothRtt(_rttTrendMs, ping);
                _packetsReceivedForQuality++;
                UpdateNetworkQuality(forcePublish: true);
                return;
            }

            var ack = heartbeat;
            ack.PlayerId = _session != null ? _session.LocalPlayerId : ack.PlayerId;
            ack.IsAck = true;
            _transport?.Send(fromId, Prepend(TagHeartbeat, NetworkSerializer.Serialize(ack)));
        }

        private void SendRecoveryRequestIfNeeded(float deltaTime)
        {
            if (_session == null || _transport == null || _session.IsHost)
                return;

            _recoveryRequestTimer += Mathf.Max(0f, deltaTime);
            if (!NetworkResilienceRules.ShouldRequestRecovery(
                    false,
                    _qualitySnapshot.Quality,
                    _recoveryRequestTimer,
                    RecoveryRequestRetryInterval))
            {
                return;
            }

            _recoveryRequestTimer = 0f;
            _reconnectAttempts++;
            var request = new NetworkRecoveryRequestPacket
            {
                PlayerId = _session.LocalPlayerId,
                LastReceivedTick = _tick,
                CurrentStage = StageManager.HasInstance ? StageManager.Instance.CurrentStageType : GameStageType.None,
                ReconnectAttempt = _reconnectAttempts
            };
            _transport.Send(_session.HostPlayerId, Prepend(TagRecoveryRequest, NetworkSerializer.Serialize(request)));
        }

        private void HandleRecoveryRequest(int fromId, in NetworkRecoveryRequestPacket request)
        {
            if (_session == null || _transport == null || !_session.IsHost)
                return;
            if (fromId >= 0 && request.PlayerId != fromId)
                return;

            var state = BuildSessionState(++_tick);
            _transport.Send(request.PlayerId, Prepend(TagSession, NetworkSerializer.Serialize(state)));
        }

        private void UpdateNetworkQuality(bool forcePublish = false)
        {
            if (_session == null || _session.State == SessionState.Offline)
                return;

            bool connected = IsTransportHealthyForQuality();
            float snapshotAge = _session.IsHost ? 0f : _lastAuthoritativeSnapshotAgeSeconds;
            var next = NetworkResilienceRules.BuildSnapshot(
                connected,
                _lastPingMs,
                _rttTrendMs,
                _packetsSentForQuality,
                _packetsReceivedForQuality,
                snapshotAge,
                _reconnectAttempts);

            if (!forcePublish && IsSameQualitySnapshot(_qualitySnapshot, next))
                return;

            _qualitySnapshot = next;
            if (!_session.IsHost &&
                next.Quality != NetworkConnectionQuality.Healthy &&
                _session.State != SessionState.Disconnected)
            {
                _session.BeginReconnectAttempt();
                return;
            }

            _session.ApplyNetworkQuality(next);
        }

        private bool IsTransportHealthyForQuality()
        {
            if (_transport == null)
                return false;
            if (_transport is NullNetworkTransport)
                return true;
            return _transport.IsConnected;
        }

        private static bool IsSameQualitySnapshot(NetworkQualitySnapshot a, NetworkQualitySnapshot b)
        {
            return a.Quality == b.Quality
                && Mathf.Abs(a.PingMs - b.PingMs) < 0.01f
                && Mathf.Abs(a.RttTrendMs - b.RttTrendMs) < 0.01f
                && Mathf.Abs(a.PacketLossRatio - b.PacketLossRatio) < 0.001f
                && Mathf.Abs(a.LastAuthoritativeSnapshotAgeSeconds - b.LastAuthoritativeSnapshotAgeSeconds) < 0.05f
                && a.ReconnectAttempts == b.ReconnectAttempts;
        }

        private void AttachWebSocketTransport(WebSocketNetworkTransport transport)
        {
            DetachWebSocketTransport();
            _webSocketTransport = transport;
            if (_webSocketTransport == null)
                return;

            _webSocketTransport.OnRemotePlayerJoined += OnTransportRemotePlayerJoined;
            _webSocketTransport.OnRemotePlayerLeft += OnTransportRemotePlayerLeft;
            _webSocketTransport.OnHostMigrated += OnTransportHostMigrated;
            _webSocketTransport.OnPlayerLobbyChanged += OnTransportPlayerLobbyChanged;
            _webSocketTransport.OnSessionMapReceived += OnTransportSessionMapReceived;
            _webSocketTransport.OnGlobalChatMessage += OnTransportGlobalChatMessage;
        }

        private void DetachWebSocketTransport()
        {
            if (_webSocketTransport == null)
                return;

            _webSocketTransport.OnRemotePlayerJoined -= OnTransportRemotePlayerJoined;
            _webSocketTransport.OnRemotePlayerLeft -= OnTransportRemotePlayerLeft;
            _webSocketTransport.OnHostMigrated -= OnTransportHostMigrated;
            _webSocketTransport.OnPlayerLobbyChanged -= OnTransportPlayerLobbyChanged;
            _webSocketTransport.OnSessionMapReceived -= OnTransportSessionMapReceived;
            _webSocketTransport.OnGlobalChatMessage -= OnTransportGlobalChatMessage;
            _webSocketTransport = null;
        }

        private void OnTransportRemotePlayerJoined(int playerId)
        {
            _session?.AddRemotePlayer(playerId, $"Player {playerId}");
        }

        private void OnTransportRemotePlayerLeft(int playerId, bool wasHost)
        {
            _session?.RemovePlayer(playerId);
        }

        private void OnTransportHostMigrated(int newHostPlayerId)
        {
            _session?.ApplyHostMigration(newHostPlayerId);
        }

        private void OnTransportPlayerLobbyChanged(int playerId, string lobbyId)
        {
            _session?.ApplyPlayerLobby(playerId, lobbyId);
        }

        private void OnTransportSessionMapReceived(SessionMapDefinition map)
        {
            _session?.ApplySessionMap(map);
        }

        private void OnTransportGlobalChatMessage(int fromPlayerId, string message)
        {
            _session?.ApplyChatMessage(fromPlayerId, message);
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

            if (input.HasCreatureInput)
            {
                ApplyRemoteCreatureInput(playerId, input, move);
                return;
            }

            if (_localCreature != null)
                return;

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

        private void ApplyRemoteCreatureInput(int playerId, in RemoteInputPacket input, Vector2 move)
        {
            var state = new CreaturePlayerNetState
            {
                PlayerId = playerId,
                CreatureId = input.CreatureId,
                CreatureDNAJson = input.CreatureDNAJson,
                PosX = input.CreaturePosX,
                PosY = input.CreaturePosY,
                RotY = input.CreatureRotY,
                Health = Mathf.Max(0f, input.CreatureHealth),
                MaxHealth = Mathf.Max(1f, input.CreatureMaxHealth),
                Stamina = Mathf.Max(0f, input.CreatureStamina),
                IsAlive = input.CreatureIsAlive
            };
            if (string.IsNullOrWhiteSpace(state.CreatureId))
                state.CreatureId = $"remote_creature_{playerId}";

            if (!_remoteCreaturePlayers.TryGetValue(playerId, out var remote) || remote == null || !remote.MatchesIdentity(state))
            {
                AddOrUpdateRemoteCreaturePlayer(state);
                _remoteCreaturePlayers.TryGetValue(playerId, out remote);
            }
            else
            {
                remote.SetDisplayName(GetRemotePlayerDisplayName(playerId));
            }

            if (remote == null)
                return;

            _lastRemoteCreatureInputHostTicks[playerId] = _tick;
            if (input.CreatureActionFlags == CreatureInputActionFlags.None)
            {
                _remoteCreatureActions.Remove(playerId);
                _lastRemoteCreatureActionHostTicks.Remove(playerId);
                _lastResolvedRemoteCreatureActionSourceTicks.Remove(playerId);
                _lastResolvedRemoteCreatureActionHostTicks.Remove(playerId);
            }
            else
            {
                _remoteCreatureActions[playerId] = new CreaturePlayerActionNetState
                {
                    PlayerId = playerId,
                    ActionFlags = input.CreatureActionFlags,
                    SourceTick = input.Tick
                };
                _lastRemoteCreatureActionHostTicks[playerId] = _tick;
            }
            remote.ApplyActionFlags(input.CreatureActionFlags);
            ResolveRemoteCreatureActionIntent(playerId, remote, input.CreatureActionFlags, input.Tick);

            if (move.sqrMagnitude <= 0.0001f)
                return;

            float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.05f);
            float moveSpeed = RemoteInputMoveSpeed;
            var delta = new Vector3(move.x, 0f, move.y) * (moveSpeed * dt);
            float rotationY = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
            remote.MoveAuthoritative(delta, rotationY);
        }

        private void ResolveRemoteCreatureActionIntent(
            int playerId,
            RemotePlayerCreature remote,
            CreatureInputActionFlags actionFlags,
            uint sourceTick)
        {
            if (remote == null || !remote.IsAliveSnapshot || actionFlags == CreatureInputActionFlags.None)
                return;

            bool hasLastSourceTick = _lastResolvedRemoteCreatureActionSourceTicks.TryGetValue(playerId, out uint lastSourceTick);
            bool hasLastHostTick = _lastResolvedRemoteCreatureActionHostTicks.TryGetValue(playerId, out uint lastHostTick);
            if (!ShouldResolveRemoteCreatureAction(
                    hasLastSourceTick,
                    lastSourceTick,
                    hasLastHostTick,
                    lastHostTick,
                    sourceTick,
                    _tick))
            {
                return;
            }

            CreatureInputActionFlags dominantAction = GetDominantCreatureAction(actionFlags);
            bool resolved = dominantAction switch
            {
                CreatureInputActionFlags.Attack => ResolveRemoteCreatureAttackIntent(remote),
                CreatureInputActionFlags.Ability => ResolveRemoteCreatureAbilityIntent(remote),
                CreatureInputActionFlags.Guard => PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Guard),
                _ => false
            };

            if (!resolved)
                return;

            _lastResolvedRemoteCreatureActionSourceTicks[playerId] = sourceTick;
            _lastResolvedRemoteCreatureActionHostTicks[playerId] = _tick;
        }

        private bool ResolveRemoteCreatureAttackIntent(RemotePlayerCreature remote)
        {
            var sourceCreature = remote != null ? remote.GetComponent<CreatureInstance>() : null;
            float damage = sourceCreature != null
                ? Mathf.Max(1f, sourceCreature.Stats.Attack)
                : RemoteCreatureFallbackAttackDamage;
            return TryApplyRemoteCreatureDamage(remote, RemoteCreatureAttackRange, damage, publishMiss: true);
        }

        private bool ResolveRemoteCreatureAbilityIntent(RemotePlayerCreature remote)
        {
            var sourceCreature = remote != null ? remote.GetComponent<CreatureInstance>() : null;
            var ability = CreaturePlayerController.ResolveInheritedAbility(sourceCreature?.DNA);
            if (ability != CreatureInheritedAbilityType.None)
            {
                EventBus.Publish(new CreatureAbilityUsedEvent
                {
                    AbilityId = CreaturePlayerController.GetAbilityId(ability),
                    AbilityLabel = CreaturePlayerController.GetAbilityDisplayName(ability),
                    StaminaSpent = BalanceConstants.CreaturePlayerAbilityStaminaCost,
                    Position = remote.transform.position
                });
            }

            switch (ability)
            {
                case CreatureInheritedAbilityType.ToxinBurst:
                    return TryApplyRemoteCreatureDamage(
                        remote,
                        RemoteCreatureAbilityRadius,
                        RemoteCreatureFallbackAbilityDamage * 1.2f,
                        publishMiss: false) ||
                        PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Telegraph);
                case CreatureInheritedAbilityType.BioelectricBurst:
                    return TryApplyRemoteCreatureDamage(
                        remote,
                        RemoteCreatureAbilityRadius * 1.12f,
                        RemoteCreatureFallbackAbilityDamage,
                        publishMiss: false) ||
                        PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Telegraph);
                case CreatureInheritedAbilityType.GuardSurge:
                case CreatureInheritedAbilityType.RegenerativeSurge:
                    return PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Guard);
                default:
                    return PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Telegraph);
            }
        }

        private bool TryApplyRemoteCreatureDamage(
            RemotePlayerCreature remote,
            float range,
            float damage,
            bool publishMiss)
        {
            var target = FindRemoteCreatureActionTarget(remote, range);
            if (target == null)
            {
                return publishMiss &&
                    PublishRemoteCreatureCombatFeedback(remote, CreatureCombatFeedbackType.Miss);
            }

            bool wasAliveBeforeHit = target.IsAlive;
            float safeDamage = Mathf.Max(1f, damage);
            target.TakeDamage(safeDamage);
            bool killed = wasAliveBeforeHit && !target.IsAlive;
            return PublishRemoteCreatureCombatFeedback(
                remote,
                killed ? CreatureCombatFeedbackType.Kill : CreatureCombatFeedbackType.Hit,
                target,
                safeDamage);
        }

        private CreatureInstance FindRemoteCreatureActionTarget(RemotePlayerCreature remote, float range)
        {
            if (remote == null)
                return null;

            var sourceCreature = remote.GetComponent<CreatureInstance>();
            Vector3 source = remote.transform.position;
            Vector3 forward = remote.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            float safeRange = Mathf.Max(0.1f, range);
            var creatures = FindObjectsByType<CreatureInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            CreatureInstance best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < creatures.Length; i++)
            {
                var candidate = creatures[i];
                if (candidate == null || candidate == sourceCreature || !candidate.IsAlive || candidate.IsPlayer)
                    continue;
                if (candidate.GetComponent<RemotePlayerCreature>() != null)
                    continue;
                if (!string.IsNullOrWhiteSpace(candidate.DNA?.CreatureId) &&
                    string.Equals(candidate.DNA.CreatureId, remote.CreatureId, StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 toTarget = candidate.transform.position - source;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                if (distance > safeRange)
                    continue;

                bool closeEnoughForAssist = distance <= safeRange * 0.42f;
                bool inAssistCone = CreatureTargetAssist.IsWithinAssistCone(forward, toTarget);
                if (!closeEnoughForAssist && !inAssistCone)
                    continue;

                float forwardDot = toTarget.sqrMagnitude < 0.0001f
                    ? 1f
                    : Vector3.Dot(forward, toTarget.normalized);
                float score = CreatureTargetAssist.ComputeTargetScore(distance, forwardDot, closeEnoughForAssist);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private bool PublishRemoteCreatureCombatFeedback(
            RemotePlayerCreature remote,
            CreatureCombatFeedbackType feedbackType,
            CreatureInstance target = null,
            float amount = 0f)
        {
            if (remote == null)
                return false;

            EventBus.Publish(new CreatureCombatFeedbackEvent
            {
                Type = feedbackType,
                SourceCreatureId = remote.CreatureId,
                TargetCreatureId = target != null ? target.DNA?.CreatureId : null,
                Position = target != null ? target.transform.position : remote.transform.position,
                Amount = Mathf.Max(0f, amount),
                IsPlayerSource = true,
                IsPlayerTarget = false
            });
            return true;
        }

        private void ApplySessionState(in SessionNetState state)
        {
            if (state.Tick > _tick)
                _tick = state.Tick;
            _lastAuthoritativeSnapshotAgeSeconds = 0f;
            if (_session.State == SessionState.Disconnected)
                _session.CompleteReconnect();

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

            if (!_session.IsHost && state.Foods != null)
                ApplyFoodState(state.Foods);

            if (!_session.IsHost && state.PartPickups != null)
                ApplyPartPickupState(state.PartPickups);

            if (!_session.IsHost && state.CreaturePlayers != null)
                ApplyCreaturePlayerState(state.CreaturePlayers);

            if (!_session.IsHost && state.CreaturePlayerActions != null)
                ApplyCreaturePlayerActionState(state.CreaturePlayerActions);

            if (!_session.IsHost && state.CreatureRivals != null)
                ApplyCreatureRivalState(state.CreatureRivals);

            if (!_session.IsHost && state.CreatureObjective.HasObjective)
                ApplyCreatureObjectiveState(state.CreatureObjective);

            if (!_session.IsHost && state.CreatureWorldProgression.HasProgression)
                ApplyCreatureWorldProgressionState(state.CreatureWorldProgression);

            UpdateNetworkQuality(forcePublish: true);
        }

        private void ApplyFoodState(FoodNetState[] foods)
        {
            FoodManager foodManager = _foodManagerCache;
            if (foodManager == null)
                foodManager = FindAnyObjectByType<FoodManager>();
            _foodManagerCache = foodManager;
            if (foodManager == null)
                return;

            _hostFoodIds.Clear();
            for (int i = 0; i < foods.Length; i++)
            {
                var fs = foods[i];
                if (fs.FoodId <= 0)
                    continue;

                if (!fs.Exists)
                {
                    foodManager.ConsumeFood(fs.FoodId);
                    continue;
                }

                _hostFoodIds.Add(fs.FoodId);
                foodManager.AddOrUpdateNetworkFood(
                    fs.FoodId,
                    new Vector3(fs.PosX, 0f, fs.PosY),
                    fs.FoodType);
            }

            foodManager.CopyFoodIds(_localFoodIds);
            for (int i = 0; i < _localFoodIds.Count; i++)
            {
                int localId = _localFoodIds[i];
                if (!_hostFoodIds.Contains(localId))
                    foodManager.ConsumeFood(localId);
            }
        }

        private void ApplyPartPickupState(PartPickupNetState[] pickups)
        {
            CellStage cellStage = _cellStageCache;
            if (cellStage == null)
                cellStage = FindAnyObjectByType<CellStage>();
            _cellStageCache = cellStage;
            if (cellStage == null)
                return;

            _hostPartPickupIds.Clear();
            for (int i = 0; i < pickups.Length; i++)
            {
                var ps = pickups[i];
                if (ps.PickupId <= 0)
                    continue;

                if (!ps.Exists)
                {
                    cellStage.RemovePartPickup(ps.PickupId);
                    continue;
                }

                _hostPartPickupIds.Add(ps.PickupId);
                cellStage.AddOrUpdateNetworkPartPickup(
                    ps.PickupId,
                    ps.PartType,
                    new Vector3(ps.PosX, 0f, ps.PosY));
            }

            cellStage.CopyPartPickupIds(_localPartPickupIds);
            for (int i = 0; i < _localPartPickupIds.Count; i++)
            {
                int localId = _localPartPickupIds[i];
                if (!_hostPartPickupIds.Contains(localId))
                    cellStage.RemovePartPickup(localId);
            }
        }

        private void ApplyCreaturePlayerState(CreaturePlayerNetState[] players)
        {
            _hostCreaturePlayerIds.Clear();
            int localPlayerId = _session != null ? _session.LocalPlayerId : -1;

            for (int i = 0; i < players.Length; i++)
            {
                var ps = players[i];
                if (ps.PlayerId < 0 || ps.PlayerId == localPlayerId)
                    continue;

                _hostCreaturePlayerIds.Add(ps.PlayerId);
                AddOrUpdateRemoteCreaturePlayer(ps);
            }

            _localCreaturePlayerIds.Clear();
            foreach (var pair in _remoteCreaturePlayers)
                _localCreaturePlayerIds.Add(pair.Key);

            for (int i = 0; i < _localCreaturePlayerIds.Count; i++)
            {
                int playerId = _localCreaturePlayerIds[i];
                if (!_hostCreaturePlayerIds.Contains(playerId))
                    RemoveRemoteCreaturePlayer(playerId);
            }
        }

        private void AddOrUpdateRemoteCreaturePlayer(CreaturePlayerNetState state)
        {
            string displayName = GetRemotePlayerDisplayName(state.PlayerId);
            if (_remoteCreaturePlayers.TryGetValue(state.PlayerId, out var existing) && existing != null)
            {
                if (existing.MatchesIdentity(state))
                {
                    existing.SetDisplayName(displayName);
                    existing.ApplyState(state, displayName);
                    return;
                }

                RemoveRemoteCreaturePlayer(state.PlayerId);
            }

            if (_remotePlayers.TryGetValue(state.PlayerId, out var ghost))
            {
                if (ghost != null)
                    DestroyNetworkObject(ghost.gameObject);
                _remotePlayers.Remove(state.PlayerId);
            }
            var remote = RemotePlayerCreature.Create(state, displayName);
            _remoteCreaturePlayers[state.PlayerId] = remote;
        }

        private void RemoveRemoteCreaturePlayer(int playerId)
        {
            if (!_remoteCreaturePlayers.TryGetValue(playerId, out var creature))
                return;

            _remoteCreaturePlayers.Remove(playerId);
            _lastRemoteCreatureInputHostTicks.Remove(playerId);
            _lastRemoteCreatureActionHostTicks.Remove(playerId);
            _lastResolvedRemoteCreatureActionSourceTicks.Remove(playerId);
            _lastResolvedRemoteCreatureActionHostTicks.Remove(playerId);
            _remoteCreatureActions.Remove(playerId);
            if (creature != null)
                DestroyNetworkObject(creature.gameObject);
        }

        private void ApplyCreaturePlayerActionState(CreaturePlayerActionNetState[] actions)
        {
            if (actions == null || actions.Length == 0)
                return;

            int localPlayerId = _session != null ? _session.LocalPlayerId : -1;
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                if (action.PlayerId < 0 || action.PlayerId == localPlayerId)
                    continue;

                if (_remoteCreaturePlayers.TryGetValue(action.PlayerId, out var remote) && remote != null)
                    remote.ApplyActionFlags(action.ActionFlags);
            }
        }

        private void ApplyCreatureRivalState(CreatureRivalNetState[] rivals)
        {
            var creatureStage = _creatureStageCache;
            if (creatureStage == null)
                creatureStage = FindAnyObjectByType<CreatureStage>();
            _creatureStageCache = creatureStage;
            if (creatureStage == null)
                return;

            _hostCreatureRivalIds.Clear();
            for (int i = 0; i < rivals.Length; i++)
            {
                var rs = rivals[i];
                if (string.IsNullOrWhiteSpace(rs.CreatureId))
                    continue;

                if (!rs.Exists || !rs.IsAlive)
                {
                    creatureStage.RemoveNetworkRival(rs.CreatureId);
                    continue;
                }

                _hostCreatureRivalIds.Add(rs.CreatureId);
                creatureStage.AddOrUpdateNetworkRival(rs);
            }

            creatureStage.CopyNetworkRivalIds(_localCreatureRivalIds);
            for (int i = 0; i < _localCreatureRivalIds.Count; i++)
            {
                string localId = _localCreatureRivalIds[i];
                if (!_hostCreatureRivalIds.Contains(localId))
                    creatureStage.RemoveNetworkRival(localId);
            }
        }

        private void ApplyCreatureObjectiveState(CreatureObjectiveNetState objective)
        {
            var creatureStage = _creatureStageCache;
            if (creatureStage == null)
                creatureStage = FindAnyObjectByType<CreatureStage>();
            _creatureStageCache = creatureStage;
            creatureStage?.ApplyNetworkObjectiveSnapshot(objective);
        }

        private void ApplyCreatureWorldProgressionState(CreatureWorldProgressionNetState progression)
        {
            var creatureStage = _creatureStageCache;
            if (creatureStage == null)
                creatureStage = FindAnyObjectByType<CreatureStage>();
            _creatureStageCache = creatureStage;
            creatureStage?.ApplyNetworkWorldProgressionSnapshot(progression);
        }

        private Transform ResolveLocalVoiceTransform()
        {
            if (_localPlayer != null)
                return _localPlayer.transform;
            var creature = ResolveLocalCreature();
            if (creature != null)
                return creature.transform;
            return null;
        }

        private CreatureInstance ResolveLocalCreature()
        {
            if (_localCreature != null && _localCreature.IsPlayer)
                return _localCreature;

            _localCreature = FindLocalCreature();
            if (_localCreature != null)
                _voiceChat?.SetLocalPlayerTransform(_localCreature.transform);
            return _localCreature;
        }

        private CreatureInstance FindLocalCreature()
        {
            var controllers = FindObjectsByType<CreaturePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                var creature = controllers[i] != null ? controllers[i].GetComponent<CreatureInstance>() : null;
                if (creature != null && creature.IsPlayer)
                    return creature;
            }

            return null;
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

        private static CreatureInputActionFlags ReadCreatureActionFlags()
        {
            if (!InputManager.HasInstance)
                return CreatureInputActionFlags.None;

            var input = InputManager.Instance;
            return BuildCreatureActionFlags(
                input.AttackPressed,
                input.JumpPressed,
                input.InteractPressed);
        }

        public static CreatureInputActionFlags BuildCreatureActionFlags(
            bool attackPressed,
            bool guardPressed,
            bool abilityPressed)
        {
            CreatureInputActionFlags flags = CreatureInputActionFlags.None;
            if (attackPressed)
                flags |= CreatureInputActionFlags.Attack;
            if (guardPressed)
                flags |= CreatureInputActionFlags.Guard;
            if (abilityPressed)
                flags |= CreatureInputActionFlags.Ability;
            return flags;
        }

        public static CreatureInputActionFlags GetDominantCreatureAction(CreatureInputActionFlags actionFlags)
        {
            if ((actionFlags & CreatureInputActionFlags.Attack) != 0)
                return CreatureInputActionFlags.Attack;
            if ((actionFlags & CreatureInputActionFlags.Ability) != 0)
                return CreatureInputActionFlags.Ability;
            if ((actionFlags & CreatureInputActionFlags.Guard) != 0)
                return CreatureInputActionFlags.Guard;
            return CreatureInputActionFlags.None;
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
            _partPickupCache = FindObjectsByType<CellPartPickup>(FindObjectsInactive.Exclude);
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
            if (_localPlayer != null)
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
            if (_remoteCreaturePlayers.TryGetValue(playerId, out var creature) && creature != null)
                return creature.transform;

            return _remotePlayers.TryGetValue(playerId, out var ghost) && ghost != null
                ? ghost.transform
                : null;
        }

        private void OnRemotePlayerLeft(MultiplayerPlayerLeftEvent e)
        {
            RemoveRemoteCreaturePlayer(e.PlayerId);
            if (_remotePlayers.TryGetValue(e.PlayerId, out var ghost))
            {
                if (ghost != null) DestroyNetworkObject(ghost.gameObject);
                _remotePlayers.Remove(e.PlayerId);
                _lastRemoteInputTicks.Remove(e.PlayerId);
                Debug.Log($"[NetworkTick] Remote player {e.PlayerId} left — ghost destroyed");
            }
        }

        private static void DestroyNetworkObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
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

        public static bool IsRemoteCreatureInputStale(
            uint hostTick,
            uint lastInputHostTick,
            uint staleTicks = RemoteCreatureInputStaleTicks)
        {
            if (hostTick < lastInputHostTick)
                return false;

            uint threshold = staleTicks == 0u ? 1u : staleTicks;
            return hostTick - lastInputHostTick >= threshold;
        }

        public static bool IsCreatureActionIntentExpired(
            uint hostTick,
            uint actionHostTick,
            uint ttlTicks = CreatureActionIntentTtlTicks)
        {
            if (hostTick < actionHostTick)
                return false;

            uint threshold = ttlTicks == 0u ? 1u : ttlTicks;
            return hostTick - actionHostTick >= threshold;
        }

        public static bool ShouldResolveRemoteCreatureAction(
            bool hasLastResolvedSourceTick,
            uint lastResolvedSourceTick,
            bool hasLastResolvedHostTick,
            uint lastResolvedHostTick,
            uint incomingSourceTick,
            uint hostTick,
            uint cooldownTicks = RemoteCreatureActionResolveCooldownTicks)
        {
            if (hasLastResolvedSourceTick && incomingSourceTick == lastResolvedSourceTick)
                return false;
            if (!hasLastResolvedHostTick || hostTick < lastResolvedHostTick)
                return true;

            uint threshold = cooldownTicks == 0u ? 1u : cooldownTicks;
            return hostTick - lastResolvedHostTick >= threshold;
        }

        public static bool ShouldApplyStageTransition(
            bool isLocalHost,
            int fromPlayerId,
            int hostPlayerId,
            GameStageType currentStage,
            GameStageType targetStage,
            bool isTransitioning)
        {
            if (isLocalHost || isTransitioning)
                return false;

            if (hostPlayerId < 0 || fromPlayerId != hostPlayerId)
                return false;

            if (!StageManager.IsStageSupported(targetStage))
                return false;

            return currentStage != targetStage;
        }

        public static float ComputeRemoteInputMoveSpeed(float baseSpeed, float remoteMass)
        {
            return Mathf.Max(0f, baseSpeed) * PlayerCell.ComputeSizeSpeedPenalty(remoteMass);
        }
    }
}
