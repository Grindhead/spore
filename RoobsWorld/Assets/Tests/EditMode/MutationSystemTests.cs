using NUnit.Framework;
using UnityEngine;
using System;
using RoobsWorld.Stages.Cell;
using SystemRandom = System.Random;

namespace RoobsWorld.Tests
{
    public class MutationSystemTests
    {
        [Test]
        public void Mutate_NullParent_ReturnsDefault()
        {
            var g = MutationSystem.Mutate(null, new SystemRandom(42));
            Assert.NotNull(g);
            Assert.AreEqual(1f, g.Speed);
            Assert.AreEqual(0.5f, g.MetabolismCost);
        }

        [Test]
        public void Mutate_WithSameSeed_IsDeterministic()
        {
            var parent = new Genome { Speed = 2f, Size = 1.5f, Attack = 3f };
            var a = MutationSystem.Mutate(parent, new SystemRandom(123));
            var b = MutationSystem.Mutate(parent, new SystemRandom(123));
            Assert.AreEqual(a.Speed, b.Speed);
            Assert.AreEqual(a.Size, b.Size);
            Assert.AreEqual(a.Attack, b.Attack);
            Assert.AreEqual(a.Spikes, b.Spikes);
            Assert.AreEqual(a.Flagella, b.Flagella);
        }

        [Test]
        public void Mutate_ReturnsNewGenome_WithPossibleChanges()
        {
            var parent = new Genome { Speed = 5f, Size = 5f, Attack = 5f, Spikes = 2f, Flagella = 3f };
            bool anyDifferent = false;
            for (int seed = 0; seed < 50 && !anyDifferent; seed++)
            {
                var child = MutationSystem.Mutate(parent, new SystemRandom(seed));
                if (child.Speed != parent.Speed || child.Size != parent.Size || child.Spikes != parent.Spikes)
                    anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "Mutation should sometimes change traits across seeds");
        }

        [Test]
        public void Mutate_ClampsTraitsBetween0And10()
        {
            var parentLow = new Genome { Speed = 0f, Size = 0f };
            var parentHigh = new Genome { Speed = 10f, Size = 10f };
            for (int seed = 0; seed < 20; seed++)
            {
                var rng = new SystemRandom(seed);
                var childLow = MutationSystem.Mutate(parentLow, rng);
                var childHigh = MutationSystem.Mutate(parentHigh, new SystemRandom(seed + 100));
                Assert.GreaterOrEqual(childLow.Speed, 0f);
                Assert.LessOrEqual(childLow.Speed, 10f);
                Assert.GreaterOrEqual(childHigh.Speed, 0f);
                Assert.LessOrEqual(childHigh.Speed, 10f);
            }
        }

        [Test]
        public void MutateGene_WithDeterministicRng_CanIncreaseOrDecrease()
        {
            float v = 5f;
            bool increased = false;
            bool decreased = false;
            for (int i = 0; i < 100; i++)
            {
                float m = MutationSystem.MutateGene(v, false, new SystemRandom(i));
                if (m > v) increased = true;
                if (m < v) decreased = true;
                Assert.GreaterOrEqual(m, 0f);
                Assert.LessOrEqual(m, 10f);
            }
            Assert.IsTrue(increased, "MutateGene should sometimes increase");
            Assert.IsTrue(decreased, "MutateGene should sometimes decrease");
        }

        [Test]
        public void Mutate_VisualGenesIncluded()
        {
            var parent = new Genome { Spikes = 4f, Flagella = 3f, Color = new Color(0.5f, 0.5f, 0.5f, 0.25f) };
            var child = MutationSystem.Mutate(parent, new SystemRandom(999));
            Assert.GreaterOrEqual(child.Spikes, 0f);
            Assert.LessOrEqual(child.Spikes, 10f);
            Assert.GreaterOrEqual(child.Flagella, 0f);
            Assert.LessOrEqual(child.Flagella, 10f);
            Assert.GreaterOrEqual(child.Color.r, 0f);
            Assert.LessOrEqual(child.Color.r, 1f);
            Assert.AreEqual(1f, child.Color.a, 0.001f);
        }

        [Test]
        public void MutateTraits_InheritsExistingTraits()
        {
            // Traits already on parent are never removed
            var parent = new Genome { Traits = GeneticTraits.Toxic | GeneticTraits.Armoured };
            for (int seed = 0; seed < 50; seed++)
            {
                var child = MutationSystem.Mutate(parent, new SystemRandom(seed));
                Assert.IsTrue(child.HasTrait(GeneticTraits.Toxic),   $"Toxic lost at seed {seed}");
                Assert.IsTrue(child.HasTrait(GeneticTraits.Armoured), $"Armoured lost at seed {seed}");
            }
        }

        [Test]
        public void MutateTraits_CanUnlockNewTrait_OverManySeeds()
        {
            // With 4% chance per mutation, should unlock at least once in 200 seeds
            var parent = new Genome { Traits = GeneticTraits.None };
            bool unlocked = false;
            for (int seed = 0; seed < 200 && !unlocked; seed++)
                unlocked = MutationSystem.MutateTraits(GeneticTraits.None, false, new SystemRandom(seed)) != GeneticTraits.None;
            Assert.IsTrue(unlocked, "MutateTraits should unlock a trait in 200 attempts");
        }

        [Test]
        public void MutateTraits_Deterministic_WithSameSeed()
        {
            var a = MutationSystem.MutateTraits(GeneticTraits.Photosynthetic, false, new SystemRandom(77));
            var b = MutationSystem.MutateTraits(GeneticTraits.Photosynthetic, false, new SystemRandom(77));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void CreateStrategicMutationChoices_GuaranteesDistinctBuildRoles()
        {
            var parent = Genome.Default();
            var options = MutationSystem.CreateStrategicMutationChoices(
                parent,
                new BehaviourBias(),
                GeneticTraits.Electric,
                new SystemRandom(42));

            Assert.AreEqual(3, options.Length);
            Assert.Greater(options[0].Attack, parent.Attack);
            Assert.Greater(options[0].Spikes, parent.Spikes);
            Assert.IsTrue(options[0].HasTrait(GeneticTraits.Toxic));

            Assert.Greater(options[1].Speed, parent.Speed);
            Assert.Greater(options[1].Flagella, parent.Flagella);
            Assert.IsTrue(options[1].HasTrait(GeneticTraits.Armoured));

            Assert.Greater(options[2].Digestion, parent.Digestion);
            Assert.Less(options[2].MetabolismCost, parent.MetabolismCost);
            Assert.IsTrue(options[2].HasTrait(GeneticTraits.Electric));
        }
    }
}
