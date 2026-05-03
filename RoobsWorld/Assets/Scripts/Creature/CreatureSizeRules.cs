using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Shared size comparison rules for creature AI decisions.
    /// </summary>
    public static class CreatureSizeRules
    {
        public const float SizeAdvantageTolerance = 0.05f;

        private const float MinimumPlanarSize = 0.1f;

        public static bool ShouldFleeFromPlayer(CreatureInstance self, CreatureInstance player, float tolerance = SizeAdvantageTolerance)
        {
            return self != null
                && player != null
                && player.IsPlayer
                && player.IsAlive
                && IsMeaningfullySmaller(self, player, tolerance);
        }

        public static bool IsMeaningfullySmaller(CreatureInstance self, CreatureInstance other, float tolerance = SizeAdvantageTolerance)
        {
            if (self == null || other == null)
                return false;

            float selfSize = EstimatePlanarSize(self);
            float otherSize = EstimatePlanarSize(other);
            float safeTolerance = Mathf.Clamp01(tolerance);
            return selfSize < otherSize * (1f - safeTolerance);
        }

        public static float EstimatePlanarSize(CreatureInstance creature)
        {
            if (creature == null)
                return MinimumPlanarSize;

            float dnaSize = EstimatePlanarSize(creature.DNA);
            float geometrySize = EstimatePlanarSize(creature.transform);
            return Mathf.Max(MinimumPlanarSize, dnaSize, geometrySize);
        }

        public static float EstimatePlanarSize(CreatureDNA dna)
        {
            if (dna?.Body == null)
                return MinimumPlanarSize;

            float spineRadius = Mathf.Max(1, dna.Body.SpineSegments) * 0.2f;
            float partReach = 0f;
            if (dna.Parts != null)
            {
                for (int i = 0; i < dna.Parts.Count; i++)
                {
                    var part = dna.Parts[i];
                    float reach = new Vector2(part.Offset.x, part.Offset.z).magnitude + Mathf.Max(0.1f, part.Scale) * 0.15f;
                    partReach = Mathf.Max(partReach, reach);
                }
            }

            Vector3 scale = dna.Body.BodyScale;
            float planarScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            if (planarScale < 0.01f)
                planarScale = 1f;

            return Mathf.Max(spineRadius, partReach) * planarScale;
        }

        private static float EstimatePlanarSize(Transform target)
        {
            if (target == null)
                return MinimumPlanarSize;

            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
                return Mathf.Max(MinimumPlanarSize, controller.radius * Mathf.Abs(target.lossyScale.x));

            var colliders = target.GetComponentsInChildren<Collider>();
            if (colliders != null && colliders.Length > 0)
                return ComputePlanarRadiusFromBounds(colliders);

            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
                return ComputePlanarRadiusFromBounds(renderers);

            return MinimumPlanarSize;
        }

        private static float ComputePlanarRadiusFromBounds<T>(T[] components) where T : Component
        {
            Bounds bounds = default;
            bool initialized = false;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                    continue;

                Bounds currentBounds;
                if (components[i] is Collider collider)
                    currentBounds = collider.bounds;
                else if (components[i] is Renderer renderer)
                    currentBounds = renderer.bounds;
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
                return MinimumPlanarSize;

            return Mathf.Max(MinimumPlanarSize, Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z));
        }
    }
}
