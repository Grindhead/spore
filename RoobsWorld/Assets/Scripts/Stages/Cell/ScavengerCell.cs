using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Scavenger NPC archetype. Actively pursues dead-cell (Meat) food within <see cref="MeatSeekRadius"/>,
    /// consuming it once within <see cref="ConsumeRadius"/>. Falls back to slow wander when no meat is
    /// nearby. Completes the food-web loop: Algae → Herbivore → Predator → Scavenger.
    /// Implements cellStageOrganismDesign.md §5 and organismTypesAndBiomes.md food-web cycle.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class ScavengerCell : MonoBehaviour
    {
        // ----- Public constants -----

        /// <summary>Radius within which the scavenger detects and moves toward Meat-type food.</summary>
        public const float MeatSeekRadius      = 20f;

        /// <summary>Radius within which detected meat is automatically consumed.</summary>
        public const float ConsumeRadius       = 1.8f;

        /// <summary>Full-speed fraction used while actively chasing a meat target.</summary>
        public const float ChaseSpeedFraction  = 1.0f;

        /// <summary>Speed fraction used when wandering (no meat target found).</summary>
        public const float WanderSpeedFraction = 0.55f;

        /// <summary>Seconds between meat-detection scans.</summary>
        public const float SeekTickInterval    = 0.25f;

        // ----- Archetype genome defaults -----

        public const float GenomeDigestion  = 6f;
        public const float GenomeAttack     = 1f;
        public const float GenomeSpeed      = 4f;
        public const float GenomeSpikes     = 1f;
        public const float GenomeFlagella   = 2f;
        public const float GenomeMetabolism = 0.55f;

        /// <summary>Amber-brown colour: visually evokes a decomposer / detritivore.</summary>
        public static readonly Color ArchetypeColor = new Color(0.75f, 0.45f, 0.10f);

        /// <summary>True: marks this Organism as a scavenger for queries and tests.</summary>
        public bool IsScavenger => true;

        // ----- Private state -----

        private Organism  _organism;
        private CellStage _cellStage;
        private float     _seekTimer;
        private Vector3   _meatTarget;
        private bool      _hasMeatTarget;
        private int       _meatTargetId;
        private float     _wanderAngle;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage   = FindAnyObjectByType<CellStage>();
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);
            _seekTimer   = Random.Range(0f, SeekTickInterval); // stagger ticks across instances

            if (_cellStage != null && _organism != null)
            {
                _organism.GetMetabolismMultiplier = () =>
                {
                    float biome = Biome.GetMetabolismMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                    float env   = _cellStage.EnvironmentEvents != null ? _cellStage.EnvironmentEvents.GlobalMetabolismMultiplier : 1f;
                    float diff  = GameManager.HasInstance ? GameManager.Instance.Modifiers.MetabolismMultiplier : 1f;
                    return biome * env * diff;
                };
                _organism.GetFoodEfficiencyMultiplier = () =>
                    Biome.GetFoodEfficiencyMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
            }
        }

        private void Update()
        {
            if (_organism == null || _organism.IsDead) return;

            float dt = Time.deltaTime;

            // Periodic meat scan
            _seekTimer -= dt;
            if (_seekTimer <= 0f)
            {
                _seekTimer = SeekTickInterval;
                UpdateMeatTarget();
            }

            // Movement: chase target or wander
            Vector3 dir;
            float   speedFraction;
            if (_hasMeatTarget)
            {
                var toMeat = _meatTarget - _organism.transform.position;
                toMeat.y = 0f;
                if (toMeat.sqrMagnitude > 0.01f)
                {
                    dir           = toMeat.normalized;
                    speedFraction = ChaseSpeedFraction;
                }
                else
                {
                    _hasMeatTarget = false;
                    dir            = GetWanderDir(dt);
                    speedFraction  = WanderSpeedFraction;
                }
            }
            else
            {
                dir           = GetWanderDir(dt);
                speedFraction = WanderSpeedFraction;
            }

            float speedMult = speedFraction;
            if (_cellStage != null)
            {
                speedMult *= Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                if (_cellStage.EnvironmentEvents != null)
                    speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;
            }

            _organism.Move(dir, dt, speedMult);

            // Consume meat when close enough
            if (_hasMeatTarget)
                TryConsumeMeat();
        }

        private void UpdateMeatTarget()
        {
            if (_cellStage?.FoodManager == null) return;
            var result = _cellStage.FoodManager.GetNearestFoodOfType(
                _organism.transform.position, MeatSeekRadius, FoodType.Meat);
            if (result.HasValue)
            {
                _meatTarget    = result.Value.Position;
                _meatTargetId  = result.Value.Id;
                _hasMeatTarget = true;
            }
            else
            {
                _hasMeatTarget = false;
            }
        }

        private void TryConsumeMeat()
        {
            if (_cellStage?.FoodManager == null || _organism == null) return;
            var result = _cellStage.FoodManager.GetNearestFoodOfType(
                _organism.transform.position, ConsumeRadius, FoodType.Meat);
            if (!result.HasValue) return;

            if (_cellStage.FoodManager.ConsumeFood(result.Value.Id))
            {
                float effMult = _organism.GetFoodEfficiencyMultiplier != null
                    ? _organism.GetFoodEfficiencyMultiplier()
                    : 1f;
                _organism.Eat(result.Value.Nutrition, effMult);
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = FoodType.Meat });
                _hasMeatTarget = false;
            }
        }

        private Vector3 GetWanderDir(float dt)
        {
            _wanderAngle += 0.7f * dt;
            return new Vector3(Mathf.Cos(_wanderAngle), 0f, Mathf.Sin(_wanderAngle * 1.1f)).normalized;
        }

        // ----- Factory -----

        /// <summary>
        /// Creates a genome tuned for scavenging: high Digestion for efficient meat processing,
        /// decent Speed to reach dead cells quickly, light Spikes for minor defence.
        /// Classifies as <see cref="CellStage.TrophicRole.Herbivore"/> (Digestion dominates Attack)
        /// which contributes to niche-fill herbivore counts and keeps balance stable.
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed          = GenomeSpeed,
                Size           = 1f,
                Attack         = GenomeAttack,
                Defence        = 1f,
                Digestion      = GenomeDigestion,
                MetabolismCost = GenomeMetabolism,
                Spikes         = GenomeSpikes,
                Flagella       = GenomeFlagella,
                Color          = ArchetypeColor,
                Traits         = GeneticTraits.None
            };
        }
    }
}

