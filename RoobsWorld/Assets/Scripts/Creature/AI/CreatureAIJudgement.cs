using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Creature.AI
{
    public enum CreatureRivalIntent
    {
        Wander,
        ReturnToTerritory,
        Flee,
        KeepDistance,
        Stalk,
        Chase
    }

    /// <summary>
    /// Pure creature AI scoring based on everyday animal tradeoffs: injury, distance,
    /// opponent strength, size, and the pull back toward a familiar territory.
    /// </summary>
    public static class CreatureAIJudgement
    {
        private const float CloseDistance = 2.5f;
        private const float FarDistance = 18f;
        private const float HomeRangeSoftLimit = 1.15f;
        private const float HomeRangeHardLimit = 1.65f;
        private const float StalkMinDistance = 4.5f;

        public static float HealthRatio(CreatureInstance creature)
        {
            if (creature == null)
                return 0f;

            return Mathf.Clamp01(creature.CurrentHealth / Mathf.Max(1f, creature.MaxHealth));
        }

        public static float EvaluateDistancePressure(float distance)
        {
            if (distance <= CloseDistance) return 1f;
            if (distance >= FarDistance) return 0f;

            float t = (distance - CloseDistance) / Mathf.Max(0.01f, FarDistance - CloseDistance);
            return 1f - Mathf.Clamp01(t);
        }

        public static float EvaluateThreatUrgency(
            CreatureStats selfStats,
            float selfHealthRatio,
            CreatureStats otherStats,
            float otherHealthRatio,
            float distance,
            bool sizeThreat)
        {
            float distancePressure = EvaluateDistancePressure(distance);
            float combatPressure = Mathf.Clamp01(
                (otherStats.Attack - selfStats.Defense * 0.85f) / Mathf.Max(1f, selfStats.Health * 0.15f));
            float injuryPressure = 1f - Mathf.Clamp01(selfHealthRatio);
            float opponentStillCapable = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(otherHealthRatio));
            float sizePressure = sizeThreat ? 0.55f : 0f;

            return Mathf.Clamp01(
                distancePressure * 0.34f
                + combatPressure * opponentStillCapable * 0.32f
                + injuryPressure * 0.24f
                + sizePressure);
        }

        public static float EvaluatePreyAppeal(
            CreatureStats selfStats,
            CreatureStats preyStats,
            float preyHealthRatio,
            float distance,
            bool sizeThreat)
        {
            if (sizeThreat)
                return 0f;

            float distanceOpportunity = EvaluateDistancePressure(distance);
            float preyWeakness = 1f - Mathf.Clamp01(preyHealthRatio);
            float attackMargin = Mathf.Clamp01(
                (selfStats.Attack - preyStats.Defense * 0.75f) / Mathf.Max(1f, preyStats.Health * 0.12f));
            float dangerCost = Mathf.Clamp01(
                preyStats.Attack / Mathf.Max(1f, selfStats.Defense + selfStats.Health * 0.08f));

            return Mathf.Clamp01(
                distanceOpportunity * 0.36f
                + preyWeakness * 0.30f
                + attackMargin * 0.34f
                - dangerCost * 0.26f);
        }

        public static bool ShouldTreatAsThreat(
            CreatureInstance self,
            CreatureInstance other,
            float distance,
            float urgencyThreshold = 0.42f)
        {
            if (self == null || other == null || !other.IsAlive)
                return false;

            bool sizeThreat = CreatureSizeRules.ShouldFleeFromPlayer(self, other)
                || CreatureSizeRules.IsMeaningfullySmaller(self, other, 0.12f);
            float urgency = EvaluateThreatUrgency(
                self.Stats,
                HealthRatio(self),
                other.Stats,
                HealthRatio(other),
                distance,
                sizeThreat);

            return urgency >= urgencyThreshold;
        }

        public static bool ShouldTreatAsPrey(
            CreatureInstance self,
            CreatureInstance other,
            float distance,
            float appealThreshold = 0.46f)
        {
            if (self == null || other == null || !other.IsAlive)
                return false;

            bool sizeThreat = CreatureSizeRules.ShouldFleeFromPlayer(self, other)
                || CreatureSizeRules.IsMeaningfullySmaller(self, other, 0.10f);
            float appeal = EvaluatePreyAppeal(
                self.Stats,
                other.Stats,
                HealthRatio(other),
                distance,
                sizeThreat);

            return appeal >= appealThreshold;
        }

        public static CreatureRivalIntent ChooseRivalIntent(
            CreatureStats selfStats,
            float selfHealthRatio,
            CreatureStats playerStats,
            float playerHealthRatio,
            float playerDistance,
            bool sizeThreat,
            Vector3 selfPosition,
            Vector3 anchorPosition,
            Vector3 playerPosition,
            float wanderRadius,
            float aggroRange,
            bool recentlyDamaged = false)
        {
            if (ShouldReturnToTerritory(selfPosition, anchorPosition, playerPosition, wanderRadius, aggroRange))
                return CreatureRivalIntent.ReturnToTerritory;

            if (playerDistance > Mathf.Max(0f, aggroRange))
                return CreatureRivalIntent.Wander;

            float health = Mathf.Clamp01(selfHealthRatio);
            float threatUrgency = EvaluateThreatUrgency(
                selfStats,
                health,
                playerStats,
                playerHealthRatio,
                playerDistance,
                sizeThreat);
            if (recentlyDamaged)
                threatUrgency = Mathf.Clamp01(threatUrgency + 0.18f);

            float preyAppeal = EvaluatePreyAppeal(
                selfStats,
                playerStats,
                playerHealthRatio,
                playerDistance,
                sizeThreat);

            if (threatUrgency >= 0.72f)
                return CreatureRivalIntent.Flee;

            if (threatUrgency >= 0.48f && preyAppeal < 0.68f)
                return CreatureRivalIntent.KeepDistance;

            if (preyAppeal >= 0.62f && health >= 0.35f)
                return CreatureRivalIntent.Chase;

            if (playerDistance > StalkMinDistance && preyAppeal >= 0.38f && threatUrgency < 0.58f)
                return CreatureRivalIntent.Stalk;

            if (threatUrgency >= 0.36f)
                return CreatureRivalIntent.KeepDistance;

            return CreatureRivalIntent.Wander;
        }

        public static bool ShouldCommitToAttack(
            CreatureInstance self,
            CreatureInstance target,
            float distance,
            bool recentlyDamaged = false)
        {
            if (self == null || target == null || !target.IsAlive)
                return false;

            bool sizeThreat = CreatureSizeRules.ShouldFleeFromPlayer(self, target)
                || CreatureSizeRules.IsMeaningfullySmaller(self, target, 0.10f);
            var intent = ChooseRivalIntent(
                self.Stats,
                HealthRatio(self),
                target.Stats,
                HealthRatio(target),
                distance,
                sizeThreat,
                self.transform.position,
                self.transform.position,
                target.transform.position,
                wanderRadius: Mathf.Max(3f, distance + 1f),
                aggroRange: Mathf.Max(3f, distance + 1f),
                recentlyDamaged);

            return intent == CreatureRivalIntent.Chase
                && EvaluatePreyAppeal(self.Stats, target.Stats, HealthRatio(target), distance, sizeThreat) >= 0.52f;
        }

        public static Vector3 ComputeCautiousDirection(Vector3 selfPosition, Vector3 playerPosition, Vector3 anchorPosition)
        {
            Vector3 away = selfPosition - playerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.forward;

            Vector3 toAnchor = anchorPosition - selfPosition;
            toAnchor.y = 0f;

            Vector3 tangent = new Vector3(-away.z, 0f, away.x);
            if (Vector3.Dot(tangent, toAnchor) < 0f)
                tangent = -tangent;

            Vector3 blended = away.normalized * 0.62f
                + tangent.normalized * 0.26f
                + (toAnchor.sqrMagnitude > 0.0001f ? toAnchor.normalized * 0.36f : Vector3.zero);
            return blended.sqrMagnitude < 0.0001f ? away.normalized : blended.normalized;
        }

        public static bool ShouldReturnToTerritory(
            Vector3 selfPosition,
            Vector3 anchorPosition,
            Vector3 playerPosition,
            float wanderRadius,
            float aggroRange)
        {
            float safeWanderRadius = Mathf.Max(0.1f, wanderRadius);
            float distanceFromHome = HorizontalDistance(selfPosition, anchorPosition);
            float playerDistanceFromHome = HorizontalDistance(playerPosition, anchorPosition);
            float softLimit = safeWanderRadius * HomeRangeSoftLimit;
            float hardLimit = safeWanderRadius * HomeRangeHardLimit;

            if (distanceFromHome >= hardLimit)
                return true;

            if (distanceFromHome < softLimit)
                return false;

            return playerDistanceFromHome > distanceFromHome
                && playerDistanceFromHome > Mathf.Max(aggroRange, safeWanderRadius);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
