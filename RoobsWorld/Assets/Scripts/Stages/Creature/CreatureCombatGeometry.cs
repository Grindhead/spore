using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Shared geometric helpers for Creature-stage melee range checks.
    /// Uses horizontal (XZ) bounds to better match perceived body silhouette.
    /// </summary>
    public static class CreatureCombatGeometry
    {
        private const float MinimumRadius = 0.35f;

        /// <summary>
        /// Estimates a target's planar body radius in world units using controller/collider/renderer bounds.
        /// </summary>
        public static float EstimatePlanarRadius(Transform target, float fallbackRadius = 0.75f)
        {
            float safeFallback = Mathf.Max(MinimumRadius, fallbackRadius);
            if (target == null)
                return safeFallback;

            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
                return Mathf.Max(safeFallback, controller.radius * Mathf.Abs(target.lossyScale.x));

            var colliders = target.GetComponentsInChildren<Collider>();
            if (colliders != null && colliders.Length > 0)
                return Mathf.Max(safeFallback, ComputePlanarRadiusFromBounds(colliders));

            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
                return Mathf.Max(safeFallback, ComputePlanarRadiusFromBounds(renderers));

            return safeFallback;
        }

        /// <summary>
        /// Computes melee reach from base range plus attacker/target body extents.
        /// </summary>
        public static float ComputeContactReach(
            float baseRange,
            float attackerRadius,
            float targetRadius,
            float attackerRadiusWeight = 0.25f,
            float targetRadiusWeight = 1f)
        {
            float safeBase = Mathf.Max(0f, baseRange);
            float safeAttacker = Mathf.Max(MinimumRadius, attackerRadius);
            float safeTarget = Mathf.Max(MinimumRadius, targetRadius);
            float safeAttackerWeight = Mathf.Max(0f, attackerRadiusWeight);
            float safeTargetWeight = Mathf.Max(0f, targetRadiusWeight);
            return safeBase + safeAttacker * safeAttackerWeight + safeTarget * safeTargetWeight;
        }

        private static float ComputePlanarRadiusFromBounds<T>(T[] components) where T : Component
        {
            if (components == null || components.Length == 0)
                return MinimumRadius;

            Bounds bounds = default;
            bool initialized = false;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                    continue;

                Bounds currentBounds;
                if (components[i] is Collider c)
                    currentBounds = c.bounds;
                else if (components[i] is Renderer r)
                    currentBounds = r.bounds;
                else
                    continue;

                if (!initialized)
                {
                    bounds = currentBounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(currentBounds);
                }
            }

            if (!initialized)
                return MinimumRadius;

            return Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
        }
    }
}
