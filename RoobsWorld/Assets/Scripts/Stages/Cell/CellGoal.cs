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

        public GoalType Type { get; }
        public FoodType TargetFoodType { get; }
        public int TargetCount { get; }
        public int CurrentCount { get; }
        /// <summary>Target time in seconds for SurviveInToxicZone goals.</summary>
        public float TargetSeconds { get; }
        /// <summary>Elapsed seconds for SurviveInToxicZone goals.</summary>
        public float ElapsedSeconds { get; }
        public bool IsComplete => Type == GoalType.SurviveInToxicZone
            ? ElapsedSeconds >= TargetSeconds && TargetSeconds > 0f
            : CurrentCount >= TargetCount && TargetCount > 0;

        private CellGoal(GoalType type, FoodType targetFoodType, int targetCount, int currentCount,
                         float targetSeconds = 0f, float elapsedSeconds = 0f)
        {
            Type = type;
            TargetFoodType = targetFoodType;
            TargetCount = targetCount;
            CurrentCount = currentCount;
            TargetSeconds = targetSeconds;
            ElapsedSeconds = elapsedSeconds;
        }

        public static CellGoal CreateEatFoodGoal(FoodType foodType, int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatFood, foodType, targetCount, 0);
        }

        /// <summary>Creates a micro goal: escape from a predator N times (e.g. get out of threat range).</summary>
        public static CellGoal CreateEscapePredatorGoal(int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EscapePredator, FoodType.Plant, targetCount, 0);
        }

        /// <summary>Creates a micro goal: reproduce N times (player chooses mutation and spawns offspring).</summary>
        public static CellGoal CreateReproduceOnceGoal(int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.ReproduceOnce, FoodType.Plant, targetCount, 0);
        }

        /// <summary>Creates a micro goal: survive N seconds inside the toxic biome zone.</summary>
        public static CellGoal CreateSurviveInToxicGoal(float targetSeconds)
        {
            if (targetSeconds < 0f) targetSeconds = 0f;
            return new CellGoal(GoalType.SurviveInToxicZone, FoodType.Plant, 0, 0, targetSeconds, 0f);
        }

        /// <summary>Creates a micro goal: eat N food items while a NutrientBloom event is active.</summary>
        public static CellGoal CreateEatDuringBloomGoal(int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatDuringNutrientBloom, FoodType.Plant, targetCount, 0);
        }

        /// <summary>Creates a micro goal: consume N NPC cells that are visually larger than the player.</summary>
        public static CellGoal CreateEatLargerPreyGoal(int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.EatLargerPrey, FoodType.Plant, targetCount, 0);
        }

        /// <summary>Creates a micro goal: collect N cell part pickups.</summary>
        public static CellGoal CreateCollectPartsGoal(int targetCount)
        {
            if (targetCount < 0) targetCount = 0;
            return new CellGoal(GoalType.CollectParts, FoodType.Plant, targetCount, 0);
        }

        /// <summary>
        /// Returns the next micro goal after the provided goal completes, or null if there is no next goal.
        /// Centralizes progression so CellStage and tests share a single source of truth.
        /// </summary>
        public static CellGoal? GetNextGoalAfterCompletion(CellGoal completedGoal)
        {
            if (completedGoal.Type == GoalType.EatFood)
            {
                return completedGoal.TargetFoodType == FoodType.Plant
                    ? CreateEatFoodGoal(FoodType.Meat, 3)
                    : completedGoal.TargetFoodType == FoodType.Meat
                        ? CreateEatFoodGoal(FoodType.Toxin, 2)
                        : completedGoal.TargetFoodType == FoodType.Toxin
                            ? CreateEscapePredatorGoal(1)
                            : null;
            }

            if (completedGoal.Type == GoalType.EscapePredator)
                return CreateReproduceOnceGoal(1);

            if (completedGoal.Type == GoalType.ReproduceOnce)
                return CreateSurviveInToxicGoal(10f);

            if (completedGoal.Type == GoalType.SurviveInToxicZone)
                return CreateEatDuringBloomGoal(3);

            if (completedGoal.Type == GoalType.EatDuringNutrientBloom)
                return CreateEatLargerPreyGoal(1);

            if (completedGoal.Type == GoalType.EatLargerPrey)
                return CreateCollectPartsGoal(2);

            return null;
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

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount);
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

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount);
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

            return new CellGoal(Type, TargetFoodType, TargetCount, nextCount);
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
            return new CellGoal(Type, TargetFoodType, TargetCount, CurrentCount, TargetSeconds, next);
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
            return new CellGoal(Type, TargetFoodType, TargetCount, next);
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
            return new CellGoal(Type, TargetFoodType, TargetCount, next);
        }

        /// <summary>
        /// Returns a new CellGoal with progress incremented when the player collects a cell part pickup.
        /// Only applies when the goal type is CollectParts.
        /// </summary>
        public CellGoal ApplyCollectedPart()
        {
            if (Type != GoalType.CollectParts || TargetCount <= 0 || IsComplete) return this;
            int next = System.Math.Min(CurrentCount + 1, TargetCount);
            return new CellGoal(Type, TargetFoodType, TargetCount, next);
        }
    }
}

