namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Immutable description of a simple cell-stage micro goal (e.g., eat N algae).
    /// Pure data + logic so it is easy to test and to drive UI/event hooks.
    /// </summary>
    public readonly struct CellGoal
    {
        public enum GoalType
        {
            EatFood,
            EscapePredator,
            ReproduceOnce,
            SurviveInToxicZone,    // survive N seconds inside the toxic biome
            EatDuringNutrientBloom, // eat N food items while a NutrientBloom event is active
            EatLargerPrey,         // consume N NPC cells that are visually larger than the player
            CollectParts            // collect N cell part pickups
        }

        public enum GoalActionCategory
        {
            Feeding,
            Escaping,
            Reproducing,
            Exploring,
            Hunting,
            Upgrading
        }

        public GoalType Type { get; }
        public FoodType TargetFoodType { get; }
        public int TargetCount { get; }
        public int CurrentCount { get; }
        public int RewardMultiplier { get; }
        public bool IsRepeatable { get; }
        /// <summary>Target time in seconds for SurviveInToxicZone goals.</summary>
        public float TargetSeconds { get; }
        /// <summary>Elapsed seconds for SurviveInToxicZone goals.</summary>
        public float ElapsedSeconds { get; }
        public bool IsComplete => Type == GoalType.SurviveInToxicZone
            ? ElapsedSeconds >= TargetSeconds && TargetSeconds > 0f
            : CurrentCount >= TargetCount && TargetCount > 0;
        public GoalActionCategory ActionCategory => GetActionCategory(this);

        private CellGoal(
            GoalType type,
            FoodType targetFoodType,
            int targetCount,
            int currentCount,
            float targetSeconds = 0f,
            float elapsedSeconds = 0f,
            int rewardMultiplier = 1,
            bool isRepeatable = false)
        {
            Type = type;
            TargetFoodType = targetFoodType;
            TargetCount = targetCount;
            CurrentCount = currentCount;
            TargetSeconds = targetSeconds;
            ElapsedSeconds = elapsedSeconds;
            RewardMultiplier = System.Math.Max(1, rewardMultiplier);
            IsRepeatable = isRepeatable;
        }

        public CellGoal WithRewardMultiplier(int rewardMultiplier)
        {
            return new CellGoal(Type, TargetFoodType, TargetCount, CurrentCount, TargetSeconds, ElapsedSeconds, rewardMultiplier, IsRepeatable);
        }

        public static CellGoal CreateEatFoodGoal(
            FoodType foodType,
            int targetCount,
            int rewardMultiplier = 1,
            bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatFood, foodType, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>Creates a micro goal: escape from a predator N times (e.g. get out of threat range).</summary>
        public static CellGoal CreateEscapePredatorGoal(int targetCount, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EscapePredator, FoodType.Plant, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>Creates a micro goal: reproduce N times (player chooses mutation and spawns offspring).</summary>
        public static CellGoal CreateReproduceOnceGoal(int targetCount, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.ReproduceOnce, FoodType.Plant, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>Creates a micro goal: survive N seconds inside the toxic biome zone.</summary>
        public static CellGoal CreateSurviveInToxicGoal(float targetSeconds, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetSeconds < 0f) targetSeconds = 0f;
            return new CellGoal(GoalType.SurviveInToxicZone, FoodType.Plant, 0, 0, targetSeconds, 0f, rewardMultiplier, isRepeatable);
        }

        /// <summary>Creates a micro goal: eat N food items while a NutrientBloom event is active.</summary>
        public static CellGoal CreateEatDuringBloomGoal(int targetCount, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatDuringNutrientBloom, FoodType.Plant, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>Creates a micro goal: consume N NPC cells that are visually larger than the player.</summary>
        public static CellGoal CreateEatLargerPreyGoal(int targetCount, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatLargerPrey, FoodType.Plant, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>Creates a micro goal: collect N cell part pickups.</summary>
        public static CellGoal CreateCollectPartsGoal(int targetCount, int rewardMultiplier = 1, bool isRepeatable = false)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.CollectParts, FoodType.Plant, targetCount, 0, rewardMultiplier: rewardMultiplier, isRepeatable: isRepeatable);
        }

        /// <summary>
        /// Returns the next micro goal after the provided goal completes, or null if there is no next goal.
        /// Centralizes progression so CellStage and tests share a single source of truth.
        /// </summary>
        public static CellGoal? GetNextGoalAfterCompletion(CellGoal completedGoal)
            => GetNextGoalAfterCompletion(completedGoal, 0);

        /// <summary>
        /// Returns the next authored goal, then falls into a repeatable weighted deck when the authored chain ends.
        /// </summary>
        public static CellGoal? GetNextGoalAfterCompletion(CellGoal completedGoal, int completedGoalCount)
        {
            if (completedGoal.IsRepeatable)
                return SelectRepeatableGoal(completedGoal.ActionCategory, completedGoalCount);

            if (completedGoal.Type == GoalType.EatFood)
            {
                return completedGoal.TargetFoodType == FoodType.Plant
                    ? CreateEscapePredatorGoal(1)
                    : completedGoal.TargetFoodType == FoodType.Meat
                        ? CreateReproduceOnceGoal(1)
                        : completedGoal.TargetFoodType == FoodType.Toxin
                            ? SelectRepeatableGoal(completedGoal.ActionCategory, completedGoalCount)
                            : null;
            }

            if (completedGoal.Type == GoalType.EscapePredator)
                return CreateEatFoodGoal(FoodType.Meat, 3);

            if (completedGoal.Type == GoalType.ReproduceOnce)
                return CreateSurviveInToxicGoal(10f);

            if (completedGoal.Type == GoalType.SurviveInToxicZone)
                return CreateEatDuringBloomGoal(3);

            if (completedGoal.Type == GoalType.EatDuringNutrientBloom)
                return CreateEatLargerPreyGoal(1);

            if (completedGoal.Type == GoalType.EatLargerPrey)
                return CreateCollectPartsGoal(2);

            if (completedGoal.Type == GoalType.CollectParts)
                return CreateEatFoodGoal(FoodType.Toxin, 2);

            return null;
        }

        public readonly struct RepeatableGoalDeckEntry
        {
            public readonly CellGoal Goal;
            public readonly int Weight;

            public RepeatableGoalDeckEntry(CellGoal goal, int weight)
            {
                Goal = goal;
                Weight = System.Math.Max(1, weight);
            }
        }

        public static RepeatableGoalDeckEntry[] GetRepeatableGoalDeck()
        {
            return new[]
            {
                new RepeatableGoalDeckEntry(CreateEatFoodGoal(FoodType.Plant, 4, isRepeatable: true), 2),
                new RepeatableGoalDeckEntry(CreateEscapePredatorGoal(1, isRepeatable: true), 2),
                new RepeatableGoalDeckEntry(CreateEatFoodGoal(FoodType.Toxin, 3, rewardMultiplier: 3, isRepeatable: true), 2),
                new RepeatableGoalDeckEntry(CreateEatDuringBloomGoal(4, rewardMultiplier: 2, isRepeatable: true), 3),
                new RepeatableGoalDeckEntry(CreateEatLargerPreyGoal(2, rewardMultiplier: 3, isRepeatable: true), 2),
                new RepeatableGoalDeckEntry(CreateCollectPartsGoal(2, rewardMultiplier: 2, isRepeatable: true), 1),
                new RepeatableGoalDeckEntry(CreateSurviveInToxicGoal(12f, rewardMultiplier: 2, isRepeatable: true), 1)
            };
        }

        public static CellGoal SelectRepeatableGoal(GoalActionCategory previousCategory, int completedGoalCount)
        {
            var deck = GetRepeatableGoalDeck();
            int totalWeight = 0;
            for (int i = 0; i < deck.Length; i++)
            {
                if (deck[i].Goal.ActionCategory != previousCategory)
                    totalWeight += deck[i].Weight;
            }

            bool allowRepeatCategory = totalWeight <= 0;
            if (allowRepeatCategory)
            {
                for (int i = 0; i < deck.Length; i++)
                    totalWeight += deck[i].Weight;
            }

            int seed = System.Math.Abs(completedGoalCount * 73 + ((int)previousCategory + 1) * 19);
            int roll = totalWeight > 0 ? seed % totalWeight : 0;
            for (int i = 0; i < deck.Length; i++)
            {
                if (!allowRepeatCategory && deck[i].Goal.ActionCategory == previousCategory)
                    continue;

                roll -= deck[i].Weight;
                if (roll < 0)
                    return ApplyRepeatableRewardRamp(deck[i].Goal, completedGoalCount);
            }

            return ApplyRepeatableRewardRamp(deck[0].Goal, completedGoalCount);
        }

        public static bool IsRepeatableRiskRoute(CellGoal goal)
        {
            return goal.IsRepeatable && goal.RewardMultiplier >= RoobsWorld.Core.BalanceConstants.CellRepeatableRiskGoalRewardMultiplier;
        }

        public static int ComputeRepeatableRewardRampBonus(int completedGoalCount, int rampStartGoalCount, int tierInterval, int maxBonus)
        {
            int safeMaxBonus = System.Math.Max(0, maxBonus);
            if (safeMaxBonus == 0) return 0;

            int safeStart = System.Math.Max(0, rampStartGoalCount);
            int safeInterval = System.Math.Max(1, tierInterval);
            int completedAfterStart = System.Math.Max(0, completedGoalCount - safeStart);
            int rawBonus = completedAfterStart / safeInterval;
            return System.Math.Min(safeMaxBonus, rawBonus);
        }

        public static CellGoal ApplyRepeatableRewardRamp(CellGoal goal, int completedGoalCount)
        {
            if (!IsRepeatableRiskRoute(goal))
                return goal;

            int bonus = ComputeRepeatableRewardRampBonus(
                completedGoalCount,
                RoobsWorld.Core.BalanceConstants.CellRepeatableRewardRampStartGoalCount,
                RoobsWorld.Core.BalanceConstants.CellRepeatableRewardRampInterval,
                RoobsWorld.Core.BalanceConstants.CellRepeatableRewardRampMaxBonus);

            return bonus <= 0 ? goal : goal.WithRewardMultiplier(goal.RewardMultiplier + bonus);
        }

        public static GoalActionCategory GetActionCategory(CellGoal goal)
        {
            return goal.Type switch
            {
                GoalType.EatFood when goal.TargetFoodType == FoodType.Meat => GoalActionCategory.Hunting,
                GoalType.EatFood when goal.TargetFoodType == FoodType.Toxin => GoalActionCategory.Exploring,
                GoalType.EatFood => GoalActionCategory.Feeding,
                GoalType.EscapePredator => GoalActionCategory.Escaping,
                GoalType.ReproduceOnce => GoalActionCategory.Reproducing,
                GoalType.SurviveInToxicZone => GoalActionCategory.Exploring,
                GoalType.EatDuringNutrientBloom => GoalActionCategory.Feeding,
                GoalType.EatLargerPrey => GoalActionCategory.Hunting,
                GoalType.CollectParts => GoalActionCategory.Upgrading,
                _ => GoalActionCategory.Feeding
            };
        }

        /// <summary>
        /// Returns a new CellGoal with progress updated for a food-consumed event.
        /// Only increments when the consumed type matches the goal's target type.
        /// </summary>
        public CellGoal ApplyFoodConsumed(FoodType consumedType)
        {
            if (Type != GoalType.EatFood || TargetCount <= 0 || IsComplete)
                return this;

            if (consumedType != TargetFoodType)
                return this;

            int nextCount = CurrentCount + 1;
            if (nextCount > TargetCount)
                nextCount = TargetCount;

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with progress updated for an escape-from-predator event.
        /// Only applies when the goal type is EscapePredator.
        /// </summary>
        public CellGoal ApplyEscapedPredator()
        {
            if (Type != GoalType.EscapePredator || TargetCount <= 0 || IsComplete)
                return this;

            int nextCount = CurrentCount + 1;
            if (nextCount > TargetCount)
                nextCount = TargetCount;

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with progress updated for a reproduction event.
        /// Only applies when the goal type is ReproduceOnce.
        /// </summary>
        public CellGoal ApplyReproduced()
        {
            if (Type != GoalType.ReproduceOnce || TargetCount <= 0 || IsComplete)
                return this;

            int nextCount = CurrentCount + 1;
            if (nextCount > TargetCount)
                nextCount = TargetCount;

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with time accumulated while the player is inside the toxic zone.
        /// Only applies when the goal type is SurviveInToxicZone.
        /// </summary>
        public CellGoal ApplySurviveInToxic(float deltaTime, bool isInToxicZone)
        {
            if (Type != GoalType.SurviveInToxicZone || TargetSeconds <= 0f || IsComplete) return this;
            if (!isInToxicZone) return this;
            float next = System.Math.Min(ElapsedSeconds + deltaTime, TargetSeconds);
            return new CellGoal(Type, TargetFoodType, TargetCount, CurrentCount, TargetSeconds, next, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with eat-count incremented when a NutrientBloom is active.
        /// Only applies when the goal type is EatDuringNutrientBloom.
        /// </summary>
        public CellGoal ApplyEatDuringBloom(bool isBloomActive)
        {
            if (Type != GoalType.EatDuringNutrientBloom || TargetCount <= 0 || IsComplete) return this;
            if (!isBloomActive) return this;
            int next = System.Math.Min(CurrentCount + 1, TargetCount);
            return new CellGoal(Type, TargetFoodType, TargetCount, next, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with progress incremented when the player consumes a cell
        /// that was visually larger than themselves (<paramref name="preyWasLarger"/> = true).
        /// Only applies when the goal type is EatLargerPrey.
        /// </summary>
        public CellGoal ApplyAteLargerPrey(bool preyWasLarger)
        {
            if (Type != GoalType.EatLargerPrey || TargetCount <= 0 || IsComplete) return this;
            if (!preyWasLarger) return this;
            int next = System.Math.Min(CurrentCount + 1, TargetCount);
            return new CellGoal(Type, TargetFoodType, TargetCount, next, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }

        /// <summary>
        /// Returns a new CellGoal with progress incremented when the player collects a cell part pickup.
        /// Only applies when the goal type is CollectParts.
        /// </summary>
        public CellGoal ApplyCollectedPart()
        {
            if (Type != GoalType.CollectParts || TargetCount <= 0 || IsComplete) return this;
            int next = System.Math.Min(CurrentCount + 1, TargetCount);
            return new CellGoal(Type, TargetFoodType, TargetCount, next, TargetSeconds, ElapsedSeconds, RewardMultiplier, IsRepeatable);
        }
    }
}

