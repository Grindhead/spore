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

    /// <summary>
    /// Scriptable definition for a creature part, including visuals, stat modifiers, and placement rules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreaturePart", menuName = "RoobsWorld/Creature Part")]
    public class CreaturePartDef : ScriptableObject
    {
        [Header("Identity")]
        public string PartId;
        public string DisplayName;
        public PartCategory Category;
        public Sprite Icon;

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

        [Header("Costs")]
        public int DNACost = 10;

        [Header("Placement Rules")]
        public int MaxCount = 2;
        public bool RequiresSymmetry = true;
        public int[] AllowedBones;
    }
}
