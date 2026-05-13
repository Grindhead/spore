using System;
using System.Collections.Generic;
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

        private IReadOnlyList<RoobsWorld.Creature.CreatureUpgradeOption> _options;
        private Action<RoobsWorld.Creature.CreatureUpgradeOption> _onChosen;
        private Canvas _canvas;
        private bool _pausedByThisUI;

        public static void Show(
            RoobsWorld.Creature.CreatureUpgradeOption option,
            Action<RoobsWorld.Creature.CreatureUpgradeOption> onChosen)
        {
            Show(new[] { option }, onChosen);
        }

        public static void Show(
            IReadOnlyList<RoobsWorld.Creature.CreatureUpgradeOption> options,
            Action<RoobsWorld.Creature.CreatureUpgradeOption> onChosen)
        {
            if (options == null || options.Count == 0 || onChosen == null)
                return;

            var go = new GameObject("CreatureUpgradeChoiceUI");
            var ui = go.AddComponent<CreatureUpgradeChoiceUI>();
            ui._options = options;
            ui._onChosen = onChosen;
            AudioManager.Instance?.PlaySFX(SFXType.MutationChoiceShown);
            ui.PauseGameplayIfNeeded();
            ui.BuildUI();
        }

        private void BuildUI()
        {
            var font = UIStyleGuide.GetDefaultFont();
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 175;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            gameObject.AddComponent<GraphicRaycaster>();

            var dimmer = CreatePanel(_canvas.transform, "Dimmer", Vector2.zero, Vector2.one, UIStyleGuide.Colors.OverlayDim);
            dimmer.raycastTarget = true;
            UIStyleGuide.AddScreenAtmosphere(_canvas.transform, "CreatureUpgrade", 1f);

            var panel = CreatePanel(
                _canvas.transform,
                "Panel",
                new Vector2(0.12f, 0.14f),
                new Vector2(0.88f, 0.86f),
                UIStyleGuide.Colors.ScreenBackground);
            UIStyleGuide.ApplyPaintedPanelChrome(panel.gameObject, strong: true, alpha: 0.98f);

            CreatePanel(panel.transform, "TopAccent", new Vector2(0f, 0.992f), Vector2.one, UIStyleGuide.Colors.AccentStrong);
            CreatePanel(panel.transform, "WarmShelf", new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.7f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.74f));

            var title = CreateText(panel.transform, "Title", "ADAPTATION READY", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), 42, FontStyle.Bold, TextAnchor.MiddleCenter, UIStyleGuide.Colors.AccentStrong, font);
            UIStyleGuide.ApplyTextShadow(title.gameObject, 0.48f);
            CreateText(panel.transform, "Subtitle", "Choose the body plan you want to live with.", new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.84f), 24, FontStyle.Italic, TextAnchor.MiddleCenter, UIStyleGuide.Colors.Subtitle, font);

            CreateChoiceButtons(panel.rectTransform, font);
        }

        private void CreateChoiceButtons(RectTransform parent, Font font)
        {
            int count = Mathf.Min(4, _options.Count);
            if (count == 1)
            {
                CreateChoiceButton(parent, _options[0], 0.28f, 0.54f, font);
                return;
            }

            float top = 0.66f;
            float buttonHeight = count >= 4 ? 0.125f : 0.15f;
            float gap = count >= 4 ? 0.018f : 0.025f;
            for (int i = 0; i < count; i++)
            {
                float yMax = top - i * (buttonHeight + gap);
                float yMin = yMax - buttonHeight;
                CreateChoiceButton(parent, _options[i], yMin, yMax, font);
            }
        }

        private void CreateChoiceButton(
            RectTransform parent,
            RoobsWorld.Creature.CreatureUpgradeOption option,
            float yMin,
            float yMax,
            Font font)
        {
            Color accent = UIStyleGuide.Colors.CoralBright;

            var buttonImage = CreatePanel(parent, "Upgrade", new Vector2(0.08f, yMin), new Vector2(0.92f, yMax), UIStyleGuide.Colors.TextOnCool);
            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            UIStyleGuide.ApplyPaintedButtonChrome(button, UIButtonVariant.Secondary);
            UIStyleGuide.AddButtonPearl(buttonImage.gameObject, UIButtonVariant.Secondary, 28f, 0.035f);
            button.onClick.AddListener(() => Select(option));

            CreatePanel(buttonImage.transform, "Accent", new Vector2(0f, 0f), new Vector2(0.012f, 1f), accent);
            CreateText(buttonImage.transform, "Title", BuildChoiceTitle(option), new Vector2(0.045f, 0.56f), new Vector2(0.48f, 0.94f), 30, FontStyle.Bold, TextAnchor.MiddleLeft, UIStyleGuide.Colors.BodyText, font);
            CreateText(buttonImage.transform, "Repercussion", FormatDelta(option), new Vector2(0.5f, 0.56f), new Vector2(0.96f, 0.94f), 27, FontStyle.Bold, TextAnchor.MiddleRight, accent, font);
            CreateText(buttonImage.transform, "Description", BuildChoiceDescription(option), new Vector2(0.045f, 0.08f), new Vector2(0.96f, 0.54f), 24, FontStyle.Normal, TextAnchor.MiddleLeft, UIStyleGuide.Colors.BodyTextMuted, font);
        }

        public static string BuildChoiceTitle(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            if (string.IsNullOrWhiteSpace(option.Tag))
                return option.Title;

            return $"{option.Title} [{option.Tag.Trim()}]";
        }

        public static string BuildChoiceDescription(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            if (string.IsNullOrWhiteSpace(option.Tradeoff))
                return option.Description;

            return $"{option.Description} Tradeoff: {option.Tradeoff.Trim()}";
        }

        private static string FormatDelta(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            string hp = option.HealthDelta >= 0f ? $"+{option.HealthDelta:0} HP" : $"{option.HealthDelta:0} HP";
            string speed = option.SpeedDelta >= 0f ? $"+{option.SpeedDelta:0.0} SPD" : $"{option.SpeedDelta:0.0} SPD";
            string summary = $"{hp}   {speed}";
            if (Mathf.Abs(option.AttackDelta) > 0.01f)
                summary += option.AttackDelta >= 0f ? $"   +{option.AttackDelta:0.0} ATK" : $"   {option.AttackDelta:0.0} ATK";
            if (Mathf.Abs(option.DefenseDelta) > 0.01f)
                summary += option.DefenseDelta >= 0f ? $"   +{option.DefenseDelta:0.0} DEF" : $"   {option.DefenseDelta:0.0} DEF";
            if (Mathf.Abs(option.MaxStaminaDelta) > 0.01f)
                summary += option.MaxStaminaDelta >= 0f ? $"   +{option.MaxStaminaDelta:0} STA" : $"   {option.MaxStaminaDelta:0} STA";
            if (Mathf.Abs(option.StaminaRecoveryDelta) > 0.01f)
                summary += option.StaminaRecoveryDelta >= 0f ? $"   +{option.StaminaRecoveryDelta:0}/s STA" : $"   {option.StaminaRecoveryDelta:0}/s STA";
            if (Mathf.Abs(option.AbilityCooldownDelta) > 0.01f)
                summary += option.AbilityCooldownDelta <= 0f ? $"   {option.AbilityCooldownDelta:0.0}s CD" : $"   +{option.AbilityCooldownDelta:0.0}s CD";
            return summary;
        }

        private void Select(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            if (string.IsNullOrEmpty(option.Id))
                return;

            _onChosen?.Invoke(option);
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
