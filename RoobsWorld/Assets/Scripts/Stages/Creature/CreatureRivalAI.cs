using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Creature.AI;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureRivalArchetype
    {
        Charger,
        Stalker,
        Tank,
        Skirmisher
    }

    public enum CreatureRivalAmbientActivity
    {
        Roam,
        Loiter,
        Forage,
        Investigate,
        Alert
    }

    /// <summary>
    /// Rival creature AI that blends animal-like ambient movement with threat/prey decisions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CreatureInstance))]
    public class CreatureRivalAI : MonoBehaviour
    {
        private const float MovementMemoryCellSize = 2.5f;
        private const int RecentLocationCapacity = 18;
        private const int WanderCandidateCount = 9;
        private const int NearbyCreatureBufferSize = 16;

        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _aggroRange = 14f;
        [SerializeField] private float _attackRange = 1.9f;
        [SerializeField] private float _attackCooldown = 0.9f;
        [SerializeField] private float _turnSpeedDegrees = 360f;
        [SerializeField] private float _speedMultiplier = 0.9f;
        [SerializeField] private float _attackMultiplier = 0.8f;
        [SerializeField] private float _attackTelegraphDuration = 0.34f;
        [SerializeField] private float _attackRecoveryDuration = 0.42f;
        [SerializeField] private float _movementAcceleration = 10f;
        [SerializeField] private float _movementDeceleration = 14f;
        [SerializeField] private float _personalSpaceRadius = 3.2f;
        [SerializeField] private float _herdAwarenessRadius = 9f;
        [SerializeField] private float _socialSpacingWeight = 0.55f;
        [SerializeField] private float _herdCohesionWeight = 0.24f;
        [SerializeField] private float _ambientActivityMinSeconds = 1.4f;
        [SerializeField] private float _ambientActivityMaxSeconds = 4.6f;
        [SerializeField] private float _meanderStrength = 0.2f;
        [SerializeField] private float _alertDurationSeconds = 2.2f;
        [SerializeField] private float _alertReactionRadius = 11f;

        private Rigidbody _rb;
        private CreatureInstance _self;
        private CreatureInstance _playerCreature;
        private Transform _player;
        private Vector3 _anchorPosition;
        private Vector3 _wanderTarget;
        private readonly Queue<Vector2Int> _recentLocations = new Queue<Vector2Int>();
        private readonly Collider[] _nearbyCreatureBuffer = new Collider[NearbyCreatureBufferSize];
        private CreatureRivalArchetype _archetype = CreatureRivalArchetype.Charger;
        private CreatureRivalAmbientActivity _ambientActivity = CreatureRivalAmbientActivity.Roam;
        private bool _isAlpha;
        private CreatureStage _stage;
        private float _wanderRetargetTimer;
        private float _ambientActivityTimer;
        private float _attackCooldownTimer;
        private float _attackTelegraphTimer;
        private float _attackRecoveryTimer;
        private float _alertTimer;
        private float _meanderPhase;
        private Vector3 _planarVelocity;
        private Vector3 _ambientInterestPoint;
        private Vector3 _lastAlertPosition;

        public CreatureRivalArchetype Archetype => _archetype;
        public bool IsAlpha => _isAlpha;
        public CreatureRivalAmbientActivity AmbientActivity => _ambientActivity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _self = GetComponent<CreatureInstance>();
            ConfigureLocomotionBody(_rb);
            _anchorPosition = transform.position;
            _wanderTarget = transform.position;
            PickWanderTarget();
            _stage = FindAnyObjectByType<CreatureStage>();
            _meanderPhase = Random.Range(0f, Mathf.PI * 2f);
            _ambientInterestPoint = _wanderTarget;
            _lastAlertPosition = transform.position;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
        }

        public void SetPlayerTarget(Transform playerTarget)
        {
            _player = playerTarget;
            _playerCreature = _player != null ? _player.GetComponent<CreatureInstance>() : null;
        }

        public void ConfigureArchetype(CreatureRivalArchetype archetype)
        {
            _archetype = archetype;

            switch (archetype)
            {
                case CreatureRivalArchetype.Stalker:
                    _aggroRange = 18f;
                    _speedMultiplier = 0.78f;
                    _attackMultiplier = 0.95f;
                    _wanderRadius = 14f;
                    break;
                case CreatureRivalArchetype.Tank:
                    _aggroRange = 12f;
                    _speedMultiplier = 0.68f;
                    _attackMultiplier = 1.25f;
                    _attackCooldown = 1.15f;
                    _attackRange = 2.15f;
                    break;
                case CreatureRivalArchetype.Skirmisher:
                    _aggroRange = 15f;
                    _speedMultiplier = 1.12f;
                    _attackMultiplier = 0.65f;
                    _attackCooldown = 0.62f;
                    _wanderRadius = 16f;
                    break;
                default:
                    _aggroRange = 14f;
                    _speedMultiplier = 1.02f;
                    _attackMultiplier = 1f;
                    _attackCooldown = 0.82f;
                    break;
            }
        }

        public void ConfigureAlphaRival()
        {
            _isAlpha = true;
            _aggroRange *= 1.18f;
            _attackRange *= 1.08f;
            _attackMultiplier *= 1.22f;
            _attackCooldown *= 0.92f;
            _wanderRadius *= 1.12f;
            if (_self != null)
                _self.GrantInvulnerability(0.2f);
        }

        private void Update()
        {
            if (_self == null || !_self.IsAlive || _rb == null)
                return;

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            _attackRecoveryTimer = Mathf.Max(0f, _attackRecoveryTimer - Time.deltaTime);
            _alertTimer = Mathf.Max(0f, _alertTimer - Time.deltaTime);
            _wanderRetargetTimer -= Time.deltaTime;

            if (_attackTelegraphTimer > 0f)
            {
                _attackTelegraphTimer = Mathf.Max(0f, _attackTelegraphTimer - Time.deltaTime);
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, Mathf.Max(1f, _movementDeceleration) * Time.deltaTime);
                _rb.linearVelocity = new Vector3(_planarVelocity.x, _rb.linearVelocity.y, _planarVelocity.z);
                if (_attackTelegraphTimer <= 0f)
                    ResolveTelegraphedAttack();
                return;
            }

            CreatureMovementMemory.RecordRecentLocation(
                _recentLocations,
                transform.position,
                MovementMemoryCellSize,
                RecentLocationCapacity);

            bool hasPlayer = _player != null && _playerCreature != null && _playerCreature.IsAlive;
            float playerDistance = hasPlayer ? Vector3.Distance(transform.position, _player.position) : float.MaxValue;
            CreatureRivalIntent intent = hasPlayer
                ? ChooseIntent(playerDistance)
                : CreatureRivalIntent.Wander;
            Vector3 desired = ResolveDesiredDirection(intent);
            bool ambientIntent = IsAmbientIntent(intent);
            if (ambientIntent)
                desired = ResolveAmbientDirection(desired);
            else
                ResetAmbientActivity();

            bool stationaryAmbient = ambientIntent && IsStationaryAmbientActivity(_ambientActivity);
            if ((intent == CreatureRivalIntent.ReturnToTerritory || ambientIntent) &&
                !stationaryAmbient &&
                (_wanderRetargetTimer <= 0f || Vector3.Distance(transform.position, _wanderTarget) < 1.5f))
            {
                PickWanderTarget();
            }
            if (desired.sqrMagnitude < 0.0001f && !stationaryAmbient)
            {
                PickWanderTarget();
                desired = SafeNormalize(_wanderTarget - transform.position);
            }

            float recoveryMultiplier = _attackRecoveryTimer > 0f ? 0.35f : 1f;
            float speed = Mathf.Max(1f, _self.Stats.Speed)
                * Mathf.Max(0.1f, _speedMultiplier)
                * GetIntentSpeedMultiplier(intent)
                * GetAmbientSpeedMultiplier(ambientIntent ? _ambientActivity : CreatureRivalAmbientActivity.Roam)
                * recoveryMultiplier;
            Vector3 targetPlanarVelocity = desired * speed;
            float accel = targetPlanarVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude
                ? Mathf.Max(1f, _movementAcceleration)
                : Mathf.Max(1f, _movementDeceleration);
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetPlanarVelocity, accel * Time.deltaTime);
            _rb.linearVelocity = new Vector3(_planarVelocity.x, _rb.linearVelocity.y, _planarVelocity.z);

            if (desired.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _turnSpeedDegrees * Time.deltaTime);
            }

            TryAttackPlayer(intent);
        }

        private CreatureRivalIntent ChooseIntent(float playerDistance)
        {
            if (_playerCreature == null)
                return CreatureRivalIntent.Wander;

            if (_stage != null &&
                _stage.TryGetSpeciesDispositionForRival(_self?.DNA?.CreatureId, out var disposition) &&
                TryResolveDispositionIntentOverride(disposition, playerDistance, _aggroRange, _attackRange, _self.WasDamagedWithin(2.5f), out var overrideIntent))
            {
                return overrideIntent;
            }

            bool sizeThreat = CreatureSizeRules.ShouldFleeFromPlayer(_self, _playerCreature)
                || CreatureSizeRules.IsMeaningfullySmaller(_self, _playerCreature, 0.12f);

            return CreatureAIJudgement.ChooseRivalIntent(
                _self.Stats,
                CreatureAIJudgement.HealthRatio(_self),
                _playerCreature.Stats,
                CreatureAIJudgement.HealthRatio(_playerCreature),
                playerDistance,
                sizeThreat,
                transform.position,
                _anchorPosition,
                _player.position,
                _wanderRadius,
                _aggroRange,
                recentlyDamaged: _self.WasDamagedWithin(2.5f));
        }

        public static bool TryResolveDispositionIntentOverride(
            CreatureSpeciesDisposition disposition,
            float playerDistance,
            float aggroRange,
            float attackRange,
            bool recentlyDamaged,
            out CreatureRivalIntent intent)
        {
            intent = CreatureRivalIntent.Wander;
            if (recentlyDamaged)
                return false;

            float safeAggro = Mathf.Max(0.1f, aggroRange);
            float safeAttack = Mathf.Max(0.1f, attackRange);
            if (disposition == CreatureSpeciesDisposition.Friendly || disposition == CreatureSpeciesDisposition.Allied)
            {
                if (playerDistance <= safeAttack * 1.2f)
                {
                    intent = CreatureRivalIntent.KeepDistance;
                    return true;
                }

                if (playerDistance <= safeAggro)
                {
                    intent = CreatureRivalIntent.Wander;
                    return true;
                }
            }

            if (disposition == CreatureSpeciesDisposition.Hostile && playerDistance <= safeAggro * 1.25f)
            {
                intent = CreatureRivalIntent.Chase;
                return true;
            }

            return false;
        }

        private Vector3 ResolveDesiredDirection(CreatureRivalIntent intent)
        {
            switch (intent)
            {
                case CreatureRivalIntent.ReturnToTerritory:
                    return ComputeDesiredDirection(transform.position, transform.position, _anchorPosition, -1f);
                case CreatureRivalIntent.Flee:
                    return ComputeDesiredDirection(transform.position, _player.position, _wanderTarget, _aggroRange, fleeFromPlayer: true);
                case CreatureRivalIntent.KeepDistance:
                    return CreatureAIJudgement.ComputeCautiousDirection(transform.position, _player.position, _anchorPosition);
                case CreatureRivalIntent.Stalk:
                    return ComputeStalkDirection(transform.position, _player.position, _wanderTarget);
                case CreatureRivalIntent.Chase:
                    return ComputeDesiredDirection(transform.position, _player.position, _wanderTarget, _aggroRange);
                default:
                    return ComputeDesiredDirection(transform.position, transform.position + Vector3.forward, _wanderTarget, -1f);
            }
        }

        private Vector3 ResolveAmbientDirection(Vector3 desired)
        {
            UpdateAmbientActivity();

            Vector3 socialSpacing = ComputeRuntimeSocialSpacingDirection();
            Vector3 herdCohesion = ComputeRuntimeHerdCohesionDirection();
            if (_alertTimer > 0f)
            {
                _ambientActivity = CreatureRivalAmbientActivity.Alert;
                Vector3 awayFromAlert = transform.position - _lastAlertPosition;
                awayFromAlert.y = 0f;
                Vector3 toAnchor = _anchorPosition - transform.position;
                toAnchor.y = 0f;
                Vector3 alertDirection = SafeNormalize(
                    awayFromAlert.normalized * 0.72f
                    + toAnchor.normalized * 0.28f);
                return BlendAmbientSteering(
                    alertDirection,
                    socialSpacing,
                    herdCohesion,
                    socialWeight: Mathf.Max(_socialSpacingWeight, 0.72f),
                    cohesionWeight: 0f);
            }

            if (IsStationaryAmbientActivity(_ambientActivity))
                return BlendAmbientSteering(
                    Vector3.zero,
                    socialSpacing,
                    herdCohesion,
                    _socialSpacingWeight,
                    _herdCohesionWeight);

            if (_ambientActivity == CreatureRivalAmbientActivity.Investigate)
            {
                Vector3 interestDelta = _ambientInterestPoint - transform.position;
                interestDelta.y = 0f;
                if (interestDelta.sqrMagnitude > 0.25f)
                    desired = interestDelta.normalized;
            }

            desired = ApplyPlanarMeander(
                desired,
                Time.time * 0.85f + _meanderPhase,
                _meanderStrength);
            return BlendAmbientSteering(
                desired,
                socialSpacing,
                herdCohesion,
                _socialSpacingWeight,
                _herdCohesionWeight);
        }

        private void UpdateAmbientActivity()
        {
            _ambientActivityTimer -= Time.deltaTime;
            if (_ambientActivityTimer > 0f)
                return;

            _ambientActivity = ChooseAmbientActivity(
                Random.value,
                CreatureAIJudgement.HealthRatio(_self),
                _self != null && _self.WasDamagedWithin(2.5f),
                _archetype);
            _ambientActivityTimer = Random.Range(
                Mathf.Max(0.2f, _ambientActivityMinSeconds),
                Mathf.Max(_ambientActivityMinSeconds + 0.1f, _ambientActivityMaxSeconds));
            _ambientInterestPoint = _anchorPosition + CreateRandomPlanarOffset(Mathf.Max(2f, _wanderRadius * 0.8f));

            if (!IsStationaryAmbientActivity(_ambientActivity))
                PickWanderTarget();
        }

        private void ResetAmbientActivity()
        {
            _ambientActivity = CreatureRivalAmbientActivity.Roam;
            _ambientActivityTimer = 0f;
        }

        private static float GetIntentSpeedMultiplier(CreatureRivalIntent intent)
        {
            switch (intent)
            {
                case CreatureRivalIntent.Flee: return 1.18f;
                case CreatureRivalIntent.KeepDistance: return 0.78f;
                case CreatureRivalIntent.Stalk: return 0.56f;
                case CreatureRivalIntent.ReturnToTerritory: return 0.82f;
                case CreatureRivalIntent.Chase: return 1.04f;
                default: return 0.72f;
            }
        }

        private void TryAttackPlayer(CreatureRivalIntent intent)
        {
            if (intent != CreatureRivalIntent.Chase)
                return;
            if (_playerCreature == null || !_playerCreature.IsAlive)
                return;
            if (!CreatureAIJudgement.ShouldCommitToAttack(
                _self,
                _playerCreature,
                HorizontalDistance(transform.position, _player.position),
                recentlyDamaged: _self.WasDamagedWithin(2.5f)))
                return;
            if (_playerCreature.IsInvulnerable)
                return;
            if (_attackCooldownTimer > 0f || _attackRecoveryTimer > 0f)
                return;

            float distance = HorizontalDistance(transform.position, _player.position);
            float selfRadius = CreatureCombatGeometry.EstimatePlanarRadius(transform);
            float playerRadius = CreatureCombatGeometry.EstimatePlanarRadius(_player);
            float effectiveReach = CreatureCombatGeometry.ComputeContactReach(_attackRange, selfRadius, playerRadius);
            if (distance > effectiveReach)
                return;

            StartAttackTelegraph();
        }

        private void StartAttackTelegraph()
        {
            float difficultyTelegraphMultiplier = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.EnemyTelegraphMultiplier
                : 1f;
            _attackTelegraphTimer = Mathf.Max(0.05f, _attackTelegraphDuration * Mathf.Max(0.1f, difficultyTelegraphMultiplier));
            EventBus.Publish(new CreatureCombatFeedbackEvent
            {
                Type = CreatureCombatFeedbackType.Telegraph,
                SourceCreatureId = _self?.DNA?.CreatureId,
                TargetCreatureId = _playerCreature?.DNA?.CreatureId,
                Position = transform.position,
                IsPlayerTarget = true
            });
        }

        private void ResolveTelegraphedAttack()
        {
            if (_playerCreature == null || !_playerCreature.IsAlive)
                return;
            if (_playerCreature.IsInvulnerable)
            {
                BeginRecovery();
                return;
            }

            float distance = HorizontalDistance(transform.position, _player.position);
            float selfRadius = CreatureCombatGeometry.EstimatePlanarRadius(transform);
            float playerRadius = CreatureCombatGeometry.EstimatePlanarRadius(_player);
            float effectiveReach = CreatureCombatGeometry.ComputeContactReach(_attackRange, selfRadius, playerRadius);
            if (!CanResolveTelegraphedAttack(distance, effectiveReach))
            {
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Miss,
                    SourceCreatureId = _self?.DNA?.CreatureId,
                    TargetCreatureId = _playerCreature?.DNA?.CreatureId,
                    Position = transform.position,
                    IsPlayerTarget = true
                });
                BeginRecovery();
                return;
            }

            float damage = Mathf.Max(1f, _self.Stats.Attack * Mathf.Max(0.1f, _attackMultiplier));
            _playerCreature.TakeDamage(damage);
            _attackCooldownTimer = _attackCooldown;
            BeginRecovery();
        }

        private void BeginRecovery()
        {
            float difficultyRecoveryMultiplier = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.RecoveryWindowMultiplier
                : 1f;
            _attackRecoveryTimer = Mathf.Max(0f, _attackRecoveryDuration * Mathf.Max(0.1f, difficultyRecoveryMultiplier));
            _attackCooldownTimer = Mathf.Max(_attackCooldownTimer, _attackCooldown);
        }

        private void PickWanderTarget()
        {
            Vector3[] candidates = new Vector3[WanderCandidateCount];
            for (int i = 0; i < candidates.Length; i++)
                candidates[i] = _anchorPosition + CreateRandomPlanarOffset(_wanderRadius);

            _wanderTarget = CreatureMovementMemory.SelectLeastVisitedCandidate(
                transform.position,
                candidates,
                _recentLocations,
                MovementMemoryCellSize,
                Mathf.Min(_wanderRadius * 0.5f, 5f));
            _wanderRetargetTimer = Random.Range(1.8f, 4.2f);
        }

        public static CreatureRivalAmbientActivity ChooseAmbientActivity(
            float random01,
            float healthRatio,
            bool recentlyDamaged,
            CreatureRivalArchetype archetype)
        {
            if (recentlyDamaged || healthRatio < 0.35f)
                return CreatureRivalAmbientActivity.Roam;

            float forageChance = archetype == CreatureRivalArchetype.Tank ? 0.34f :
                archetype == CreatureRivalArchetype.Stalker ? 0.12f : 0.24f;
            float loiterChance = archetype == CreatureRivalArchetype.Charger ? 0.14f : 0.2f;
            float investigateChance = archetype == CreatureRivalArchetype.Stalker ? 0.38f :
                archetype == CreatureRivalArchetype.Skirmisher ? 0.28f : 0.18f;

            float roll = Mathf.Clamp01(random01);
            if (roll < forageChance)
                return CreatureRivalAmbientActivity.Forage;
            if (roll < forageChance + loiterChance)
                return CreatureRivalAmbientActivity.Loiter;
            if (roll < forageChance + loiterChance + investigateChance)
                return CreatureRivalAmbientActivity.Investigate;
            return CreatureRivalAmbientActivity.Roam;
        }

        public static float GetAmbientSpeedMultiplier(CreatureRivalAmbientActivity activity)
        {
            switch (activity)
            {
                case CreatureRivalAmbientActivity.Forage: return 0.18f;
                case CreatureRivalAmbientActivity.Loiter: return 0.12f;
                case CreatureRivalAmbientActivity.Investigate: return 0.72f;
                case CreatureRivalAmbientActivity.Alert: return 1.08f;
                default: return 1f;
            }
        }

        public static bool IsStationaryAmbientActivity(CreatureRivalAmbientActivity activity)
        {
            return activity == CreatureRivalAmbientActivity.Forage
                || activity == CreatureRivalAmbientActivity.Loiter;
        }

        public static Vector3 ComputeSocialSpacingDirection(
            Vector3 selfPosition,
            IReadOnlyList<Vector3> neighborPositions,
            float personalSpaceRadius)
        {
            if (neighborPositions == null || neighborPositions.Count == 0)
                return Vector3.zero;

            float safeRadius = Mathf.Max(0.1f, personalSpaceRadius);
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < neighborPositions.Count; i++)
                sum += ComputeSocialSpacingContribution(selfPosition, neighborPositions[i], safeRadius);

            return SafeNormalize(sum);
        }

        public static Vector3 BlendSocialSteering(
            Vector3 desiredDirection,
            Vector3 socialSpacingDirection,
            float socialSpacingWeight)
        {
            desiredDirection.y = 0f;
            socialSpacingDirection.y = 0f;

            Vector3 desired = SafeNormalize(desiredDirection);
            Vector3 spacing = SafeNormalize(socialSpacingDirection);
            if (desired.sqrMagnitude < 0.0001f)
                return spacing;
            if (spacing.sqrMagnitude < 0.0001f)
                return desired;

            float weight = Mathf.Clamp01(socialSpacingWeight);
            return SafeNormalize(desired * (1f - weight) + spacing * weight);
        }

        public static Vector3 ComputeHerdCohesionDirection(
            Vector3 selfPosition,
            IReadOnlyList<Vector3> herdPositions,
            float minComfortDistance,
            float herdAwarenessRadius)
        {
            if (herdPositions == null || herdPositions.Count == 0)
                return Vector3.zero;

            float safeMin = Mathf.Max(0f, minComfortDistance);
            float safeMax = Mathf.Max(safeMin + 0.1f, herdAwarenessRadius);
            Vector3 center = Vector3.zero;
            int count = 0;

            for (int i = 0; i < herdPositions.Count; i++)
            {
                Vector3 delta = herdPositions[i] - selfPosition;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= safeMin || distance > safeMax)
                    continue;

                center += herdPositions[i];
                count++;
            }

            if (count == 0)
                return Vector3.zero;

            center /= count;
            center.y = selfPosition.y;
            return SafeNormalize(center - selfPosition);
        }

        public static Vector3 BlendAmbientSteering(
            Vector3 desiredDirection,
            Vector3 socialSpacingDirection,
            Vector3 herdCohesionDirection,
            float socialWeight,
            float cohesionWeight)
        {
            Vector3 blended = BlendSocialSteering(desiredDirection, socialSpacingDirection, socialWeight);
            Vector3 cohesion = SafeNormalize(herdCohesionDirection);
            if (cohesion.sqrMagnitude < 0.0001f)
                return blended;
            if (blended.sqrMagnitude < 0.0001f)
                return cohesion;

            float weight = Mathf.Clamp01(cohesionWeight);
            return SafeNormalize(blended * (1f - weight) + cohesion * weight);
        }

        public static Vector3 ApplyPlanarMeander(Vector3 desiredDirection, float phase, float strength)
        {
            Vector3 desired = SafeNormalize(new Vector3(desiredDirection.x, 0f, desiredDirection.z));
            if (desired.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 tangent = new Vector3(-desired.z, 0f, desired.x);
            return SafeNormalize(desired + tangent * Mathf.Sin(phase) * Mathf.Max(0f, strength));
        }

        /// <summary>
        /// Chooses chase direction when target is within aggro range; otherwise heads to wander target.
        /// </summary>
        public static Vector3 ComputeDesiredDirection(
            Vector3 selfPosition,
            Vector3 playerPosition,
            Vector3 wanderTarget,
            float aggroRange,
            bool fleeFromPlayer = false)
        {
            Vector3 playerDelta = playerPosition - selfPosition;
            playerDelta.y = 0f;
            if (aggroRange >= 0f && playerDelta.magnitude <= aggroRange)
                return SafeNormalize(fleeFromPlayer ? -playerDelta : playerDelta);

            Vector3 wanderDelta = wanderTarget - selfPosition;
            wanderDelta.y = 0f;
            return SafeNormalize(wanderDelta);
        }

        public static Vector3 ComputeStalkDirection(Vector3 selfPosition, Vector3 playerPosition, Vector3 wanderTarget)
        {
            Vector3 toPlayer = playerPosition - selfPosition;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
                return ComputeDesiredDirection(selfPosition, playerPosition, wanderTarget, -1f);

            Vector3 tangent = new Vector3(-toPlayer.z, 0f, toPlayer.x).normalized;
            Vector3 toward = toPlayer.normalized;
            return SafeNormalize(toward * 0.54f + tangent * 0.46f);
        }

        /// <summary>
        /// Returns a random planar offset whose magnitude is in [radius*0.35, radius].
        /// </summary>
        public static Vector3 CreateRandomPlanarOffset(float radius)
        {
            float safeRadius = Mathf.Max(0.1f, radius);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(safeRadius * 0.35f, safeRadius);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        private static Vector3 SafeNormalize(Vector3 vector)
        {
            return vector.sqrMagnitude < 0.0001f ? Vector3.zero : vector.normalized;
        }

        private Vector3 ComputeRuntimeSocialSpacingDirection()
        {
            float safeRadius = Mathf.Max(0.1f, _personalSpaceRadius);
            int count = Physics.OverlapSphereNonAlloc(transform.position, safeRadius, _nearbyCreatureBuffer);
            if (count <= 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var other = FindCreatureOnCollider(_nearbyCreatureBuffer[i]);
                if (other == null || other == _self || !other.IsAlive)
                    continue;

                sum += ComputeSocialSpacingContribution(transform.position, other.transform.position, safeRadius);
            }

            return SafeNormalize(sum);
        }

        private Vector3 ComputeRuntimeHerdCohesionDirection()
        {
            float awarenessRadius = Mathf.Max(_personalSpaceRadius + 0.1f, _herdAwarenessRadius);
            int count = Physics.OverlapSphereNonAlloc(transform.position, awarenessRadius, _nearbyCreatureBuffer);
            if (count <= 0)
                return Vector3.zero;

            var herdPositions = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                var otherAi = FindRivalAIOnCollider(_nearbyCreatureBuffer[i]);
                if (otherAi == null || otherAi == this || otherAi.Archetype != _archetype)
                    continue;

                var otherCreature = otherAi.GetComponent<CreatureInstance>();
                if (otherCreature == null || !otherCreature.IsAlive)
                    continue;

                herdPositions.Add(otherAi.transform.position);
            }

            return ComputeHerdCohesionDirection(
                transform.position,
                herdPositions,
                _personalSpaceRadius,
                awarenessRadius);
        }

        private static Vector3 ComputeSocialSpacingContribution(
            Vector3 selfPosition,
            Vector3 neighborPosition,
            float personalSpaceRadius)
        {
            Vector3 away = selfPosition - neighborPosition;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance <= 0.001f || distance >= personalSpaceRadius)
                return Vector3.zero;

            float closeness = 1f - (distance / Mathf.Max(0.1f, personalSpaceRadius));
            return away.normalized * closeness;
        }

        private static CreatureInstance FindCreatureOnCollider(Collider hit)
        {
            if (hit == null)
                return null;

            var direct = hit.GetComponent<CreatureInstance>();
            if (direct != null)
                return direct;

            if (hit.attachedRigidbody != null)
            {
                var body = hit.attachedRigidbody.GetComponent<CreatureInstance>();
                if (body != null)
                    return body;
            }

            var parent = hit.GetComponentInParent<CreatureInstance>();
            return parent != null ? parent : hit.GetComponentInChildren<CreatureInstance>();
        }

        private static CreatureRivalAI FindRivalAIOnCollider(Collider hit)
        {
            if (hit == null)
                return null;

            var direct = hit.GetComponent<CreatureRivalAI>();
            if (direct != null)
                return direct;

            if (hit.attachedRigidbody != null)
            {
                var body = hit.attachedRigidbody.GetComponent<CreatureRivalAI>();
                if (body != null)
                    return body;
            }

            var parent = hit.GetComponentInParent<CreatureRivalAI>();
            return parent != null ? parent : hit.GetComponentInChildren<CreatureRivalAI>();
        }

        private void OnCombatFeedback(CreatureCombatFeedbackEvent evt)
        {
            if (_self == null || !_self.IsAlive)
                return;

            if (evt.Type != CreatureCombatFeedbackType.Hit &&
                evt.Type != CreatureCombatFeedbackType.PlayerDamaged &&
                evt.Type != CreatureCombatFeedbackType.Kill &&
                evt.Type != CreatureCombatFeedbackType.Telegraph)
            {
                return;
            }

            if (evt.SourceCreatureId == _self.DNA?.CreatureId)
                return;

            Vector3 delta = evt.Position - transform.position;
            delta.y = 0f;
            if (delta.magnitude > Mathf.Max(0.1f, _alertReactionRadius))
                return;

            _lastAlertPosition = evt.Position;
            _alertTimer = Mathf.Max(_alertTimer, Mathf.Max(0.1f, _alertDurationSeconds));
            _ambientActivityTimer = 0f;
        }

        private static bool IsAmbientIntent(CreatureRivalIntent intent)
        {
            return intent == CreatureRivalIntent.Wander;
        }

        public static bool CanResolveTelegraphedAttack(float distance, float effectiveReach)
        {
            return distance <= Mathf.Max(0f, effectiveReach);
        }

        private static void ConfigureLocomotionBody(Rigidbody rb)
        {
            if (rb == null)
                return;

            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
