using System;
using System.Collections.Generic;
using System.Text;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    public enum SessionState { Offline, Hosting, Joining, Connected, Disconnected }

    /// <summary>
    /// Platform-agnostic transport interface.
    /// Swap <see cref="NullNetworkTransport"/> for a real NGO or relay implementation.
    /// </summary>
    public interface INetworkTransport
    {
        bool   IsConnected { get; }
        void   Send(int targetPlayerId, byte[] data);
        void   Broadcast(byte[] data);
        byte[] TryReceive(out int fromPlayerId);
        void   Disconnect();
    }

    /// <summary>No-op transport for offline / EditMode test use.</summary>
    public sealed class NullNetworkTransport : INetworkTransport
    {
        public bool IsConnected => false;
        public void   Send(int targetPlayerId, byte[] data) { }
        public void   Broadcast(byte[] data) { }
        public byte[] TryReceive(out int fromPlayerId) { fromPlayerId = -1; return null; }
        public void   Disconnect() { }
    }

    /// <summary>A remote player present in the current session.</summary>
    public sealed class RemotePlayer
    {
        public int    PlayerId     { get; }
        public string DisplayName { get; }
        public bool   IsReady     { get; private set; }

        public RemotePlayer(int id, string name) { PlayerId = id; DisplayName = name; }

        public void SetReady(bool ready)
        {
            IsReady = ready;
        }
    }

    /// <summary>A player-created clan inside the current multiplayer session.</summary>
    public sealed class MultiplayerClan
    {
        private readonly List<int> _memberPlayerIds = new List<int>();

        public int    ClanId         { get; }
        public string Name           { get; }
        public string Tag            { get; }
        public int    LeaderPlayerId { get; }

        public IReadOnlyList<int> MemberPlayerIds => _memberPlayerIds;

        public MultiplayerClan(int clanId, string name, string tag, int leaderPlayerId)
        {
            ClanId = clanId;
            Name = name;
            Tag = tag;
            LeaderPlayerId = leaderPlayerId;
            _memberPlayerIds.Add(leaderPlayerId);
        }

        public bool HasMember(int playerId) => _memberPlayerIds.Contains(playerId);

        public void AddMember(int playerId)
        {
            if (!_memberPlayerIds.Contains(playerId))
                _memberPlayerIds.Add(playerId);
        }

        public void RemoveMember(int playerId)
        {
            _memberPlayerIds.Remove(playerId);
        }
    }

    /// <summary>A server-filtered global chat message in the current multiplayer session.</summary>
    public sealed class MultiplayerChatMessage
    {
        public int    FromPlayerId { get; }
        public string SenderName   { get; }
        public string Message      { get; }
        public bool   IsLocal      { get; }

        public MultiplayerChatMessage(int fromPlayerId, string senderName, string message, bool isLocal)
        {
            FromPlayerId = fromPlayerId;
            SenderName = string.IsNullOrWhiteSpace(senderName) ? $"Player {fromPlayerId}" : senderName;
            Message = message ?? string.Empty;
            IsLocal = isLocal;
        }
    }

    /// <summary>
    /// Manages session lifecycle: host, join, drop-in/drop-out, host migration.
    /// Platform-agnostic: inject a real <see cref="INetworkTransport"/> when NGO/relay is available.
    ///
    /// Session model:
    ///   - Unlimited players, invite-by-code (6-char alphanumeric).
    ///   - Player 0 is always the initial host.
    ///   - On host disconnect, the lowest remaining player id is promoted to host.
    ///   - Drop-in/drop-out: players may join or leave any time.
    /// </summary>
    public sealed class MultiplayerSessionManager
    {
        public const int MaxPlayers       = int.MaxValue;
        public const bool HasPlayerLimit  = false;
        public const int SessionCodeLength = 6;
        public const string DefaultLobbyId = "session";
        public const int ClanNameMinLength = 3;
        public const int ClanNameMaxLength = 24;
        public const int ClanTagMinLength  = 2;
        public const int ClanTagMaxLength  = 5;
        public const int PartyNameMinLength = ClanNameMinLength;
        public const int PartyNameMaxLength = ClanNameMaxLength;
        public const int PartyTagMinLength  = ClanTagMinLength;
        public const int PartyTagMaxLength  = ClanTagMaxLength;
        public const int ChatMessageMaxLength = 160;
        public const int ChatHistoryMaxMessages = 50;

        private static readonly string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly INetworkTransport   _transport;
        private readonly List<RemotePlayer> _remotePlayers = new List<RemotePlayer>();
        private readonly List<MultiplayerClan> _clans = new List<MultiplayerClan>();
        private readonly List<MultiplayerChatMessage> _chatMessages = new List<MultiplayerChatMessage>();

        private int    _localPlayerId = -1;
        private int    _hostPlayerId  = -1;
        private int    _localClanId   = -1;
        private int    _nextLocalClanId = 1;
        private bool   _localReady;
        private string _sessionCode;
        private string _localLobbyId = DefaultLobbyId;
        private SessionMapDefinition? _sessionMap;
        private readonly Dictionary<int, string> _playerLobbyIds = new Dictionary<int, string>();
        private NetworkQualitySnapshot _networkQuality;

        // ---- public read properties ----

        public SessionState State         { get; private set; } = SessionState.Offline;
        public string       SessionCode   => _sessionCode;
        public int          LocalPlayerId => _localPlayerId;
        public int          HostPlayerId  => _hostPlayerId;
        public bool         IsHost        => _localPlayerId >= 0 && _localPlayerId == _hostPlayerId;
        public bool         LocalReady    => _localReady;
        public int          LocalClanId   => _localClanId;
        public string       LocalLobbyId  => _localLobbyId;
        public MultiplayerClan LocalClan  => _clans.Find(c => c.ClanId == _localClanId);
        public int          LocalPartyId  => _localClanId;
        public MultiplayerClan LocalParty => LocalClan;
        public bool         HasSessionMap => _sessionMap.HasValue;
        public SessionMapDefinition SessionMap => _sessionMap.GetValueOrDefault();
        public bool         CanStartGame  => State != SessionState.Offline
            && IsHost
            && AllPlayersReady();

        /// <summary>Total players in the session (local + remote).</summary>
        public int PlayerCount => State == SessionState.Offline ? 0 : _remotePlayers.Count + 1;

        public IReadOnlyList<RemotePlayer> RemotePlayers => _remotePlayers;
        public IReadOnlyList<MultiplayerClan> Clans => _clans;
        public IReadOnlyList<MultiplayerClan> Parties => _clans;
        public IReadOnlyList<MultiplayerChatMessage> ChatMessages => _chatMessages;
        public NetworkQualitySnapshot NetworkQuality => _networkQuality;

        // ---- events ----

        /// <summary>A remote player has joined the session.</summary>
        public event Action<RemotePlayer> OnPlayerJoined;

        /// <summary>A player has left. bool = wasHost.</summary>
        public event Action<int, bool> OnPlayerLeft;

        /// <summary>Host authority has migrated to a new player ID.</summary>
        public event Action<int> OnHostMigrated;

        /// <summary>A clan has been created in this session.</summary>
        public event Action<MultiplayerClan> OnClanCreated;
        public event Action<MultiplayerClan> OnPartyCreated;

        /// <summary>A player changed ready state in the lobby.</summary>
        public event Action<int, bool> OnPlayerReadyChanged;

        /// <summary>The host started the shared game.</summary>
        public event Action OnGameStarted;

        /// <summary>The relay sent or refreshed the server-owned session map.</summary>
        public event Action<SessionMapDefinition> OnSessionMapChanged;

        /// <summary>A player moved between relay bandwidth lobbies.</summary>
        public event Action<int, string> OnPlayerLobbyChanged;

        /// <summary>A server-filtered global chat message was received for this session.</summary>
        public event Action<MultiplayerChatMessage> OnChatMessageReceived;

        /// <summary>Network quality changed due to heartbeat, packet loss, reconnect, or snapshot age.</summary>
        public event Action<NetworkQualitySnapshot> OnNetworkQualityChanged;

        // ---- constructor ----

        public MultiplayerSessionManager(INetworkTransport transport = null)
        {
            _transport = transport ?? new NullNetworkTransport();
        }

        // ---- session lifecycle ----

        /// <summary>
        /// Creates a new hosted session with a random 6-char invite code.
        /// Local player becomes host (id = 0).
        /// Returns the session code.
        /// </summary>
        public string CreateSession()
        {
            _sessionCode   = GenerateCode();
            _localPlayerId = 0;
            _hostPlayerId  = 0;
            _localClanId   = -1;
            _localReady    = true;
            _remotePlayers.Clear();
            _clans.Clear();
            _chatMessages.Clear();
            _sessionMap = null;
            _networkQuality = default;
            ResetLobbyState();
            State          = SessionState.Hosting;

            EventBus.Publish(new MultiplayerSessionStartedEvent
            {
                SessionCode = _sessionCode,
                PlayerCount = 1
            });

            return _sessionCode;
        }

        /// <summary>
        /// Applies the session details assigned by the relay after a create request succeeds.
        /// </summary>
        public void ApplyCreatedSession(string code, int localPlayerId)
        {
            if (!IsValidAssignedSession(code, localPlayerId)) return;

            _remotePlayers.Clear();
            _clans.Clear();
            _sessionCode   = code.ToUpperInvariant();
            _localPlayerId = localPlayerId;
            _hostPlayerId  = localPlayerId;
            _localClanId   = -1;
            _localReady    = true;
            _chatMessages.Clear();
            _sessionMap = null;
            _networkQuality = default;
            ResetLobbyState();
            State          = SessionState.Hosting;

            EventBus.Publish(new MultiplayerSessionStartedEvent
            {
                SessionCode = _sessionCode,
                PlayerCount = 1
            });
        }

        /// <summary>
        /// Applies the session details assigned by the relay after a join request succeeds.
        /// </summary>
        public void ApplyJoinedSession(string code, int localPlayerId, int hostPlayerId)
        {
            if (!IsValidAssignedSession(code, localPlayerId)) return;
            if (hostPlayerId < 0) return;

            _remotePlayers.Clear();
            _clans.Clear();
            _sessionCode   = code.ToUpperInvariant();
            _localPlayerId = localPlayerId;
            _hostPlayerId  = hostPlayerId;
            _localClanId   = -1;
            _localReady    = false;
            _chatMessages.Clear();
            _sessionMap = null;
            _networkQuality = default;
            ResetLobbyState();
            State          = localPlayerId == hostPlayerId ? SessionState.Hosting : SessionState.Connected;

            if (hostPlayerId != localPlayerId)
                AddRemotePlayer(hostPlayerId, $"Player {hostPlayerId}");

            EventBus.Publish(new MultiplayerSessionStartedEvent
            {
                SessionCode = _sessionCode,
                PlayerCount = PlayerCount
            });
        }

        /// <summary>
        /// Attempts to join an existing session by invite code.
        /// Returns false if the code is invalid or the session is full.
        /// </summary>
        public bool JoinSession(string code, int localPlayerId)
        {
            if (string.IsNullOrEmpty(code) || code.Length != SessionCodeLength) return false;
            if (localPlayerId < 0) return false;

            _sessionCode   = code.ToUpperInvariant();
            _localPlayerId = localPlayerId;
            _hostPlayerId  = 0; // host is always player 0
            _localClanId   = -1;
            _localReady    = false;
            _sessionMap = null;
            _remotePlayers.Clear();
            _clans.Clear();
            _chatMessages.Clear();
            _networkQuality = default;
            ResetLobbyState();
            State          = SessionState.Connected;
            return true;
        }

        /// <summary>Applies the relay-owned map definition for the current shared session.</summary>
        public bool ApplySessionMap(SessionMapDefinition map)
        {
            if (!map.IsValid)
                return false;

            _sessionMap = map;
            OnSessionMapChanged?.Invoke(map);
            return true;
        }

        public bool TryGetSessionMap(out SessionMapDefinition map)
        {
            if (_sessionMap.HasValue)
            {
                map = _sessionMap.Value;
                return true;
            }

            map = default;
            return false;
        }

        /// <summary>Moves the local player into a relay lobby used to limit high-rate packet fan-out.</summary>
        public bool SetLocalLobby(string lobbyId)
        {
            if (State == SessionState.Offline || _localPlayerId < 0) return false;
            return ApplyPlayerLobby(_localPlayerId, lobbyId);
        }

        /// <summary>Applies a relay-reported bandwidth lobby change for any known player.</summary>
        public bool ApplyPlayerLobby(int playerId, string lobbyId)
        {
            if (State == SessionState.Offline || !IsKnownPlayer(playerId)) return false;
            if (!IsValidLobbyId(lobbyId)) return false;

            if (playerId == _localPlayerId)
                _localLobbyId = lobbyId;

            _playerLobbyIds[playerId] = lobbyId;
            OnPlayerLobbyChanged?.Invoke(playerId, lobbyId);
            EventBus.Publish(new MultiplayerPlayerLobbyChangedEvent { PlayerId = playerId, LobbyId = lobbyId });
            return true;
        }

        public string GetPlayerLobby(int playerId)
        {
            return _playerLobbyIds.TryGetValue(playerId, out string lobbyId)
                ? lobbyId
                : DefaultLobbyId;
        }

        /// <summary>Changes the local player's lobby readiness.</summary>
        public bool SetLocalReady(bool ready)
        {
            if (State == SessionState.Offline || _localPlayerId < 0) return false;
            return ApplyPlayerReady(_localPlayerId, ready);
        }

        /// <summary>Applies a readiness update from the relay or another client.</summary>
        public bool ApplyPlayerReady(int playerId, bool ready)
        {
            if (State == SessionState.Offline || !IsKnownPlayer(playerId)) return false;

            if (playerId == _localPlayerId)
            {
                if (_localReady == ready) return true;
                _localReady = ready;
            }
            else
            {
                var remote = _remotePlayers.Find(p => p.PlayerId == playerId);
                if (remote == null) return false;
                if (remote.IsReady == ready) return true;
                remote.SetReady(ready);
            }

            OnPlayerReadyChanged?.Invoke(playerId, ready);
            EventBus.Publish(new MultiplayerPlayerReadyChangedEvent { PlayerId = playerId, IsReady = ready });
            return true;
        }

        /// <summary>Applies the host-authoritative start signal.</summary>
        public bool ApplyGameStarted()
        {
            if (State == SessionState.Offline) return false;
            OnGameStarted?.Invoke();
            EventBus.Publish(new MultiplayerGameStartedEvent { SessionCode = _sessionCode });
            return true;
        }

        /// <summary>Applies latest network quality diagnostics from the tick manager.</summary>
        public bool ApplyNetworkQuality(NetworkQualitySnapshot snapshot)
        {
            if (State == SessionState.Offline)
                return false;

            _networkQuality = snapshot;
            OnNetworkQualityChanged?.Invoke(_networkQuality);
            EventBus.Publish(new MultiplayerNetworkQualityChangedEvent
            {
                PingMs = _networkQuality.PingMs,
                RttTrendMs = _networkQuality.RttTrendMs,
                PacketLossRatio = _networkQuality.PacketLossRatio,
                LastAuthoritativeSnapshotAgeSeconds = _networkQuality.LastAuthoritativeSnapshotAgeSeconds,
                ReconnectAttempts = _networkQuality.ReconnectAttempts,
                Quality = _networkQuality.Quality
            });
            return true;
        }

        /// <summary>Marks the session as temporarily disconnected without losing lobby, chat, clan, or map state.</summary>
        public bool BeginReconnectAttempt()
        {
            if (State == SessionState.Offline)
                return false;

            State = SessionState.Disconnected;
            var next = _networkQuality;
            next.ReconnectAttempts = Mathf.Max(0, next.ReconnectAttempts) + 1;
            next.Quality = NetworkConnectionQuality.Reconnecting;
            return ApplyNetworkQuality(next);
        }

        /// <summary>Restores connected/hosting state after a successful authoritative snapshot resync.</summary>
        public bool CompleteReconnect()
        {
            if (State == SessionState.Offline)
                return false;

            State = IsHost ? SessionState.Hosting : SessionState.Connected;
            var next = _networkQuality;
            next.LastAuthoritativeSnapshotAgeSeconds = 0f;
            next.Quality = NetworkConnectionQuality.Healthy;
            return ApplyNetworkQuality(next);
        }

        /// <summary>Applies a server-filtered global chat message for the current session.</summary>
        public bool ApplyChatMessage(int fromPlayerId, string message)
        {
            if (State == SessionState.Offline) return false;
            if (fromPlayerId < 0) return false;
            if (!TryNormalizeChatMessage(message, out string normalizedMessage)) return false;

            var chatMessage = new MultiplayerChatMessage(
                fromPlayerId,
                GetPlayerDisplayName(fromPlayerId),
                normalizedMessage,
                fromPlayerId == _localPlayerId);
            _chatMessages.Add(chatMessage);
            while (_chatMessages.Count > ChatHistoryMaxMessages)
                _chatMessages.RemoveAt(0);

            OnChatMessageReceived?.Invoke(chatMessage);
            EventBus.Publish(new MultiplayerChatMessageEvent
            {
                FromPlayerId = chatMessage.FromPlayerId,
                SenderName = chatMessage.SenderName,
                Message = chatMessage.Message,
                IsLocal = chatMessage.IsLocal
            });
            return true;
        }

        /// <summary>
        /// Creates a clan for the local player. Returns false when the session is offline,
        /// the local player already belongs to a clan, or the name/tag is invalid.
        /// </summary>
        public bool CreateClan(string clanName, string clanTag)
        {
            return TryApplyClanCreated(_nextLocalClanId++, clanName, clanTag, _localPlayerId, createdByLocalPlayer: true);
        }

        /// <summary>
        /// Creates a party for the local player. This is an alias of <see cref="CreateClan"/>.
        /// </summary>
        public bool CreateParty(string partyName, string partyTag)
        {
            return CreateClan(partyName, partyTag);
        }

        /// <summary>Applies a clan created by the relay or another client.</summary>
        public bool ApplyClanCreated(int clanId, string clanName, string clanTag, int leaderPlayerId)
        {
            return TryApplyClanCreated(clanId, clanName, clanTag, leaderPlayerId, leaderPlayerId == _localPlayerId);
        }

        /// <summary>
        /// Applies a party created by the relay or another client. This is an alias of <see cref="ApplyClanCreated"/>.
        /// </summary>
        public bool ApplyPartyCreated(int partyId, string partyName, string partyTag, int leaderPlayerId)
        {
            return ApplyClanCreated(partyId, partyName, partyTag, leaderPlayerId);
        }

        /// <summary>
        /// Registers an inbound remote player. Valid when <see cref="State"/> is Hosting or Connected.
        /// Returns false if the session is full.
        /// </summary>
        public bool AddRemotePlayer(int playerId, string displayName)
        {
            if (State != SessionState.Hosting && State != SessionState.Connected) return false;
            if (playerId < 0) return false;
            if (playerId == _localPlayerId) return false;
            if (_remotePlayers.Exists(p => p.PlayerId == playerId)) return false;

            var p = new RemotePlayer(playerId, displayName);
            _remotePlayers.Add(p);

            OnPlayerJoined?.Invoke(p);
            EventBus.Publish(new MultiplayerPlayerJoinedEvent { PlayerId = playerId });
            return true;
        }

        /// <summary>
        /// Removes a player (voluntary leave or disconnect).
        /// Triggers host migration if the host left and other players remain.
        /// </summary>
        public void RemovePlayer(int playerId)
        {
            bool wasHost = playerId == _hostPlayerId;
            if (playerId == _localPlayerId)
            {
                if (wasHost && _remotePlayers.Count > 0)
                {
                    _localPlayerId = -1;
                    _hostPlayerId = _remotePlayers[0].PlayerId;
                    RemovePlayerFromClans(playerId);
                    OnPlayerLeft?.Invoke(playerId, wasHost);
                    EventBus.Publish(new MultiplayerPlayerLeftEvent { PlayerId = playerId, WasHost = wasHost });
                    OnHostMigrated?.Invoke(_hostPlayerId);
                    EventBus.Publish(new MultiplayerHostMigratedEvent { NewHostPlayerId = _hostPlayerId });
                    return;
                }

                LeaveSession();
                return;
            }

            _remotePlayers.RemoveAll(p => p.PlayerId == playerId);
            _playerLobbyIds.Remove(playerId);
            RemovePlayerFromClans(playerId);

            OnPlayerLeft?.Invoke(playerId, wasHost);
            EventBus.Publish(new MultiplayerPlayerLeftEvent { PlayerId = playerId, WasHost = wasHost });

            if (wasHost)
            {
                if (_remotePlayers.Count > 0)
                {
                    _hostPlayerId = _remotePlayers[0].PlayerId;
                    OnHostMigrated?.Invoke(_hostPlayerId);
                    EventBus.Publish(new MultiplayerHostMigratedEvent { NewHostPlayerId = _hostPlayerId });
                }
                else if (_localPlayerId >= 0)
                {
                    LeaveSession();
                }
                else
                {
                    LeaveSession();
                }
            }
        }

        /// <summary>Updates host authority after the relay elects a new host.</summary>
        public void ApplyHostMigration(int newHostPlayerId)
        {
            if (newHostPlayerId < 0) return;
            _hostPlayerId = newHostPlayerId;
            State = _localPlayerId == _hostPlayerId ? SessionState.Hosting : SessionState.Connected;
            OnHostMigrated?.Invoke(_hostPlayerId);
            EventBus.Publish(new MultiplayerHostMigratedEvent { NewHostPlayerId = _hostPlayerId });
        }

        /// <summary>Disconnects the local player from the session and resets all state.</summary>
        public void LeaveSession()
        {
            _transport.Disconnect();
            _remotePlayers.Clear();
            _clans.Clear();
            _chatMessages.Clear();
            _sessionCode   = null;
            _localPlayerId = -1;
            _hostPlayerId  = -1;
            _localClanId   = -1;
            _localReady    = false;
            _sessionMap    = null;
            _networkQuality = default;
            ResetLobbyState();
            State          = SessionState.Offline;
        }

        // ---- helpers ----

        private bool TryApplyClanCreated(int clanId, string clanName, string clanTag, int leaderPlayerId, bool createdByLocalPlayer)
        {
            if (State != SessionState.Hosting && State != SessionState.Connected) return false;
            if (!IsKnownPlayer(leaderPlayerId)) return false;
            if (!TryNormalizeClan(clanName, clanTag, out string name, out string tag)) return false;
            if (clanId <= 0 || _clans.Exists(c => c.ClanId == clanId)) return false;
            if (_clans.Exists(c => string.Equals(c.Tag, tag, StringComparison.OrdinalIgnoreCase))) return false;
            if (createdByLocalPlayer && _localClanId != -1) return false;
            if (PlayerHasClan(leaderPlayerId)) return false;

            var clan = new MultiplayerClan(clanId, name, tag, leaderPlayerId);
            _clans.Add(clan);
            if (createdByLocalPlayer)
                _localClanId = clanId;

            OnClanCreated?.Invoke(clan);
            OnPartyCreated?.Invoke(clan);
            EventBus.Publish(new MultiplayerClanCreatedEvent
            {
                ClanId = clan.ClanId,
                Name = clan.Name,
                Tag = clan.Tag,
                LeaderPlayerId = clan.LeaderPlayerId
            });
            return true;
        }

        private bool PlayerHasClan(int playerId)
        {
            return _clans.Exists(c => c.HasMember(playerId));
        }

        private string GetPlayerDisplayName(int playerId)
        {
            if (playerId == _localPlayerId) return "You";
            var remote = _remotePlayers.Find(p => p.PlayerId == playerId);
            return remote != null ? remote.DisplayName : $"Player {playerId}";
        }

        private bool IsKnownPlayer(int playerId)
        {
            return playerId == _localPlayerId
                || _remotePlayers.Exists(p => p.PlayerId == playerId);
        }

        private bool AllPlayersReady()
        {
            if (!_localReady) return false;
            foreach (var player in _remotePlayers)
            {
                if (!player.IsReady)
                    return false;
            }
            return true;
        }

        private void RemovePlayerFromClans(int playerId)
        {
            for (int i = _clans.Count - 1; i >= 0; i--)
            {
                var clan = _clans[i];
                clan.RemoveMember(playerId);
                if (clan.MemberPlayerIds.Count == 0 || clan.LeaderPlayerId == playerId)
                    _clans.RemoveAt(i);
            }

            if (_localClanId != -1 && !_clans.Exists(c => c.ClanId == _localClanId))
                _localClanId = -1;
        }

        private void ResetLobbyState()
        {
            _localLobbyId = DefaultLobbyId;
            _playerLobbyIds.Clear();
            if (_localPlayerId >= 0)
                _playerLobbyIds[_localPlayerId] = _localLobbyId;
        }

        private static bool IsValidLobbyId(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId) || lobbyId.Length > 24) return false;
            for (int i = 0; i < lobbyId.Length; i++)
            {
                char c = lobbyId[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return false;
            }
            return true;
        }

        private static bool TryNormalizeClan(string clanName, string clanTag, out string normalizedName, out string normalizedTag)
        {
            normalizedName = clanName?.Trim();
            normalizedTag = clanTag?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(normalizedName) || string.IsNullOrEmpty(normalizedTag)) return false;
            if (normalizedName.Length < ClanNameMinLength || normalizedName.Length > ClanNameMaxLength) return false;
            if (normalizedTag.Length < ClanTagMinLength || normalizedTag.Length > ClanTagMaxLength) return false;

            for (int i = 0; i < normalizedTag.Length; i++)
            {
                char c = normalizedTag[i];
                if (!char.IsLetterOrDigit(c)) return false;
            }

            return true;
        }

        public static bool TryNormalizeChatMessage(string message, out string normalizedMessage)
        {
            normalizedMessage = null;
            if (string.IsNullOrWhiteSpace(message)) return false;

            var sb = new StringBuilder(message.Length);
            bool previousWhitespace = false;
            foreach (char c in message)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    return false;

                if (char.IsWhiteSpace(c))
                {
                    if (!previousWhitespace)
                    {
                        sb.Append(' ');
                        previousWhitespace = true;
                    }
                    continue;
                }

                sb.Append(c);
                previousWhitespace = false;
            }

            normalizedMessage = sb.ToString().Trim();
            return normalizedMessage.Length > 0 && normalizedMessage.Length <= ChatMessageMaxLength;
        }

        private static bool IsValidAssignedSession(string code, int localPlayerId)
        {
            return !string.IsNullOrEmpty(code)
                && code.Length == SessionCodeLength
                && localPlayerId >= 0;
        }

        private static string GenerateCode()
        {
            var rng = new System.Random();
            var sb  = new StringBuilder(SessionCodeLength);
            for (int i = 0; i < SessionCodeLength; i++)
                sb.Append(CodeChars[rng.Next(CodeChars.Length)]);
            return sb.ToString();
        }
    }
}
