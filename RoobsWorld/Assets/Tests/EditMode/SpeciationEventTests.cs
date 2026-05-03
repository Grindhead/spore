using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Verifies that SpeciationSystem publishes CellNewSpeciesEvent and CellSpeciesExtinctEvent correctly.
    /// </summary>
    public class SpeciationEventTests
    {
        [SetUp]
        public void SetUp()
        {
            SpeciationSystem.Reset();
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SpeciationSystem.Reset();
            EventBus.Clear();
        }

        [Test]
        public void GetSpeciesIdForOffspring_PublishesCellNewSpeciesEvent_WhenDivergent()
        {
            // Arrange
            var parent = Genome.Default();
            var offspring = Genome.Default();
            offspring.Speed = parent.Speed + 100f; // force large distance
            offspring.Attack = parent.Attack + 100f;

            CellNewSpeciesEvent? received = null;
            EventBus.Subscribe<CellNewSpeciesEvent>(e => received = e);

            // Act
            int newId = SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);

            // Assert
            Assert.IsTrue(received.HasValue, "CellNewSpeciesEvent should have been published");
            Assert.AreEqual(newId, received.Value.NewSpeciesId);
            Assert.AreEqual(0, received.Value.ParentSpeciesId);
            Assert.Greater(received.Value.TotalSpeciesCount, 1);
        }

        [Test]
        public void GetSpeciesIdForOffspring_DoesNotPublishEvent_WhenSameSpecies()
        {
            // Arrange — identical genomes are within threshold
            var parent = Genome.Default();
            var offspring = Genome.Default();

            bool eventFired = false;
            EventBus.Subscribe<CellNewSpeciesEvent>(_ => eventFired = true);

            // Act
            SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);

            // Assert
            Assert.IsFalse(eventFired, "No CellNewSpeciesEvent for same-species offspring");
        }

        [Test]
        public void UnregisterOrganism_PublishesCellSpeciesExtinctEvent_WhenPopulationReachesZero()
        {
            // Arrange — register one organism then kill it
            var genome = Genome.Default();
            SpeciationSystem.EnsureSpeciesExists(0, genome);
            SpeciationSystem.RegisterOrganism(0);

            CellSpeciesExtinctEvent? received = null;
            EventBus.Subscribe<CellSpeciesExtinctEvent>(e => received = e);

            // Act
            SpeciationSystem.UnregisterOrganism(0);

            // Assert
            Assert.IsTrue(received.HasValue, "CellSpeciesExtinctEvent should fire when population hits 0");
            Assert.AreEqual(0, received.Value.ExtinctSpeciesId);
        }

        [Test]
        public void UnregisterOrganism_DoesNotPublishExtinctEvent_WhenPopulationStillPositive()
        {
            // Arrange — two organisms; removing one should not fire extinct
            var genome = Genome.Default();
            SpeciationSystem.EnsureSpeciesExists(0, genome);
            SpeciationSystem.RegisterOrganism(0);
            SpeciationSystem.RegisterOrganism(0);

            bool eventFired = false;
            EventBus.Subscribe<CellSpeciesExtinctEvent>(_ => eventFired = true);

            // Act
            SpeciationSystem.UnregisterOrganism(0);

            // Assert
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void NewSpeciesEvent_TotalSpeciesCount_IncludesNewSpecies()
        {
            var parent = Genome.Default();
            var offspring = Genome.Default();
            offspring.Speed = parent.Speed + 200f;

            CellNewSpeciesEvent? received = null;
            EventBus.Subscribe<CellNewSpeciesEvent>(e => received = e);

            SpeciationSystem.GetSpeciesIdForOffspring(0, parent, offspring);

            Assert.IsTrue(received.HasValue);
            // Should include at least species 0 and the new one
            Assert.GreaterOrEqual(received.Value.TotalSpeciesCount, 2);
        }
    }
}
