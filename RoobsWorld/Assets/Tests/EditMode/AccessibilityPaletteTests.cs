using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for AccessibilityPalette colour-blind safe palettes.
    /// No SettingsManager available in EditMode so we test the two-argument overloads directly.
    /// </summary>
    public class AccessibilityPaletteTests
    {
        // ------------------------------------------------------------------ Sanity: all combinations return valid colours

        [Test]
        public void GetFoodColor_AllTypeAndModeCombinations_ReturnValidColors()
        {
            var types = new[] { FoodType.Plant, FoodType.Meat, FoodType.Toxin };
            var modes = new[] { ColorBlindMode.None, ColorBlindMode.Deuteranopia, ColorBlindMode.Protanopia, ColorBlindMode.Tritanopia };

            foreach (var type in types)
            {
                foreach (var mode in modes)
                {
                    var c = AccessibilityPalette.GetFoodColor(type, mode);
                    Assert.GreaterOrEqual(c.r, 0f, $"{type}/{mode} r out of range");
                    Assert.LessOrEqual(c.r,   1f, $"{type}/{mode} r out of range");
                    Assert.GreaterOrEqual(c.g, 0f, $"{type}/{mode} g out of range");
                    Assert.LessOrEqual(c.g,   1f, $"{type}/{mode} g out of range");
                    Assert.GreaterOrEqual(c.b, 0f, $"{type}/{mode} b out of range");
                    Assert.LessOrEqual(c.b,   1f, $"{type}/{mode} b out of range");
                }
            }
        }

        // ------------------------------------------------------------------ Default palette matches original hardcoded values

        [Test]
        public void GetFoodColor_None_Plant_IsGreen()
        {
            var c = AccessibilityPalette.GetFoodColor(FoodType.Plant, ColorBlindMode.None);
            Assert.Greater(c.g, c.r, "Plant (default) should be primarily green");
            Assert.Greater(c.g, c.b, "Plant (default) should be primarily green");
        }

        [Test]
        public void GetFoodColor_None_Toxin_IsPurple()
        {
            var c = AccessibilityPalette.GetFoodColor(FoodType.Toxin, ColorBlindMode.None);
            Assert.Greater(c.r + c.b, c.g * 2f, "Toxin (default) should be purple (high R+B relative to G)");
        }

        // ------------------------------------------------------------------ Deuteranopia: Plant is blue, Meat is orange

        [Test]
        public void GetFoodColor_Deuteranopia_Plant_IsBlueish()
        {
            var c = AccessibilityPalette.GetFoodColor(FoodType.Plant, ColorBlindMode.Deuteranopia);
            Assert.Greater(c.b, 0.7f, "Deuteranopia Plant should be sky-blue (high B)");
        }

        [Test]
        public void GetFoodColor_Deuteranopia_Meat_IsOrange()
        {
            var c = AccessibilityPalette.GetFoodColor(FoodType.Meat, ColorBlindMode.Deuteranopia);
            Assert.Greater(c.r, 0.8f, "Deuteranopia Meat should be orange (high R)");
            Assert.Less(c.b, 0.2f,    "Deuteranopia Meat should be orange (low B)");
        }

        // ------------------------------------------------------------------ All three food colours are visually distinct per mode

        [Test]
        public void GetFoodColor_EachMode_ThreeFoodTypesAreDistinct()
        {
            var modes = new[] { ColorBlindMode.None, ColorBlindMode.Deuteranopia, ColorBlindMode.Protanopia, ColorBlindMode.Tritanopia };
            foreach (var mode in modes)
            {
                var plant = AccessibilityPalette.GetFoodColor(FoodType.Plant, mode);
                var meat  = AccessibilityPalette.GetFoodColor(FoodType.Meat,  mode);
                var toxin = AccessibilityPalette.GetFoodColor(FoodType.Toxin, mode);

                float pm = ColorDistance(plant, meat);
                float pt = ColorDistance(plant, toxin);
                float mt = ColorDistance(meat,  toxin);

                Assert.Greater(pm, 0.2f, $"{mode}: Plant and Meat too similar (dist={pm:F3})");
                Assert.Greater(pt, 0.2f, $"{mode}: Plant and Toxin too similar (dist={pt:F3})");
                Assert.Greater(mt, 0.2f, $"{mode}: Meat and Toxin too similar (dist={mt:F3})");
            }
        }

        // ------------------------------------------------------------------ GetBacteriaColor: distinct from plain Plant

        [Test]
        public void GetBacteriaColor_IsDifferentFromPlant()
        {
            var modes = new[] { ColorBlindMode.None, ColorBlindMode.Deuteranopia, ColorBlindMode.Protanopia, ColorBlindMode.Tritanopia };
            foreach (var mode in modes)
            {
                var plant    = AccessibilityPalette.GetFoodColor(FoodType.Plant, mode);
                var bacteria = AccessibilityPalette.GetBacteriaColor(mode);
                float dist = ColorDistance(plant, bacteria);
                Assert.Greater(dist, 0.05f, $"{mode}: Bacteria colour should differ from Plant");
            }
        }

        // ------------------------------------------------------------------ GetCurrentMode: no SettingsManager → None

        [Test]
        public void GetCurrentMode_WithoutSettingsManager_ReturnsNone()
        {
            Assert.AreEqual(ColorBlindMode.None, AccessibilityPalette.GetCurrentMode());
        }

        // ------------------------------------------------------------------ Localization keys exist

        [Test]
        public void Localization_ColorBlindKeys_ArePresent()
        {
            Assert.AreNotEqual("settings.colorblind_mode",        Localization.Get("settings.colorblind_mode"));
            Assert.AreNotEqual("settings.colorblind_none",        Localization.Get("settings.colorblind_none"));
            Assert.AreNotEqual("settings.colorblind_deuteranopia",Localization.Get("settings.colorblind_deuteranopia"));
            Assert.AreNotEqual("settings.colorblind_protanopia",  Localization.Get("settings.colorblind_protanopia"));
            Assert.AreNotEqual("settings.colorblind_tritanopia",  Localization.Get("settings.colorblind_tritanopia"));
        }

        // ------------------------------------------------------------------ Helper

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
