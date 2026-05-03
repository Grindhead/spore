using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for BacteriaSwarmCell: boids constants, BuildGenome(), and trophic classification.
    /// Implements cellStageOrganismDesign §7 (swarming) and organismTypesAndBiomes §3.
    /// </summary>
    public class BacteriaSwarmCellTests
    {
        // ---- Boids constants ----

        [Test]
        public void SwarmRadius_IsLargerThanSeparationRadius()
        {
            Assert.Greater(BacteriaSwarmCell.SwarmRadius, BacteriaSwarmCell.SeparationRadius,
                "Detection radius must exceed separation radius or the swarm can never cohere");
        }

        [Test]
        public void SeparationRadius_IsPositive()
        {
            Assert.Greater(BacteriaSwarmCell.SeparationRadius, 0f);
        }

        [Test]
        public void CohesionWeight_IsPositive()
        {
            Assert.Greater(BacteriaSwarmCell.CohesionWeight, 0f);
        }

        [Test]
        public void SeparationWeight_IsPositive()
        {
            Assert.Greater(BacteriaSwarmCell.SeparationWeight, 0f);
        }

        [Test]
        public void WanderWeight_IsPositive()
        {
            Assert.Greater(BacteriaSwarmCell.WanderWeight, 0f);
        }

        [Test]
        public void SwarmTickInterval_IsPositive()
        {
            Assert.Greater(BacteriaSwarmCell.SwarmTickInterval, 0f);
        }

        [Test]
        public void SeparationWeight_GreaterThanOrEqualToCohesion_AvoidsCrowding()
        {
            // Separation should be at least as strong as cohesion to prevent
            // all bacteria collapsing to a single point.
            Assert.GreaterOrEqual(BacteriaSwarmCell.SeparationWeight, BacteriaSwarmCell.CohesionWeight);
        }

        // ---- Genome ----

        [Test]
        public void BuildGenome_HasHighSpeed()
        {
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.GreaterOrEqual(g.Speed, 5f, "Bacteria must be fast to outrun most organisms");
        }

        [Test]
        public void BuildGenome_HasSmallSize()
        {
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.Less(g.Size, 1f, "Bacteria must be smaller than the default organism size");
            Assert.Greater(g.Size, 0f);
        }

        [Test]
        public void BuildGenome_HasHighMetabolism()
        {
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.Greater(g.MetabolismCost, 1f,
                "High metabolism is intentional: bacteria must keep feeding to survive");
        }

        [Test]
        public void BuildGenome_HasHighFlagella()
        {
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.GreaterOrEqual(g.Flagella, 5f, "Dense flagella represent bacterial propulsion");
        }

        [Test]
        public void BuildGenome_HasNoSpikes()
        {
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.AreEqual(0f, g.Spikes, "Bacteria are non-aggressive; no spikes");
        }

        [Test]
        public void BuildGenome_ClassifiesAsOmnivore()
        {
            // Attack=0.5, Digestion=2 → digestAdv=1.5, threshold=2 → neither Herbivore nor Carnivore
            var g = BacteriaSwarmCell.BuildGenome();
            Assert.AreEqual(CellStage.TrophicRole.Omnivore, CellStage.ClassifyTrophicRole(g),
                "Bacteria are opportunistic feeders — Omnivore trophic role");
        }

        // ---- Colour ----

        [Test]
        public void ArchetypeColor_IsDistinctFromFilterFeeder()
        {
            Assert.AreNotEqual(FilterFeederCell.ArchetypeColor, BacteriaSwarmCell.ArchetypeColor);
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromScavenger()
        {
            Assert.AreNotEqual(ScavengerCell.ArchetypeColor, BacteriaSwarmCell.ArchetypeColor);
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromPredator()
        {
            var predatorColor = new Color(0.5f, 0f, 0.5f);
            Assert.AreNotEqual(predatorColor, BacteriaSwarmCell.ArchetypeColor);
        }

        // ---- Genome size constraint ----

        [Test]
        public void GenomeSize_IsLessThanFilterFeederSize()
        {
            // Bacteria should be visibly smaller than filter feeders
            Assert.Less(BacteriaSwarmCell.GenomeSize, FilterFeederCell.BuildGenome().Size);
        }

        [Test]
        public void GenomeSpeed_IsHigherThanFilterFeederSpeed()
        {
            Assert.Greater(BacteriaSwarmCell.GenomeSpeed, FilterFeederCell.GenomeSpeed,
                "Bacteria sprint to cover ground; filter feeders are slow drifters");
        }
    }
}
