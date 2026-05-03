using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Cell-stage environment zones and trait modifiers. Step 6 of cell-stage evolution.
    /// Zones: algae forest (outer band), open water (mid), deep (inner), toxic (small circle).
    /// Use GetBiomeAt(arenaCenter, position, arenaRadius) then Get*Multiplier(biome) for metabolism, speed, food efficiency.
    /// </summary>
    public static class Biome
    {
        /// <summary>Environmental zone type; determines trait bonuses/penalties.</summary>
        public enum BiomeType
        {
            OpenWater,
            AlgaeForest,
            DeepZone,
            ToxicZone
        }

        /// <summary>
        /// Determines which biome a position is in. Arena is a circle; toxic zone is a smaller circle at offset.
        /// Uses BalanceConstants for zone radius fractions.
        /// </summary>
        /// <param name="arenaCenter">Center of the arena (e.g. Vector3.zero).</param>
        /// <param name="position">World position to test (XZ used).</param>
        /// <param name="arenaRadius">Full arena radius.</param>
        /// <returns>Biome at that position.</returns>
        public static BiomeType GetBiomeAt(Vector3 arenaCenter, Vector3 position, float arenaRadius)
        {
            if (arenaRadius <= 0f) return BiomeType.OpenWater;

            Vector3 offset = position - arenaCenter;
            offset.y = 0f;
            float dist = offset.magnitude;

            // Toxic zone: circle at (offsetFraction*R, 0, offsetFraction*R) with radius toxicRadiusFraction*R
            float toxicCenterOffset = arenaRadius * BalanceConstants.CellBiomeToxicZoneCenterOffsetFraction;
            Vector3 toxicCenter = arenaCenter + new Vector3(toxicCenterOffset, 0f, toxicCenterOffset);
            Vector3 toToxic = position - toxicCenter;
            toToxic.y = 0f;
            float toxicRadius = arenaRadius * BalanceConstants.CellBiomeToxicZoneRadiusFraction;
            if (toToxic.sqrMagnitude <= toxicRadius * toxicRadius)
                return BiomeType.ToxicZone;

            // Radial bands from center
            float rFrac = dist / arenaRadius;
            if (rFrac <= BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction)
                return BiomeType.DeepZone;
            if (rFrac <= BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction)
                return BiomeType.OpenWater;
            if (rFrac <= BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction)
                return BiomeType.AlgaeForest;
            return BiomeType.OpenWater;
        }

        /// <summary>Metabolism drain multiplier for the given biome (e.g. 0.7 = slower drain in Deep).</summary>
        public static float GetMetabolismMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.DeepZone: return BalanceConstants.CellBiomeMetabolismMultiplierDeep;
                case BiomeType.OpenWater: return BalanceConstants.CellBiomeMetabolismMultiplierOpenWater;
                case BiomeType.AlgaeForest: return BalanceConstants.CellBiomeMetabolismMultiplierAlgaeForest;
                case BiomeType.ToxicZone: return BalanceConstants.CellBiomeMetabolismMultiplierToxic;
                default: return BalanceConstants.CellBiomeMetabolismMultiplierOpenWater;
            }
        }

        /// <summary>Movement speed multiplier for the given biome.</summary>
        public static float GetSpeedMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.DeepZone: return BalanceConstants.CellBiomeSpeedMultiplierDeep;
                case BiomeType.OpenWater: return BalanceConstants.CellBiomeSpeedMultiplierOpenWater;
                case BiomeType.AlgaeForest: return BalanceConstants.CellBiomeSpeedMultiplierAlgaeForest;
                case BiomeType.ToxicZone: return BalanceConstants.CellBiomeSpeedMultiplierToxic;
                default: return BalanceConstants.CellBiomeSpeedMultiplierOpenWater;
            }
        }

        /// <summary>Food efficiency multiplier when eating in the given biome (e.g. Toxic = 0.7).</summary>
        public static float GetFoodEfficiencyMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.ToxicZone: return BalanceConstants.CellBiomeFoodEfficiencyMultiplierToxic;
                default: return BalanceConstants.CellBiomeFoodEfficiencyMultiplierDefault;
            }
        }
    }
}
