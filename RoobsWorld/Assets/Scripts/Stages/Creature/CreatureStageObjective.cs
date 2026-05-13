using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureStageObjectiveType
    {
        RivalCull,
        AlphaHunt,
        HoldFeedingGround
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
            int evolutionLevel = CreatureUpgradeCatalog.GetEvolutionLevel(dna);
            int partCount = dna?.Parts?.Count ?? 0;
            int slot = ComputeObjectiveSlot(evolutionLevel, partCount, completedObjectives);
            return CreateObjectiveForSlot(slot);
        }

        public static int ComputeObjectiveSlot(int evolutionLevel, int partCount, int completedObjectives)
        {
            int seed = Mathf.Max(0, evolutionLevel) + Mathf.Max(0, partCount) + Mathf.Max(0, completedObjectives);
            return seed % 3;
        }

        public static CreatureStageObjective CreateObjectiveForSlot(int slot)
        {
            int normalizedSlot = Mathf.Abs(slot) % 3;
            return normalizedSlot switch
            {
                1 => new CreatureStageObjective(CreatureStageObjectiveType.AlphaHunt, 3, 1f, "creature.objective.alpha_hunt"),
                2 => new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground"),
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
