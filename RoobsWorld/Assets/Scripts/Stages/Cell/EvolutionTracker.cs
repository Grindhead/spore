using System;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tracks mass thresholds for successive cell evolutions and raises events when each level is reached.
    /// </summary>
    public sealed class EvolutionTracker
    {
        private readonly float _thresholdMass;
        private readonly int _maxEvolutions;

        public int CurrentLevel { get; private set; }
        public bool IsFinalEvolution => CurrentLevel >= _maxEvolutions;

        public event Action<int> OnEvolutionReached;

        public EvolutionTracker(float thresholdMass, int maxEvolutions)
        {
            _thresholdMass = thresholdMass;
            _maxEvolutions = maxEvolutions;
        }

        public void UpdateMass(float currentMass)
        {
            UpdateMass(currentMass, int.MaxValue, 1);
        }

        public void UpdateMass(float currentMass, int consumedFoodCount, int foodRequiredPerEvolution)
        {
            if (IsFinalEvolution) return;

            int targetLevelByMass = (int)(currentMass / _thresholdMass);
            int targetLevelByFood = foodRequiredPerEvolution <= 0
                ? _maxEvolutions
                : consumedFoodCount / foodRequiredPerEvolution;

            int targetLevel = targetLevelByMass;
            if (targetLevel > targetLevelByFood)
                targetLevel = targetLevelByFood;
            if (targetLevel > _maxEvolutions)
                targetLevel = _maxEvolutions;

            while (CurrentLevel < targetLevel)
            {
                CurrentLevel++;
                OnEvolutionReached?.Invoke(CurrentLevel);
            }
        }
    }
}
