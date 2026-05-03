using System;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

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
            ShowSummary(elapsed);
        }

        private void ShowSummary(float elapsed)
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

            BuildPanel(elapsed, peakMass, dnaEarned, speciesCreated, traitCount, flags, totalRuns);
        }

        private void BuildPanel(float elapsed, float peakMass, int dna, int species, int traits,
                                 NewBestFlags newBest, int totalRuns)
        {
            float scale = SettingsManager.GetUITextScale();
            var font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // Dark overlay
            var overlay = MakeElement("Overlay", transform, Vector2.zero, Vector2.one);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            // Panel border
            var border = MakeElement("PanelBorder", transform,
                new Vector2(0.24f, 0.17f), new Vector2(0.76f, 0.83f));
            border.AddComponent<Image>().color = new Color(0.25f, 0.55f, 0.8f, 0.3f);

            // Panel
            var panel = MakeElement("Panel", transform,
                new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.82f));
            panel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.14f, 0.97f);

            // Header bar
            var headerBar = MakeElement("HeaderBar", panel.transform,
                new Vector2(0f, 0.87f), Vector2.one);
            headerBar.AddComponent<Image>().color = new Color(0.1f, 0.18f, 0.32f, 1f);

            // Top accent line
            var topLine = MakeElement("TopLine", panel.transform,
                new Vector2(0f, 0.999f), new Vector2(1f, 1f));
            topLine.AddComponent<Image>().color = new Color(0.35f, 0.75f, 1f, 0.9f);

            // Title inside header
            AddLabel(panel.transform, "Title",
                new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.99f),
                Localization.Get("cell.summary.title"),
                26, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold, scale, addOutline: true);

            // Run count top-right
            if (totalRuns > 0)
            {
                string runCountText = string.Format(Localization.Get("cell.summary.run_count"), totalRuns);
                AddLabel(panel.transform, "RunCount",
                    new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.99f),
                    runCountText, 13, new Color(0.55f, 0.65f, 0.82f), TextAnchor.MiddleRight, FontStyle.Normal, scale);
            }

            // Stat rows
            string newBestLabel = Localization.Get("cell.summary.new_best");
            float y = 0.70f;
            float step = 0.115f;
            bool[] altRow = { false, true, false, true, false };
            int r = 0;
            AddStatRow(panel.transform, "TimeStat",    y, Localization.Get("cell.summary.time"),      FormatTime(elapsed),       scale, newBest.Time     ? newBestLabel : null, altRow[r++]); y -= step;
            AddStatRow(panel.transform, "PeakStat",    y, Localization.Get("cell.summary.peak_mass"), peakMass.ToString("F1"),   scale, newBest.PeakMass ? newBestLabel : null, altRow[r++]); y -= step;
            AddStatRow(panel.transform, "DNAStat",     y, Localization.Get("cell.summary.dna"),       dna.ToString(),            scale, newBest.DNA      ? newBestLabel : null, altRow[r++]); y -= step;
            AddStatRow(panel.transform, "SpeciesStat", y, Localization.Get("cell.summary.species"),   species.ToString(),        scale, newBest.Species  ? newBestLabel : null, altRow[r++]); y -= step;
            AddStatRow(panel.transform, "TraitStat",   y, Localization.Get("cell.summary.traits"),    traits.ToString(),         scale, newBest.Traits   ? newBestLabel : null, altRow[r]);

            // Buttons
            AddButton(panel.transform, "PlayAgainBtn",
                new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.16f),
                Localization.Get("cell.summary.restart"),
                new Color(0.12f, 0.42f, 0.2f, 0.95f), scale, OnRestart);
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return m > 0 ? $"{m}m {s:D2}s" : $"{s}s";
        }

        private void AddStatRow(Transform parent, string name, float yAnchor,
                                string label, string value, float scale, string newBestBadge = null,
                                bool altBackground = false)
        {
            float rowHeight = 0.088f;
            var row = MakeElement(name, parent,
                new Vector2(0f, yAnchor - rowHeight * 0.5f),
                new Vector2(1f, yAnchor + rowHeight * 0.5f));

            if (altBackground)
                row.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.2f, 0.5f);

            AddLabel(row.transform, "Label",
                new Vector2(0.05f, 0f), new Vector2(0.55f, 1f),
                label, 15, new Color(0.65f, 0.8f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal, scale);

            float valueEndX = newBestBadge != null ? 0.71f : 0.95f;
            AddLabel(row.transform, "Value",
                new Vector2(0.56f, 0f), new Vector2(valueEndX, 1f),
                value, 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold, scale);

            if (newBestBadge != null)
            {
                var badgeBg = MakeElement("BadgeBg", row.transform,
                    new Vector2(0.72f, 0.15f), new Vector2(0.98f, 0.85f));
                badgeBg.AddComponent<Image>().color = new Color(0.4f, 0.28f, 0f, 0.5f);

                AddLabel(row.transform, "NewBest",
                    new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f),
                    newBestBadge, 12, new Color(1f, 0.88f, 0.15f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            }
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
                o.effectColor = new Color(0f, 0.1f, 0.3f, 0.75f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void AddButton(Transform parent, string name,
                                Vector2 anchorMin, Vector2 anchorMax,
                                string label, Color bgColor, float scale,
                                Action onClick)
        {
            var go = MakeElement(name, parent, anchorMin, anchorMax);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = new Color(bgColor.r * 1.4f, bgColor.g * 1.4f, bgColor.b * 1.4f, 1f);
            colors.pressedColor     = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
            colors.selectedColor    = colors.highlightedColor;
            btn.targetGraphic = img;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            // Left accent bar
            var barGo = MakeElement("AccentBar", go.transform,
                new Vector2(0f, 0.1f), new Vector2(0f, 0.9f));
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMax = new Vector2(0f, 0.9f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(4, 0);
            barGo.AddComponent<Image>().color = new Color(bgColor.r * 1.6f, bgColor.g * 1.6f, bgColor.b * 1.6f, 0.9f);

            var txtGo = MakeElement("Text", go.transform, Vector2.zero, Vector2.one);
            var t = txtGo.AddComponent<Text>();
            t.text = label;
            t.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            t.fontSize = Mathf.RoundToInt(18 * scale);
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
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
