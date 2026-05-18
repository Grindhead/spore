using System;

namespace RoobsWorld.Stages.Cell
{
    public sealed class CellStageSpawnDirector
    {
        public void SpawnInitial(Action spawnArena, Action spawnPlayer, Action spawnFood, Action spawnNpcs, Action spawnOrganisms, Action spawnPickups, Action spawnHazards)
        {
            spawnArena?.Invoke();
            spawnPlayer?.Invoke();
            spawnFood?.Invoke();
            spawnNpcs?.Invoke();
            spawnOrganisms?.Invoke();
            spawnPickups?.Invoke();
            spawnHazards?.Invoke();
        }
    }
}
