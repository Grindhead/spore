using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureInheritedAbilityType
    {
        None,
        Dash,
        ToxinBurst,
        BioelectricBurst,
        RegenerativeSurge,
        GuardSurge
    }

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
        [SerializeField] private float _guardDuration = 0.48f;
        [SerializeField] private float _guardCooldown = 1.4f;
        [SerializeField] private float _guardMoveMultiplier = 0.65f;
        [SerializeField] private float _guardCounterWindow = 1.1f;
        [SerializeField] private float _guardCounterAttackMultiplier = 1.45f;
        [SerializeField] private float _maxStamina = BalanceConstants.CreaturePlayerMaxStamina;
        [SerializeField] private float _staminaRecoveryPerSecond = BalanceConstants.CreaturePlayerStaminaRecoveryPerSecond;
        [SerializeField] private float _attackStaminaCost = BalanceConstants.CreaturePlayerAttackStaminaCost;
        [SerializeField] private float _guardStaminaCost = BalanceConstants.CreaturePlayerGuardStaminaCost;
        [SerializeField] private float _abilityStaminaCost = BalanceConstants.CreaturePlayerAbilityStaminaCost;
        [SerializeField] private float _abilityCooldown = BalanceConstants.CreaturePlayerAbilityCooldownSeconds;
        [SerializeField] private float _abilityDashDistance = 4.5f;
        [SerializeField] private float _abilityRadius = 3.2f;
        [SerializeField] private float _abilityDamage = 8f;
        [SerializeField] private float _staminaRestoredOnKill = BalanceConstants.CreaturePlayerStaminaRestoredOnKill;
        [SerializeField] private float _staminaRestoredOnCounter = BalanceConstants.CreaturePlayerStaminaRestoredOnCounter;

        private CharacterController _controller;
        private RoobsWorld.Creature.CreatureInstance _creature;
        private float _verticalVelocity;
        private float _attackCooldownTimer;
        private float _guardTimer;
        private float _guardCooldownTimer;
        private float _counterAttackTimer;
        private float _currentStamina;
        private float _abilityCooldownTimer;

        public float CurrentStamina => _currentStamina;
        public float MaxStamina => ComputeMaxStamina(_creature?.DNA, _maxStamina);
        public float NormalizedStamina => Mathf.Clamp01(_currentStamina / MaxStamina);
        public float AbilityCooldownRemaining => _abilityCooldownTimer;
        public float AbilityCooldownDuration => ComputeAbilityCooldown(_creature?.DNA, _abilityCooldown);
        public CreatureInheritedAbilityType InheritedAbility => ResolveInheritedAbility(_creature?.DNA);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _creature = GetComponent<RoobsWorld.Creature.CreatureInstance>();
            _currentStamina = MaxStamina;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
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

            _guardTimer = Mathf.Max(0f, _guardTimer - Time.deltaTime);
            _guardCooldownTimer = Mathf.Max(0f, _guardCooldownTimer - Time.deltaTime);
            _counterAttackTimer = Mathf.Max(0f, _counterAttackTimer - Time.deltaTime);
            _abilityCooldownTimer = Mathf.Max(0f, _abilityCooldownTimer - Time.deltaTime);
            _currentStamina = RecoverStamina(
                _currentStamina,
                MaxStamina,
                ComputeStaminaRecovery(_creature?.DNA, _staminaRecoveryPerSecond),
                Time.deltaTime);

            bool interactPressed = InputManager.HasInstance && InputManager.Instance.InteractPressed;
            bool usedInteractAsAbility = interactPressed && TryUseInheritedAbility();
            bool guardPressed = InputManager.HasInstance && (InputManager.Instance.JumpPressed || (interactPressed && !usedInteractAsAbility));
            if (guardPressed && CanStartGuard(_guardCooldownTimer, _guardTimer) && TrySpendStamina(_guardStaminaCost, "guard"))
                StartGuard();

            float speed = GetMoveSpeed();
            if (_guardTimer > 0f)
                speed *= Mathf.Clamp(_guardMoveMultiplier, 0.1f, 1f);
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
            if (attackPressed && _attackCooldownTimer <= 0f && TrySpendStamina(_attackStaminaCost, "attack"))
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
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].GetComponent<RoobsWorld.Creature.CreatureInstance>();
                if (candidate == null || candidate == _creature || !candidate.IsAlive)
                    continue;

                float candidateRadius = CreatureCombatGeometry.EstimatePlanarRadius(candidate.transform);
                float dist = HorizontalDistance(transform.position, candidate.transform.position);
                float effectiveReach = CreatureCombatGeometry.ComputeContactReach(_attackRange, selfRadius, candidateRadius);
                bool overlapsAttackZone = HorizontalDistance(attackCenter, candidate.transform.position) <= _attackRadius + candidateRadius;
                Vector3 toCandidate = candidate.transform.position - transform.position;
                bool assistedReach = dist <= effectiveReach * 1.12f &&
                                      CreatureTargetAssist.IsWithinAssistCone(transform.forward, toCandidate);
                if (!assistedReach && !(dist <= effectiveReach && overlapsAttackZone))
                    continue;

                float forwardDot = toCandidate.sqrMagnitude < 0.0001f
                    ? 1f
                    : Vector3.Dot(transform.forward, toCandidate.normalized);
                float score = CreatureTargetAssist.ComputeTargetScore(dist, forwardDot, overlapsAttackZone);
                if (score > bestScore)
                {
                    bestScore = score;
                    closest = candidate;
                }
            }

            if (closest == null)
            {
                _counterAttackTimer = 0f;
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Miss,
                    SourceCreatureId = _creature?.DNA?.CreatureId,
                    Position = attackCenter,
                    IsPlayerSource = true
                });
                return;
            }

            float damageBase = _creature != null ? _creature.Stats.Attack : 6f;
            float baseDamage = damageBase * Mathf.Max(0.1f, _attackMultiplier);
            float damage = Mathf.Max(1f, ApplyCounterAttackMultiplier(
                baseDamage,
                _counterAttackTimer,
                _guardCounterAttackMultiplier));
            _counterAttackTimer = 0f;
            bool wasAliveBeforeHit = closest.IsAlive;
            closest.TakeDamage(damage);

            if (_creature != null && wasAliveBeforeHit && !closest.IsAlive)
            {
                _creature.Heal(Mathf.Max(0f, _healOnKill));
                RestoreStamina(_staminaRestoredOnKill, "kill");
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Kill,
                    SourceCreatureId = _creature.DNA?.CreatureId,
                    TargetCreatureId = closest.DNA?.CreatureId,
                    Position = closest.transform.position,
                    Amount = damage,
                    IsPlayerSource = true
                });
            }
            else
            {
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Hit,
                    SourceCreatureId = _creature?.DNA?.CreatureId,
                    TargetCreatureId = closest.DNA?.CreatureId,
                    Position = closest.transform.position,
                    Amount = damage,
                    IsPlayerSource = true
                });
            }

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

        private void StartGuard()
        {
            _guardTimer = Mathf.Max(0.05f, _guardDuration);
            _guardCooldownTimer = Mathf.Max(_guardTimer, _guardCooldown);
            _creature?.GrantInvulnerability(_guardTimer);
            EventBus.Publish(new CreatureCombatFeedbackEvent
            {
                Type = CreatureCombatFeedbackType.Guard,
                SourceCreatureId = _creature?.DNA?.CreatureId,
                Position = transform.position,
                IsPlayerSource = true
            });

            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.04f, 0.05f);
            if (InputManager.HasInstance)
                InputManager.Instance.RequestHaptic(0.08f, 0.18f, 0.06f);
        }

        private bool TryUseInheritedAbility()
        {
            var ability = InheritedAbility;
            if (ability == CreatureInheritedAbilityType.None)
                return false;

            if (_abilityCooldownTimer > 0f)
                return true;

            if (!TrySpendStamina(_abilityStaminaCost, "ability"))
                return true;

            _abilityCooldownTimer = Mathf.Max(0.1f, AbilityCooldownDuration);
            switch (ability)
            {
                case CreatureInheritedAbilityType.ToxinBurst:
                    ApplyAreaAbility(Mathf.Max(1f, _abilityRadius), Mathf.Max(1f, _abilityDamage * 1.2f));
                    break;
                case CreatureInheritedAbilityType.BioelectricBurst:
                    ApplyAreaAbility(Mathf.Max(1f, _abilityRadius * 1.12f), Mathf.Max(1f, _abilityDamage));
                    break;
                case CreatureInheritedAbilityType.RegenerativeSurge:
                    _creature?.Heal(24f);
                    break;
                case CreatureInheritedAbilityType.GuardSurge:
                    _creature?.GrantInvulnerability(1.15f);
                    break;
                default:
                    ApplyDashAbility(Mathf.Max(0.5f, _abilityDashDistance));
                    break;
            }

            string label = GetAbilityDisplayName(ability);
            EventBus.Publish(new CreatureAbilityUsedEvent
            {
                AbilityId = GetAbilityId(ability),
                AbilityLabel = label,
                StaminaSpent = Mathf.Max(0f, _abilityStaminaCost),
                Position = transform.position
            });

            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.08f, 0.1f);
            if (InputManager.HasInstance)
                InputManager.Instance.RequestHaptic(0.12f, 0.22f, 0.08f);

            return true;
        }

        private void ApplyDashAbility(float distance)
        {
            if (_controller == null)
                return;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            _controller.Move(forward.normalized * distance);
        }

        private void ApplyAreaAbility(float radius, float damage)
        {
            var hits = Physics.OverlapSphere(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].GetComponentInParent<RoobsWorld.Creature.CreatureInstance>();
                if (candidate == null || candidate == _creature || !candidate.IsAlive)
                    continue;

                candidate.TakeDamage(damage);
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Hit,
                    SourceCreatureId = _creature?.DNA?.CreatureId,
                    TargetCreatureId = candidate.DNA?.CreatureId,
                    Position = candidate.transform.position,
                    Amount = damage,
                    IsPlayerSource = true
                });
            }
        }

        private bool TrySpendStamina(float cost, string actionId)
        {
            float safeCost = Mathf.Max(0f, cost);
            if (CanSpendStamina(_currentStamina, safeCost))
            {
                _currentStamina = SpendStamina(_currentStamina, safeCost);
                return true;
            }

            EventBus.Publish(new CreatureStaminaExhaustedEvent
            {
                ActionId = actionId,
                CurrentStamina = _currentStamina,
                RequiredStamina = safeCost
            });
            return false;
        }

        private void OnCombatFeedback(CreatureCombatFeedbackEvent evt)
        {
            if (_creature == null || !_creature.IsAlive)
                return;

            if (!string.IsNullOrEmpty(evt.TargetCreatureId) && evt.TargetCreatureId != _creature.DNA?.CreatureId)
                return;

            if (!CanPrimeGuardCounter(_guardTimer, evt.Type, evt.IsPlayerTarget))
                return;

            _counterAttackTimer = Mathf.Max(_counterAttackTimer, Mathf.Max(0.05f, _guardCounterWindow));
            RestoreStamina(_staminaRestoredOnCounter, "counter");
            EventBus.Publish(new CreatureCombatFeedbackEvent
            {
                Type = CreatureCombatFeedbackType.Counter,
                SourceCreatureId = _creature?.DNA?.CreatureId,
                Position = transform.position,
                Amount = _guardCounterAttackMultiplier,
                IsPlayerSource = true
            });

            if (InputManager.HasInstance)
                InputManager.Instance.RequestHaptic(0.12f, 0.35f, 0.07f);
        }

        private void RestoreStamina(float amount, string reasonId)
        {
            float safeAmount = Mathf.Max(0f, amount);
            if (safeAmount <= 0f)
                return;

            float before = _currentStamina;
            _currentStamina = RestoreStamina(_currentStamina, MaxStamina, safeAmount);
            float restored = _currentStamina - before;
            if (restored <= 0.001f)
                return;

            EventBus.Publish(new CreatureStaminaRestoredEvent
            {
                ReasonId = reasonId,
                Amount = restored,
                CurrentStamina = _currentStamina,
                MaxStamina = MaxStamina
            });
        }

        public void RestoreStaminaFromObjective(float amount)
        {
            RestoreStamina(amount, "objective");
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

        public static bool CanSpendStamina(float currentStamina, float cost)
        {
            return Mathf.Max(0f, currentStamina) >= Mathf.Max(0f, cost);
        }

        public static float SpendStamina(float currentStamina, float cost)
        {
            return Mathf.Max(0f, Mathf.Max(0f, currentStamina) - Mathf.Max(0f, cost));
        }

        public static float RecoverStamina(float currentStamina, float maxStamina, float recoveryPerSecond, float deltaTime)
        {
            float max = Mathf.Max(1f, maxStamina);
            float current = Mathf.Clamp(currentStamina, 0f, max);
            float recovered = Mathf.Max(0f, recoveryPerSecond) * Mathf.Max(0f, deltaTime);
            return Mathf.Min(max, current + recovered);
        }

        public static float RestoreStamina(float currentStamina, float maxStamina, float restoreAmount)
        {
            float max = Mathf.Max(1f, maxStamina);
            float current = Mathf.Clamp(currentStamina, 0f, max);
            return Mathf.Min(max, current + Mathf.Max(0f, restoreAmount));
        }

        public static float ComputeMaxStamina(RoobsWorld.Creature.CreatureDNA dna, float baseMaxStamina)
        {
            float value = Mathf.Max(1f, baseMaxStamina);
            if (dna?.Upgrades != null)
            {
                for (int i = 0; i < dna.Upgrades.Count; i++)
                    value += dna.Upgrades[i]?.MaxStaminaDelta ?? 0f;
            }

            return Mathf.Max(25f, value);
        }

        public static float ComputeStaminaRecovery(RoobsWorld.Creature.CreatureDNA dna, float baseRecoveryPerSecond)
        {
            float value = Mathf.Max(0f, baseRecoveryPerSecond);
            if (dna?.Upgrades != null)
            {
                for (int i = 0; i < dna.Upgrades.Count; i++)
                    value += dna.Upgrades[i]?.StaminaRecoveryDelta ?? 0f;
            }

            return Mathf.Max(1f, value);
        }

        public static float ComputeAbilityCooldown(RoobsWorld.Creature.CreatureDNA dna, float baseCooldownSeconds)
        {
            float value = Mathf.Max(0.1f, baseCooldownSeconds);
            if (dna?.Upgrades != null)
            {
                for (int i = 0; i < dna.Upgrades.Count; i++)
                    value += dna.Upgrades[i]?.AbilityCooldownDelta ?? 0f;
            }

            return Mathf.Max(1f, value);
        }

        public static CreatureInheritedAbilityType ResolveInheritedAbility(RoobsWorld.Creature.CreatureDNA dna)
        {
            string id = dna?.LegacyAbilityId;
            if (!string.IsNullOrWhiteSpace(id))
            {
                switch (id.Trim().ToLowerInvariant())
                {
                    case "toxin_burst": return CreatureInheritedAbilityType.ToxinBurst;
                    case "bioelectric_burst": return CreatureInheritedAbilityType.BioelectricBurst;
                    case "regenerative_surge": return CreatureInheritedAbilityType.RegenerativeSurge;
                    case "guard_surge": return CreatureInheritedAbilityType.GuardSurge;
                    case "dash":
                    case "legacy_dash":
                    case "survival_dash":
                        return CreatureInheritedAbilityType.Dash;
                }
            }

            if (dna?.Parts != null)
            {
                for (int i = 0; i < dna.Parts.Count; i++)
                {
                    string partId = dna.Parts[i]?.PartId;
                    if (partId == "tail_basic" || partId == "wing_basic")
                        return CreatureInheritedAbilityType.Dash;
                    if (partId == "shell_basic")
                        return CreatureInheritedAbilityType.GuardSurge;
                    if (partId == "spike_basic" || partId == "horn_basic")
                        return CreatureInheritedAbilityType.BioelectricBurst;
                }
            }

            return CreatureInheritedAbilityType.None;
        }

        public static string GetAbilityId(CreatureInheritedAbilityType ability)
        {
            return ability switch
            {
                CreatureInheritedAbilityType.ToxinBurst => "toxin_burst",
                CreatureInheritedAbilityType.BioelectricBurst => "bioelectric_burst",
                CreatureInheritedAbilityType.RegenerativeSurge => "regenerative_surge",
                CreatureInheritedAbilityType.GuardSurge => "guard_surge",
                CreatureInheritedAbilityType.Dash => "dash",
                _ => "none"
            };
        }

        public static string GetAbilityLocalizationKey(CreatureInheritedAbilityType ability)
        {
            return ability switch
            {
                CreatureInheritedAbilityType.ToxinBurst => "creature.ability.toxin_burst",
                CreatureInheritedAbilityType.BioelectricBurst => "creature.ability.bioelectric_burst",
                CreatureInheritedAbilityType.RegenerativeSurge => "creature.ability.regenerative_surge",
                CreatureInheritedAbilityType.GuardSurge => "creature.ability.guard_surge",
                CreatureInheritedAbilityType.Dash => "creature.ability.dash",
                _ => "creature.ability.none"
            };
        }

        public static string GetAbilityDisplayName(CreatureInheritedAbilityType ability)
        {
            return Localization.Get(GetAbilityLocalizationKey(ability));
        }

        public static bool CanStartGuard(float cooldownRemaining, float guardRemaining)
        {
            return cooldownRemaining <= 0f && guardRemaining <= 0f;
        }

        public static bool CanPrimeGuardCounter(
            float guardRemaining,
            CreatureCombatFeedbackType feedbackType,
            bool isPlayerTarget)
        {
            return guardRemaining > 0f
                && isPlayerTarget
                && feedbackType == CreatureCombatFeedbackType.Blocked;
        }

        public static float ApplyCounterAttackMultiplier(
            float baseDamage,
            float counterWindowRemaining,
            float counterMultiplier)
        {
            if (counterWindowRemaining <= 0f)
                return Mathf.Max(0f, baseDamage);

            return Mathf.Max(0f, baseDamage) * Mathf.Max(1f, counterMultiplier);
        }
    }
}
