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
            return new CreatureDNA
            {
                CreatureId = Guid.NewGuid().ToString("N")[..8],
                CreatureName = "Roob",
                Body = new BodyPlan
                {
                    SpineSegments = 3,
                    Symmetry = SymmetryType.Bilateral,
                    BodyScale = Vector3.one
                },
                Parts = new List<PartPlacement>
                {
                    new PartPlacement { PartId = "mouth_basic", AttachBone = 0, Offset = Vector3.forward * 0.5f },
                    new PartPlacement { PartId = "eye_basic", AttachBone = 0, Offset = new Vector3(0.2f, 0.3f, 0.4f) },
                    new PartPlacement { PartId = "eye_basic", AttachBone = 0, Offset = new Vector3(-0.2f, 0.3f, 0.4f) },
                    new PartPlacement { PartId = "leg_basic", AttachBone = 1, Offset = new Vector3(0.4f, -0.3f, 0) },
                    new PartPlacement { PartId = "leg_basic", AttachBone = 1, Offset = new Vector3(-0.4f, -0.3f, 0) },
                    new PartPlacement { PartId = "leg_basic", AttachBone = 2, Offset = new Vector3(0.4f, -0.3f, 0) },
                    new PartPlacement { PartId = "leg_basic", AttachBone = 2, Offset = new Vector3(-0.4f, -0.3f, 0) },
                },
                Upgrades = new List<CreatureUpgradeRecord>()
            };
        }
    }

    /// <summary>
    /// High-level structural description of a creature's body (spine, symmetry, and global scale).
    /// </summary>
    [Serializable]
    public class BodyPlan
    {
        public int SpineSegments = 3;
        public SymmetryType Symmetry = SymmetryType.Bilateral;
        public Vector3 BodyScale = Vector3.one;
        public float FrontPull = 0f;
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
    }
}
