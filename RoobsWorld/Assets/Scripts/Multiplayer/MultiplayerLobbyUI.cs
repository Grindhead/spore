using UnityEngine;
using UnityEngine.UI;
using RoobsWorld.Core;
using RoobsWorld.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// In-game overlay for hosting or joining a multiplayer session.
    ///
    /// Layout (built entirely in code, no prefab required):
    ///   - Root: full-screen semi-transparent panel, visible only when open.
    ///   - Offline panel: host button + clickable public lobby list.
    ///   - Session panel: shows connected players, leave button.
    ///   - Status bar: transient message (error, info) auto-hiding after 4 s.
    ///
    /// Usage: attach to a persistent canvas GameObject. Call <see cref="Open"/>/<see cref="Close"/>
    /// from a parent menu. The UI wires itself to a <see cref="MultiplayerSessionManager"/> via the
    /// <see cref="Bind"/> method.
    ///
    /// Not networked itself — all real networking goes through <see cref="MultiplayerSessionManager"/>.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class MultiplayerLobbyUI : MonoBehaviour
    {
        private const string DefaultRelayUrl = "wss://spore.fly.dev";
        private const string DefaultRelayName = "Official Europe";
        private static readonly Color FieldBackground = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.PanelBackground, 0.98f);
        private static readonly Color FieldBorder = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AccentStrong, 0.38f);

        [SerializeField] private string _relayUrl = DefaultRelayUrl;
        [SerializeField] private string _gameToken = "";
        [SerializeField] private string _selectedRelayName = DefaultRelayName;

        private MultiplayerSessionManager _session;
        private WebSocketNetworkTransport _transport;
        private readonly List<RelayEndpoint> _officialServers = new List<RelayEndpoint>();
        private readonly List<string> _selectedRelayUrls = new List<string>();

        // ---- Panels ----
        private GameObject _offlinePanel;
        private GameObject _publicLobbyListPanel;
        private RectTransform _publicLobbyContent;
        private GameObject _sessionPanel;
        private RectTransform _loadingSpinner;

        // ---- Controls ----
        private InputField _customRelayUrlField;
        private Text       _selectedRelayLabel;
        private Text       _sessionCodeLabel;
        private Text       _sessionSummaryLabel;
        private Text       _playerListText;
        private Text       _publicLobbyListText;
        private readonly List<Button> _publicLobbyButtons = new List<Button>();
        private readonly List<ScannedLobbyInfo> _scannedLobbyRows = new List<ScannedLobbyInfo>();
        private ScannedLobbyInfo _selectedScannedRow;
        private bool       _hasSelectedScannedRow;
        private Text       _statusText;
        private Button     _startButton;
        private Text       _startButtonLabel;
        private float      _statusHideAt;
        private bool       _isConnecting;
        private bool       _isScanningHosts;
        private float      _spinnerAngle;
        private float      _spinnerPulseTime;
        private float      _uiScale = 1f;
        private string     _activeServerName = "Public Server";
        private readonly List<Image> _spinnerSegments = new List<Image>();
        private Image _spinnerCore;

        private bool _isOpen;

        public Action OnStartGameRequested { get; set; }
        public Action OnClosed { get; set; }

        // ---- Setup ----

        private void Awake()
        {
            ResetOfficialServersToDefaults();
            ApplyRelayConfigFromResources();
            ApplyRelayConfigFromCommandLine();

            var canvas        = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvas.pixelPerfect = true;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // CanvasScaler already handles physical screen scaling; keep font sizes in
            // reference-resolution units so lower resolutions do not double-shrink text.
            _uiScale = 1f;
            BuildUI(_uiScale);
            RefreshSelectedRelayLabel();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Bind(null);
            _transport?.Disconnect();
            _transport = null;
        }

        /// <summary>Binds the UI to a session manager. Call before <see cref="Open"/>.</summary>
        public void Bind(MultiplayerSessionManager session)
        {
            if (_session != null)
            {
                _session.OnPlayerJoined  -= OnPlayerJoined;
                _session.OnPlayerLeft    -= OnPlayerLeft;
                _session.OnHostMigrated  -= OnHostMigrated;
                _session.OnPlayerReadyChanged -= OnPlayerReadyChanged;
                _session.OnGameStarted -= OnGameStarted;
            }

            _session = session;

            if (_session != null)
            {
                _session.OnPlayerJoined  += OnPlayerJoined;
                _session.OnPlayerLeft    += OnPlayerLeft;
                _session.OnHostMigrated  += OnHostMigrated;
                _session.OnPlayerReadyChanged += OnPlayerReadyChanged;
                _session.OnGameStarted += OnGameStarted;
            }
        }

        public void ConfigureRelay(string relayUrl, string gameToken = null)
        {
            if (!string.IsNullOrWhiteSpace(relayUrl))
                SelectRelayEndpoint("Custom Server", relayUrl, gameToken ?? "", disconnectExisting: true);
            else
                _gameToken = gameToken ?? "";
        }

        private void ResetOfficialServersToDefaults()
        {
            _officialServers.Clear();
            _officialServers.Add(new RelayEndpoint("Official Europe", "London", "wss://spore.fly.dev", ""));
            _officialServers.Add(new RelayEndpoint("Official US East", "Virginia", "wss://spore-us.fly.dev", ""));
            _officialServers.Add(new RelayEndpoint("Official Asia Pacific", "Tokyo / Singapore",
                new[] { "wss://spore-ap.fly.dev", "wss://spore-sg.fly.dev" }, ""));
            SelectRelayEndpoint(_officialServers[0], disconnectExisting: false);
        }

        private void ApplyRelayConfigFromResources()
        {
            var asset = Resources.Load<TextAsset>("multiplayer-relay");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            try
            {
                var config = JsonUtility.FromJson<RelayConfig>(asset.text);
                ApplyRelayConfig(config);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MultiplayerLobbyUI] Invalid multiplayer-relay config: " + ex.Message);
            }
        }

        private void ApplyRelayConfigFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-relayUrl")
                    SelectRelayEndpoint("Custom Server", args[i + 1], _gameToken, disconnectExisting: false);
                else if (args[i] == "-gameToken")
                    _gameToken = args[i + 1];
                else if (args[i] == "-relayName")
                    _selectedRelayName = args[i + 1];
            }
        }

        private void ApplyRelayConfig(RelayConfig config)
        {
            if (config == null) return;
            if (config.officialServers != null && config.officialServers.Length > 0)
            {
                _officialServers.Clear();
                foreach (var server in config.officialServers)
                {
                    if (server == null || server.GetRelayUrls().Length == 0)
                        continue;

                    string name = string.IsNullOrWhiteSpace(server.name)
                        ? "Official Server"
                        : server.name.Trim();
                    string region = string.IsNullOrWhiteSpace(server.region)
                        ? ""
                        : server.region.Trim();
                    _officialServers.Add(new RelayEndpoint(name, region, server.GetRelayUrls(), server.gameToken ?? config.gameToken ?? ""));
                }
            }

            if (!string.IsNullOrWhiteSpace(config.relayUrl))
                SelectRelayEndpoint(
                    string.IsNullOrWhiteSpace(config.name) ? DefaultRelayName : config.name,
                    config.relayUrl,
                    config.gameToken ?? _gameToken,
                    disconnectExisting: false);
            else if (_officialServers.Count > 0)
                SelectRelayEndpoint(_officialServers[0], disconnectExisting: false);
        }

        [Serializable]
        private sealed class RelayConfig
        {
            public string name = "";
            public string relayUrl = "";
            public string gameToken = "";
            public RelayServerConfig[] officialServers = Array.Empty<RelayServerConfig>();
        }

        [Serializable]
        private sealed class RelayServerConfig
        {
            public string name = "";
            public string region = "";
            public string relayUrl = "";
            public string[] relayUrls = Array.Empty<string>();
            public string gameToken = "";

            public string[] GetRelayUrls()
            {
                var urls = new List<string>();
                AddRelayUrl(urls, relayUrl);
                if (relayUrls != null)
                {
                    foreach (var url in relayUrls)
                        AddRelayUrl(urls, url);
                }

                return urls.ToArray();
            }
        }

        private readonly struct RelayEndpoint
        {
            public readonly string Name;
            public readonly string Region;
            public readonly string RelayUrl;
            public readonly string[] RelayUrls;
            public readonly string GameToken;

            public RelayEndpoint(string name, string region, string relayUrl, string gameToken)
                : this(name, region, new[] { relayUrl }, gameToken)
            {
            }

            public RelayEndpoint(string name, string region, string[] relayUrls, string gameToken)
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Server" : name.Trim();
                Region = string.IsNullOrWhiteSpace(region) ? "" : region.Trim();
                RelayUrls = NormalizeRelayUrls(relayUrls);
                RelayUrl = RelayUrls.Length > 0 ? RelayUrls[0] : "";
                GameToken = gameToken ?? "";
            }
        }

        private readonly struct ScannedLobbyInfo
        {
            public readonly RelayEndpoint Endpoint;
            public readonly PublicLobbyInfo Lobby;
            public readonly bool HasLobby;

            public ScannedLobbyInfo(RelayEndpoint endpoint, PublicLobbyInfo lobby, bool hasLobby)
            {
                Endpoint = endpoint;
                Lobby = lobby;
                HasLobby = hasLobby;
            }
        }

        // ---- Public API ----

        public void Open()
        {
            gameObject.SetActive(true);
            _isOpen = true;
            RefreshPanels();
            ShowConfiguredServerList();
            _ = RefreshPublicLobbiesAsync();
        }

        public void Close()
        {
            _isOpen = false;
            SetHostScanning(false);

            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }

        private void OnClosePressed()
        {
            Close();
        }

        // ---- Update ----

        private void Update()
        {
            if (!_isOpen) return;
            _transport?.PumpEvents();
            UpdateLoadingSpinner();
            if (_statusText != null && _statusText.enabled && Time.unscaledTime >= _statusHideAt)
                _statusText.enabled = false;
        }

        // ---- Button handlers ----

        private async void OnHostPressed()
        {
            if (_isConnecting) return;

            SelectEndpointForAction();
            _activeServerName = ResolveHostServerName();
            await ConnectAndCreateAsync();
        }

        private async void OnJoinServerPressed()
        {
            if (_isConnecting) return;
            if (!_hasSelectedScannedRow)
            {
                ShowStatus("Select a server first.", isError: true);
                return;
            }

            await JoinScannedServerAsync(_selectedScannedRow);
        }

        private async void OnPublicLobbyPressed(ScannedLobbyInfo row)
        {
            if (_isConnecting) return;

            _selectedScannedRow = row;
            _hasSelectedScannedRow = true;
            SelectRelayEndpoint(row.Endpoint, disconnectExisting: true);
            await JoinScannedServerAsync(row);
        }

        private async Task JoinScannedServerAsync(ScannedLobbyInfo row)
        {
            SelectRelayEndpoint(row.Endpoint, disconnectExisting: true);
            if (!row.HasLobby)
            {
                ShowStatus("No open lobby on " + row.Endpoint.Name + ".", isError: true);
                return;
            }

            _activeServerName = string.IsNullOrWhiteSpace(row.Lobby.ServerName) ? row.Endpoint.Name : row.Lobby.ServerName;
            await ConnectAndJoinAsync(row.Lobby.Code);
        }

        private void OnLeavePressed()
        {
            _session?.LeaveSession();
            if (GameManager.HasInstance)
                GameManager.Instance.SetSession(null, null);
            RefreshPanels();
        }

        private async void OnStartPressed()
        {
            if (_session == null || _session.State == SessionState.Offline)
                return;

            if (!_session.IsHost)
            {
                bool nextReady = !_session.LocalReady;
                if (_transport != null && _transport.IsConnected)
                    await _transport.SendReadyAsync(nextReady);
                else
                    _session.SetLocalReady(nextReady);

                RefreshPlayerList();
                RefreshStartButton();
                return;
            }

            if (_transport != null && _transport.IsConnected)
                await _transport.SendStartGameAsync();
            else
                _session.ApplyGameStarted();
        }

        private async Task ConnectAndCreateAsync()
        {
            EnsureSession();
            if (!await EnsureTransportConnectedAsync()) return;

            ShowStatus("Creating session...");
            await _transport.SendCreateSessionAsync(_activeServerName);
        }

        private async Task ConnectAndJoinAsync(string code)
        {
            EnsureSession();
            if (!await EnsureTransportConnectedAsync()) return;

            ShowStatus("Joining session...");
            await _transport.SendJoinSessionAsync(code);
        }

        private void SelectEndpointForAction()
        {
            string typedUrl = GetTypedServerAddress();
            if (!string.IsNullOrWhiteSpace(typedUrl))
            {
                SelectRelayEndpoint("Community Server", typedUrl, "", disconnectExisting: true);
                return;
            }

            if (_hasSelectedScannedRow)
                SelectRelayEndpoint(_selectedScannedRow.Endpoint, disconnectExisting: true);
        }

        private string GetTypedServerAddress()
        {
            string value = _customRelayUrlField == null ? "" : _customRelayUrlField.text;
            value = value?.Trim();
            return string.IsNullOrEmpty(value) ? "" : value;
        }

        private string ResolveHostServerName()
        {
            if (_hasSelectedScannedRow && !string.IsNullOrWhiteSpace(_selectedScannedRow.Endpoint.Name))
                return _selectedScannedRow.Endpoint.Name;

            if (!string.IsNullOrWhiteSpace(_selectedRelayName))
                return _selectedRelayName;

            return "Public Server";
        }

        private void SelectRelayEndpoint(RelayEndpoint endpoint, bool disconnectExisting)
        {
            SelectRelayEndpoint(endpoint.Name, endpoint.RelayUrls, endpoint.GameToken, disconnectExisting);
        }

        private void SelectRelayEndpoint(string displayName, string relayUrl, string gameToken, bool disconnectExisting)
        {
            SelectRelayEndpoint(displayName, new[] { relayUrl }, gameToken, disconnectExisting);
        }

        private void SelectRelayEndpoint(string displayName, string[] relayUrls, string gameToken, bool disconnectExisting)
        {
            string[] normalizedUrls = NormalizeRelayUrls(relayUrls);
            if (normalizedUrls.Length == 0)
                return;

            bool changed = !RelayUrlListsEqual(_selectedRelayUrls, normalizedUrls)
                || !string.Equals(_gameToken ?? "", gameToken ?? "", StringComparison.Ordinal);

            _selectedRelayName = string.IsNullOrWhiteSpace(displayName) ? "Server" : displayName.Trim();
            _relayUrl = normalizedUrls[0];
            _selectedRelayUrls.Clear();
            _selectedRelayUrls.AddRange(normalizedUrls);
            _gameToken = gameToken ?? "";

            if (disconnectExisting && changed)
            {
                _transport?.Disconnect();
                _transport = null;
            }

            RefreshSelectedRelayLabel();
        }

        private static string NormalizeRelayUrl(string relayUrl)
        {
            if (string.IsNullOrWhiteSpace(relayUrl))
                return "";

            string trimmed = relayUrl.Trim();
            if (trimmed.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                return trimmed;

            return "wss://" + trimmed;
        }

        private static string[] NormalizeRelayUrls(string[] relayUrls)
        {
            if (relayUrls == null || relayUrls.Length == 0)
                return Array.Empty<string>();

            var normalized = new List<string>();
            foreach (var relayUrl in relayUrls)
                AddRelayUrl(normalized, relayUrl);

            return normalized.ToArray();
        }

        private static void AddRelayUrl(List<string> urls, string relayUrl)
        {
            string normalized = NormalizeRelayUrl(relayUrl);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            foreach (var existing in urls)
            {
                if (string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            urls.Add(normalized);
        }

        private static bool RelayUrlListsEqual(List<string> existingUrls, string[] nextUrls)
        {
            if (existingUrls.Count != nextUrls.Length)
                return false;

            for (int i = 0; i < nextUrls.Length; i++)
            {
                if (!string.Equals(existingUrls[i], nextUrls[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void RefreshSelectedRelayLabel()
        {
            if (_selectedRelayLabel == null) return;

            _selectedRelayLabel.text = string.IsNullOrWhiteSpace(_relayUrl)
                ? "No server selected"
                : "Selected: " + _selectedRelayName;
        }

        private async Task RefreshPublicLobbiesAsync()
        {
            if (_session != null && _session.State != SessionState.Offline)
                return;

            ShowStatus("Scanning servers...");
            SetHostScanning(true);
            ClearPublicLobbyButtons();
            _scannedLobbyRows.Clear();
            _hasSelectedScannedRow = false;

            try
            {
                var endpoints = BuildScanEndpoints();
                int availableHosts = endpoints.Count;
                foreach (var endpoint in endpoints)
                {
                    var lobbies = await TryFetchPublicLobbiesAsync(endpoint);
                    if (lobbies == null || lobbies.Count == 0)
                    {
                        _scannedLobbyRows.Add(new ScannedLobbyInfo(endpoint, default, hasLobby: false));
                        continue;
                    }

                    foreach (var lobby in lobbies)
                        _scannedLobbyRows.Add(new ScannedLobbyInfo(endpoint, lobby, hasLobby: true));
                }

                RefreshScannedLobbyList(availableHosts);
            }
            finally
            {
                SetHostScanning(false);
            }
        }

        private void ShowConfiguredServerList()
        {
            ClearPublicLobbyButtons();
            _scannedLobbyRows.Clear();
            _hasSelectedScannedRow = false;

            var endpoints = BuildScanEndpoints();
            foreach (var endpoint in endpoints)
                _scannedLobbyRows.Add(new ScannedLobbyInfo(endpoint, default, hasLobby: false));

            RefreshScannedLobbyList(endpoints.Count);
        }

        private List<RelayEndpoint> BuildScanEndpoints()
        {
            var endpoints = new List<RelayEndpoint>();
            foreach (var endpoint in _officialServers)
                AddUniqueEndpoint(endpoints, endpoint);

            string typedUrl = _customRelayUrlField == null ? "" : _customRelayUrlField.text;
            if (!string.IsNullOrWhiteSpace(typedUrl))
                AddUniqueEndpoint(endpoints, new RelayEndpoint("Community Server", "", typedUrl, ""));

            return endpoints;
        }

        private static void AddUniqueEndpoint(List<RelayEndpoint> endpoints, RelayEndpoint endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint.RelayUrl)) return;
            foreach (var existing in endpoints)
            {
                if (string.Equals(existing.RelayUrl, endpoint.RelayUrl, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            endpoints.Add(endpoint);
        }

        private async Task<IReadOnlyList<PublicLobbyInfo>> TryFetchPublicLobbiesAsync(RelayEndpoint endpoint)
        {
            foreach (var relayUrl in endpoint.RelayUrls)
            {
                var result = await TryFetchPublicLobbiesAsync(relayUrl, endpoint.GameToken);
                if (result.Succeeded)
                    return result.Lobbies;
            }

            return null;
        }

        private async Task<LobbyFetchResult> TryFetchPublicLobbiesAsync(string relayUrl, string gameToken)
        {
            var transport = new WebSocketNetworkTransport(
                relayUrl,
                string.IsNullOrWhiteSpace(gameToken) ? null : gameToken);
            var completion = new TaskCompletionSource<IReadOnlyList<PublicLobbyInfo>>();
            bool relayError = false;

            transport.OnPublicLobbies += lobbies => completion.TrySetResult(lobbies);
            transport.OnError += _ =>
            {
                relayError = true;
                completion.TrySetResult(Array.Empty<PublicLobbyInfo>());
            };

            try
            {
                await transport.ConnectAsync();
                await transport.SendListPublicLobbiesAsync();

                float deadline = Time.realtimeSinceStartup + 2.5f;
                while (!completion.Task.IsCompleted && Time.realtimeSinceStartup < deadline)
                {
                    transport.PumpEvents();
                    await Task.Delay(50);
                }

                transport.PumpEvents();
                if (!completion.Task.IsCompleted || relayError)
                    return LobbyFetchResult.Failed;

                return new LobbyFetchResult(true, completion.Task.Result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MultiplayerLobbyUI] Server scan failed for " + relayUrl + ": " + ex.Message);
                return LobbyFetchResult.Failed;
            }
            finally
            {
                transport.Disconnect();
            }
        }

        private readonly struct LobbyFetchResult
        {
            public static readonly LobbyFetchResult Failed = new LobbyFetchResult(false, null);

            public readonly bool Succeeded;
            public readonly IReadOnlyList<PublicLobbyInfo> Lobbies;

            public LobbyFetchResult(bool succeeded, IReadOnlyList<PublicLobbyInfo> lobbies)
            {
                Succeeded = succeeded;
                Lobbies = lobbies ?? Array.Empty<PublicLobbyInfo>();
            }
        }

        private async Task<bool> EnsureTransportConnectedAsync()
        {
            if (_transport != null && _transport.IsConnected)
                return true;

            _isConnecting = true;
            try
            {
                _transport?.Disconnect();
                foreach (var relayUrl in GetSelectedRelayUrls())
                {
                    _transport = new WebSocketNetworkTransport(
                        relayUrl,
                        string.IsNullOrWhiteSpace(_gameToken) ? null : _gameToken);
                    WireTransport(_transport);
                    ShowStatus("Connecting to relay...");

                    try
                    {
                        await _transport.ConnectAsync();
                        _relayUrl = relayUrl;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[MultiplayerLobbyUI] Relay unavailable for " + relayUrl + ": " + ex.Message);
                        _transport?.Disconnect();
                        _transport = null;
                    }
                }

                ShowStatus("Relay unavailable.", isError: true);
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private IReadOnlyList<string> GetSelectedRelayUrls()
        {
            if (_selectedRelayUrls.Count > 0)
                return _selectedRelayUrls;

            string fallback = string.IsNullOrWhiteSpace(_relayUrl) ? DefaultRelayUrl : _relayUrl;
            return new[] { fallback };
        }

        private void EnsureSession()
        {
            if (_session != null) return;
            Bind(new MultiplayerSessionManager());
        }

        private void WireTransport(WebSocketNetworkTransport transport)
        {
            transport.OnSessionCreated += (code, playerId) =>
            {
                EnsureSession();
                _session.ApplyCreatedSession(code, playerId);
                if (GameManager.HasInstance)
                    GameManager.Instance.SetSession(_session, transport);
                ShowStatus("Public lobby created.");
                RefreshPanels();
            };

            transport.OnSessionJoined += (code, playerId, hostPlayerId) =>
            {
                EnsureSession();
                _session.ApplyJoinedSession(code, playerId, hostPlayerId);
                if (string.IsNullOrWhiteSpace(_activeServerName))
                    _activeServerName = "Public Server";
                if (GameManager.HasInstance)
                    GameManager.Instance.SetSession(_session, transport);
                ShowStatus(FormatPlayerCountStatus(_session.PlayerCount));
                RefreshPanels();
            };

            transport.OnRemotePlayerJoined += playerId =>
            {
                EnsureSession();
                _session.AddRemotePlayer(playerId, $"Player {playerId}");
                RefreshPanels();
            };

            transport.OnRemotePlayerLeft += (playerId, wasHost) =>
            {
                EnsureSession();
                _session.RemovePlayer(playerId);
                RefreshPanels();
            };

            transport.OnHostMigrated += newHostId =>
            {
                EnsureSession();
                _session.ApplyHostMigration(newHostId);
                RefreshPanels();
            };

            transport.OnPlayerReadyChanged += (playerId, ready) =>
            {
                EnsureSession();
                _session.ApplyPlayerReady(playerId, ready);
                RefreshPlayerList();
                RefreshStartButton();
            };

            transport.OnPlayerLobbyChanged += (playerId, lobbyId) =>
            {
                EnsureSession();
                _session.ApplyPlayerLobby(playerId, lobbyId);
            };

            transport.OnGameStarted += () =>
            {
                EnsureSession();
                _session.ApplyGameStarted();
            };

            transport.OnError += reason =>
            {
                ShowStatus(GetFriendlyRelayError(reason), isError: true);
                RefreshPanels();
            };
        }

        private static string GetFriendlyRelayError(string reason)
        {
            switch (reason)
            {
                case "not_found": return Localization.Get("multiplayer.invalid_code");
                case "full": return Localization.Get("multiplayer.session_full");
                case "already_in_session": return "Already in a session.";
                case "rate_limited": return "Too many attempts. Try again in a moment.";
                case "not_host": return "Only the host can start the game.";
                case "not_ready": return Localization.Get("multiplayer.waiting_ready");
                case "invalid_lobby": return "Could not switch multiplayer lobby.";
                default: return "Multiplayer error: " + reason;
            }
        }

        private void RefreshScannedLobbyList(int availableHosts)
        {
            if (_publicLobbyListText == null) return;
            ClearPublicLobbyButtons();

            if (availableHosts == 0)
            {
                _publicLobbyListText.text = "No hosts found.";
                return;
            }

            _publicLobbyListText.text = availableHosts == 1
                ? "1 host found"
                : availableHosts + " hosts found";

            for (int i = 0; i < _scannedLobbyRows.Count; i++)
            {
                var row = _scannedLobbyRows[i];
                string label;
                bool full = false;

                if (row.HasLobby)
                {
                    full = row.Lobby.MaxPlayers > 0 && row.Lobby.Players >= row.Lobby.MaxPlayers;
                    string capacity = row.Lobby.MaxPlayers > 0
                        ? row.Lobby.Players + "/" + row.Lobby.MaxPlayers + " players"
                        : row.Lobby.Players + " players";
                    label = row.Endpoint.Name + "    " + row.Lobby.ServerName + "    " + capacity
                        + (full ? "    Full" : "");
                }
                else
                {
                    label = row.Endpoint.Name + "    No open lobbies";
                }

                ScannedLobbyInfo selectedRow = row;

                var button = MakeButton(_publicLobbyContent, "PublicLobbyBtn" + i,
                    Vector2.zero, Vector2.one,
                    label, full ? UIStyleGuide.Colors.PanelBackground : UIStyleGuide.Colors.TideTeal,
                    _uiScale, () => OnPublicLobbyPressed(selectedRow),
                    UIStyleGuide.Typography.BodySmall);
                button.interactable = !full;
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 42f;
                layout.minHeight = 42f;
                _publicLobbyButtons.Add(button);
            }
        }

        private void ClearPublicLobbyButtons()
        {
            for (int i = 0; i < _publicLobbyButtons.Count; i++)
            {
                var button = _publicLobbyButtons[i];
                if (button != null)
                    Destroy(button.gameObject);
            }
            _publicLobbyButtons.Clear();
        }

        private void SetHostScanning(bool isScanning)
        {
            _isScanningHosts = isScanning;
            if (_loadingSpinner != null)
            {
                _loadingSpinner.gameObject.SetActive(isScanning);
                if (isScanning)
                {
                    _spinnerAngle = 0f;
                    _spinnerPulseTime = 0f;
                }
            }
        }

        private void UpdateLoadingSpinner()
        {
            if (!_isScanningHosts || _loadingSpinner == null) return;
            float dt = Time.unscaledDeltaTime;
            _spinnerPulseTime += dt;
            _spinnerAngle = (_spinnerAngle + 300f * dt) % 360f;

            int segmentCount = _spinnerSegments.Count;
            if (segmentCount > 0)
            {
                float head = _spinnerAngle / 360f * segmentCount;
                for (int i = 0; i < segmentCount; i++)
                {
                    float distance = Mathf.Repeat(head - i + segmentCount, segmentCount);
                    float t = 1f - Mathf.Clamp01(distance / 4f);
                    float alpha = Mathf.Lerp(0.2f, 1f, t);
                    _spinnerSegments[i].color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGold, alpha);
                }
            }

            if (_spinnerCore != null)
            {
                float pulse = 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(_spinnerPulseTime * 5.5f));
                _spinnerCore.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, pulse);
                float scale = 0.92f + 0.12f * (0.5f + 0.5f * Mathf.Sin(_spinnerPulseTime * 5.5f));
                _spinnerCore.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // ---- Session event handlers ----

        private void OnPlayerJoined(RemotePlayer p)
        {
            ShowStatus(FormatPlayerCountStatus(_session.PlayerCount));
            RefreshPlayerList();
        }

        private void OnPlayerLeft(int id, bool wasHost)
        {
            RefreshPlayerList();
        }

        private void OnHostMigrated(int newHostId)
        {
            ShowStatus(Localization.Get("multiplayer.host_migrated"), isError: false);
        }

        private void OnPlayerReadyChanged(int playerId, bool ready)
        {
            RefreshPlayerList();
            RefreshStartButton();
        }

        private void OnGameStarted()
        {
            OnStartGameRequested?.Invoke();
        }

        // ---- Panel state ----

        private void RefreshPanels()
        {
            bool offline  = _session == null || _session.State == SessionState.Offline;
            bool inSession = !offline;

            _offlinePanel.SetActive(offline);
            _sessionPanel.SetActive(inSession);

            if (inSession)
            {
                RefreshSessionCode();
                RefreshPlayerList();
                RefreshStartButton();
            }
        }

        private void RefreshSessionCode()
        {
            if (_sessionCodeLabel == null || _session == null) return;
            _sessionCodeLabel.text = string.IsNullOrWhiteSpace(_activeServerName) ? "Public Server" : _activeServerName;
            if (_sessionSummaryLabel != null)
            {
                _sessionSummaryLabel.text = _session.PlayerCount
                    + " players    " + (_session.IsHost ? "Host" : "Joined");
            }
        }

        private void RefreshPlayerList()
        {
            if (_playerListText == null || _session == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(FormatPlayerRow("You", _session.LocalPlayerId, _session.IsHost, _session.LocalReady));
            foreach (var rp in _session.RemotePlayers)
                sb.AppendLine(FormatPlayerRow(rp.DisplayName, rp.PlayerId, rp.PlayerId == _session.HostPlayerId, rp.IsReady));

            _playerListText.text = sb.ToString();
        }

        private static string FormatPlayerCountStatus(int playerCount)
        {
            return playerCount == 1 ? "1 player connected." : playerCount + " players connected.";
        }

        private string FormatPlayerRow(string name, int playerId, bool isHost, bool ready)
        {
            string role = isHost ? "Host" : (ready ? "Ready" : "Not ready");
            return name + "    Player " + playerId + "    " + role;
        }

        private void RefreshStartButton()
        {
            if (_startButton == null || _startButtonLabel == null || _session == null) return;

            if (_session.IsHost)
            {
                _startButtonLabel.text = Localization.Get("multiplayer.start_game");
                // Keep host start actionable so we can show an explicit "waiting for ready players" status
                // instead of presenting a dead disabled button.
                _startButton.interactable = _session.State != SessionState.Offline;
            }
            else
            {
                _startButtonLabel.text = _session.LocalReady
                    ? Localization.Get("multiplayer.not_ready")
                    : Localization.Get("multiplayer.ready");
                _startButton.interactable = true;
            }
        }

        private void ShowStatus(string msg, bool isError = false)
        {
            if (_statusText == null) return;
            _statusText.text    = msg;
            _statusText.color   = isError ? UIStyleGuide.Colors.Error : UIStyleGuide.Colors.FossilGold;
            _statusText.enabled = true;
            _statusHideAt = Time.unscaledTime + 4f;
        }

        // ---- UI construction ----

        private void BuildUI(float scale)
        {
            // Root backdrop
            var backdrop = MakePanel("Backdrop", transform,
                Vector2.zero, Vector2.one,
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.94f));
            UIStyleGuide.AddScreenAtmosphere(backdrop.transform, "Lobby", 1.08f);

            // Title
            MakeLabel(backdrop.transform, "Title",
                new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f),
                "Primeval Multiplayer",
                UIStyleGuide.Typography.TitleLarge, UIStyleGuide.Colors.FossilGold, TextAnchor.MiddleCenter, FontStyle.Bold, scale);

            // ---- Offline panel ----
            _offlinePanel = MakePanel("OfflinePanel", backdrop.transform,
                new Vector2(0.2f, 0.16f), new Vector2(0.8f, 0.86f), UIStyleGuide.Colors.PanelBackgroundStrong);
            UIStyleGuide.ApplyPaintedPanelChrome(_offlinePanel, strong: true, alpha: 0.97f);

            var customUrlGo = MakePanel("CustomRelayUrlField", _offlinePanel.transform,
                new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.89f), FieldBackground);
            _customRelayUrlField = MakeTextInput(customUrlGo, 128, "Server address", scale);

            _selectedRelayLabel = MakeLabel(_offlinePanel.transform, "SelectedRelayLabel",
                new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.77f),
                "", UIStyleGuide.Typography.Caption, UIStyleGuide.Colors.BodyTextMuted, TextAnchor.MiddleLeft, FontStyle.Normal, scale);

            MakeButton(_offlinePanel.transform, "HostBtn",
                new Vector2(0.06f, 0.58f), new Vector2(0.48f, 0.69f),
                "Host Server",
                UIStyleGuide.Colors.FossilAmber, scale, OnHostPressed);

            MakeButton(_offlinePanel.transform, "JoinServerBtn",
                new Vector2(0.52f, 0.58f), new Vector2(0.94f, 0.69f),
                "Join Server",
                UIStyleGuide.Colors.Amethyst, scale, OnJoinServerPressed);

            _publicLobbyListText = MakeLabel(_offlinePanel.transform, "PublicLobbyList",
                new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.55f),
                "Available hosts",
                UIStyleGuide.Typography.BodyMedium, UIStyleGuide.Colors.BodyText, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            _publicLobbyListPanel = MakePanel("PublicLobbyListPanel", _offlinePanel.transform,
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.46f), UIStyleGuide.Colors.RowStripe);
            UIStyleGuide.ApplyPaintedPanelChrome(_publicLobbyListPanel, strong: false, alpha: 0.82f, atmosphere: false);
            var lobbyScroll = _publicLobbyListPanel.AddComponent<ScrollRect>();
            lobbyScroll.horizontal = false;
            lobbyScroll.movementType = ScrollRect.MovementType.Clamped;
            var lobbyMask = _publicLobbyListPanel.AddComponent<Mask>();
            lobbyMask.showMaskGraphic = false;
            lobbyScroll.viewport = _publicLobbyListPanel.GetComponent<RectTransform>();

            var contentGo = new GameObject("PublicLobbyContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(_publicLobbyListPanel.transform, false);
            _publicLobbyContent = contentGo.GetComponent<RectTransform>();
            _publicLobbyContent.anchorMin = new Vector2(0f, 1f);
            _publicLobbyContent.anchorMax = new Vector2(1f, 1f);
            _publicLobbyContent.pivot = new Vector2(0.5f, 1f);
            _publicLobbyContent.offsetMin = Vector2.zero;
            _publicLobbyContent.offsetMax = Vector2.zero;
            var lobbyLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            lobbyLayout.padding = new RectOffset(10, 10, 10, 10);
            lobbyLayout.spacing = 8f;
            lobbyLayout.childControlWidth = true;
            lobbyLayout.childControlHeight = true;
            lobbyLayout.childForceExpandWidth = true;
            lobbyLayout.childForceExpandHeight = false;
            var lobbySize = contentGo.GetComponent<ContentSizeFitter>();
            lobbySize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            lobbyScroll.content = _publicLobbyContent;

            _loadingSpinner = MakeLoadingSpinner(_publicLobbyListPanel.transform, scale);
            _loadingSpinner.gameObject.SetActive(false);

            // ---- Session panel ----
            _sessionPanel = MakePanel("SessionPanel", backdrop.transform,
                new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.84f), UIStyleGuide.Colors.PanelBackgroundStrong);
            UIStyleGuide.ApplyPaintedPanelChrome(_sessionPanel, strong: true, alpha: 0.97f);
            _sessionPanel.SetActive(false);

            _sessionCodeLabel = MakeLabel(_sessionPanel.transform, "CodeLabel",
                new Vector2(0.08f, 0.84f), new Vector2(0.62f, 0.96f),
                "", UIStyleGuide.Typography.TitleMedium, UIStyleGuide.Colors.FossilGold, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            _sessionSummaryLabel = MakeLabel(_sessionPanel.transform, "SessionSummary",
                new Vector2(0.62f, 0.84f), new Vector2(0.92f, 0.96f),
                "", UIStyleGuide.Typography.BodySmall, UIStyleGuide.Colors.TideTealBright, TextAnchor.MiddleRight, FontStyle.Bold, scale);

            var rosterPanel = MakePanel("RosterPanel", _sessionPanel.transform,
                new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.8f), UIStyleGuide.Colors.RowStripe);
            UIStyleGuide.ApplyPaintedPanelChrome(rosterPanel, strong: false, alpha: 0.8f, atmosphere: false);

            MakeLabel(_sessionPanel.transform, "RosterHeader",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.84f),
                "Players", UIStyleGuide.Typography.BodySmall, UIStyleGuide.Colors.Subtitle, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            _playerListText = MakeLabel(rosterPanel.transform, "PlayerList",
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                "", UIStyleGuide.Typography.BodySmall, UIStyleGuide.Colors.BodyText, TextAnchor.UpperLeft, FontStyle.Normal, scale);

            MakeButton(_sessionPanel.transform, "LeaveBtn",
                new Vector2(0.08f, 0.07f), new Vector2(0.42f, 0.17f),
                Localization.Get("multiplayer.leave_button"),
                UIStyleGuide.Colors.Coral, scale, OnLeavePressed);

            _startButton = MakeButton(_sessionPanel.transform, "StartBtn",
                new Vector2(0.48f, 0.07f), new Vector2(0.92f, 0.17f),
                Localization.Get("multiplayer.start_game"),
                UIStyleGuide.Colors.FossilAmber, scale, OnStartPressed);
            _startButtonLabel = _startButton.GetComponentInChildren<Text>();

            // ---- Status bar ----
            _statusText = MakeLabel(backdrop.transform, "Status",
                new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.11f),
                "", UIStyleGuide.Typography.BodyMedium, UIStyleGuide.Colors.FossilGold, TextAnchor.MiddleCenter, FontStyle.Italic, scale);
            _statusText.enabled = false;

            // ---- Close button (X) ----
            var closeButton = MakeButton(backdrop.transform, "CloseBtn",
                new Vector2(0.9f, 0.92f), new Vector2(0.98f, 0.99f),
                "X", UIStyleGuide.Colors.Coral, scale, OnClosePressed);
            closeButton.transform.SetAsLastSibling();
        }

        // ---- UI helpers ----

        private static GameObject MakePanel(string name, Transform parent,
                                             Vector2 anchorMin, Vector2 anchorMax,
                                             Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Text MakeLabel(Transform parent, string name,
                                       Vector2 anchorMin, Vector2 anchorMax,
                                       string text, int fontSize, Color color,
                                       TextAnchor anchor, FontStyle style, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            t.text = text; t.fontSize = Mathf.RoundToInt(fontSize * scale);
            t.color = color; t.alignment = anchor; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        private static InputField MakeTextInput(GameObject fieldGo, int characterLimit, string placeholder, float scale)
        {
            var fieldImage = fieldGo.GetComponent<Image>();
            if (fieldImage != null)
            {
                fieldImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
                fieldImage.type = Image.Type.Sliced;
                fieldImage.color = FieldBackground;
            }
            AddOutline(fieldGo, FieldBorder, new Vector2(1f, -1f));

            var input = fieldGo.AddComponent<InputField>();
            input.characterLimit = characterLimit;
            input.contentType = placeholder == "TAG"
                ? InputField.ContentType.Alphanumeric
                : InputField.ContentType.Standard;
            input.caretColor = UIStyleGuide.Colors.FossilGold;
            input.selectionColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.42f);
            input.textComponent = MakeInputText(fieldGo.transform, "FieldText", "", scale);
            input.placeholder = MakeInputText(fieldGo.transform, "Placeholder", placeholder, scale);
            input.placeholder.color = UIStyleGuide.Colors.BodyTextSubtle;

            var colors = UIStyleGuide.BuildButtonColors(UIButtonVariant.Secondary);
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
            input.colors = colors;
            return input;
        }

        private static Text MakeInputText(Transform parent, string name, string text, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 7f);
            rt.offsetMax = new Vector2(-18f, -7f);
            var t = go.GetComponent<Text>();
            t.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            t.text = text;
            t.color = UIStyleGuide.Colors.BodyText;
            t.fontSize = Mathf.RoundToInt(UIStyleGuide.Typography.BodyMedium * scale);
            t.alignment = TextAnchor.MiddleLeft;
            t.fontStyle = FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        private RectTransform MakeLoadingSpinner(Transform parent, float scale)
        {
            _spinnerSegments.Clear();
            _spinnerCore = null;

            var root = new GameObject("HostLoadingSpinner", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(54f, 54f) * scale;
            rootRt.anchoredPosition = Vector2.zero;

            const int segmentCount = 12;
            float radius = 17f * scale;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * (360f / segmentCount);
                float radians = angle * Mathf.Deg2Rad;
                var segment = new GameObject("Segment" + i, typeof(RectTransform), typeof(Image));
                segment.transform.SetParent(root.transform, false);

                var segmentRt = segment.GetComponent<RectTransform>();
                segmentRt.anchorMin = segmentRt.anchorMax = new Vector2(0.5f, 0.5f);
                segmentRt.sizeDelta = new Vector2(4f, 10f) * scale;
                segmentRt.anchoredPosition = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                segmentRt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

                var segmentImage = segment.GetComponent<Image>();
                segmentImage.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGold, 0.2f);
                _spinnerSegments.Add(segmentImage);
            }

            var core = new GameObject("Core", typeof(RectTransform), typeof(Image));
            core.transform.SetParent(root.transform, false);
            var coreRt = core.GetComponent<RectTransform>();
            coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(12f, 12f) * scale;
            coreRt.anchoredPosition = Vector2.zero;
            _spinnerCore = core.GetComponent<Image>();
            _spinnerCore.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.7f);

            return rootRt;
        }

        private static Button MakeButton(Transform parent, string name,
                                        Vector2 anchorMin, Vector2 anchorMax,
                                        string label, Color bgColor, float scale,
                                        UnityEngine.Events.UnityAction onClick,
                                        int fontSize = UIStyleGuide.Typography.BodyLarge)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = bgColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var variant = ResolveButtonVariant(bgColor);
            UIStyleGuide.ApplyPaintedButtonChrome(button, variant);
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, UIStyleGuide.GetButtonAccentColor(variant), 0.4f);
            colors.pressedColor = Color.Lerp(bgColor, UIStyleGuide.Colors.DeepNavy, 0.42f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.LockedGray, 0.78f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            image.color = bgColor;
            bool hasPearl = fontSize >= UIStyleGuide.Typography.BodyMedium && name != "CloseBtn";
            if (hasPearl)
                UIStyleGuide.AddButtonPearl(go, variant, variant == UIButtonVariant.Primary ? 30f : 22f);

            var labelText = MakeLabel(go.transform, "Label",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f),
                label, fontSize, UIStyleGuide.GetButtonTextColor(variant), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            if (hasPearl)
                labelText.rectTransform.offsetMin = new Vector2(22f, labelText.rectTransform.offsetMin.y);
            UIStyleGuide.ApplyTextShadow(labelText.gameObject, variant == UIButtonVariant.Primary ? 0.18f : 0.34f);
            return button;
        }

        private static UIButtonVariant ResolveButtonVariant(Color color)
        {
            if (Approximately(color, UIStyleGuide.Colors.FossilAmber) || Approximately(color, UIStyleGuide.Colors.FossilGold))
                return UIButtonVariant.Primary;
            if (Approximately(color, UIStyleGuide.Colors.Coral) || Approximately(color, UIStyleGuide.Colors.CoralBright))
                return UIButtonVariant.Danger;
            return UIButtonVariant.Secondary;
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f
                && Mathf.Abs(a.g - b.g) < 0.002f
                && Mathf.Abs(a.b - b.b) < 0.002f;
        }
    }
}
