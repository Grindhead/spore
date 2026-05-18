using RoobsWorld.UI;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    public static class CellHudPresenter
    {
        public static string BuildThreatText(ThreatIndicator indicator)
        {
            if (indicator == null || !indicator.IsThreatDetected)
                return string.Empty;

            string direction = GetCompassLabel(indicator.PrimaryThreatDirection);
            string distanceLabel = float.IsInfinity(indicator.NearestThreatDistance)
                ? string.Empty
                : $" {indicator.NearestThreatDistance:F0}m";
            string threatLabel = ThreatIndicator.GetThreatCueLabel(indicator.PrimaryThreatType);
            return $"{threatLabel} nearby {direction}{distanceLabel}";
        }

        public static Color BuildThreatColor(float intensity)
        {
            float t = Mathf.Clamp01(intensity);
            return Color.Lerp(
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGold, 0.95f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.CoralBright, 0.98f),
                t);
        }

        private static string GetCompassLabel(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return "ahead";

            Vector3 d = direction.normalized;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.z))
                return d.x > 0f ? "right" : "left";

            return d.z > 0f ? "ahead" : "behind";
        }
    }
}
