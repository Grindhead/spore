using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Drives an Organism by ticking Behaviour.Decide on an interval while applying cached movement every frame.
    /// Step 2 cell-stage evolution. Set optional food/threat/prey targets or leave null to Wander.
    /// </summary>
    [RequireComponent(typeof(Organism))]
    public class CellOrganismAI : MonoBehaviour
    {
        [Header("Optional targets (assign or leave null to Wander)")]
        [Tooltip("Nearest food position; if set, used when energy is low.")]
        public Transform NearestFood;
        [Tooltip("Nearest threat; if set, organism will Flee.")]
        public Transform NearestThreat;
        [Tooltip("Nearest prey; if set, organism will Attack.")]
        public Transform NearestPrey;

        [Header("Tick")]
        [Tooltip("Seconds between AI decisions; 0 = every frame. Step 9: use BalanceConstants.CellAITickIntervalSeconds for scale.")]
        [Min(0f)]
        public float TickInterval = RoobsWorld.Core.BalanceConstants.CellAITickIntervalSeconds;

        private Organism _organism;
        private float _nextTickTime;
        private Vector3 _wanderDirection;
        private CellStage _cellStage;
        private Vector3 _cachedMoveDirection;
        private float _cachedSpeedMultiplier = 1f;
        private bool _hasCachedMoveDirection;
        private float _wanderHeading;
        private float _wanderNoiseTime;
        private Vector3 _lastDecisionPosition;
        private Vector3 _lastCommittedMoveDirection;
        private float _stuckDecisionTimer;
        private float _forcedExploreTimer;
        private Vector3 _forcedExploreDirection;
        private float _nextThreatExperienceTime;
        private BehaviourState _lastBehaviourState = BehaviourState.Wander;

        private void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        private void Start()
        {
            _nextTickTime = Time.time;
            _wanderDirection = new Vector3(1f, 0f, 0f);
            _cachedMoveDirection = _wanderDirection;
            _cachedSpeedMultiplier = 1f;
            _hasCachedMoveDirection = false;
            _wanderHeading = Random.Range(0f, Mathf.PI * 2f);
            _wanderNoiseTime = Random.Range(0f, 100f);
            _lastDecisionPosition = transform.position;
            _lastCommittedMoveDirection = _wanderDirection;
            _forcedExploreDirection = _wanderDirection;
            _cellStage = FindAnyObjectByType<CellStage>();
            if (_cellStage != null && _organism != null)
            {
                _organism.GetMetabolismMultiplier = () =>
                {
                    float biome = Biome.GetMetabolismMultiplier(_cellStage.GetBiomeAt(_organism.transform.position));
                    float env = _cellStage.EnvironmentEvents != null ? _cellStage.EnvironmentEvents.GlobalMetabolismMultiplier : 1f;
                    float balance = GameManager.HasInstance ? GameManager.Instance.Modifiers.MetabolismMultiplier : 1f;
                    return biome * env * balance;
                };
                _organism.GetFoodEfficiencyMultiplier = () => Biome.GetFoodEfficiencyMultiplier(
                    _cellStage.GetBiomeAt(_organism.transform.position));
            }
        }

        private void Update()
        {
            if (_organism == null || _organism.IsDead) return;

            float frameDelta = Time.deltaTime;
            bool shouldRetick = TickInterval <= 0f || Time.time >= _nextTickTime;
            if (shouldRetick)
            {
                if (TickInterval > 0f)
                    _nextTickTime = Time.time + TickInterval;

                EvaluateDecision();
            }

            if (!_hasCachedMoveDirection || _cachedMoveDirection.sqrMagnitude < 0.01f || frameDelta <= 0f)
                return;

            Vector3 moveDir = Movement.ApplyWobble(_cachedMoveDirection, Time.time, _organism.Genome);
            _organism.Move(moveDir, frameDelta, _cachedSpeedMultiplier);
        }

        private void EvaluateDecision()
        {
            var ctx = BuildContext();
            var result = Behaviour.DecideWithMemory(ctx, _lastBehaviourState);
            _lastBehaviourState = result.State;
            RecordAdaptivePressure(result.State);

            if (result.State == BehaviourState.Reproduce)
            {
                var pool = _cellStage != null ? _cellStage.GetOrganismPool() : null;
                ReproductionSystem.TryReproduce(_organism, pool: pool);
                _hasCachedMoveDirection = false;
                return;
            }

            Vector3 dir = result.State == BehaviourState.Wander
                ? GetNextWanderDirection()
                : Behaviour.GetMoveDirection(result, _organism.transform.position);
            UpdateExplorationState(result.State);
            dir = ApplyForcedExploration(dir, result.State);
            if (dir.sqrMagnitude < 0.01f)
            {
                _hasCachedMoveDirection = false;
                return;
            }

            bool allowImmediateReverse = result.State == BehaviourState.Flee;
            dir = StabilizeMoveDirection(_lastCommittedMoveDirection, dir, allowImmediateReverse);
            if (dir.sqrMagnitude < 0.01f)
            {
                _hasCachedMoveDirection = false;
                return;
            }

            float speedMult = _cellStage != null
                ? Biome.GetSpeedMultiplier(_cellStage.GetBiomeAt(_organism.transform.position))
                : 1f;
            if (_cellStage?.EnvironmentEvents != null)
                speedMult *= _cellStage.EnvironmentEvents.GlobalSpeedMultiplier;

            _cachedMoveDirection = dir.normalized;
            _lastCommittedMoveDirection = _cachedMoveDirection;
            _cachedSpeedMultiplier = speedMult;
            _hasCachedMoveDirection = true;
        }

        private const float MaxFoodSeekDistance = 25f;
        private const float MaxPreySeekDistance = 28f;
        private const float MaxThreatSeekDistance = 25f;
        private const float PreySizeRatio = 0.82f;
        private const float ThreatSizeRatio = 1.18f;
        private const float PlayerThreatSizeRatio = 1.0f;
        private const float BloomHotspotSeekDistance = 45f;
        private const float StuckDecisionDistance = 0.28f;
        private const float StuckDecisionDuration = 1.0f;
        private const float ForcedExploreDuration = 2.2f;
        private const float ForcedExploreBlend = 0.68f;
        private const float ThreatExperienceCooldown = 4f;
        private const float MinNonFleeTurnDot = -0.15f;
        private const float DirectionPersistenceBlend = 0.55f;

        // ---- Chemotaxis / biome affinity (cellStageOrganismDesign §7) ----

        /// <summary>
        /// Blend weight applied to the biome-affinity drift when computing the wander direction.
        /// 0 = no affinity; 1 = move purely toward preferred zone (ignores randomness).
        /// </summary>
        public const float BiomeAffinityStrength = 0.35f;

        /// <summary>
        /// Returns the preferred orbit radius as a fraction of arenaRadius, or −1 when the genome
        /// has no biome affinity. Drives emergent spatial clustering:
        /// Photosynthetic / Herbivore → AlgaeForest ring (~0.75);
        /// Carnivore → OpenWater ring (~0.40);
        /// Low-metabolism → DeepZone (~0.10).
        /// </summary>
        public static float GetPreferredRadiusFraction(Genome g)
        {
            if (g == null) return -1f;
            if (g.HasTrait(GeneticTraits.Photosynthetic)) return 0.75f;
            float digestAdv = g.Digestion - g.Attack;
            if (digestAdv >=  2f) return 0.75f;   // herbivore → AlgaeForest
            if (digestAdv <= -2f) return 0.40f;   // carnivore → OpenWater
            if (g.MetabolismCost < 0.3f) return 0.10f;  // low-cost → DeepZone
            return -1f;  // balanced genome, no biome preference
        }

        /// <summary>
        /// Returns a unit-scaled direction that biases an organism toward the ring at its preferred
        /// radius. Strength scales with distance from the target ring (stronger far away, zero when
        /// already on-ring). Returns Vector3.zero when there is no affinity or arenaRadius ≤ 0.
        /// </summary>
        /// <summary>
        /// Returns true when this genome should actively migrate toward NutrientBloom hotspots.
        /// Photosynthetic and herbivore genomes are considered bloom seekers.
        /// </summary>
        public static bool ShouldSeekBloomHotspots(Genome g)
        {
            if (g == null) return false;
            if (g.HasTrait(GeneticTraits.Photosynthetic)) return true;
            return CellStage.ClassifyTrophicRole(g) == CellStage.TrophicRole.Herbivore;
        }

        /// <summary>
        /// Returns a normalized drift vector toward a nutrient bloom hotspot.
        /// Strength scales with hotspot distance relative to arena size (0..1).
        /// </summary>
        public static Vector3 GetBloomHotspotDrift(Vector3 position, Vector3 hotspot, float arenaRadius)
        {
            Vector3 toHotspot = hotspot - position;
            toHotspot.y = 0f;
            if (toHotspot.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            float safeArenaRadius = Mathf.Max(1f, arenaRadius);
            float strength = Mathf.Clamp01(toHotspot.magnitude / safeArenaRadius);
            return toHotspot.normalized * strength;
        }

        public static Vector3 GetBiomeAffinityDrift(
            Vector3 position, Vector3 arenaCenter, float arenaRadius, Genome genome)
        {
            float prefFrac = GetPreferredRadiusFraction(genome);
            if (prefFrac < 0f || arenaRadius <= 0f) return Vector3.zero;

            Vector3 toCenter = arenaCenter - position;
            toCenter.y = 0f;
            float currentDist = toCenter.magnitude;
            if (currentDist < 0.01f) return Vector3.zero;

            float preferredDist  = arenaRadius * prefFrac;
            float radialError    = currentDist - preferredDist;

            // Positive error → too far from centre → drift inward; negative → drift outward.
            Vector3 radialDir = radialError > 0f ? toCenter.normalized : -toCenter.normalized;
            float   strength  = Mathf.Clamp01(Mathf.Abs(radialError) / arenaRadius);
            return radialDir * strength;
        }

        private BehaviourContext BuildContext()
        {
            var pos = _organism.transform.position;
            bool hasFood = false;
            Vector3 foodPos = Vector3.zero;
            float foodDist = float.MaxValue;
            if (_cellStage != null && _cellStage.FoodManager != null)
            {
                var nearest = _cellStage.FoodManager.GetNearestFood(pos, MaxFoodSeekDistance);
                if (nearest.HasValue)
                {
                    hasFood = true;
                    foodPos = nearest.Value.Position;
                    foodDist = Vector3.Distance(pos, foodPos);
                }
            }
            if (!hasFood && NearestFood != null)
            {
                hasFood = true;
                foodPos = NearestFood.position;
                foodDist = Vector3.Distance(pos, foodPos);
            }
            if (!hasFood
                && _cellStage?.EnvironmentEvents != null
                && ShouldSeekBloomHotspots(_organism?.Genome)
                && _cellStage.EnvironmentEvents.TryGetNearestNutrientBloomHotspot(pos, out Vector3 hotspot))
            {
                float hotspotDist = Vector3.Distance(pos, hotspot);
                if (hotspotDist <= BloomHotspotSeekDistance)
                {
                    hasFood = true;
                    foodPos = hotspot;
                    foodDist = hotspotDist;
                }
            }

            Transform nearestThreat = null;
            float threatDist = float.MaxValue;
            Transform nearestPrey = null;
            float preyDist = float.MaxValue;
            ScanNearbyTargets(pos, ref nearestThreat, ref threatDist, ref nearestPrey, ref preyDist);

            var ctx = new BehaviourContext
            {
                Energy = _organism.Energy,
                MyPosition = pos,
                HasNearestFood = hasFood,
                NearestFoodPosition = foodPos,
                NearestFoodDistance = foodDist,
                HasThreat = nearestThreat != null,
                ThreatPosition = nearestThreat != null ? nearestThreat.position : Vector3.zero,
                ThreatDistance = threatDist,
                HasPrey = nearestPrey != null,
                PreyPosition = nearestPrey != null ? nearestPrey.position : Vector3.zero,
                PreyDistance = preyDist
            };
            return ctx;
        }

        private void ScanNearbyTargets(Vector3 position,
            ref Transform nearestThreat, ref float threatDist,
            ref Transform nearestPrey, ref float preyDist)
        {
            if (_organism == null) return;

            float mySize = _organism.transform.localScale.x;
            float scanRadius = Mathf.Max(MaxPreySeekDistance, MaxThreatSeekDistance);
            var hits = Physics.OverlapSphere(position, scanRadius, Physics.AllLayers, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue;
                var player = FindComponentOnCollider<PlayerCell>(hit);
                var otherOrganism = FindComponentOnCollider<Organism>(hit);
                var otherCellAi = FindComponentOnCollider<CellAI>(hit);

                var other = player != null
                    ? player.transform
                    : otherOrganism != null
                        ? otherOrganism.transform
                        : otherCellAi != null
                            ? otherCellAi.transform
                            : hit.transform;

                if (other == transform) continue;
                if (otherOrganism != null && otherOrganism.IsDead) continue;

                float dist = Vector3.Distance(position, other.position);
                if (dist >= scanRadius) continue;

                bool isPlayer = player != null;
                bool isValidTarget = isPlayer || otherOrganism != null || otherCellAi != null;
                if (!isValidTarget) continue;

                float otherSize = other.localScale.x;
                bool isThreat = IsThreatEntity(mySize, otherSize, isPlayer);
                bool isPrey = otherSize <= mySize * PreySizeRatio;
                if (!isThreat && !isPrey) continue;

                if (isThreat && dist < threatDist && dist <= MaxThreatSeekDistance)
                {
                    nearestThreat = other;
                    threatDist = dist;
                }

                if (isPrey && dist < preyDist && dist <= MaxPreySeekDistance)
                {
                    nearestPrey = other;
                    preyDist = dist;
                }
            }
        }

        private static T FindComponentOnCollider<T>(Collider other) where T : Component
        {
            if (other == null) return null;
            var direct = other.GetComponent<T>();
            if (direct != null) return direct;

            var attachedBody = other.attachedRigidbody;
            if (attachedBody != null)
            {
                var bodyComponent = attachedBody.GetComponent<T>();
                if (bodyComponent != null) return bodyComponent;
            }

            var inParent = other.GetComponentInParent<T>();
            if (inParent != null) return inParent;

            return other.GetComponentInChildren<T>();
        }

        private Vector3 GetNextWanderDirection()
        {
            float decisionStep = TickInterval > 0f ? TickInterval : Mathf.Max(0.05f, Time.deltaTime);
            _wanderNoiseTime += decisionStep * Random.Range(0.7f, 1.35f);
            float turnJitter = (Mathf.PerlinNoise(_wanderNoiseTime, _wanderHeading) - 0.5f) * 1.1f;
            _wanderHeading += turnJitter;
            _wanderDirection = new Vector3(Mathf.Cos(_wanderHeading), 0f, Mathf.Sin(_wanderHeading));
            Vector3 wander = _wanderDirection.sqrMagnitude > 0.0001f ? _wanderDirection.normalized : Vector3.forward;

            // Chemotaxis: blend a radial drift toward the genome's preferred biome zone.
            if (_cellStage != null && _organism?.Genome != null)
            {
                Vector3 drift = GetBiomeAffinityDrift(
                    _organism.transform.position,
                    _cellStage.ArenaCenter,
                    _cellStage.ArenaRadius,
                    _organism.Genome);
                if (drift.sqrMagnitude > 0.0001f)
                    wander = Vector3.Lerp(wander, drift.normalized, BiomeAffinityStrength).normalized;
            }

            // During nutrient bloom, herbivore/phototrophic organisms drift toward active bloom hotspots.
            if (_cellStage?.EnvironmentEvents != null
                && ShouldSeekBloomHotspots(_organism?.Genome)
                && _cellStage.EnvironmentEvents.TryGetNearestNutrientBloomHotspot(
                    _organism.transform.position, out Vector3 hotspot))
            {
                Vector3 bloomDrift = GetBloomHotspotDrift(
                    _organism.transform.position,
                    hotspot,
                    _cellStage.ArenaRadius);
                if (bloomDrift.sqrMagnitude > 0.0001f)
                {
                    wander = Vector3.Lerp(
                        wander,
                        bloomDrift.normalized,
                        BalanceConstants.CellNutrientBloomHerbivoreDriftStrength).normalized;
                }
            }

            return wander;
        }

        public static bool IsThreatEntity(float mySize, float otherSize, bool isPlayerEntity)
        {
            if (mySize <= 0f || otherSize <= 0f)
                return false;

            float ratio = isPlayerEntity ? PlayerThreatSizeRatio : ThreatSizeRatio;
            return otherSize > mySize * ratio;
        }

        public static Vector3 StabilizeMoveDirection(
            Vector3 previousDirection,
            Vector3 desiredDirection,
            bool allowImmediateReverse)
        {
            previousDirection.y = 0f;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 desired = desiredDirection.normalized;
            if (previousDirection.sqrMagnitude < 0.0001f || allowImmediateReverse)
                return desired;

            Vector3 previous = previousDirection.normalized;
            float dot = Mathf.Clamp(Vector3.Dot(previous, desired), -1f, 1f);
            if (dot < MinNonFleeTurnDot)
            {
                Vector3 tangent = new Vector3(-previous.z, 0f, previous.x);
                if (Vector3.Dot(tangent, desired) < 0f)
                    tangent = -tangent;

                desired = Vector3.Lerp(previous, tangent, 0.8f).normalized;
            }

            return Vector3.Slerp(previous, desired, DirectionPersistenceBlend).normalized;
        }

        private void UpdateExplorationState(BehaviourState state)
        {
            Vector3 currentPos = _organism != null ? _organism.transform.position : transform.position;
            float moved = Vector3.Distance(currentPos, _lastDecisionPosition);
            _lastDecisionPosition = currentPos;

            if (state == BehaviourState.Flee || moved >= StuckDecisionDistance)
            {
                _stuckDecisionTimer = 0f;
                return;
            }

            float decisionStep = TickInterval > 0f ? TickInterval : Mathf.Max(0.05f, Time.deltaTime);
            _stuckDecisionTimer += decisionStep;
            if (_stuckDecisionTimer < StuckDecisionDuration)
                return;

            _stuckDecisionTimer = 0f;
            _forcedExploreTimer = ForcedExploreDuration + Random.Range(0f, 0.5f);
            _forcedExploreDirection = PickForcedExploreDirection();
        }

        private Vector3 ApplyForcedExploration(Vector3 baseDirection, BehaviourState state)
        {
            if (_forcedExploreTimer <= 0f)
                return baseDirection;

            if (state == BehaviourState.Flee)
                return baseDirection;

            float decisionStep = TickInterval > 0f ? TickInterval : Mathf.Max(0.05f, Time.deltaTime);
            _forcedExploreTimer = Mathf.Max(0f, _forcedExploreTimer - decisionStep);

            if (baseDirection.sqrMagnitude < 0.0001f)
                return _forcedExploreDirection;

            return Vector3.Lerp(baseDirection.normalized, _forcedExploreDirection, ForcedExploreBlend).normalized;
        }

        private Vector3 PickForcedExploreDirection()
        {
            Vector2 random2D = Random.insideUnitCircle.normalized;
            Vector3 randomDir = new Vector3(random2D.x, 0f, random2D.y);
            if (_cellStage == null || _cellStage.ArenaRadius <= 0f)
                return randomDir.sqrMagnitude > 0.0001f ? randomDir : Vector3.forward;

            Vector3 toCenter = _cellStage.ArenaCenter - transform.position;
            toCenter.y = 0f;
            Vector3 inward = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward;
            Vector3 tangent = new Vector3(-inward.z, 0f, inward.x) * (Random.value < 0.5f ? -1f : 1f);
            Vector3 exploratory = inward * 0.55f + tangent * 0.35f + randomDir * 0.45f;
            if (exploratory.sqrMagnitude < 0.0001f)
                exploratory = inward;
            return exploratory.normalized;
        }

        private void RecordAdaptivePressure(BehaviourState state)
        {
            if (state != BehaviourState.Flee || _organism == null)
                return;

            if (Time.time < _nextThreatExperienceTime)
                return;

            _organism.RecordThreatEscapeExperience();
            _nextThreatExperienceTime = Time.time + ThreatExperienceCooldown;
        }
    }
}

