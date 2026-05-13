using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// A permanent creature-stage adaptation selected by the player.
    /// </summary>
    [Serializable]
    public class CreatureUpgradeRecord
    {
        public string UpgradeId;
        public float HealthDelta;
        public float SpeedDelta;
        public float AttackDelta;
        public float DefenseDelta;
        public float MaxStaminaDelta;
        public float StaminaRecoveryDelta;
        public float AbilityCooldownDelta;
    }

    /// <summary>
    /// Describes an offered upgrade choice and the lasting stat tradeoff it applies.
    /// </summary>
    public struct CreatureUpgradeOption
    {
        public string Id;
        public string Title;
        public string Description;
        public string Tag;
        public string Tradeoff;
        public float HealthDelta;
        public float SpeedDelta;
        public float AttackDelta;
        public float DefenseDelta;
        public float MaxStaminaDelta;
        public float StaminaRecoveryDelta;
        public float AbilityCooldownDelta;

        public CreatureUpgradeRecord ToRecord()
        {
            return new CreatureUpgradeRecord
            {
                UpgradeId = Id,
                HealthDelta = HealthDelta,
                SpeedDelta = SpeedDelta,
                AttackDelta = AttackDelta,
                DefenseDelta = DefenseDelta,
                MaxStaminaDelta = MaxStaminaDelta,
                StaminaRecoveryDelta = StaminaRecoveryDelta,
                AbilityCooldownDelta = AbilityCooldownDelta
            };
        }
    }

    /// <summary>
    /// Shared upgrade rules for creature-stage evolution decisions.
    /// </summary>
    public static class CreatureUpgradeCatalog
    {
        public const int RivalDefeatsPerUpgrade = 2;

        private static readonly CreatureUpgradeOption[] CoreOptions =
        {
            new CreatureUpgradeOption
            {
                Id = "armored_bulk",
                Title = "Armored Bulk",
                Description = "Gain a much larger health pool, but become noticeably slower.",
                Tag = "survival",
                Tradeoff = "Lower chase speed and a smaller stamina pool.",
                HealthDelta = 45f,
                SpeedDelta = -1.4f,
                DefenseDelta = 2.2f,
                MaxStaminaDelta = -8f
            },
            new CreatureUpgradeOption
            {
                Id = "runner_frame",
                Title = "Runner Frame",
                Description = "Move fast enough to pick fights and escape, but lose survivability.",
                Tag = "mobility",
                Tradeoff = "Lower health and defense make mistakes costly.",
                HealthDelta = -25f,
                SpeedDelta = 2.2f,
                DefenseDelta = -1f,
                MaxStaminaDelta = 22f
            },
            new CreatureUpgradeOption
            {
                Id = "lean_muscle",
                Title = "Lean Muscle",
                Description = "Hit harder and move a little faster for decisive short fights.",
                Tag = "burst damage",
                Tradeoff = "Lower stamina reserve punishes long brawls.",
                HealthDelta = 12f,
                SpeedDelta = 0.6f,
                AttackDelta = 2.4f,
                MaxStaminaDelta = -6f
            },
            new CreatureUpgradeOption
            {
                Id = "neural_overdrive",
                Title = "Neural Overdrive",
                Description = "Recover stamina faster and reuse your inherited ability sooner.",
                Tag = "control",
                Tradeoff = "Lower health and raw attack.",
                HealthDelta = -12f,
                SpeedDelta = 0.2f,
                AttackDelta = -0.8f,
                StaminaRecoveryDelta = 10f,
                AbilityCooldownDelta = -1.6f
            }
        };

        public static IReadOnlyList<CreatureUpgradeOption> GetCoreOptions()
        {
            return CoreOptions;
        }

        public static CreatureUpgradeOption GetCoreOption()
        {
            return CoreOptions[0];
        }

        public static void EnsureUpgradeList(CreatureDNA dna)
        {
            if (dna == null)
                return;
            dna.Upgrades ??= new List<CreatureUpgradeRecord>();
        }

        public static void ApplyUpgrade(CreatureDNA dna, CreatureUpgradeOption option)
        {
            if (dna == null)
                return;

            EnsureUpgradeList(dna);
            dna.Upgrades.Add(option.ToRecord());

            float scaleBias = Mathf.Clamp(option.HealthDelta / 120f - option.SpeedDelta / 8f, -0.18f, 0.25f);
            Vector3 scale = dna.Body != null ? dna.Body.BodyScale : Vector3.one;
            if (scale == Vector3.zero)
                scale = Vector3.one;
            scale.x = Mathf.Clamp(scale.x + scaleBias * 0.25f, 0.75f, 1.35f);
            scale.y = Mathf.Clamp(scale.y + scaleBias * 0.18f, 0.75f, 1.35f);
            scale.z = Mathf.Clamp(scale.z + scaleBias * 0.35f, 0.75f, 1.45f);
            if (dna.Body != null)
                dna.Body.BodyScale = scale;
        }

        public static int GetEvolutionLevel(CreatureDNA dna)
        {
            EnsureUpgradeList(dna);
            return dna?.Upgrades?.Count ?? 0;
        }
    }
}
