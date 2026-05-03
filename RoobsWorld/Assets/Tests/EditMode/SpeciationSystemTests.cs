using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class SpeciationSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            SpeciationSystem.Reset();
        }

        [Test]
        public void GetGeneticDistanceThreshold_ReturnsBalanceConstant()
        {
            Assert.AreEqual(BalanceConstants.CellSpeciationGeneticDistanceThreshold, SpeciationSystem.GetGeneticDistanceThreshold());
        }

        [Test]
        public void GeneticDistance_SameGenome_ReturnsZero()
        {
            var g = Genome.Default();
            Assert.AreEqual(0f, SpeciationSystem.GeneticDistance(g, g), 0.0001f);
        }

        [Test]
        public void GeneticDistance_NullFirst_ReturnsZero()
        {
            Assert.AreEqual(0f, SpeciationSystem.GeneticDistance(null, Genome.Default()), 0.0001f);
        }

        [Test]
        public void GeneticDistance_NullSecond_ReturnsZero()
        {
            Assert.AreEqual(0f, SpeciationSystem.GeneticDistance(Genome.Default(), null), 0.0001f);
        }

        [Test]
        public void GeneticDistance_DifferentTraits_ReturnsPositiveSum()
        {
            var a = Genome.Default();
            var b = Genome.Default();
            b.Speed = 3f;
            b.Attack = 2f;
            float d = SpeciationSystem.GeneticDistance(a, b);
            Assert.Greater(d, 0f);
            Assert.AreEqual(3f, d, 0.01f); // |3-1| + |2-1| = 2 + 1 = 3
            Assert.GreaterOrEqual(d, 2f);
        }

        [Test]
        public void GetSpeciesIdForOffspring_WhenDistanceWithinThreshold_ReturnsParentId()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            var parent = Genome.Default();
            var offspring = parent.Clone();
            offspring.Speed = parent.Speed + 0.5f; // small change, under 5
            int id = SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);
            Assert.AreEqual(0, id);
        }

        [Test]
        public void GetSpeciesIdForOffspring_WhenDistanceExceedsThreshold_ReturnsNewSpeciesId()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            var parent = Genome.Default();
            var offspring = parent.Clone();
            offspring.Speed = 10f;
            offspring.Attack = 10f;
            offspring.Defence = 0f;
            int id = SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);
            Assert.AreEqual(1, id);
            var species = SpeciationSystem.GetSpecies(1);
            Assert.NotNull(species);
            Assert.AreEqual(1, species.SpeciesId);
            Assert.AreEqual(0, species.ParentSpeciesId);
        }

        [Test]
        public void GetSpeciesIdForOffspring_EnsuresSpeciesZeroExists()
        {
            int id = SpeciationSystem.GetSpeciesIdForOffspring(0, Genome.Default(), Genome.Default());
            Assert.AreEqual(0, id);
            var s0 = SpeciationSystem.GetSpecies(0);
            Assert.NotNull(s0);
            Assert.AreEqual(0, s0.SpeciesId);
            Assert.AreEqual(-1, s0.ParentSpeciesId);
        }

        [Test]
        public void RegisterOrganism_IncrementsPopulation()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            SpeciationSystem.RegisterOrganism(0);
            SpeciationSystem.RegisterOrganism(0);
            var s = SpeciationSystem.GetSpecies(0);
            Assert.AreEqual(2, s.Population);
        }

        [Test]
        public void UnregisterOrganism_DecrementsPopulation()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            SpeciationSystem.RegisterOrganism(0);
            SpeciationSystem.RegisterOrganism(0);
            SpeciationSystem.UnregisterOrganism(0);
            var s = SpeciationSystem.GetSpecies(0);
            Assert.AreEqual(1, s.Population);
        }

        [Test]
        public void UnregisterOrganism_DoesNotGoBelowZero()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            SpeciationSystem.UnregisterOrganism(0);
            var s = SpeciationSystem.GetSpecies(0);
            Assert.AreEqual(0, s.Population);
        }

        [Test]
        public void GetSpecies_UnknownId_ReturnsNull()
        {
            Assert.IsNull(SpeciationSystem.GetSpecies(99));
        }

        [Test]
        public void GetAllSpecies_ReturnsSortedById()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            var parent = Genome.Default();
            var offspring = parent.Clone();
            offspring.Speed = 10f;
            SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);
            var all = SpeciationSystem.GetAllSpecies();
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(0, all[0].SpeciesId);
            Assert.AreEqual(1, all[1].SpeciesId);
        }

        [Test]
        public void Reset_ClearsRegistry()
        {
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            SpeciationSystem.Reset();
            Assert.IsNull(SpeciationSystem.GetSpecies(0));
            Assert.AreEqual(0, SpeciationSystem.GetAllSpecies().Count);
        }
    }

    public class SpeciesTests
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var g = Genome.Default();
            var s = new Species(1, 0, g);
            Assert.AreEqual(1, s.SpeciesId);
            Assert.AreEqual(0, s.ParentSpeciesId);
            Assert.NotNull(s.BaseGenome);
            Assert.AreEqual(0, s.Population);
        }

        [Test]
        public void BaseGenome_IsClone_NotReference()
        {
            var g = Genome.Default();
            g.Speed = 5f;
            var s = new Species(0, -1, g);
            g.Speed = 10f;
            Assert.AreEqual(5f, s.BaseGenome.Speed, 0.0001f);
        }
    }
}
