using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public static class CreatureTargetAssist
    {
        public const float MinimumAssistDot = 0.45f;

        public static bool IsWithinAssistCone(Vector3 attackerForward, Vector3 toTarget, float minimumDot = MinimumAssistDot)
        {
            attackerForward.y = 0f;
            toTarget.y = 0f;
            if (attackerForward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
                return true;

            attackerForward.Normalize();
            toTarget.Normalize();
            return Vector3.Dot(attackerForward, toTarget) >= minimumDot;
        }

        public static float ComputeTargetScore(float distance, float forwardDot, bool overlapsAttackZone)
        {
            float safeDistance = Mathf.Max(0.01f, distance);
            float cone = Mathf.Clamp01((forwardDot + 1f) * 0.5f);
            float zoneBonus = overlapsAttackZone ? 1.2f : 0f;
            return cone * 2f + zoneBonus - safeDistance * 0.18f;
        }
    }
}
