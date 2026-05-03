using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for FilterFeederCell: constants, BuildGenome(), and trophic classification.
    /// Implements the organism-archetype tests for the filter-feeder niche (organismTypesAndBiomes §9).
    /// </summary>
    public class FilterFeederCellTests
    {
        // ---- Constants ----

        [Test]
        public void AbsorptionRadius_IsPositiveAndSmallerThanFoodSeekDistance()
        {
            Assert.Greater(FilterFeederCell.AbsorptionRadius, 0f);
            // CellOrganismAI.MaxFoodSeekDistance = 25f — absorption must be far smaller
            Assert.Less(FilterFeederCell.AbsorptionRadius, 10f);
        }

        [Test]
        public void WanderSpeedFraction_IsBetweenZeroAndOne()
        {
            Assert.Greater(FilterFeederCell.WanderSpeedFraction, 0f);
            Assert.Less(FilterFeederCell.WanderSpeedFraction, 1f,
                "Filter feeders must be slower than full genome speed");
        }

        [Test]
        public void CiliaOscillationRate_IsPositive()
        {
            Assert.Greater(FilterFeederCell.CiliaOscillationRate, 0f);
        }

        [Test]
        public void CiliaAmplitude_IsPositiveAndLessThanTwoPi()
        {
            Assert.Greater(FilterFeederCell.CiliaAmplitude, 0f);
            Assert.Less(FilterFeederCell.CiliaAmplitude, Mathf.PI * 2f);
        }

        [Test]
        public void AbsorptionTickInterval_IsPositive()
        {
            Assert.Greater(FilterFeederCell.AbsorptionTickInterval, 0f);
        }

        // ---- Genome ----

        [Test]
        public void BuildGenome_HasHighDigestion()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.GreaterOrEqual(g.Digestion, 5f, "Filter feeders need high Digestion to survive passively");
        }

        [Test]
        public void BuildGenome_HasNearZeroAttack()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.Less(g.Attack, 1f, "Filter feeders are non-aggressive — Attack should be near zero");
        }

        [Test]
        public void BuildGenome_HasLowSpeed()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.Less(g.Speed, 5f, "Filter feeders drift slowly on cilia");
        }

        [Test]
        public void BuildGenome_HasHighFlagella()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.GreaterOrEqual(g.Flagella, 4f, "Dense cilia = high Flagella gene");
        }

        [Test]
        public void BuildGenome_HasNoSpikes()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.AreEqual(0f, g.Spikes, "Filter feeders have no offensive spikes");
        }

        [Test]
        public void BuildGenome_HasLowMetabolism()
        {
            var g = FilterFeederCell.BuildGenome();
            Assert.Less(g.MetabolismCost, 1f, "Passive feeding requires low energy burn rate");
        }

        [Test]
        public void BuildGenome_ClassifiesAsHerbivore()
        {
            var g = FilterFeederCell.BuildGenome();
            var role = CellStage.ClassifyTrophicRole(g);
            Assert.AreEqual(CellStage.TrophicRole.Herbivore, role,
                "High Digestion + zero Attack must classify as Herbivore");
        }

        // ---- Archetype colour ----

        [Test]
        public void ArchetypeColor_IsDistinctFromPredatorColor()
        {
            var predatorColor = new Color(0.5f, 0f, 0.5f);
            Assert.AreNotEqual(predatorColor, FilterFeederCell.ArchetypeColor);
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromDefaultPlayerColor()
        {
            var playerColor = new Color(0.3f, 1f, 0.5f);
            Assert.AreNotEqual(playerColor, FilterFeederCell.ArchetypeColor);
        }

        // ---- Localization ----

        [Test]
        public void LocalizationKey_FilterFeederName_Exists()
        {
            var value = Localization.Get("npc.filter_feeder.name");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.filter_feeder.name", value, "Key must resolve to a real string, not fall back to key itself");
        }

        [Test]
        public void LocalizationKey_FilterFeederTooltip_Exists()
        {
            var value = Localization.Get("npc.filter_feeder.tooltip");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.filter_feeder.tooltip", value);
        }
    }
}
