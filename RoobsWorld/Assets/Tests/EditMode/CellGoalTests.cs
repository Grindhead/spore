using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class CellGoalTests
    {
        [Test]
        public void NewGoal_StartsWithZeroProgress_AndNotComplete()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Plant, 5);
            Assert.AreEqual(0, goal.CurrentCount);
            Assert.False(goal.IsComplete);
        }

        [Test]
        public void ApplyFoodConsumed_IncrementsOnlyMatchingType()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Plant, 3);

            goal = goal.ApplyFoodConsumed(FoodType.Meat);  // wrong type
            Assert.AreEqual(0, goal.CurrentCount);
            Assert.False(goal.IsComplete);

            goal = goal.ApplyFoodConsumed(FoodType.Plant);
            goal = goal.ApplyFoodConsumed(FoodType.Plant);
            Assert.AreEqual(2, goal.CurrentCount);
            Assert.False(goal.IsComplete);

            goal = goal.ApplyFoodConsumed(FoodType.Plant);
            Assert.AreEqual(3, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyFoodConsumed_DoesNotExceedTarget()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Plant, 2);

            goal = goal.ApplyFoodConsumed(FoodType.Plant);
            goal = goal.ApplyFoodConsumed(FoodType.Plant);
            goal = goal.ApplyFoodConsumed(FoodType.Plant);

            Assert.AreEqual(2, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyFoodConsumed_ToxinGoal_CompletesWhenTargetReached()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Toxin, 2);

            goal = goal.ApplyFoodConsumed(FoodType.Toxin);
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.False(goal.IsComplete);

            goal = goal.ApplyFoodConsumed(FoodType.Toxin);
            Assert.AreEqual(2, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void CreateEscapePredatorGoal_StartsWithZeroProgress_AndNotComplete()
        {
            var goal = CellGoal.CreateEscapePredatorGoal(1);
            Assert.AreEqual(CellGoal.GoalType.EscapePredator, goal.Type);
            Assert.AreEqual(0, goal.CurrentCount);
            Assert.AreEqual(1, goal.TargetCount);
            Assert.False(goal.IsComplete);
        }

        [Test]
        public void ApplyEscapedPredator_IncrementsAndCompletesWhenTargetReached()
        {
            var goal = CellGoal.CreateEscapePredatorGoal(1);

            goal = goal.ApplyEscapedPredator();
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyEscapedPredator_DoesNotExceedTarget()
        {
            var goal = CellGoal.CreateEscapePredatorGoal(1);

            goal = goal.ApplyEscapedPredator();
            goal = goal.ApplyEscapedPredator();
            goal = goal.ApplyEscapedPredator();

            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyEscapedPredator_DoesNothingForEatFoodGoal()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Plant, 5);
            var next = goal.ApplyEscapedPredator();
            Assert.AreEqual(0, next.CurrentCount);
            Assert.False(next.IsComplete);
        }

        [Test]
        public void CreateReproduceOnceGoal_StartsWithZeroProgress_AndNotComplete()
        {
            var goal = CellGoal.CreateReproduceOnceGoal(1);
            Assert.AreEqual(CellGoal.GoalType.ReproduceOnce, goal.Type);
            Assert.AreEqual(0, goal.CurrentCount);
            Assert.AreEqual(1, goal.TargetCount);
            Assert.False(goal.IsComplete);
        }

        [Test]
        public void ApplyReproduced_IncrementsAndCompletesWhenTargetReached()
        {
            var goal = CellGoal.CreateReproduceOnceGoal(1);
            goal = goal.ApplyReproduced();
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyReproduced_DoesNotExceedTarget()
        {
            var goal = CellGoal.CreateReproduceOnceGoal(1);
            goal = goal.ApplyReproduced();
            goal = goal.ApplyReproduced();
            goal = goal.ApplyReproduced();
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyReproduced_DoesNothingForEatFoodGoal()
        {
            var goal = CellGoal.CreateEatFoodGoal(FoodType.Plant, 5);
            var next = goal.ApplyReproduced();
            Assert.AreEqual(0, next.CurrentCount);
            Assert.False(next.IsComplete);
        }

        [Test]
        public void GetNextGoalAfterCompletion_FollowsExpectedCellStageProgression()
        {
            // Eat 5 algae → Eat 3 dead cells → Eat 2 toxin → Escape 1 predator → Reproduce 1 time
            //   → Survive 10s Toxic → Eat 3 during Bloom → Eat 1 prey larger than you → null
            var eatPlants = CellGoal.CreateEatFoodGoal(FoodType.Plant, 5);
            var eatMeat = CellGoal.GetNextGoalAfterCompletion(eatPlants);
            Assert.True(eatMeat.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EatFood, eatMeat.Value.Type);
            Assert.AreEqual(FoodType.Meat, eatMeat.Value.TargetFoodType);

            var eatToxin = CellGoal.GetNextGoalAfterCompletion(eatMeat.Value);
            Assert.True(eatToxin.HasValue);
            Assert.AreEqual(FoodType.Toxin, eatToxin.Value.TargetFoodType);

            var escape = CellGoal.GetNextGoalAfterCompletion(eatToxin.Value);
            Assert.True(escape.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EscapePredator, escape.Value.Type);

            var reproduce = CellGoal.GetNextGoalAfterCompletion(escape.Value);
            Assert.True(reproduce.HasValue);
            Assert.AreEqual(CellGoal.GoalType.ReproduceOnce, reproduce.Value.Type);

            var toxic = CellGoal.GetNextGoalAfterCompletion(reproduce.Value);
            Assert.True(toxic.HasValue);
            Assert.AreEqual(CellGoal.GoalType.SurviveInToxicZone, toxic.Value.Type);
            Assert.Greater(toxic.Value.TargetSeconds, 0f);

            var bloom = CellGoal.GetNextGoalAfterCompletion(toxic.Value);
            Assert.True(bloom.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EatDuringNutrientBloom, bloom.Value.Type);
            Assert.Greater(bloom.Value.TargetCount, 0);

            var eatLarger = CellGoal.GetNextGoalAfterCompletion(bloom.Value);
            Assert.True(eatLarger.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EatLargerPrey, eatLarger.Value.Type);
            Assert.Greater(eatLarger.Value.TargetCount, 0);

            var collect = CellGoal.GetNextGoalAfterCompletion(eatLarger.Value);
            Assert.True(collect.HasValue);
            Assert.AreEqual(CellGoal.GoalType.CollectParts, collect.Value.Type);
            Assert.Greater(collect.Value.TargetCount, 0);

            var none = CellGoal.GetNextGoalAfterCompletion(collect.Value);
            Assert.False(none.HasValue);
        }

        [Test]
        public void ApplySurviveInToxic_AccumulatesTimeOnlyWhenInToxicZone()
        {
            var goal = CellGoal.CreateSurviveInToxicGoal(10f);
            Assert.AreEqual(0f, goal.ElapsedSeconds);
            Assert.False(goal.IsComplete);

            goal = goal.ApplySurviveInToxic(3f, isInToxicZone: false);
            Assert.AreEqual(0f, goal.ElapsedSeconds, "Should not accumulate outside toxic zone");

            goal = goal.ApplySurviveInToxic(5f, isInToxicZone: true);
            Assert.AreEqual(5f, goal.ElapsedSeconds, 0.001f);
            Assert.False(goal.IsComplete);

            goal = goal.ApplySurviveInToxic(5f, isInToxicZone: true);
            Assert.GreaterOrEqual(goal.ElapsedSeconds, 10f);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplySurviveInToxic_ClampsToTarget()
        {
            var goal = CellGoal.CreateSurviveInToxicGoal(5f);
            goal = goal.ApplySurviveInToxic(100f, isInToxicZone: true);
            Assert.AreEqual(5f, goal.ElapsedSeconds, 0.001f);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyEatDuringBloom_IncrementsOnlyWhenBloomActive()
        {
            var goal = CellGoal.CreateEatDuringBloomGoal(3);
            Assert.AreEqual(0, goal.CurrentCount);

            goal = goal.ApplyEatDuringBloom(isBloomActive: false);
            Assert.AreEqual(0, goal.CurrentCount, "Should not count without bloom");

            goal = goal.ApplyEatDuringBloom(isBloomActive: true);
            goal = goal.ApplyEatDuringBloom(isBloomActive: true);
            Assert.AreEqual(2, goal.CurrentCount);
            Assert.False(goal.IsComplete);

            goal = goal.ApplyEatDuringBloom(isBloomActive: true);
            Assert.AreEqual(3, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void ApplyEatDuringBloom_DoesNotExceedTarget()
        {
            var goal = CellGoal.CreateEatDuringBloomGoal(2);
            for (int i = 0; i < 5; i++)
                goal = goal.ApplyEatDuringBloom(true);
            Assert.AreEqual(2, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void EatLargerPrey_OnlyProgressesWhenPreyWasLarger()
        {
            var goal = CellGoal.CreateEatLargerPreyGoal(1);
            Assert.AreEqual(0, goal.CurrentCount);
            Assert.False(goal.IsComplete);

            goal = goal.ApplyAteLargerPrey(preyWasLarger: false);
            Assert.AreEqual(0, goal.CurrentCount, "Should not progress when prey was smaller");

            goal = goal.ApplyAteLargerPrey(preyWasLarger: true);
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void EatLargerPrey_DoesNotExceedTarget()
        {
            var goal = CellGoal.CreateEatLargerPreyGoal(1);
            for (int i = 0; i < 5; i++)
                goal = goal.ApplyAteLargerPrey(true);
            Assert.AreEqual(1, goal.CurrentCount);
            Assert.True(goal.IsComplete);
        }

        [Test]
        public void EatLargerPrey_LocalizationKey_HasValue()
        {
            string val = RoobsWorld.Core.Localization.Get("cell.goal.eat_larger_task");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.goal.eat_larger_task", val);
        }
    }
}

