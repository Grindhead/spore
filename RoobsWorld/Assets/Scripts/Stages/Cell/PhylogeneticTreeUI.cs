using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Displays the phylogenetic tree of species: branching and extinct vs active.
    /// Step 8 of cell-stage evolution. Data from SpeciationSystem; updates in real time.
    /// </summary>
    public class PhylogeneticTreeUI : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _treeText;
        private float _refreshInterval = 0.5f;
        private float _nextRefresh;

        /// <summary>Builds the overlay and shows it. Call from CellStage after HUD.</summary>
        public static PhylogeneticTreeUI CreateRuntime()
        {
            var go = new GameObject("PhylogeneticTreeUI");
            var ui = go.AddComponent<PhylogeneticTreeUI>();
            ui.BuildUI();
            return ui;
        }

        /// <summary>Formats species list into tree lines (indent by depth). For tests and display.</summary>
        public static IReadOnlyList<string> FormatTreeLines(IReadOnlyList<Species> speciesList)
        {
            if (speciesList == null || speciesList.Count == 0)
                return new[] { "No species yet." };

            var lines = new List<string>();
            var depthBySpecies = new Dictionary<int, int>();

            foreach (var s in speciesList)
            {
                int depth = 0;
                int current = s.SpeciesId;
                while (true)
                {
                    var sp = GetSpeciesById(speciesList, current);
                    if (sp == null) break;
                    if (sp.ParentSpeciesId < 0) break;
                    depth++;
                    current = sp.ParentSpeciesId;
                }
                depthBySpecies[s.SpeciesId] = depth;
            }

            var sorted = new List<Species>(speciesList);
            sorted.Sort((a, b) =>
            {
                int da = depthBySpecies.TryGetValue(a.SpeciesId, out var dda) ? dda : 0;
                int db = depthBySpecies.TryGetValue(b.SpeciesId, out var ddb) ? ddb : 0;
                if (da != db) return da.CompareTo(db);
                return a.SpeciesId.CompareTo(b.SpeciesId);
            });

            foreach (var s in sorted)
            {
                int d = depthBySpecies.TryGetValue(s.SpeciesId, out var dd) ? dd : 0;
                string indent = new string(' ', d * 2);
                string extinct = s.Population <= 0 ? " (extinct)" : "";
                lines.Add($"{indent}Species {s.SpeciesId} — pop: {s.Population}{extinct}");
            }

            return lines;
        }

        private static Species GetSpeciesById(IReadOnlyList<Species> list, int id)
        {
            foreach (var s in list)
                if (s.SpeciesId == id) return s;
            return null;
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
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(8f, -28f);
            titleRect.offsetMax = new Vector2(-8f, -4f);

            var titleText = titleGo.AddComponent<Text>();
            titleText.text = "Evolution Tree";
            titleText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            titleText.fontSize = Mathf.RoundToInt(14 * scale);
            titleText.color = UIStyleGuide.Colors.AccentStrong;
            titleText.fontStyle = FontStyle.Bold;
            UIStyleGuide.ApplyTextShadow(titleGo, 0.35f);

            var contentGo = new GameObject("TreeContent");
            contentGo.transform.SetParent(panel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(8f, 8f);
            contentRect.offsetMax = new Vector2(-8f, -36f);

            _treeText = contentGo.AddComponent<Text>();
            _treeText.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            _treeText.fontSize = Mathf.RoundToInt(12 * scale);
            _treeText.color = UIStyleGuide.Colors.BodyTextMuted;
            _treeText.alignment = TextAnchor.UpperLeft;
            _treeText.text = "No species yet.";

            _nextRefresh = _refreshInterval;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var go = new GameObject("TreePanel");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.72f, 0.5f);
            rect.anchorMax = new Vector2(0.98f, 0.95f);
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
            if (_treeText == null) return;
            var species = SpeciationSystem.GetAllSpecies();
            var lines = FormatTreeLines(species);
            _treeText.text = string.Join("\n", lines);
        }
    }
}
