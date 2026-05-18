using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Simple AI controller for non-player cells that wander, chase smaller cells, and flee larger threats.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CellAI : MonoBehaviour, ICellStageContextAware
    {
        private enum CellBehavior { Wander, SeekFood, Chase, Flee }
        private const float MinimumAliveScale = 0.12f;
        private const float PreyConsumeSizeRatio = 0.78f;
        private const float ThreatSizeRatio = 1.26f;
        private const float PlayerThreatSizeRatio = 1.0f;
        private const float HungerScaleThreshold = 0.65f;
        private const float CriticalHungerScaleThreshold = 0.42f;
        private const float ImmediateThreatDistance = 1.8f;
        private const float ThreatRiskFarDistance = 8.5f;
        private const float ResourceOpportunityNearDistance = 0.25f;
        private const float ResourceOpportunityFarDistance = 12f;
        private const float FoodGrowthScaleFactor = 0.045f;
        private const float PredationGrowthScaleFactor = 0.18f;
        private const float MaximumNpcScale = 4.8f;
        private const float ContactBiteDamage = 0.05f;
        private const float MinimumPredationProbeRadius = 0.14f;
        private const float PredationProbeContactPadding = 0.12f;
        private const float StuckCheckInterval = 0.7f;
        private const float StuckMinTravelDistance = 0.22f;
        private const float StuckVelocityThreshold = 0.14f;
        private const float ForcedExploreDuration = 1.9f;
        private const float ForcedExploreBlend = 0.72f;
        private const float RouteMemoryCellSize = 2.2f;
        private const int RecentRouteCapacity = 22;
        private const int WanderCandidateCount = 9;
        private const float WanderProjectionDistance = 6.2f;
        private const float OrganicTurnRateDegrees = 170f;
        private const float NonFleeBacktrackDot = -0.25f;
        private const float WanderMeanderStrength = 0.18f;
        private const float GoalMeanderStrength = 0.05f;
        private const float FleeMeanderStrength = 0.025f;
        private const float LocomotionPulseMagnitude = 0.055f;
        private const float LocomotionPulseRate = 2.4f;

        [SerializeField] private float _moveSpeed = 1.55f;
        [SerializeField] private float _perceptionRadius = 7.2f;
        [SerializeField] private float _wanderChangeInterval = 3.2f;

        private Rigidbody _rb;
        private CellBehavior _currentBehavior = CellBehavior.Wander;
        private Vector3 _wanderDirection;
        private float _wanderTimer;
        private Transform _target;
        private FoodManager _foodManager;
        private CellStage _cellStage;
        private float _stuckCheckTimer;
        private Vector3 _lastStuckCheckPosition;
        private float _forcedExploreTimer;
        private Vector3 _forcedExploreDirection;
        private readonly Collider[] _predationProbeHits = new Collider[32];
        private readonly Queue<Vector2Int> _recentRouteCells = new Queue<Vector2Int>();
        private Vector3 _currentMoveDirection;
        private float _locomotionPulseOffset;

        public struct RiskTradeoffScores
        {
            public float HungerPressure;
            public float ThreatRisk;
            public float FoodOpportunity;
            public float FleeDrive;
            public float ForageDrive;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            EnsureTriggerColliders();
        }

        private void OnEnable()
        {
            EnsureTriggerColliders();
        }

        private void Start()
        {
            _foodManager ??= FindAnyObjectByType<FoodManager>();
            _cellStage ??= FindAnyObjectByType<CellStage>();
            _stuckCheckTimer = StuckCheckInterval;
            _lastStuckCheckPosition = transform.position;
            _forcedExploreDirection = transform.forward.sqrMagnitude > 0.01f ? transform.forward.normalized : Vector3.forward;
            _currentMoveDirection = _forcedExploreDirection;
            _locomotionPulseOffset = Random.Range(0f, 20f);
            PickNovelWanderDirection();
            CellSwimVisual.Ensure(gameObject, CellSwimVisual.SwimProfile.Npc);
        }

        public void BindCellStageContext(CellStage stage, FoodManager foodManager)
        {
            _cellStage = stage;
            _foodManager = foodManager;
        }

        private void FixedUpdate()
        {
            CellRouteMemory.RecordRecentLocation(
                _recentRouteCells,
                transform.position,
                RouteMemoryCellSize,
                RecentRouteCapacity);
            UpdateStuckState(Time.fixedDeltaTime);
            Perceive();
            ProbeNearbyPredationContacts();

            switch (_currentBehavior)
            {
                case CellBehavior.Wander:
                    Wander();
                    break;
                case CellBehavior.SeekFood:
                    SeekFood();
                    break;
                case CellBehavior.Chase:
                    Chase();
                    break;
                case CellBehavior.Flee:
                    Flee();
                    break;
            }
        }

        private void Perceive()
        {
            var colliders = Physics.OverlapSphere(transform.position, _perceptionRadius);
            float mySize = transform.localScale.x;
            _target = null;

            float closestThreat = float.MaxValue;
            float closestPrey = float.MaxValue;
            float closestFood = float.MaxValue;
            Transform threat = null;
            Transform prey = null;
            Transform food = null;

            foreach (var col in colliders)
            {
                if (col.transform == transform) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                var foodItem = FindComponentOnCollider<FoodItem>(col);
                if (foodItem != null && foodItem.isActiveAndEnabled)
                {
                    if (dist < closestFood)
                    {
                        closestFood = dist;
                        food = foodItem.transform;
                    }
                    continue;
                }

                float otherSize = col.transform.localScale.x;

                bool isPlayer = FindComponentOnCollider<PlayerCell>(col) != null;
                bool isCellAI = FindComponentOnCollider<CellAI>(col) != null;
                var organism = FindComponentOnCollider<Organism>(col);
                bool isOrganism = organism != null && !organism.IsDead;
                if (!(isPlayer || isCellAI || isOrganism)) continue;

                if (IsThreatEntity(mySize, otherSize, isPlayer) && dist < closestThreat)
                {
                    closestThreat = dist;
                    threat = col.transform;
                }
                else if (otherSize < mySize * PreyConsumeSizeRatio && dist < closestPrey)
                {
                    closestPrey = dist;
                    prey = col.transform;
                }
            }

            bool shouldFlee = ShouldFleeUnderPredationRisk(
                hasThreat: threat != null,
                hasFood: food != null,
                currentScale: mySize,
                threatDistance: closestThreat,
                foodDistance: closestFood);
            bool hungry = mySize <= HungerScaleThreshold;

            if (shouldFlee)
            {
                _currentBehavior = CellBehavior.Flee;
                _target = threat;
            }
            else if (food != null && (hungry || prey == null))
            {
                _currentBehavior = CellBehavior.SeekFood;
                _target = food;
            }
            else if (prey != null)
            {
                _currentBehavior = CellBehavior.Chase;
                _target = prey;
            }
            else if (food != null)
            {
                _currentBehavior = CellBehavior.SeekFood;
                _target = food;
            }
            else
            {
                _currentBehavior = CellBehavior.Wander;
            }
        }

        /// <summary>
        /// Returns true when the cell should flee under predation pressure.
        /// Critically hungry cells may continue foraging if food is safer/closer and threat is not immediate.
        /// </summary>
        public static bool ShouldFleeUnderPredationRisk(
            bool hasThreat,
            bool hasFood,
            float currentScale,
            float threatDistance,
            float foodDistance)
        {
            if (!hasThreat) return false;
            RiskTradeoffScores scores = EvaluateRiskTradeoffScores(
                hasThreat, hasFood, currentScale, threatDistance, foodDistance);
            return scores.FleeDrive >= scores.ForageDrive;
        }

        /// <summary>
        /// Evaluates threat-vs-forage drives using a state-dependent risk model.
        /// </summary>
        public static RiskTradeoffScores EvaluateRiskTradeoffScores(
            bool hasThreat,
            bool hasFood,
            float currentScale,
            float threatDistance,
            float foodDistance)
        {
            float hungerPressure = Mathf.Clamp01(
                (HungerScaleThreshold - currentScale) / Mathf.Max(0.01f, HungerScaleThreshold));
            float threatRisk = hasThreat ? EvaluateThreatRisk(threatDistance) : 0f;
            float foodOpportunity = hasFood ? EvaluateResourceOpportunity(foodDistance) : 0f;

            // Hunger reduces avoidance of threat and increases willingness to forage under risk.
            float predationPressure = threatRisk * Mathf.Lerp(1f, 0.66f, hungerPressure);
            float fleeDrive = hasThreat
                ? predationPressure * (0.84f + (1f - hungerPressure) * 0.24f) - hungerPressure * 0.30f
                : float.NegativeInfinity;
            float forageDrive = hasFood
                ? foodOpportunity * (0.34f + hungerPressure * 1.40f) - predationPressure * 0.70f
                : float.NegativeInfinity;

            if (hasThreat && threatDistance <= ImmediateThreatDistance)
            {
                fleeDrive += 0.55f;
                forageDrive -= 0.45f;
            }

            // Non-critical cells should avoid risky foraging.
            if (currentScale > CriticalHungerScaleThreshold)
                forageDrive -= predationPressure * 0.2f;

            return new RiskTradeoffScores
            {
                HungerPressure = hungerPressure,
                ThreatRisk = threatRisk,
                FoodOpportunity = foodOpportunity,
                FleeDrive = fleeDrive,
                ForageDrive = forageDrive
            };
        }

        public static float EvaluateThreatRisk(float threatDistance)
        {
            if (threatDistance <= ImmediateThreatDistance) return 1f;
            if (threatDistance >= ThreatRiskFarDistance) return 0f;

            float span = Mathf.Max(0.01f, ThreatRiskFarDistance - ImmediateThreatDistance);
            float t = (threatDistance - ImmediateThreatDistance) / span;
            return 1f - Mathf.Clamp01(t);
        }

        public static float EvaluateResourceOpportunity(float distance)
        {
            if (distance <= ResourceOpportunityNearDistance) return 1f;
            if (distance >= ResourceOpportunityFarDistance) return 0f;

            float span = Mathf.Max(0.01f, ResourceOpportunityFarDistance - ResourceOpportunityNearDistance);
            float t = (distance - ResourceOpportunityNearDistance) / span;
            return 1f - Mathf.Clamp01(t);
        }

        /// <summary>
        /// Player entities are treated as threats as soon as they are larger than this cell.
        /// Non-player threats keep a higher ratio threshold to reduce over-reactive fleeing.
        /// </summary>
        public static bool IsThreatEntity(float mySize, float otherSize, bool isPlayerEntity)
        {
            if (mySize <= 0f || otherSize <= 0f)
                return false;

            float ratio = isPlayerEntity ? PlayerThreatSizeRatio : ThreatSizeRatio;
            return otherSize > mySize * ratio;
        }

        private void Wander()
        {
            _wanderTimer -= Time.fixedDeltaTime;
            if (_wanderTimer <= 0)
                PickNovelWanderDirection();

            ApplyVelocity(
                _wanderDirection,
                _moveSpeed * CellFeelDirector.NpcCellSpeedMultiplier,
                allowImmediateReverse: false,
                meanderStrength: WanderMeanderStrength);
        }

        private void Chase()
        {
            if (_target == null) return;
            var dir = (_target.position - transform.position).normalized;
            dir.y = 0;
            ApplyVelocity(
                dir,
                _moveSpeed * 1.06f * CellFeelDirector.NpcCellSpeedMultiplier,
                allowImmediateReverse: false,
                meanderStrength: GoalMeanderStrength);
        }

        private void SeekFood()
        {
            if (_target == null)
            {
                _currentBehavior = CellBehavior.Wander;
                return;
            }

            var dir = (_target.position - transform.position).normalized;
            dir.y = 0;
            ApplyVelocity(
                dir,
                _moveSpeed * 0.98f * CellFeelDirector.NpcCellSpeedMultiplier,
                allowImmediateReverse: false,
                meanderStrength: GoalMeanderStrength);
        }

        private void Flee()
        {
            if (_target == null) return;
            var dir = (transform.position - _target.position).normalized;
            dir.y = 0;
            ApplyVelocity(
                dir,
                _moveSpeed * 1.22f * CellFeelDirector.NpcCellSpeedMultiplier,
                allowImmediateReverse: true,
                meanderStrength: FleeMeanderStrength);
        }

        private void PickNovelWanderDirection()
        {
            Vector3[] candidates = new Vector3[WanderCandidateCount];
            Vector3 basis = _wanderDirection.sqrMagnitude > 0.0001f
                ? _wanderDirection.normalized
                : (_currentMoveDirection.sqrMagnitude > 0.0001f ? _currentMoveDirection.normalized : Vector3.forward);
            float basisAngle = Mathf.Atan2(basis.z, basis.x);

            for (int i = 0; i < candidates.Length; i++)
            {
                float angle = i == 0
                    ? basisAngle + Random.Range(-0.45f, 0.45f)
                    : Random.Range(0f, Mathf.PI * 2f);
                candidates[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            _wanderDirection = SelectNovelWanderDirection(
                transform.position,
                candidates,
                _recentRouteCells,
                RouteMemoryCellSize,
                WanderProjectionDistance,
                basis);
            _wanderTimer = _wanderChangeInterval + Random.Range(-1f, 1f);
        }

        public static Vector3 SelectNovelWanderDirection(
            Vector3 currentPosition,
            IReadOnlyList<Vector3> candidateDirections,
            IEnumerable<Vector2Int> recentCells,
            float memoryCellSize,
            float projectionDistance,
            Vector3 previousDirection)
        {
            Vector3 fallback = NormalizePlanar(previousDirection, Vector3.forward);
            if (candidateDirections == null || candidateDirections.Count == 0)
                return fallback;

            float safeProjection = Mathf.Max(0.5f, projectionDistance);
            Vector3 previous = NormalizePlanar(previousDirection, Vector3.zero);
            Vector3 bestDirection = Vector3.zero;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < candidateDirections.Count; i++)
            {
                Vector3 candidate = NormalizePlanar(candidateDirections[i], Vector3.zero);
                if (candidate.sqrMagnitude < 0.0001f)
                    continue;

                Vector3 projected = currentPosition + candidate * safeProjection;
                float score = CellRouteMemory.ComputeNoveltyScore(projected, recentCells, memoryCellSize);

                if (previous.sqrMagnitude > 0.0001f)
                {
                    float dot = Vector3.Dot(previous, candidate);
                    score += Mathf.Max(0f, dot) * 0.18f;
                    if (dot < NonFleeBacktrackDot)
                        score -= (NonFleeBacktrackDot - dot) * 2.1f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = candidate;
                }
            }

            return bestDirection.sqrMagnitude > 0.0001f ? bestDirection.normalized : fallback;
        }

        private void ApplyVelocity(Vector3 direction, float speed, bool allowImmediateReverse, float meanderStrength)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                _rb.linearVelocity = Vector3.zero;
                return;
            }

            Vector3 biased = ApplyForcedExploreBias(direction.normalized);
            biased = ApplyOrganicMeander(biased, meanderStrength);
            Vector3 separation = CellSeparation.GetDirection(transform);
            biased = CellSeparation.Apply(biased, separation);
            biased = ApplyOrganicSteering(biased, allowImmediateReverse);
            float cappedSpeed = Organism.ClampSpeedToPlayer(speed, BalanceConstants.CellNpcMaxSpeedToPlayerRatio);
            float pulse = GetLocomotionPulse();
            Vector3 velocity = biased * (cappedSpeed * pulse) + separation * (cappedSpeed * CellSeparation.PushSpeedMultiplier);
            _rb.linearVelocity = Organism.ClampVelocityToPlayer(velocity, BalanceConstants.CellNpcMaxSpeedToPlayerRatio);
        }

        private Vector3 ApplyOrganicMeander(Vector3 direction, float strength)
        {
            direction.y = 0f;
            if (strength <= 0f || direction.sqrMagnitude < 0.0001f)
                return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;

            Vector3 normalized = direction.normalized;
            Vector3 tangent = new Vector3(-normalized.z, 0f, normalized.x);
            float t = Time.time + _locomotionPulseOffset;
            float wave = Mathf.Sin(t * 1.55f) + Mathf.Sin(t * 0.73f + _locomotionPulseOffset) * 0.45f;
            Vector3 meandered = normalized + tangent * (wave * strength);
            return meandered.sqrMagnitude > 0.0001f ? meandered.normalized : normalized;
        }

        private Vector3 ApplyOrganicSteering(Vector3 desiredDirection, bool allowImmediateReverse)
        {
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 desired = desiredDirection.normalized;
            if (_currentMoveDirection.sqrMagnitude < 0.0001f || allowImmediateReverse)
            {
                _currentMoveDirection = desired;
                return desired;
            }

            Vector3 previous = _currentMoveDirection.normalized;
            float dot = Mathf.Clamp(Vector3.Dot(previous, desired), -1f, 1f);
            if (dot < NonFleeBacktrackDot)
            {
                Vector3 tangent = new Vector3(-previous.z, 0f, previous.x);
                if (Vector3.Dot(tangent, desired) < 0f)
                    tangent = -tangent;

                desired = Vector3.Lerp(previous, tangent, 0.75f).normalized;
            }

            float maxRadians = OrganicTurnRateDegrees * Mathf.Deg2Rad * Mathf.Max(0.001f, Time.fixedDeltaTime);
            _currentMoveDirection = Vector3.RotateTowards(previous, desired, maxRadians, 0f);
            if (_currentMoveDirection.sqrMagnitude < 0.0001f)
                _currentMoveDirection = desired;

            return _currentMoveDirection.normalized;
        }

        private float GetLocomotionPulse()
        {
            float pulse = Mathf.Sin((Time.time + _locomotionPulseOffset) * LocomotionPulseRate);
            return Mathf.Max(0.75f, 1f + pulse * LocomotionPulseMagnitude);
        }

        private Vector3 ApplyForcedExploreBias(Vector3 baseDirection)
        {
            if (_forcedExploreTimer <= 0f)
                return baseDirection;

            _forcedExploreTimer = Mathf.Max(0f, _forcedExploreTimer - Time.fixedDeltaTime);
            if (baseDirection.sqrMagnitude < 0.0001f)
                return _forcedExploreDirection;

            return Vector3.Lerp(baseDirection, _forcedExploreDirection, ForcedExploreBlend).normalized;
        }

        private void UpdateStuckState(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _stuckCheckTimer -= deltaTime;
            if (_stuckCheckTimer > 0f)
                return;

            _stuckCheckTimer = StuckCheckInterval;
            Vector3 currentPos = transform.position;
            float moved = Vector3.Distance(currentPos, _lastStuckCheckPosition);
            float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            _lastStuckCheckPosition = currentPos;

            if (moved >= StuckMinTravelDistance || speed > StuckVelocityThreshold)
                return;

            _forcedExploreTimer = ForcedExploreDuration + Random.Range(0f, 0.5f);
            _forcedExploreDirection = PickForcedExploreDirection();
            PickNovelWanderDirection();
        }

        private Vector3 PickForcedExploreDirection()
        {
            Vector2 random2D = Random.insideUnitCircle.normalized;
            Vector3 direction = new Vector3(random2D.x, 0f, random2D.y);

            if (_cellStage == null || _cellStage.ArenaRadius <= 0f)
                return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            Vector3 toCenter = _cellStage.ArenaCenter - transform.position;
            toCenter.y = 0f;
            Vector3 inward = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward;
            Vector3 tangent = new Vector3(-inward.z, 0f, inward.x);
            float tangentSign = Random.value < 0.5f ? -1f : 1f;
            Vector3 exploratory = (inward * 0.65f) + (tangent * tangentSign * 0.35f) + (direction * 0.4f);
            if (exploratory.sqrMagnitude < 0.0001f)
                exploratory = inward;
            Vector3[] candidates =
            {
                exploratory,
                inward * 0.65f + tangent * tangentSign * 0.55f,
                inward * 0.65f - tangent * tangentSign * 0.55f,
                direction
            };
            return SelectNovelWanderDirection(
                transform.position,
                candidates,
                _recentRouteCells,
                RouteMemoryCellSize,
                WanderProjectionDistance,
                exploratory);
        }

        /// <summary>
        /// Ensures NPC colliders are trigger-enabled so player overlap detection remains reliable
        /// even when visuals/prefabs are swapped at runtime.
        /// </summary>
        private void EnsureTriggerColliders()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            bool hasSupportedTrigger = false;

            for (int i = 0; colliders != null && i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;

                if (col is MeshCollider)
                {
                    // Mesh hull generation can fail for tiny visual meshes; NPC overlap uses root sphere triggers.
                    UnityObjectUtil.Destroy(col);
                    continue;
                }

                col.enabled = true;
                col.isTrigger = true;
                hasSupportedTrigger = true;
            }

            if (!hasSupportedTrigger)
            {
                var fallback = GetComponent<SphereCollider>();
                if (fallback == null)
                    fallback = gameObject.AddComponent<SphereCollider>();

                fallback.enabled = true;
                fallback.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleTriggerContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryHandleTriggerContact(other);
        }

        private bool TryHandleTriggerContact(Collider other)
        {
            if (TryConsumeFood(other))
                return true;

            var otherCell = FindComponentOnCollider<CellAI>(other);
            if (otherCell != null && otherCell != this && otherCell.transform.localScale.x < transform.localScale.x * PreyConsumeSizeRatio)
            {
                GrowByScale(otherCell.transform.localScale.x * PredationGrowthScaleFactor);
                UnityObjectUtil.Destroy(otherCell.gameObject);
                return true;
            }

            var otherOrganism = FindComponentOnCollider<Organism>(other);
            if (otherOrganism != null && !otherOrganism.IsDead && otherOrganism.transform.localScale.x < transform.localScale.x * PreyConsumeSizeRatio)
            {
                GrowByScale(otherOrganism.transform.localScale.x * PredationGrowthScaleFactor);
                otherOrganism.Die();
                return true;
            }

            var player = FindComponentOnCollider<PlayerCell>(other);
            if (player != null && !player.IsDead && player.transform.localScale.x < transform.localScale.x * PreyConsumeSizeRatio)
            {
                player.TakeDamage(ContactBiteDamage);
                GrowByScale(ContactBiteDamage * 0.6f);
                return true;
            }

            return false;
        }

        private void ProbeNearbyPredationContacts()
        {
            float probeRadius = ComputePredationProbeRadius(transform.lossyScale, GetComponent<SphereCollider>());
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                probeRadius,
                _predationProbeHits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _predationProbeHits[i];
                _predationProbeHits[i] = null;
                if (hit == null || !hit.enabled)
                    continue;

                if (TryHandleTriggerContact(hit))
                    break;
            }
        }

        public static float ComputePredationProbeRadius(Vector3 worldScale, SphereCollider collider)
        {
            float scaleRadius = Mathf.Max(Mathf.Abs(worldScale.x), Mathf.Abs(worldScale.z)) * 0.5f;
            float colliderRadius = 0f;

            if (collider != null)
            {
                float colliderScale = Mathf.Max(
                    Mathf.Abs(collider.transform.lossyScale.x),
                    Mathf.Abs(collider.transform.lossyScale.z));
                colliderRadius = Mathf.Max(0f, collider.radius) * colliderScale;
            }

            return Mathf.Max(MinimumPredationProbeRadius, Mathf.Max(scaleRadius, colliderRadius))
                   + PredationProbeContactPadding;
        }

        public void ApplyDamage(float scaleDamage)
        {
            if (scaleDamage <= 0f) return;

            float newScale = transform.localScale.x - scaleDamage;
            if (newScale <= MinimumAliveScale)
            {
                UnityObjectUtil.Destroy(gameObject);
                return;
            }

            transform.localScale = Vector3.one * newScale;
        }

        private bool TryConsumeFood(Collider other)
        {
            var food = FindComponentOnCollider<FoodItem>(other);
            if (food == null || !food.isActiveAndEnabled)
                return false;

            float nutrition = food.NutritionValue;
            int id = food.FoodManagerId;
            if (id != 0)
            {
                _foodManager ??= FindAnyObjectByType<FoodManager>();
                if (_foodManager != null && _foodManager.ConsumeFood(id))
                    nutrition = food.NutritionValue;
                else
                    return false;
            }
            else
            {
                UnityObjectUtil.Destroy(food.gameObject);
            }

            GrowByScale(Mathf.Max(0.01f, nutrition * FoodGrowthScaleFactor));
            return true;
        }

        private void GrowByScale(float scaleGain)
        {
            if (scaleGain <= 0f)
                return;

            float current = transform.localScale.x;
            float next = Mathf.Min(MaximumNpcScale, current + scaleGain);
            if (next <= current)
                return;

            transform.localScale = Vector3.one * next;
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

        private static Vector3 NormalizePlanar(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.zero;
        }
    }

    /// <summary>
    /// Quantized short-term route memory for cell-stage AI. It keeps wander targets novel enough
    /// that NPC cells do not pace across the same strip of water every retarget.
    /// </summary>
    public static class CellRouteMemory
    {
        public static Vector2Int QuantizePlanar(Vector3 position, float cellSize)
        {
            float safeCellSize = Mathf.Max(0.1f, cellSize);
            return new Vector2Int(
                Mathf.RoundToInt(position.x / safeCellSize),
                Mathf.RoundToInt(position.z / safeCellSize));
        }

        public static void RecordRecentLocation(
            Queue<Vector2Int> recentCells,
            Vector3 position,
            float cellSize,
            int capacity)
        {
            if (recentCells == null)
                return;

            int safeCapacity = Mathf.Max(1, capacity);
            Vector2Int cell = QuantizePlanar(position, cellSize);
            if (recentCells.Count == 0 || GetLastCell(recentCells) != cell)
                recentCells.Enqueue(cell);

            while (recentCells.Count > safeCapacity)
                recentCells.Dequeue();
        }

        public static float ComputeNoveltyScore(
            Vector3 candidate,
            IEnumerable<Vector2Int> recentCells,
            float cellSize)
        {
            if (recentCells == null)
                return 100f;

            int recentCount = 0;
            if (recentCells is ICollection<Vector2Int> collection)
            {
                recentCount = collection.Count;
            }
            else
            {
                foreach (Vector2Int _ in recentCells)
                    recentCount++;
            }

            if (recentCount == 0)
                return 100f;

            Vector2Int candidateCell = QuantizePlanar(candidate, cellSize);
            float nearestCellDistance = float.MaxValue;
            int index = 0;
            foreach (Vector2Int recentCell in recentCells)
            {
                int age = recentCount - 1 - index;
                float cellDistance = Vector2Int.Distance(candidateCell, recentCell);
                float recencyPenalty = Mathf.Max(0f, 8f - age) * 0.25f;
                nearestCellDistance = Mathf.Min(nearestCellDistance, cellDistance - recencyPenalty);
                index++;
            }

            return nearestCellDistance;
        }

        private static Vector2Int GetLastCell(Queue<Vector2Int> cells)
        {
            Vector2Int last = default;
            foreach (Vector2Int cell in cells)
                last = cell;
            return last;
        }
    }

    /// <summary>
    /// Shared cell-stage spacing helper so AI entities avoid stacking on top of each other.
    /// Kept in this compiled file because Unity regenerates the project file outside the editor.
    /// </summary>
    internal static class CellSeparation
    {
        private const float MinimumSeparationRadius = 1.35f;
        private const float QueryPadding = 1.1f;
        private const float ConsumablePreySizeRatio = 0.82f;
        public const float PushSpeedMultiplier = 1.45f;
        private const float SeparationWeight = 3.0f;
        private const float MinimumExactOverlapPush = 0.35f;

        public static Vector3 Apply(Vector3 desiredDirection, Transform self)
        {
            return Apply(desiredDirection, GetDirection(self));
        }

        public static Vector3 Apply(Vector3 desiredDirection, Vector3 separation)
        {
            desiredDirection.y = 0f;
            if (separation.sqrMagnitude < 0.0001f)
                return desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : Vector3.zero;

            Vector3 baseDirection = desiredDirection.sqrMagnitude > 0.0001f
                ? desiredDirection.normalized
                : separation.normalized;
            return (baseDirection + separation * SeparationWeight).normalized;
        }

        public static Vector3 GetDirection(Transform self)
        {
            if (self == null)
                return Vector3.zero;

            float selfRadius = EstimatePlanarRadius(self);
            float selfScale = EstimatePredationScale(self);
            float queryRadius = Mathf.Max(MinimumSeparationRadius, selfRadius + QueryPadding);
            var hits = Physics.OverlapSphere(
                self.position,
                queryRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            Vector3 separation = Vector3.zero;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (FindComponentOnCollider<PlayerCell>(hit) != null)
                    continue;

                Transform other = ResolveEntityTransform(hit);
                if (other == null || other == self)
                    continue;
                if (IsConsumablePrey(selfScale, other))
                    continue;

                Vector3 away = self.position - other.position;
                away.y = 0f;
                float distance = away.magnitude;
                float desiredDistance = Mathf.Max(MinimumSeparationRadius, selfRadius + EstimatePlanarRadius(other));

                if (distance >= desiredDistance)
                    continue;

                if (distance < 0.001f)
                {
                    away = PickDeterministicPush(self, other);
                    distance = MinimumExactOverlapPush;
                }

                float strength = Mathf.Clamp01((desiredDistance - distance) / desiredDistance);
                separation += away.normalized * strength;
            }

            return separation.sqrMagnitude > 0.0001f ? Vector3.ClampMagnitude(separation, 1f) : Vector3.zero;
        }

        private static bool IsConsumablePrey(float selfScale, Transform other)
        {
            if (selfScale <= 0f || other == null)
                return false;

            float otherScale = EstimatePredationScale(other);
            return otherScale > 0f && otherScale <= selfScale * ConsumablePreySizeRatio;
        }

        private static Transform ResolveEntityTransform(Collider hit)
        {
            if (hit == null)
                return null;

            var predator = FindComponentOnCollider<PredatorCell>(hit);
            if (predator != null)
                return predator.transform;

            var organism = FindComponentOnCollider<Organism>(hit);
            if (organism != null && !organism.IsDead)
                return organism.transform;

            var cellAi = FindComponentOnCollider<CellAI>(hit);
            if (cellAi != null)
                return cellAi.transform;

            return null;
        }

        private static float EstimatePlanarRadius(Transform target)
        {
            Vector3 scale = target.localScale;
            return Mathf.Max(0.45f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.55f);
        }

        private static float EstimatePredationScale(Transform target)
        {
            if (target == null)
                return 0f;

            Vector3 scale = target.localScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        private static Vector3 PickDeterministicPush(Transform self, Transform other)
        {
            int hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(self) * 397
                ^ System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(other);
            float angle = Mathf.Abs(hash % 6283) * 0.001f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
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
    }
}
