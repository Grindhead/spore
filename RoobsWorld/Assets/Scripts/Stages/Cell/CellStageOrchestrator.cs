using System;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Coordinates high-level Cell stage lifecycle flow while the stage is incrementally modularized.
    /// </summary>
    public sealed class CellStageOrchestrator
    {
        public void Enter(Action enterStageLogic) => enterStageLogic?.Invoke();
        public void Exit(Action exitStageLogic) => exitStageLogic?.Invoke();
        public void Tick(Action updateStageLogic) => updateStageLogic?.Invoke();
    }
}
