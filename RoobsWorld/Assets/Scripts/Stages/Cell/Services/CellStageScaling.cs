using UnityEngine;

namespace RoobsWorld.Stages.Cell.Services
{
    /// <summary>
    /// Multiplayer-aware scaling helpers extracted from CellStage orchestration.
    /// </summary>
    public static class CellStageScaling
    {
        public static int ComputeScaledCount(int baseCount, int playerCount, int perExtraPlayer, int maxBonus)
        {
            int safeBase = Mathf.Max(0, baseCount);
            int extraPlayers = Mathf.Max(0, playerCount - 1);
            int bonus = extraPlayers * Mathf.Max(0, perExtraPlayer);
            int cap = Mathf.Max(0, maxBonus);
            if (cap == 0)
                bonus = 0;
            else
                bonus = Mathf.Min(bonus, cap);

            return safeBase + bonus;
        }

        public static int ComputePredatorCount(int baseCount, int playerCount, int extraPlayersPerBonus, int maxBonus)
        {
            int safeBase = Mathf.Max(0, baseCount);
            int extraPlayers = Mathf.Max(0, playerCount - 1);
            int interval = Mathf.Max(1, extraPlayersPerBonus);
            int bonus = extraPlayers / interval;
            int cap = Mathf.Max(0, maxBonus);
            if (cap == 0)
                bonus = 0;
            else
                bonus = Mathf.Min(bonus, cap);

            return safeBase + bonus;
        }

        public static float ComputeArenaRadius(float baseRadius, int playerCount, float perExtraPlayerFraction, float maxBonusFraction)
        {
            float safeBase = Mathf.Max(0f, baseRadius);
            int extraPlayers = Mathf.Max(0, playerCount - 1);
            float bonusFraction = extraPlayers * Mathf.Max(0f, perExtraPlayerFraction);
            float cap = Mathf.Max(0f, maxBonusFraction);
            if (cap == 0f)
                bonusFraction = 0f;
            else
                bonusFraction = Mathf.Min(bonusFraction, cap);

            return safeBase * (1f + bonusFraction);
        }
    }
}
