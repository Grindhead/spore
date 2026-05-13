using System;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// UI shown when player upgrades: apply the recommended mutated genome variant.
    /// Step 10 cell-stage evolution (memory-bank/cellStageImplementationSteps.md).
    /// </summary>
    public class MutationChoiceUI : UIScreen
    {
        public const string ID = "MutationChoice";
        public override string ScreenId => ID;

        private Genome[] _options;
        private Genome _parentGenome;
        private Action<Genome> _onChosen;
        private Canvas _canvas;
        private string _biasDescription;
        private bool _pausedByThisUI;

        /// <summary>
        /// Show the mutation upgrade screen with the recommended genome option.
        /// <paramref name="biasDescription"/> (optional) is shown as a subtitle explaining how play style shaped the options.
        /// OnChosen is invoked when the player picks one; panel is destroyed after choice.
        /// </summary>
        public static void Show(Genome option, Action<Genome> onChosen, string biasDescription = null)
        {
            Show(option != null ? new[] { option } : null, onChosen, biasDescription, null);
        }

        public static void Show(Genome[] options, Action<Genome> onChosen, string biasDescription = null, Genome parentGenome = null)
        {
            if (options == null || options.Length == 0 || onChosen == null) return;
            var go = new GameObject("MutationChoiceUI");
            var ui = go.AddComponent<MutationChoiceUI>();
            ui._options = options;
            ui._parentGenome = parentGenome;
            ui._onChosen = onChosen;
            ui._biasDescription = biasDescription;
            AudioManager.Instance?.PlaySFX(SFXType.MutationChoiceShown);
            ui.PauseGameplayIfNeeded();
            ui.BuildUI();
        }

        private static readonly Color PrimaryAccent = UIStyleGuide.Colors.TideTealBright;

        private void BuildUI()
        {
            var font = UIStyleGuide.GetDefaultFont();

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 101;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            gameObject.AddComponent<GraphicRaycaster>();

            // Dimmer
            var dimmer = new GameObject("Dimmer");
            dimmer.transform.SetParent(_canvas.transform, false);
            var dimRect = dimmer.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimmer.AddComponent<Image>().color = UIStyleGuide.Colors.OverlayDim;
            UIStyleGuide.AddScreenAtmosphere(_canvas.transform, "Mutation", 1f);

            var border = new GameObject("PanelBorder");
            border.transform.SetParent(_canvas.transform, false);
            var brRect = border.AddComponent<RectTransform>();
            brRect.anchorMin = new Vector2(0.1f, 0.13f);
            brRect.anchorMax = new Vector2(0.9f, 0.87f);
            brRect.offsetMin = Vector2.zero;
            brRect.offsetMax = Vector2.zero;
            var borderImage = border.AddComponent<Image>();
            borderImage.sprite = UIStyleGuide.GetPaintedPanelSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.2f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvas.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.11f, 0.14f);
            panelRect.anchorMax = new Vector2(0.89f, 0.86f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UIStyleGuide.ApplyPaintedPanelChrome(panel, strong: true, alpha: 0.98f);

            // Top accent bar
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(panel.transform, false);
            var tbRect = topBar.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(1f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = Vector2.zero;
            tbRect.sizeDelta = new Vector2(0, 5);
            topBar.AddComponent<Image>().color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.78f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.84f);
            titleRect.anchorMax = new Vector2(0.95f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGo.AddComponent<Text>();
            titleText.text = _options != null && _options.Length > 1 ? "CHOOSE MUTATION" : "UPGRADE";
            titleText.font = font;
            titleText.fontSize = 42;
            titleText.color = UIStyleGuide.Colors.AccentStrong;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            var titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.8f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);
            UIStyleGuide.ApplyTextShadow(titleGo, 0.45f);

            float biasBottom = 0.73f;
            if (!string.IsNullOrEmpty(_biasDescription))
            {
                var subGo = new GameObject("BiasSubtitle");
                subGo.transform.SetParent(panel.transform, false);
                var subRect = subGo.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0.05f, 0.72f);
                subRect.anchorMax = new Vector2(0.96f, 0.84f);
                subRect.offsetMin = Vector2.zero;
                subRect.offsetMax = Vector2.zero;
                var subText = subGo.AddComponent<Text>();
                subText.text = _biasDescription;
                subText.font = font;
                subText.fontSize = 23;
                subText.color = UIStyleGuide.Colors.BodyTextMuted;
                subText.alignment = TextAnchor.MiddleCenter;
                subText.fontStyle = FontStyle.Italic;
                biasBottom = 0.64f;
            }

            CreateChoiceButtons(panel.GetComponent<RectTransform>(), biasBottom);
        }

        private void CreateChoiceButtons(RectTransform parent, float biasBottom)
        {
            if (_options == null || _options.Length == 0)
                return;

            if (_options.Length == 1)
            {
                string traitLabel = EvolutionStatsUI.FormatTraits(_options[0]);
                if (string.IsNullOrEmpty(traitLabel)) traitLabel = "Apply Upgrade";
                CreateChoiceButton(parent, _options[0], traitLabel, "GO", 0.28f, Mathf.Min(0.56f, biasBottom - 0.04f));
                return;
            }

            int count = Mathf.Min(3, _options.Length);
            float top = Mathf.Min(0.67f, biasBottom - 0.035f);
            float buttonHeight = 0.145f;
            float gap = 0.025f;
            for (int i = 0; i < count; i++)
            {
                float yMax = top - i * (buttonHeight + gap);
                float yMin = yMax - buttonHeight;
                int optionIndex = i;
                string label = MutationSystem.GetStrategicChoiceLabel(_options[optionIndex], _parentGenome, optionIndex);
                string letter = ((char)('A' + optionIndex)).ToString();
                CreateChoiceButton(parent, _options[optionIndex], label, letter, yMin, yMax);
            }
        }

        private void CreateChoiceButton(RectTransform parent, Genome option, string label, string letter, float yMin, float yMax)
        {
            var font = UIStyleGuide.GetDefaultFont();
            Color accent = PrimaryAccent;

            // Border
            var frameGo = new GameObject("Frame_Upgrade");
            frameGo.transform.SetParent(parent, false);
            var frameRect = frameGo.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.06f, yMin - 0.005f);
            frameRect.anchorMax = new Vector2(0.94f, yMax + 0.005f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            var frameImage = frameGo.AddComponent<Image>();
            frameImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            frameImage.type = Image.Type.Sliced;
            frameImage.color = UIStyleGuide.Colors.WithAlpha(accent, 0.22f);

            var btnGo = new GameObject("Btn_Upgrade");
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.07f, yMin);
            btnRect.anchorMax = new Vector2(0.93f, yMax);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnImage = btnGo.AddComponent<Image>();
            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImage;
            UIStyleGuide.ApplyPaintedButtonChrome(button, UIButtonVariant.Secondary);
            UIStyleGuide.AddButtonPearl(btnGo, UIButtonVariant.Secondary, 28f, 0.045f);
            button.onClick.AddListener(() => OnUpgradeSelected(option));

            // Left accent bar
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(btnGo.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.1f);
            barRect.anchorMax = new Vector2(0f, 0.9f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(5, 0);
            barGo.AddComponent<Image>().color = UIStyleGuide.Colors.WithAlpha(accent, 0.9f);

            // Variant letter badge
            var letterGo = new GameObject("Letter");
            letterGo.transform.SetParent(btnGo.transform, false);
            var letterRect = letterGo.AddComponent<RectTransform>();
            letterRect.anchorMin = new Vector2(0f, 0f);
            letterRect.anchorMax = new Vector2(0f, 1f);
            letterRect.pivot = new Vector2(0f, 0.5f);
            letterRect.anchoredPosition = new Vector2(12, 0);
            letterRect.sizeDelta = new Vector2(44, 0);
            var letterText = letterGo.AddComponent<Text>();
            letterText.text = letter;
            letterText.font = font;
            letterText.fontSize = 30;
            letterText.color = UIStyleGuide.Colors.WithAlpha(accent, 0.9f);
            letterText.alignment = TextAnchor.MiddleCenter;
            letterText.fontStyle = FontStyle.Bold;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(64, 0);
            textRect.offsetMax = Vector2.zero;
            var btnText = textGo.AddComponent<Text>();
            btnText.text = label;
            btnText.font = font;
            btnText.fontSize = 29;
            btnText.color = UIStyleGuide.Colors.TextOnCool;
            btnText.alignment = TextAnchor.MiddleLeft;
            btnText.horizontalOverflow = HorizontalWrapMode.Wrap;
            btnText.verticalOverflow = VerticalWrapMode.Overflow;
            btnText.resizeTextForBestFit = true;
            btnText.resizeTextMinSize = 23;
            btnText.resizeTextMaxSize = 30;
            UIStyleGuide.ApplyTextShadow(textGo, 0.32f);
        }

        private void OnUpgradeSelected(Genome option)
        {
            if (option == null || _onChosen == null) return;
            _onChosen?.Invoke(option);
            ResumeGameplayIfNeeded();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ResumeGameplayIfNeeded();
        }

        private void PauseGameplayIfNeeded()
        {
            if (!GameManager.HasInstance) return;
            if (GameManager.Instance.IsPaused) return;

            GameManager.Instance.SetPaused(true, suppressPauseMenu: true);
            _pausedByThisUI = true;
        }

        private void ResumeGameplayIfNeeded()
        {
            if (!_pausedByThisUI) return;
            if (!GameManager.HasInstance) return;
            if (!GameManager.Instance.IsPaused)
            {
                _pausedByThisUI = false;
                return;
            }

            GameManager.Instance.SetPaused(false);
            _pausedByThisUI = false;
        }
    }
}
