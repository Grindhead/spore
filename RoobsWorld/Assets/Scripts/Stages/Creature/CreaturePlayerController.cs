using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Basic player controller for the Creature stage using camera-relative movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CreaturePlayerController : MonoBehaviour
    {
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _turnSpeedDegrees = 540f;
        [SerializeField] private float _fallbackMoveSpeed = 6f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _attackRange = 2.2f;
        [SerializeField] private float _attackRadius = 1.2f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _attackMultiplier = 1f;
        [SerializeField] private float _attackForwardOffset = 1.1f;
        [SerializeField] private float _passiveRegenPerSecond = 3f;
        [SerializeField] private float _passiveRegenDelayAfterDamage = 4f;
        [SerializeField] private float _healOnKill = 10f;

        private CharacterController _controller;
        private RoobsWorld.Creature.CreatureInstance _creature;
        private float _verticalVelocity;
        private float _attackCooldownTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _creature = GetComponent<RoobsWorld.Creature.CreatureInstance>();
        }

        private void Update()
        {
            if (_controller == null)
                return;
            if (_creature != null && !_creature.IsAlive)
                return;

            var moveInput = InputManager.HasInstance ? InputManager.Instance.MoveInput : Vector2.zero;
            var camera = CameraManager.HasInstance ? CameraManager.Instance.MainCamera : Camera.main;

            Vector3 moveWorld = ComputeMoveWorld(
                moveInput,
                camera != null ? camera.transform.forward : Vector3.forward,
                camera != null ? camera.transform.right : Vector3.right);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            float speed = GetMoveSpeed();
            Vector3 velocity = moveWorld * speed;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            if (moveWorld.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveWorld, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _turnSpeedDegrees * Time.deltaTime);
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            bool attackPressed = InputManager.HasInstance && InputManager.Instance.AttackPressed;
            if (attackPressed && _attackCooldownTimer <= 0f)
                TryAttack();

            HandlePassiveRegen();
        }

        private float GetMoveSpeed()
        {
            float baseSpeed = _creature != null
                ? Mathf.Max(1f, _creature.Stats.Speed)
                : _fallbackMoveSpeed;

            return baseSpeed * Mathf.Max(0.1f, _speedMultiplier);
        }

        private void TryAttack()
        {
            _attackCooldownTimer = _attackCooldown;

            Vector3 attackCenter = transform.position + transform.forward * _attackForwardOffset;
            float selfRadius = CreatureCombatGeometry.EstimatePlanarRadius(transform);
            float searchRadius = Mathf.Max(_attackRadius, _attackRange) + selfRadius + 0.5f;
            var hits = Physics.OverlapSphere(attackCenter, searchRadius);

            RoobsWorld.Creature.CreatureInstance closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].GetComponent<RoobsWorld.Creature.CreatureInstance>();
                if (candidate == null || candidate == _creature || !candidate.IsAlive)
                    continue;

                float candidateRadius = CreatureCombatGeometry.EstimatePlanarRadius(candidate.transform);
                float dist = HorizontalDistance(transform.position, candidate.transform.position);
                float effectiveReach = CreatureCombatGeometry.ComputeContactReach(_attackRange, selfRadius, candidateRadius);
                bool overlapsAttackZone = HorizontalDistance(attackCenter, candidate.transform.position) <= _attackRadius + candidateRadius;
                if (dist <= effectiveReach && overlapsAttackZone && dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = candidate;
                }
            }

            if (closest == null)
                return;

            float damageBase = _creature != null ? _creature.Stats.Attack : 6f;
            float damage = Mathf.Max(1f, damageBase * Mathf.Max(0.1f, _attackMultiplier));
            bool wasAliveBeforeHit = closest.IsAlive;
            closest.TakeDamage(damage);

            if (_creature != null && wasAliveBeforeHit && !closest.IsAlive)
                _creature.Heal(Mathf.Max(0f, _healOnKill));

            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.08f, 0.08f);
            if (InputManager.HasInstance)
                InputManager.Instance.RequestHaptic(0.15f, 0.2f, 0.08f);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void HandlePassiveRegen()
        {
            if (_creature == null || !_creature.IsAlive)
                return;

            float amount = ComputePassiveRegenAmount(
                currentHealth: _creature.CurrentHealth,
                maxHealth: _creature.MaxHealth,
                tookDamageRecently: _creature.WasDamagedWithin(_passiveRegenDelayAfterDamage),
                regenPerSecond: _passiveRegenPerSecond,
                deltaTime: Time.deltaTime);

            if (amount > 0f)
                _creature.Heal(amount);
        }

        /// <summary>
        /// Converts move input into a normalized world-space vector using camera orientation.
        /// </summary>
        public static Vector3 ComputeMoveWorld(Vector2 moveInput, Vector3 cameraForward, Vector3 cameraRight)
        {
            Vector3 flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z);
            Vector3 flatRight = new Vector3(cameraRight.x, 0f, cameraRight.z);

            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.forward;
            else
                flatForward.Normalize();

            if (flatRight.sqrMagnitude < 0.0001f)
                flatRight = Vector3.right;
            else
                flatRight.Normalize();

            Vector3 moveWorld = flatForward * moveInput.y + flatRight * moveInput.x;
            if (moveWorld.sqrMagnitude > 1f)
                moveWorld.Normalize();
            return moveWorld;
        }

        /// <summary>
        /// Computes passive regeneration amount for a single frame.
        /// </summary>
        public static float ComputePassiveRegenAmount(
            float currentHealth,
            float maxHealth,
            bool tookDamageRecently,
            float regenPerSecond,
            float deltaTime)
        {
            if (tookDamageRecently)
                return 0f;
            if (currentHealth >= maxHealth)
                return 0f;

            float safeRegen = Mathf.Max(0f, regenPerSecond);
            float safeDelta = Mathf.Max(0f, deltaTime);
            float missingHealth = Mathf.Max(0f, maxHealth - currentHealth);
            return Mathf.Min(missingHealth, safeRegen * safeDelta);
        }
    }
}
