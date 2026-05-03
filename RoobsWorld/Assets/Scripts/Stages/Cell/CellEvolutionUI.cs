using RoobsWorld.Core;
using RoobsWorld.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Full-screen UI shown on cell evolution, allowing the player to pick a new cell part upgrade.
    /// </summary>
    public class CellEvolutionUI : UIScreen
    {
        private sealed class PartOption
        {
            public CellPartType Type;
            public string Name;
            public string Effect;
            public string Flavor;
            public string Glyph;
            public Color Accent;
        }

        public const string ID = "CellEvolution";
        public override string ScreenId => ID;

        private PlayerCell _playerCell;
        private Canvas _canvas;
        private readonly List<PartOption> _partOptions = new();
        private readonly Dictionary<CellPartType, Button> _partButtons = new();
        private readonly Dictionary<CellPartType, Image> _partFrames = new();

        private Text _statusText;
        private Text _selectedPartNameText;
        private Text _selectedPartEffectText;
        private Text _selectedPartFlavorText;
        private Text _previewGlyphText;
        private Text _confirmButtonText;
        private Button _confirmButton;
        private CellPartType _selectedPart;
        private bool _pausedByThisUI;

        public static CellEvolutionUI CreateRuntime(PlayerCell playerCell)
        {
            var go = new GameObject("CellEvolutionUI");
            var ui = go.AddComponent<CellEvolutionUI>();
            ui._playerCell = playerCell;
            ui.BuildUI();
            UIManager.Instance.RegisterScreen(ui);
            return ui;
        }

        public override void Show()
        {
            base.Show();
            PauseGameplayIfNeeded();
        }

        public override void Hide()
        {
            ResumeGameplayIfNeeded();
            base.Hide();
        }

        private void BuildUI()
        {
            var font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            BuildPartOptions();

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // Dimmer overlay
            var dimmer = new GameObject("Dimmer");
            dimmer.transform.SetParent(_canvas.transform, false);
            var dimRect = dimmer.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimmer.AddComponent<Image>().color = new Color(0.01f, 0.03f, 0.08f, 0.86f);

            var rootPanel = CreatePanel("GeneLabRoot", _canvas.transform, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f), new Color(0.03f, 0.1f, 0.19f, 0.96f));
            var rootBorder = rootPanel.gameObject.AddComponent<Outline>();
            rootBorder.effectColor = new Color(0.24f, 0.68f, 1f, 0.8f);
            rootBorder.effectDistance = new Vector2(2f, -2f);

            var topStrip = CreatePanel("TopStrip", rootPanel, new Vector2(0f, 0.93f), new Vector2(1f, 1f), new Color(0.13f, 0.31f, 0.53f, 0.96f));
            CreateText("Title", topStrip, font, "EVOLUTION LAB", 44, new Color(0.74f, 0.92f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);

            _statusText = CreateText("Status", rootPanel, font, "Select a gene and confirm your mutation.", 24, new Color(1f, 0.55f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var statusRect = _statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.26f, 0.86f);
            statusRect.anchorMax = new Vector2(0.74f, 0.92f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            var palettePanel = CreatePanel("GenePalette", rootPanel, new Vector2(0.02f, 0.12f), new Vector2(0.3f, 0.9f), new Color(0.02f, 0.14f, 0.28f, 0.95f));
            CreateText("PaletteTitle", palettePanel, font, "GENE PARTS", 26, new Color(0.66f, 0.87f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 6f, 50f);

            var stagePanel = CreatePanel("CellStage", rootPanel, new Vector2(0.32f, 0.12f), new Vector2(0.74f, 0.9f), new Color(0.03f, 0.19f, 0.35f, 0.95f));
            var infoPanel = CreatePanel("GeneInfo", rootPanel, new Vector2(0.76f, 0.12f), new Vector2(0.98f, 0.9f), new Color(0.03f, 0.12f, 0.24f, 0.95f));

            CreatePaletteButtons(palettePanel, font);
            CreatePreviewStage(stagePanel, font);
            CreateInfoPanel(infoPanel, font);

            var footer = CreatePanel("Footer", rootPanel, new Vector2(0f, 0f), new Vector2(1f, 0.1f), new Color(0.06f, 0.16f, 0.29f, 0.95f));
            CreateText("FooterHint", footer, font, "Palette on the left - Preview in the center - Mutation controls on the right", 20, new Color(0.64f, 0.81f, 0.93f), TextAnchor.MiddleCenter, FontStyle.Italic);

            _selectedPart = SelectInitialPart();
            RefreshSelectionVisuals();
        }

        private void BuildPartOptions()
        {
            _partOptions.Clear();
            _partOptions.Add(new PartOption
            {
                Type = CellPartType.Flagella,
                Name = "Flagella",
                Effect = "+20% swim speed",
                Flavor = "Fast and agile propulsion for quick turns and chases.",
                Glyph = "~~",
                Accent = new Color(0.2f, 0.9f, 0.4f)
            });
            _partOptions.Add(new PartOption
            {
                Type = CellPartType.Cilia,
                Name = "Cilia",
                Effect = "+10% swim speed",
                Flavor = "Balanced locomotion upgrade with steady movement control.",
                Glyph = "///",
                Accent = new Color(0.2f, 0.6f, 1f)
            });
            _partOptions.Add(new PartOption
            {
                Type = CellPartType.Spike,
                Name = "Spike",
                Effect = "Contact damage",
                Flavor = "Weaponized surface trait to punish close-range predators.",
                Glyph = "^",
                Accent = new Color(1f, 0.25f, 0.25f)
            });
            _partOptions.Add(new PartOption
            {
                Type = CellPartType.Jet,
                Name = "Jet",
                Effect = "+35% swim speed",
                Flavor = "Burst propulsion mutation for high-speed repositioning.",
                Glyph = ">>",
                Accent = new Color(1f, 0.7f, 0.2f)
            });
        }

        private void CreatePaletteButtons(RectTransform parent, Font font)
        {
            int count = _partOptions.Count;
            for (int i = 0; i < count; i++)
            {
                var option = _partOptions[i];
                int col = i % 2;
                int row = i / 2;

                float width = 0.43f;
                float height = 0.32f;
                float xMin = col == 0 ? 0.06f : 0.51f;
                float xMax = xMin + width;
                float yMax = row == 0 ? 0.82f : 0.45f;
                float yMin = yMax - height;

                var frame = CreatePanel($"Frame_{option.Type}", parent, new Vector2(xMin, yMin), new Vector2(xMax, yMax), new Color(option.Accent.r, option.Accent.g, option.Accent.b, 0.27f));
                _partFrames[option.Type] = frame.GetComponent<Image>();

                var buttonObj = new GameObject($"PartButton_{option.Type}");
                buttonObj.transform.SetParent(frame, false);
                var buttonRect = buttonObj.AddComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.02f, 0.02f);
                buttonRect.anchorMax = new Vector2(0.98f, 0.98f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                var buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.05f, 0.12f, 0.21f, 0.96f);

                var button = buttonObj.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = buttonImage.color;
                colors.highlightedColor = new Color(0.1f, 0.2f, 0.32f, 1f);
                colors.pressedColor = new Color(0.06f, 0.15f, 0.25f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
                button.colors = colors;
                button.targetGraphic = buttonImage;
                button.onClick.AddListener(() => OnPartSelected(option.Type));
                _partButtons[option.Type] = button;

                var glyph = CreateText("Glyph", buttonRect, font, option.Glyph, 38, option.Accent, TextAnchor.UpperCenter, FontStyle.Bold);
                var glyphRect = glyph.rectTransform;
                glyphRect.anchorMin = new Vector2(0f, 0.52f);
                glyphRect.anchorMax = new Vector2(1f, 1f);
                glyphRect.offsetMin = Vector2.zero;
                glyphRect.offsetMax = Vector2.zero;

                var name = CreateText("Name", buttonRect, font, option.Name, 22, new Color(0.85f, 0.93f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                var nameRect = name.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 0.3f);
                nameRect.anchorMax = new Vector2(1f, 0.58f);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;

                var effect = CreateText("Effect", buttonRect, font, option.Effect, 16, new Color(0.62f, 0.83f, 1f), TextAnchor.MiddleCenter, FontStyle.Italic);
                var effectRect = effect.rectTransform;
                effectRect.anchorMin = new Vector2(0.05f, 0.05f);
                effectRect.anchorMax = new Vector2(0.95f, 0.28f);
                effectRect.offsetMin = Vector2.zero;
                effectRect.offsetMax = Vector2.zero;
            }
        }

        private void CreatePreviewStage(RectTransform parent, Font font)
        {
            CreateText("PreviewTitle", parent, font, "LIVE PREVIEW", 26, new Color(0.73f, 0.91f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 8f, 48f);

            var ringOuter = CreateCircle("RingOuter", parent, new Vector2(0.5f, 0.54f), 0.72f, new Color(0.15f, 0.43f, 0.7f, 0.75f));
            var ringMid = CreateCircle("RingMid", parent, new Vector2(0.5f, 0.54f), 0.54f, new Color(0.2f, 0.56f, 0.82f, 0.63f));
            var ringInner = CreateCircle("RingInner", parent, new Vector2(0.5f, 0.54f), 0.35f, new Color(0.21f, 0.76f, 0.9f, 0.66f));

            ringOuter.SetSiblingIndex(1);
            ringMid.SetSiblingIndex(2);
            ringInner.SetSiblingIndex(3);

            _previewGlyphText = CreateText("PreviewGlyph", parent, font, "?", 74, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var glyphRect = _previewGlyphText.rectTransform;
            glyphRect.anchorMin = new Vector2(0.34f, 0.37f);
            glyphRect.anchorMax = new Vector2(0.66f, 0.71f);
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;

            var previewLabel = CreateText("PreviewCellName", parent, font, "Chemicell Prototype", 28, new Color(0.88f, 0.95f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var previewLabelRect = previewLabel.rectTransform;
            previewLabelRect.anchorMin = new Vector2(0.2f, 0.16f);
            previewLabelRect.anchorMax = new Vector2(0.8f, 0.25f);
            previewLabelRect.offsetMin = Vector2.zero;
            previewLabelRect.offsetMax = Vector2.zero;

            var previewHint = CreateText("PreviewHint", parent, font, "Center stage shows the selected mutation family.", 18, new Color(0.65f, 0.82f, 0.93f), TextAnchor.MiddleCenter, FontStyle.Italic);
            var previewHintRect = previewHint.rectTransform;
            previewHintRect.anchorMin = new Vector2(0.12f, 0.06f);
            previewHintRect.anchorMax = new Vector2(0.88f, 0.14f);
            previewHintRect.offsetMin = Vector2.zero;
            previewHintRect.offsetMax = Vector2.zero;
        }

        private void CreateInfoPanel(RectTransform parent, Font font)
        {
            CreateText("InfoTitle", parent, font, "GENE DETAILS", 24, new Color(0.69f, 0.88f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10f, 42f);

            _selectedPartNameText = CreateText("PartName", parent, font, string.Empty, 30, new Color(0.9f, 0.97f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var nameRect = _selectedPartNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.08f, 0.74f);
            nameRect.anchorMax = new Vector2(0.92f, 0.82f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            _selectedPartEffectText = CreateText("PartEffect", parent, font, string.Empty, 19, new Color(0.72f, 0.89f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var effectRect = _selectedPartEffectText.rectTransform;
            effectRect.anchorMin = new Vector2(0.08f, 0.66f);
            effectRect.anchorMax = new Vector2(0.92f, 0.74f);
            effectRect.offsetMin = Vector2.zero;
            effectRect.offsetMax = Vector2.zero;

            _selectedPartFlavorText = CreateText("PartFlavor", parent, font, string.Empty, 18, new Color(0.75f, 0.86f, 0.95f), TextAnchor.UpperCenter, FontStyle.Italic);
            var flavorRect = _selectedPartFlavorText.rectTransform;
            flavorRect.anchorMin = new Vector2(0.08f, 0.43f);
            flavorRect.anchorMax = new Vector2(0.92f, 0.64f);
            flavorRect.offsetMin = Vector2.zero;
            flavorRect.offsetMax = Vector2.zero;

            var confirmObj = new GameObject("ConfirmButton");
            confirmObj.transform.SetParent(parent, false);
            var confirmRect = confirmObj.AddComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.12f, 0.19f);
            confirmRect.anchorMax = new Vector2(0.88f, 0.32f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;

            var confirmImage = confirmObj.AddComponent<Image>();
            confirmImage.color = new Color(0.18f, 0.62f, 0.29f, 0.95f);

            _confirmButton = confirmObj.AddComponent<Button>();
            var colors = _confirmButton.colors;
            colors.normalColor = confirmImage.color;
            colors.highlightedColor = new Color(0.22f, 0.74f, 0.34f, 1f);
            colors.pressedColor = new Color(0.12f, 0.47f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.22f, 0.3f, 0.24f, 0.7f);
            _confirmButton.colors = colors;
            _confirmButton.targetGraphic = confirmImage;
            _confirmButton.onClick.AddListener(OnConfirmSelection);

            _confirmButtonText = CreateText("ConfirmText", confirmRect, font, "EVOLVE", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            var note = CreateText("Note", parent, font, "Evolution consumes this event and resumes gameplay.", 15, new Color(0.56f, 0.72f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Italic);
            var noteRect = note.rectTransform;
            noteRect.anchorMin = new Vector2(0.08f, 0.06f);
            noteRect.anchorMax = new Vector2(0.92f, 0.16f);
            noteRect.offsetMin = Vector2.zero;
            noteRect.offsetMax = Vector2.zero;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = color;
            return rect;
        }

        private RectTransform CreateCircle(string name, RectTransform parent, Vector2 center, float diameterFraction, Color color)
        {
            var circle = new GameObject(name);
            circle.transform.SetParent(parent, false);
            var rect = circle.AddComponent<RectTransform>();
            float half = diameterFraction * 0.5f;
            rect.anchorMin = new Vector2(center.x - half, center.y - half);
            rect.anchorMax = new Vector2(center.x + half, center.y + half);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = circle.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Simple;
            return rect;
        }

        private Text CreateText(string name, Transform parent, Font font, string value, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private CellPartType SelectInitialPart()
        {
            for (int i = 0; i < _partOptions.Count; i++)
            {
                if (_playerCell == null || !_playerCell.PartLoadout.HasPart(_partOptions[i].Type))
                    return _partOptions[i].Type;
            }

            return _partOptions[0].Type;
        }

        private PartOption GetOption(CellPartType partType)
        {
            for (int i = 0; i < _partOptions.Count; i++)
            {
                if (_partOptions[i].Type == partType)
                    return _partOptions[i];
            }

            return null;
        }

        private bool AreAllPartsOwned()
        {
            if (_playerCell == null) return false;
            for (int i = 0; i < _partOptions.Count; i++)
            {
                if (!_playerCell.PartLoadout.HasPart(_partOptions[i].Type))
                    return false;
            }

            return true;
        }

        private void RefreshSelectionVisuals()
        {
            var selectedOption = GetOption(_selectedPart);
            if (selectedOption == null) return;

            bool partAlreadyOwned = _playerCell != null && _playerCell.PartLoadout.HasPart(_selectedPart);
            bool allOwned = AreAllPartsOwned();

            _selectedPartNameText.text = selectedOption.Name.ToUpperInvariant();
            _selectedPartNameText.color = selectedOption.Accent;
            _selectedPartEffectText.text = selectedOption.Effect;
            _selectedPartFlavorText.text = selectedOption.Flavor;
            _previewGlyphText.text = selectedOption.Glyph;
            _previewGlyphText.color = selectedOption.Accent;

            foreach (var entry in _partButtons)
            {
                CellPartType type = entry.Key;
                bool isSelected = type == _selectedPart;
                bool isOwned = _playerCell != null && _playerCell.PartLoadout.HasPart(type);

                var frame = _partFrames[type];
                var option = GetOption(type);
                if (isOwned)
                    frame.color = new Color(0.31f, 0.33f, 0.38f, isSelected ? 0.92f : 0.65f);
                else if (isSelected)
                    frame.color = new Color(option.Accent.r, option.Accent.g, option.Accent.b, 0.9f);
                else
                    frame.color = new Color(option.Accent.r, option.Accent.g, option.Accent.b, 0.28f);
            }

            if (allOwned)
            {
                _statusText.text = "All known genes already installed. Continue to exit the editor.";
                _confirmButtonText.text = "CONTINUE";
                _confirmButton.interactable = true;
            }
            else if (partAlreadyOwned)
            {
                _statusText.text = "That gene is already installed. Pick another mutation.";
                _confirmButtonText.text = "SELECT NEW GENE";
                _confirmButton.interactable = false;
            }
            else
            {
                _statusText.text = "Ready to mutate. Apply this gene to continue evolution.";
                _confirmButtonText.text = "EVOLVE";
                _confirmButton.interactable = true;
            }
        }

        private void OnPartSelected(CellPartType partType)
        {
            _selectedPart = partType;
            RefreshSelectionVisuals();
        }

        private void OnConfirmSelection()
        {
            if (_playerCell == null)
            {
                UIManager.Instance.PopScreen();
                return;
            }

            bool allOwned = AreAllPartsOwned();
            bool unlocked = _playerCell.PartLoadout.TryUnlock(_selectedPart);

            if (unlocked)
            {
                GameManager.Instance.AddDNAPoints(BalanceConstants.CellDNAPointsPerEvolutionChoice);
            }
            else if (!allOwned)
            {
                RefreshSelectionVisuals();
                return;
            }

            UIManager.Instance.PopScreen();
            _playerCell.CompleteEvolution();
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
