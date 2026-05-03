using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Parasite NPC archetype. Seeks larger hosts, latches onto them, and drains energy over time.
    /// Adds a parasite-pressure loop to the ecosystem as described in organismTypesAndBiomes section 6.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class ParasiteCell : MonoBehaviour
    {
        // ----- Public constants -----

        /// <summary>Radius used to scan for valid hosts.</summary>
        public const float HostSeekRadius = 18f;

        /// <summary>Distance at which a parasite can attach to a host.</summary>
        public const float AttachRadius = 1.6f;

        /// <summary>Maximum allowed distance from host before forced detach.</summary>
        public const float DetachRadius = 7f;

        /// <summary>Energy drained per second from an attached host.</summary>
        public const float DrainPerSecond = 0.09f;

        /// <summary>Maximum time in seconds a parasite can stay attached before searching again.</summary>
        public const float MaxAttachDuration = 6f;

        /// <summary>Seconds between host-scan ticks while searching.</summary>
        public const float SeekTickInterval = 0.25f;

        /// <summary>Speed fraction while pursuing a host target.</summary>
        public const float ChaseSpeedFraction = 0.95f;

        /// <summary>Speed fraction while wandering without a target.</summary>
        public const float WanderSpeedFraction = 0.45f;

        /// <summary>Required host size ratio relative to parasite size for valid targeting.</summary>
        public const float MinimumHostSizeRatio = 1.25f;

        // ----- Archetype genome defaults -----

        public const float GenomeSize = 0.42f;
        public const float GenomeSpeed = 5.2f;
        public const float GenomeAttack = 2.3f;
        public const float GenomeDigestion = 3.0f;
        public const float GenomeFlagella = 3.5f;
        public const float GenomeSpikes = 0.4f;
        public const float GenomeMetabolism = 0.9f;

        /// <summary>Distinct purple-magenta tint for parasite readability.</summary>
        public static readonly Color ArchetypeColor = new Color(0.62f, 0.2f, 0.78f);

        /// <summary>True: marks this Organism as a parasite for tests and queries.</summary>
        public bool IsParasite => true;

        // ----- Private state -----

        private Organism _organism;
        private CellStage _cellStage;
        private float _seekTimer;
        private float _wanderAngle;

        private Transform _hostTransform;
        private PlayerCell _hostPlayer;
        private Organism _hostOrganism;
        private bool _isAttached;
        private float _attachedTimer;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _cellStage = FindAnyObjectByType<CellStage>();
            _seekTimer = Random.Range(0f, SeekTickInterval);
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);

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

            if (_isAttached)
            {
                if (!HasValidHost())
                {
                    Detach();
                }
                else
                {
                    _attachedTimer += dt;
                    if (_attachedTimer >= MaxAttachDuration || DistanceToHost() > DetachRadius)
                    {
                        Detach();
                    }
                    else
                    {
                        FollowHostAnchor(dt);
                        DrainHost(dt);
                        return;
                    }
                }
            }

            _seekTimer -= dt;
            if (_seekTimer <= 0f)
            {
                _seekTimer = SeekTickInterval;
                UpdateHostTarget();
            }

            Vector3 dir;
            float speedFraction;
            if (HasValidHost())
            {
                var toHost = _hostTransform.position - _organism.transform.position;
                toHost.y = 0f;
                float dist = toHost.magnitude;
                if (dist <= AttachRadius)
                {
                    Attach();
                    return;
                }

                dir = toHost.sqrMagnitude > 0.01f ? toHost.normalized : GetWanderDirection(dt);
                speedFraction = ChaseSpeedFraction;
            }
            else
            {
                dir = GetWanderDirection(dt);
                speedFraction = WanderSpeedFraction;
            }

            _organism.Move(dir, dt, GetSpeedMultiplier(speedFraction));
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

        private void UpdateHostTarget()
        {
            ClearHostIfInvalid();

            float mySize = transform.localScale.x;
            float bestDist = float.MaxValue;
            Transform bestTransform = null;
            PlayerCell bestPlayer = null;
            Organism bestOrganism = null;

            var hits = Physics.OverlapSphere(
                transform.position,
                HostSeekRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || hit.transform == transform) continue;

                var candidateTransform = hit.transform;
                float dist = Vector3.Distance(transform.position, candidateTransform.position);
                if (dist > HostSeekRadius || dist >= bestDist) continue;

                PlayerCell player = candidateTransform.GetComponent<PlayerCell>();
                if (player != null)
                {
                    if (player.IsDead) continue;
                    if (!IsHostLargeEnough(mySize, player.transform.localScale.x)) continue;

                    bestDist = dist;
                    bestTransform = candidateTransform;
                    bestPlayer = player;
                    bestOrganism = null;
                    continue;
                }

                Organism organism = candidateTransform.GetComponent<Organism>();
                if (organism == null || organism == _organism || organism.IsDead) continue;
                if (organism.GetComponent<ParasiteCell>() != null) continue;
                if (!IsHostLargeEnough(mySize, organism.transform.localScale.x)) continue;

                bestDist = dist;
                bestTransform = candidateTransform;
                bestPlayer = null;
                bestOrganism = organism;
            }

            _hostTransform = bestTransform;
            _hostPlayer = bestPlayer;
            _hostOrganism = bestOrganism;
        }

        private static bool IsHostLargeEnough(float parasiteScale, float hostScale)
        {
            return hostScale >= parasiteScale * MinimumHostSizeRatio;
        }

        private bool HasValidHost()
        {
            if (_hostTransform == null) return false;
            if (_hostPlayer != null) return !_hostPlayer.IsDead;
            if (_hostOrganism != null) return !_hostOrganism.IsDead;
            return false;
        }

        private void ClearHostIfInvalid()
        {
            if (HasValidHost()) return;
            _hostTransform = null;
            _hostPlayer = null;
            _hostOrganism = null;
        }

        private float DistanceToHost()
        {
            if (!HasValidHost()) return float.MaxValue;
            return Vector3.Distance(_organism.transform.position, _hostTransform.position);
        }

        private void Attach()
        {
            if (!HasValidHost()) return;
            _isAttached = true;
            _attachedTimer = 0f;
        }

        private void Detach()
        {
            _isAttached = false;
            _attachedTimer = 0f;
            _seekTimer = 0f;
        }

        private void FollowHostAnchor(float dt)
        {
            if (!HasValidHost()) return;

            Vector3 toParasite = _organism.transform.position - _hostTransform.position;
            toParasite.y = 0f;
            if (toParasite.sqrMagnitude < 0.01f)
                toParasite = Vector3.right;

            float hostScale = _hostTransform.localScale.x;
            float myScale = _organism.transform.localScale.x;
            float anchorDist = hostScale * 0.55f + myScale * 0.35f;
            Vector3 anchor = _hostTransform.position + toParasite.normalized * anchorDist;

            Vector3 dir = anchor - _organism.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                _organism.Move(dir.normalized, dt, GetSpeedMultiplier(ChaseSpeedFraction * 1.15f));
        }

        private void DrainHost(float dt)
        {
            float amount = DrainPerSecond * dt;
            if (amount <= 0f || !HasValidHost()) return;

            if (_hostPlayer != null)
                _hostPlayer.TakeDamage(amount);
            else if (_hostOrganism != null)
                _hostOrganism.TakeDamage(amount);

            _organism.Eat(amount * 0.9f);
        }

        private Vector3 GetWanderDirection(float dt)
        {
            _wanderAngle += 0.8f * dt;
            return new Vector3(Mathf.Cos(_wanderAngle), 0f, Mathf.Sin(_wanderAngle * 1.21f)).normalized;
        }

        /// <summary>
        /// Creates a parasite-tuned genome: small, fast enough to latch on, and balanced attack/digestion.
        /// Balanced attack-vs-digestion keeps trophic role Omnivore for ecosystem niche distribution.
        /// </summary>
        public static Genome BuildGenome()
        {
            return new Genome
            {
                Speed = GenomeSpeed,
                Size = GenomeSize,
                Attack = GenomeAttack,
                Defence = 0.8f,
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

