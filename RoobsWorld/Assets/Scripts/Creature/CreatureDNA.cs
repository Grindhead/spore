using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Serializable genome for a creature, including body plan, placed parts, and color palette.
    /// </summary>
    [Serializable]
    public class CreatureDNA
    {
        public string CreatureId;
        public string CreatureName = "Unnamed";
        public BodyPlan Body = new BodyPlan();
        public List<PartPlacement> Parts = new List<PartPlacement>();
        public List<CreatureUpgradeRecord> Upgrades = new List<CreatureUpgradeRecord>();
        public CreatureColors Colors = new CreatureColors();
        public int LegacyTraitMask;
        public string LegacyAbilityId;
        public string LegacySummary;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static CreatureDNA FromJson(string json)
        {
            return JsonUtility.FromJson<CreatureDNA>(json);
        }

        public CreatureDNA Clone()
        {
            return FromJson(ToJson());
        }

        /// <summary>
        /// Creates a default starter DNA used when no prior creature exists.
        /// </summary>
        public static CreatureDNA CreateDefault()
        {
            string mouthPart = ResolveStarterPartId("animal_head_001", "animal_jaw_001", "mouth_basic");
            string eyePart = ResolveStarterPartId("animal_ear_001", "eye_basic");
            string legPart = ResolveStarterPartId("animal_leg_001", "leg_basic");

            return new CreatureDNA
            {
                CreatureId = Guid.NewGuid().ToString("N")[..8],
                CreatureName = "Roob",
                Body = new BodyPlan
                {
                    SpineSegments = 2,
                    Symmetry = SymmetryType.Bilateral,
                    BodyScale = Vector3.one,
                    Spine = new List<SpineSegment>
                    {
                        new SpineSegment { Offset = Vector3.zero, Scale = Vector2.one, Curve = 0f },
                        new SpineSegment { Offset = Vector3.zero, Scale = Vector2.one, Curve = 0f }
                    }
                },
                Parts = new List<PartPlacement>
                {
                    new PartPlacement { PartId = mouthPart, AttachBone = 0, Offset = new Vector3(0f, 0.06f, 0.5f) },
                    new PartPlacement { PartId = eyePart, AttachBone = 0, Offset = new Vector3(0.23f, 0.24f, 0.34f), Scale = 0.9f },
                    new PartPlacement { PartId = eyePart, AttachBone = 0, Offset = new Vector3(-0.23f, 0.24f, 0.34f), Scale = 0.9f },
                    new PartPlacement { PartId = legPart, AttachBone = 0, Offset = new Vector3(0.36f, -0.34f, 0.1f) },
                    new PartPlacement { PartId = legPart, AttachBone = 0, Offset = new Vector3(-0.36f, -0.34f, 0.1f) },
                    new PartPlacement { PartId = legPart, AttachBone = 1, Offset = new Vector3(0.34f, -0.34f, -0.08f) },
                    new PartPlacement { PartId = legPart, AttachBone = 1, Offset = new Vector3(-0.34f, -0.34f, -0.08f) },
                },
                Upgrades = new List<CreatureUpgradeRecord>()
            };
        }

        private static string ResolveStarterPartId(params string[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
                return string.Empty;

            for (int i = 0; i < candidates.Length; i++)
            {
                string id = candidates[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (Resources.Load<GameObject>($"Models/CreatureParts/{id}") != null)
                    return id;

                if (Resources.Load<GameObject>($"Models/CellParts/{id}") != null)
                    return id;
            }

            return candidates[0];
        }
    }

    /// <summary>
    /// High-level structural description of a creature's body (spine, symmetry, and global scale).
    /// </summary>
    [Serializable]
    public class BodyPlan
    {
        public int SpineSegments = 1;
        public SymmetryType Symmetry = SymmetryType.Bilateral;
        public Vector3 BodyScale = Vector3.one;
        public float FrontPull = 0f;
        public List<SpineSegment> Spine = new List<SpineSegment>();
    }

    /// <summary>
    /// Per-bone sculpt data used by the creature creator to bend, offset, and resize the body spine.
    /// </summary>
    [Serializable]
    public class SpineSegment
    {
        public Vector3 Offset;
        public Vector2 Scale = Vector2.one;
        public float Curve;
    }

    /// <summary>
    /// Supported symmetry modes for automatically mirroring parts.
    /// </summary>
    public enum SymmetryType
    {
        None,
        Bilateral,
        Radial
    }

    /// <summary>
    /// Placement of a single creature part on a body bone, with local transform overrides.
    /// </summary>
    [Serializable]
    public class PartPlacement
    {
        public string PartId;
        public int AttachBone;
        public Vector3 Offset;
        public Vector3 Rotation;
        public float Scale = 1f;
    }

    /// <summary>
    /// Color palette applied to creature skin and accents.
    /// </summary>
    [Serializable]
    public class CreatureColors
    {
        public Color PrimaryColor = new Color(0.4f, 0.7f, 0.3f);
        public Color SecondaryColor = new Color(0.6f, 0.5f, 0.2f);
        public Color AccentColor = Color.white;
        public CreaturePaintPattern Pattern = CreaturePaintPattern.None;
        public CreatureMaterialFinish Material = CreatureMaterialFinish.Matte;
        public float PatternStrength = 0.5f;
        public List<CreaturePartPaintOverride> PartOverrides = new List<CreaturePartPaintOverride>();
    }

    public enum CreaturePaintPattern
    {
        None,
        Spots,
        Stripes,
        Mottled,
        Countershade,
        Mask
    }

    public enum CreatureMaterialFinish
    {
        Matte,
        Gloss,
        Iridescent,
        Furred,
        Armored
    }

    /// <summary>
    /// Optional color/material override for a specific placed part or part family.
    /// </summary>
    [Serializable]
    public class CreaturePartPaintOverride
    {
        public string PartId;
        public Color PrimaryColor = Color.clear;
        public Color SecondaryColor = Color.clear;
        public Color AccentColor = Color.clear;
        public bool OverridePrimary;
        public bool OverrideSecondary;
        public bool OverrideAccent;
        public bool OverrideMaterial;
        public CreatureMaterialFinish Material = CreatureMaterialFinish.Matte;
    }
}
