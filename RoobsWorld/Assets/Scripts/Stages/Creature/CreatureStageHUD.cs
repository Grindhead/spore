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
        private Text _feedbackText;
        private Image _healthFill;
        private Image _staminaFill;
        private Image _abilityFill;
        private RoobsWorld.Creature.CreatureInstance _playerCreature;
        private int _activeRivals;
        private int _defeatedRivals;
        private int _targetDefeats;
        private int _desiredActiveRivals;
        private int _killStreak;
        private float _feedbackShowUntil;
        private string _objectiveLabel;
        private string _objectiveProgressOverride;
        private bool _showObjectiveGuidance;
        private CreatureStageObjectiveType _objectiveType;
        private bool _objectiveIsInsideHoldGround;
        private float _objectiveGuidanceDistance = -1f;

        public void Initialize(RoobsWorld.Creature.CreatureInstance playerCreature)
        {
            _playerCreature = playerCreature;
            if (_canvas == null)
                BuildUI();
            SetVisible(true);
            Refresh();
            if (!string.IsNullOrWhiteSpace(_playerCreature?.DNA?.LegacySummary))
                ShowFeedback(_playerCreature.DNA.LegacySummary, UIStyleGuide.Colors.FossilGoldBright, 4.2f);
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

        public void SetObjectiveStatus(string objectiveLabel, string progressText)
        {
            _objectiveLabel = objectiveLabel;
            _objectiveProgressOverride = progressText;
            _showObjectiveGuidance = false;
            _objectiveGuidanceDistance = -1f;
            Refresh();
        }

        public void SetObjectiveStatus(
            string objectiveLabel,
            string progressText,
            CreatureStageObjectiveType objectiveType,
            bool isInsideHoldGround,
            bool showObjectiveGuidance,
            float guidanceDistance = -1f)
        {
            _objectiveLabel = objectiveLabel;
            _objectiveProgressOverride = progressText;
            _objectiveType = objectiveType;
            _objectiveIsInsideHoldGround = isInsideHoldGround;
            _showObjectiveGuidance = showObjectiveGuidance;
            _objectiveGuidanceDistance = guidanceDistance;
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

            if (_feedbackText != null && _feedbackText.enabled && Time.time >= _feedbackShowUntil)
                _feedbackText.enabled = false;

            Refresh();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
            EventBus.Subscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            EventBus.Subscribe<CreatureObjectiveCompletedEvent>(OnObjectiveCompleted);
            EventBus.Subscribe<CreatureAbilityUsedEvent>(OnAbilityUsed);
            EventBus.Subscribe<CreatureStaminaExhaustedEvent>(OnStaminaExhausted);
            EventBus.Subscribe<CreatureStaminaRestoredEvent>(OnStaminaRestored);
            EventBus.Subscribe<CreatureHealingZoneEvent>(OnHealingZone);
            EventBus.Subscribe<CreatureStreakBonusEvent>(OnStreakBonus);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
            EventBus.Unsubscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            EventBus.Unsubscribe<CreatureObjectiveCompletedEvent>(OnObjectiveCompleted);
            EventBus.Unsubscribe<CreatureAbilityUsedEvent>(OnAbilityUsed);
            EventBus.Unsubscribe<CreatureStaminaExhaustedEvent>(OnStaminaExhausted);
            EventBus.Unsubscribe<CreatureStaminaRestoredEvent>(OnStaminaRestored);
            EventBus.Unsubscribe<CreatureHealingZoneEvent>(OnHealingZone);
            EventBus.Unsubscribe<CreatureStreakBonusEvent>(OnStreakBonus);
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
                if (_staminaFill != null)
                    _staminaFill.enabled = false;
                if (_abilityFill != null)
                    _abilityFill.enabled = false;
                if (_progressText != null)
                    _progressText.text = BuildObjectiveProgressText();
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

            string staminaSummary = string.Empty;
            var controller = _playerCreature.GetComponent<CreaturePlayerController>();
            if (controller != null)
            {
                staminaSummary = $"  |  STA: {controller.CurrentStamina:0}/{controller.MaxStamina:0}  |  {CreaturePlayerController.GetAbilityDisplayName(controller.InheritedAbility)}";
            }

            _statsText.text = $"{stageLabel}  |  Evo: {evolutionLevel}  |  {Localization.Get("hud.health")}: {hpCurrent:0}/{hpMax:0}  |  {Localization.Get("hud.dna")}: {dnaPoints}{staminaSummary}";
            if (_hintText != null)
                _hintText.text = BuildHintText(_playerCreature.DNA?.LegacySummary);
            if (_healthFill != null)
            {
                _healthFill.enabled = true;
                var fillRect = _healthFill.rectTransform;
                fillRect.anchorMax = new Vector2(Mathf.Lerp(0.02f, 0.98f, Mathf.Clamp01(hpCurrent / hpMax)), fillRect.anchorMax.y);
            }
            if (_staminaFill != null)
            {
                _staminaFill.enabled = controller != null;
                if (controller != null)
                {
                    var fillRect = _staminaFill.rectTransform;
                    fillRect.anchorMax = new Vector2(Mathf.Lerp(0.02f, 0.98f, controller.NormalizedStamina), fillRect.anchorMax.y);
                }
            }
            if (_abilityFill != null)
            {
                bool hasAbility = controller != null && controller.InheritedAbility != CreatureInheritedAbilityType.None;
                _abilityFill.enabled = hasAbility;
                if (hasAbility)
                {
                    float cooldown = Mathf.Max(0.1f, controller.AbilityCooldownDuration);
                    float ready = 1f - Mathf.Clamp01(controller.AbilityCooldownRemaining / cooldown);
                    var fillRect = _abilityFill.rectTransform;
                    fillRect.anchorMax = new Vector2(Mathf.Lerp(0.02f, 0.98f, ready), fillRect.anchorMax.y);
                }
            }

            if (_progressText != null)
                _progressText.text = BuildObjectiveProgressText();

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
                new Vector2(0.02f, 0.56f),
                new Vector2(0.98f, 0.62f),
                UIStyleGuide.Colors.HudMeterBackground);
            var healthFillGo = CreateImageBlock(
                panel.transform,
                "HealthFill",
                new Vector2(0.02f, 0.56f),
                new Vector2(0.98f, 0.62f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.9f));
            _healthFill = healthFillGo.GetComponent<Image>();
            CreateImageBlock(
                panel.transform,
                "StaminaTrack",
                new Vector2(0.02f, 0.50f),
                new Vector2(0.98f, 0.54f),
                UIStyleGuide.Colors.HudMeterBackground);
            var staminaFillGo = CreateImageBlock(
                panel.transform,
                "StaminaFill",
                new Vector2(0.02f, 0.50f),
                new Vector2(0.98f, 0.54f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.88f));
            _staminaFill = staminaFillGo.GetComponent<Image>();
            CreateImageBlock(
                panel.transform,
                "AbilityTrack",
                new Vector2(0.02f, 0.45f),
                new Vector2(0.98f, 0.48f),
                UIStyleGuide.Colors.HudMeterBackground);
            var abilityFillGo = CreateImageBlock(
                panel.transform,
                "AbilityFill",
                new Vector2(0.02f, 0.45f),
                new Vector2(0.98f, 0.48f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.9f));
            _abilityFill = abilityFillGo.GetComponent<Image>();
            CreateImageBlock(
                panel.transform,
                "ProgressStrip",
                new Vector2(0.02f, 0.27f),
                new Vector2(0.98f, 0.42f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.ShellBrown, 0.42f));
            CreateImageBlock(
                panel.transform,
                "StatusStrip",
                new Vector2(0.02f, 0.12f),
                new Vector2(0.98f, 0.24f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.42f));

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
                new Vector2(0.02f, 0.01f),
                new Vector2(0.98f, 0.11f),
                GetTopLeftFontSize(13, textScale),
                FontStyle.Normal);
            _hintText.text = BuildHintText(null);
            _hintText.color = UIStyleGuide.Colors.BodyTextMuted;

            _progressText = CreateText(
                panel.transform,
                "ProgressText",
                new Vector2(0.035f, 0.27f),
                new Vector2(0.965f, 0.42f),
                GetTopLeftFontSize(17, textScale),
                FontStyle.Bold);
            _progressText.color = UIStyleGuide.Colors.FossilGoldBright;

            _statusText = CreateText(
                panel.transform,
                "StatusText",
                new Vector2(0.035f, 0.12f),
                new Vector2(0.965f, 0.24f),
                GetTopLeftFontSize(15, textScale),
                FontStyle.Italic);
            _statusText.color = UIStyleGuide.Colors.Subtitle;

            _feedbackText = CreateText(
                canvasObj.transform,
                "CombatFeedbackText",
                new Vector2(0.33f, 0.72f),
                new Vector2(0.67f, 0.8f),
                Mathf.RoundToInt(22 * textScale),
                FontStyle.Bold);
            _feedbackText.alignment = TextAnchor.MiddleCenter;
            _feedbackText.color = UIStyleGuide.Colors.FossilGoldBright;
            _feedbackText.enabled = false;
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

        private string BuildObjectiveProgressText()
        {
            if (!string.IsNullOrWhiteSpace(_objectiveProgressOverride))
            {
                string prefix = string.IsNullOrWhiteSpace(_objectiveLabel) ? string.Empty : $"{_objectiveLabel}: ";
                string guidance = _showObjectiveGuidance
                    ? BuildObjectiveGuidanceSuffix(_objectiveType, _objectiveIsInsideHoldGround, _objectiveGuidanceDistance)
                    : string.Empty;
                return prefix + _objectiveProgressOverride + guidance;
            }

            return string.Format(
                Localization.Get("creature.encounter.progress"),
                _defeatedRivals,
                _targetDefeats,
                _activeRivals,
                Mathf.Max(1, _desiredActiveRivals),
                _killStreak);
        }

        private void OnCombatFeedback(CreatureCombatFeedbackEvent evt)
        {
            if (_feedbackText == null)
                return;

            string key = GetFeedbackLocalizationKey(evt.Type);
            ShowFeedback(
                Localization.Get(key),
                evt.Type switch
                {
                    CreatureCombatFeedbackType.PlayerDamaged => UIStyleGuide.Colors.CoralBright,
                    CreatureCombatFeedbackType.LowHealth => UIStyleGuide.Colors.CoralBright,
                    CreatureCombatFeedbackType.Miss => UIStyleGuide.Colors.BodyTextMuted,
                    CreatureCombatFeedbackType.Guard => UIStyleGuide.Colors.TideTealBright,
                    CreatureCombatFeedbackType.Blocked => UIStyleGuide.Colors.TideTealBright,
                    CreatureCombatFeedbackType.Counter => UIStyleGuide.Colors.AlgaeLime,
                    _ => UIStyleGuide.Colors.FossilGoldBright
                },
                0.85f);
        }

        private void OnRivalDefeated(CreatureRivalDefeatedEvent evt)
        {
            if (_feedbackText == null)
                return;

            ShowFeedback(BuildRivalDefeatedLabel(evt.ArchetypeName), UIStyleGuide.Colors.FossilGoldBright, 1.05f);
        }

        private void OnObjectiveCompleted(CreatureObjectiveCompletedEvent evt)
        {
            ShowFeedback(
                BuildObjectiveCompletedLabel(evt.DnaReward, evt.CompletedObjectiveCount, evt.CompletionSeconds, evt.KillStreak),
                UIStyleGuide.Colors.AlgaeLime,
                1.8f);
        }

        private void OnAbilityUsed(CreatureAbilityUsedEvent evt)
        {
            string label = string.IsNullOrWhiteSpace(evt.AbilityLabel) ? Localization.Get("creature.ability.none") : evt.AbilityLabel;
            ShowFeedback(string.Format(Localization.Get("creature.ability.used"), label), UIStyleGuide.Colors.TideTealBright, 0.95f);
        }

        private void OnStaminaExhausted(CreatureStaminaExhaustedEvent evt)
        {
            ShowFeedback(
                BuildStaminaExhaustedLabel(evt.ActionId, evt.CurrentStamina, evt.RequiredStamina),
                UIStyleGuide.Colors.CoralBright,
                0.85f);
        }

        private void OnStaminaRestored(CreatureStaminaRestoredEvent evt)
        {
            ShowFeedback(BuildStaminaRestoredLabel(evt.ReasonId, evt.Amount), UIStyleGuide.Colors.TideTealBright, 0.85f);
        }

        private void OnHealingZone(CreatureHealingZoneEvent evt)
        {
            if (!evt.Started || !evt.IsPlayer)
                return;

            ShowFeedback(Localization.Get("creature.healing_zone"), UIStyleGuide.Colors.AlgaeLime, 1.1f);
        }

        private void OnStreakBonus(CreatureStreakBonusEvent evt)
        {
            ShowFeedback(BuildStreakBonusLabel(evt.StreakCount, evt.DnaBonus), UIStyleGuide.Colors.FossilGoldBright, 1.1f);
        }

        private void ShowFeedback(string message, Color color, float duration)
        {
            if (_feedbackText == null)
                return;

            _feedbackText.text = message;
            _feedbackText.color = color;
            _feedbackText.enabled = true;
            _feedbackShowUntil = Time.time + Mathf.Max(0.1f, duration);
        }

        public static string GetFeedbackLocalizationKey(CreatureCombatFeedbackType type)
        {
            switch (type)
            {
                case CreatureCombatFeedbackType.Miss: return "creature.feedback.miss";
                case CreatureCombatFeedbackType.Kill: return "creature.feedback.kill";
                case CreatureCombatFeedbackType.PlayerDamaged: return "creature.feedback.player_damaged";
                case CreatureCombatFeedbackType.Guard: return "creature.feedback.guard";
                case CreatureCombatFeedbackType.Blocked: return "creature.feedback.blocked";
                case CreatureCombatFeedbackType.Counter: return "creature.feedback.counter";
                case CreatureCombatFeedbackType.Telegraph: return "creature.feedback.telegraph";
                case CreatureCombatFeedbackType.LowHealth: return "creature.feedback.low_health";
                default: return "creature.feedback.hit";
            }
        }

        public static string BuildRivalDefeatedLabel(string archetypeName)
        {
            if (string.IsNullOrWhiteSpace(archetypeName))
                return "Rival down";

            string label = archetypeName.Trim();
            return $"{label} rival down";
        }

        public static string BuildObjectiveCompletedLabel(int dnaReward)
        {
            return string.Format(
                Localization.Get("creature.objective.completed_reward"),
                Mathf.Max(0, dnaReward));
        }

        public static string BuildObjectiveCompletedLabel(int dnaReward, int completedObjectiveCount, float completionSeconds, int killStreak)
        {
            if (completedObjectiveCount <= 0 || completionSeconds <= 0f)
                return BuildObjectiveCompletedLabel(dnaReward);

            return string.Format(
                Localization.Get("creature.objective.completed_reward_stats"),
                Mathf.Max(1, completedObjectiveCount),
                Mathf.Max(0, dnaReward),
                Mathf.Max(0f, completionSeconds),
                Mathf.Max(0, killStreak));
        }

        public static string BuildObjectiveGuidanceSuffix(
            CreatureStageObjectiveType objectiveType,
            bool isInsideHoldGround,
            float guidanceDistance = -1f)
        {
            string key = objectiveType switch
            {
                CreatureStageObjectiveType.AlphaHunt => "creature.objective.guidance.alpha_hunt",
                CreatureStageObjectiveType.HoldFeedingGround => isInsideHoldGround
                    ? "creature.objective.guidance.hold_ground.secure"
                    : "creature.objective.guidance.hold_ground.return",
                _ => "creature.objective.guidance.rival_cull"
            };

            bool showDistance = objectiveType != CreatureStageObjectiveType.HoldFeedingGround || !isInsideHoldGround;
            string distance = showDistance ? BuildGuidanceDistanceLabel(guidanceDistance) : string.Empty;
            return $"   {Localization.Get(key)}{distance}";
        }

        public static string BuildGuidanceDistanceLabel(float distance)
        {
            if (distance < 0f || float.IsNaN(distance) || float.IsInfinity(distance))
                return string.Empty;

            return $" ({Mathf.CeilToInt(Mathf.Max(0f, distance))}m)";
        }

        public static string BuildHintText(string legacySummary)
        {
            string hint = Localization.Get("creature.controls.hint") + "  Ability: Interact   Guard: Jump";
            if (string.IsNullOrWhiteSpace(legacySummary))
                return hint;

            return $"{hint}\n{legacySummary.Trim()}";
        }

        public static string BuildStaminaExhaustedLabel(string actionId, float currentStamina, float requiredStamina)
        {
            return string.Format(
                Localization.Get("creature.stamina.exhausted.action"),
                GetStaminaActionLabel(actionId),
                Mathf.Max(0f, currentStamina),
                Mathf.Max(0f, requiredStamina));
        }

        public static string GetStaminaActionLabel(string actionId)
        {
            return actionId?.ToLowerInvariant() switch
            {
                "attack" => "attack",
                "guard" => "guard",
                "ability" => "use ability",
                _ => "act"
            };
        }

        public static string BuildStaminaRestoredLabel(string reasonId, float amount)
        {
            string key = reasonId?.ToLowerInvariant() switch
            {
                "counter" => "creature.stamina.restored.counter",
                "objective" => "creature.stamina.restored.objective",
                _ => "creature.stamina.restored.kill"
            };
            return string.Format(Localization.Get(key), Mathf.Max(0f, amount));
        }

        public static string BuildStreakBonusLabel(int streakCount, int dnaBonus)
        {
            return string.Format(
                Localization.Get("creature.streak.bonus"),
                Mathf.Max(1, streakCount),
                Mathf.Max(0, dnaBonus));
        }
    }
}
