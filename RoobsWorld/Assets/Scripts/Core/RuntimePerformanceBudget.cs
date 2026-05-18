using UnityEngine;

namespace RoobsWorld.Core
{
    public readonly struct RuntimePoolBudget
    {
        public readonly int Food;
        public readonly int Organisms;
        public readonly int Vfx;
        public readonly int RemoteGhosts;
        public readonly int CreatureRivals;
        public readonly int PreviewMeshes;

        public RuntimePoolBudget(int food, int organisms, int vfx, int remoteGhosts, int creatureRivals, int previewMeshes)
        {
            Food = Mathf.Max(0, food);
            Organisms = Mathf.Max(0, organisms);
            Vfx = Mathf.Max(0, vfx);
            RemoteGhosts = Mathf.Max(0, remoteGhosts);
            CreatureRivals = Mathf.Max(0, creatureRivals);
            PreviewMeshes = Mathf.Max(0, previewMeshes);
        }
    }

    /// <summary>
    /// Shared object-pool sizing rules for Cell, Creature, multiplayer ghosts, VFX, and editor previews.
    /// </summary>
    public static class RuntimePerformanceBudget
    {
        public static RuntimePoolBudget Calculate(int playerCount, float arenaRadius, bool stressSession = false)
        {
            int players = Mathf.Max(1, playerCount);
            float radiusFactor = Mathf.Clamp(Mathf.Max(10f, arenaRadius) / 48f, 0.5f, 2.5f);
            int stress = stressSession ? 2 : 0;

            return new RuntimePoolBudget(
                food: Mathf.CeilToInt((72 + players * 14 + stress * 24) * radiusFactor),
                organisms: Mathf.CeilToInt((28 + players * 6 + stress * 12) * radiusFactor),
                vfx: 24 + players * 8 + stress * 16,
                remoteGhosts: Mathf.Max(0, players - 1) + 4,
                creatureRivals: Mathf.Clamp(4 + players * 2 + stress * 4, 4, 28),
                previewMeshes: 18 + players * 2);
        }

        public static int ClampNetworkTickRate(int requestedHz, int playerCount)
        {
            int players = Mathf.Max(1, playerCount);
            int maxHz = players >= 8 ? 12 : players >= 4 ? 16 : 20;
            return Mathf.Clamp(requestedHz, 8, maxHz);
        }

        public static bool IsStressBudget(RuntimePoolBudget budget)
        {
            return budget.Food >= 140 || budget.Organisms >= 70 || budget.CreatureRivals >= 16;
        }
    }
}
