namespace RoobsWorld.Stages.Cell.Services
{
    /// <summary>
    /// Pure symbiosis and trait math extracted from PlayerCell.
    /// </summary>
    public static class PlayerCellSymbiosisRules
    {
        public static float GetTraitPassiveMultiplier(int symbiosisLevel, float bonusPerLevel)
        {
            return 1f + symbiosisLevel * bonusPerLevel;
        }

        public static bool CanUpgradeSymbiosisLevel(int currentLevel, int maxLevel)
        {
            return currentLevel < maxLevel;
        }

        public static int UpgradeSymbiosisLevel(int currentLevel, int maxLevel)
        {
            return currentLevel < maxLevel ? currentLevel + 1 : currentLevel;
        }

        public static float GetArmouredDamageReduction(float baseReduction, int armouredLevel, float bonusPerLevel)
        {
            return baseReduction * (1f + armouredLevel * bonusPerLevel);
        }

        public static float GetElectricPulseInterval(float baseInterval, int electricLevel, float intervalReductionPerLevel)
        {
            float mult = 1f - electricLevel * intervalReductionPerLevel;
            return baseInterval * (mult < 0.2f ? 0.2f : mult);
        }
    }
}
