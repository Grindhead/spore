using RoobsWorld.Core;
using RoobsWorld.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Controls the player-controlled cell: movement, growth from food, evolution triggers, and death handling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCell : MonoBehaviour
    {
        private static PlayerCell _activeInstance;

        private const float SpikeContactDamage = 0.08f;
        private const float OrganismConsumeMassFactor = 0.45f;
        private const float NpcCellConsumeSizeRatio = 0.8f;
        private const float OrganismConsumeSizeRatio = 0.82f;
        private const float EndosymbiosisTraitTransferChance = 0.35f;
        private const double EndosymbiosisPreferredTraitBiasChance = 0.72d;
        private const int MaxSymbiosisLevel = 3;
        private const float BaseVisualScale = 0.08f;
        private const float GenomeSizeVisualScaleFactor = 0.05f;
        private const float MassVisualScaleFactor = 0.08f;
        private const float MinimumConsumptionMassGain = 0.005f;
        private const float UpgradeSizeBonus = 0.25f;
        private const float UpgradeSpeedBonus = 0.35f;
        private const float MinGenomeSpeedMultiplier = 0.9f;
        private const float MaxGenomeSpeedMultiplier = 1.6f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5.6f;
        [SerializeField] private float _boostMultiplier = 1.55f;
        [SerializeField] private float _boostDuration = 0.42f;
        [SerializeField] private float _acceleration = 9.6f;
        [SerializeField] private float _deceleration = 8.6f;
        [SerializeField] private float _turnSpeed = 5.1f;
        [SerializeField] private float _mouseDeadZone = 1.4f;
        [SerializeField] private float _currentFlowInfluence = 0.2f;

        [Header("Growth")]
        [SerializeField] private float _currentMass = 1f;
        [SerializeField] private float _growthPerFood = 0.12f;
        [SerializeField] private float _evolutionThreshold = BalanceConstants.CellEvolutionMassThresholdBase;
        [SerializeField] private int _foodConsumedPerUpgrade = BalanceConstants.CellFoodConsumedPerUpgrade;
        [SerializeField] private float _upgradePromptDelayAfterSpawnSeconds = 2f;

        private Rigidbody _rb;
        private float _boostTimer;
        private int _dnaPointsCollected;
        private readonly CellPartLoadout _partLoadout = new();
        private EvolutionTracker _evolutionTracker;
        private bool _isEvolving;
        private Genome _playerGenome;
        private bool _reproductionUIActive;
        private readonly BehaviourBiasTracker _biasTracker = new BehaviourBiasTracker();

        private const float MinimumMass = 0.5f;
        private bool _isDead;
        private float _peakMass;
        private float _maxUnlockedBaseScale;
        private CellStage _stageCache;
        private int _foodConsumedTotal;
        private int _foodConsumedSinceUpgrade;
        private float _upgradePromptUnlockTime;

        public float CurrentMass => _currentMass;
        public float PeakMass => _peakMass;
        public Genome PlayerGenome => _playerGenome;
        public float MaxMassForStage => _evolutionThreshold * MaxEvolutions;
        public float NormalizedMass => Mathf.Clamp01(_currentMass / MaxMassForStage);
        public int DNAPoints => _dnaPointsCollected;
        public CellPartLoadout PartLoadout => _partLoadout;
        public bool IsDead => _isDead;
        public bool HasAnySymbiosisLevels =>
            _photosyntheticSymbiosisLevel > 0 ||
            _toxicSymbiosisLevel > 0 ||
            _armouredSymbiosisLevel > 0 ||
            _regenerativeSymbiosisLevel > 0 ||
            _electricSymbiosisLevel > 0;

        /// <summary>Returns true when there is a currently active, alive player cell instance.</summary>
        public static bool TryGetActiveInstance(out PlayerCell player)
        {
            if (_activeInstance != null && _activeInstance.isActiveAndEnabled && !_activeInstance.IsDead)
            {
                player = _activeInstance;
                return true;
            }

            var scenePlayers = FindObjectsByType<PlayerCell>(FindObjectsInactive.Exclude);
            for (int i = 0; i < scenePlayers.Length; i++)
            {
                var candidate = scenePlayers[i];
                if (candidate != null && candidate.isActiveAndEnabled && !candidate.IsDead)
                {
                    _activeInstance = candidate;
                    player = candidate;
                    return true;
                }
            }

            _activeInstance = null;
            player = null;
            return false;
        }

        private const int MaxEvolutions = 3;

        // Genetic trait passive rates (mass-equivalent, scaled down from NPC energy rates)
        private const float TraitPhotosyntheticMassRate = 0.004f;  // mass/sec
        private const float TraitRegenerativeMassRate   = 0.003f;  // mass/sec
        private const float TraitRegenerativeDigestionBonusMultiplier = 1.1f;
        private const float TraitPassiveSymbiosisBonusPerLevel = 0.20f;
        private const float TraitRegenerativeDigestionBonusPerLevel = 0.08f;
        private const float TraitArmouredReductionBonusPerLevel = 0.05f;
        private const float TraitElectricIntervalReductionPerLevel = 0.12f;

        private float _electricPulseTimer;
        private int _photosyntheticSymbiosisLevel;
        private int _toxicSymbiosisLevel;
        private int _armouredSymbiosisLevel;
        private int _regenerativeSymbiosisLevel;
        private int _electricSymbiosisLevel;

        // Eat-pulse feedback
        private float _eatPulseScaleMult = 1f;
        private Coroutine _eatPulseCoroutine;

        // Low-mass danger haptic
        private float _lowMassHapticTimer;
        private const float LowMassDangerThreshold = 1.0f;  // below this mass the player is in danger
        private const float LowMassHapticInterval  = 2.5f;
        private const float MinimumConsumeProbeRadius = 0.12f;
        private const float ConsumeProbeContactPadding = 0.04f;
        private Vector3 _lastAimDirection = Vector3.forward;
        private readonly Collider[] _consumeProbeHits = new Collider[64];

        private void Awake()
        {
            _activeInstance = this;
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            // Apply the shared balance multiplier if GameManager is ready.
            if (GameManager.HasInstance)
                _evolutionThreshold *= GameManager.Instance.Modifiers.EvolutionThresholdMult;

            _evolutionTracker = new EvolutionTracker(_evolutionThreshold, MaxEvolutions);
            _evolutionTracker.OnEvolutionReached += OnEvolutionReached;
            _playerGenome = Genome.Default();
            _electricPulseTimer = GetElectricPulseInterval(
                BalanceConstants.GeneticTraitElectricPulseInterval,
                _electricSymbiosisLevel);
        }

        private void Start()
        {
            CameraManager.Instance.SetFollowTarget(transform);
            _stageCache = FindAnyObjectByType<CellStage>();
            SpeciationSystem.EnsureSpeciesExists(0, _playerGenome);
            RefreshGenomeVisuals();
            UpdateVisualScale();
            CellSwimVisual.Ensure(gameObject, CellSwimVisual.SwimProfile.Player);
            _upgradePromptUnlockTime = Time.time + Mathf.Max(0f, _upgradePromptDelayAfterSpawnSeconds);
        }

        private void OnEnable()
        {
            _activeInstance = this;
        }

        private void Update()
        {
            if (_isEvolving || _isDead || _reproductionUIActive) return;
            UpdateTraitPassives(Time.deltaTime);
            UpdateLowMassHaptic(Time.deltaTime);
            HandleBoostInput();
        }

        private void OnDestroy()
        {
            if (_activeInstance == this)
                _activeInstance = null;

            if (_evolutionTracker != null)
                _evolutionTracker.OnEvolutionReached -= OnEvolutionReached;
        }

        private void OnDisable()
        {
            if (_activeInstance == this)
                _activeInstance = null;
        }

        private void RefreshGenomeVisuals()
        {
            Color displayColor = ProceduralCellGenerator.ApplyTraitTint(_playerGenome.Color, _playerGenome.Traits);
            ProceduralCellGenerator.ApplyGenomeVisuals(gameObject, _playerGenome);
            ProceduralCellGenerator.ApplyLoadoutVisuals(gameObject, _partLoadout, displayColor);
        }

        private void FixedUpdate()
        {
            if (_isEvolving || _isDead) return;

            ProbeNearbyConsumables();

            var moveDir = ResolveMoveDirection();

            float speed = _moveSpeed;
            if (_boostTimer > 0)
            {
                speed *= _boostMultiplier;
                _boostTimer -= Time.fixedDeltaTime;
            }

            speed *= CellFeelDirector.PlayerSpeedMultiplier;
            speed *= _partLoadout.SpeedMultiplier;
            speed *= GetGenomeSpeedMultiplier();

            float sizeSpeedPenalty = 1f / (1f + _currentMass * 0.17f);
            if (moveDir.sqrMagnitude > 0.0001f)
                _lastAimDirection = moveDir;

            Vector3 wobbleDir = moveDir.sqrMagnitude > 0.0001f
                ? Movement.ApplyWobble(moveDir, Time.time, _playerGenome)
                : Vector3.zero;
            Vector3 targetVelocity = wobbleDir * speed * sizeSpeedPenalty;
            targetVelocity += GetCurrentFlowVelocity();

            float accel = moveDir.sqrMagnitude > 0.0001f ? _acceleration : _deceleration;
            accel *= CellFeelDirector.PlayerAccelerationMultiplier;
            _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, targetVelocity, accel * Time.fixedDeltaTime);

            if (_lastAimDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_lastAimDirection, Vector3.up);
                float feelTurnSpeed = _turnSpeed * CellFeelDirector.PlayerTurnMultiplier;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, feelTurnSpeed * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Fallback consumable detection to handle fast movement / tiny trigger edge cases
        /// where OnTriggerEnter can be missed between physics steps.
        /// </summary>
        private void ProbeNearbyConsumables()
        {
            float probeRadius = ComputeConsumeProbeRadius(transform.lossyScale, GetComponent<SphereCollider>());
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                probeRadius,
                _consumeProbeHits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _consumeProbeHits[i];
                _consumeProbeHits[i] = null;
                if (hit == null) continue;
                if (!hit.enabled) continue;
                TryHandleTriggerContact(hit);
            }
        }

        public static float ComputeConsumeProbeRadius(Vector3 playerWorldScale, SphereCollider playerCollider)
        {
            float scaleRadius = Mathf.Max(Mathf.Abs(playerWorldScale.x), Mathf.Abs(playerWorldScale.z)) * 0.5f;
            float colliderRadius = 0f;

            if (playerCollider != null)
            {
                float colliderScale = Mathf.Max(
                    Mathf.Abs(playerCollider.transform.lossyScale.x),
                    Mathf.Abs(playerCollider.transform.lossyScale.z));
                colliderRadius = Mathf.Max(0f, playerCollider.radius) * colliderScale;
            }

            return Mathf.Max(MinimumConsumeProbeRadius, Mathf.Max(scaleRadius, colliderRadius)) + ConsumeProbeContactPadding;
        }

        private Vector3 GetCurrentFlowVelocity()
        {
            if (_currentFlowInfluence <= 0f) return Vector3.zero;
            if (_stageCache == null)
                _stageCache = FindAnyObjectByType<CellStage>();
            if (_stageCache == null) return Vector3.zero;
            if (!_stageCache.TryGetCurrentAtPlayer(out var direction, out var force)) return Vector3.zero;
            if (direction.sqrMagnitude < 0.0001f || force <= 0f) return Vector3.zero;
            return direction.normalized * force * _currentFlowInfluence;
        }

        private void HandleBoostInput()
        {
            bool triggerBoost = false;
            if (InputManager.HasInstance && InputManager.Instance.AttackPressed)
                triggerBoost = true;

            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame ||
                 keyboard.leftShiftKey.wasPressedThisFrame ||
                 keyboard.rightShiftKey.wasPressedThisFrame))
            {
                triggerBoost = true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                triggerBoost = true;

            if (triggerBoost)
                Boost();
        }

        private Vector3 ResolveMoveDirection()
        {
            Vector2 move = Vector2.zero;
            if (InputManager.HasInstance)
                move += InputManager.Instance.MoveInput;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            }

            if (move.sqrMagnitude > 1f) move.Normalize();
            if (move.sqrMagnitude > 0.0001f)
                return new Vector3(move.x, 0f, move.y).normalized;

            return TryGetMouseSteerDirection(out var mouseDir) ? mouseDir : Vector3.zero;
        }

        private bool TryGetMouseSteerDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            var mouse = Mouse.current;
            if (mouse == null) return false;

            bool steeringRequested = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
            if (!steeringRequested) return false;

            Camera cam = CameraManager.HasInstance ? CameraManager.Instance.MainCamera : Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float enter)) return false;

            Vector3 worldPoint = ray.GetPoint(enter);
            Vector3 toCursor = worldPoint - transform.position;
            toCursor.y = 0f;
            if (toCursor.sqrMagnitude < _mouseDeadZone * _mouseDeadZone) return false;

            direction = toCursor.normalized;
            return true;
        }

        private void UpdateTraitPassives(float deltaTime)
        {
            if (_playerGenome == null) return;

            if (_playerGenome.HasTrait(GeneticTraits.Photosynthetic))
                _currentMass += TraitPhotosyntheticMassRate
                                * GetTraitPassiveMultiplier(_photosyntheticSymbiosisLevel)
                                * CellEnvironmentEventManager.GlobalPhotosynthesisTraitMultiplier
                                * deltaTime;

            if (_playerGenome.HasTrait(GeneticTraits.Regenerative) && _currentMass > MinimumMass)
                _currentMass += TraitRegenerativeMassRate
                                * GetTraitPassiveMultiplier(_regenerativeSymbiosisLevel)
                                * deltaTime;

            if (_playerGenome.HasTrait(GeneticTraits.Electric))
            {
                _electricPulseTimer -= deltaTime;
                if (_electricPulseTimer <= 0f)
                {
                    ApplyPlayerElectricPulse();
                    _electricPulseTimer = GetElectricPulseInterval(
                        BalanceConstants.GeneticTraitElectricPulseInterval,
                        _electricSymbiosisLevel);
                }
            }
        }

        private void UpdateLowMassHaptic(float deltaTime)
        {
            _lowMassHapticTimer -= deltaTime;
            if (_currentMass < LowMassDangerThreshold && _lowMassHapticTimer <= 0f)
            {
                _lowMassHapticTimer = LowMassHapticInterval;
                // Pulse strength scales with how close to death: stronger when nearer MinimumMass
                float danger = 1f - Mathf.Clamp01((_currentMass - MinimumMass) / (LowMassDangerThreshold - MinimumMass));
                float intensity = Mathf.Lerp(0.2f, 0.7f, danger);
                InputManager.Instance?.RequestHaptic(intensity, intensity * 0.5f, 0.15f);
            }
        }

        private void ApplyPlayerElectricPulse()
        {
            var hits = Physics.OverlapSphere(transform.position,
                BalanceConstants.GeneticTraitElectricPulseRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                hit.GetComponent<Organism>()?.ApplyElectricSlow();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (TryHandleTriggerContact(other))
                return;

            if (_isDead || _playerGenome == null || !_playerGenome.HasTrait(GeneticTraits.Toxic)) return;
            var org = FindComponentOnCollider<Organism>(other);
            if (org != null && !org.IsDead)
                org.TakeDamage(BalanceConstants.GeneticTraitToxicContactDrain
                               * GetTraitPassiveMultiplier(_toxicSymbiosisLevel)
                               * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleTriggerContact(other);
        }

        private bool TryHandleTriggerContact(Collider other)
        {
            if (_isDead || other == null)
                return false;

            var food = FindComponentOnCollider<FoodItem>(other);
            if (food != null && food.isActiveAndEnabled)
            {
                Consume(food);
                return true;
            }

            var partPickup = FindComponentOnCollider<CellPartPickup>(other);
            if (partPickup != null && partPickup.isActiveAndEnabled)
            {
                ConsumePartPickup(partPickup);
                return true;
            }

            var npcCell = FindComponentOnCollider<CellAI>(other);
            if (npcCell != null)
            {
                float playerScale = transform.localScale.x;
                float targetScale = npcCell.transform.localScale.x;
                if (CanConsumeTarget(playerScale, targetScale, NpcCellConsumeSizeRatio))
                {
                    ConsumeCell(npcCell);
                }
                else if (ShouldDieFromFailedConsumeAttempt(playerScale, targetScale, NpcCellConsumeSizeRatio))
                {
                    TriggerDeath();
                }
                else if (_partLoadout.HasContactDamage)
                {
                    npcCell.ApplyDamage(SpikeContactDamage);
                }
                return true;
            }

            var organism = FindComponentOnCollider<Organism>(other);
            if (organism != null && !organism.IsDead)
            {
                float playerScale = transform.localScale.x;
                float targetScale = organism.transform.localScale.x;
                if (CanConsumeTarget(playerScale, targetScale, OrganismConsumeSizeRatio))
                {
                    ConsumeOrganism(organism);
                }
                else if (ShouldDieFromFailedConsumeAttempt(playerScale, targetScale, OrganismConsumeSizeRatio))
                {
                    TriggerDeath();
                }
                else if (_partLoadout.HasContactDamage)
                {
                    organism.TakeDamage(SpikeContactDamage * 6f);
                }
                return true;
            }

            return false;
        }

        private static T FindComponentOnCollider<T>(Collider other) where T : Component
        {
            if (other == null) return null;
            var direct = other.GetComponent<T>();
            if (direct != null) return direct;

            var attachedBody = other.attachedRigidbody;
            if (attachedBody != null)
            {
                var onBody = attachedBody.GetComponent<T>();
                if (onBody != null) return onBody;

                var onBodyParent = attachedBody.GetComponentInParent<T>();
                if (onBodyParent != null) return onBodyParent;

                var onBodyChild = attachedBody.GetComponentInChildren<T>();
                if (onBodyChild != null) return onBodyChild;
            }

            var inParent = other.GetComponentInParent<T>();
            if (inParent != null) return inParent;

            return other.GetComponentInChildren<T>();
        }

        private static bool CanConsumeTarget(float playerScale, float targetScale, float consumeSizeRatio)
            => targetScale < playerScale * consumeSizeRatio;

        /// <summary>
        /// Returns true when a failed consume attempt should kill the player:
        /// target is not consumable and is strictly larger than the player.
        /// </summary>
        public static bool ShouldDieFromFailedConsumeAttempt(float playerScale, float targetScale, float consumeSizeRatio)
        {
            if (CanConsumeTarget(playerScale, targetScale, consumeSizeRatio))
                return false;

            return targetScale > playerScale;
        }

        private void TrackPeakMass()
        {
            if (_currentMass > _peakMass) _peakMass = _currentMass;
        }

        private void ApplyConsumptionMassGain(float massGain)
        {
            _currentMass += Mathf.Max(MinimumConsumptionMassGain, massGain);
            TrackPeakMass();
        }

        private void Consume(FoodItem food)
        {
            if (food == null || !food.isActiveAndEnabled) return;

            FoodType foodType = food.Type;
            Vector3 foodPosition = food.transform.position;
            int foodManagerId = food.FoodManagerId;

            DisableTriggerSource(food);
            food.enabled = false;

            bool consumedAuthoritatively = false;
            if (foodManagerId != 0)
            {
                var fm = FindAnyObjectByType<FoodManager>();
                if (fm != null)
                    consumedAuthoritatively = fm.ConsumeFood(foodManagerId);

                // Fallback cleanup for stale/desynced entries so inert food cannot persist.
                if (!consumedAuthoritatively)
                    UnityObjectUtil.Destroy(food.gameObject);
            }
            else
            {
                consumedAuthoritatively = true;
                UnityObjectUtil.Destroy(food.gameObject);
            }

            if (!consumedAuthoritatively)
                return;

            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.FoodEat, foodPosition);
            ApplyConsumptionMassGain(_growthPerFood * GetDigestiveGainMultiplier(_playerGenome, _regenerativeSymbiosisLevel));
            _foodConsumedTotal++;
            _foodConsumedSinceUpgrade++;
            _dnaPointsCollected += BalanceConstants.CellDNAPointsPerFood;
            GameManager.Instance.AddDNAPoints(BalanceConstants.CellDNAPointsPerFood);
            _biasTracker.RecordFoodEaten(foodType);
            EventBus.Publish(new CellFoodConsumedEvent { FoodType = foodType });
            var eatSfx = foodType == FoodType.Meat  ? SFXType.FoodEatenMeat
                       : foodType == FoodType.Toxin ? SFXType.FoodEatenToxin
                       : SFXType.FoodEatenPlant;
            AudioManager.Instance?.PlaySFX(eatSfx);
            InputManager.Instance?.RequestHaptic(0.1f, 0.3f, 0.06f);
            if (RoobsWorld.Core.CameraManager.HasInstance)
                RoobsWorld.Core.CameraManager.Instance.RequestShake(0.12f, 0.1f);
            PlayEatPulse(foodType);
            UpdateVisualScale();

            CheckEvolution();
            TryApplyUpgradeAfterFood();
        }

        private void ConsumePartPickup(CellPartPickup pickup)
        {
            if (pickup == null || !pickup.isActiveAndEnabled) return;

            DisableTriggerSource(pickup);
            pickup.enabled = false;
            Vector3 pos = pickup.transform.position;
            bool unlockedNewPart = _partLoadout.TryUnlock(pickup.PartType);
            if (unlockedNewPart)
            {
                _dnaPointsCollected += pickup.DnaReward;
                GameManager.Instance.AddDNAPoints(pickup.DnaReward);
                RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DNAPickup, pos);
                AudioManager.Instance?.PlaySFX(SFXType.DNAGained);
                InputManager.Instance?.RequestHaptic(0.3f, 0.6f, 0.12f);
                RefreshGenomeVisuals();
                UpdateVisualScale();
            }

            EventBus.Publish(new CellPartCollectedEvent { PartType = pickup.PartType });
            UnityObjectUtil.Destroy(pickup.gameObject);
        }

        private static void DisableTriggerSource(Component source)
        {
            if (source == null) return;
            var colliders = source.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;
                col.enabled = false;
            }
        }

        private void ConsumeCell(CellAI cell)
        {
            Vector3 consumePosition = cell.transform.position;
            bool preyWasLarger = cell.transform.localScale.x > transform.localScale.x;
            ApplyConsumptionMassGain(cell.transform.localScale.x * 0.5f * GetDigestiveGainMultiplier(_playerGenome, _regenerativeSymbiosisLevel));
            _dnaPointsCollected += BalanceConstants.CellDNAPointsPerCellConsume;
            GameManager.Instance.AddDNAPoints(BalanceConstants.CellDNAPointsPerCellConsume);
            _biasTracker.RecordCellConsumed();
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.FoodEat, consumePosition);
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DamageHit, consumePosition);
            AudioManager.Instance?.PlaySFX(SFXType.AttackHit);
            InputManager.Instance?.RequestHaptic(0.2f, 0.5f, 0.08f);
            UpdateVisualScale();
            UnityObjectUtil.Destroy(cell.gameObject);
            EventBus.Publish(new CellAteLargerPreyEvent { PreyWasLarger = preyWasLarger });
        }

        private void ConsumeOrganism(Organism organism)
        {
            if (organism == null || organism.IsDead) return;

            Vector3 consumePosition = organism.transform.position;
            bool preyWasLarger = organism.transform.localScale.x > transform.localScale.x;
            ApplyConsumptionMassGain(organism.transform.localScale.x * OrganismConsumeMassFactor * GetDigestiveGainMultiplier(_playerGenome, _regenerativeSymbiosisLevel));
            _dnaPointsCollected += BalanceConstants.CellDNAPointsPerCellConsume;
            GameManager.Instance.AddDNAPoints(BalanceConstants.CellDNAPointsPerCellConsume);
            _biasTracker.RecordCellConsumed();
            TryAbsorbSymbiont(organism.Genome);
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.FoodEat, consumePosition);
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DamageHit, consumePosition);
            AudioManager.Instance?.PlaySFX(SFXType.AttackHit);
            InputManager.Instance?.RequestHaptic(0.22f, 0.55f, 0.09f);
            UpdateVisualScale();
            organism.Die();
            EventBus.Publish(new CellAteLargerPreyEvent { PreyWasLarger = preyWasLarger });
        }

        private void TryAbsorbSymbiont(Genome preyGenome)
        {
            if (preyGenome == null || _playerGenome == null) return;
            if (UnityEngine.Random.value > EndosymbiosisTraitTransferChance) return;

            GeneticTraits transferable = BuildEndosymbiosisTransferPool(preyGenome, _playerGenome.Traits);
            GeneticTraits preferred = DetermineSymbiosisPreferredTrait(preyGenome);

            GeneticTraits chosen = GeneticTraits.None;
            if (transferable != GeneticTraits.None)
            {
                var randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
                chosen = SelectTraitForEndosymbiosis(transferable, preferred, new System.Random(randomSeed));
            }
            else if (preferred != GeneticTraits.None &&
                     (_playerGenome.Traits & preferred) != 0 &&
                     CanUpgradeSymbiosisLevel(GetSymbiosisLevel(preferred)))
            {
                // Already-owned organelle can still be reinforced by consuming matching microbes.
                chosen = preferred;
            }

            if (chosen == GeneticTraits.None) return;

            bool ownedBefore = (_playerGenome.Traits & chosen) != 0;
            _playerGenome.Traits |= chosen;
            TryUpgradeSymbiosisLevel(chosen);
            RoobsWorld.Core.GameManager.Instance?.RecordTraitDiscovery(chosen);
            RefreshGenomeVisuals();
            UpdateVisualScale();
            string tipKey = ownedBefore ? "cell.endosymbiosis.upgraded" : GetEndosymbiosisTipKey(chosen);
            RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip(tipKey, 4f);
        }

        /// <summary>
        /// Builds the trait pool eligible for endosymbiosis transfer.
        /// Includes explicit prey traits not already owned by the player, and may inject one
        /// heuristic preferred trait for traitless prey archetypes.
        /// </summary>
        public static GeneticTraits BuildEndosymbiosisTransferPool(Genome preyGenome, GeneticTraits playerTraits)
        {
            if (preyGenome == null) return GeneticTraits.None;
            const GeneticTraits Allowed =
                GeneticTraits.Photosynthetic | GeneticTraits.Toxic | GeneticTraits.Armoured |
                GeneticTraits.Regenerative | GeneticTraits.Electric;

            GeneticTraits transferable = (preyGenome.Traits & ~playerTraits) & Allowed;
            GeneticTraits preferred = DetermineSymbiosisPreferredTrait(preyGenome);
            if (preferred != GeneticTraits.None && (playerTraits & preferred) == 0)
                transferable |= preferred;

            return transferable & Allowed;
        }

        /// <summary>
        /// Returns nutrient-conversion multiplier from active symbiotic traits.
        /// Regenerative symbiosis slightly improves conversion of consumed matter into mass.
        /// </summary>
        public static float GetDigestiveGainMultiplier(Genome genome)
            => GetDigestiveGainMultiplier(genome, 0);

        /// <summary>
        /// Returns nutrient-conversion multiplier from active symbiotic traits and regeneration level.
        /// </summary>
        public static float GetDigestiveGainMultiplier(Genome genome, int regenerativeLevel)
        {
            if (genome == null) return 1f;
            float multiplier = genome.HasTrait(GeneticTraits.Regenerative)
                ? TraitRegenerativeDigestionBonusMultiplier
                : 1f;
            return multiplier * (1f + Mathf.Max(0, regenerativeLevel) * TraitRegenerativeDigestionBonusPerLevel);
        }

        /// <summary>Returns true when the current symbiosis level can still be upgraded.</summary>
        public static bool CanUpgradeSymbiosisLevel(int currentLevel)
            => currentLevel < MaxSymbiosisLevel;

        /// <summary>Increments symbiosis level by one and clamps to the max supported level.</summary>
        public static int UpgradeSymbiosisLevel(int currentLevel)
            => Mathf.Clamp(currentLevel + 1, 0, MaxSymbiosisLevel);

        /// <summary>Trait passive scalar based on symbiosis reinforcement level.</summary>
        public static float GetTraitPassiveMultiplier(int symbiosisLevel)
            => 1f + Mathf.Max(0, symbiosisLevel) * TraitPassiveSymbiosisBonusPerLevel;

        /// <summary>Computes armoured damage reduction including symbiosis reinforcement.</summary>
        public static float GetArmouredDamageReduction(float baseReduction, int armouredLevel)
            => Mathf.Clamp(baseReduction + Mathf.Max(0, armouredLevel) * TraitArmouredReductionBonusPerLevel, 0f, 0.85f);

        /// <summary>Computes electric pulse cooldown with symbiosis reinforcement.</summary>
        public static float GetElectricPulseInterval(float baseInterval, int electricLevel)
        {
            float scalar = 1f - Mathf.Max(0, electricLevel) * TraitElectricIntervalReductionPerLevel;
            return Mathf.Max(0.35f, baseInterval * scalar);
        }

        /// <summary>
        /// Calculates the player's visible scale from mass growth and optional genome size.
        /// </summary>
        public static float ComputeVisualScale(float currentMass, Genome genome)
        {
            float clampedMass = Mathf.Max(0f, currentMass);
            float massOnlyScale = BaseVisualScale + clampedMass * MassVisualScaleFactor;
            if (genome == null)
                return massOnlyScale;

            float genomeScale = BaseVisualScale + Mathf.Max(0f, genome.Size) * GenomeSizeVisualScaleFactor;
            float massGrowthDelta = (clampedMass - 1f) * MassVisualScaleFactor;
            return Mathf.Max(0.05f, genomeScale + massGrowthDelta);
        }

        /// <summary>
        /// Picks a single trait flag from a bitmask of available traits for endosymbiosis transfer.
        /// Exposed as pure logic for deterministic EditMode tests.
        /// </summary>
        public static GeneticTraits SelectTraitForEndosymbiosis(GeneticTraits available, System.Random rng = null)
            => SelectTraitForEndosymbiosis(available, GeneticTraits.None, rng);

        /// <summary>
        /// Picks a single trait from a bitmask, with optional bias toward a prey-dominant preferred trait.
        /// </summary>
        public static GeneticTraits SelectTraitForEndosymbiosis(GeneticTraits available, GeneticTraits preferred, System.Random rng = null)
        {
            if (available == GeneticTraits.None) return GeneticTraits.None;
            var options = new System.Collections.Generic.List<GeneticTraits>(5);
            if ((available & GeneticTraits.Photosynthetic) != 0) options.Add(GeneticTraits.Photosynthetic);
            if ((available & GeneticTraits.Toxic) != 0) options.Add(GeneticTraits.Toxic);
            if ((available & GeneticTraits.Armoured) != 0) options.Add(GeneticTraits.Armoured);
            if ((available & GeneticTraits.Regenerative) != 0) options.Add(GeneticTraits.Regenerative);
            if ((available & GeneticTraits.Electric) != 0) options.Add(GeneticTraits.Electric);
            if (options.Count == 0) return GeneticTraits.None;

            rng ??= new System.Random();
            if (preferred != GeneticTraits.None &&
                (available & preferred) != 0 &&
                rng.NextDouble() < EndosymbiosisPreferredTraitBiasChance)
            {
                return preferred;
            }

            return options[rng.Next(0, options.Count)];
        }

        /// <summary>
        /// Infers the dominant trait a prey organism is most likely to transfer on successful endosymbiosis.
        /// Explicit prey traits are preferred; otherwise simple genome heuristics are used.
        /// </summary>
        public static GeneticTraits DetermineSymbiosisPreferredTrait(Genome preyGenome)
        {
            if (preyGenome == null) return GeneticTraits.None;

            GeneticTraits transferableTraits =
                preyGenome.Traits &
                (GeneticTraits.Photosynthetic | GeneticTraits.Toxic | GeneticTraits.Armoured |
                 GeneticTraits.Regenerative | GeneticTraits.Electric);

            if ((transferableTraits & GeneticTraits.Photosynthetic) != 0) return GeneticTraits.Photosynthetic;
            if ((transferableTraits & GeneticTraits.Toxic) != 0) return GeneticTraits.Toxic;
            if ((transferableTraits & GeneticTraits.Armoured) != 0) return GeneticTraits.Armoured;
            if ((transferableTraits & GeneticTraits.Regenerative) != 0) return GeneticTraits.Regenerative;
            if ((transferableTraits & GeneticTraits.Electric) != 0) return GeneticTraits.Electric;

            bool bacteriaLike =
                preyGenome.Size <= 0.55f &&
                preyGenome.Flagella >= 5f &&
                preyGenome.Attack <= 1.2f;
            if (bacteriaLike) return GeneticTraits.Regenerative;

            if (preyGenome.Digestion >= 6f) return GeneticTraits.Photosynthetic;
            if (preyGenome.Spikes >= 3.5f || preyGenome.Attack >= 6f) return GeneticTraits.Toxic;
            if (preyGenome.Defence >= 6f || preyGenome.Size >= 6.5f) return GeneticTraits.Armoured;
            if (preyGenome.MetabolismCost <= 0.45f || (preyGenome.Digestion >= 5f && preyGenome.Attack <= 2f))
                return GeneticTraits.Regenerative;
            if (preyGenome.Speed >= 6f || preyGenome.Flagella >= 6f) return GeneticTraits.Electric;

            return GeneticTraits.None;
        }

        /// <summary>
        /// Returns the localization key used when an endosymbiosis trait is absorbed.
        /// </summary>
        public static string GetEndosymbiosisTipKey(GeneticTraits absorbedTrait)
        {
            if ((absorbedTrait & GeneticTraits.Photosynthetic) != 0) return "cell.endosymbiosis.acquired.photosynthetic";
            if ((absorbedTrait & GeneticTraits.Toxic) != 0) return "cell.endosymbiosis.acquired.toxic";
            if ((absorbedTrait & GeneticTraits.Armoured) != 0) return "cell.endosymbiosis.acquired.armoured";
            if ((absorbedTrait & GeneticTraits.Regenerative) != 0) return "cell.endosymbiosis.acquired.regenerative";
            if ((absorbedTrait & GeneticTraits.Electric) != 0) return "cell.endosymbiosis.acquired.electric";
            return "cell.endosymbiosis.acquired";
        }

        private bool TryUpgradeSymbiosisLevel(GeneticTraits trait)
        {
            if ((trait & GeneticTraits.Photosynthetic) != 0 && CanUpgradeSymbiosisLevel(_photosyntheticSymbiosisLevel))
            {
                _photosyntheticSymbiosisLevel = UpgradeSymbiosisLevel(_photosyntheticSymbiosisLevel);
                return true;
            }
            if ((trait & GeneticTraits.Toxic) != 0 && CanUpgradeSymbiosisLevel(_toxicSymbiosisLevel))
            {
                _toxicSymbiosisLevel = UpgradeSymbiosisLevel(_toxicSymbiosisLevel);
                return true;
            }
            if ((trait & GeneticTraits.Armoured) != 0 && CanUpgradeSymbiosisLevel(_armouredSymbiosisLevel))
            {
                _armouredSymbiosisLevel = UpgradeSymbiosisLevel(_armouredSymbiosisLevel);
                return true;
            }
            if ((trait & GeneticTraits.Regenerative) != 0 && CanUpgradeSymbiosisLevel(_regenerativeSymbiosisLevel))
            {
                _regenerativeSymbiosisLevel = UpgradeSymbiosisLevel(_regenerativeSymbiosisLevel);
                return true;
            }
            if ((trait & GeneticTraits.Electric) != 0 && CanUpgradeSymbiosisLevel(_electricSymbiosisLevel))
            {
                _electricSymbiosisLevel = UpgradeSymbiosisLevel(_electricSymbiosisLevel);
                return true;
            }

            return false;
        }

        public int GetSymbiosisLevel(GeneticTraits trait)
        {
            if ((trait & GeneticTraits.Photosynthetic) != 0) return _photosyntheticSymbiosisLevel;
            if ((trait & GeneticTraits.Toxic) != 0) return _toxicSymbiosisLevel;
            if ((trait & GeneticTraits.Armoured) != 0) return _armouredSymbiosisLevel;
            if ((trait & GeneticTraits.Regenerative) != 0) return _regenerativeSymbiosisLevel;
            if ((trait & GeneticTraits.Electric) != 0) return _electricSymbiosisLevel;
            return 0;
        }

        private void UpdateVisualScale()
        {
            var mr = GetComponent<MeshRenderer>();
            float computedBaseScale = ComputeVisualScale(_currentMass, _playerGenome);
            _maxUnlockedBaseScale = Mathf.Max(_maxUnlockedBaseScale, computedBaseScale);
            float clampedBaseScale = Mathf.Max(computedBaseScale, _maxUnlockedBaseScale);
            transform.localScale = Vector3.one
                * clampedBaseScale
                * BalanceConstants.CellEntityVisualScaleMultiplier
                * _eatPulseScaleMult;
            if (_playerGenome != null)
            {
                Color displayColor = ProceduralCellGenerator.ApplyTraitTint(_playerGenome.Color, _playerGenome.Traits);
                displayColor = ProceduralTextureFactory.Opaque(displayColor);
                float massNorm = NormalizedMass;
                float intensity = Mathf.Lerp(0.1f, 0.7f, massNorm);
                if (mr != null && mr.material != null)
                {
                    mr.material.color = displayColor;
                    if (mr.material.HasProperty("_Color"))
                        mr.material.SetColor("_Color", displayColor);
                    if (mr.material.HasProperty("_BaseColor"))
                        mr.material.SetColor("_BaseColor", displayColor);
                    mr.material.EnableKeyword("_EMISSION");
                    if (HighContrastOutlineManager.IsActive)
                    {
                        // High-contrast: override with bright cyan so the player is easy to spot.
                        mr.material.SetColor("_EmissionColor", HighContrastOutlineManager.PlayerHighlightColor);
                    }
                    else
                    {
                        // Bioluminescence: emission scales with mass progress through stage.
                        mr.material.SetColor("_EmissionColor", displayColor * intensity);
                    }
                }

                // Force child materials directly instead of property blocks to avoid stale tint blocks.
                var renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null || renderer == mr)
                        continue;

                    var mat = renderer.material;
                    if (mat == null)
                        continue;

                    Color tint = HighContrastOutlineManager.IsActive
                        ? HighContrastOutlineManager.PlayerHighlightColor
                        : displayColor;
                    tint = ProceduralTextureFactory.Opaque(tint);

                    mat.color = tint;
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", tint);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", tint);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", HighContrastOutlineManager.IsActive ? tint * 1.2f : tint * intensity);
                    }
                }
            }
        }

        /// <summary>
        /// Plays a brief scale-punch animation scaled to the food type.
        /// Plant = subtle (1.15×), Meat = moderate (1.20×), Toxin = strong (1.30×).
        /// </summary>
        private void PlayEatPulse(FoodType type)
        {
            float peakMult = type == FoodType.Toxin ? 1.30f
                           : type == FoodType.Meat  ? 1.20f
                           : 1.15f;
            if (_eatPulseCoroutine != null) StopCoroutine(_eatPulseCoroutine);
            _eatPulseCoroutine = StartCoroutine(EatPulseRoutine(peakMult));
        }

        private System.Collections.IEnumerator EatPulseRoutine(float peakMult)
        {
            const float expandTime  = 0.07f;
            const float contractTime = 0.12f;
            float t = 0f;
            while (t < expandTime)
            {
                _eatPulseScaleMult = Mathf.Lerp(1f, peakMult, t / expandTime);
                UpdateVisualScale();
                t += Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < contractTime)
            {
                _eatPulseScaleMult = Mathf.Lerp(peakMult, 1f, t / contractTime);
                UpdateVisualScale();
                t += Time.deltaTime;
                yield return null;
            }
            _eatPulseScaleMult = 1f;
            UpdateVisualScale();
            _eatPulseCoroutine = null;
        }

        /// <summary>Called by CellStage when the player escapes a predator threat radius.</summary>
        public void NotifyPredatorEscaped() => _biasTracker.RecordPredatorEscaped();

        /// <summary>
        /// Called by CellStage on a RadiationSurge event. Offers the player one bonus biased mutation
        /// choice without triggering reproduction (no mass cost, no offspring spawned).
        /// No-ops if the player is already dead, evolving, or already choosing a mutation.
        /// </summary>
        public void OfferRadiationMutation()
        {
            if (_isDead || _isEvolving || _reproductionUIActive) return;
            _reproductionUIActive = true;
            var bias = _biasTracker.ComputeBias();
            var discovered = RoobsWorld.Core.GameManager.HasInstance
                ? RoobsWorld.Core.GameManager.Instance.DiscoveredTraits
                : GeneticTraits.None;
            var radiationVariant = MutationSystem.MutateRecommendedWithDiscoveries(_playerGenome, bias, discovered);
            string subtitle = Localization.Get("cell.env.radiation_surge_subtitle");
            MutationChoiceUI.Show(radiationVariant, OnRadiationMutationChoice, subtitle);
        }

        private void OnRadiationMutationChoice(Genome chosen)
        {
            if (chosen != null)
            {
                RoobsWorld.Core.GameManager.Instance?.RecordTraitDiscovery(chosen.Traits);
                int speciesId = SpeciationSystem.GetSpeciesIdForOffspring(0, _playerGenome, chosen);
                _playerGenome = chosen;
                RefreshGenomeVisuals();
                UpdateVisualScale();
                if (speciesId != 0) SpeciationSystem.RegisterOrganism(speciesId);
            }
            _reproductionUIActive = false;
        }

        private void OnUpgradeChoice(Genome chosen)
        {
            if (chosen == null) { _reproductionUIActive = false; return; }
            Genome previousGenome = _playerGenome;
            Genome upgradedGenome = EnsureUpgradeImprovesCoreStats(chosen, previousGenome);
            _biasTracker.Reset();
            RoobsWorld.Core.GameManager.Instance?.RecordTraitDiscovery(upgradedGenome.Traits);
            int speciesId = SpeciationSystem.GetSpeciesIdForOffspring(0, previousGenome, upgradedGenome);
            _playerGenome = upgradedGenome;
            RefreshGenomeVisuals();
            UpdateVisualScale();
            _currentMass = Mathf.Max(MinimumMass + 0.1f, _currentMass - BalanceConstants.CellPlayerReproductionMassCost);
            _foodConsumedSinceUpgrade = 0;
            if (speciesId != 0) SpeciationSystem.RegisterOrganism(speciesId);
            _reproductionUIActive = false;
            EventBus.Publish(new CellPlayerReproducedEvent());
        }

        public static bool HasConsumedEnoughFoodForUpgrade(int consumedFoodCount, int requiredFoodCount)
            => consumedFoodCount >= Mathf.Max(1, requiredFoodCount);

        private void TryApplyUpgradeAfterFood()
        {
            if (_isEvolving || _isDead || _reproductionUIActive) return;
            if (Time.time < _upgradePromptUnlockTime) return;
            if (_currentMass < BalanceConstants.CellPlayerReproductionMassThreshold) return;
            if (!HasConsumedEnoughFoodForUpgrade(_foodConsumedSinceUpgrade, _foodConsumedPerUpgrade)) return;

            var bias = _biasTracker.ComputeBias();
            var discovered = RoobsWorld.Core.GameManager.HasInstance
                ? RoobsWorld.Core.GameManager.Instance.DiscoveredTraits
                : GeneticTraits.None;
            var variant = MutationSystem.MutateRecommendedWithDiscoveries(_playerGenome, bias, discovered);
            ApplyAutomaticUpgrade(variant);
        }

        private void ApplyAutomaticUpgrade(Genome chosen)
        {
            if (chosen == null) return;

            Genome previousGenome = _playerGenome;
            Genome upgradedGenome = EnsureUpgradeImprovesCoreStats(chosen, previousGenome);
            _biasTracker.Reset();
            RoobsWorld.Core.GameManager.Instance?.RecordTraitDiscovery(upgradedGenome.Traits);
            int speciesId = SpeciationSystem.GetSpeciesIdForOffspring(0, previousGenome, upgradedGenome);
            _playerGenome = upgradedGenome;
            RefreshGenomeVisuals();
            UpdateVisualScale();
            _currentMass = Mathf.Max(MinimumMass + 0.1f, _currentMass - BalanceConstants.CellPlayerReproductionMassCost);
            _foodConsumedSinceUpgrade = 0;
            if (speciesId != 0) SpeciationSystem.RegisterOrganism(speciesId);

            AudioManager.Instance?.PlaySFX(SFXType.LevelUp);
            EventBus.Publish(new PlayerCharacterImprovedEvent
            {
                Title = Localization.Get("character.improved"),
                Description = Localization.Get("character.improved.cell"),
                StatSummary = string.Format(
                    Localization.Get("character.improved.cell_stats"),
                    UpgradeSizeBonus,
                    UpgradeSpeedBonus)
            });
            EventBus.Publish(new CellPlayerReproducedEvent());
        }

        private static Genome EnsureUpgradeImprovesCoreStats(Genome chosen, Genome previousGenome)
        {
            if (chosen == null) return previousGenome;
            if (previousGenome == null) return chosen;

            var upgraded = chosen.Clone();
            upgraded.Size = Mathf.Clamp(Mathf.Max(upgraded.Size, previousGenome.Size + UpgradeSizeBonus), 0f, 10f);
            upgraded.Speed = Mathf.Clamp(Mathf.Max(upgraded.Speed, previousGenome.Speed + UpgradeSpeedBonus), 0f, 10f);
            return upgraded;
        }

        private float GetGenomeSpeedMultiplier()
        {
            float genomeSpeed = _playerGenome != null ? _playerGenome.Speed : 1f;
            float t = Mathf.Clamp01(genomeSpeed / 10f);
            return Mathf.Lerp(MinGenomeSpeedMultiplier, MaxGenomeSpeedMultiplier, t);
        }

        private void CheckEvolution()
        {
            _evolutionTracker.UpdateMass(_currentMass, _foodConsumedTotal, _foodConsumedPerUpgrade);
        }

        private void OnEvolutionReached(int level)
        {
            _isEvolving = true;
            _rb.linearVelocity = Vector3.zero;
            AudioManager.Instance?.PlaySFX(SFXType.EvolutionComplete);
            InputManager.Instance?.RequestHaptic(0.5f, 1f, 0.3f);
            if (RoobsWorld.Core.CameraManager.HasInstance)
                RoobsWorld.Core.CameraManager.Instance.RequestShake(0.25f, 0.2f);

            EventBus.Publish(new CellEvolutionTriggeredEvent
            {
                EvolutionLevel = level,
                CurrentMass = _currentMass
            });

            EventBus.Publish(new CreatureEvolvedEvent
            {
                CreatureId = "player_cell",
                EvolutionLevel = level
            });
        }

        public void CompleteEvolution()
        {
            _isEvolving = false;

            if (_evolutionTracker.IsFinalEvolution)
            {
                EventBus.Publish(new CellEvolutionCompleteEvent
                {
                    EvolutionLevel = _evolutionTracker.CurrentLevel
                });
            }
        }

        public void Boost()
        {
            _boostTimer = _boostDuration * CellFeelDirector.PlayerBoostDurationMultiplier;
        }

        public void TakeDamage(float massDrain)
        {
            if (_isDead || _isEvolving) return;

            if (_playerGenome != null && _playerGenome.HasTrait(GeneticTraits.Armoured))
                massDrain *= 1f - GetArmouredDamageReduction(BalanceConstants.GeneticTraitArmouredDamageReduction, _armouredSymbiosisLevel);

            _biasTracker.RecordDamageTaken();
            _currentMass -= massDrain;
            if (_currentMass < MinimumMass)
            {
                TriggerDeath();
                return;
            }

            AudioManager.Instance?.PlaySFX(SFXType.ShieldHit);
            InputManager.Instance?.RequestHaptic(0.6f, 0.4f, 0.12f);
            if (RoobsWorld.Core.CameraManager.HasInstance)
                RoobsWorld.Core.CameraManager.Instance.RequestShake(0.18f, 0.12f);
            UpdateVisualScale();
        }

        private void TriggerDeath()
        {
            if (_isDead) return;

            _currentMass = 0f;
            _isDead = true;
            if (_rb != null)
                _rb.linearVelocity = Vector3.zero;
            AudioManager.Instance?.PlaySFX(SFXType.CreatureDeath);
            InputManager.Instance?.RequestHaptic(1f, 1f, 0.4f);

            EventBus.Publish(new CreatureDiedEvent
            {
                CreatureObject = gameObject,
                CreatureId = "player_cell",
                IsPlayer = true
            });
        }
    }
}
