using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// Normalizes creature part placements against a lightweight skeleton envelope so editor mutations stay valid.
    /// </summary>
    public static class CreatureSkeletonConstraintSolver
    {
        private const int MaxIterations = 3;
        private const float MinSpacing = 0.14f;

        public static void Apply(CreatureDNA dna)
        {
            if (dna == null)
                return;

            dna.Body ??= new BodyPlan();
            dna.Parts ??= new List<PartPlacement>();

            int spineSegments = Mathf.Clamp(dna.Body.SpineSegments, 1, 8);
            dna.Body.SpineSegments = spineSegments;
            dna.Body.BodyScale = ClampBodyScale(dna.Body.BodyScale);
            dna.Body.FrontPull = Mathf.Clamp(dna.Body.FrontPull, -0.3f, 0.8f);

            for (int i = 0; i < dna.Parts.Count; i++)
            {
                var part = dna.Parts[i];
                if (part == null)
                    continue;

                part.AttachBone = Mathf.Clamp(part.AttachBone, 0, spineSegments - 1);
                part.Scale = Mathf.Clamp(part.Scale, 0.1f, 3f);
                part.Offset = ClampOffset(part, dna.Body.BodyScale);
            }

            ResolveOverlapsByBone(dna.Parts);
        }

        private static Vector3 ClampBodyScale(Vector3 scale)
        {
            if (scale == Vector3.zero)
                scale = Vector3.one;

            return new Vector3(
                Mathf.Clamp(scale.x, 0.5f, 2.5f),
                Mathf.Clamp(scale.y, 0.5f, 2.5f),
                Mathf.Clamp(scale.z, 0.5f, 2.5f));
        }

        private static Vector3 ClampOffset(PartPlacement part, Vector3 bodyScale)
        {
            float scaleFactor = Mathf.Max(0.1f, part.Scale);
            float xLimit = bodyScale.x * 0.95f + 0.22f * scaleFactor;
            float yMin = -bodyScale.y * 0.9f;
            float yMax = bodyScale.y * 1.1f;
            float zLimit = bodyScale.z * 1.1f + 0.18f * scaleFactor;
            Vector3 offset = part.Offset;

            bool frontFacingOnly = StartsWith(part.PartId, "mouth") || StartsWith(part.PartId, "eye");
            if (frontFacingOnly)
                offset.z = Mathf.Max(0.02f, offset.z);

            return new Vector3(
                Mathf.Clamp(offset.x, -xLimit, xLimit),
                Mathf.Clamp(offset.y, yMin, yMax),
                Mathf.Clamp(offset.z, -zLimit, zLimit));
        }

        private static void ResolveOverlapsByBone(List<PartPlacement> parts)
        {
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool movedAny = false;
                for (int i = 0; i < parts.Count; i++)
                {
                    var a = parts[i];
                    if (a == null)
                        continue;

                    for (int j = i + 1; j < parts.Count; j++)
                    {
                        var b = parts[j];
                        if (b == null || a.AttachBone != b.AttachBone)
                            continue;

                        Vector2 aXZ = new Vector2(a.Offset.x, a.Offset.z);
                        Vector2 bXZ = new Vector2(b.Offset.x, b.Offset.z);
                        Vector2 delta = bXZ - aXZ;
                        float dist = delta.magnitude;
                        if (dist >= MinSpacing)
                            continue;

                        Vector2 dir = dist > 0.0001f ? delta / dist : Vector2.right;
                        float push = (MinSpacing - dist) * 0.5f;
                        a.Offset -= new Vector3(dir.x * push, 0f, dir.y * push);
                        b.Offset += new Vector3(dir.x * push, 0f, dir.y * push);
                        movedAny = true;
                    }
                }

                if (!movedAny)
                    break;
            }
        }

        private static bool StartsWith(string partId, string prefix)
        {
            return !string.IsNullOrEmpty(partId) && partId.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
