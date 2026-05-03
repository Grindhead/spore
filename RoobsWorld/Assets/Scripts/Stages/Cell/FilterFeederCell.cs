using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Filter-feeder NPC archetype. Inspired by Paramecium: uses cilia-driven drift and passively
    /// absorbs food particles within <see cref="AbsorptionRadius"/> without actively chasing them.
    /// Classifies as Herbivore (high Digestion genome) and provides a safe, slow-feeding ecological niche.
    /// Implements organismTypesAndBiomes.md §9.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class FilterFeederCell : MonoBehaviour
    {
        // ----- Public constants (used by tests and CellStage) -----

        /// <summary>Radius within which food is passively absorbed each tick.</summary>
        public const float AbsorptionRadius = 3f;

        /// <summary>Fraction of genome Speed used for cilia drift (filter feeders move slowly).</summary>
        public const float WanderSpeedFraction = 0.35f;

        /// <summary>Angular rate of cilia-driven heading oscillation in radians/second.</summary>
        public const float CiliaOscillationRate = 1.8f;

        /// <summary>Maximum heading deviation (radians) from the oscillation.</summary>
        public const float CiliaAmplitude = 0.55f;

        /// <summary>Seconds between passive absorption checks.</summary>
        public const float AbsorptionTickInterval = 0.3f;

        // ----- Archetype genome defaults -----

        public const float GenomeDigestion     = 7f;
        public const float GenomeAttack        = 0f;
        public const float GenomeSpeed         = 2f;
        public const float GenomeFlagella      = 5f;   // dense cilia represented via flagella gene
        public const float GenomeSpikes        = 0f;
        public const float GenomeMetabolism    = 0.4f;

        /// <summary>Distinct pale-teal colour for visual identification in the arena.</summary>
        public static readonly Color ArchetypeColor = new Color(0.25f, 0.75f, 0.85f);

        /// <summary>True: marks this Organism as a filter feeder for queries and tests.</summary>
        public bool IsFilterFeeder => true;

        // ----- Private state -----

        private Organism  _organism;
        private CellStage _cellStage;
        private float     _wanderAngle;
        private float     _absorbTimer;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage   = FindAnyObjectByType<CellStage>();
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);
            _absorbTimer = 0f;

            if (_cellStage != null && _organism != null)
            {
                _organism.GetMetabolismMultiplier = () =>
                {
                    float biome      = Biome.GetMetabolismMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                    float env        = _cellStage.EnvironmentEvents != null ? _cellStage.EnvironmentEvents.GlobalMetabolismMultiplier : 1f;
                    float balance = GameManager.HasInstance ? GameManager.Instance.Modifiers.MetabolismMultiplier : 1f;
                    return biome * env * balance;
                };
                _organism.GetFoodEfficiencyMultiplier = () =>
                    Biome.GetFoodEfficiencyMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
            }
        }

        private void Update()
        {
            if (_organism == null || _organism.IsDead) return;

            float dt = Time.deltaTime;

            // Cilia-driven drift: sinusoidal heading gives a gentle meandering motion.
            _wanderAngle += CiliaOscillationRate * dt;
            float heading = _wanderAngle + Mathf.Sin(_wanderAngle * 1.3f) * CiliaAmplitude;
            var   dir     = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));

            float speedMult = WanderSpeedFraction;
            if (_cellStage != null)
            {
                speedMult *= Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                if (_cellStage.EnvironmentEvents != null)
                    speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;
            }
            _organism.Move(dir, dt, speedMult);

            // Passive absorption tick
            _absorbTimer -= dt;
            if (_absorbTimer <= 0f)
            {
                _absorbTimer = AbsorptionTickInterval;
                TryAbsorbFood();
            }
        }

        private void TryAbsorbFood()
        {
            if (_cellStage == null || _cellStage.FoodManager == null || _organism == null) return;

            var result = _cellStage.FoodManager.GetNearestFood(_organism.transform.position, AbsorptionRadius);
            if (!result.HasValue) return;

            if (_cellStage.FoodManager.ConsumeFood(result.Value.Id))
            {
                float effMult = _organism.GetFoodEfficiencyMultiplier != null
                    ? _organism.GetFoodEfficiencyMultiplier()
                    : 1f;
                _organism.Eat(result.Value.Nutrition, effMult);
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = result.Value.Type });
            }
        }

        // ----- Factory helpers -----

        /// <summary>
        /// Creates a genome tuned for filter feeding: high Digestion, near-zero Attack, slow Speed, dense cilia.
        /// Classifies as <see cref="CellStage.TrophicRole.Herbivore"/> via <see cref="CellStage.ClassifyTrophicRole"/>.
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed         = GenomeSpeed,
                Size          = 1f,
                Attack        = GenomeAttack,
                Defence       = 1f,
                Digestion     = GenomeDigestion,
                MetabolismCost = GenomeMetabolism,
                Spikes        = GenomeSpikes,
                Flagella      = GenomeFlagella,
                Color         = ArchetypeColor,
                Traits        = GeneticTraits.None
            };
        }
    }
}

