using System;
using System.Text;
using System.Threading.Tasks;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoobsWorld.Multiplayer
{
    /// <summary>Small gameplay HUD for the relay-backed global session chat.</summary>
    public sealed class GlobalChatOverlay : MonoBehaviour
    {
        private static readonly Color PanelColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.PanelBackgroundStrong, 0.9f);
        private static readonly Color FieldBackground = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.PanelBackground, 0.96f);
        private static readonly Color FieldBorder = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AccentStrong, 0.35f);

        private MultiplayerSessionManager _session;
        private WebSocketNetworkTransport _transport;
        private GameObject _root;
        private Text _chatLogText;
        private Text _statusText;
        private InputField _inputField;
        private float _statusHideAt;
        private bool _isSending;

        public void Activate(MultiplayerSessionManager session, WebSocketNetworkTransport transport)
        {
            Deactivate();

            if (session == null || session.State == SessionState.Offline || transport == null)
                return;

            _session = session;
            _transport = transport;
            BuildUI();
            _session.OnChatMessageReceived += OnChatMessageReceived;
            _transport.OnError += OnTransportError;
            RefreshChatLog();
        }

        public void Deactivate()
        {
            if (_session != null)
                _session.OnChatMessageReceived -= OnChatMessageReceived;
            if (_transport != null)
                _transport.OnError -= OnTransportError;

            _session = null;
            _transport = null;
            _inputField = null;
            _chatLogText = null;
            _statusText = null;
            _isSending = false;

            if (_root != null)
                Destroy(_root);
            _root = null;
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        private void Update()
        {
            if (_session == null || _transport == null)
                return;

            _transport.PumpEvents();

            if (_statusText != null && _statusText.enabled && Time.unscaledTime >= _statusHideAt)
                _statusText.enabled = false;

            var keyboard = Keyboard.current;
            if (keyboard == null || _inputField == null)
                return;

            bool enterPressed = keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (enterPressed)
            {
                if (IsInputFocused())
                    _ = SendCurrentMessageAsync();
                else
                    FocusInput();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame && IsInputFocused() && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private bool IsInputFocused()
        {
            return EventSystem.current != null
                && _inputField != null
                && EventSystem.current.currentSelectedGameObject == _inputField.gameObject;
        }

        private void FocusInput()
        {
            if (EventSystem.current == null || _inputField == null)
                return;

            EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
            _inputField.ActivateInputField();
        }

        private async void OnSendPressed()
        {
            await SendCurrentMessageAsync();
        }

        private async Task SendCurrentMessageAsync()
        {
            if (_isSending || _inputField == null)
                return;

            if (!MultiplayerSessionManager.TryNormalizeChatMessage(_inputField.text, out string message))
            {
                ShowStatus("Message is empty or too long.", true);
                return;
            }

            if (_transport == null || !_transport.IsConnected)
            {
                ShowStatus("Chat is unavailable.", true);
                return;
            }

            _isSending = true;
            _inputField.text = "";
            try
            {
                await _transport.SendGlobalChatMessageAsync(message);
                FocusInput();
            }
            catch (Exception ex)
            {
                _inputField.text = message;
                ShowStatus("Chat is unavailable.", true);
                Debug.LogWarning("[GlobalChatOverlay] Failed to send chat: " + ex.Message);
            }
            finally
            {
                _isSending = false;
            }
        }

        private void OnChatMessageReceived(MultiplayerChatMessage message)
        {
            RefreshChatLog();
        }

        private void OnTransportError(string reason)
        {
            switch (reason)
            {
                case "invalid_chat":
                    ShowStatus("Message was not sent.", true);
                    break;
                case "chat_rate_limited":
                    ShowStatus("Slow down before sending another message.", true);
                    break;
                case "not_in_session":
                    ShowStatus("Chat is unavailable.", true);
                    break;
            }
        }

        private void RefreshChatLog()
        {
            if (_chatLogText == null)
                return;

            var messages = _session?.ChatMessages;
            if (messages == null || messages.Count == 0)
            {
                _chatLogText.text = "No messages yet.";
                return;
            }

            int start = Mathf.Max(0, messages.Count - 5);
            var sb = new StringBuilder();
            for (int i = start; i < messages.Count; i++)
            {
                var message = messages[i];
                if (message == null)
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(message.SenderName).Append(": ").Append(message.Message);
            }
            _chatLogText.text = sb.ToString();
        }

        private void ShowStatus(string message, bool isError)
        {
            if (_statusText == null)
                return;

            _statusText.text = message;
            _statusText.color = isError ? UIStyleGuide.Colors.Error : UIStyleGuide.Colors.FossilGold;
            _statusText.enabled = true;
            _statusHideAt = Time.unscaledTime + 3f;
        }

        private void BuildUI()
        {
            _root = new GameObject("GlobalChatOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 310;
            canvas.pixelPerfect = true;
            UIStyleGuide.ApplyCanvasScalerDefaults(_root.GetComponent<CanvasScaler>());

            var panel = MakePanel("GlobalChatPanel", _root.transform, new Vector2(0.018f, 0.035f), new Vector2(0.39f, 0.245f), PanelColor);
            UIStyleGuide.ApplyPaintedPanelChrome(panel, strong: false, alpha: 0.9f, atmosphere: false);

            MakeLabel(panel.transform, "Header", new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f),
                "Global Chat", UIStyleGuide.Typography.BodySmall, UIStyleGuide.Colors.Subtitle, TextAnchor.MiddleLeft, FontStyle.Bold);

            _chatLogText = MakeLabel(panel.transform, "ChatLog", new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.81f),
                "", UIStyleGuide.Typography.BodySmall, UIStyleGuide.Colors.BodyText, TextAnchor.UpperLeft, FontStyle.Normal);
            _chatLogText.verticalOverflow = VerticalWrapMode.Truncate;

            var inputGo = MakePanel("ChatInputField", panel.transform, new Vector2(0.04f, 0.1f), new Vector2(0.73f, 0.31f), FieldBackground);
            _inputField = MakeTextInput(inputGo, MultiplayerSessionManager.ChatMessageMaxLength, "Message");
            _inputField.lineType = InputField.LineType.SingleLine;

            MakeButton(panel.transform, "SendButton", new Vector2(0.76f, 0.1f), new Vector2(0.96f, 0.31f),
                "Send", UIStyleGuide.Colors.Amethyst, OnSendPressed);

            _statusText = MakeLabel(panel.transform, "Status", new Vector2(0.04f, 0.005f), new Vector2(0.96f, 0.09f),
                "", UIStyleGuide.Typography.Caption, UIStyleGuide.Colors.FossilGold, TextAnchor.MiddleLeft, FontStyle.Italic);
            _statusText.enabled = false;
        }

        private static GameObject MakePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text MakeLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string text, int fontSize, Color color, TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var label = go.GetComponent<Text>();
            label.font = UIStyleGuide.GetDefaultFont();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static InputField MakeTextInput(GameObject fieldGo, int characterLimit, string placeholder)
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
            input.contentType = InputField.ContentType.Standard;
            input.caretColor = UIStyleGuide.Colors.FossilGold;
            input.selectionColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.42f);
            input.textComponent = MakeInputText(fieldGo.transform, "FieldText", "");
            input.placeholder = MakeInputText(fieldGo.transform, "Placeholder", placeholder);
            input.placeholder.color = UIStyleGuide.Colors.BodyTextSubtle;
            return input;
        }

        private static Text MakeInputText(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 4f);
            rt.offsetMax = new Vector2(-12f, -4f);

            var label = go.GetComponent<Text>();
            label.font = UIStyleGuide.GetDefaultFont();
            label.text = text;
            label.color = UIStyleGuide.Colors.BodyText;
            label.fontSize = UIStyleGuide.Typography.BodySmall;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static Button MakeButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = bgColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            UIStyleGuide.ApplyPaintedButtonChrome(button, UIButtonVariant.Secondary);

            MakeLabel(go.transform, "Label", new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f),
                label, UIStyleGuide.Typography.BodySmall, UIStyleGuide.GetButtonTextColor(UIButtonVariant.Secondary),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
