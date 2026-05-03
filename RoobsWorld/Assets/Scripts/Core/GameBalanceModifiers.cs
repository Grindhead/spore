using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Single baseline multiplier bundle for a new game run.
    /// All multipliers are applied relative to base <see cref="BalanceConstants"/> values.
    /// </summary>
    public readonly struct GameBalanceModifiers
    {
        /// <summary>Multiplier on food spawn count (>1 = more food).</summary>
        public float FoodSpawnMultiplier    { get; }
        /// <summary>Multiplier on predator count and aggression.</summary>
        public float PredatorMultiplier     { get; }
        /// <summary>Multiplier on NPC organism count.</summary>
        public float NPCCountMultiplier     { get; }
        /// <summary>Multiplier on metabolism drain speed (>1 = faster drain).</summary>
        public float MetabolismMultiplier   { get; }
        /// <summary>Multiplier on evolution mass thresholds (>1 = harder to evolve).</summary>
        public float EvolutionThresholdMult { get; }
        /// <summary>DNA points multiplier per goal complete.</summary>
        public float DNARewardMultiplier    { get; }

        private GameBalanceModifiers(float food, float predator, float npc,
                                     float metabolism, float evolutionThreshold, float dnaReward)
        {
            FoodSpawnMultiplier    = food;
            PredatorMultiplier     = predator;
            NPCCountMultiplier     = npc;
            MetabolismMultiplier   = metabolism;
            EvolutionThresholdMult = evolutionThreshold;
            DNARewardMultiplier    = dnaReward;
        }

        /// <summary>Baseline game balance used for every run.</summary>
        public static GameBalanceModifiers Default { get; } = new GameBalanceModifiers(
            food: 1.0f,
            predator: 1.0f,
            npc: 1.0f,
            metabolism: 1.0f,
            evolutionThreshold: 1.0f,
            dnaReward: 1.0f);

        /// <summary>Applies food/predator/NPC count modifiers to an int count.</summary>
        public int ApplyFoodCount(int baseCount) =>
            Mathf.Max(1, Mathf.RoundToInt(baseCount * FoodSpawnMultiplier));

        public int ApplyPredatorCount(int baseCount) =>
            Mathf.Max(0, Mathf.RoundToInt(baseCount * PredatorMultiplier));

        public int ApplyNPCCount(int baseCount) =>
            Mathf.Max(1, Mathf.RoundToInt(baseCount * NPCCountMultiplier));

        public int ApplyDNAReward(int baseReward) =>
            Mathf.Max(1, Mathf.RoundToInt(baseReward * DNARewardMultiplier));
    }
}
