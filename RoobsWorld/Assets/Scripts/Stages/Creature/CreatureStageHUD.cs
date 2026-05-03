using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Lightweight HUD for Creature stage that shows current health, encounter progress, and control hints.
    /// </summary>
    public class CreatureStageHUD : MonoBehaviour
    {
        private const float TopLeftHudSizeBoost = 1.22f;

        private Canvas _canvas;
        private Text _statsText;
        private Text _hintText;
        private Text _progressText;
        private Text _statusText;
        private Image _healthFill;
        private RoobsWorld.Creature.CreatureInstance _playerCreature;
        private int _activeRivals;
        private int _defeatedRivals;
        private int _targetDefeats;
        private int _desiredActiveRivals;
        private int _killStreak;

        public void Initialize(RoobsWorld.Creature.CreatureInstance playerCreature)
        {
            _playerCreature = playerCreature;
            if (_canvas == null)
                BuildUI();
            SetVisible(true);
            Refresh();
        }

        public void SetEncounterProgress(
            int defeatedRivals,
            int activeRivals,
            int targetDefeats,
            int desiredActiveRivals = 0,
            int killStreak = 0)
        {
            _defeatedRivals = Mathf.Max(0, defeatedRivals);
            _activeRivals = Mathf.Max(0, activeRivals);
            _targetDefeats = Mathf.Max(1, targetDefeats);
            _desiredActiveRivals = Mathf.Max(1, desiredActiveRivals);
            _killStreak = Mathf.Max(0, killStreak);
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.gameObject.activeInHierarchy)
                return;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void Refresh()
        {
            if (_statsText == null)
                return;

            string stageLabel = Localization.Get("stage.creature");

            if (_playerCreature == null)
            {
                _statsText.text = stageLabel;
                if (_healthFill != null)
                    _healthFill.enabled = false;
                if (_progressText != null)
                    _progressText.text = string.Format(
                        Localization.Get("creature.encounter.progress"),
                        _defeatedRivals,
                        _targetDefeats,
                        _activeRivals,
                        Mathf.Max(1, _desiredActiveRivals),
                        _killStreak);
                if (_statusText != null)
                    _statusText.text = Localization.Get("creature.status.hunt");
                return;
            }

            float hpCurrent = Mathf.Max(0f, _playerCreature.CurrentHealth);
            float hpMax = Mathf.Max(1f, _playerCreature.Stats.Health);
            int evolutionLevel = RoobsWorld.Creature.CreatureUpgradeCatalog.GetEvolutionLevel(_playerCreature.DNA);
            int dnaPoints = 0;
            if (GameManager.HasInstance && GameManager.Instance.CurrentSave != null)
                dnaPoints = GameManager.Instance.CurrentSave.DNAPoints;

            _statsText.text = $"{stageLabel}  |  Evo: {evolutionLevel}  |  {Localization.Get("hud.health")}: {hpCurrent:0}/{hpMax:0}  |  {Localization.Get("hud.dna")}: {dnaPoints}";
            if (_healthFill != null)
            {
                _healthFill.enabled = true;
                var fillRect = _healthFill.rectTransform;
                fillRect.anchorMax = new Vector2(Mathf.Lerp(0.02f, 0.98f, Mathf.Clamp01(hpCurrent / hpMax)), fillRect.anchorMax.y);
            }

            if (_progressText != null)
                _progressText.text = string.Format(
                    Localization.Get("creature.encounter.progress"),
                    _defeatedRivals,
                    _targetDefeats,
                    _activeRivals,
                    Mathf.Max(1, _desiredActiveRivals),
                    _killStreak);

            if (_statusText != null)
            {
                float hpRatio = hpCurrent / hpMax;
                string key = GetStatusLocalizationKey(
                    isInvulnerable: _playerCreature.IsInvulnerable,
                    healthRatio: hpRatio,
                    defeatedRivals: _defeatedRivals,
                    targetDefeats: _targetDefeats);
                _statusText.text = Localization.Get(key);
            }
        }

        private void BuildUI()
        {
            float textScale = SettingsManager.GetUITextScale();
            var canvasObj = new GameObject("CreatureStageHUDCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 170;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.015f, 0.86f);
            panelRect.anchorMax = new Vector2(0.72f, 0.99f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = UIStyleGuide.Colors.HudPanelStrong;
            panelImage.raycastTarget = false;
            var panelOutline = panel.AddComponent<Outline>();
            panelOutline.effectColor = UIStyleGuide.Colors.HudPanelBorder;
            panelOutline.effectDistance = new Vector2(1f, -1f);

            CreateImageBlock(
                panel.transform,
                "AccentBar",
                new Vector2(0f, 0f),
                new Vector2(0.009f, 1f),
                UIStyleGuide.Colors.HudAccentSoft);
            CreateImageBlock(
                panel.transform,
                "HealthTrack",
                new Vector2(0.02f, 0.55f),
                new Vector2(0.98f, 0.62f),
                UIStyleGuide.Colors.HudMeterBackground);
            var healthFillGo = CreateImageBlock(
                panel.transform,
                "HealthFill",
                new Vector2(0.02f, 0.55f),
                new Vector2(0.98f, 0.62f),
                new Color(0.42f, 0.94f, 0.46f, 0.9f));
            _healthFill = healthFillGo.GetComponent<Image>();
            CreateImageBlock(
                panel.transform,
                "ProgressStrip",
                new Vector2(0.02f, 0.31f),
                new Vector2(0.98f, 0.50f),
                new Color(0.11f, 0.09f, 0.04f, 0.42f));
            CreateImageBlock(
                panel.transform,
                "StatusStrip",
                new Vector2(0.02f, 0.15f),
                new Vector2(0.98f, 0.29f),
                new Color(0.04f, 0.13f, 0.09f, 0.42f));

            _statsText = CreateText(
                panel.transform,
                "StatsText",
                new Vector2(0.02f, 0.64f),
                new Vector2(0.98f, 0.97f),
                GetTopLeftFontSize(20, textScale),
                FontStyle.Bold);

            _hintText = CreateText(
                panel.transform,
                "HintText",
                new Vector2(0.02f, 0.03f),
                new Vector2(0.98f, 0.18f),
                GetTopLeftFontSize(15, textScale),
                FontStyle.Normal);
            _hintText.text = Localization.Get("creature.controls.hint");
            _hintText.color = new Color(0.76f, 0.86f, 0.98f, 1f);

            _progressText = CreateText(
                panel.transform,
                "ProgressText",
                new Vector2(0.035f, 0.31f),
                new Vector2(0.965f, 0.50f),
                GetTopLeftFontSize(17, textScale),
                FontStyle.Bold);
            _progressText.color = new Color(1f, 0.86f, 0.52f, 1f);

            _statusText = CreateText(
                panel.transform,
                "StatusText",
                new Vector2(0.035f, 0.15f),
                new Vector2(0.965f, 0.29f),
                GetTopLeftFontSize(15, textScale),
                FontStyle.Italic);
            _statusText.color = new Color(0.8f, 0.94f, 0.86f, 1f);
        }

        private static int GetTopLeftFontSize(int baseSize, float textScale)
        {
            return Mathf.RoundToInt(baseSize * textScale * TopLeftHudSizeBoost);
        }

        private static GameObject CreateImageBlock(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
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
            image.raycastTarget = false;
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            FontStyle fontStyle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = UIStyleGuide.Colors.BodyText;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        /// <summary>
        /// Chooses a short status label key based on current encounter/survival state.
        /// </summary>
        public static string GetStatusLocalizationKey(
            bool isInvulnerable,
            float healthRatio,
            int defeatedRivals,
            int targetDefeats)
        {
            if (isInvulnerable)
                return "creature.status.shielded";
            if (defeatedRivals >= Mathf.Max(1, targetDefeats))
                return "creature.status.objective_done";
            if (healthRatio <= 0.3f)
                return "creature.status.low_health";
            return "creature.status.hunt";
        }
    }
}
