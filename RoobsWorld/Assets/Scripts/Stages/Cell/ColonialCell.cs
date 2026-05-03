using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Colonial NPC archetype inspired by Volvox-like cooperative clusters.
    /// Nearby colonies align and cohere into loose formations, with short-range energy sharing
    /// between neighbours to stabilize group survival under local pressure.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class ColonialCell : MonoBehaviour
    {
        // ----- Public tuning constants -----

        /// <summary>Radius used to discover other colony neighbours.</summary>
        public const float ColonyNeighborRadius = 9f;

        /// <summary>Distance below which colonies push apart to avoid overlap.</summary>
        public const float SeparationRadius = 1.6f;

        /// <summary>Radius used for cooperative energy transfer.</summary>
        public const float EnergyShareRadius = 2.4f;

        /// <summary>Energy transferred per second from richer to poorer nearby colony members.</summary>
        public const float EnergySharePerSecond = 0.24f;

        /// <summary>Boids-like direction recomputation interval.</summary>
        public const float ColonyTickInterval = 0.26f;

        /// <summary>Movement fraction for colony drift speed.</summary>
        public const float ColonySpeedFraction = 0.82f;

        /// <summary>Cohesion weight toward colony center of mass.</summary>
        public const float CohesionWeight = 0.62f;

        /// <summary>Separation weight away from crowded neighbours.</summary>
        public const float SeparationWeight = 0.78f;

        /// <summary>Alignment weight toward average neighbour heading.</summary>
        public const float AlignmentWeight = 0.45f;

        /// <summary>Random wander contribution to prevent rigid lockstep.</summary>
        public const float WanderWeight = 0.28f;

        // ----- Archetype genome defaults -----

        public const float GenomeSpeed = 3.2f;
        public const float GenomeSize = 0.78f;
        public const float GenomeAttack = 2.6f;
        public const float GenomeDigestion = 3.8f;
        public const float GenomeFlagella = 3.6f;
        public const float GenomeSpikes = 1.1f;
        public const float GenomeMetabolism = 0.6f;

        /// <summary>Soft cyan-green tint to visually identify colony members.</summary>
        public static readonly Color ArchetypeColor = new Color(0.35f, 0.82f, 0.72f);

        /// <summary>True: marks this organism as part of a colony archetype.</summary>
        public bool IsColonial => true;

        // ----- State -----

        private Organism _organism;
        private CellStage _cellStage;
        private float _tickTimer;
        private Vector3 _cachedDirection;
        private float _wanderAngle;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage = FindAnyObjectByType<CellStage>();
            _tickTimer = Random.Range(0f, ColonyTickInterval);
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);
            _cachedDirection = new Vector3(Mathf.Cos(_wanderAngle), 0f, Mathf.Sin(_wanderAngle)).normalized;

            if (_cellStage != null && _organism != null)
            {
                _organism.GetMetabolismMultiplier = () =>
                {
                    float biome = Biome.GetMetabolismMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                    float env = _cellStage.EnvironmentEvents != null ? _cellStage.EnvironmentEvents.GlobalMetabolismMultiplier : 1f;
                    float diff = GameManager.HasInstance ? GameManager.Instance.Modifiers.MetabolismMultiplier : 1f;
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
            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = ColonyTickInterval;
                _cachedDirection = ComputeColonyDirection();
            }

            float speedMult = ColonySpeedFraction;
            if (_cellStage != null)
            {
                speedMult *= Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                if (_cellStage.EnvironmentEvents != null)
                    speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;
            }

            _organism.Move(_cachedDirection, dt, speedMult);
            ShareEnergy(dt);
        }

        private Vector3 ComputeColonyDirection()
        {
            var hits = Physics.OverlapSphere(
                transform.position,
                ColonyNeighborRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            Vector3 cohesionSum = Vector3.zero;
            Vector3 separationSum = Vector3.zero;
            Vector3 alignmentSum = Vector3.zero;
            int count = 0;

            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == gameObject) continue;
                var otherColony = hit.GetComponent<ColonialCell>();
                if (otherColony == null || otherColony._organism == null || otherColony._organism.IsDead) continue;

                Vector3 diff = hit.transform.position - transform.position;
                diff.y = 0f;
                float dist = diff.magnitude;
                if (dist < 0.01f) continue;

                cohesionSum += hit.transform.position;
                alignmentSum += otherColony._cachedDirection;
                count++;

                if (dist < SeparationRadius)
                    separationSum -= diff.normalized * (1f - dist / SeparationRadius);
            }

            _wanderAngle += 0.8f * ColonyTickInterval;
            Vector3 wander = new Vector3(Mathf.Cos(_wanderAngle), 0f, Mathf.Sin(_wanderAngle * 1.17f));
            Vector3 result = wander * WanderWeight;

            if (count > 0)
            {
                Vector3 center = cohesionSum / count;
                Vector3 cohesionDir = center - transform.position;
                cohesionDir.y = 0f;
                if (cohesionDir.sqrMagnitude > 0.01f)
                    result += cohesionDir.normalized * CohesionWeight;

                if (alignmentSum.sqrMagnitude > 0.01f)
                    result += alignmentSum.normalized * AlignmentWeight;
            }

            if (separationSum.sqrMagnitude > 0.01f)
                result += separationSum.normalized * SeparationWeight;

            return result.sqrMagnitude > 0.01f ? result.normalized : wander.normalized;
        }

        private void ShareEnergy(float dt)
        {
            if (_organism == null || _organism.IsDead || dt <= 0f) return;

            var hits = Physics.OverlapSphere(
                transform.position,
                EnergyShareRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            ColonialCell bestReceiver = null;
            float largestDeficit = 0f;

            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == gameObject) continue;
                var other = hit.GetComponent<ColonialCell>();
                if (other == null || other._organism == null || other._organism.IsDead) continue;

                float deficit = _organism.Energy - other._organism.Energy;
                if (deficit > largestDeficit)
                {
                    largestDeficit = deficit;
                    bestReceiver = other;
                }
            }

            if (bestReceiver == null || largestDeficit < 3f) return;

            float transfer = Mathf.Min(EnergySharePerSecond * dt, largestDeficit * 0.25f);
            if (transfer <= 0f) return;
            _organism.DeductEnergy(transfer);

            float receiverEfficiency = Mathf.Max(0.1f, bestReceiver._organism.Genome != null
                ? bestReceiver._organism.Genome.FoodEfficiency
                : 1f);
            bestReceiver._organism.Eat(transfer / receiverEfficiency);
        }

        /// <summary>
        /// Creates a colony-oriented genome with balanced attack/digestion for omnivore behaviour
        /// and moderate speed suitable for cooperative formation movement.
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed = GenomeSpeed,
                Size = GenomeSize,
                Attack = GenomeAttack,
                Defence = 1.6f,
                Digestion = GenomeDigestion,
                MetabolismCost = GenomeMetabolism,
                Spikes = GenomeSpikes,
                Flagella = GenomeFlagella,
                Color = ArchetypeColor,
                Traits = GeneticTraits.None
            };
        }
    }
}

