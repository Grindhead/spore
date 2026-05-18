using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// High-level functional category for a creature part (used for stats and editor grouping).
    /// </summary>
    public enum PartCategory
    {
        Mouth,
        Eye,
        Leg,
        Arm,
        Wing,
        Tail,
        Horn,
        Fin,
        Shell,
        Spike
    }

    public enum CreaturePartRarity
    {
        Common,
        Uncommon,
        Rare,
        Apex
    }

    public enum CreatureVerb
    {
        Bite,
        Charge,
        Strike,
        Spit,
        Jump,
        Glide,
        Sprint,
        Sing,
        Charm,
        Pose,
        Sneak,
        CallPack
    }

    /// <summary>
    /// Scriptable definition for a creature part, including visuals, stat modifiers, and placement rules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreaturePart", menuName = "RoobsWorld/Creature Part")]
    public class CreaturePartDef : ScriptableObject
    {
        [Header("Identity")]
        public string PartId;
        public string DisplayName;
        public string Tooltip;
        public PartCategory Category;
        public CreaturePartRarity Rarity = CreaturePartRarity.Common;
        public Sprite Icon;
        public string IconGlyph;

        [Header("Visual")]
        public GameObject ModelPrefab;
        public Vector3 DefaultScale = Vector3.one;

        [Header("Stat Modifiers")]
        public float SpeedModifier;
        public float HealthModifier;
        public float AttackModifier;
        public float DefenseModifier;
        public float SocialModifier;
        public float StealthModifier;
        public CreatureVerb[] Verbs = System.Array.Empty<CreatureVerb>();
        public string AnimationHint;

        [Header("Costs")]
        public int DNACost = 10;

        [Header("Placement Rules")]
        public int MaxCount = 2;
        public bool RequiresSymmetry = true;
        public int[] AllowedBones;
        public bool HiddenInCatalog;
        public string ProgressionFamily;
        public int ProgressionTier = 1;
    }
}
