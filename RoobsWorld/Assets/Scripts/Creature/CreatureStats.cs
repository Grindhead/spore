using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Aggregated gameplay stats derived from a creature's DNA and part catalog.
    /// </summary>
    public struct CreatureStats
    {
        public float Speed;
        public float Health;
        public float Attack;
        public float Defense;
        public float Social;
        public float Stealth;

        public static CreatureStats BaseStats => new CreatureStats
        {
            Speed = 5f,
            Health = 100f,
            Attack = 5f,
            Defense = 5f,
            Social = 5f,
            Stealth = 5f
        };

        public override string ToString()
        {
            return $"SPD:{Speed:F1} HP:{Health:F0} ATK:{Attack:F1} DEF:{Defense:F1} SOC:{Social:F1} STL:{Stealth:F1}";
        }
    }

    /// <summary>
    /// Computes <see cref="CreatureStats"/> from DNA by summing modifiers from each equipped part.
    /// </summary>
    public static class StatCalculator
    {
        public static CreatureStats Calculate(CreatureDNA dna, Dictionary<string, CreaturePartDef> partCatalog)
        {
            var stats = CreatureStats.BaseStats;

            foreach (var placement in dna.Parts)
            {
                if (!partCatalog.TryGetValue(placement.PartId, out var partDef))
                    continue;

                stats.Speed += partDef.SpeedModifier;
                stats.Health += partDef.HealthModifier;
                stats.Attack += partDef.AttackModifier;
                stats.Defense += partDef.DefenseModifier;
                stats.Social += partDef.SocialModifier;
                stats.Stealth += partDef.StealthModifier;
            }

            CreatureUpgradeCatalog.EnsureUpgradeList(dna);
            foreach (var upgrade in dna.Upgrades)
            {
                if (upgrade == null)
                    continue;

                stats.Health += upgrade.HealthDelta;
                stats.Speed += upgrade.SpeedDelta;
                stats.Attack += upgrade.AttackDelta;
                stats.Defense += upgrade.DefenseDelta;
            }

            stats.Speed = Mathf.Max(1f, stats.Speed);
            stats.Health = Mathf.Max(10f, stats.Health);
            stats.Attack = Mathf.Max(0f, stats.Attack);
            stats.Defense = Mathf.Max(0f, stats.Defense);
            stats.Social = Mathf.Max(0f, stats.Social);
            stats.Stealth = Mathf.Max(0f, stats.Stealth);

            return stats;
        }
    }
}
