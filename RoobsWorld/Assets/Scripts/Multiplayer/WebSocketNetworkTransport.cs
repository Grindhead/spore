using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    public readonly struct PublicLobbyInfo
    {
        public readonly string Code;
        public readonly string ServerName;
        public readonly int Players;
        public readonly int MaxPlayers;

        public PublicLobbyInfo(string code, string serverName, int players, int maxPlayers)
        {
            Code = code;
            ServerName = serverName;
            Players = players;
            MaxPlayers = maxPlayers;
        }
    }

    /// <summary>
    /// <see cref="INetworkTransport"/> implementation backed by a WebSocket relay server.
    /// Requires Unity 2021.2+ (.NET Standard 2.1).
    ///
    /// Usage:
    ///   var transport = new WebSocketNetworkTransport("ws://your-server:7788");
    ///   await transport.ConnectAsync();
    ///   // pass to MultiplayerSessionManager
    ///
    /// All Unity-facing methods (Send, Broadcast, TryReceive, Disconnect) are main-thread safe.
    /// The receive loop runs on a background task.
    /// </summary>
    public sealed class WebSocketNetworkTransport : INetworkTransport, IDisposable
    {
        // ---- public events (raised on main thread via queue) ----

        /// <summary>Fired when the server assigns us a player ID (after create or join).</summary>
        public event Action<int>         OnAssignedPlayerId;

        /// <summary>Fired when the server confirms our session code (after create).</summary>
        public event Action<string, int> OnSessionCreated;   // (code, playerId)

        /// <summary>Fired when we successfully joined a session.</summary>
        public event Action<string, int, int> OnSessionJoined; // (code, playerId, hostPlayerId)

        /// <summary>Fired when the server reports an error (not_found, full, already_in_session).</summary>
        public event Action<string>      OnError;

        /// <summary>Fired when the relay reports that another player joined this session.</summary>
        public event Action<int>         OnRemotePlayerJoined;

        /// <summary>Fired when the relay reports that a player left this session. bool = wasHost.</summary>
        public event Action<int, bool>   OnRemotePlayerLeft;

        /// <summary>Fired when the relay migrates host authority.</summary>
        public event Action<int>         OnHostMigrated;

        /// <summary>Fired when a clan is created in the current session.</summary>
        public event Action<int, string, string, int> OnClanCreated; // (clanId, name, tag, leaderPlayerId)
        /// <summary>Fired when a party is created in the current session.</summary>
        public event Action<int, string, string, int> OnPartyCreated; // (partyId, name, tag, leaderPlayerId)

        /// <summary>Fired when a lobby player changes ready state.</summary>
        public event Action<int, bool>   OnPlayerReadyChanged;

        /// <summary>Fired when the relay moves a player into a bandwidth lobby.</summary>
        public event Action<int, string> OnPlayerLobbyChanged;

        /// <summary>Fired when the host starts the shared game.</summary>
        public event Action              OnGameStarted;

        /// <summary>Fired when the relay sends the server-owned shared map definition.</summary>
        public event Action<SessionMapDefinition> OnSessionMapReceived;

        /// <summary>Fired when the relay returns the current public lobby list.</summary>
        public event Action<IReadOnlyList<PublicLobbyInfo>> OnPublicLobbies;

        // ---- private state ----

        private readonly string            _serverUrl;
        private readonly string            _gameToken;   // appended as ?token= on connect
        private ClientWebSocket            _ws;
        private CancellationTokenSource    _cts;
        private int                        _localPlayerId = -1;

        // Thread-safe queues — receive loop writes, main thread reads.
        private readonly ConcurrentQueue<(int fromId, byte[] data)> _inboundData  = new();
        private readonly ConcurrentQueue<Action>                     _mainThreadCbs = new();

        public bool IsConnected =>
            _ws != null && _ws.State == WebSocketState.Open;

        public int LocalPlayerId => _localPlayerId;

        // ---- constructor ----

        /// <param name="serverUrl">WebSocket server URL, e.g. "ws://your-server:7788"</param>
        /// <param name="gameToken">
        ///   Shared game token matched against GAME_TOKEN on the server.
        ///   Leave null for local/dev servers running without a token.
        /// </param>
        public WebSocketNetworkTransport(string serverUrl, string gameToken = null)
        {
            _serverUrl  = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            _gameToken  = gameToken;
        }

        // ---- connect / disconnect ----

        /// <summary>
        /// Connects to the relay server. Call once before creating or joining a session.
        /// Throws on failure — catch and show an error in the lobby UI.
        /// </summary>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            _ws  = new ClientWebSocket();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Append auth token as query param if provided
            var url = string.IsNullOrEmpty(_gameToken)
                ? _serverUrl
                : $"{_serverUrl}?token={Uri.EscapeDataString(_gameToken)}";

            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            _ = ReceiveLoopAsync(_cts.Token);
        }

        /// <summary>Sends { type:"create" } to the server; response fires <see cref="OnSessionCreated"/>.</summary>
        public Task SendCreateSessionAsync(string serverName = null) => SendJsonAsync(new { type = "create", serverName });

        /// <summary>Sends { type:"join", code } to the server; response fires <see cref="OnSessionJoined"/>.</summary>
        public Task SendJoinSessionAsync(string code) => SendJsonAsync(new { type = "join", code });

        /// <summary>Sends { type:"list_public_lobbies" } to the server.</summary>
        public Task SendListPublicLobbiesAsync() => SendJsonAsync(new { type = "list_public_lobbies" });

        /// <summary>Sends { type:"create_clan", name, tag } to the server.</summary>
        public Task SendCreateClanAsync(string name, string tag) => SendJsonAsync(new { type = "create_clan", name, tag });
        /// <summary>Sends { type:"create_party", name, tag } to the server.</summary>
        public Task SendCreatePartyAsync(string name, string tag) => SendJsonAsync(new { type = "create_party", name, tag });

        /// <summary>Sends { type:"ready", isReady } to the server.</summary>
        public Task SendReadyAsync(bool isReady) => SendJsonAsync(new { type = "ready", isReady });

        /// <summary>Sends { type:"join_lobby", lobbyId } to the server to scope high-rate relay traffic.</summary>
        public Task SendJoinLobbyAsync(string lobbyId) => SendJsonAsync(new { type = "join_lobby", lobbyId });

        /// <summary>Sends { type:"start_game" } to the server; only the host may start.</summary>
        public Task SendStartGameAsync() => SendJsonAsync(new { type = "start_game" });

        // ---- INetworkTransport ----

        /// <inheritdoc/>
        public void Send(int targetPlayerId, byte[] data)
        {
            if (!IsConnected) return;
            var msg = new
            {
                type = "relay",
                to   = targetPlayerId,
                data = Convert.ToBase64String(data)
            };
            _ = SendJsonAsync(msg);
        }

        /// <inheritdoc/>
        public void Broadcast(byte[] data)
        {
            if (!IsConnected) return;
            var msg = new
            {
                type = "relay",
                to   = (object)null,
                data = Convert.ToBase64String(data)
            };
            _ = SendJsonAsync(msg);
        }

        /// <summary>
        /// Dequeues one inbound data packet. Call from the Unity main thread (e.g. Update or CellStage tick).
        /// Also drains any pending callbacks (event notifications from the receive loop).
        /// </summary>
        public byte[] TryReceive(out int fromPlayerId)
        {
            PumpEvents();

            if (_inboundData.TryDequeue(out var entry))
            {
                fromPlayerId = entry.fromId;
                return entry.data;
            }
            fromPlayerId = -1;
            return null;
        }

        /// <summary>Drains relay callbacks onto the Unity main thread without consuming data packets.</summary>
        public void PumpEvents()
        {
            while (_mainThreadCbs.TryDequeue(out var cb)) cb?.Invoke();
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            if (_ws == null) return;
            if (IsConnected)
                _ = SendJsonAsync(new { type = "leave" });

            _cts?.Cancel();
            _ws.Abort();
            _ws.Dispose();
            _ws = null;
        }

        public void Dispose() => Disconnect();

        // ---- receive loop ----

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buf = new byte[64 * 1024]; // 64 KB max message
            var sb  = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    HandleServerMessage(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocketTransport] receive error: {ex.Message}");
            }
        }

        // ---- message dispatch ----

        private void HandleServerMessage(string json)
        {
            // Minimal JSON parsing without extra dependencies.
            // We only need a handful of well-known fields.
            string type = ParseStringField(json, "type");
            if (type == null) return;

            switch (type)
            {
                case "created":
                {
                    string code     = ParseStringField(json, "code");
                    int    playerId = ParseIntField(json, "playerId");
                    _localPlayerId = playerId;
                    _mainThreadCbs.Enqueue(() =>
                    {
                        OnAssignedPlayerId?.Invoke(playerId);
                        OnSessionCreated?.Invoke(code, playerId);
                    });
                    break;
                }

                case "joined":
                {
                    string code         = ParseStringField(json, "code");
                    int    playerId     = ParseIntField(json, "playerId");
                    int    hostPlayerId = ParseIntField(json, "hostPlayerId");
                    _localPlayerId = playerId;
                    _mainThreadCbs.Enqueue(() =>
                    {
                        OnAssignedPlayerId?.Invoke(playerId);
                        OnSessionJoined?.Invoke(code, playerId, hostPlayerId);
                    });
                    break;
                }

                case "player_joined":
                {
                    int pid = ParseIntField(json, "playerId");
                    _mainThreadCbs.Enqueue(() => OnRemotePlayerJoined?.Invoke(pid));
                    break;
                }

                case "player_left":
                {
                    int  pid     = ParseIntField(json, "playerId");
                    bool wasHost = ParseBoolField(json, "wasHost");
                    _mainThreadCbs.Enqueue(() => OnRemotePlayerLeft?.Invoke(pid, wasHost));
                    break;
                }

                case "host_migrated":
                {
                    int newHostId = ParseIntField(json, "newHostId");
                    _mainThreadCbs.Enqueue(() => OnHostMigrated?.Invoke(newHostId));
                    break;
                }

                case "clan_created":
                case "party_created":
                {
                    int clanId = ParseIntField(json, "clanId");
                    string name = ParseStringField(json, "name");
                    string tag = ParseStringField(json, "tag");
                    int leaderPlayerId = ParseIntField(json, "leaderPlayerId");
                    _mainThreadCbs.Enqueue(() =>
                    {
                        OnClanCreated?.Invoke(clanId, name, tag, leaderPlayerId);
                        OnPartyCreated?.Invoke(clanId, name, tag, leaderPlayerId);
                    });
                    break;
                }

                case "player_ready":
                {
                    int pid = ParseIntField(json, "playerId");
                    bool ready = ParseBoolField(json, "isReady");
                    _mainThreadCbs.Enqueue(() => OnPlayerReadyChanged?.Invoke(pid, ready));
                    break;
                }

                case "player_lobby":
                {
                    int pid = ParseIntField(json, "playerId");
                    string lobbyId = ParseStringField(json, "lobbyId");
                    _mainThreadCbs.Enqueue(() => OnPlayerLobbyChanged?.Invoke(pid, lobbyId));
                    break;
                }

                case "game_started":
                {
                    _mainThreadCbs.Enqueue(() => OnGameStarted?.Invoke());
                    break;
                }

                case "creature_map":
                {
                    string encoded = ParseStringField(json, "map");
                    if (SessionMapDefinition.TryDecode(encoded, out var map))
                        _mainThreadCbs.Enqueue(() => OnSessionMapReceived?.Invoke(map));
                    else
                        Debug.LogWarning("[WebSocketTransport] Ignored invalid creature map payload.");
                    break;
                }

                case "public_lobbies":
                {
                    string encoded = ParseStringField(json, "lobbies") ?? "";
                    var lobbies = ParsePublicLobbies(encoded);
                    _mainThreadCbs.Enqueue(() => OnPublicLobbies?.Invoke(lobbies));
                    break;
                }

                case "data":
                {
                    int    from    = ParseIntField(json, "from");
                    string b64     = ParseStringField(json, "data");
                    if (b64 != null)
                    {
                        byte[] bytes = Convert.FromBase64String(b64);
                        _inboundData.Enqueue((from, bytes));
                    }
                    break;
                }

                case "error":
                {
                    string reason = ParseStringField(json, "reason") ?? "unknown";
                    _mainThreadCbs.Enqueue(() => OnError?.Invoke(reason));
                    break;
                }

                case "ping":
                    _ = SendJsonAsync(new { type = "pong" });
                    break;
            }
        }

        // ---- helpers ----

        private Task SendJsonAsync(object payload)
        {
            if (!IsConnected) return Task.CompletedTask;
            byte[] bytes = Encoding.UTF8.GetBytes(SimpleJson(payload));
            return _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: _cts?.Token ?? CancellationToken.None);
        }

        // Minimal serializer for the few outbound message shapes we use.
        // Avoids a dependency on Newtonsoft / Unity's JsonUtility for anon types.
        private static string SimpleJson(object o)
        {
            // Use reflection only for these small control messages — not for game data.
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var prop in o.GetType().GetProperties())
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(prop.Name).Append("\":");
                var val = prop.GetValue(o);
                if (val == null)               sb.Append("null");
                else if (val is string s)      sb.Append('"').Append(EscapeJsonString(s)).Append('"');
                else if (val is bool b)        sb.Append(b ? "true" : "false");
                else if (val is int i)         sb.Append(i);
                else                           sb.Append('"').Append(val).Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // Lightweight field extractors — no regex, no full parser needed for these tiny messages.
        private static string ParseStringField(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return null;
            start += search.Length;
            int end = json.IndexOf('"', start);
            return end < 0 ? null : json.Substring(start, end - start);
        }

        private static int ParseIntField(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return -1;
            start += search.Length;
            while (start < json.Length && json[start] == ' ') start++;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            return int.TryParse(json.Substring(start, end - start), out int v) ? v : -1;
        }

        private static bool ParseBoolField(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return false;
            start += search.Length;
            while (start < json.Length && json[start] == ' ') start++;
            return start + 4 <= json.Length && json.Substring(start, 4) == "true";
        }

        private static IReadOnlyList<PublicLobbyInfo> ParsePublicLobbies(string encoded)
        {
            var lobbies = new List<PublicLobbyInfo>();
            if (string.IsNullOrEmpty(encoded)) return lobbies;

            string[] entries = encoded.Split(';');
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(',');
                if (parts.Length < 3) continue;
                if (!int.TryParse(parts[1], out int players)) continue;
                if (!int.TryParse(parts[2], out int maxPlayers)) continue;
                if (string.IsNullOrEmpty(parts[0])) continue;
                string serverName = parts.Length >= 4
                    ? Uri.UnescapeDataString(parts[3])
                    : "Public Server";

                lobbies.Add(new PublicLobbyInfo(parts[0], serverName, players, maxPlayers));
            }

            return lobbies;
        }
    }
}
