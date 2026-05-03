using System.Collections.Generic;
using RoobsWorld.Creature;
using RoobsWorld.Creature.AI;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Simple rival AI that wanders around its anchor point and chases/attacks the player in range.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CreatureInstance))]
    public class CreatureRivalAI : MonoBehaviour
    {
        private const float MovementMemoryCellSize = 2.5f;
        private const int RecentLocationCapacity = 18;
        private const int WanderCandidateCount = 9;

        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _aggroRange = 14f;
        [SerializeField] private float _attackRange = 1.9f;
        [SerializeField] private float _attackCooldown = 0.9f;
        [SerializeField] private float _turnSpeedDegrees = 360f;
        [SerializeField] private float _speedMultiplier = 0.9f;
        [SerializeField] private float _attackMultiplier = 0.8f;

        private Rigidbody _rb;
        private CreatureInstance _self;
        private CreatureInstance _playerCreature;
        private Transform _player;
        private Vector3 _anchorPosition;
        private Vector3 _wanderTarget;
        private readonly Queue<Vector2Int> _recentLocations = new Queue<Vector2Int>();
        private float _wanderRetargetTimer;
        private float _attackCooldownTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _self = GetComponent<CreatureInstance>();
            _anchorPosition = transform.position;
            _wanderTarget = transform.position;
            PickWanderTarget();
        }

        public void SetPlayerTarget(Transform playerTarget)
        {
            _player = playerTarget;
            _playerCreature = _player != null ? _player.GetComponent<CreatureInstance>() : null;
        }

        private void Update()
        {
            if (_self == null || !_self.IsAlive || _rb == null)
                return;

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            _wanderRetargetTimer -= Time.deltaTime;
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

            if (intent == CreatureRivalIntent.ReturnToTerritory || !hasPlayer || playerDistance > _aggroRange)
            {
                if (_wanderRetargetTimer <= 0f || Vector3.Distance(transform.position, _wanderTarget) < 1.5f)
                    PickWanderTarget();
            }

            float speed = Mathf.Max(1f, _self.Stats.Speed) * Mathf.Max(0.1f, _speedMultiplier) * GetIntentSpeedMultiplier(intent);
            Vector3 planarVelocity = desired * speed;
            _rb.linearVelocity = new Vector3(planarVelocity.x, _rb.linearVelocity.y, planarVelocity.z);

            if (desired.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _turnSpeedDegrees * Time.deltaTime);
            }

            TryAttackPlayer();
        }

        private CreatureRivalIntent ChooseIntent(float playerDistance)
        {
            if (_playerCreature == null)
                return CreatureRivalIntent.Wander;

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

        private void TryAttackPlayer()
        {
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
            if (_attackCooldownTimer > 0f)
                return;

            float distance = HorizontalDistance(transform.position, _player.position);
            float selfRadius = CreatureCombatGeometry.EstimatePlanarRadius(transform);
            float playerRadius = CreatureCombatGeometry.EstimatePlanarRadius(_player);
            float effectiveReach = CreatureCombatGeometry.ComputeContactReach(_attackRange, selfRadius, playerRadius);
            if (distance > effectiveReach)
                return;

            float damage = Mathf.Max(1f, _self.Stats.Attack * Mathf.Max(0.1f, _attackMultiplier));
            _playerCreature.TakeDamage(damage);
            _attackCooldownTimer = _attackCooldown;
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

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
