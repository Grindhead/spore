using System.Collections.Generic;
using System.Text;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Displays evolution stats: total population, species count, and optional trait summary.
    /// Step 8 of cell-stage evolution. Updates in real time from SpeciationSystem.
    /// </summary>
    public class EvolutionStatsUI : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _statsText;
        private float _refreshInterval = 0.5f;
        private float _nextRefresh;

        /// <summary>Builds the overlay and shows it. Call from CellStage after HUD.</summary>
        public static EvolutionStatsUI CreateRuntime()
        {
            var go = new GameObject("EvolutionStatsUI");
            var ui = go.AddComponent<EvolutionStatsUI>();
            ui.BuildUI();
            return ui;
        }

        /// <summary>Formats summary stats from species list. For tests and display.</summary>
        public static string FormatSummary(IReadOnlyList<Species> speciesList)
        {
            if (speciesList == null)
                return "Total: 0  Species: 0";

            int total = 0;
            int activeSpecies = 0;
            foreach (var s in speciesList)
            {
                total += s.Population;
                if (s.Population > 0) activeSpecies++;
            }

            var sb = new StringBuilder();
            sb.Append($"Total organisms: {total}\n");
            sb.Append($"Species: {speciesList.Count} ({activeSpecies} active, {speciesList.Count - activeSpecies} extinct)");
            return sb.ToString();
        }

        /// <summary>Format genome stats and any active special traits for mutation-choice and organism display.</summary>
        public static string FormatTraits(Genome genome)
        {
            if (genome == null) return "";
            var sb = new StringBuilder();
            sb.Append($"Spd {genome.Speed:F1}  Sz {genome.Size:F1}  Atk {genome.Attack:F1}  Def {genome.Defence:F1}");
            if (genome.Traits != GeneticTraits.None)
            {
                sb.Append("\n");
                if (genome.HasTrait(GeneticTraits.Photosynthetic)) sb.Append("[Photosynthetic] ");
                if (genome.HasTrait(GeneticTraits.Toxic))          sb.Append("[Toxic] ");
                if (genome.HasTrait(GeneticTraits.Armoured))       sb.Append("[Armoured] ");
                if (genome.HasTrait(GeneticTraits.Regenerative))   sb.Append("[Regenerative] ");
                if (genome.HasTrait(GeneticTraits.Electric))       sb.Append("[Electric] ");
            }
            return sb.ToString().TrimEnd();
        }

        private void BuildUI()
        {
            float scale = SettingsManager.GetUITextScale();
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            gameObject.AddComponent<GraphicRaycaster>();

            var panel = CreatePanel(_canvas.transform);
            var contentGo = new GameObject("StatsContent");
            contentGo.transform.SetParent(panel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(8f, 8f);
            contentRect.offsetMax = new Vector2(-8f, -8f);

            _statsText = contentGo.AddComponent<Text>();
            _statsText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _statsText.fontSize = Mathf.RoundToInt(12 * scale);
            _statsText.color = UIStyleGuide.Colors.BodyText;
            _statsText.alignment = TextAnchor.UpperLeft;
            _statsText.text = "Total organisms: 0\nSpecies: 0 (0 active, 0 extinct)";

            _nextRefresh = _refreshInterval;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var go = new GameObject("StatsPanel");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.72f, 0.08f);
            rect.anchorMax = new Vector2(0.98f, 0.42f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.PanelBackgroundStrong, 0.9f);
            UIStyleGuide.ApplyOutline(go, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.HudPanelBorder, 0.32f), new Vector2(1f, -1f));
            return go;
        }

        private void Update()
        {
            if (Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + _refreshInterval;
            Refresh();
        }

        /// <summary>Refreshes display from SpeciationSystem.</summary>
        public void Refresh()
        {
            if (_statsText == null) return;
            var species = SpeciationSystem.GetAllSpecies();
            _statsText.text = FormatSummary(species);
        }
    }
}
