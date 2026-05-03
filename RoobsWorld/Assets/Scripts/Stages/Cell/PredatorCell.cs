using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Large hostile cell that hunts the player.
    /// States:
    ///   Wander - patrol (random if roaming; within territory if territorial).
    ///   Ambush - slow creep while lurking in the outer detection ring; burst-sprints on strike range.
    ///   Chase - direct pursuit at full speed (+ burst window after ambush trigger).
    ///   Flee - retreats when the player has grown significantly larger.
    ///   Return - (territorial only) returns to territory origin after a long chase took it too far.
    /// Contact damage fires on a cooldown so repeated collision remains fair.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PredatorCell : MonoBehaviour
    {
        // ---- Tuning ----
        [SerializeField] private float _moveSpeed = 3.3f;
        [SerializeField] private float _perceptionRadius = 13.5f;
        [SerializeField] private float _strikeRadius = 4.2f;
        [SerializeField] private float _minScale = 1.2f;
        [SerializeField] private float _maxScale = 2.5f;
        [SerializeField] private float _damagePerHit = 0.18f;
        [SerializeField] private float _damageCooldown = 0.75f;
        [SerializeField] private float _burstDuration = 0.95f;
        [SerializeField] private float _burstSpeedMultiplier = 1.75f;
        [SerializeField] private float _fleeScaleRatio = 1.45f;

        // ---- Territory (optional) ----
        private bool _isTerritorial;
        private Vector3 _territoryOrigin;
        private float _territoryRadius;
        private Vector3 _patrolTarget;

        private const float TerritoryAbandonMultiplier = 2.0f;
        private const float PatrolWaypointArrivalDist = 2.0f;
        private const float PlayerRefreshInterval = 0.35f;

        private enum PredatorState
        {
            Wander,
            Ambush,
            Chase,
            Flee,
            Return
        }

        private Rigidbody _rb;
        private PlayerCell _player;
        private Vector3 _wanderDir;
        private float _wanderTimer;
        private float _damageTimer;
        private float _burstTimer;
        private float _sizeSpeedMultiplier = 1f;
        private float _sizeDamageMultiplier = 1f;
        private float _playerRefreshTimer;
        private PredatorState _state = PredatorState.Wander;

        private float EffectivePerceptionRadius => _perceptionRadius * CellFeelDirector.PredatorPerceptionMultiplier;
        private float EffectiveMoveSpeed => _moveSpeed * CellFeelDirector.PredatorSpeedMultiplier;
        private float EffectiveDamagePerHit => _damagePerHit * _sizeDamageMultiplier * CellFeelDirector.PredatorDamageMultiplier;
        private float EffectiveDamageCooldown => _damageCooldown / Mathf.Max(0.01f, CellFeelDirector.PredatorAttackCadenceMultiplier);

        public bool IsTerritorial => _isTerritorial;
        public Vector3 TerritoryOrigin => _territoryOrigin;
        public float TerritoryRadius => _territoryRadius;
        public bool IsChasing => _state == PredatorState.Chase;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            PredatorPackCoordinator.Register(this);
        }

        private void OnDestroy()
        {
            PredatorPackCoordinator.Unregister(this);
        }

        private void Start()
        {
            PickWanderDirection();
        }

        public void Initialize(float scaleFactor)
        {
            float size = Mathf.Lerp(_minScale, _maxScale, scaleFactor);
            transform.localScale = Vector3.one * size * BalanceConstants.CellEntityVisualScaleMultiplier;
            _sizeSpeedMultiplier = Mathf.Lerp(1.35f, 0.85f, scaleFactor);
            _sizeDamageMultiplier = Mathf.Lerp(0.85f, 1.15f, scaleFactor);
            CellSwimVisual.Ensure(gameObject, CellSwimVisual.SwimProfile.Predator);
        }

        public void SetTerritory(Vector3 origin, float radius)
        {
            _isTerritorial = true;
            _territoryOrigin = origin;
            _territoryOrigin.y = 0f;
            _territoryRadius = Mathf.Max(1f, radius);
            _patrolTarget = PickTerritoryWaypoint();
        }

        private void FixedUpdate()
        {
            _damageTimer -= Time.fixedDeltaTime;
            RefreshPlayerReference(Time.fixedDeltaTime);
            UpdateState();
            ExecuteState();
        }

        // ---- State machine ----

        private void UpdateState()
        {
            if (_player == null)
            {
                _state = PredatorState.Wander;
                return;
            }

            if (_player.transform.localScale.x >= transform.localScale.x * _fleeScaleRatio)
            {
                _state = PredatorState.Flee;
                return;
            }

            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distSqr = toPlayer.sqrMagnitude;

            float perception = EffectivePerceptionRadius;
            float perceptionSqr = perception * perception;
            float strikeRadiusSqr = _strikeRadius * _strikeRadius;
            float chaseLoseDistanceSqr = (perception * 1.3f) * (perception * 1.3f);
            float fleeExitDistanceSqr = (perception * 1.5f) * (perception * 1.5f);

            switch (_state)
            {
                case PredatorState.Wander:
                    if (distSqr <= perceptionSqr)
                        _state = PredatorState.Ambush;
                    break;

                case PredatorState.Ambush:
                    if (distSqr > perceptionSqr)
                    {
                        _state = PredatorState.Wander;
                    }
                    else if (distSqr <= strikeRadiusSqr)
                    {
                        _burstTimer = _burstDuration;
                        _state = PredatorState.Chase;
                    }
                    break;

                case PredatorState.Chase:
                    _burstTimer -= Time.fixedDeltaTime;
                    float abandonRadius = _territoryRadius * TerritoryAbandonMultiplier;
                    if (_isTerritorial &&
                        (transform.position - _territoryOrigin).sqrMagnitude > abandonRadius * abandonRadius)
                    {
                        _state = PredatorState.Return;
                    }
                    else if (distSqr > chaseLoseDistanceSqr)
                    {
                        _state = _isTerritorial ? PredatorState.Return : PredatorState.Wander;
                    }
                    break;

                case PredatorState.Flee:
                    if (_player.transform.localScale.x < transform.localScale.x * _fleeScaleRatio ||
                        distSqr > fleeExitDistanceSqr)
                    {
                        _state = _isTerritorial ? PredatorState.Return : PredatorState.Wander;
                    }
                    break;

                case PredatorState.Return:
                    float returnRadius = _territoryRadius * 0.6f;
                    if ((transform.position - _territoryOrigin).sqrMagnitude <= returnRadius * returnRadius)
                    {
                        _patrolTarget = PickTerritoryWaypoint();
                        _state = PredatorState.Wander;
                    }
                    break;
            }
        }

        private void ExecuteState()
        {
            switch (_state)
            {
                case PredatorState.Wander:
                    Wander();
                    break;

                case PredatorState.Ambush:
                    if (_player != null)
                    {
                        var dir = (_player.transform.position - transform.position).normalized;
                        dir.y = 0f;
                        SetSeparatedVelocity(dir, EffectiveMoveSpeed * 0.18f);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector3.zero;
                    }
                    break;

                case PredatorState.Chase:
                    if (_player != null)
                    {
                        Vector3 target = PredatorPackCoordinator.GetFlankTarget(this, _player.transform.position);
                        var dir = (target - transform.position).normalized;
                        dir.y = 0f;
                        float speedMult = _burstTimer > 0f ? _burstSpeedMultiplier : 1f;
                        SetSeparatedVelocity(dir, EffectiveMoveSpeed * speedMult * _sizeSpeedMultiplier);
                    }
                    break;

                case PredatorState.Flee:
                    if (_player != null)
                    {
                        var dir = (transform.position - _player.transform.position).normalized;
                        dir.y = 0f;
                        SetSeparatedVelocity(dir, EffectiveMoveSpeed * 1.2f * _sizeSpeedMultiplier);
                    }
                    break;

                case PredatorState.Return:
                {
                    var dir = _territoryOrigin - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        SetSeparatedVelocity(dir.normalized, EffectiveMoveSpeed * _sizeSpeedMultiplier);
                    else
                        _rb.linearVelocity = Vector3.zero;
                    break;
                }
            }
        }

        // ---- Helpers ----

        private void RefreshPlayerReference(float deltaTime)
        {
            bool hasPlayer = _player != null;
            bool playerActive = hasPlayer && _player.isActiveAndEnabled;
            bool playerDead = hasPlayer && _player.IsDead;
            _playerRefreshTimer -= deltaTime;

            if (!ShouldRefreshPlayerReference(_playerRefreshTimer, hasPlayer, playerActive, playerDead))
                return;

            _playerRefreshTimer = PlayerRefreshInterval;

            if (PlayerCell.TryGetActiveInstance(out var activePlayer))
            {
                _player = activePlayer;
                return;
            }

            _player = FindAnyObjectByType<PlayerCell>();
        }

        /// <summary>
        /// Returns true when predator should refresh its player reference.
        /// </summary>
        public static bool ShouldRefreshPlayerReference(
            float refreshTimerRemaining,
            bool hasPlayer,
            bool playerIsActive,
            bool playerIsDead)
        {
            if (!hasPlayer) return true;
            if (!playerIsActive) return true;
            if (playerIsDead) return true;
            return refreshTimerRemaining <= 0f;
        }

        private void Wander()
        {
            if (_isTerritorial)
            {
                var toTarget = _patrolTarget - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < PatrolWaypointArrivalDist * PatrolWaypointArrivalDist)
                {
                    _patrolTarget = PickTerritoryWaypoint();
                    toTarget = _patrolTarget - transform.position;
                    toTarget.y = 0f;
                }

                if (toTarget.sqrMagnitude > 0.01f)
                    SetSeparatedVelocity(toTarget.normalized, EffectiveMoveSpeed * 0.45f * _sizeSpeedMultiplier);
            }
            else
            {
                _wanderTimer -= Time.fixedDeltaTime;
                if (_wanderTimer <= 0f)
                    PickWanderDirection();
                SetSeparatedVelocity(_wanderDir, EffectiveMoveSpeed * 0.5f * _sizeSpeedMultiplier);
            }
        }

        private void SetSeparatedVelocity(Vector3 direction, float speed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                _rb.linearVelocity = Vector3.zero;
                return;
            }

            Vector3 separation = CellSeparation.GetDirection(transform);
            _rb.linearVelocity = CellSeparation.Apply(direction, separation) * speed
                + separation * (speed * CellSeparation.PushSpeedMultiplier);
        }

        private void PickWanderDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _wanderDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            _wanderTimer = Random.Range(2f, 5f);
        }

        private Vector3 PickTerritoryWaypoint()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = _territoryRadius * Random.Range(0.25f, 0.75f);
            return _territoryOrigin + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        // ---- Contact damage (cooldown-based) ----

        private void OnTriggerStay(Collider other)
        {
            if (_damageTimer > 0f) return;
            var player = other.GetComponent<PlayerCell>() ?? other.GetComponentInParent<PlayerCell>();
            if (player != null && _state != PredatorState.Flee)
            {
                player.TakeDamage(EffectiveDamagePerHit);
                _damageTimer = EffectiveDamageCooldown;
            }
        }
    }
}
