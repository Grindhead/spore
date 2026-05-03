using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for HighContrastOutlineManager constants and static IsActive logic.
    /// </summary>
    public class HighContrastOutlineManagerTests
    {
        // ------------------------------------------------------------------ colour constant sanity

        [Test]
        public void PredatorHighlightColor_IsHighSaturation_Red()
        {
            Color c = HighContrastOutlineManager.PredatorHighlightColor;
            // R channel must dominate
            Assert.Greater(c.r, 0.9f, "Predator highlight R should be near 1");
            Assert.Less(c.g, 0.3f,    "Predator highlight G should be low");
            Assert.Less(c.b, 0.3f,    "Predator highlight B should be low");
        }

        [Test]
        public void ToxinHighlightColor_IsHighSaturation_Yellow()
        {
            Color c = HighContrastOutlineManager.ToxinHighlightColor;
            // Bright yellow: high R and G, low B
            Assert.Greater(c.r, 0.8f, "Toxin highlight R should be high");
            Assert.Greater(c.g, 0.8f, "Toxin highlight G should be high");
            Assert.Less(c.b, 0.2f,    "Toxin highlight B should be low");
        }

        [Test]
        public void PlayerHighlightColor_IsHighSaturation_Cyan()
        {
            Color c = HighContrastOutlineManager.PlayerHighlightColor;
            // Bright cyan: low R, high G and B
            Assert.Less(c.r, 0.3f,    "Player highlight R should be low");
            Assert.Greater(c.g, 0.7f, "Player highlight G should be high");
            Assert.Greater(c.b, 0.8f, "Player highlight B should be high");
        }

        [Test]
        public void AllHighlightColors_AreDistinctFromEachOther()
        {
            Color pred  = HighContrastOutlineManager.PredatorHighlightColor;
            Color toxin = HighContrastOutlineManager.ToxinHighlightColor;
            Color play  = HighContrastOutlineManager.PlayerHighlightColor;

            Assert.AreNotEqual(pred,  toxin, "Predator and toxin colours should differ");
            Assert.AreNotEqual(pred,  play,  "Predator and player colours should differ");
            Assert.AreNotEqual(toxin, play,  "Toxin and player colours should differ");
        }

        // ------------------------------------------------------------------ ScanInterval sanity

        [Test]
        public void ScanInterval_IsPositive_AndReasonable()
        {
            Assert.Greater(HighContrastOutlineManager.ScanInterval, 0f);
            Assert.LessOrEqual(HighContrastOutlineManager.ScanInterval, 2f,
                "Scan interval should be at most 2 s to feel responsive");
        }

        // ------------------------------------------------------------------ IsActive without SettingsManager

        [Test]
        public void IsActive_ReturnsFalse_WhenSettingsManagerAbsent()
        {
            // No SettingsManager singleton in EditMode — should default to false
            bool active = HighContrastOutlineManager.IsActive;
            Assert.IsFalse(active, "IsActive should be false when SettingsManager has no instance");
        }

        // ------------------------------------------------------------------ Localization key

        [Test]
        public void Localization_HighContrastKey_IsTranslated()
        {
            string val = RoobsWorld.Core.Localization.Get("settings.high_contrast");
            Assert.IsFalse(string.IsNullOrEmpty(val), "settings.high_contrast should have a value");
            Assert.AreNotEqual("settings.high_contrast", val, "settings.high_contrast should be translated");
        }
    }
}
