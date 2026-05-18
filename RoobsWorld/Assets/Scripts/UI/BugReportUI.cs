using System;
using System.Collections;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    public class BugReportUI : MonoBehaviour
    {
        private Canvas _canvas;
        private Font _font;
        private InputField _descriptionInput;
        private InputField _emailInput;
        private Text _statusText;
        private bool _submitting;

        public Action OnClosed;

        public void Show()
        {
            if (_canvas == null)
                BuildUI();

            ResetForm();
            _canvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void BuildUI()
        {
            _font = UIStyleGuide.GetDefaultFont();
            float textScale = SettingsManager.GetUITextScale();

            var canvasObj = new GameObject("BugReportCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 245;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            var dimmer = CreateFullRect(canvasObj.transform, "Dimmer");
            dimmer.AddComponent<Image>().color = UIStyleGuide.Colors.OverlayDim;

            var panel = new GameObject("BugReportPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 540f);
            UIStyleGuide.ApplyPaintedPanelChrome(panel, strong: true, alpha: 0.98f);

            AddText(panel.transform, "Title", "Report Bug", new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f),
                Mathf.RoundToInt(UIStyleGuide.Typography.TitleMedium * textScale), UIStyleGuide.Colors.FossilGoldBright, FontStyle.Bold, TextAnchor.MiddleCenter);

            AddText(panel.transform, "DescriptionLabel", "What happened?", new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.82f),
                Mathf.RoundToInt(UIStyleGuide.Typography.BodyMedium * textScale), UIStyleGuide.Colors.BodyText, FontStyle.Bold, TextAnchor.MiddleLeft);

            _descriptionInput = CreateInputField(panel.transform, "DescriptionInput", "Short repro, expected result, and what went wrong",
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.74f), textScale, multiline: true);

            AddText(panel.transform, "EmailLabel", "Email (optional)", new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.33f),
                Mathf.RoundToInt(UIStyleGuide.Typography.BodyMedium * textScale), UIStyleGuide.Colors.BodyText, FontStyle.Bold, TextAnchor.MiddleLeft);

            _emailInput = CreateInputField(panel.transform, "EmailInput", "you@example.com",
                new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.27f), textScale, multiline: false);

            _statusText = AddText(panel.transform, "Status", "", new Vector2(0.08f, 0.105f), new Vector2(0.92f, 0.155f),
                Mathf.RoundToInt(UIStyleGuide.Typography.BodySmall * textScale), UIStyleGuide.Colors.BodyTextMuted, FontStyle.Normal, TextAnchor.MiddleLeft);

            CreateActionButton(panel.transform, "Send", new Vector2(0.53f, 0.04f), new Vector2(0.73f, 0.105f), OnSubmit, UIButtonVariant.Primary, textScale);
            CreateActionButton(panel.transform, "Back", new Vector2(0.75f, 0.04f), new Vector2(0.92f, 0.105f), OnBack, UIButtonVariant.Secondary, textScale);
        }

        private InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax, float textScale, bool multiline)
        {
            var fieldObj = new GameObject(name);
            fieldObj.transform.SetParent(parent, false);
            var rect = fieldObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = fieldObj.AddComponent<Image>();
            image.sprite = UIStyleGuide.GetPaintedFieldSprite();
            image.type = Image.Type.Sliced;
            image.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.HudPanelStrong, 0.95f);

            var input = fieldObj.AddComponent<InputField>();
            input.targetGraphic = image;
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.characterLimit = multiline ? 2000 : 160;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(fieldObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, multiline ? 12f : 0f);
            textRect.offsetMax = new Vector2(-16f, multiline ? -12f : 0f);
            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.fontSize = Mathf.RoundToInt(UIStyleGuide.Typography.BodyMedium * textScale);
            text.color = UIStyleGuide.Colors.BodyText;
            text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = multiline ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            input.textComponent = text;

            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(fieldObj.transform, false);
            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = textRect.offsetMin;
            placeholderRect.offsetMax = textRect.offsetMax;
            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.font = _font;
            placeholderText.text = placeholder;
            placeholderText.fontSize = Mathf.RoundToInt(UIStyleGuide.Typography.BodySmall * textScale);
            placeholderText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.BodyTextSubtle, 0.8f);
            placeholderText.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
            input.placeholder = placeholderText;

            return input;
        }

        private Text AddText(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, FontStyle style, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = obj.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (style == FontStyle.Bold)
                UIStyleGuide.ApplyTextShadow(obj, 0.32f);
            return text;
        }

        private void CreateActionButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Action onClick, UIButtonVariant variant, float textScale)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = btnObj.AddComponent<Image>();
            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;
            UIStyleGuide.ApplyPaintedButtonChrome(button, variant);
            button.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = Mathf.RoundToInt(UIStyleGuide.Typography.BodyMedium * textScale);
            text.alignment = TextAnchor.MiddleCenter;
            UIStyleGuide.ApplyButtonLabel(text, variant, bold: true);
            UIStyleGuide.ApplyTextShadow(textObj, 0.28f);
        }

        private void OnSubmit()
        {
            if (_submitting)
                return;

            var request = new BugReportRequest(_descriptionInput.text, _emailInput.text, attachScreenshot: true);
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                ShowStatus(false, "Describe the bug first.");
                return;
            }

            if (!BugReporting.IsAvailable)
            {
                ShowStatus(false, "Bug reporting is not configured.");
                return;
            }

            StartCoroutine(SubmitAfterScreenshotFrame(request));
        }

        private IEnumerator SubmitAfterScreenshotFrame(BugReportRequest request)
        {
            _submitting = true;
            _canvas.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();

            bool submitted = BugReporting.Submit(request, out string error);
            yield return null;

            _canvas.gameObject.SetActive(true);
            ShowStatus(submitted, submitted ? "Report sent." : error);
            if (submitted)
            {
                _descriptionInput.text = "";
                _emailInput.text = "";
            }

            _submitting = false;
        }

        private void ShowStatus(bool success, string message)
        {
            _statusText.color = success ? UIStyleGuide.Colors.Success : UIStyleGuide.Colors.Warning;
            _statusText.text = message;
        }

        private void OnBack()
        {
            Hide();
            OnClosed?.Invoke();
        }

        private void ResetForm()
        {
            if (_descriptionInput != null)
                _descriptionInput.text = "";
            if (_emailInput != null)
                _emailInput.text = "";
            if (_statusText != null)
            {
                _statusText.color = UIStyleGuide.Colors.BodyTextMuted;
                _statusText.text = "";
            }
        }

        private static GameObject CreateFullRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }
    }
}
