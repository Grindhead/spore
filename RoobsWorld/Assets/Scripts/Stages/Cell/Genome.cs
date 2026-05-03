using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Special emergent traits that can unlock through mutation.
    /// Multiple traits can coexist (flags enum). Each trait adds a passive gameplay effect.
    /// </summary>
    [System.Flags]
    public enum GeneticTraits
    {
        None           = 0,
        Photosynthetic = 1 << 0,  // Gains passive energy from light; stronger in Algae/OpenWater biomes
        Toxic          = 1 << 1,  // Deals contact energy drain to other organisms; resists toxin biome
        Armoured       = 1 << 2,  // Reduces incoming damage by a fixed fraction
        Regenerative   = 1 << 3,  // Slowly recovers energy over time when not starving
        Electric       = 1 << 4,  // Periodically emits a stun pulse that briefly slows nearby organisms
    }

    /// <summary>
    /// Evolvable traits and visual genes for cell-stage organisms.
    /// Aligns with memory-bank/cellStageImplementationSteps.md and evolutionSimulationDesign.md.
    /// </summary>
    [System.Serializable]
    public class Genome
    {
        [Header("Traits (0-10)")]
        [Tooltip("Movement speed")]
        [Range(0f, 10f)] public float Speed = 1f;
        [Tooltip("Body size; affects scale and health")]
        [Range(0f, 10f)] public float Size = 1f;
        [Tooltip("Damage dealt")]
        [Range(0f, 10f)] public float Attack = 1f;
        [Tooltip("Damage reduction")]
        [Range(0f, 10f)] public float Defence = 1f;
        [Tooltip("Energy gained from food")]
        [Range(0f, 10f)] public float Digestion = 1f;
        [Tooltip("Energy drain per second (metabolism)")]
        [Range(0f, 10f)] public float MetabolismCost = 0.5f;

        [Header("Visual genes")]
        [Tooltip("Spike level (0-10)")]
        [Range(0f, 10f)] public float Spikes = 0f;
        [Tooltip("Flagella level (0-10)")]
        [Range(0f, 10f)] public float Flagella = 0f;
        [Tooltip("Body color")]
        public Color Color = new Color(0.82f, 0.2f, 0.22f);

        [Header("Special traits")]
        [Tooltip("Emergent traits unlocked through mutation. Multiple can coexist.")]
        public GeneticTraits Traits = GeneticTraits.None;

        /// <summary>Creates a default genome with baseline traits.</summary>
        public static Genome Default()
        {
            return new Genome
            {
                Speed = 1f,
                Size = 1f,
                Attack = 1f,
                Defence = 1f,
                Digestion = 1f,
                MetabolismCost = 0.5f,
                Spikes = 0f,
                Flagella = 0f,
                Color = new Color(0.82f, 0.2f, 0.22f),
                Traits = GeneticTraits.None
            };
        }

        /// <summary>Returns a deep copy of this genome for mutation/offspring.</summary>
        public Genome Clone()
        {
            return new Genome
            {
                Speed = Speed,
                Size = Size,
                Attack = Attack,
                Defence = Defence,
                Digestion = Digestion,
                MetabolismCost = MetabolismCost,
                Spikes = Spikes,
                Flagella = Flagella,
                Color = Color,
                Traits = Traits
            };
        }

        /// <summary>Returns true if this genome has the given trait flag set.</summary>
        public bool HasTrait(GeneticTraits trait) => (Traits & trait) != 0;

        /// <summary>Number of special traits currently active.</summary>
        public int TraitCount
        {
            get
            {
                int n = 0;
                int v = (int)Traits;
                while (v != 0) { n += v & 1; v >>= 1; }
                return n;
            }
        }

        /// <summary>Derived max health from size and defence (trade-off design).</summary>
        public float MaxHealth => Mathf.Max(0.1f, Size * Defence);

        /// <summary>Movement cost factor (speed × metabolism) for trade-offs.</summary>
        public float MovementCostFactor => Speed * MetabolismCost;

        /// <summary>Food-to-energy efficiency.</summary>
        public float FoodEfficiency => Digestion;
    }
}
