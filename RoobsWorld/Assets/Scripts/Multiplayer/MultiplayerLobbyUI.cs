using UnityEngine;
using UnityEngine.UI;
using RoobsWorld.Core;
using System;
using System.Threading.Tasks;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// In-game overlay for hosting or joining a multiplayer session.
    ///
    /// Layout (built entirely in code, no prefab required):
    ///   - Root: full-screen semi-transparent panel, visible only when open.
    ///   - Offline panel: "Host Session" / "Join Session" buttons.
    ///   - Join sub-panel: code entry field + confirm button (slides in on Join press).
    ///   - Session panel: shows session code, connected players, leave button.
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
        private const string DefaultRelayUrl = "ws://localhost:7788";

        [SerializeField] private string _relayUrl = DefaultRelayUrl;
        [SerializeField] private string _gameToken = "";

        private MultiplayerSessionManager _session;
        private WebSocketNetworkTransport _transport;

        // ---- Panels ----
        private GameObject _offlinePanel;
        private GameObject _joinSubPanel;
        private GameObject _sessionPanel;

        // ---- Controls ----
        private InputField _codeEntryField;
        private InputField _clanNameField;
        private InputField _clanTagField;
        private Text       _sessionCodeLabel;
        private Text       _clanStatusLabel;
        private Text       _playerListText;
        private Text       _statusText;
        private Button     _startButton;
        private Text       _startButtonLabel;
        private float      _statusHideAt;
        private bool       _isConnecting;

        private bool _isOpen;

        public Action OnStartGameRequested { get; set; }

        // ---- Setup ----

        private void Awake()
        {
            ApplyRelayConfigFromResources();
            ApplyRelayConfigFromCommandLine();

            var canvas        = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            float scale = Screen.height / 1080f;
            BuildUI(scale);
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
                _session.OnClanCreated   -= OnClanCreated;
                _session.OnPlayerReadyChanged -= OnPlayerReadyChanged;
                _session.OnGameStarted -= OnGameStarted;
            }

            _session = session;

            if (_session != null)
            {
                _session.OnPlayerJoined  += OnPlayerJoined;
                _session.OnPlayerLeft    += OnPlayerLeft;
                _session.OnHostMigrated  += OnHostMigrated;
                _session.OnClanCreated   += OnClanCreated;
                _session.OnPlayerReadyChanged += OnPlayerReadyChanged;
                _session.OnGameStarted += OnGameStarted;
            }
        }

        public void ConfigureRelay(string relayUrl, string gameToken = null)
        {
            if (!string.IsNullOrWhiteSpace(relayUrl))
                _relayUrl = relayUrl.Trim();
            _gameToken = gameToken ?? "";
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
                    _relayUrl = args[i + 1];
                else if (args[i] == "-gameToken")
                    _gameToken = args[i + 1];
            }
        }

        private void ApplyRelayConfig(RelayConfig config)
        {
            if (config == null) return;
            if (!string.IsNullOrWhiteSpace(config.relayUrl))
                _relayUrl = config.relayUrl.Trim();
            _gameToken = config.gameToken ?? _gameToken;
        }

        [Serializable]
        private sealed class RelayConfig
        {
            public string relayUrl;
            public string gameToken;
        }

        // ---- Public API ----

        public void Open()
        {
            gameObject.SetActive(true);
            _isOpen = true;
            RefreshPanels();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _isOpen = false;
        }

        // ---- Update ----

        private void Update()
        {
            if (!_isOpen) return;
            _transport?.PumpEvents();
            if (_statusText != null && _statusText.enabled && Time.unscaledTime >= _statusHideAt)
                _statusText.enabled = false;
        }

        // ---- Button handlers ----

        private async void OnHostPressed()
        {
            if (_isConnecting) return;
            await ConnectAndCreateAsync();
        }

        private void OnJoinTogglePressed()
        {
            bool showing = _joinSubPanel.activeSelf;
            _joinSubPanel.SetActive(!showing);
        }

        private async void OnJoinConfirmPressed()
        {
            if (_codeEntryField == null) return;
            string code = _codeEntryField.text.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(code) || code.Length != MultiplayerSessionManager.SessionCodeLength)
            {
                ShowStatus(Localization.Get("multiplayer.invalid_code"), isError: true);
                return;
            }

            if (_isConnecting) return;
            await ConnectAndJoinAsync(code);
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

            if (!_session.CanStartGame)
            {
                ShowStatus(Localization.Get("multiplayer.waiting_ready"), isError: true);
                return;
            }

            if (_transport != null && _transport.IsConnected)
                await _transport.SendStartGameAsync();
            else
                _session.ApplyGameStarted();
        }

        private async void OnCreateClanPressed()
        {
            if (_session == null || _session.State == SessionState.Offline)
                return;

            string clanName = _clanNameField == null ? "" : _clanNameField.text;
            string clanTag = _clanTagField == null ? "" : _clanTagField.text;

            if (_session.LocalClan != null)
            {
                ShowStatus(Localization.Get("multiplayer.clan_already_in"), isError: true);
                return;
            }

            if (_transport != null && _transport.IsConnected)
            {
                await _transport.SendCreateClanAsync(clanName, clanTag);
                return;
            }

            if (!_session.CreateClan(clanName, clanTag))
                ShowStatus(Localization.Get("multiplayer.clan_invalid"), isError: true);
            RefreshClanStatus();
            RefreshPlayerList();
        }

        private async Task ConnectAndCreateAsync()
        {
            EnsureSession();
            if (!await EnsureTransportConnectedAsync()) return;

            ShowStatus("Creating session...");
            await _transport.SendCreateSessionAsync();
        }

        private async Task ConnectAndJoinAsync(string code)
        {
            EnsureSession();
            if (!await EnsureTransportConnectedAsync()) return;

            ShowStatus("Joining session...");
            await _transport.SendJoinSessionAsync(code);
        }

        private async Task<bool> EnsureTransportConnectedAsync()
        {
            if (_transport != null && _transport.IsConnected)
                return true;

            _isConnecting = true;
            try
            {
                _transport?.Disconnect();
                _transport = new WebSocketNetworkTransport(
                    string.IsNullOrWhiteSpace(_relayUrl) ? DefaultRelayUrl : _relayUrl,
                    string.IsNullOrWhiteSpace(_gameToken) ? null : _gameToken);
                WireTransport(_transport);
                ShowStatus("Connecting to relay...");
                await _transport.ConnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                ShowStatus("Relay unavailable: " + ex.Message, isError: true);
                _transport?.Disconnect();
                _transport = null;
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
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
                ShowStatus(Localization.Get("multiplayer.session_code_label") + " " + code);
                RefreshPanels();
            };

            transport.OnSessionJoined += (code, playerId, hostPlayerId) =>
            {
                EnsureSession();
                _session.ApplyJoinedSession(code, playerId, hostPlayerId);
                if (GameManager.HasInstance)
                    GameManager.Instance.SetSession(_session, transport);
                ShowStatus(string.Format(Localization.Get("multiplayer.players_connected"),
                    _session.PlayerCount, MultiplayerSessionManager.MaxPlayers));
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

            transport.OnClanCreated += (clanId, name, tag, leaderPlayerId) =>
            {
                EnsureSession();
                if (!_session.ApplyClanCreated(clanId, name, tag, leaderPlayerId))
                    ShowStatus(Localization.Get("multiplayer.clan_invalid"), isError: true);
                RefreshClanStatus();
                RefreshPlayerList();
            };

            transport.OnPlayerReadyChanged += (playerId, ready) =>
            {
                EnsureSession();
                _session.ApplyPlayerReady(playerId, ready);
                RefreshPlayerList();
                RefreshStartButton();
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
                case "already_in_clan": return Localization.Get("multiplayer.clan_already_in");
                case "invalid_clan": return Localization.Get("multiplayer.clan_invalid");
                case "rate_limited": return "Too many attempts. Try again in a moment.";
                case "not_host": return "Only the host can start the game.";
                case "not_ready": return Localization.Get("multiplayer.waiting_ready");
                default: return "Multiplayer error: " + reason;
            }
        }

        // ---- Session event handlers ----

        private void OnPlayerJoined(RemotePlayer p)
        {
            ShowStatus(string.Format(Localization.Get("multiplayer.players_connected"),
                _session.PlayerCount, MultiplayerSessionManager.MaxPlayers));
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

        private void OnClanCreated(MultiplayerClan clan)
        {
            ShowStatus(string.Format(Localization.Get("multiplayer.clan_created"), clan.Tag));
            RefreshClanStatus();
            RefreshPlayerList();
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
            _joinSubPanel.SetActive(false); // reset join field whenever state changes
            _sessionPanel.SetActive(inSession);

            if (inSession)
            {
                RefreshSessionCode();
                RefreshClanStatus();
                RefreshPlayerList();
                RefreshStartButton();
            }
        }

        private void RefreshSessionCode()
        {
            if (_sessionCodeLabel == null || _session == null) return;
            _sessionCodeLabel.text = Localization.Get("multiplayer.session_code_label")
                + " " + (_session.SessionCode ?? "—");
        }

        private void RefreshPlayerList()
        {
            if (_playerListText == null || _session == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("You (Player " + _session.LocalPlayerId + ")" +
                          (_session.IsHost ? " [Host]" : "") +
                          (_session.LocalReady ? " [Ready]" : " [Not Ready]") +
                          GetClanSuffix(_session.LocalPlayerId));
            foreach (var rp in _session.RemotePlayers)
                sb.AppendLine("Player " + rp.PlayerId + " — " + rp.DisplayName +
                              (rp.PlayerId == _session.HostPlayerId ? " [Host]" : "") +
                              (rp.IsReady ? " [Ready]" : " [Not Ready]") +
                              GetClanSuffix(rp.PlayerId));
            _playerListText.text = sb.ToString();
        }

        private void RefreshStartButton()
        {
            if (_startButton == null || _startButtonLabel == null || _session == null) return;

            if (_session.IsHost)
            {
                _startButtonLabel.text = Localization.Get("multiplayer.start_game");
                _startButton.interactable = _session.CanStartGame;
            }
            else
            {
                _startButtonLabel.text = _session.LocalReady
                    ? Localization.Get("multiplayer.not_ready")
                    : Localization.Get("multiplayer.ready");
                _startButton.interactable = true;
            }
        }

        private void RefreshClanStatus()
        {
            if (_clanStatusLabel == null || _session == null) return;
            var clan = _session.LocalClan;
            _clanStatusLabel.text = clan == null
                ? Localization.Get("multiplayer.clan_none")
                : string.Format(Localization.Get("multiplayer.clan_current"), clan.Tag, clan.Name);
        }

        private string GetClanSuffix(int playerId)
        {
            if (_session == null) return "";
            foreach (var clan in _session.Clans)
            {
                if (clan.HasMember(playerId))
                    return " [" + clan.Tag + "]";
            }
            return "";
        }

        private void ShowStatus(string msg, bool isError = false)
        {
            if (_statusText == null) return;
            _statusText.text    = msg;
            _statusText.color   = isError ? new Color(1f, 0.3f, 0.3f) : new Color(0.5f, 1f, 0.6f);
            _statusText.enabled = true;
            _statusHideAt = Time.unscaledTime + 4f;
        }

        // ---- UI construction ----

        private void BuildUI(float scale)
        {
            // Root backdrop
            var backdrop = MakePanel("Backdrop", transform,
                Vector2.zero, Vector2.one,
                new Color(0.05f, 0.05f, 0.12f, 0.92f));

            // Title
            MakeLabel(backdrop.transform, "Title",
                new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f),
                Localization.Get("multiplayer.host_button").Replace("Host ", "") + " Multiplayer",
                26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);

            // ---- Offline panel ----
            _offlinePanel = MakePanel("OfflinePanel", backdrop.transform,
                new Vector2(0.25f, 0.55f), new Vector2(0.75f, 0.84f), Color.clear);

            MakeButton(_offlinePanel.transform, "HostBtn",
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                Localization.Get("multiplayer.host_button"),
                new Color(0.15f, 0.45f, 0.7f), scale, OnHostPressed);

            MakeButton(_offlinePanel.transform, "JoinBtn",
                new Vector2(0f, 0f), new Vector2(1f, 0.48f),
                Localization.Get("multiplayer.join_button"),
                new Color(0.1f, 0.35f, 0.55f), scale, OnJoinTogglePressed);

            // ---- Join sub-panel ----
            _joinSubPanel = MakePanel("JoinPanel", backdrop.transform,
                new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.54f), new Color(0.1f, 0.1f, 0.2f, 0.9f));
            _joinSubPanel.SetActive(false);

            MakeLabel(_joinSubPanel.transform, "EnterLabel",
                new Vector2(0.02f, 0.55f), new Vector2(0.45f, 0.98f),
                Localization.Get("multiplayer.enter_code"),
                14, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal, scale);

            var fieldGo = MakePanel("CodeField", _joinSubPanel.transform,
                new Vector2(0.48f, 0.52f), new Vector2(0.82f, 0.98f), Color.white);
            _codeEntryField = fieldGo.AddComponent<InputField>();
            _codeEntryField.characterLimit = MultiplayerSessionManager.SessionCodeLength;
            _codeEntryField.contentType    = InputField.ContentType.Alphanumeric;
            _codeEntryField.textComponent = MakeInputText(fieldGo.transform, "FieldText", "", scale);

            MakeButton(_joinSubPanel.transform, "ConfirmBtn",
                new Vector2(0.84f, 0.52f), new Vector2(1f, 0.98f),
                "Go", new Color(0.2f, 0.6f, 0.3f), scale, OnJoinConfirmPressed);

            // ---- Session panel ----
            _sessionPanel = MakePanel("SessionPanel", backdrop.transform,
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.84f), Color.clear);
            _sessionPanel.SetActive(false);

            _sessionCodeLabel = MakeLabel(_sessionPanel.transform, "CodeLabel",
                new Vector2(0f, 0.82f), new Vector2(1f, 1f),
                "", 18, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);

            _playerListText = MakeLabel(_sessionPanel.transform, "PlayerList",
                new Vector2(0f, 0.42f), new Vector2(1f, 0.8f),
                "", 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, scale);

            _clanStatusLabel = MakeLabel(_sessionPanel.transform, "ClanStatus",
                new Vector2(0f, 0.32f), new Vector2(1f, 0.4f),
                "", 14, new Color(0.85f, 1f, 0.7f), TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            var clanNameGo = MakePanel("ClanNameField", _sessionPanel.transform,
                new Vector2(0f, 0.2f), new Vector2(0.48f, 0.3f), Color.white);
            _clanNameField = MakeTextInput(clanNameGo, MultiplayerSessionManager.ClanNameMaxLength, "Clan name", scale);

            var clanTagGo = MakePanel("ClanTagField", _sessionPanel.transform,
                new Vector2(0.5f, 0.2f), new Vector2(0.68f, 0.3f), Color.white);
            _clanTagField = MakeTextInput(clanTagGo, MultiplayerSessionManager.ClanTagMaxLength, "TAG", scale);

            MakeButton(_sessionPanel.transform, "CreateClanBtn",
                new Vector2(0.7f, 0.2f), new Vector2(1f, 0.3f),
                Localization.Get("multiplayer.clan_create"),
                new Color(0.3f, 0.38f, 0.18f), scale, OnCreateClanPressed);

            MakeButton(_sessionPanel.transform, "LeaveBtn",
                new Vector2(0.08f, 0f), new Vector2(0.48f, 0.18f),
                Localization.Get("multiplayer.leave_button"),
                new Color(0.55f, 0.12f, 0.12f), scale, OnLeavePressed);

            _startButton = MakeButton(_sessionPanel.transform, "StartBtn",
                new Vector2(0.52f, 0f), new Vector2(0.92f, 0.18f),
                Localization.Get("multiplayer.start_game"),
                new Color(0.15f, 0.55f, 0.28f), scale, OnStartPressed);
            _startButtonLabel = _startButton.GetComponentInChildren<Text>();

            // ---- Status bar ----
            _statusText = MakeLabel(backdrop.transform, "Status",
                new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.11f),
                "", 14, Color.green, TextAnchor.MiddleCenter, FontStyle.Italic, scale);
            _statusText.enabled = false;

            // ---- Close button (X) ----
            MakeButton(backdrop.transform, "CloseBtn",
                new Vector2(0.9f, 0.92f), new Vector2(0.98f, 0.99f),
                "✕", new Color(0.4f, 0.1f, 0.1f), scale, Close);
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
            return t;
        }

        private static InputField MakeTextInput(GameObject fieldGo, int characterLimit, string placeholder, float scale)
        {
            var input = fieldGo.AddComponent<InputField>();
            input.characterLimit = characterLimit;
            input.contentType = placeholder == "TAG"
                ? InputField.ContentType.Alphanumeric
                : InputField.ContentType.Standard;
            input.textComponent = MakeInputText(fieldGo.transform, "FieldText", "", scale);
            input.placeholder = MakeInputText(fieldGo.transform, "Placeholder", placeholder, scale);
            input.placeholder.color = new Color(0.35f, 0.35f, 0.35f);
            return input;
        }

        private static Text MakeInputText(Transform parent, string name, string text, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4, 0); rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            t.text = text;
            t.color = Color.black;
            t.fontSize = Mathf.RoundToInt(16 * scale);
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private static Button MakeButton(Transform parent, string name,
                                        Vector2 anchorMin, Vector2 anchorMax,
                                        string label, Color bgColor, float scale,
                                        UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = bgColor;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            MakeLabel(go.transform, "Label",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f),
                label, 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            return button;
        }
    }
}
