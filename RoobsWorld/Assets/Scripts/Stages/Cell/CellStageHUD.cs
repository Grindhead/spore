using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// HUD for the Cell stage showing mass, DNA points, and unlocked cell parts.
    /// </summary>
    public class CellStageHUD : UIScreen
    {
        public const string ID = "CellStageHUD";
        public override string ScreenId => ID;

        private const float GoalCompleteDisplayDuration = 2.5f;
        private const float StageLookupInterval = 0.75f;
        private const float TopHudSizeBoost = 1.15f;
        private const float LeftHudSizeBoost = 1.12f;

        private PlayerCell _player;
        private Canvas _canvas;
        private RectTransform _safeAreaRoot;

        private Text _massText;
        private Image _massBar;
        private Text _dnaText;
        private Text _partsText;
        private Text _symbiosisText;
        private Text _goalText;
        private Text _goalCompleteText;
        private Text _currentIndicatorText;
        private Text _envEventText;
        private Text _leviathanWarningText;
        private float _leviathanWarningShowUntil;
        private Text _infectionText;
        private float _infectionShowUntil;
        private Text _threatWarningText;
        private GameObject _threatWarningPanel;
        private Text _biomeText;
        private Text _controlsText;
        private readonly List<Vector3> _bloomHotspotBuffer = new List<Vector3>(8);
        private readonly Color _bloomPulseColorA = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.95f);
        private readonly Color _bloomPulseColorB = UIStyleGuide.Colors.FossilGoldBright;
        private Biome.BiomeType _lastBiome = (Biome.BiomeType)(-1); // force first update
        private CellStage _cellStage;
        private float _nextStageLookupTime;
        private ThreatIndicator _threatIndicator;
        private float _goalCompleteShowUntil;

        public static CellStageHUD CreateRuntime(PlayerCell player, CellStage cellStage = null, ThreatIndicator threatIndicator = null)
        {
            var go = new GameObject("CellStageHUD");
            var hud = go.AddComponent<CellStageHUD>();
            hud._player = player;
            hud._cellStage = cellStage;
            hud._threatIndicator = threatIndicator;
            hud.BuildUI();
            UIManager.Instance.RegisterScreen(hud);
            UIManager.Instance.ShowScreen(ID);
            return hud;
        }

        private void BuildUI()
        {
            float scale = SettingsManager.GetUITextScale();
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            scaler.matchWidthOrHeight = GetCanvasMatch();
            gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("SafeAreaRoot");
            rootGo.transform.SetParent(transform, false);
            _safeAreaRoot = rootGo.AddComponent<RectTransform>();
            _safeAreaRoot.anchorMin = Vector2.zero;
            _safeAreaRoot.anchorMax = Vector2.one;
            _safeAreaRoot.offsetMin = Vector2.zero;
            _safeAreaRoot.offsetMax = Vector2.zero;
            SafeAreaHelper.Apply(_safeAreaRoot);

            BuildHudChrome();
            BuildMassDisplay(scale);
            BuildDNADisplay(scale);
            BuildPartsDisplay(scale);
            BuildSymbiosisDisplay(scale);
            BuildGoalDisplay(scale);
            BuildGoalCompleteDisplay(scale);
            BuildCurrentIndicator(scale);
            BuildThreatWarningDisplay(scale);
            BuildEnvEventDisplay(scale);
            BuildLeviathanWarningDisplay(scale);
            BuildInfectionDisplay(scale);
            BuildBiomeDisplay(scale);
            BuildControlsHint(scale);
        }

        private void BuildHudChrome()
        {
            CreateHudPanel(
                "VitalsPanel",
                _safeAreaRoot,
                new Vector2(0.012f, 0.705f),
                new Vector2(0.53f, 0.972f),
                UIStyleGuide.Colors.HudPanelStrong);
            CreateHudAccent(
                "VitalsAccent",
                _safeAreaRoot,
                new Vector2(0.014f, 0.705f),
                new Vector2(0.018f, 0.972f),
                UIStyleGuide.Colors.HudAccentSoft);
            CreateHudPanel(
                "SituationalPanel",
                _safeAreaRoot,
                new Vector2(0.755f, 0.812f),
                new Vector2(0.988f, 0.972f),
                UIStyleGuide.Colors.HudPanel);
            CreateHudPanel(
                "ContextPanel",
                _safeAreaRoot,
                new Vector2(0.008f, 0.008f),
                new Vector2(0.992f, 0.085f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.HudPanel, 0.52f));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CellGoalCompletedEvent>(OnGoalCompleted);
            EventBus.Subscribe<CellDietVarietyBonusEvent>(OnDietVarietyBonus);
            EventBus.Subscribe<CellEnvironmentEventTriggered>(OnEnvironmentEvent);
            EventBus.Subscribe<CellLeviathanSpawnedEvent>(OnLeviathanSpawned);
            EventBus.Subscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
            EventBus.Subscribe<CellPlayerInfectedEvent>(OnPlayerInfected);
            EventBus.Subscribe<CellPlayerEscapedPredatorEvent>(OnPlayerEscapedPredator);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CellGoalCompletedEvent>(OnGoalCompleted);
            EventBus.Unsubscribe<CellDietVarietyBonusEvent>(OnDietVarietyBonus);
            EventBus.Unsubscribe<CellEnvironmentEventTriggered>(OnEnvironmentEvent);
            EventBus.Unsubscribe<CellLeviathanSpawnedEvent>(OnLeviathanSpawned);
            EventBus.Unsubscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
            EventBus.Unsubscribe<CellPlayerInfectedEvent>(OnPlayerInfected);
            EventBus.Unsubscribe<CellPlayerEscapedPredatorEvent>(OnPlayerEscapedPredator);
        }

        private void UpdateThreatWarning()
        {
            if (_threatWarningText == null || _threatIndicator == null)
                return;

            if (_threatIndicator.IsThreatDetected)
            {
                string direction = GetCompassLabel(_threatIndicator.PrimaryThreatDirection);
                string distanceLabel = float.IsInfinity(_threatIndicator.NearestThreatDistance)
                    ? string.Empty
                    : $" {_threatIndicator.NearestThreatDistance:F0}m";
                string threatLabel = ThreatIndicator.GetThreatCueLabel(_threatIndicator.PrimaryThreatType);
                _threatWarningText.text = $"{threatLabel} nearby {direction}{distanceLabel}";
                float t = Mathf.Clamp01(_threatIndicator.ThreatIntensity);
                _threatWarningText.color = Color.Lerp(
                    UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGold, 0.95f),
                    UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.CoralBright, 0.98f),
                    t);
                _threatWarningText.enabled = true;
                if (_threatWarningPanel != null)
                    _threatWarningPanel.SetActive(true);
            }
            else
            {
                _threatWarningText.enabled = false;
                if (_threatWarningPanel != null)
                    _threatWarningPanel.SetActive(false);
            }
        }

        private void UpdateTransientTexts()
        {
            if (_leviathanWarningText != null && _leviathanWarningText.enabled && Time.time > _leviathanWarningShowUntil)
                _leviathanWarningText.enabled = false;

            if (_infectionText != null && _infectionText.enabled && Time.time > _infectionShowUntil)
                _infectionText.enabled = false;
        }

        private void OnEnvironmentEvent(CellEnvironmentEventTriggered evt)
        {
            if (_envEventText == null) return;
            if (!evt.Started)
            {
                _envEventText.enabled = false;
                return;
            }
            switch (evt.EventType)
            {
                case CellEnvironmentEventType.NutrientBloom:
                    _envEventText.text = Localization.Get("cell.env.nutrient_bloom");
                    _envEventText.color = UIStyleGuide.Colors.AlgaeLime;
                    break;
                case CellEnvironmentEventType.VirusOutbreak:
                    _envEventText.text = Localization.Get("cell.env.virus_outbreak");
                    _envEventText.color = UIStyleGuide.Colors.CoralBright;
                    break;
                case CellEnvironmentEventType.TemperatureChange:
                    _envEventText.text = Localization.Get("cell.env.temperature_change");
                    _envEventText.color = UIStyleGuide.Colors.TideTealBright;
                    break;
                case CellEnvironmentEventType.RadiationSurge:
                    _envEventText.text = Localization.Get("cell.env.radiation_surge");
                    _envEventText.color = UIStyleGuide.Colors.FossilGoldBright;
                    break;
                case CellEnvironmentEventType.OxygenSurge:
                    _envEventText.text = Localization.Get("cell.env.oxygen_surge");
                    _envEventText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.86f);
                    break;
            }
            _envEventText.enabled = true;
        }

        private void OnPlayerInfected(CellPlayerInfectedEvent evt)
        {
            if (_infectionText == null) return;
            _infectionText.text = Localization.Get("cell.virus.infected");
            _infectionText.enabled = true;
            _infectionShowUntil = Time.time + 1.8f;
        }

        private void OnPlayerEscapedPredator(CellPlayerEscapedPredatorEvent evt)
        {
            if (evt.BoostSeconds <= 0f)
                return;

            ShowGoalRewardToast(
                BuildPredatorEscapeSurgeText(evt.BoostSeconds),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.95f),
                1.4f);
        }

        private void OnLeviathanSpawned(CellLeviathanSpawnedEvent evt)
        {
            if (_leviathanWarningText == null) return;
            _leviathanWarningText.text = Localization.Get("cell.leviathan.warning");
            _leviathanWarningText.enabled = true;
            _leviathanWarningShowUntil = Time.time + 5f;
            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.4f, 0.3f);
        }

        private void OnLeviathanDefeated(CellLeviathanDefeatedEvent evt)
        {
            if (_leviathanWarningText == null) return;
            _leviathanWarningText.text = Localization.Get("cell.leviathan.defeated");
            _leviathanWarningText.color = UIStyleGuide.Colors.FossilGoldBright;
            _leviathanWarningText.enabled = true;
            _leviathanWarningShowUntil = Time.time + 3f;
        }

        private void UpdateBiomeLabel()
        {
            if (_biomeText == null || _cellStage == null || _player == null) return;
            var biome = _cellStage.GetBiomeAt(_player.transform.position);
            if (biome == _lastBiome) return;
            _lastBiome = biome;
            var profile = Biome.GetProfile(biome);
            _biomeText.text = $"{Localization.Get("cell.biome." + biome.ToString().ToLower())}: {Localization.Get(profile.VisitReasonKey)}";
            // Tint the label to match biome colour
            _biomeText.color = biome switch
            {
                Biome.BiomeType.AlgaeForest => UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.75f),
                Biome.BiomeType.DeepZone    => UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.75f),
                Biome.BiomeType.ToxicZone   => UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Amethyst, 0.85f),
                _                           => UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Subtitle, 0.75f),
            };
        }

        private void OnGoalCompleted(CellGoalCompletedEvent evt)
        {
            ShowGoalRewardToast(
                BuildGoalCompletedText(evt),
                UIStyleGuide.Colors.WithAlpha(
                    evt.RewardMultiplier >= BalanceConstants.CellRepeatableRiskGoalRewardMultiplier
                        ? UIStyleGuide.Colors.AlgaeLime
                        : UIStyleGuide.Colors.FossilGoldBright,
                    0.95f),
                GoalCompleteDisplayDuration);
        }

        private void OnDietVarietyBonus(CellDietVarietyBonusEvent evt)
        {
            ShowGoalRewardToast(
                BuildDietVarietyBonusText(evt.ChainLength, evt.DnaBonus),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.95f),
                1.8f);
        }

        public static string BuildGoalCompletedText(CellGoalCompletedEvent evt)
        {
            int dna = evt.DnaReward > 0 ? evt.DnaReward : BalanceConstants.CellDNAPointsPerGoalComplete;
            string key = evt.RewardMultiplier >= BalanceConstants.CellRepeatableRiskGoalRewardMultiplier
                ? "cell.goal.risk_complete_message"
                : "cell.goal.complete_message";
            string text = string.Format(Localization.Get(key), dna);
            if (evt.VarietyBonusDna > 0)
                text += $"  |  Variety x{Mathf.Max(1, evt.VarietyChain)} +{Mathf.Max(0, evt.VarietyBonusDna)} DNA";

            return text;
        }

        private void ShowGoalRewardToast(string message, Color color, float duration)
        {
            _goalCompleteShowUntil = Time.time + Mathf.Max(0.1f, duration);
            if (_goalCompleteText != null)
            {
                _goalCompleteText.text = message;
                _goalCompleteText.color = color;
                _goalCompleteText.enabled = true;
            }
        }

        public static string BuildDietVarietyBonusText(int chainLength, int dnaBonus)
        {
            return string.Format(
                Localization.Get("cell.diet.variety_bonus"),
                Mathf.Max(1, chainLength),
                Mathf.Max(0, dnaBonus));
        }

        public static string BuildPredatorEscapeSurgeText(float boostSeconds)
        {
            return string.Format(
                Localization.Get("cell.predator_escape_surge"),
                Mathf.Max(0f, boostSeconds));
        }

        public static string BuildGoalRewardSuffix(CellGoal goal, int baseReward, float rewardMultiplier = 1f)
        {
            int reward = CellStage.ComputeGoalDnaReward(goal, baseReward, rewardMultiplier);
            string key = goal.RewardMultiplier >= BalanceConstants.CellRepeatableRiskGoalRewardMultiplier
                ? "cell.goal.risk_reward_suffix"
                : "cell.goal.reward_suffix";
            return "  |  " + string.Format(Localization.Get(key), reward);
        }

        public static string BuildGoalGuidanceSuffix(CellGoal goal)
        {
            switch (goal.Type)
            {
                case CellGoal.GoalType.EatFood:
                    return goal.TargetFoodType == FoodType.Toxin
                        ? "  |  Toxic trail"
                        : "  |  Nearby food";
                case CellGoal.GoalType.EscapePredator:
                    return "  |  Break away";
                case CellGoal.GoalType.ReproduceOnce:
                    return "  |  Grow to upgrade";
                case CellGoal.GoalType.SurviveInToxicZone:
                    return "  |  Hold toxic zone";
                case CellGoal.GoalType.EatDuringNutrientBloom:
                    return "  |  Bloom food";
                case CellGoal.GoalType.EatLargerPrey:
                    return "  |  Orange prey";
                case CellGoal.GoalType.CollectParts:
                    return "  |  Nearby part";
                default:
                    return string.Empty;
            }
        }

        private void BuildGoalDisplay(float textScale)
        {
            var goalGo = CreateUIElement("GoalDisplay", _safeAreaRoot,
                new Vector2(0.02f, 0.72f), new Vector2(0.4f, 0.77f), emphasizeTop: true, emphasizeLeft: true);
            _goalText = goalGo.AddComponent<Text>();
            _goalText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _goalText.fontSize = GetHudFontSize(14, textScale, emphasizeTop: true, emphasizeLeft: true);
            _goalText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.BodyText, 0.95f);
            _goalText.alignment = TextAnchor.MiddleLeft;
        }

        private void BuildGoalCompleteDisplay(float textScale)
        {
            var go = CreateUIElement("GoalCompleteDisplay", _safeAreaRoot,
                new Vector2(0.02f, 0.68f), new Vector2(0.45f, 0.72f), emphasizeTop: true, emphasizeLeft: true);
            _goalCompleteText = go.AddComponent<Text>();
            _goalCompleteText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _goalCompleteText.fontSize = GetHudFontSize(16, textScale, emphasizeTop: true, emphasizeLeft: true);
            _goalCompleteText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.95f);
            _goalCompleteText.alignment = TextAnchor.MiddleLeft;
            _goalCompleteText.enabled = false;
        }

        private void BuildCurrentIndicator(float textScale)
        {
            var go = CreateUIElement("CurrentIndicator", _safeAreaRoot,
                new Vector2(0.78f, 0.83f), new Vector2(0.98f, 0.88f), emphasizeTop: true);
            _currentIndicatorText = go.AddComponent<Text>();
            _currentIndicatorText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _currentIndicatorText.fontSize = GetHudFontSize(12, textScale, emphasizeTop: true);
            _currentIndicatorText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.9f);
            _currentIndicatorText.alignment = TextAnchor.MiddleRight;
            _currentIndicatorText.enabled = false;
        }

        private static string GetFoodTypeGoalName(FoodType foodType)
        {
            switch (foodType)
            {
                case FoodType.Plant: return Localization.Get("cell.goal.food_plant");
                case FoodType.Meat: return Localization.Get("cell.goal.food_meat");
                case FoodType.Toxin: return Localization.Get("cell.goal.food_toxin");
                default: return Localization.Get("cell.goal.food_plant");
            }
        }

        private static string GetCompassLabel(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return "N";

            direction.Normalize();
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            if (angle < 22.5f || angle >= 337.5f) return "N";
            if (angle < 67.5f) return "NE";
            if (angle < 112.5f) return "E";
            if (angle < 157.5f) return "SE";
            if (angle < 202.5f) return "S";
            if (angle < 247.5f) return "SW";
            if (angle < 292.5f) return "W";
            return "NW";
        }

        private static float GetCanvasMatch()
        {
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0 || h <= 0) return 0.5f;
            float aspect = w / h;
            if (aspect > 2f) return 0f;
            if (aspect < 0.5f) return 1f;
            return 0.5f;
        }

        public static bool ShouldLookupCellStage(CellStage cellStage, float now, float nextLookupTime)
        {
            return cellStage == null && now >= nextLookupTime;
        }

        private CellStage ResolveStageReference(float now)
        {
            if (ShouldLookupCellStage(_cellStage, now, _nextStageLookupTime))
            {
                _cellStage = FindAnyObjectByType<CellStage>();
                _nextStageLookupTime = now + StageLookupInterval;
            }

            return _cellStage;
        }

        private void BuildThreatWarningDisplay(float textScale)
        {
            _threatWarningPanel = CreateHudPanel(
                "ThreatWarningPanel",
                _safeAreaRoot,
                new Vector2(0.29f, 0.895f),
                new Vector2(0.71f, 0.967f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Coral, 0.48f));
            _threatWarningPanel.SetActive(false);
            var go = CreateUIElement("ThreatWarning", _safeAreaRoot,
                new Vector2(0.3f, 0.90f), new Vector2(0.7f, 0.96f), emphasizeTop: true);
            _threatWarningText = go.AddComponent<Text>();
            _threatWarningText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _threatWarningText.fontSize = GetHudFontSize(18, textScale, emphasizeTop: true);
            _threatWarningText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.CoralBright, 0.95f);
            _threatWarningText.alignment = TextAnchor.MiddleCenter;
            _threatWarningText.fontStyle = FontStyle.Bold;
            _threatWarningText.enabled = false;
        }

        private void BuildEnvEventDisplay(float textScale)
        {
            var go = CreateUIElement("EnvEventDisplay", _safeAreaRoot,
                new Vector2(0.25f, 0.93f), new Vector2(0.75f, 0.98f), emphasizeTop: true);
            _envEventText = go.AddComponent<Text>();
            _envEventText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _envEventText.fontSize = GetHudFontSize(18, textScale, emphasizeTop: true);
            _envEventText.color = UIStyleGuide.Colors.AlgaeLime;
            _envEventText.alignment = TextAnchor.MiddleCenter;
            _envEventText.fontStyle = FontStyle.Bold;
            _envEventText.enabled = false;
        }

        private void BuildInfectionDisplay(float textScale)
        {
            var go = CreateUIElement("InfectionWarning", _safeAreaRoot,
                new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.62f));
            _infectionText = go.AddComponent<Text>();
            _infectionText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _infectionText.fontSize = Mathf.RoundToInt(18 * textScale);
            _infectionText.color = UIStyleGuide.Colors.Amethyst;
            _infectionText.alignment = TextAnchor.MiddleCenter;
            _infectionText.fontStyle = FontStyle.Bold;
            _infectionText.enabled = false;
        }

        private void BuildBiomeDisplay(float textScale)
        {
            // Bottom-left corner — subtle biome zone label
            var go = CreateUIElement("BiomeLabel", _safeAreaRoot,
                new Vector2(0.01f, 0.01f), new Vector2(0.35f, 0.07f));
            _biomeText = go.AddComponent<Text>();
            _biomeText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _biomeText.fontSize = Mathf.RoundToInt(12 * textScale);
            _biomeText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Subtitle, 0.75f);
            _biomeText.alignment = TextAnchor.MiddleLeft;
            _biomeText.fontStyle = FontStyle.Italic;
        }

        private void BuildControlsHint(float textScale)
        {
            var go = CreateUIElement("ControlsHint", _safeAreaRoot,
                new Vector2(0.62f, 0.01f), new Vector2(0.99f, 0.08f));
            _controlsText = go.AddComponent<Text>();
            _controlsText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _controlsText.fontSize = Mathf.RoundToInt(11 * textScale);
            _controlsText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.BodyTextMuted, 0.7f);
            _controlsText.alignment = TextAnchor.LowerRight;
            _controlsText.text = "Move: Hold LMB and steer cursor  Boost: Shift/Space/RMB  Speed: ]/Interact";
        }

        private void BuildLeviathanWarningDisplay(float textScale)
        {
            var go = CreateUIElement("LeviathanWarning", _safeAreaRoot,
                new Vector2(0.2f, 0.86f), new Vector2(0.8f, 0.92f), emphasizeTop: true);
            _leviathanWarningText = go.AddComponent<Text>();
            _leviathanWarningText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _leviathanWarningText.fontSize = GetHudFontSize(22, textScale, emphasizeTop: true);
            _leviathanWarningText.color = UIStyleGuide.Colors.CoralBright;
            _leviathanWarningText.alignment = TextAnchor.MiddleCenter;
            _leviathanWarningText.fontStyle = FontStyle.Bold;
            _leviathanWarningText.enabled = false;
        }

        private void BuildMassDisplay(float textScale)
        {
            var barBg = CreateUIElement("MassBarBG", _safeAreaRoot,
                new Vector2(0.02f, 0.92f), new Vector2(0.25f, 0.96f), emphasizeTop: true, emphasizeLeft: true);
            var bgImage = barBg.AddComponent<Image>();
            bgImage.color = UIStyleGuide.Colors.HudMeterBackground;
            bgImage.raycastTarget = false;
            var bgOutline = barBg.AddComponent<Outline>();
            bgOutline.effectColor = UIStyleGuide.Colors.HudPanelBorder;
            bgOutline.effectDistance = new Vector2(1f, -1f);

            var barFill = CreateUIElement("MassBarFill", barBg.transform,
                Vector2.zero, Vector2.one);
            _massBar = barFill.AddComponent<Image>();
            _massBar.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.AlgaeLime, 0.92f);
            _massBar.raycastTarget = false;

            var labelGo = CreateUIElement("MassLabel", _safeAreaRoot,
                new Vector2(0.02f, 0.88f), new Vector2(0.25f, 0.92f), emphasizeTop: true, emphasizeLeft: true);
            _massText = labelGo.AddComponent<Text>();
            _massText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _massText.fontSize = GetHudFontSize(16, textScale, emphasizeTop: true, emphasizeLeft: true);
            _massText.color = UIStyleGuide.Colors.BodyText;
            _massText.alignment = TextAnchor.MiddleLeft;
            _massText.fontStyle = FontStyle.Bold;
        }

        private void BuildDNADisplay(float textScale)
        {
            var dnaGo = CreateUIElement("DNADisplay", _safeAreaRoot,
                new Vector2(0.02f, 0.83f), new Vector2(0.2f, 0.88f), emphasizeTop: true, emphasizeLeft: true);
            _dnaText = dnaGo.AddComponent<Text>();
            _dnaText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _dnaText.fontSize = GetHudFontSize(18, textScale, emphasizeTop: true, emphasizeLeft: true);
            _dnaText.color = UIStyleGuide.Colors.UnlockedGold;
            _dnaText.alignment = TextAnchor.MiddleLeft;
            _dnaText.fontStyle = FontStyle.Bold;
        }

        private void BuildPartsDisplay(float textScale)
        {
            var partsGo = CreateUIElement("PartsDisplay", _safeAreaRoot,
                new Vector2(0.02f, 0.78f), new Vector2(0.3f, 0.83f), emphasizeTop: true, emphasizeLeft: true);
            _partsText = partsGo.AddComponent<Text>();
            _partsText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _partsText.fontSize = GetHudFontSize(14, textScale, emphasizeTop: true, emphasizeLeft: true);
            _partsText.color = UIStyleGuide.Colors.Subtitle;
            _partsText.alignment = TextAnchor.MiddleLeft;
        }

        private void BuildSymbiosisDisplay(float textScale)
        {
            var go = CreateUIElement("SymbiosisDisplay", _safeAreaRoot,
                new Vector2(0.02f, 0.74f), new Vector2(0.5f, 0.78f), emphasizeTop: true, emphasizeLeft: true);
            _symbiosisText = go.AddComponent<Text>();
            _symbiosisText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _symbiosisText.fontSize = GetHudFontSize(13, textScale, emphasizeTop: true, emphasizeLeft: true);
            _symbiosisText.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.95f);
            _symbiosisText.alignment = TextAnchor.MiddleLeft;
            _symbiosisText.enabled = false;
        }

        private static GameObject CreateUIElement(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool emphasizeTop = false,
            bool emphasizeLeft = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            if (emphasizeTop || emphasizeLeft)
            {
                float expandXMin = emphasizeLeft ? 0.008f : 0f;
                float expandXMax = emphasizeLeft ? 0.025f : 0.01f;
                float expandY = emphasizeTop ? 0.012f : 0.006f;
                anchorMin.x = Mathf.Max(0f, anchorMin.x - expandXMin);
                anchorMax.x = Mathf.Min(1f, anchorMax.x + expandXMax);
                anchorMin.y = Mathf.Max(0f, anchorMin.y - expandY);
                anchorMax.y = Mathf.Min(1f, anchorMax.y + expandY);
            }
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateHudPanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var panel = CreateUIElement(name, parent, anchorMin, anchorMax);
            var image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = UIStyleGuide.Colors.HudPanelBorder;
            outline.effectDistance = new Vector2(1f, -1f);
            return panel;
        }

        private static GameObject CreateHudAccent(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var accent = CreateUIElement(name, parent, anchorMin, anchorMax);
            var image = accent.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return accent;
        }

        private static int GetHudFontSize(int baseSize, float textScale, bool emphasizeTop = false, bool emphasizeLeft = false)
        {
            float boost = 1f;
            if (emphasizeTop)
                boost *= TopHudSizeBoost;
            if (emphasizeLeft)
                boost *= LeftHudSizeBoost;
            return Mathf.RoundToInt(baseSize * textScale * boost);
        }

        private void Update()
        {
            if (_goalCompleteText != null && _goalCompleteText.enabled && Time.time >= _goalCompleteShowUntil)
            {
                _goalCompleteText.enabled = false;
            }

            if (_leviathanWarningText != null && _leviathanWarningText.enabled && Time.time >= _leviathanWarningShowUntil)
            {
                _leviathanWarningText.enabled = false;
                _leviathanWarningText.color = UIStyleGuide.Colors.CoralBright;
            }

            if (_infectionText != null && _infectionText.enabled && Time.time >= _infectionShowUntil)
            {
                _infectionText.enabled = false;
            }

            UpdateThreatWarning();
            UpdateBiomeLabel();
            UpdateTransientTexts();

            float now = Time.time;
            var stage = ResolveStageReference(now);
            UpdateBloomEventDisplay(stage, now);
            UpdateControlsHint(stage);
            if (_currentIndicatorText != null && stage != null)
            {
                if (stage.TryGetCurrentAtPlayer(out Vector3 dir, out float force))
                {
                    _currentIndicatorText.text = $"{Localization.Get("cell.current.indicator")} {GetCompassLabel(dir)} {force:F1}";
                    _currentIndicatorText.enabled = true;
                }
                else
                {
                    _currentIndicatorText.enabled = false;
                }
            }

            if (_player == null || _player.IsDead) return;

            _massText.text = $"Mass: {_player.CurrentMass:F1}";
            _massBar.rectTransform.anchorMax = new Vector2(_player.NormalizedMass, 1f);
            _dnaText.text = $"DNA: {_player.DNAPoints}";

            var loadout = _player.PartLoadout;
            string parts = "Parts:";
            if (loadout.HasPart(CellPartType.Flagella)) parts += " [Flagella]";
            if (loadout.HasPart(CellPartType.Cilia)) parts += " [Cilia]";
            if (loadout.HasPart(CellPartType.Spike)) parts += " [Spike]";
            if (loadout.HasPart(CellPartType.Jet)) parts += " [Jet]";
            _partsText.text = parts + BuildNearestPartPreviewText();

            if (_symbiosisText != null)
            {
                int p = _player.GetSymbiosisLevel(GeneticTraits.Photosynthetic);
                int t = _player.GetSymbiosisLevel(GeneticTraits.Toxic);
                int a = _player.GetSymbiosisLevel(GeneticTraits.Armoured);
                int r = _player.GetSymbiosisLevel(GeneticTraits.Regenerative);
                int e = _player.GetSymbiosisLevel(GeneticTraits.Electric);
                if (p + t + a + r + e > 0)
                {
                    string levels = "";
                    if (p > 0) levels += $" P{p}";
                    if (t > 0) levels += $" T{t}";
                    if (a > 0) levels += $" A{a}";
                    if (r > 0) levels += $" R{r}";
                    if (e > 0) levels += $" E{e}";
                    _symbiosisText.text = Localization.Get("cell.symbiosis.label") + ":" + levels;
                    _symbiosisText.enabled = true;
                }
                else
                {
                    _symbiosisText.enabled = false;
                }
            }

            if (_goalText != null)
            {
                if (stage != null && stage.CurrentGoal.HasValue)
                {
                    var g = stage.CurrentGoal.Value;
                    string task;
                    string progress;
                    if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.EscapePredator)
                    {
                        task = string.Format(Localization.Get("cell.goal.escape_predator_task"), g.TargetCount);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    else if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.ReproduceOnce)
                    {
                        task = string.Format(Localization.Get("cell.goal.reproduce_task"), g.TargetCount);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    else if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.SurviveInToxicZone)
                    {
                        task = string.Format(Localization.Get("cell.goal.survive_toxic_task"), g.TargetSeconds);
                        progress = string.Format(Localization.Get("cell.goal.survive_toxic_progress"), g.ElapsedSeconds, g.TargetSeconds);
                    }
                    else if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.EatDuringNutrientBloom)
                    {
                        task = string.Format(Localization.Get("cell.goal.bloom_eat_task"), g.TargetCount);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    else if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.EatLargerPrey)
                    {
                        task = string.Format(Localization.Get("cell.goal.eat_larger_task"), g.TargetCount);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    else if (g.Type == RoobsWorld.Stages.Cell.CellGoal.GoalType.CollectParts)
                    {
                        task = string.Format(Localization.Get("cell.goal.collect_parts_task"), g.TargetCount);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    else
                    {
                        string foodName = GetFoodTypeGoalName(g.TargetFoodType);
                        task = string.Format(Localization.Get("cell.goal.task"), g.TargetCount, foodName);
                        progress = string.Format(Localization.Get("cell.goal.progress"), g.CurrentCount, g.TargetCount);
                    }
                    float rewardMultiplier = GameManager.HasInstance
                        ? GameManager.Instance.Modifiers.DNARewardMultiplier
                        : 1f;
                    string upgradeProgress = string.Format(
                        Localization.Get("cell.upgrade.progress"),
                        _player.FoodConsumedSinceUpgrade,
                        _player.FoodRequiredPerUpgrade);
                    string upgradeFeedback = _player.IsUpgradeReady
                        ? Localization.Get("cell.upgrade.ready")
                        : string.Format(Localization.Get("cell.upgrade.remaining"), _player.FoodRemainingForUpgrade);
                    _goalText.text = "Goal: " + task + progress + BuildGoalGuidanceSuffix(g) + BuildGoalRewardSuffix(
                        g,
                        BalanceConstants.CellDNAPointsPerGoalComplete,
                        rewardMultiplier) + "  |  " + upgradeProgress + "  |  " + upgradeFeedback;
                    _goalText.enabled = true;
                }
                else
                {
                    string upgradeProgress = string.Format(
                        Localization.Get("cell.upgrade.progress"),
                        _player.FoodConsumedSinceUpgrade,
                        _player.FoodRequiredPerUpgrade);
                    string upgradeFeedback = _player.IsUpgradeReady
                        ? Localization.Get("cell.upgrade.ready")
                        : string.Format(Localization.Get("cell.upgrade.remaining"), _player.FoodRemainingForUpgrade);
                    _goalText.text = $"{upgradeProgress}  |  {upgradeFeedback}";
                    _goalText.enabled = true;
                }
            }
        }

        private void UpdateControlsHint(CellStage stage)
        {
            if (_controlsText == null)
                return;

            float speed = stage != null ? stage.CurrentDocumentaryTimeScale : 1f;
            _controlsText.text = $"Move: Hold LMB and steer cursor  Boost: Shift/Space/RMB  Speed: {speed:0.#}x ]/Interact";
        }

        private void UpdateBloomEventDisplay(CellStage stage, float now)
        {
            if (_envEventText == null || stage == null || stage.EnvironmentEvents == null)
                return;

            var events = stage.EnvironmentEvents;
            if (events.ActiveEvent != CellEnvironmentEventType.NutrientBloom || !_envEventText.enabled)
                return;

            float remaining = events.ActiveEventRemainingSeconds;
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * BalanceConstants.CellNutrientBloomHudPulseFrequency);
            _envEventText.color = Color.Lerp(_bloomPulseColorA, _bloomPulseColorB, pulse);
            events.CopyNutrientBloomHotspots(_bloomHotspotBuffer);
            Vector3 pingOrigin = _player != null ? _player.transform.position : stage.ArenaCenter;
            string pingSuffix = BuildBloomPingSuffix(pingOrigin, _bloomHotspotBuffer, 3);
            _envEventText.text = $"{Localization.Get("cell.env.nutrient_bloom")} ({remaining:F0}s){pingSuffix}";
        }

        /// <summary>
        /// Builds a compact directional ping suffix (e.g. " [NE,E]") from nearest bloom hotspots.
        /// </summary>
        public static string BuildBloomPingSuffix(Vector3 playerPosition, IReadOnlyList<Vector3> hotspots, int maxPings = 3)
        {
            if (hotspots == null || hotspots.Count == 0 || maxPings <= 0)
                return string.Empty;

            int count = Mathf.Min(maxPings, hotspots.Count);
            int[] bestIndices = new int[count];
            float[] bestDistSqr = new float[count];
            for (int i = 0; i < count; i++)
            {
                bestIndices[i] = -1;
                bestDistSqr[i] = float.MaxValue;
            }

            for (int i = 0; i < hotspots.Count; i++)
            {
                Vector3 offset = hotspots[i] - playerPosition;
                offset.y = 0f;
                float d = offset.sqrMagnitude;
                for (int slot = 0; slot < count; slot++)
                {
                    if (d >= bestDistSqr[slot]) continue;
                    for (int shift = count - 1; shift > slot; shift--)
                    {
                        bestDistSqr[shift] = bestDistSqr[shift - 1];
                        bestIndices[shift] = bestIndices[shift - 1];
                    }
                    bestDistSqr[slot] = d;
                    bestIndices[slot] = i;
                    break;
                }
            }

            string suffix = string.Empty;
            int appended = 0;
            for (int i = 0; i < count; i++)
            {
                int idx = bestIndices[i];
                if (idx < 0) continue;
                Vector3 dir = hotspots[idx] - playerPosition;
                dir.y = 0f;
                string label = GetCompassLabel(dir);
                if (string.IsNullOrEmpty(label))
                    continue;
                if (appended == 0)
                    suffix += " [";
                else
                    suffix += ",";
                suffix += label;
                appended++;
            }

            if (appended == 0)
                return string.Empty;
            return suffix + "]";
        }

        private string BuildNearestPartPreviewText()
        {
            if (_player == null)
                return string.Empty;

            var pickups = FindObjectsByType<CellPartPickup>(FindObjectsInactive.Exclude);
            CellPartPickup nearest = null;
            float bestDistanceSqr = float.MaxValue;
            Vector3 playerPosition = _player.transform.position;
            for (int i = 0; i < pickups.Length; i++)
            {
                var pickup = pickups[i];
                if (pickup == null)
                    continue;
                float d = (pickup.transform.position - playerPosition).sqrMagnitude;
                if (d < bestDistanceSqr)
                {
                    bestDistanceSqr = d;
                    nearest = pickup;
                }
            }

            if (nearest == null)
                return string.Empty;

            return "  |  " + BuildPartPreviewText(nearest.PartType, Mathf.Sqrt(bestDistanceSqr));
        }

        public static string BuildPartPreviewText(CellPartType partType, float distance)
        {
            return $"{CellPartPickupPlanner.GetPreviewText(partType)} {Mathf.Max(0f, distance):F0}m";
        }
    }
}
