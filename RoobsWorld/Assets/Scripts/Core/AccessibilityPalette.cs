using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Colour-blind-safe palette for game elements whose colour carries meaning
    /// (food type, biome zone, cell traits).
    ///
    /// Each method has a two-argument form (explicit <see cref="ColorBlindMode"/>) that is
    /// unit-testable without a running SettingsManager, and a zero-argument helper that reads
    /// the current mode from <see cref="SettingsManager"/> when available.
    ///
    /// Palettes are derived from the Okabe–Ito colour-blind-safe set and perceptual simulations
    /// of deuteranopia, protanopia, and tritanopia.
    /// </summary>
    public static class AccessibilityPalette
    {
        // ------------------------------------------------------------------ Food colours

        // Default (full-colour vision)
        private static readonly Color PlantDefault  = new Color(0.20f, 0.80f, 0.20f);
        private static readonly Color MeatDefault   = new Color(0.50f, 0.25f, 0.20f);
        private static readonly Color ToxinDefault  = new Color(0.60f, 0.20f, 0.80f);

        // Deuteranopia-safe (red-green blind, ~6% of males)
        private static readonly Color PlantDeutan   = new Color(0.35f, 0.70f, 0.95f); // sky blue
        private static readonly Color MeatDeutan    = new Color(0.95f, 0.55f, 0.00f); // vivid orange
        private static readonly Color ToxinDeutan   = new Color(0.85f, 0.35f, 0.75f); // pink-magenta

        // Protanopia-safe (red blind, ~1% of males)
        private static readonly Color PlantProtan   = new Color(0.10f, 0.85f, 0.85f); // cyan
        private static readonly Color MeatProtan    = new Color(0.95f, 0.85f, 0.00f); // vivid yellow
        private static readonly Color ToxinProtan   = new Color(0.35f, 0.25f, 0.90f); // deep indigo

        // Tritanopia-safe (blue-yellow blind, ~0.01%)
        private static readonly Color PlantTritan   = new Color(0.15f, 0.95f, 0.25f); // strong green
        private static readonly Color MeatTritan    = new Color(0.95f, 0.15f, 0.15f); // strong red
        private static readonly Color ToxinTritan   = new Color(0.95f, 0.50f, 0.50f); // salmon/pink

        // ------------------------------------------------------------------ Public API

        /// <summary>
        /// Returns the colour to use for a food item of <paramref name="type"/> given
        /// the supplied <paramref name="mode"/>.
        /// </summary>
        public static Color GetFoodColor(FoodType type, ColorBlindMode mode)
        {
            return type switch
            {
                FoodType.Plant => mode switch
                {
                    ColorBlindMode.Deuteranopia => PlantDeutan,
                    ColorBlindMode.Protanopia   => PlantProtan,
                    ColorBlindMode.Tritanopia   => PlantTritan,
                    _                           => PlantDefault
                },
                FoodType.Meat => mode switch
                {
                    ColorBlindMode.Deuteranopia => MeatDeutan,
                    ColorBlindMode.Protanopia   => MeatProtan,
                    ColorBlindMode.Tritanopia   => MeatTritan,
                    _                           => MeatDefault
                },
                FoodType.Toxin => mode switch
                {
                    ColorBlindMode.Deuteranopia => ToxinDeutan,
                    ColorBlindMode.Protanopia   => ToxinProtan,
                    ColorBlindMode.Tritanopia   => ToxinTritan,
                    _                           => ToxinDefault
                },
                _ => PlantDefault
            };
        }

        /// <summary>
        /// Convenience overload that reads the current mode from <see cref="SettingsManager"/>.
        /// Returns the default palette when SettingsManager is unavailable (e.g. EditMode tests).
        /// </summary>
        public static Color GetFoodColor(FoodType type) =>
            GetFoodColor(type, GetCurrentMode());

        /// <summary>
        /// Returns a bacteria-variant tint for Plant food (slightly warmer than pure algae green)
        /// adjusted for the active colour-blind mode.
        /// </summary>
        public static Color GetBacteriaColor(ColorBlindMode mode)
        {
            // Bacteria is a sub-variant of Plant; tint it warm-yellow regardless of mode
            var base_ = GetFoodColor(FoodType.Plant, mode);
            return Color.Lerp(base_, new Color(0.95f, 0.88f, 0.20f), 0.45f);
        }

        /// <summary>Reads <see cref="ColorBlindMode"/> from SettingsManager, or None if unavailable.</summary>
        public static ColorBlindMode GetCurrentMode()
        {
            if (!SettingsManager.HasInstance || SettingsManager.Instance.Current == null)
                return ColorBlindMode.None;
            int idx = SettingsManager.Instance.Current.ColorBlindModeIndex;
            return (ColorBlindMode)Mathf.Clamp(idx, 0, 3);
        }
    }
}
