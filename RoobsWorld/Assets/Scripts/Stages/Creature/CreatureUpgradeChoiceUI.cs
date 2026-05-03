using System;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Modal creature-stage upgrade prompt focused on the recommended health/speed tradeoff.
    /// </summary>
    public class CreatureUpgradeChoiceUI : UIScreen
    {
        public const string ID = "CreatureUpgradeChoice";
        public override string ScreenId => ID;

        private RoobsWorld.Creature.CreatureUpgradeOption _option;
        private Action<RoobsWorld.Creature.CreatureUpgradeOption> _onChosen;
        private Canvas _canvas;
        private bool _pausedByThisUI;

        public static void Show(
            RoobsWorld.Creature.CreatureUpgradeOption option,
            Action<RoobsWorld.Creature.CreatureUpgradeOption> onChosen)
        {
            if (string.IsNullOrEmpty(option.Id) || onChosen == null)
                return;

            var go = new GameObject("CreatureUpgradeChoiceUI");
            var ui = go.AddComponent<CreatureUpgradeChoiceUI>();
            ui._option = option;
            ui._onChosen = onChosen;
            AudioManager.Instance?.PlaySFX(SFXType.MutationChoiceShown);
            ui.PauseGameplayIfNeeded();
            ui.BuildUI();
        }

        private void BuildUI()
        {
            var font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 175;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            var dimmer = CreatePanel(_canvas.transform, "Dimmer", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.58f));
            dimmer.raycastTarget = true;

            var panel = CreatePanel(
                _canvas.transform,
                "Panel",
                new Vector2(0.12f, 0.14f),
                new Vector2(0.88f, 0.86f),
                new Color(0.055f, 0.065f, 0.11f, 0.98f));

            CreateText(panel.transform, "Title", "ADAPTATION READY", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.55f, 0.9f, 1f), font);
            CreateText(panel.transform, "Subtitle", "Your creature has grown into a new body plan.", new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.84f), 24, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.72f, 0.8f, 0.88f), font);

            CreateChoiceButton(panel.rectTransform, _option, 0.28f, 0.54f, font);
        }

        private void CreateChoiceButton(
            RectTransform parent,
            RoobsWorld.Creature.CreatureUpgradeOption option,
            float yMin,
            float yMax,
            Font font)
        {
            Color accent = new Color(0.95f, 0.55f, 0.36f, 1f);

            var buttonImage = CreatePanel(parent, "Upgrade", new Vector2(0.08f, yMin), new Vector2(0.92f, yMax), Color.white);
            var button = buttonImage.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(accent.r * 0.16f, accent.g * 0.16f, accent.b * 0.16f, 0.95f);
            colors.highlightedColor = new Color(accent.r * 0.27f, accent.g * 0.27f, accent.b * 0.27f, 1f);
            colors.pressedColor = new Color(accent.r * 0.1f, accent.g * 0.1f, accent.b * 0.1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(Select);

            CreatePanel(buttonImage.transform, "Accent", new Vector2(0f, 0f), new Vector2(0.012f, 1f), accent);
            CreateText(buttonImage.transform, "Title", option.Title, new Vector2(0.045f, 0.56f), new Vector2(0.48f, 0.94f), 30, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 1f), font);
            CreateText(buttonImage.transform, "Repercussion", FormatDelta(option), new Vector2(0.5f, 0.56f), new Vector2(0.96f, 0.94f), 27, FontStyle.Bold, TextAnchor.MiddleRight, accent, font);
            CreateText(buttonImage.transform, "Description", option.Description, new Vector2(0.045f, 0.08f), new Vector2(0.96f, 0.54f), 24, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.8f, 0.86f, 0.92f), font);
        }

        private static string FormatDelta(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            string hp = option.HealthDelta >= 0f ? $"+{option.HealthDelta:0} HP" : $"{option.HealthDelta:0} HP";
            string speed = option.SpeedDelta >= 0f ? $"+{option.SpeedDelta:0.0} SPD" : $"{option.SpeedDelta:0.0} SPD";
            return $"{hp}   {speed}";
        }

        private void Select()
        {
            if (string.IsNullOrEmpty(_option.Id))
                return;

            _onChosen?.Invoke(_option);
            ResumeGameplayIfNeeded();
            Destroy(gameObject);
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color,
            Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 4);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private void PauseGameplayIfNeeded()
        {
            if (!GameManager.HasInstance || GameManager.Instance.IsPaused)
                return;

            GameManager.Instance.SetPaused(true, suppressPauseMenu: true);
            _pausedByThisUI = true;
        }

        private void ResumeGameplayIfNeeded()
        {
            if (!_pausedByThisUI || !GameManager.HasInstance)
                return;

            if (GameManager.Instance.IsPaused)
                GameManager.Instance.SetPaused(false);
            _pausedByThisUI = false;
        }

        private void OnDestroy()
        {
            ResumeGameplayIfNeeded();
        }
    }
}
