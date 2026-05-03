using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Rare, enormous predator that emerges once the ecosystem has matured (several species exist).
    /// States: Patrol → Hunt → Charge (telegraphed) → Rest.
    ///
    /// Design: gameplayLoopAndEvolutionDesign.md §8 ("giant leviathans"), activeContext "rare evolution paths".
    /// One leviathan exists at a time; CellStage spawns it after LeviathanMinSpeciesCount species appear
    /// and LeviathanMinSpawnSeconds have elapsed, then waits LeviathanRespawnIntervalSeconds before
    /// spawning the next one.
    ///
    /// The leviathan is not an Organism (no pool/genome evolution); it is a unique encounter creature.
    /// Contact damage fires on a cooldown. Charge is telegraphed by a slow-creep wind-up, then a
    /// high-speed dash. After contact or reaching maximum charge range the leviathan enters Rest briefly.
    /// On death it drops a large food burst and fires CellLeviathanDefeatedEvent.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LeviathanCell : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<LeviathanCell> _activeInstances =
            new System.Collections.Generic.List<LeviathanCell>(4);

        // ---- FSM ----
        private enum State { Patrol, Hunt, ChargingUp, Charging, Rest }

        // ---- Internal state ----
        private State  _state        = State.Patrol;
        private float  _health;
        private float  _stateTimer;
        private Vector3 _patrolTarget;
        private Vector3 _chargeDirection;
        private float  _damageCooldown;
        private bool   _playerSurvivedThisLeviathan;
        private bool   _dead;

        private PlayerCell _player;
        private Rigidbody  _rb;
        private FoodManager _foodManager;

        // ---- Accessors for tests ----
        public float  Health      => _health;
        public bool   IsDead      => _dead;
        public string StateName   => _state.ToString();

        /// <summary>
        /// Copies currently active leviathans into <paramref name="buffer"/> and returns the count.
        /// Null or destroyed references are compacted out of the registry.
        /// </summary>
        public static int CopyActiveInstances(System.Collections.Generic.List<LeviathanCell> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            CompactRegistry();
            var sceneInstances = FindObjectsByType<LeviathanCell>(FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneInstances.Length; i++)
            {
                var leviathan = sceneInstances[i];
                if (leviathan != null && !_activeInstances.Contains(leviathan))
                    _activeInstances.Add(leviathan);
            }

            for (int i = 0; i < _activeInstances.Count; i++)
            {
                var leviathan = _activeInstances[i];
                if (leviathan != null && leviathan.isActiveAndEnabled)
                    buffer.Add(leviathan);
            }

            return buffer.Count;
        }

        private static void CompactRegistry()
        {
            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                if (_activeInstances[i] == null)
                    _activeInstances.RemoveAt(i);
            }
        }

        // ---- Lifecycle ----

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity  = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

        private void OnEnable()
        {
            if (!_activeInstances.Contains(this))
                _activeInstances.Add(this);
        }

        private void OnDisable()
        {
            _activeInstances.Remove(this);
        }

        /// <summary>
        /// Call immediately after adding this component.
        /// </summary>
        public void Initialize(PlayerCell player, FoodManager foodManager)
        {
            _player      = player;
            _foodManager = foodManager;
            _health      = BalanceConstants.LeviathanMaxHealth;
            _state       = State.Patrol;
            _stateTimer  = BalanceConstants.LeviathanPatrolDuration;
            PickPatrolTarget();

            // Visual: large dark crimson cell
            transform.localScale = Vector3.one
                * BalanceConstants.LeviathanSize
                * BalanceConstants.CellEntityVisualScaleMultiplier;
            CellSwimVisual.Ensure(gameObject, CellSwimVisual.SwimProfile.Leviathan);

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.55f, 0.05f, 0.05f, 1f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", new Color(0.4f, 0f, 0f) * 1.5f);
            }

            EventBus.Publish(new CellLeviathanSpawnedEvent
            {
                Position = transform.position
            });
        }

        private void Update()
        {
            if (_dead || _player == null) return;
            _damageCooldown -= Time.deltaTime;
            _stateTimer     -= Time.deltaTime;

            switch (_state)
            {
                case State.Patrol:     UpdatePatrol();     break;
                case State.Hunt:       UpdateHunt();       break;
                case State.ChargingUp: UpdateChargingUp(); break;
                case State.Charging:   UpdateCharging();   break;
                case State.Rest:       UpdateRest();       break;
            }

            // Track player survival for achievement + record
            float dist = Vector3.Distance(transform.position, _player.transform.position);
            if (dist > BalanceConstants.LeviathanSurviveRadius && !_playerSurvivedThisLeviathan)
            {
                _playerSurvivedThisLeviathan = true;
                GameManager.Instance?.UnlockAchievement("cell_survived_leviathan");
                GameManager.Instance?.RecordLeviathanSurvived();
            }
        }

        // ---- States ----

        private void UpdatePatrol()
        {
            MoveToward(_patrolTarget, BalanceConstants.LeviathanPatrolSpeed);

            if (Vector3.Distance(transform.position, _patrolTarget) < 3f)
                PickPatrolTarget();

            float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
            if (distToPlayer < BalanceConstants.LeviathanDetectionRadius)
                EnterHunt();
            else if (_stateTimer <= 0f)
                PickPatrolTarget();
        }

        private void UpdateHunt()
        {
            MoveToward(_player.transform.position, BalanceConstants.LeviathanHuntSpeed);

            float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

            if (distToPlayer < BalanceConstants.LeviathanChargeWindupRadius)
                EnterChargeUp();
            else if (distToPlayer > BalanceConstants.LeviathanDetectionRadius * 1.5f)
                EnterPatrol(); // Player escaped detection range
        }

        private void UpdateChargingUp()
        {
            // Slow wind-up creep — telegraphs the charge direction
            MoveToward(_player.transform.position, BalanceConstants.LeviathanPatrolSpeed * 0.4f);

            if (_stateTimer <= 0f)
            {
                // Lock charge direction toward player's current position
                _chargeDirection = (_player.transform.position - transform.position).normalized;
                _chargeDirection.y = 0f;
                EnterCharging();
            }
        }

        private void UpdateCharging()
        {
            // High-speed dash in locked direction
            _rb.linearVelocity = _chargeDirection * BalanceConstants.LeviathanChargeSpeed;

            if (_stateTimer <= 0f)
                EnterRest();
        }

        private void UpdateRest()
        {
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.deltaTime * 3f);

            if (_stateTimer <= 0f)
            {
                float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);
                if (distToPlayer < BalanceConstants.LeviathanDetectionRadius)
                    EnterHunt();
                else
                    EnterPatrol();
            }
        }

        // ---- Transitions ----

        private void EnterPatrol()
        {
            _state      = State.Patrol;
            _stateTimer = BalanceConstants.LeviathanPatrolDuration;
            PickPatrolTarget();
        }

        private void EnterHunt()
        {
            _state      = State.Hunt;
            _stateTimer = 999f; // Hunt until close enough or player escapes
        }

        private void EnterChargeUp()
        {
            _state      = State.ChargingUp;
            _stateTimer = BalanceConstants.LeviathanChargeWindupSeconds;
        }

        private void EnterCharging()
        {
            _state      = State.Charging;
            _stateTimer = BalanceConstants.LeviathanChargeDurationSeconds;
        }

        private void EnterRest()
        {
            _state      = State.Rest;
            _stateTimer = BalanceConstants.LeviathanRestDurationSeconds;
            _rb.linearVelocity = Vector3.zero;
        }

        // ---- Movement helper ----

        private void MoveToward(Vector3 target, float speed)
        {
            var dir = (target - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            _rb.linearVelocity = dir.normalized * speed;
        }

        private void PickPatrolTarget()
        {
            float r = BalanceConstants.LeviathanPatrolRadius;
            _patrolTarget = new Vector3(
                Random.Range(-r, r), 0f, Random.Range(-r, r));
            _stateTimer = BalanceConstants.LeviathanPatrolDuration;
        }

        // ---- Contact damage ----

        private void OnTriggerStay(Collider other)
        {
            if (_dead || _damageCooldown > 0f) return;
            var pc = other.GetComponentInParent<PlayerCell>();
            if (pc == null) return;
            _damageCooldown = BalanceConstants.LeviathanDamageCooldown;
            pc.TakeDamage(BalanceConstants.LeviathanDamagePerHit);
            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.3f, 0.2f);
        }

        // ---- Taking damage ----

        /// <summary>Apply damage to the leviathan (e.g. from player spikes or toxin).</summary>
        public void TakeDamage(float amount)
        {
            if (_dead) return;
            _health -= amount;
            if (_health <= 0f)
                Die();
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;
            _activeInstances.Remove(this);

            // Burst of meat food at death position
            if (_foodManager != null)
            {
                for (int i = 0; i < BalanceConstants.LeviathanFoodDropCount; i++)
                {
                    var offset = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                    _foodManager.AddFoodAt(transform.position + offset, FoodType.Meat,
                        BalanceConstants.LeviathanFoodDropNutrition);
                }
            }

            GameManager.Instance?.UnlockAchievement("cell_defeated_leviathan");
            GameManager.Instance?.RecordLeviathanDefeated();
            EventBus.Publish(new CellLeviathanDefeatedEvent { Position = transform.position });

            Destroy(gameObject, 0.1f);
        }
    }
}
