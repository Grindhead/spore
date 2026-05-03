using NUnit.Framework;
using System;
using RoobsWorld.Stages.Cell;
using SystemRandom = System.Random;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for BehaviourBiasTracker and MutationSystem.MutateBiased.
    /// </summary>
    public class BehaviourBiasTrackerTests
    {
        // ------------------------------------------------------------------ BehaviourBiasTracker

        [Test]
        public void FreshTracker_AllCountersAreZero()
        {
            var t = new BehaviourBiasTracker();
            Assert.AreEqual(0, t.PlantEaten);
            Assert.AreEqual(0, t.MeatEaten);
            Assert.AreEqual(0, t.ToxinEaten);
            Assert.AreEqual(0, t.CellsConsumed);
            Assert.AreEqual(0, t.DamageTaken);
            Assert.AreEqual(0, t.PredatorsEscaped);
        }

        [Test]
        public void RecordFoodEaten_IncrementsByType()
        {
            var t = new BehaviourBiasTracker();
            t.RecordFoodEaten(FoodType.Plant);
            t.RecordFoodEaten(FoodType.Plant);
            t.RecordFoodEaten(FoodType.Meat);
            t.RecordFoodEaten(FoodType.Toxin);
            Assert.AreEqual(2, t.PlantEaten);
            Assert.AreEqual(1, t.MeatEaten);
            Assert.AreEqual(1, t.ToxinEaten);
        }

        [Test]
        public void RecordCellConsumed_IncrementsCounter()
        {
            var t = new BehaviourBiasTracker();
            t.RecordCellConsumed();
            t.RecordCellConsumed();
            Assert.AreEqual(2, t.CellsConsumed);
        }

        [Test]
        public void RecordDamageTaken_IncrementsCounter()
        {
            var t = new BehaviourBiasTracker();
            t.RecordDamageTaken();
            t.RecordDamageTaken();
            t.RecordDamageTaken();
            Assert.AreEqual(3, t.DamageTaken);
        }

        [Test]
        public void RecordPredatorEscaped_IncrementsCounter()
        {
            var t = new BehaviourBiasTracker();
            t.RecordPredatorEscaped();
            Assert.AreEqual(1, t.PredatorsEscaped);
        }

        [Test]
        public void Reset_ClearsAllCounters()
        {
            var t = new BehaviourBiasTracker();
            t.RecordFoodEaten(FoodType.Plant);
            t.RecordCellConsumed();
            t.RecordDamageTaken();
            t.RecordPredatorEscaped();
            t.Reset();
            Assert.AreEqual(0, t.PlantEaten);
            Assert.AreEqual(0, t.CellsConsumed);
            Assert.AreEqual(0, t.DamageTaken);
            Assert.AreEqual(0, t.PredatorsEscaped);
        }

        [Test]
        public void ComputeBias_NoActivity_AllBiasesNearZero()
        {
            var t = new BehaviourBiasTracker();
            var b = t.ComputeBias();
            Assert.AreEqual(0f, b.Speed);
            Assert.AreEqual(0f, b.Attack);
            Assert.AreEqual(0f, b.Defence);
            Assert.AreEqual(0f, b.Digestion);
        }

        [Test]
        public void ComputeBias_HeavyPlantEater_FavorsDigestionAndSpeed()
        {
            var t = new BehaviourBiasTracker();
            for (int i = 0; i < 10; i++) t.RecordFoodEaten(FoodType.Plant);
            var b = t.ComputeBias();
            Assert.Greater(b.Digestion, 0f, "Heavy plant diet should bias Digestion up");
            Assert.Greater(b.Speed, 0f, "Heavy plant diet should bias Speed up");
        }

        [Test]
        public void ComputeBias_HeavyCarnivore_FavorsAttackAndSpikes()
        {
            var t = new BehaviourBiasTracker();
            for (int i = 0; i < 8; i++) t.RecordFoodEaten(FoodType.Meat);
            for (int i = 0; i < 5; i++) t.RecordCellConsumed();
            var b = t.ComputeBias();
            Assert.Greater(b.Attack, 0f, "Meat diet should bias Attack up");
            Assert.Greater(b.Spikes, 0f, "Meat diet should bias Spikes up");
        }

        [Test]
        public void ComputeBias_FrequentDamage_FavorsDefence()
        {
            var t = new BehaviourBiasTracker();
            for (int i = 0; i < 10; i++) t.RecordDamageTaken();
            var b = t.ComputeBias();
            Assert.Greater(b.Defence, 0f, "Frequent damage should bias Defence up");
        }

        [Test]
        public void ComputeBias_PredatorEscapes_FavorsSpeedAndFlagella()
        {
            var t = new BehaviourBiasTracker();
            t.RecordPredatorEscaped();
            t.RecordPredatorEscaped();
            var b = t.ComputeBias();
            Assert.Greater(b.Speed, 0f, "Predator escapes should bias Speed up");
            Assert.Greater(b.Flagella, 0f, "Predator escapes should bias Flagella up");
        }

        [Test]
        public void GetBiasDescription_NoActivity_ReturnsNull()
        {
            var t = new BehaviourBiasTracker();
            Assert.IsNull(t.GetBiasDescription());
        }

        [Test]
        public void GetBiasDescription_HeavyCarnivore_ReturnsNonNull()
        {
            var t = new BehaviourBiasTracker();
            for (int i = 0; i < 8; i++) t.RecordFoodEaten(FoodType.Meat);
            Assert.IsNotNull(t.GetBiasDescription());
        }

        // ------------------------------------------------------------------ MutationSystem.MutateBiased

        [Test]
        public void MutateBiased_NullParent_ReturnsDefault()
        {
            var g = MutationSystem.MutateBiased(null, default, new SystemRandom(1));
            Assert.NotNull(g);
        }

        [Test]
        public void MutateBiased_ClampsTraitsBetween0And10()
        {
            var parent = new Genome { Speed = 9.9f, Attack = 9.9f, Defence = 0.1f };
            var strongBias = new BehaviourBias { Speed = 1f, Attack = 1f, Defence = 1f };
            for (int seed = 0; seed < 30; seed++)
            {
                var child = MutationSystem.MutateBiased(parent, strongBias, new SystemRandom(seed));
                Assert.LessOrEqual(child.Speed, 10f);
                Assert.GreaterOrEqual(child.Speed, 0f);
                Assert.LessOrEqual(child.Attack, 10f);
                Assert.GreaterOrEqual(child.Attack, 0f);
            }
        }

        [Test]
        public void MutateBiased_PositiveBias_TendencyToIncreaseTrait()
        {
            // With a strong positive attack bias, across many seeds more children should have
            // attack >= parent than in the unbiased case.
            var parent = new Genome { Attack = 3f };
            var bias   = new BehaviourBias { Attack = 1f };
            int biasedIncreased = 0;
            int unbiasedIncreased = 0;

            for (int seed = 0; seed < 100; seed++)
            {
                var biased   = MutationSystem.MutateBiased(parent, bias,    new SystemRandom(seed));
                var unbiased = MutationSystem.Mutate(parent, new SystemRandom(seed));
                if (biased.Attack   >= parent.Attack) biasedIncreased++;
                if (unbiased.Attack >= parent.Attack) unbiasedIncreased++;
            }

            Assert.GreaterOrEqual(biasedIncreased, unbiasedIncreased,
                "Positive attack bias should result in at least as many increases as unbiased");
        }

        [Test]
        public void MutateBiased_WithSameSeed_IsDeterministic()
        {
            var parent = new Genome { Speed = 4f, Attack = 5f };
            var bias   = new BehaviourBias { Speed = 0.5f, Attack = 0.8f };
            var a = MutationSystem.MutateBiased(parent, bias, new SystemRandom(77));
            var b = MutationSystem.MutateBiased(parent, bias, new SystemRandom(77));
            Assert.AreEqual(a.Speed,  b.Speed);
            Assert.AreEqual(a.Attack, b.Attack);
        }
    }
}
