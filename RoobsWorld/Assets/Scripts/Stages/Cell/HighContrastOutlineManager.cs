using UnityEngine;
using RoobsWorld.Core;
using System.Collections.Generic;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// When High Contrast Mode is enabled in Settings, boosts emission on hazards every
    /// <see cref="ScanInterval"/> seconds: PredatorCells glow bright red, ToxinClouds glow bright
    /// yellow, and the player cell is highlighted in bright cyan (via PlayerCell.UpdateVisualScale).
    /// Attach to any persistent GameObject in the cell stage (CellStage creates it in OnEnter).
    /// </summary>
    public class HighContrastOutlineManager : MonoBehaviour
    {
        public const float ScanInterval = 0.5f;

        // Emission colours applied to hazards when the mode is active.
        public static readonly Color PredatorHighlightColor = new Color(1.0f, 0.10f, 0.05f); // bright red
        public static readonly Color ToxinHighlightColor    = new Color(0.95f, 0.90f, 0.05f); // bright yellow
        public static readonly Color PlayerHighlightColor   = new Color(0.10f, 0.90f, 1.00f); // bright cyan

        private float _timer;
        private readonly List<PredatorCell> _predatorBuffer = new List<PredatorCell>(24);
        private readonly List<ToxinCloud> _toxinBuffer = new List<ToxinCloud>(24);

        /// <summary>
        /// Returns true when High Contrast Mode is currently enabled.
        /// Safe to call without a live instance (returns false when SettingsManager is absent).
        /// </summary>
        public static bool IsActive
        {
            get
            {
                if (!SettingsManager.HasInstance) return false;
                return SettingsManager.Instance.Current.HighContrastMode;
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = ScanInterval;
            ApplyToScene(IsActive);
        }

        private void OnDisable()
        {
            // Clear forced emission when the component is deactivated (e.g. stage exit).
            ApplyToScene(false);
        }

        // ------------------------------------------------------------------ internal

        private void ApplyToScene(bool active)
        {
            _predatorBuffer.Clear();
            int predatorCount = PredatorPackCoordinator.CopyRegisteredPredators(_predatorBuffer);
            if (predatorCount == 0)
            {
                var predators = FindObjectsByType<PredatorCell>(FindObjectsInactive.Exclude);
                for (int i = 0; i < predators.Length; i++)
                {
                    if (predators[i] != null)
                        _predatorBuffer.Add(predators[i]);
                }
            }

            for (int i = 0; i < _predatorBuffer.Count; i++)
            {
                var predator = _predatorBuffer[i];
                if (predator == null) continue;
                ApplyEmission(predator.GetComponent<MeshRenderer>(), active, PredatorHighlightColor);
            }

            _toxinBuffer.Clear();
            int toxinCount = ToxinCloud.CopyActiveInstances(_toxinBuffer);
            if (toxinCount == 0)
            {
                var clouds = FindObjectsByType<ToxinCloud>(FindObjectsInactive.Exclude);
                for (int i = 0; i < clouds.Length; i++)
                {
                    if (clouds[i] != null)
                        _toxinBuffer.Add(clouds[i]);
                }
            }

            for (int i = 0; i < _toxinBuffer.Count; i++)
            {
                var cloud = _toxinBuffer[i];
                if (cloud == null) continue;
                ApplyEmission(cloud.GetComponent<MeshRenderer>(), active, ToxinHighlightColor);
            }
        }

        private static void ApplyEmission(MeshRenderer mr, bool active, Color highlightColor)
        {
            if (mr == null || mr.material == null) return;
            if (active)
            {
                mr.material.EnableKeyword("_EMISSION");
                mr.material.SetColor("_EmissionColor", highlightColor);
            }
            else
            {
                mr.material.SetColor("_EmissionColor", Color.black);
                mr.material.DisableKeyword("_EMISSION");
            }
        }
    }
}
