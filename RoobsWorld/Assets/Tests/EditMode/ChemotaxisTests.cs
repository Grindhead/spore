using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for CellOrganismAI chemotaxis / biome-affinity helpers:
    ///   GetPreferredRadiusFraction — genome → preferred biome radius fraction
    ///   GetBiomeAffinityDrift     — radial drift vector toward the preferred ring
    /// Implements cellStageOrganismDesign §7 (chemotaxis / phototaxis).
    /// </summary>
    public class ChemotaxisTests
    {
        // ---- GetPreferredRadiusFraction ----

        [Test]
        public void GetPreferredRadiusFraction_NullGenome_ReturnsMinusOne()
        {
            Assert.AreEqual(-1f, CellOrganismAI.GetPreferredRadiusFraction(null));
        }

        [Test]
        public void GetPreferredRadiusFraction_DefaultBalancedGenome_ReturnsMinusOne()
        {
            // Attack == Digestion, normal metabolism → no biome preference
            var g = Genome.Default();
            Assert.AreEqual(-1f, CellOrganismAI.GetPreferredRadiusFraction(g));
        }

        [Test]
        public void GetPreferredRadiusFraction_PhotosyntheticTrait_ReturnsAlgaeForestBand()
        {
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            float frac = CellOrganismAI.GetPreferredRadiusFraction(g);
            Assert.Greater(frac, BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction,
                "Photosynthetic organisms should prefer the AlgaeForest (outer band)");
            Assert.LessOrEqual(frac, BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction);
        }

        [Test]
        public void GetPreferredRadiusFraction_HerbivoreGenome_ReturnsAlgaeForestBand()
        {
            var g = Genome.Default();
            g.Digestion = 7f;
            g.Attack    = 1f;   // Digestion − Attack = 6 ≥ 2 → herbivore
            float frac = CellOrganismAI.GetPreferredRadiusFraction(g);
            Assert.Greater(frac, BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction);
            Assert.LessOrEqual(frac, BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction);
        }

        [Test]
        public void GetPreferredRadiusFraction_CarnivoreGenome_ReturnsOpenWaterBand()
        {
            var g = Genome.Default();
            g.Attack    = 7f;
            g.Digestion = 1f;   // Attack − Digestion = 6 ≥ 2 → carnivore
            float frac = CellOrganismAI.GetPreferredRadiusFraction(g);
            Assert.Greater(frac, BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction,
                "Carnivore should prefer OpenWater (mid band)");
            Assert.LessOrEqual(frac, BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction);
        }

        [Test]
        public void GetPreferredRadiusFraction_LowMetabolismGenome_ReturnsDeepZoneBand()
        {
            var g = Genome.Default();
            g.MetabolismCost = 0.2f;   // < 0.3 → DeepZone
            float frac = CellOrganismAI.GetPreferredRadiusFraction(g);
            Assert.GreaterOrEqual(frac, 0f);
            Assert.LessOrEqual(frac, BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction,
                "Low-metabolism organism should prefer the DeepZone (inner band)");
        }

        [Test]
        public void GetPreferredRadiusFraction_PhotosyntheticOverridesHighAttack()
        {
            // Photosynthetic trait takes priority even on a high-Attack genome
            var g = Genome.Default();
            g.Attack    = 9f;
            g.Digestion = 1f;
            g.Traits    = GeneticTraits.Photosynthetic;
            float frac = CellOrganismAI.GetPreferredRadiusFraction(g);
            Assert.Greater(frac, BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction,
                "Photosynthetic trait takes precedence and maps to AlgaeForest");
        }

        // ---- GetBiomeAffinityDrift ----

        [Test]
        public void GetBiomeAffinityDrift_ZeroArenaRadius_ReturnsZero()
        {
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            var drift = CellOrganismAI.GetBiomeAffinityDrift(Vector3.zero, Vector3.zero, 0f, g);
            Assert.AreEqual(Vector3.zero, drift);
        }

        [Test]
        public void GetBiomeAffinityDrift_NullGenome_ReturnsZero()
        {
            var drift = CellOrganismAI.GetBiomeAffinityDrift(
                new Vector3(10f, 0f, 0f), Vector3.zero, 50f, null);
            Assert.AreEqual(Vector3.zero, drift);
        }

        [Test]
        public void GetBiomeAffinityDrift_BalancedGenome_ReturnsZero()
        {
            // No biome preference → no drift
            var g = Genome.Default();
            var drift = CellOrganismAI.GetBiomeAffinityDrift(
                new Vector3(20f, 0f, 0f), Vector3.zero, 50f, g);
            Assert.AreEqual(Vector3.zero, drift);
        }

        [Test]
        public void GetBiomeAffinityDrift_OrganismBeyondPreferredRing_DriftsInward()
        {
            // Photosynthetic preferred radius = 0.75 × 50 = 37.5 units
            // Place organism at 45 units → should drift toward centre (inward)
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            var pos = new Vector3(45f, 0f, 0f);
            var drift = CellOrganismAI.GetBiomeAffinityDrift(pos, Vector3.zero, 50f, g);

            // X component should be negative (pointing toward origin from +X side)
            Assert.Less(drift.x, 0f, "Organism past preferred ring must drift inward");
            Assert.AreEqual(0f, drift.y, 0.001f);
        }

        [Test]
        public void GetBiomeAffinityDrift_OrganismInsidePreferredRing_DriftsOutward()
        {
            // Photosynthetic preferred radius = 37.5; place at 10 units → drift outward
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            var pos = new Vector3(10f, 0f, 0f);
            var drift = CellOrganismAI.GetBiomeAffinityDrift(pos, Vector3.zero, 50f, g);

            Assert.Greater(drift.x, 0f, "Organism inside preferred ring must drift outward");
        }

        [Test]
        public void GetBiomeAffinityDrift_AtPreferredRing_StrengthNearZero()
        {
            // Place organism exactly at 0.75 × 50 = 37.5 units → radial error = 0 → drift ≈ 0
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            var pos = new Vector3(37.5f, 0f, 0f);
            var drift = CellOrganismAI.GetBiomeAffinityDrift(pos, Vector3.zero, 50f, g);
            Assert.AreEqual(0f, drift.magnitude, 0.001f, "On-ring drift should be zero");
        }

        [Test]
        public void GetBiomeAffinityDrift_MagnitudeClampedToOne()
        {
            // Furthest possible position from preferred ring should not exceed magnitude 1
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            var pos = new Vector3(0f, 0f, 0f); // at arena centre, preferred ring at 37.5
            var drift = CellOrganismAI.GetBiomeAffinityDrift(pos, Vector3.zero, 50f, g);
            Assert.LessOrEqual(drift.magnitude, 1.001f, "Drift magnitude must not exceed 1");
        }

        [Test]
        public void ShouldSeekBloomHotspots_PhotosyntheticGenome_ReturnsTrue()
        {
            var g = Genome.Default();
            g.Traits = GeneticTraits.Photosynthetic;
            Assert.IsTrue(CellOrganismAI.ShouldSeekBloomHotspots(g));
        }

        [Test]
        public void ShouldSeekBloomHotspots_HerbivoreGenome_ReturnsTrue()
        {
            var g = Genome.Default();
            g.Digestion = 7f;
            g.Attack = 1f;
            Assert.IsTrue(CellOrganismAI.ShouldSeekBloomHotspots(g));
        }

        [Test]
        public void ShouldSeekBloomHotspots_CarnivoreGenome_ReturnsFalse()
        {
            var g = Genome.Default();
            g.Attack = 8f;
            g.Digestion = 1f;
            Assert.IsFalse(CellOrganismAI.ShouldSeekBloomHotspots(g));
        }

        [Test]
        public void GetBloomHotspotDrift_PointsTowardHotspot()
        {
            Vector3 pos = new Vector3(0f, 0f, 0f);
            Vector3 hotspot = new Vector3(10f, 0f, 0f);
            Vector3 drift = CellOrganismAI.GetBloomHotspotDrift(pos, hotspot, arenaRadius: 50f);
            Assert.Greater(drift.x, 0f);
            Assert.AreEqual(0f, drift.y, 0.0001f);
        }

        // ---- BiomeAffinityStrength constant ----

        [Test]
        public void BiomeAffinityStrength_IsBetweenZeroAndOne()
        {
            Assert.Greater(CellOrganismAI.BiomeAffinityStrength, 0f);
            Assert.Less(CellOrganismAI.BiomeAffinityStrength, 1f,
                "Strength must be < 1 so wander randomness is preserved");
        }
    }
}
