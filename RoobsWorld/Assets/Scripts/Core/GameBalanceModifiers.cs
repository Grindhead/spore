using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Difficulty presets change pacing and recovery windows, not only spawn counts.
    /// </summary>
    public enum GameplayDifficultyPreset
    {
        Easy,
        Normal,
        Hard
    }

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
        /// <summary>Multiplier on enemy attack telegraph time (>1 = easier to read).</summary>
        public float EnemyTelegraphMultiplier { get; }
        /// <summary>Multiplier on enemy recovery windows after attacks (>1 = easier to punish).</summary>
        public float RecoveryWindowMultiplier { get; }

        private GameBalanceModifiers(float food, float predator, float npc,
                                     float metabolism, float evolutionThreshold, float dnaReward,
                                     float enemyTelegraph, float recoveryWindow)
        {
            FoodSpawnMultiplier    = food;
            PredatorMultiplier     = predator;
            NPCCountMultiplier     = npc;
            MetabolismMultiplier   = metabolism;
            EvolutionThresholdMult = evolutionThreshold;
            DNARewardMultiplier    = dnaReward;
            EnemyTelegraphMultiplier = enemyTelegraph;
            RecoveryWindowMultiplier = recoveryWindow;
        }

        /// <summary>Baseline game balance used for every run.</summary>
        public static GameBalanceModifiers Default { get; } = new GameBalanceModifiers(
            food: 1.0f,
            predator: 1.0f,
            npc: 1.0f,
            metabolism: 1.0f,
            evolutionThreshold: 1.0f,
            dnaReward: 1.0f,
            enemyTelegraph: 1.0f,
            recoveryWindow: 1.0f);

        /// <summary>Returns a coherent modifier set for the requested difficulty preset.</summary>
        public static GameBalanceModifiers ForDifficulty(GameplayDifficultyPreset preset)
        {
            switch (preset)
            {
                case GameplayDifficultyPreset.Easy:
                    return new GameBalanceModifiers(
                        food: 1.18f,
                        predator: 0.78f,
                        npc: 1.0f,
                        metabolism: 0.9f,
                        evolutionThreshold: 0.92f,
                        dnaReward: 1.08f,
                        enemyTelegraph: 1.35f,
                        recoveryWindow: 1.25f);
                case GameplayDifficultyPreset.Hard:
                    return new GameBalanceModifiers(
                        food: 0.9f,
                        predator: 1.18f,
                        npc: 1.04f,
                        metabolism: 1.12f,
                        evolutionThreshold: 1.08f,
                        dnaReward: 0.95f,
                        enemyTelegraph: 0.82f,
                        recoveryWindow: 0.82f);
                default:
                    return Default;
            }
        }

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
