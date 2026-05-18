using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureStageObjectiveType
    {
        RivalCull,
        AlphaHunt,
        HoldFeedingGround,
        PacifyRivals
    }

    public readonly struct CreatureStageObjective
    {
        public readonly CreatureStageObjectiveType Type;
        public readonly int RivalDefeatTarget;
        public readonly float HoldSecondsTarget;
        public readonly string LocalizationKey;

        public bool UsesHoldTimer => Type == CreatureStageObjectiveType.HoldFeedingGround;

        public CreatureStageObjective(
            CreatureStageObjectiveType type,
            int rivalDefeatTarget,
            float holdSecondsTarget,
            string localizationKey)
        {
            Type = type;
            RivalDefeatTarget = Mathf.Max(1, rivalDefeatTarget);
            HoldSecondsTarget = Mathf.Max(1f, holdSecondsTarget);
            LocalizationKey = localizationKey;
        }
    }

    public static class CreatureStageObjectivePlanner
    {
        public const float FeedingGroundRadius = 13f;

        public static CreatureStageObjective CreateObjective(CreatureDNA dna)
            => CreateObjective(dna, completedObjectives: 0);

        public static CreatureStageObjective CreateObjective(CreatureDNA dna, int completedObjectives)
        {
            int slot = ComputeObjectiveSlotForCreature(dna, completedObjectives);
            return CreateObjectiveForSlot(slot);
        }

        public static int ComputeObjectiveSlotForCreature(CreatureDNA dna, int completedObjectives)
        {
            int evolutionLevel = CreatureUpgradeCatalog.GetEvolutionLevel(dna);
            int partCount = dna?.Parts?.Count ?? 0;
            int slot = ComputeObjectiveSlot(evolutionLevel, partCount, completedObjectives);
            if (completedObjectives <= 0)
                return slot;

            int biasOffset = ComputeBuildBiasOffset(dna, evolutionLevel, partCount);
            int normalized = (slot + biasOffset) % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        public static int ComputeObjectiveSlot(int evolutionLevel, int partCount, int completedObjectives)
        {
            int seed = Mathf.Max(0, evolutionLevel) + Mathf.Max(0, partCount) + Mathf.Max(0, completedObjectives);
            return seed % 4;
        }

        public static int ComputeBuildBiasOffset(CreatureDNA dna, int evolutionLevel, int partCount)
        {
            if (dna == null || partCount <= 0)
                return 0;

            var stats = StatCalculator.Calculate(dna, CreaturePartCatalog.GetDefaultCatalog());
            float aggression = stats.Attack - stats.Social + Mathf.Max(0f, stats.Speed - 6f) * 0.25f;
            float socialDefensive = stats.Social + stats.Defense * 0.35f - stats.Attack * 0.2f;

            // Spore creature stage supports attack-heavy and social-heavy routes; bias objective pacing accordingly.
            if (socialDefensive >= 8f)
                return 3; // shift toward PacifyRivals opportunities
            if (aggression >= 5f)
                return 1; // shift toward AlphaHunt opportunities

            // Keep very early creature forms near the baseline objective rotation.
            if (evolutionLevel <= 0 && partCount <= 2)
                return 0;

            return 0;
        }

        public static CreatureStageObjective CreateObjectiveForSlot(int slot)
        {
            int normalizedSlot = Mathf.Abs(slot) % 4;
            return normalizedSlot switch
            {
                1 => new CreatureStageObjective(CreatureStageObjectiveType.AlphaHunt, 3, 1f, "creature.objective.alpha_hunt"),
                2 => new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground"),
                3 => new CreatureStageObjective(CreatureStageObjectiveType.PacifyRivals, 4, 1f, "creature.objective.pacify"),
                _ => new CreatureStageObjective(CreatureStageObjectiveType.RivalCull, 5, 1f, "creature.objective.rival_cull")
            };
        }

        public static bool IsInsideFeedingGround(Vector3 position)
        {
            position.y = 0f;
            return position.sqrMagnitude <= FeedingGroundRadius * FeedingGroundRadius;
        }
    }
}
