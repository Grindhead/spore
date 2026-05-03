using NUnit.Framework;
using UnityEngine;
using System;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;
using SystemRandom = System.Random;

namespace RoobsWorld.Tests
{
    public class ReproductionSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            SpeciationSystem.Reset();
        }

        [Test]
        public void CanReproduce_WhenEnergyAtOrAboveThreshold_ReturnsTrue()
        {
            Assert.IsTrue(ReproductionSystem.CanReproduce(BalanceConstants.CellReproductionEnergyThreshold));
            Assert.IsTrue(ReproductionSystem.CanReproduce(BalanceConstants.CellReproductionEnergyThreshold + 10f));
        }

        [Test]
        public void CanReproduce_WhenEnergyBelowThreshold_ReturnsFalse()
        {
            Assert.IsFalse(ReproductionSystem.CanReproduce(BalanceConstants.CellReproductionEnergyThreshold - 0.1f));
            Assert.IsFalse(ReproductionSystem.CanReproduce(0f));
        }

        [Test]
        public void GetReproductionThreshold_ReturnsBalanceConstant()
        {
            Assert.AreEqual(BalanceConstants.CellReproductionEnergyThreshold, ReproductionSystem.GetReproductionThreshold());
        }

        [Test]
        public void GetReproductionCost_ReturnsBalanceConstant()
        {
            Assert.AreEqual(BalanceConstants.CellReproductionEnergyCost, ReproductionSystem.GetReproductionCost());
        }

        [Test]
        public void GetOffspringInitialEnergy_ReturnsBalanceConstant()
        {
            Assert.AreEqual(BalanceConstants.CellOffspringInitialEnergy, ReproductionSystem.GetOffspringInitialEnergy());
        }

        [Test]
        public void CanReproduce_Organism_WhenEnoughEnergy_ReturnsTrue()
        {
            float energy = BalanceConstants.CellReproductionEnergyThreshold + 20f;
            var org = Organism.Create(Vector3.zero, Genome.Default(), energy);
            Assert.IsTrue(ReproductionSystem.CanReproduce(org));
            UnityEngine.Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void CanReproduce_Organism_WhenDead_ReturnsFalse()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default(), 100f);
            org.Die();
            Assert.IsFalse(ReproductionSystem.CanReproduce(org));
            UnityEngine.Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void CreateOffspringGenome_ReturnsMutatedGenome()
        {
            var parent = new Genome { Speed = 3f, Attack = 4f };
            var child = ReproductionSystem.CreateOffspringGenome(parent, new SystemRandom(42));
            Assert.NotNull(child);
            Assert.AreNotSame(parent, child);
            Assert.GreaterOrEqual(child.Speed, 0f);
            Assert.LessOrEqual(child.Speed, 10f);
        }

        [Test]
        public void TryReproduce_WhenCanReproduce_SpawnsChildAndDeductsParentEnergy()
        {
            float startEnergy = BalanceConstants.CellReproductionEnergyThreshold + 50f;
            var parent = Organism.Create(Vector3.zero, new Genome { Speed = 2f }, startEnergy);
            float cost = ReproductionSystem.GetReproductionCost();
            float offspringEnergy = ReproductionSystem.GetOffspringInitialEnergy();

            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(123));

            Assert.NotNull(child);
            Assert.AreEqual(startEnergy - cost, parent.Energy, 0.001f);
            Assert.AreEqual(offspringEnergy, child.Energy, 0.001f);
            Assert.NotNull(child.Genome);
            Assert.AreNotSame(parent.Genome, child.Genome);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        [Test]
        public void TryReproduce_WhenInsufficientEnergy_ReturnsNullAndDoesNotDeduct()
        {
            float lowEnergy = BalanceConstants.CellReproductionEnergyThreshold - 10f;
            var parent = Organism.Create(Vector3.zero, Genome.Default(), lowEnergy);

            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(1));

            Assert.IsNull(child);
            Assert.AreEqual(lowEnergy, parent.Energy, 0.001f);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
        }

        [Test]
        public void TryReproduce_WhenDead_ReturnsNull()
        {
            var parent = Organism.Create(Vector3.zero, Genome.Default(), 100f);
            parent.Die();

            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(1));

            Assert.IsNull(child);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
        }

        [Test]
        public void TryReproduce_ChildHasMutatedGenome()
        {
            var parentGenome = new Genome { Speed = 5f, Size = 5f };
            var parent = Organism.Create(Vector3.zero, parentGenome, 100f);
            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(456));

            Assert.NotNull(child);
            Assert.GreaterOrEqual(child.Genome.Speed, 0f);
            Assert.LessOrEqual(child.Genome.Speed, 10f);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        [Test]
        public void TryReproduce_ChildHasSpeciesIdAndIsRegistered()
        {
            var parent = Organism.Create(Vector3.zero, Genome.Default(), BalanceConstants.CellReproductionEnergyThreshold + 50f);
            parent.SetSpeciesId(0);
            SpeciationSystem.EnsureSpeciesExists(0, Genome.Default());
            SpeciationSystem.RegisterOrganism(0);

            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(999));

            Assert.NotNull(child);
            Assert.GreaterOrEqual(child.SpeciesId, 0);
            var species0 = SpeciationSystem.GetSpecies(0);
            Assert.NotNull(species0);
            Assert.GreaterOrEqual(species0.Population, 1);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        [Test]
        public void TryReproduce_ResetsParentAdaptiveMutationBias()
        {
            var parent = Organism.Create(Vector3.zero, Genome.Default(), BalanceConstants.CellReproductionEnergyThreshold + 50f);
            for (int i = 0; i < 8; i++)
                parent.RecordPredationExperience();
            Assert.Greater(parent.GetAdaptiveMutationBias().Attack, 0f);

            var child = ReproductionSystem.TryReproduce(parent, new SystemRandom(12));

            Assert.NotNull(child);
            Assert.AreEqual(0f, parent.GetAdaptiveMutationBias().Attack);
            UnityEngine.Object.DestroyImmediate(parent.gameObject);
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
