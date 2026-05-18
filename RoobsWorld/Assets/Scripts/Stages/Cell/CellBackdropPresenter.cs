using System;

namespace RoobsWorld.Stages.Cell
{
    public sealed class CellBackdropPresenter
    {
        public void BuildBackdrop(Action spawnArenaAction) => spawnArenaAction?.Invoke();
        public void TickBackdrop(Action updateBackdropAction) => updateBackdropAction?.Invoke();
        public void Cleanup(Action cleanupAction) => cleanupAction?.Invoke();
    }
}
