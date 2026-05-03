using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Photosynthetic NPC archetype. Stays around light-rich algae bands and survives through
    /// passive trait energy plus local plant absorption, adding a stable producer niche.
    /// Implements organismTypesAndBiomes section 5.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class PhotosyntheticCell : MonoBehaviour
    {
        // ----- Public constants -----

        /// <summary>Radius for passive plant absorption while drifting through algae zones.</summary>
        public const float PlantAbsorptionRadius = 2.4f;

        /// <summary>Seconds between passive plant absorption checks.</summary>
        public const float AbsorptionTickInterval = 0.35f;

        /// <summary>Speed fraction while wandering; photosynthetic cells are intentionally slow.</summary>
        public const float WanderSpeedFraction = 0.5f;

        /// <summary>Blend strength toward algae-ring drift when computing movement direction.</summary>
        public const float LightSeekingStrength = 0.58f;

        /// <summary>Oscillation rate of natural drifting motion.</summary>
        public const float DriftOscillationRate = 0.7f;

        /// <summary>Maximum wander heading variance in radians.</summary>
        public const float DriftHeadingVariance = 0.45f;

        // ----- Archetype genome defaults -----

        public const float GenomeSpeed = 2.4f;
        public const float GenomeSize = 0.9f;
        public const float GenomeAttack = 0.35f;
        public const float GenomeDigestion = 3.6f;
        public const float GenomeFlagella = 2.8f;
        public const float GenomeSpikes = 0f;
        public const float GenomeMetabolism = 0.45f;

        /// <summary>Distinct bright green tint for readability in the algae band.</summary>
        public static readonly Color ArchetypeColor = new Color(0.28f, 0.86f, 0.36f);

        /// <summary>True: marks this Organism as photosynthetic for tests and queries.</summary>
        public bool IsPhotosynthetic => true;

        // ----- Private state -----

        private Organism _organism;
        private CellStage _cellStage;
        private float _driftAngle;
        private float _absorbTimer;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage = FindAnyObjectByType<CellStage>();
            _driftAngle = Random.Range(0f, Mathf.PI * 2f);
            _absorbTimer = Random.Range(0f, AbsorptionTickInterval);

            if (_cellStage != null && _organism != null)
            {
                _organism.GetMetabolismMultiplier = () =>
                {
                    float biome = Biome.GetMetabolismMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                    float env = _cellStage.EnvironmentEvents != null ? _cellStage.EnvironmentEvents.GlobalMetabolismMultiplier : 1f;
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
            Vector3 dir = ComputeDriftDirection(dt);
            _organism.Move(dir, dt, GetSpeedMultiplier(WanderSpeedFraction));

            _absorbTimer -= dt;
            if (_absorbTimer <= 0f)
            {
                _absorbTimer = AbsorptionTickInterval;
                TryAbsorbNearbyPlant();
            }
        }

        private Vector3 ComputeDriftDirection(float dt)
        {
            _driftAngle += DriftOscillationRate * dt;
            float heading = _driftAngle + Mathf.Sin(_driftAngle * 1.2f) * DriftHeadingVariance;
            Vector3 wander = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading)).normalized;

            if (_cellStage == null || _organism?.Genome == null)
                return wander;

            Vector3 driftToLightBand = CellOrganismAI.GetBiomeAffinityDrift(
                _organism.transform.position,
                _cellStage.ArenaCenter,
                _cellStage.ArenaRadius,
                _organism.Genome);

            if (driftToLightBand.sqrMagnitude < 0.0001f)
                return wander;

            return Vector3.Lerp(wander, driftToLightBand.normalized, LightSeekingStrength).normalized;
        }

        private void TryAbsorbNearbyPlant()
        {
            if (_cellStage?.FoodManager == null || _organism == null) return;

            var result = _cellStage.FoodManager.GetNearestFoodOfType(
                _organism.transform.position,
                PlantAbsorptionRadius,
                FoodType.Plant);
            if (!result.HasValue) return;

            if (_cellStage.FoodManager.ConsumeFood(result.Value.Id))
            {
                float effMult = _organism.GetFoodEfficiencyMultiplier != null
                    ? _organism.GetFoodEfficiencyMultiplier()
                    : 1f;
                _organism.Eat(result.Value.Nutrition, effMult);
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = FoodType.Plant });
            }
        }

        private float GetSpeedMultiplier(float fraction)
        {
            float speedMult = fraction;
            if (_cellStage != null)
            {
                speedMult *= Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                if (_cellStage.EnvironmentEvents != null)
                    speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;
            }
            return speedMult;
        }

        /// <summary>
        /// Creates a producer-style genome: low attack, moderate digestion, low metabolism, and
        /// Photosynthetic trait enabled for passive energy gain.
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed = GenomeSpeed,
                Size = GenomeSize,
                Attack = GenomeAttack,
                Defence = 0.9f,
                Digestion = GenomeDigestion,
                MetabolismCost = GenomeMetabolism,
                Spikes = GenomeSpikes,
                Flagella = GenomeFlagella,
                Color = ArchetypeColor,
                Traits = GeneticTraits.Photosynthetic
            };
        }
    }
}

