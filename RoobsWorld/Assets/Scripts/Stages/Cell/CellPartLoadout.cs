using System.Collections.Generic;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tracks which cell parts the player has unlocked and exposes derived movement and damage properties.
    /// </summary>
    public enum CellPartType
    {
        Flagella,
        Cilia,
        Spike,
        Jet
    }

    /// <summary>
    /// Runtime loadout of unlocked cell parts that modifies speed and contact damage.
    /// </summary>
    public sealed class CellPartLoadout
    {
        private readonly HashSet<CellPartType> _unlockedParts = new();

        public float SpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                if (_unlockedParts.Contains(CellPartType.Flagella)) multiplier += 0.2f;
                if (_unlockedParts.Contains(CellPartType.Cilia)) multiplier += 0.1f;
                if (_unlockedParts.Contains(CellPartType.Jet)) multiplier += 0.35f;
                return multiplier;
            }
        }

        public bool HasContactDamage => _unlockedParts.Contains(CellPartType.Spike);
        public float ContactDamageMultiplier => _unlockedParts.Contains(CellPartType.Spike) ? 1.45f : 1f;
        public float ConsumeProbeRadiusBonus => _unlockedParts.Contains(CellPartType.Cilia) ? 0.35f : 0f;
        public float FoodGainMultiplier => _unlockedParts.Contains(CellPartType.Cilia) ? 1.15f : 1f;
        public float BoostDurationMultiplier => _unlockedParts.Contains(CellPartType.Jet) ? 1.35f : 1f;
        public float BoostSpeedMultiplier => _unlockedParts.Contains(CellPartType.Jet) ? 1.15f : 1f;

        public bool TryUnlock(CellPartType partType)
        {
            return _unlockedParts.Add(partType);
        }

        public bool HasPart(CellPartType partType)
        {
            return _unlockedParts.Contains(partType);
        }
    }
}
