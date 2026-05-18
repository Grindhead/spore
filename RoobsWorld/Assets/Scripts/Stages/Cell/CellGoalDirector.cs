using System;

namespace RoobsWorld.Stages.Cell
{
    public sealed class CellGoalDirector
    {
        public void UpdateGoals(Action updateToxicSurvivalGoal, Action updateEscapePredatorGoal)
        {
            updateToxicSurvivalGoal?.Invoke();
            updateEscapePredatorGoal?.Invoke();
        }
    }
}
