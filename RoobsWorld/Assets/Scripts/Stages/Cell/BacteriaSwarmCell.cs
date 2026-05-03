using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Bacteria-swarm NPC archetype. Uses a simplified boids algorithm (cohesion + separation +
    /// random wander) to produce emergent flocking behaviour. Individual bacteria are tiny and fast
    /// with high metabolism; they survive by collectively sweeping food-rich areas.
    /// Implements cellStageOrganismDesign.md §7 (swarming) and organismTypesAndBiomes.md §3.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class BacteriaSwarmCell : MonoBehaviour
    {
        // ----- Public constants (used by tests and CellStage) -----

        /// <summary>Radius within which other bacteria are detected as swarm-mates.</summary>
        public const float SwarmRadius       = 8f;

        /// <summary>Distance below which bacteria push away from each other (separation rule).</summary>
        public const float SeparationRadius  = 1.5f;

        /// <summary>Weight of the cohesion vector (move toward swarm centroid).</summary>
        public const float CohesionWeight    = 0.55f;

        /// <summary>Weight of the separation vector (push away from too-close neighbours).</summary>
        public const float SeparationWeight  = 0.75f;

        /// <summary>Weight of the random wander vector (baseline movement and direction diversity).</summary>
        public const float WanderWeight      = 0.35f;

        /// <summary>Seconds between full boids-computation ticks. Direction is held between ticks.</summary>
        public const float SwarmTickInterval = 0.28f;

        // ----- Archetype genome defaults -----

        public const float GenomeSize        = 0.35f;  // visually tiny
        public const float GenomeSpeed       = 6f;     // fast — outrun most organisms
        public const float GenomeAttack      = 0.5f;
        public const float GenomeDigestion   = 2f;
        public const float GenomeFlagella    = 6f;     // dense flagella = many bacteria flagella
        public const float GenomeSpikes      = 0f;
        public const float GenomeMetabolism  = 1.2f;   // high burn rate — must keep feeding

        /// <summary>Bright lime-green: evokes E.coli / fluorescent bacteria microscopy imagery.</summary>
        public static readonly Color ArchetypeColor = new Color(0.55f, 0.92f, 0.25f);

        /// <summary>True: marks this Organism as a bacterium for queries and tests.</summary>
        public bool IsBacterium => true;

        // ----- Private state -----

        private Organism  _organism;
        private CellStage _cellStage;
        private float     _swarmTimer;
        private Vector3   _cachedSwarmDir;   // reused between ticks
        private float     _wanderAngle;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage = FindAnyObjectByType<CellStage>();

            // Stagger tick phase so all bacteria don't compute on the same frame.
            _swarmTimer    = Random.Range(0f, SwarmTickInterval);
            _wanderAngle   = Random.Range(0f, Mathf.PI * 2f);
            _cachedSwarmDir = Random.insideUnitCircle.normalized.ToVector3XZ();

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

            // Recompute boids direction on tick interval; hold cached value in between.
            _swarmTimer -= dt;
            if (_swarmTimer <= 0f)
            {
                _swarmTimer     = SwarmTickInterval;
                _cachedSwarmDir = ComputeSwarmDirection();
            }

            float speedMult = 1f;
            if (_cellStage != null)
            {
                speedMult *= Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                if (_cellStage.EnvironmentEvents != null)
                    speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;
            }

            _organism.Move(_cachedSwarmDir, dt, speedMult);
        }

        /// <summary>
        /// Simplified boids: cohesion (move toward swarm centroid) + separation (avoid crowding)
        /// + random wander. Physics.OverlapSphere finds nearby bacteria; non-bacteria are ignored.
        /// Returns a normalised XZ direction.
        /// </summary>
        private Vector3 ComputeSwarmDirection()
        {
            var hits = Physics.OverlapSphere(
                transform.position, SwarmRadius,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            Vector3 cohesionSum  = Vector3.zero;
            Vector3 separationSum = Vector3.zero;
            int     neighborCount = 0;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var other = hit.GetComponent<BacteriaSwarmCell>();
                if (other == null || other._organism == null || other._organism.IsDead) continue;

                Vector3 diff = hit.transform.position - transform.position;
                diff.y = 0f;
                float dist = diff.magnitude;
                if (dist < 0.01f) continue;

                cohesionSum += hit.transform.position;
                neighborCount++;

                if (dist < SeparationRadius)
                    separationSum -= diff.normalized * (1f - dist / SeparationRadius);
            }

            // Advance wander angle slowly for smooth meandering.
            _wanderAngle += 0.9f * SwarmTickInterval;
            Vector3 wanderDir = new Vector3(Mathf.Cos(_wanderAngle), 0f, Mathf.Sin(_wanderAngle * 1.15f));

            Vector3 result = wanderDir * WanderWeight;

            if (neighborCount > 0)
            {
                Vector3 centroid     = cohesionSum / neighborCount;
                Vector3 cohesionDir  = centroid - transform.position;
                cohesionDir.y = 0f;
                if (cohesionDir.sqrMagnitude > 0.01f)
                    result += cohesionDir.normalized * CohesionWeight;
            }

            if (separationSum.sqrMagnitude > 0.01f)
                result += separationSum.normalized * SeparationWeight;

            return result.sqrMagnitude > 0.01f ? result.normalized : wanderDir.normalized;
        }

        // ----- Factory -----

        /// <summary>
        /// Creates a genome tuned for bacteria: tiny and fast with high metabolism and dense flagella.
        /// Low Attack and Digestion → Omnivore trophic role (feeds opportunistically on whatever they pass through).
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed          = GenomeSpeed,
                Size           = GenomeSize,
                Attack         = GenomeAttack,
                Defence        = 0.5f,
                Digestion      = GenomeDigestion,
                MetabolismCost = GenomeMetabolism,
                Spikes         = GenomeSpikes,
                Flagella       = GenomeFlagella,
                Color          = ArchetypeColor,
                Traits         = GeneticTraits.None
            };
        }
    }

    /// <summary>Extension to convert a 2D Vector2 to an XZ Vector3 (y = 0).</summary>
    internal static class Vector2Extensions
    {
        public static Vector3 ToVector3XZ(this Vector2 v) => new Vector3(v.x, 0f, v.y);
    }
}

