using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Injects Cell stage runtime references to actors to reduce per-actor scene discovery calls.
    /// </summary>
    public static class CellActorContextBinder
    {
        public static void Bind(GameObject actorRoot, CellStage stage, FoodManager foodManager)
        {
            if (actorRoot == null)
                return;

            var receivers = actorRoot.GetComponentsInChildren<ICellStageContextAware>(true);
            for (int i = 0; i < receivers.Length; i++)
                receivers[i]?.BindCellStageContext(stage, foodManager);
        }
    }
}
