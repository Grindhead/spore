using System;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UIColors = RoobsWorld.UI.UIStyleGuide.Colors;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Shown on player death: displays run statistics then offers Play Again.
    /// Subscribes to <see cref="CreatureDiedEvent"/> (IsPlayer=true) to auto-show.
    /// Created at cell-stage start via <see cref="Attach(PlayerCell)"/>; destroyed when the stage exits.
    /// </summary>
    public class RunSummaryUI : MonoBehaviour
    {
        private PlayerCell _player;
        private float _startTime;
        private bool _shown;

        private readonly struct SummaryStat
        {
            public readonly string Name;
            public readonly string Label;
            public readonly string Value;
            public readonly bool IsNewBest;
            public readonly Color Accent;

            public SummaryStat(string name, string label, string value, bool isNewBest, Color accent)
            {
                Name = name;
                Label = label;
                Value = value;
                IsNewBest = isNewBest;
                Accent = accent;
            }
        }

        /// <summary>
        /// Attaches the tracker to the scene and begins recording time.
        /// Call from CellStage.OnEnter after the player cell is created.
        /// </summary>
        public static RunSummaryUI Attach(PlayerCell player)
        {
            var go = new GameObject("RunSummaryUI");
            var ui = go.AddComponent<RunSummaryUI>();
            ui._player = player;
            ui._startTime = Time.time;
            return ui;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CreatureDiedEvent>(OnCreatureDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CreatureDiedEvent>(OnCreatureDied);
        }

        private void OnCreatureDied(CreatureDiedEvent evt)
        {
            if (!evt.IsPlayer || _shown) return;
            _shown = true;
            float elapsed = Time.time - _startTime;
            ShowSummary(elapsed, evt.Cause, evt.RecommendationKey);
        }

        private void ShowSummary(float elapsed, string causeKey, string recommendationKey)
        {
            // Pause gameplay
            Time.timeScale = 0f;

            // Gather stats
            float peakMass   = _player != null ? _player.PeakMass  : 0f;
            int   dnaEarned  = _player != null ? _player.DNAPoints : 0;
            int   traitCount = _player?.PlayerGenome?.TraitCount ?? 0;

            var species = SpeciationSystem.GetAllSpecies();
            int speciesCreated = species != null ? species.Count : 0;

            // Update personal bests and get which were broken
            NewBestFlags flags = default;
            int totalRuns = 0;
            if (GameManager.HasInstance)
            {
                flags      = GameManager.Instance.UpdateRunRecord(elapsed, peakMass, dnaEarned, speciesCreated, traitCount);
                totalRuns  = GameManager.Instance.GetRunRecord().TotalRunsCompleted;
            }

            var story = CellEvolutionStoryBuilder.Build(_player?.PlayerGenome, _player?.PartLoadout);
            BuildPanel(elapsed, peakMass, dnaEarned, speciesCreated, traitCount, flags, totalRuns, causeKey, recommendationKey, story);
        }

        private void BuildPanel(float elapsed, float peakMass, int dna, int species, int traits,
                                 NewBestFlags newBest, int totalRuns,
                                 string causeKey, string recommendationKey,
                                 CellEvolutionStory story)
        {
            float scale = SettingsManager.GetUITextScale();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            gameObject.AddComponent<GraphicRaycaster>();

            var overlay = MakeElement("Overlay", transform, Vector2.zero, Vector2.one);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = UIColors.WithAlpha(UIColors.DeepNavy, 0.78f);

            UIStyleGuide.AddScreenAtmosphere(transform, "RunSummary", 1f);
            AddAtmosphere(transform);

            var panelGlow = MakeElement("PanelGlow", transform,
                new Vector2(0.205f, 0.125f), new Vector2(0.795f, 0.875f));
            panelGlow.AddComponent<Image>().color = UIColors.WithAlpha(UIColors.AccentStrong, 0.12f);

            var border = MakeElement("PanelBorder", transform,
                new Vector2(0.22f, 0.14f), new Vector2(0.78f, 0.86f));
            var borderImage = border.AddComponent<Image>();
            borderImage.sprite = UIStyleGuide.GetPaintedPanelSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.color = UIColors.WithAlpha(UIColors.FossilGoldBright, 0.24f);

            var panel = MakeElement("Panel", transform,
                new Vector2(0.225f, 0.148f), new Vector2(0.775f, 0.852f));
            panel.AddComponent<Image>();
            UIStyleGuide.ApplyPaintedPanelChrome(panel, strong: true, alpha: 0.98f);

            var header = MakeElement("Header", panel.transform,
                new Vector2(0f, 0.72f), Vector2.one);
            var headerImage = header.AddComponent<Image>();
            headerImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            headerImage.type = Image.Type.Sliced;
            headerImage.color = UIColors.WithAlpha(UIColors.InkBlue, 0.86f);

            var topLine = MakeElement("TopAccent", panel.transform,
                new Vector2(0f, 0.992f), Vector2.one);
            topLine.AddComponent<Image>().color = UIColors.AccentStrong;

            var warmLine = MakeElement("WarmAccent", panel.transform,
                new Vector2(0.045f, 0.715f), new Vector2(0.955f, 0.724f));
            warmLine.AddComponent<Image>().color = UIColors.WithAlpha(UIColors.FossilAmber, 0.75f);

            AddLabel(panel.transform, "Kicker",
                new Vector2(0.055f, 0.91f), new Vector2(0.945f, 0.975f),
                "PRIMORDIAL RUN",
                13, UIColors.AccentStrong, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            AddLabel(panel.transform, "Title",
                new Vector2(0.05f, 0.79f), new Vector2(0.64f, 0.93f),
                Localization.Get("cell.summary.title").ToUpperInvariant(),
                34, UIColors.BodyText, TextAnchor.MiddleLeft, FontStyle.Bold, scale, addOutline: true);

            if (totalRuns > 0)
            {
                string runCountText = string.Format(Localization.Get("cell.summary.run_count"), totalRuns);
                AddLabel(panel.transform, "RunCount",
                    new Vector2(0.64f, 0.86f), new Vector2(0.945f, 0.955f),
                    runCountText.ToUpperInvariant(), 13, UIColors.BodyTextMuted, TextAnchor.MiddleRight, FontStyle.Bold, scale);
            }

            string cause = Localization.Get(string.IsNullOrWhiteSpace(causeKey) ? "cell.death.cause.unknown" : causeKey);
            string advice = Localization.Get(string.IsNullOrWhiteSpace(recommendationKey) ? "cell.death.tip.general" : recommendationKey);
            AddLabel(panel.transform, "DeathCause",
                new Vector2(0.645f, 0.785f), new Vector2(0.945f, 0.845f),
                cause, 12, UIColors.CoralBright, TextAnchor.MiddleRight, FontStyle.Bold, scale);
            AddLabel(panel.transform, "DeathAdvice",
                new Vector2(0.645f, 0.742f), new Vector2(0.945f, 0.792f),
                advice, 11, UIColors.BodyTextMuted, TextAnchor.MiddleRight, FontStyle.Italic, scale);

            SummaryStat[] stats =
            {
                new SummaryStat("TimeStat", Localization.Get("cell.summary.time"), FormatTime(elapsed), newBest.Time, UIColors.AccentStrong),
                new SummaryStat("PeakStat", Localization.Get("cell.summary.peak_mass"), peakMass.ToString("F1"), newBest.PeakMass, UIColors.Success),
                new SummaryStat("DNAStat", Localization.Get("cell.summary.dna"), dna.ToString(), newBest.DNA, UIColors.FossilGold),
                new SummaryStat("SpeciesStat", Localization.Get("cell.summary.species"), species.ToString(), newBest.Species, UIColors.Amethyst),
                new SummaryStat("TraitStat", Localization.Get("cell.summary.traits"), traits.ToString(), newBest.Traits, UIColors.Coral)
            };

            int highlightIndex = GetHighlightStatIndex(stats);
            AddHeroStat(panel.transform, stats[highlightIndex], scale);

            AddStatCard(panel.transform, stats[0], new Vector2(0.055f, 0.44f), new Vector2(0.365f, 0.62f), scale);
            AddStatCard(panel.transform, stats[1], new Vector2(0.382f, 0.44f), new Vector2(0.692f, 0.62f), scale);
            AddStatCard(panel.transform, stats[2], new Vector2(0.055f, 0.225f), new Vector2(0.365f, 0.405f), scale);
            AddStatCard(panel.transform, stats[3], new Vector2(0.382f, 0.225f), new Vector2(0.692f, 0.405f), scale);
            AddStatCard(panel.transform, stats[4], new Vector2(0.708f, 0.225f), new Vector2(0.945f, 0.62f), scale, tall: true);

            AddLabel(panel.transform, "EvolutionStory",
                new Vector2(0.055f, 0.158f), new Vector2(0.945f, 0.215f),
                $"{Localization.Get("cell.summary.story")}: {story.Summary}",
                12, UIColors.BodyTextMuted, TextAnchor.MiddleCenter, FontStyle.Italic, scale);

            AddButton(panel.transform, "PlayAgainBtn",
                new Vector2(0.265f, 0.055f), new Vector2(0.735f, 0.155f),
                Localization.Get("cell.summary.restart").ToUpperInvariant(),
                UIButtonVariant.Primary, scale, OnRestart);
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return m > 0 ? $"{m}m {s:D2}s" : $"{s}s";
        }

        private static int GetHighlightStatIndex(SummaryStat[] stats)
        {
            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i].IsNewBest)
                    return i;
            }

            return 0;
        }

        private void AddHeroStat(Transform parent, SummaryStat stat, float scale)
        {
            var hero = MakeElement("HeroStat", parent,
                new Vector2(0.055f, 0.655f), new Vector2(0.945f, 0.755f));
            var heroImage = hero.AddComponent<Image>();
            heroImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            heroImage.type = Image.Type.Sliced;
            heroImage.color = UIColors.WithAlpha(UIColors.SurfaceMuted, 0.74f);
            var outline = hero.AddComponent<Outline>();
            outline.effectColor = UIColors.WithAlpha(stat.Accent, 0.45f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            var accent = MakeElement("HeroAccent", hero.transform, Vector2.zero, new Vector2(0.008f, 1f));
            accent.AddComponent<Image>().color = stat.Accent;

            AddLabel(hero.transform, "HeroLabel",
                new Vector2(0.035f, 0f), new Vector2(0.55f, 1f),
                stat.IsNewBest ? "NEW PERSONAL BEST" : stat.Label.ToUpperInvariant(),
                14, stat.IsNewBest ? UIColors.FossilGold : UIColors.BodyTextMuted, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            AddLabel(hero.transform, "HeroValue",
                new Vector2(0.55f, 0f), new Vector2(0.965f, 1f),
                stat.Value, 31, UIColors.BodyText, TextAnchor.MiddleRight, FontStyle.Bold, scale, addOutline: true);
        }

        private void AddStatCard(Transform parent, SummaryStat stat, Vector2 anchorMin, Vector2 anchorMax, float scale, bool tall = false)
        {
            var card = MakeElement(stat.Name, parent, anchorMin, anchorMax);
            var cardImage = card.AddComponent<Image>();
            cardImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            cardImage.type = Image.Type.Sliced;
            cardImage.color = stat.IsNewBest
                ? UIColors.WithAlpha(UIColors.ShellBrown, 0.9f)
                : UIColors.WithAlpha(UIColors.PanelBackground, 0.72f);

            var outline = card.AddComponent<Outline>();
            outline.effectColor = stat.IsNewBest
                ? UIColors.WithAlpha(UIColors.FossilGold, 0.62f)
                : UIColors.WithAlpha(stat.Accent, 0.26f);
            outline.effectDistance = new Vector2(1f, -1f);

            var accent = MakeElement("Accent", card.transform, new Vector2(0f, 0.86f), Vector2.one);
            accent.AddComponent<Image>().color = stat.IsNewBest ? UIColors.FossilGold : UIColors.WithAlpha(stat.Accent, 0.82f);

            AddLabel(card.transform, "Label",
                new Vector2(0.07f, tall ? 0.66f : 0.54f), new Vector2(0.93f, tall ? 0.84f : 0.82f),
                stat.Label.ToUpperInvariant(), tall ? 12 : 13, UIColors.BodyTextMuted, TextAnchor.MiddleLeft, FontStyle.Bold, scale);

            AddLabel(card.transform, "Value",
                new Vector2(0.07f, tall ? 0.28f : 0.16f), new Vector2(0.93f, tall ? 0.62f : 0.5f),
                stat.Value, tall ? 34 : 27, UIColors.BodyText, TextAnchor.MiddleLeft, FontStyle.Bold, scale, addOutline: true);

            if (!stat.IsNewBest)
                return;

            var badge = MakeElement("BestBadge", card.transform,
                new Vector2(0.07f, tall ? 0.08f : 0.07f), new Vector2(0.93f, tall ? 0.22f : 0.24f));
            var badgeImage = badge.AddComponent<Image>();
            badgeImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = UIColors.WithAlpha(UIColors.FossilAmber, 0.22f);

            AddLabel(badge.transform, "BadgeText",
                Vector2.zero, Vector2.one,
                "NEW BEST", tall ? 11 : 12, UIColors.FossilGold, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
        }

        private static void AddLabel(Transform parent, string name,
                                     Vector2 anchorMin, Vector2 anchorMax,
                                     string text, int fontSize, Color color,
                                     TextAnchor alignment, FontStyle style, float scale,
                                     bool addOutline = false)
        {
            var go = MakeElement(name, parent, anchorMin, anchorMax);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            t.fontSize = Mathf.RoundToInt(fontSize * scale);
            t.color = color;
            t.alignment = alignment;
            t.fontStyle = style;
            if (addOutline)
            {
                var o = go.AddComponent<Outline>();
                o.effectColor = UIColors.WithAlpha(UIColors.DeepNavy, 0.75f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void AddButton(Transform parent, string name,
                                Vector2 anchorMin, Vector2 anchorMax,
                                string label, UIButtonVariant variant, float scale,
                                Action onClick)
        {
            var go = MakeElement(name, parent, anchorMin, anchorMax);
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            UIStyleGuide.ApplyPaintedButtonChrome(btn, variant);
            UIStyleGuide.AddButtonPearl(go, variant, variant == UIButtonVariant.Primary ? 30f : 23f);
            btn.onClick.AddListener(() => onClick());

            var txtGo = MakeElement("Text", go.transform, Vector2.zero, Vector2.one);
            var textRect = txtGo.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(28f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            var t = txtGo.AddComponent<Text>();
            t.text = label;
            t.font = UIStyleGuide.GetDefaultFont();
            t.fontSize = Mathf.RoundToInt(18 * scale);
            UIStyleGuide.ApplyButtonLabel(t, variant, bold: true);
            t.alignment = TextAnchor.MiddleCenter;
            var shadow = txtGo.AddComponent<Shadow>();
            shadow.effectColor = UIColors.Shadow;
            shadow.effectDistance = new Vector2(1.4f, -1.4f);

        }

        private static void AddButtonInteractionAnimation(GameObject buttonObject)
        {
            var trigger = buttonObject.AddComponent<EventTrigger>();
            Vector3 normalScale = Vector3.one;
            Vector3 hoverScale = new Vector3(1.018f, 1.018f, 1f);
            Vector3 pressScale = new Vector3(0.988f, 0.988f, 1f);

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => buttonObject.transform.localScale = hoverScale);
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => buttonObject.transform.localScale = normalScale);
            trigger.triggers.Add(exit);

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => buttonObject.transform.localScale = pressScale);
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => buttonObject.transform.localScale = hoverScale);
            trigger.triggers.Add(up);
        }

        private static void AddAtmosphere(Transform parent)
        {
            CreateAtmosphereBand(parent, "TopTide", new Vector2(0f, 0.58f), Vector2.one,
                UIColors.WithAlpha(UIColors.AccentStrong, 0.08f));
            CreateAtmosphereBand(parent, "BottomAmber", Vector2.zero, new Vector2(1f, 0.34f),
                UIColors.WithAlpha(UIColors.FossilAmber, 0.07f));
            CreateAtmosphereBand(parent, "CentreDepth", new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f),
                UIColors.WithAlpha(UIColors.TideTeal, 0.08f));
        }

        private static void CreateAtmosphereBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var band = MakeElement(name, parent, anchorMin, anchorMax);
            var image = band.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static GameObject MakeElement(string name, Transform parent,
                                              Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private void OnRestart()
        {
            Time.timeScale = 1f;
            if (GameManager.HasInstance)
            {
                long seed = GameManager.Instance.CurrentSave != null
                    ? GameManager.Instance.CurrentSave.WorldSeed
                    : 0;
                GameManager.Instance.StartNewGame(seed);
                return;
            }

            StageManager.Instance?.TransitionTo(GameStageType.Cell);
        }

    }
}
