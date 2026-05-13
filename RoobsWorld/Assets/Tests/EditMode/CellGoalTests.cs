using NUnit.Framework;
using RoobsWorld.Core;
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
            var escape = CellGoal.GetNextGoalAfterCompletion(eatPlants);
            Assert.True(escape.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EscapePredator, escape.Value.Type);

            var eatMeat = CellGoal.GetNextGoalAfterCompletion(escape.Value);
            Assert.True(eatMeat.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EatFood, eatMeat.Value.Type);
            Assert.AreEqual(FoodType.Meat, eatMeat.Value.TargetFoodType);

            var reproduce = CellGoal.GetNextGoalAfterCompletion(eatMeat.Value);
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

            var eatToxin = CellGoal.GetNextGoalAfterCompletion(collect.Value);
            Assert.True(eatToxin.HasValue);
            Assert.AreEqual(CellGoal.GoalType.EatFood, eatToxin.Value.Type);
            Assert.AreEqual(FoodType.Toxin, eatToxin.Value.TargetFoodType);

            var repeatable = CellGoal.GetNextGoalAfterCompletion(eatToxin.Value, 8);
            Assert.True(repeatable.HasValue);
            Assert.True(repeatable.Value.IsRepeatable);
            Assert.AreNotEqual(eatToxin.Value.ActionCategory, repeatable.Value.ActionCategory);
        }

        [Test]
        public void GetNextGoalAfterCompletion_DoesNotRepeatActionCategories()
        {
            CellGoal? current = CellGoal.CreateEatFoodGoal(FoodType.Plant, 3);
            CellGoal.GoalActionCategory? previousCategory = null;

            for (int i = 0; i < 12 && current.HasValue; i++)
            {
                var category = current.Value.ActionCategory;
                if (previousCategory.HasValue)
                    Assert.AreNotEqual(previousCategory.Value, category);

                previousCategory = category;
                current = CellGoal.GetNextGoalAfterCompletion(current.Value);
            }
        }

        [Test]
        public void RepeatableGoalDeck_ContainsRiskRoutesWithHigherRewards()
        {
            var deck = CellGoal.GetRepeatableGoalDeck();
            bool hasBloomRisk = false;
            bool hasToxinRisk = false;
            bool hasHuntRisk = false;

            for (int i = 0; i < deck.Length; i++)
            {
                var goal = deck[i].Goal;
                Assert.True(goal.IsRepeatable);
                Assert.Greater(deck[i].Weight, 0);

                if (goal.Type == CellGoal.GoalType.EatDuringNutrientBloom && goal.RewardMultiplier > 1)
                    hasBloomRisk = true;
                if (goal.Type == CellGoal.GoalType.EatFood && goal.TargetFoodType == FoodType.Toxin && goal.RewardMultiplier > 1)
                    hasToxinRisk = true;
                if (goal.Type == CellGoal.GoalType.EatLargerPrey && goal.RewardMultiplier > 1)
                    hasHuntRisk = true;
            }

            Assert.True(hasBloomRisk);
            Assert.True(hasToxinRisk);
            Assert.True(hasHuntRisk);
        }

        [Test]
        public void SelectRepeatableGoal_AvoidsPreviousActionCategory()
        {
            for (int i = 0; i < 24; i++)
            {
                var next = CellGoal.SelectRepeatableGoal(CellGoal.GoalActionCategory.Feeding, i);
                Assert.AreNotEqual(CellGoal.GoalActionCategory.Feeding, next.ActionCategory);
                Assert.True(next.IsRepeatable);
            }
        }

        [Test]
        public void ComputeRepeatableRewardRampBonus_StartsLateAndCaps()
        {
            Assert.AreEqual(0, CellGoal.ComputeRepeatableRewardRampBonus(8, 9, 4, 2));
            Assert.AreEqual(0, CellGoal.ComputeRepeatableRewardRampBonus(12, 9, 4, 2));
            Assert.AreEqual(1, CellGoal.ComputeRepeatableRewardRampBonus(13, 9, 4, 2));
            Assert.AreEqual(2, CellGoal.ComputeRepeatableRewardRampBonus(30, 9, 4, 2));
            Assert.AreEqual(0, CellGoal.ComputeRepeatableRewardRampBonus(30, 9, 4, 0));
        }

        [Test]
        public void ApplyRepeatableRewardRamp_RaisesRiskRoutesAndLeavesSafeRoutesStable()
        {
            var toxin = CellGoal.CreateEatFoodGoal(FoodType.Toxin, 3, rewardMultiplier: 3, isRepeatable: true);
            var plant = CellGoal.CreateEatFoodGoal(FoodType.Plant, 4, isRepeatable: true);
            var escape = CellGoal.CreateEscapePredatorGoal(1, isRepeatable: true);

            Assert.True(CellGoal.IsRepeatableRiskRoute(toxin));
            Assert.False(CellGoal.IsRepeatableRiskRoute(plant));
            Assert.False(CellGoal.IsRepeatableRiskRoute(escape));
            Assert.AreEqual(3, CellGoal.ApplyRepeatableRewardRamp(toxin, 9).RewardMultiplier);
            Assert.AreEqual(4, CellGoal.ApplyRepeatableRewardRamp(toxin, 13).RewardMultiplier);
            Assert.AreEqual(5, CellGoal.ApplyRepeatableRewardRamp(toxin, 80).RewardMultiplier);
            Assert.AreEqual(1, CellGoal.ApplyRepeatableRewardRamp(plant, 80).RewardMultiplier);
            Assert.AreEqual(1, CellGoal.ApplyRepeatableRewardRamp(escape, 80).RewardMultiplier);
        }

        [Test]
        public void SelectRepeatableGoal_AppliesLateRunRampOnlyToRiskRoutes()
        {
            bool sawSafeRecovery = false;
            bool sawRampedRisk = false;

            for (int i = 30; i < 130; i++)
            {
                var next = CellGoal.SelectRepeatableGoal(CellGoal.GoalActionCategory.Feeding, i);
                if (CellGoal.IsRepeatableRiskRoute(next))
                {
                    Assert.GreaterOrEqual(next.RewardMultiplier, BalanceConstants.CellRepeatableRiskGoalRewardMultiplier + BalanceConstants.CellRepeatableRewardRampMaxBonus);
                    sawRampedRisk = true;
                }
                else
                {
                    Assert.AreEqual(1, next.RewardMultiplier);
                    sawSafeRecovery = true;
                }
            }

            Assert.True(sawSafeRecovery);
            Assert.True(sawRampedRisk);
        }

        [Test]
        public void GoalRewardSuffix_ShowsRiskRewardMultiplier()
        {
            var goal = CellGoal.CreateEatLargerPreyGoal(2, rewardMultiplier: 3, isRepeatable: true);

            string suffix = CellStageHUD.BuildGoalRewardSuffix(
                goal,
                BalanceConstants.CellDNAPointsPerGoalComplete);

            StringAssert.Contains("Risk reward", suffix);
            StringAssert.Contains("+9 DNA", suffix);
        }

        [Test]
        public void GoalRewardSuffix_ReflectsLateRunRiskRamp()
        {
            var goal = CellGoal.ApplyRepeatableRewardRamp(
                CellGoal.CreateEatFoodGoal(FoodType.Toxin, 3, rewardMultiplier: 3, isRepeatable: true),
                80);

            string suffix = CellStageHUD.BuildGoalRewardSuffix(
                goal,
                BalanceConstants.CellDNAPointsPerGoalComplete);

            StringAssert.Contains("Risk reward", suffix);
            StringAssert.Contains("+15 DNA", suffix);
        }

        [Test]
        public void GoalCompletedText_IncludesVarietyBonusWhenPresent()
        {
            var evt = new CellGoalCompletedEvent
            {
                DnaReward = 7,
                RewardMultiplier = 1,
                VarietyChain = 3,
                VarietyBonusDna = 2
            };

            string text = CellStageHUD.BuildGoalCompletedText(evt);

            StringAssert.Contains("Goal complete", text);
            StringAssert.Contains("+7 DNA", text);
            StringAssert.Contains("Variety x3 +2 DNA", text);
        }

        [Test]
        public void GoalCompletedText_UsesRiskMessageWithoutVarietyNoise()
        {
            var evt = new CellGoalCompletedEvent
            {
                DnaReward = 9,
                RewardMultiplier = BalanceConstants.CellRepeatableRiskGoalRewardMultiplier
            };

            string text = CellStageHUD.BuildGoalCompletedText(evt);

            StringAssert.Contains("Risk route complete", text);
            StringAssert.Contains("+9 DNA", text);
            Assert.False(text.Contains("Variety"));
        }

        [Test]
        public void GoalGuidanceSuffix_NamesSeededOpportunity()
        {
            Assert.AreEqual("  |  Nearby food", CellStageHUD.BuildGoalGuidanceSuffix(CellGoal.CreateEatFoodGoal(FoodType.Plant, 3)));
            Assert.AreEqual("  |  Toxic trail", CellStageHUD.BuildGoalGuidanceSuffix(CellGoal.CreateEatFoodGoal(FoodType.Toxin, 2)));
            Assert.AreEqual("  |  Orange prey", CellStageHUD.BuildGoalGuidanceSuffix(CellGoal.CreateEatLargerPreyGoal(1)));
            Assert.AreEqual("  |  Nearby part", CellStageHUD.BuildGoalGuidanceSuffix(CellGoal.CreateCollectPartsGoal(1)));
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

