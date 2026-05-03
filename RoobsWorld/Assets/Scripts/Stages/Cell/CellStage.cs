using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using RoobsWorld.UI;
using RoobsWorld.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Implements the Cell stage gameplay loop: arena setup, player cell, NPC cells, food, pickups, and hazards.
    /// </summary>
    public class CellStage : MonoBehaviour, IGameStage
    {
        [Header("Stage Settings")]
        [SerializeField] private Vector3 _arenaCenter = Vector3.zero;
        [SerializeField] private float _arenaRadius = 50f;
        [SerializeField] private int _initialNPCCount = 8;
        [SerializeField] private int _initialOrganismCount = 9;
        [SerializeField] private int _initialPartPickupCount = 8;

        [Header("Hazards")]
        [SerializeField] private int _predatorCount = 2;
        [SerializeField] private int _toxinCloudCount = 5;
        [SerializeField] private int _currentZoneCount = 3;
        [SerializeField] private int _whirlpoolCount = 2;

        private bool _isActive;
        private PlayerCell _player;
        private FoodManager _foodManager;
        private CellSpatialGrid _spatialGrid;
        private OrganismPool _organismPool;
        private CellEnvironmentEventManager _envEventManager;
        private CellTutorialController _tutorialController;
        private HighContrastOutlineManager _highContrastManager;
        private CellArenaDebris _arenaDebris;
        private int _timeScaleIndex;
        private CellGoal? _currentGoal;
        private bool _playerWasThreatenedByPredator;
        private HostAuthorityController _authority;
        private NetworkTickManager _networkTick;
        private readonly List<ArenaCurrent> _arenaCurrentsCache = new List<ArenaCurrent>(8);
        private readonly List<PredatorCell> _predatorCache = new List<PredatorCell>(24);
        private readonly List<Organism> _organismCache = new List<Organism>(64);
        private float _nextArenaCurrentsRefreshTime;

        [Header("Niche Filling")]
        [SerializeField] private float _nicheFillCheckInterval = 30f;
        [SerializeField] private int _minOrganismCount = 8;
        private float _nicheFillTimer;
        private float _dynamicFoodTimer;
        private float _dynamicPredatorTimer;

        // ---- Leviathan ----
        private float _leviathanStageTimer;      // time elapsed since stage start
        private float _leviathanRespawnTimer;    // countdown after a leviathan is gone
        private bool  _leviathanActive;
        private LeviathanCell _activeLeviathanCell;
        private static Material s_wakeTrailMaterial;
        private static readonly float[] s_defaultTimeScaleOptions = { 1f };
        private const float ArenaCurrentCacheRefreshInterval = 0.45f;
        private const float PartPickupSpawnMinRadiusFraction = 0.12f;
        private const float PartPickupSpawnMaxRadiusFraction = 0.82f;
        private const float PartPickupMinSpacing = 6f;
        private const int PartPickupSpawnAttempts = 16;
        private const float EntitySpawnMinSpacing = 5.5f;
        private const int EntitySpawnPositionAttempts = 28;

        /// <summary>Current micro goal for the HUD (e.g. eat N algae). Null when none active.</summary>
        public CellGoal? CurrentGoal => _currentGoal;
        public GameStageType StageType => GameStageType.Cell;
        public FoodManager FoodManager => _foodManager;
        /// <summary>Arena center for biome zone lookup (Step 6).</summary>
        public Vector3 ArenaCenter => _arenaCenter;
        /// <summary>Organism pool for AI reproduction (returns null outside of active stage).</summary>
        public OrganismPool GetOrganismPool() => _organismPool;
        /// <summary>Active environment event manager; null outside of active stage.</summary>
        public CellEnvironmentEventManager EnvironmentEvents => _envEventManager;
        /// <summary>Arena radius for biome zone lookup and spawn bounds.</summary>
        public float ArenaRadius => _arenaRadius;

        /// <summary>Returns the biome at the given world position. Used for environmental modifiers (Step 6).</summary>
        public Biome.BiomeType GetBiomeAt(Vector3 position)
        {
            return Biome.GetBiomeAt(_arenaCenter, position, _arenaRadius);
        }

        /// <summary>If the player is inside an ArenaCurrent trigger, returns its direction and force; otherwise false.</summary>
        public bool TryGetCurrentAtPlayer(out Vector3 direction, out float force)
        {
            direction = Vector3.zero;
            force = 0f;
            if (_player == null) return false;
            RefreshArenaCurrentsCache();

            Vector3 pos = _player.transform.position;
            bool sawDestroyedReference = false;
            for (int i = 0; i < _arenaCurrentsCache.Count; i++)
            {
                var current = _arenaCurrentsCache[i];
                if (current == null)
                {
                    sawDestroyedReference = true;
                    continue;
                }
                var col = current.GetComponent<Collider>();
                if (col != null && col.bounds.Contains(pos))
                {
                    direction = current.FlowDirection;
                    force = current.Force;
                    return true;
                }
                if (col == null)
                    sawDestroyedReference = true;
            }

            if (sawDestroyedReference)
                _nextArenaCurrentsRefreshTime = 0f;

            return false;
        }

        private void Start()
        {
            StageManager.Instance.RegisterStage(this);
            _foodManager = GetComponent<FoodManager>();
            if (_foodManager == null)
                _foodManager = gameObject.AddComponent<FoodManager>();
        }

        private static float[] GetDocumentaryTimeScaleOptions()
        {
            var options = BalanceConstants.CellDocumentaryTimeScaleOptions;
            return options != null && options.Length > 0 ? options : s_defaultTimeScaleOptions;
        }

        public void OnEnter()
        {
            _isActive = true;
            CellFeelDirector.SetProfile(CellFeelProfile.Classic);
            ApplyCellOnlyPreset();
            var timeScaleOptions = GetDocumentaryTimeScaleOptions();
            _timeScaleIndex = 0;
            Time.timeScale = timeScaleOptions[0];
            SpeciationSystem.Reset();
            _authority = new HostAuthorityController(GameManager.HasInstance ? GameManager.Instance.Session : null);
            Debug.Log("[CellStage] Entering Cell Stage");
            CameraManager.Instance.ActivateRig(CameraRigType.Orthographic2D);
            _envEventManager = gameObject.GetComponent<CellEnvironmentEventManager>()
                ?? gameObject.AddComponent<CellEnvironmentEventManager>();
            SpawnArena();
            _spatialGrid = new CellSpatialGrid(_arenaCenter, _arenaRadius, BalanceConstants.CellSpatialGridCellSize);
            _foodManager.SetSpatialGrid(_spatialGrid);
            int npcOrg = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.ApplyNPCCount(_initialOrganismCount)
                : _initialOrganismCount;
            _organismPool = new OrganismPool(prewarm: npcOrg, maxPooled: npcOrg * 3);
            SpawnPlayer();
            SpawnFood();
            SpawnNPCs();
            SpawnOrganisms();
            SpawnPartPickups();
            SpawnHazards();

            _envEventManager.Activate(_foodManager, _arenaRadius, _player, _authority, _arenaCenter);

            // Multiplayer tick — activates only when a session is connected; no-op offline.
            _networkTick = gameObject.GetComponent<NetworkTickManager>()
                ?? gameObject.AddComponent<NetworkTickManager>();
            var session   = GameManager.HasInstance ? GameManager.Instance.Session : null;
            var transport = GameManager.HasInstance ? GameManager.Instance.SessionTransport : null;
            _networkTick.Activate(session, transport, _player);
            _tutorialController = gameObject.GetComponent<CellTutorialController>()
                ?? gameObject.AddComponent<CellTutorialController>();
            _tutorialController.Activate(_player);
            _highContrastManager = gameObject.GetComponent<HighContrastOutlineManager>()
                ?? gameObject.AddComponent<HighContrastOutlineManager>();
            _highContrastManager.enabled = true;
            _nicheFillTimer = _nicheFillCheckInterval;
            _dynamicFoodTimer = BalanceConstants.CellDynamicFoodCheckIntervalSeconds;
            _dynamicPredatorTimer = BalanceConstants.CellDynamicPredatorReinforcementIntervalSeconds;
            SetCurrentGoal(CellGoal.CreateEatFoodGoal(FoodType.Plant, 3));
            ShowGoalLinkedTutorialTip();
            EventBus.Subscribe<CellEvolutionTriggeredEvent>(OnEvolutionTriggered);
            EventBus.Subscribe<CellEvolutionCompleteEvent>(OnFinalEvolutionComplete);
            EventBus.Subscribe<CellOrganismDiedEvent>(OnCellOrganismDied);
            EventBus.Subscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Subscribe<CellPlayerReproducedEvent>(OnPlayerReproduced);
            EventBus.Subscribe<CellEnvironmentEventTriggered>(OnEnvironmentEvent);
            EventBus.Subscribe<CellAteLargerPreyEvent>(OnAteLargerPrey);
            EventBus.Subscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
            EventBus.Subscribe<CellPartCollectedEvent>(OnPartCollected);
            _leviathanStageTimer = 0f;
            _leviathanRespawnTimer = BalanceConstants.LeviathanMinSpawnSeconds;
            _leviathanActive = false;
            _nextArenaCurrentsRefreshTime = 0f;
            RefreshArenaCurrentsCache(force: true);
        }

        /// <summary>
        /// Enforces a Spore-like cell sandbox baseline regardless of scene/prefab inspector drift.
        /// </summary>
        private void ApplyCellOnlyPreset()
        {
            // Dense ecosystem of small actors with limited lethal hazards, closer to classic Cell Stage pacing.
            _initialNPCCount = 16;
            _initialOrganismCount = 24;
            _initialPartPickupCount = 4;

            _predatorCount = 1;
            _toxinCloudCount = 0;
            _currentZoneCount = 0;
            _whirlpoolCount = 0;
        }

        public void OnExit()
        {
            _isActive = false;
            _currentGoal = null;
            _playerWasThreatenedByPredator = false;
            Time.timeScale = 1f;
            _tutorialController?.Deactivate();
            _tutorialController = null;
            if (_highContrastManager != null) _highContrastManager.enabled = false;
            _highContrastManager = null;
            if (_arenaDebris != null) { _arenaDebris.enabled = false; _arenaDebris = null; }
            _envEventManager?.Deactivate();
            _envEventManager = null;
            _organismPool?.Clear();
            _organismPool = null;
            EventBus.Unsubscribe<CellEvolutionTriggeredEvent>(OnEvolutionTriggered);
            EventBus.Unsubscribe<CellEvolutionCompleteEvent>(OnFinalEvolutionComplete);
            EventBus.Unsubscribe<CellOrganismDiedEvent>(OnCellOrganismDied);
            EventBus.Unsubscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Unsubscribe<CellPlayerReproducedEvent>(OnPlayerReproduced);
            EventBus.Unsubscribe<CellEnvironmentEventTriggered>(OnEnvironmentEvent);
            EventBus.Unsubscribe<CellAteLargerPreyEvent>(OnAteLargerPrey);
            EventBus.Unsubscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
            EventBus.Unsubscribe<CellPartCollectedEvent>(OnPartCollected);
            _leviathanActive = false;
            _activeLeviathanCell = null;
            _authority = null;
            _networkTick?.Deactivate();
            _networkTick = null;
            _arenaCurrentsCache.Clear();
            _predatorCache.Clear();
            _organismCache.Clear();
            Debug.Log("[CellStage] Exiting Cell Stage");
        }

        private void OnDestroy()
        {
            if (_isActive)
                OnExit();
            if (StageManager.HasInstance)
                StageManager.Instance.UnregisterStage(GameStageType.Cell);
        }

        public void OnUpdate()
        {
            if (!_isActive) return;
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;

            var timeScaleOptions = GetDocumentaryTimeScaleOptions();
            _timeScaleIndex = Mathf.Clamp(_timeScaleIndex, 0, timeScaleOptions.Length - 1);
            Time.timeScale = timeScaleOptions[_timeScaleIndex];

            var kb = Keyboard.current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb != null && kb.rightBracketKey.wasPressedThisFrame)
            {
                _timeScaleIndex = (_timeScaleIndex + 1) % timeScaleOptions.Length;
            }

            Vector3 envPos = _player != null ? _player.transform.position : _arenaCenter;
            if (kb != null && kb.leftAltKey.isPressed)
            {
                if (kb.fKey.wasPressedThisFrame)
                    _foodManager.AddFoodAt(envPos + new Vector3(2f, 0, 0), FoodType.Plant, 5f);
                if (kb.tKey.wasPressedThisFrame)
                    SpawnToxinAtPosition(envPos + new Vector3(3f, 0, 0));
                if (kb.cKey.wasPressedThisFrame)
                    SpawnCurrentAtPosition(envPos + new Vector3(4f, 0, 0), new Vector3(1f, 0, 0));
            }
#endif

            UpdateEscapePredatorGoal();
            UpdateToxicSurvivalGoal();
            UpdateNicheFill();
            UpdateEcosystemBalancing();
            UpdateLeviathan();
        }

        private void UpdateToxicSurvivalGoal()
        {
            if (_player == null || !_currentGoal.HasValue) return;
            if (_currentGoal.Value.Type != CellGoal.GoalType.SurviveInToxicZone) return;

            var biome = GetBiomeAt(_player.transform.position);
            bool inToxic = biome == Biome.BiomeType.ToxicZone;
            var next = _currentGoal.Value.ApplySurviveInToxic(Time.deltaTime, inToxic);
            HandleGoalProgress(next);
        }

        // ------------------------------------------------------------------ trophic role classification

        /// <summary>Ecological role derived from genome Attack vs Digestion balance.</summary>
        public enum TrophicRole { Herbivore, Carnivore, Omnivore }

        /// <summary>
        /// Classifies a genome into a trophic role.
        /// Herbivore: Digestion at least 2 points above Attack.
        /// Carnivore: Attack at least 2 points above Digestion.
        /// Omnivore: balanced.
        /// </summary>
        public static TrophicRole ClassifyTrophicRole(Genome g)
        {
            const float threshold = 2f;
            if (g.Digestion >= g.Attack + threshold) return TrophicRole.Herbivore;
            if (g.Attack    >= g.Digestion + threshold) return TrophicRole.Carnivore;
            return TrophicRole.Omnivore;
        }

        private void UpdateNicheFill()
        {
            _nicheFillTimer -= Time.deltaTime;
            if (_nicheFillTimer > 0f) return;
            _nicheFillTimer = _nicheFillCheckInterval;

            // Count live NPC organisms and classify each by trophic role.
            int liveCount = 0, herbivores = 0, carnivores = 0;
            FillActiveOrganismCache();
            for (int i = 0; i < _organismCache.Count; i++)
            {
                var o = _organismCache[i];
                if (o == null || o.IsDead) continue;
                liveCount++;
                var role = ClassifyTrophicRole(o.Genome);
                if (role == TrophicRole.Herbivore) herbivores++;
                else if (role == TrophicRole.Carnivore) carnivores++;
            }

            if (liveCount >= _minOrganismCount && herbivores >= 2 && carnivores >= 1) return;

            int toSpawn = Mathf.Max(0, _minOrganismCount - liveCount);
            // Always ensure at least 2 herbivores and 1 carnivore regardless of total count.
            if (herbivores < 2) toSpawn = Mathf.Max(toSpawn, 2 - herbivores);
            if (carnivores < 1) toSpawn = Mathf.Max(toSpawn, 1);

            var baseGenome = Genome.Default();
            for (int i = 0; i < toSpawn; i++)
            {
                // Prioritise the most-depleted role: herbivores first, then carnivores, then alternate.
                bool spawnHerbivore = herbivores < 2 || (carnivores >= 1 && i % 2 == 0);
                if (carnivores < 1) spawnHerbivore = false;  // force carnivore when none exist
                SpawnNicheFillOrganism(baseGenome, spawnHerbivore);
                if (spawnHerbivore) herbivores++;
                else carnivores++;
            }
        }

        private void SpawnNicheFillOrganism(Genome baseGenome, bool herbivore)
        {
            if (_authority != null && !_authority.CanSpawnOrganism()) return;
            var genome = MutationSystem.Mutate(baseGenome);
            // Apply niche bias: herbivore = high Digestion/low Attack; carnivore = high Attack/lower Digestion
            if (herbivore)
            {
                genome.Digestion = Mathf.Clamp(genome.Digestion + 3f, 0f, 10f);
                genome.Attack    = Mathf.Clamp(genome.Attack    - 2f, 0f, 10f);
                genome.Speed     = Mathf.Clamp(genome.Speed     + 1f, 0f, 10f);
            }
            else
            {
                genome.Attack    = Mathf.Clamp(genome.Attack    + 3f, 0f, 10f);
                genome.Digestion = Mathf.Clamp(genome.Digestion - 1f, 0f, 10f);
                genome.Size      = Mathf.Clamp(genome.Size      + 1f, 0f, 10f);
            }

            var pos = GetSeparatedEntitySpawnPoint(() => GetRandomPointInArena(0.8f), EntitySpawnMinSpacing);
            int speciesId = SpeciationSystem.GetSpeciesIdForOffspring(0, baseGenome, genome);
            float energy = BalanceConstants.CellOffspringInitialEnergy * 1.5f;
            var org = _organismPool != null
                ? _organismPool.Rent(pos, genome, energy, speciesId)
                : Organism.Create(pos, genome, energy, speciesId);
            SpeciationSystem.RegisterOrganism(speciesId);
            OrganismArchetypeConfigurator.Configure(org, OrganismArchetype.Generalist);
        }

        private void UpdateEcosystemBalancing()
        {
            if (_player == null || _foodManager == null) return;

            _dynamicFoodTimer -= Time.deltaTime;
            _dynamicPredatorTimer -= Time.deltaTime;

            if (_dynamicFoodTimer <= 0f)
            {
                _dynamicFoodTimer = BalanceConstants.CellDynamicFoodCheckIntervalSeconds;
                TryReinforceFood();
            }

            if (_dynamicPredatorTimer <= 0f)
            {
                _dynamicPredatorTimer = BalanceConstants.CellDynamicPredatorReinforcementIntervalSeconds;
                TryReinforcePredators();
            }
        }

        private void TryReinforceFood()
        {
            if (_foodManager.FoodCount >= BalanceConstants.CellDynamicMinimumFoodCount) return;

            int spawnCount = BalanceConstants.CellDynamicFoodSpawnCount;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 offset = Random.insideUnitSphere * (_arenaRadius * 0.18f);
                offset.y = 0f;
                Vector3 position = ClampToArena(_player.transform.position + offset);

                _foodManager.AddFoodAt(position, FoodType.Plant, 6f);
            }
        }

        private void TryReinforcePredators()
        {
            int currentPredators = FillActivePredatorCache();
            int desiredMax = Mathf.Max(_predatorCount + 1, BalanceConstants.CellDynamicPredatorMaxCount);
            if (currentPredators >= desiredMax) return;
            if (_player.NormalizedMass < BalanceConstants.CellDynamicPredatorMassThresholdFraction) return;
            if (_playerWasThreatenedByPredator) return;

            if (!TryGetPredatorReinforcementSpawnPosition(out Vector3 spawnPos))
                return;

            bool territorial = Random.value < 0.4f;
            float scaleFactor = Mathf.Clamp01(_player.NormalizedMass * 0.9f + Random.Range(-0.15f, 0.15f));
            SpawnPredator(spawnPos, territorial, scaleFactor);
        }

        private bool TryGetPredatorReinforcementSpawnPosition(out Vector3 spawnPos)
        {
            spawnPos = _arenaCenter;
            if (_player == null) return false;

            float minDist = Mathf.Max(0f, BalanceConstants.CellDynamicPredatorMinSpawnDistanceFromPlayer);
            float minDistSqr = minDist * minDist;
            int attempts = Mathf.Max(1, BalanceConstants.CellDynamicPredatorSpawnAttempts);
            for (int i = 0; i < attempts; i++)
            {
                Vector3 candidate = GetRandomArenaEdgePosition();
                if ((candidate - _player.transform.position).sqrMagnitude >= minDistSqr)
                {
                    spawnPos = candidate;
                    return true;
                }
            }

            Vector3 awayFromPlayer = (_arenaCenter - _player.transform.position);
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude < 0.01f)
                awayFromPlayer = Vector3.forward;
            spawnPos = ClampToArena(_arenaCenter + awayFromPlayer.normalized * (_arenaRadius * 0.85f));
            return (spawnPos - _player.transform.position).sqrMagnitude >= minDistSqr * 0.5f;
        }

        private Vector3 GetRandomArenaEdgePosition()
        {
            return GetRandomPointInArenaRing(0.70f, 0.9f);
        }

        /// <summary>
        /// Returns spawn ring fractions for each archetype slot in the 10-organism cycle.
        /// This keeps ecosystem niches spatially coherent at run start.
        /// </summary>
        public static void GetArchetypeSpawnBand(
            int cycleMod10,
            out float minRadiusFraction,
            out float maxRadiusFraction,
            out bool preferAlgaeForest)
        {
            preferAlgaeForest = false;
            switch (cycleMod10)
            {
                case 0: // FilterFeeder
                    minRadiusFraction = 0.18f;
                    maxRadiusFraction = 0.62f;
                    return;
                case 1: // BacteriaSwarm
                    minRadiusFraction = 0.12f;
                    maxRadiusFraction = 0.42f;
                    return;
                case 3: // Scavenger
                    minRadiusFraction = 0.45f;
                    maxRadiusFraction = 0.88f;
                    return;
                case 5: // Parasite
                    minRadiusFraction = 0.30f;
                    maxRadiusFraction = 0.72f;
                    return;
                case 7: // Photosynthetic
                    minRadiusFraction = 0.66f;
                    maxRadiusFraction = 0.89f;
                    preferAlgaeForest = true;
                    return;
                case 9: // Colonial
                    minRadiusFraction = 0.38f;
                    maxRadiusFraction = 0.84f;
                    return;
                default: // Generalist
                    minRadiusFraction = 0.08f;
                    maxRadiusFraction = 0.92f;
                    return;
            }
        }

        private Vector3 GetOrganismSpawnPoint(int cycleMod10)
        {
            GetArchetypeSpawnBand(
                cycleMod10,
                out float minFrac,
                out float maxFrac,
                out bool preferAlgaeForest);

            Vector3 fallback = GetRandomPointInArenaRing(minFrac, maxFrac);
            const int attempts = 10;
            for (int i = 0; i < attempts; i++)
            {
                Vector3 candidate = GetRandomPointInArenaRing(minFrac, maxFrac);
                var biome = GetBiomeAt(candidate);
                if (preferAlgaeForest)
                {
                    if (biome == Biome.BiomeType.AlgaeForest)
                        return candidate;
                    if (biome != Biome.BiomeType.ToxicZone)
                        fallback = candidate;
                }
                else
                {
                    if (biome != Biome.BiomeType.ToxicZone)
                        return candidate;
                }
            }
            return fallback;
        }

        private Vector3 GetRandomPointInArena(float maxRadiusFraction = 1f)
        {
            float clampedFraction = Mathf.Clamp01(maxRadiusFraction);
            float maxRadius = _arenaRadius * clampedFraction;
            float radius = Mathf.Sqrt(Random.Range(0f, maxRadius * maxRadius));
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return _arenaCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private Vector3 GetRandomPointInArenaRing(float minRadiusFraction, float maxRadiusFraction)
        {
            float minFraction = Mathf.Clamp01(minRadiusFraction);
            float maxFraction = Mathf.Clamp(maxRadiusFraction, minFraction, 1f);
            float minRadius = _arenaRadius * minFraction;
            float maxRadius = _arenaRadius * maxFraction;
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return _arenaCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private Vector3 GetSeparatedEntitySpawnPoint(System.Func<Vector3> candidateFactory, float minSpacing)
        {
            Vector3 fallback = ClampToArena(candidateFactory());
            float spacing = Mathf.Max(0.5f, minSpacing);
            for (int i = 0; i < EntitySpawnPositionAttempts; i++)
            {
                Vector3 candidate = ClampToArena(candidateFactory());
                if (IsEntitySpawnPointClear(candidate, spacing))
                    return candidate;

                fallback = candidate;
            }

            return PushSpawnPointAwayFromNearbyEntities(fallback, spacing);
        }

        private static bool IsEntitySpawnPointClear(Vector3 position, float minSpacing)
        {
            var hits = Physics.OverlapSphere(
                position,
                minSpacing,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;

                if (hit.GetComponentInParent<PlayerCell>() != null
                    || hit.GetComponentInParent<CellAI>() != null
                    || hit.GetComponentInParent<Organism>() != null
                    || hit.GetComponentInParent<PredatorCell>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 PushSpawnPointAwayFromNearbyEntities(Vector3 position, float minSpacing)
        {
            var hits = Physics.OverlapSphere(
                position,
                minSpacing,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            Vector3 push = Vector3.zero;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;

                bool isEntity = hit.GetComponentInParent<PlayerCell>() != null
                    || hit.GetComponentInParent<CellAI>() != null
                    || hit.GetComponentInParent<Organism>() != null
                    || hit.GetComponentInParent<PredatorCell>() != null;
                if (!isEntity)
                    continue;

                Vector3 away = position - hit.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.001f)
                    away = GetRandomEntityScatterOffset(1f);
                push += away.normalized;
            }

            if (push.sqrMagnitude < 0.001f)
                return ClampToArena(position);

            return ClampToArena(position + push.normalized * minSpacing);
        }

        private void SpawnPredator(Vector3 position, bool territorial, float scaleFactor)
        {
            position = GetSeparatedEntitySpawnPoint(
                () => ClampToArena(position + GetRandomEntityScatterOffset(EntitySpawnMinSpacing)),
                EntitySpawnMinSpacing * 1.2f);
            var pred = ProceduralCellGenerator.CreateCellFromGenome(position, BuildPredatorGenome(territorial ? 1 : 2));
            if (pred == null)
            {
                Debug.LogWarning("Predator reinforcement spawn skipped: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                return;
            }
            pred.name = territorial ? "Predator_Reinforcement_Territorial" : "Predator_Reinforcement";

            var col = pred.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            if (EnsureCellRigidbody(pred, "Predator reinforcement spawn") == null)
            {
                Destroy(pred);
                return;
            }

            var predator = pred.AddComponent<PredatorCell>();
            predator.Initialize(scaleFactor);
            if (territorial)
                predator.SetTerritory(position, TerritorialPredatorRadius);
            CellSwimVisual.Ensure(pred, CellSwimVisual.SwimProfile.Predator);
        }

        private void UpdateEscapePredatorGoal()
        {
            if (_player == null) return;

            FillActivePredatorCache();
            float threatRadius = BalanceConstants.CellGoalPredatorThreatRadius;
            float safeRadius = BalanceConstants.CellGoalPredatorSafeRadius;
            float threatRadiusSqr = threatRadius * threatRadius;
            float safeRadiusSqr = safeRadius * safeRadius;
            Vector3 playerPos = _player.transform.position;

            bool anyThreat = IsAnyPredatorWithinRadiusSqr(_predatorCache, playerPos, threatRadiusSqr);

            if (anyThreat)
            {
                _playerWasThreatenedByPredator = true;
                return;
            }

            if (!_playerWasThreatenedByPredator) return;

            bool allSafe = !IsAnyPredatorWithinRadiusSqr(_predatorCache, playerPos, safeRadiusSqr);

            if (allSafe)
            {
                _playerWasThreatenedByPredator = false;
                _player.NotifyPredatorEscaped();
                EventBus.Publish(new CellPlayerEscapedPredatorEvent());
                OnPlayerEscapedPredator();
            }
        }

        private void RefreshArenaCurrentsCache(bool force = false)
        {
            if (!force && Time.time < _nextArenaCurrentsRefreshTime)
                return;

            _nextArenaCurrentsRefreshTime = Time.time + ArenaCurrentCacheRefreshInterval;
            _arenaCurrentsCache.Clear();
            var currents = FindObjectsByType<ArenaCurrent>(FindObjectsInactive.Exclude);
            for (int i = 0; i < currents.Length; i++)
            {
                if (currents[i] != null)
                    _arenaCurrentsCache.Add(currents[i]);
            }
        }

        private int FillActivePredatorCache()
        {
            _predatorCache.Clear();
            int fromRegistry = PredatorPackCoordinator.CopyRegisteredPredators(_predatorCache);
            if (fromRegistry > 0)
                return _predatorCache.Count;

            var predators = FindObjectsByType<PredatorCell>(FindObjectsInactive.Exclude);
            for (int i = 0; i < predators.Length; i++)
            {
                if (predators[i] != null)
                    _predatorCache.Add(predators[i]);
            }

            return _predatorCache.Count;
        }

        private int FillActiveOrganismCache()
        {
            _organismCache.Clear();
            int fromRegistry = Organism.CopyActiveInstances(_organismCache);
            if (fromRegistry > 0)
                return _organismCache.Count;

            var organisms = FindObjectsByType<Organism>(FindObjectsInactive.Exclude);
            for (int i = 0; i < organisms.Length; i++)
            {
                if (organisms[i] != null)
                    _organismCache.Add(organisms[i]);
            }

            return _organismCache.Count;
        }

        /// <summary>Returns true when any predator in the list is within the provided squared radius.</summary>
        public static bool IsAnyPredatorWithinRadiusSqr(
            IReadOnlyList<PredatorCell> predators,
            Vector3 playerPosition,
            float radiusSqr)
        {
            if (predators == null || radiusSqr <= 0f) return false;
            for (int i = 0; i < predators.Count; i++)
            {
                var predator = predators[i];
                if (predator == null) continue;
                if ((predator.transform.position - playerPosition).sqrMagnitude < radiusSqr)
                    return true;
            }

            return false;
        }

        private void OnPlayerEscapedPredator()
        {
            if (!_currentGoal.HasValue) return;
            var next = _currentGoal.Value.ApplyEscapedPredator();
            HandleGoalProgress(next);
        }

        private void OnPlayerReproduced(CellPlayerReproducedEvent _)
        {
            if (!_currentGoal.HasValue) return;
            var next = _currentGoal.Value.ApplyReproduced();
            HandleGoalProgress(next);
        }

        private void OnAteLargerPrey(CellAteLargerPreyEvent evt)
        {
            if (!_currentGoal.HasValue) return;
            var next = _currentGoal.Value.ApplyAteLargerPrey(evt.PreyWasLarger);
            HandleGoalProgress(next);
        }

        private void OnPartCollected(CellPartCollectedEvent evt)
        {
            if (!_currentGoal.HasValue) return;
            var next = _currentGoal.Value.ApplyCollectedPart();
            HandleGoalProgress(next);
        }

        private void OnEnvironmentEvent(CellEnvironmentEventTriggered evt)
        {
            if (evt.EventType == CellEnvironmentEventType.RadiationSurge && evt.Started)
                _player?.OfferRadiationMutation();
        }

        private void SetCurrentGoal(CellGoal? goal)
        {
            _currentGoal = goal;
            if (!goal.HasValue) return;
            ShowContextualGoalTip(goal.Value);
        }

        private void HandleGoalProgress(CellGoal next)
        {
            _currentGoal = next;
            if (!next.IsComplete) return;

            if (GameManager.Instance != null)
                GameManager.Instance.AddDNAPoints(
                    GameManager.Instance.Modifiers.ApplyDNAReward(BalanceConstants.CellDNAPointsPerGoalComplete));
            EventBus.Publish(new CellGoalCompletedEvent { GoalType = next.Type });
            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(0.2f, 0.15f);
            SetCurrentGoal(CellGoal.GetNextGoalAfterCompletion(next));
        }

        private void ShowContextualGoalTip(CellGoal goal)
        {
            var overlay = RoobsWorld.UI.TutorialOverlay.Instance;
            if (overlay == null) return;

            switch (goal.Type)
            {
                case CellGoal.GoalType.EscapePredator:
                    overlay.ShowContextualTip("tutorial.cell.tip.predator_warning", 4f);
                    break;
                case CellGoal.GoalType.ReproduceOnce:
                    overlay.ShowContextualTip("tutorial.cell.tip.first_reproduction", 4f);
                    break;
                case CellGoal.GoalType.EatFood:
                case CellGoal.GoalType.SurviveInToxicZone:
                case CellGoal.GoalType.EatDuringNutrientBloom:
                case CellGoal.GoalType.EatLargerPrey:
                case CellGoal.GoalType.CollectParts:
                    overlay.ShowContextualTip("tutorial.cell.goal_intro", 4f);
                    break;
                default:
                    overlay.ShowContextualTip("tutorial.cell.goal_intro", 4f);
                    break;
            }
        }

        // ------------------------------------------------------------------ leviathan

        private void UpdateLeviathan()
        {
            _leviathanStageTimer += Time.deltaTime;

            // Clean up if the leviathan GameObject was destroyed externally
            if (_leviathanActive && (_activeLeviathanCell == null || _activeLeviathanCell.IsDead))
            {
                _leviathanActive      = false;
                _activeLeviathanCell  = null;
                _leviathanRespawnTimer = BalanceConstants.LeviathanRespawnIntervalSeconds;
            }

            if (_leviathanActive) return;

            _leviathanRespawnTimer -= Time.deltaTime;
            if (_leviathanRespawnTimer > 0f) return;

            // Spawn conditions: enough time elapsed AND enough species in the ecosystem
            if (_leviathanStageTimer < BalanceConstants.LeviathanMinSpawnSeconds) return;
            var species = SpeciationSystem.GetAllSpecies();
            if (species == null || species.Count < BalanceConstants.LeviathanMinSpeciesCount) return;
            if (_authority != null && !_authority.CanSpawnLeviathan()) return;

            SpawnLeviathan();
        }

        private void SpawnLeviathan()
        {
            if (_player == null) return;

            // Spawn on the outer ring, opposite side from the player
            Vector3 playerDir = (_player.transform.position - _arenaCenter).normalized;
            if (playerDir.sqrMagnitude < 0.01f) playerDir = Vector3.forward;
            float spawnRadius = _arenaRadius * 0.85f;
            Vector3 spawnPos  = _arenaCenter - playerDir * spawnRadius;
            spawnPos.y = 0f;

            var go = ProceduralCellGenerator.CreateCellFromGenome(spawnPos, BuildLeviathanGenome());
            if (go == null)
            {
                Debug.LogWarning("Leviathan spawn skipped: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                return;
            }
            go.name = "Leviathan";

            if (EnsureCellRigidbody(go, "Leviathan spawn") == null)
            {
                Destroy(go);
                return;
            }

            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var leviathan = go.AddComponent<LeviathanCell>();
            leviathan.Initialize(_player, _foodManager);
            AttachCellWakeTrail(go, baseWidth: 0.4f, lifetime: 1.8f, alpha: 0.28f);
            CellSwimVisual.Ensure(go, CellSwimVisual.SwimProfile.Leviathan);

            _activeLeviathanCell = leviathan;
            _leviathanActive     = true;

            // Tutorial tip on first encounter
            var overlay = RoobsWorld.UI.TutorialOverlay.Instance;
            overlay?.ShowContextualTip("cell.leviathan.tutorial", 7f);
        }

        private void OnLeviathanDefeated(CellLeviathanDefeatedEvent evt)
        {
            _leviathanActive     = false;
            _activeLeviathanCell = null;
            _leviathanRespawnTimer = BalanceConstants.LeviathanRespawnIntervalSeconds;
        }

        /// <summary>
        /// Shows a one-off tutorial tip explaining the goal system the first time a run starts,
        /// as long as the cell-stage tutorial sequence hasn't been completed yet.
        /// </summary>
        private void ShowGoalLinkedTutorialTip()
        {
            var overlay = RoobsWorld.UI.TutorialOverlay.Instance;
            if (overlay == null) return;
            // Only show if the cell stage tutorial is not yet marked complete
            bool cellDone = overlay.Progress?.IsStageCompleted(GameStageType.Cell) ?? false;
            if (!cellDone)
                overlay.ShowContextualTip("tutorial.cell.goal_intro", 6f);
        }

        private void SpawnPlayer()
        {
            var playerGo = ProceduralCellGenerator.CreateCellFromGenome(_arenaCenter, Genome.Default());
            if (playerGo == null)
            {
                Debug.LogWarning("Player spawn failed: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                return;
            }
            playerGo.name = "PlayerCell";
            AttachCellWakeTrail(playerGo, baseWidth: 0.18f, lifetime: 1.15f, alpha: 0.5f);
            CellSwimVisual.Ensure(playerGo, CellSwimVisual.SwimProfile.Player);

            if (EnsureCellRigidbody(playerGo, "Player spawn") == null)
            {
                Destroy(playerGo);
                return;
            }

            var playerColliders = playerGo.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < playerColliders.Length; i++)
            {
                var col = playerColliders[i];
                if (col == null) continue;
                if (col is MeshCollider)
                {
                    // Dynamic rigidbodies cannot use non-convex MeshColliders.
                    // Keep imported mesh colliders disabled and rely on primitive triggers.
                    col.enabled = false;
                    continue;
                }
                col.enabled = true;
                col.isTrigger = true;
            }

            _player = playerGo.AddComponent<PlayerCell>();

            CellEvolutionUI.CreateRuntime(_player);
            var threatIndicator = ThreatIndicator.CreateRuntime(_player);
            ThreatAudioFeedback.CreateRuntime(threatIndicator);
            RunSummaryUI.Attach(_player);
        }

        private void SpawnArena()
        {
            var arenaMat = ProceduralTextureFactory.CreateTransparentMaterial(
                ProceduralTextureFactory.CreateCellArenaTexture(256),
                new Color(0.56f, 0.9f, 0.96f, 0.95f));
            var floor = ModelFactory.CreateGround(_arenaRadius * 2f, arenaMat);
            floor.transform.position = _arenaCenter;
            floor.name = "CellArenaFloor";

            // Add soft translucent water layers to create depth/haze and a more painterly cell-ocean feel.
            SpawnBackdropLayer("CellArenaBackdropNear", _arenaRadius * 2.5f, -0.06f, 0.58f, new Color(0.67f, 0.93f, 1f, 0.44f));
            SpawnBackdropLayer("CellArenaBackdropFar",  _arenaRadius * 3.5f, -0.12f, 0.35f, new Color(0.79f, 0.98f, 1f, 0.3f));
            SpawnWaterVignette();
            SpawnForegroundAlgae();
        }

        private void SpawnBackdropLayer(string name, float size, float yOffset, float smoothness, Color tint)
        {
            var mat = CreateWaterBackdropMaterial(name, tint, smoothness);
            var layer = ModelFactory.CreateGround(size, mat);
            layer.transform.position = _arenaCenter + new Vector3(0f, yOffset, 0f);
            layer.name = name;
        }

        private static Material CreateWaterBackdropMaterial(string layerName, Color tint, float smoothness)
        {
            var waterShader = Shader.Find("RoobsWorld/WaterBackdrop");
            if (waterShader != null)
            {
                var waterMat = new Material(waterShader)
                {
                    name = $"{layerName}_WaterMat"
                };
                var deep = Color.Lerp(new Color(0.04f, 0.16f, 0.27f, tint.a), tint, 0.35f);
                var foam = Color.Lerp(Color.white, tint, 0.2f);
                foam.a = tint.a * 0.9f;
                waterMat.SetColor("_BaseColor", tint);
                waterMat.SetColor("_DeepColor", deep);
                waterMat.SetColor("_FoamColor", foam);
                waterMat.SetFloat("_WaveScale", Mathf.Lerp(2.4f, 4.2f, smoothness));
                waterMat.SetFloat("_WaveSpeed", Mathf.Lerp(0.12f, 0.24f, smoothness));
                waterMat.SetFloat("_Distortion", Mathf.Lerp(0.16f, 0.34f, smoothness));
                waterMat.SetFloat("_CausticStrength", Mathf.Lerp(0.2f, 0.45f, smoothness));
                waterMat.SetFloat("_EdgeFade", 1.12f);
                return waterMat;
            }

            var fallbackMat = ProceduralTextureFactory.CreateTransparentMaterial(
                ProceduralTextureFactory.CreateCellArenaTexture(192),
                tint);
            fallbackMat.SetFloat("_Smoothness", smoothness);
            return fallbackMat;
        }

        private void SpawnWaterVignette()
        {
            SpawnCornerCloud("Vignette_TopRight", new Vector3(_arenaRadius * 0.84f, -0.05f, _arenaRadius * 0.84f), _arenaRadius * 0.95f, new Color(0.05f, 0.1f, 0.16f, 0.42f));
            SpawnCornerCloud("Vignette_TopLeft", new Vector3(-_arenaRadius * 0.9f, -0.05f, _arenaRadius * 0.75f), _arenaRadius * 0.72f, new Color(0.2f, 0.32f, 0.58f, 0.24f));
            SpawnCornerCloud("Vignette_BottomLeft", new Vector3(-_arenaRadius * 0.82f, -0.05f, -_arenaRadius * 0.8f), _arenaRadius * 0.68f, new Color(0.3f, 0.09f, 0.26f, 0.22f));
        }

        private void SpawnCornerCloud(string name, Vector3 localOffset, float size, Color tint)
        {
            var cloudMat = ProceduralTextureFactory.CreateTransparentMaterial(
                ProceduralTextureFactory.CreateCellArenaTexture(128),
                tint);
            cloudMat.SetFloat("_Smoothness", 0.2f);
            var cloud = ModelFactory.CreateGround(size, cloudMat);
            cloud.name = name;
            cloud.transform.position = _arenaCenter + localOffset;
        }

        private void SpawnForegroundAlgae()
        {
            Vector3 basePos = _arenaCenter + new Vector3(-_arenaRadius * 0.42f, 0f, -_arenaRadius * 0.46f);
            for (int i = 0; i < 22; i++)
            {
                float angle = i * 0.47f;
                float r = 0.6f + (i % 6) * 0.22f;
                Vector3 pos = basePos + new Vector3(Mathf.Cos(angle) * r, 0.22f + (i % 3) * 0.1f, Mathf.Sin(angle) * r);
                var frond = ModelFactory.CreateCellFood(pos,
                    Color.Lerp(new Color(0.5f, 0.9f, 0.22f), new Color(0.86f, 1f, 0.3f), (i % 5) / 4f), 0.18f + (i % 4) * 0.03f);
                frond.name = $"ForegroundAlgae_{i}";
                var foodItem = frond.AddComponent<FoodItem>();
                foodItem.Initialize(FoodType.Plant, 3f);
                foodItem.EnsureTriggerColliders();
            }
        }

        private void SpawnFood()
        {
            if (_authority != null && !_authority.CanSpawnFood()) return;
            int foodMult = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.ApplyFoodCount(_foodManager.InitialFoodCount)
                : _foodManager.InitialFoodCount;
            _foodManager.SpawnInArena(_arenaCenter, _arenaRadius, foodMult);
        }

        private void SpawnNPCs()
        {
            if (_authority != null && !_authority.CanSpawnOrganism()) return;
            int npcCount = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.ApplyNPCCount(_initialNPCCount)
                : _initialNPCCount;
            for (int i = 0; i < npcCount; i++)
            {
                float size = Random.Range(0.2f, 0.52f);
                Color npcColor = GetClassicNpcColor(i);
                var pos = GetSeparatedEntitySpawnPoint(() => GetRandomPointInArena(), EntitySpawnMinSpacing);
                var npcGenome = BuildDecorativeNpcGenome(i, npcColor, size);
                var npc = ProceduralCellGenerator.CreateCellFromGenome(pos, npcGenome);
                if (npc == null)
                {
                    Debug.LogWarning("NPC spawn skipped: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                    continue;
                }
                npc.name = $"NPCCell_{i}";
                AttachCellWakeTrail(npc, baseWidth: 0.08f, lifetime: 0.8f, alpha: 0.32f);
                CellSwimVisual.Ensure(npc, CellSwimVisual.SwimProfile.Npc);

                if (EnsureCellRigidbody(npc, $"NPC spawn {i}") == null)
                {
                    Destroy(npc);
                    continue;
                }

                npc.AddComponent<CellAI>();
            }
        }

        private static Genome BuildDecorativeNpcGenome(int index, Color color, float visualScale)
        {
            float normalizedSize = Mathf.Clamp(visualScale / 0.5f, 0.35f, 1.1f);
            return new Genome
            {
                Speed = 1.4f + (index % 5) * 0.55f,
                Size = normalizedSize,
                Attack = index % 4 == 0 ? 4.8f : 1.2f + (index % 3),
                Defence = 0.9f + (index % 4) * 0.35f,
                Digestion = index % 4 == 0 ? 1.1f : 2.1f + (index % 5) * 0.45f,
                MetabolismCost = 0.5f,
                Spikes = index % 3 == 0 ? 5.2f : 1.3f + (index % 4) * 0.85f,
                Flagella = 3.2f + (index % 6) * 0.9f,
                Color = color,
                Traits = index % 9 == 0 ? GeneticTraits.Photosynthetic :
                         index % 11 == 0 ? GeneticTraits.Toxic :
                         GeneticTraits.None
            };
        }

        private static Genome BuildPredatorGenome(int index)
        {
            return new Genome
            {
                Speed = 5.8f + (index % 3) * 0.45f,
                Size = 2.2f,
                Attack = 7.5f,
                Defence = 3.5f,
                Digestion = 1.1f,
                MetabolismCost = 0.75f,
                Spikes = 7.8f,
                Flagella = 5.5f,
                Color = index % 2 == 0 ? new Color(0.52f, 0.08f, 0.62f) : new Color(0.78f, 0.16f, 0.22f),
                Traits = GeneticTraits.Toxic
            };
        }

        private static Genome BuildLeviathanGenome()
        {
            return new Genome
            {
                Speed = 4.2f,
                Size = 3.8f,
                Attack = 9.2f,
                Defence = 7.5f,
                Digestion = 1.4f,
                MetabolismCost = 0.95f,
                Spikes = 9f,
                Flagella = 4.5f,
                Color = new Color(0.62f, 0.07f, 0.12f),
                Traits = GeneticTraits.Armoured | GeneticTraits.Toxic
            };
        }

        private static Color GetClassicNpcColor(int index)
        {
            // Spore-like cell-stage palette: saturated, playful species colors with enough
            // warm variants that the red player still reads as the focal point.
            switch (index % 14)
            {
                case 0: return new Color(0.94f, 0.72f, 0.2f);
                case 1: return new Color(0.26f, 0.72f, 0.3f);
                case 2: return new Color(0.24f, 0.5f, 0.95f);
                case 3: return new Color(0.88f, 0.32f, 0.66f);
                case 4: return new Color(0.55f, 0.24f, 0.92f);
                case 5: return new Color(0.95f, 0.47f, 0.16f);
                case 6: return new Color(0.22f, 0.78f, 0.74f);
                case 7: return new Color(0.78f, 0.18f, 0.28f);
                case 8: return new Color(0.55f, 0.92f, 0.34f);
                case 9: return new Color(0.12f, 0.22f, 0.5f);
                case 10: return new Color(0.98f, 0.58f, 0.67f);
                case 11: return new Color(0.62f, 0.83f, 0.25f);
                case 12: return new Color(0.48f, 0.22f, 0.12f);
                default: return new Color(0.94f, 0.82f, 0.44f);
            }
        }

        private void SpawnOrganisms()
        {
            if (_authority != null && !_authority.CanSpawnOrganism()) return;
            var baseGenome = Genome.Default();
            baseGenome.Color = new Color(0.83f, 0.66f, 0.3f);
            baseGenome.Spikes = 2.3f;
            baseGenome.Flagella = 3.8f;
            SpeciationSystem.EnsureSpeciesExists(0, baseGenome);
            int count = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.ApplyNPCCount(_initialOrganismCount)
                : _initialOrganismCount;

            for (int i = 0; i < count; i++)
            {
                // Archetype split (per 10-organism cycle):
                //   i % 10 == 0 -> FilterFeeder (passive cilia absorber, Herbivore)
                //   i % 10 == 1 -> BacteriaSwarm (boids flocking, Omnivore, tiny + fast)
                //   i % 10 == 3 -> Scavenger (active meat-seeker, Herbivore trophic)
                //   i % 10 == 5 -> Parasite (latches onto larger hosts, Omnivore trophic)
                //   i % 10 == 7 -> Photosynthetic (light-seeking producer)
                //   i % 10 == 9 -> Colonial (cohesive cluster, energy-sharing omnivore)
                //   else -> standard CellOrganismAI (general herbivore / omnivore / carnivore)
                bool isFilterFeeder  = (i % 10 == 0);
                bool isBacteria      = (i % 10 == 1);
                bool isScavenger     = (i % 10 == 3);
                bool isParasite      = (i % 10 == 5);
                bool isPhotosynthetic = (i % 10 == 7);
                bool isColonial      = (i % 10 == 9);
                var archetype = OrganismArchetype.Generalist;

                Genome genome;
                if (isFilterFeeder)
                {
                    genome = FilterFeederCell.BuildGenome();
                    archetype = OrganismArchetype.FilterFeeder;
                }
                else if (isBacteria)
                {
                    genome = BacteriaSwarmCell.BuildGenome();
                    archetype = OrganismArchetype.BacteriaSwarm;
                }
                else if (isScavenger)
                {
                    genome = ScavengerCell.BuildGenome();
                    archetype = OrganismArchetype.Scavenger;
                }
                else if (isParasite)
                {
                    genome = ParasiteCell.BuildGenome();
                    archetype = OrganismArchetype.Parasite;
                }
                else if (isPhotosynthetic)
                {
                    genome = PhotosyntheticCell.BuildGenome();
                    archetype = OrganismArchetype.Photosynthetic;
                }
                else if (isColonial)
                {
                    genome = ColonialCell.BuildGenome();
                    archetype = OrganismArchetype.Colonial;
                }
                else
                {
                    genome = MutationSystem.Mutate(baseGenome);
                }

                // Art direction: keep the ecosystem mostly warm yellow/tan with mild variation,
                // while the player remains the strongest red focal point.
                genome.Color = GetClassicNpcColor(
                    i + (isBacteria ? 3 : isFilterFeeder ? 5 : isScavenger ? 7 : isParasite ? 11 : isPhotosynthetic ? 17 : isColonial ? 19 : 13));
                genome.Spikes = Mathf.Clamp(genome.Spikes * 0.7f + 1.2f, 0f, 10f);
                genome.Flagella = Mathf.Clamp(genome.Flagella * 0.9f + 0.9f, 0f, 10f);
                genome.Size = Mathf.Clamp(genome.Size * 0.78f, 0.3f, 2.2f);

                var pos = GetSeparatedEntitySpawnPoint(() => GetOrganismSpawnPoint(i % 10), EntitySpawnMinSpacing);
                int   speciesId = SpeciationSystem.GetSpeciesIdForOffspring(0, baseGenome, genome);
                // Bacteria start with extra energy to offset their higher metabolism.
                float energy = isBacteria
                    ? BalanceConstants.CellOffspringInitialEnergy * 3f
                    : BalanceConstants.CellOffspringInitialEnergy * 2f;
                var org = _organismPool != null
                    ? _organismPool.Rent(pos, genome, energy, speciesId)
                    : Organism.Create(pos, genome, energy, speciesId);

                if (isFilterFeeder)   org.gameObject.name = $"FilterFeeder_{i}";
                else if (isBacteria)  org.gameObject.name = $"Bacteria_{i}";
                else if (isScavenger) org.gameObject.name = $"Scavenger_{i}";
                else if (isParasite)  org.gameObject.name = $"Parasite_{i}";
                else if (isPhotosynthetic) org.gameObject.name = $"Photosynthetic_{i}";
                else if (isColonial) org.gameObject.name = $"Colonial_{i}";
                else                  org.gameObject.name = $"Organism_{i}";

                SpeciationSystem.RegisterOrganism(speciesId);
                OrganismArchetypeConfigurator.Configure(org, archetype);

                AttachCellWakeTrail(org.gameObject, baseWidth: 0.075f, lifetime: 0.9f, alpha: 0.28f);
                CellSwimVisual.Ensure(org.gameObject, CellSwimVisual.SwimProfile.Npc);
            }
        }

        /// <summary>Spawns an offspring organism, using the pool when available. Used by ReproductionSystem via event.</summary>
        public Organism SpawnPooledOrganism(Vector3 position, Genome genome, float energy, int speciesId)
        {
            position = GetSeparatedEntitySpawnPoint(
                () => ClampToArena(position + GetRandomEntityScatterOffset(EntitySpawnMinSpacing)),
                EntitySpawnMinSpacing);
            var org = _organismPool != null
                ? _organismPool.Rent(position, genome, energy, speciesId)
                : Organism.Create(position, genome, energy, speciesId);
            OrganismArchetypeConfigurator.Configure(org, OrganismArchetype.Generalist);
            CellSwimVisual.Ensure(org.gameObject, CellSwimVisual.SwimProfile.Npc);
            return org;
        }

        private void SpawnPartPickups()
        {
            var pickupPlan = CellPartPickupPlanner.BuildPlan(_initialPartPickupCount);
            var spawnedPositions = new List<Vector3>(pickupPlan.Count);
            for (int i = 0; i < pickupPlan.Count; i++)
            {
                CellPartType partType = pickupPlan[i];
                Color pickupColor = GetPickupColor(partType);
                var pos = GetPartPickupSpawnPoint(spawnedPositions);
                pos.y = 0.3f;
                var pickup = ModelFactory.CreateCellPickup(pos, pickupColor);
                pickup.name = $"CellPartPickup_{i}";

                var pickupComponent = pickup.AddComponent<CellPartPickup>();
                pickupComponent.SetPartType(partType);
                pickupComponent.EnsureTriggerColliders();
                spawnedPositions.Add(pos);
            }
        }

        private Vector3 GetPartPickupSpawnPoint(List<Vector3> existingPositions)
        {
            float minSpacingSqr = PartPickupMinSpacing * PartPickupMinSpacing;
            Vector3 fallback = ClampToArena(GetRandomPointInArenaRing(PartPickupSpawnMinRadiusFraction, PartPickupSpawnMaxRadiusFraction));

            for (int i = 0; i < PartPickupSpawnAttempts; i++)
            {
                Vector3 candidate = GetRandomPointInArenaRing(PartPickupSpawnMinRadiusFraction, PartPickupSpawnMaxRadiusFraction);
                if (GetBiomeAt(candidate) == Biome.BiomeType.ToxicZone)
                    continue;

                bool overlapsExisting = false;
                for (int j = 0; j < existingPositions.Count; j++)
                {
                    if ((existingPositions[j] - candidate).sqrMagnitude < minSpacingSqr)
                    {
                        overlapsExisting = true;
                        break;
                    }
                }

                if (!overlapsExisting)
                    return candidate;

                fallback = candidate;
            }

            return fallback;
        }

        private void SpawnHazards()
        {
            SpawnPredators();
            SpawnToxinClouds();
            SpawnCurrentZones();
            SpawnWhirlpools();
            _arenaDebris = CellArenaDebris.Spawn(_arenaCenter, _arenaRadius);
        }

        // Territorial predators guard the AlgaeForest band (60–90 % of arena radius from centre).
        private const float TerritorialPredatorInnerFrac   = 0.62f;
        private const float TerritorialPredatorOuterFrac   = 0.88f;
        /// <summary>Patrol radius assigned to territorial predators (used by TerritorialPredatorTests).</summary>
        public  const float TerritorialPredatorRadius      = 12f;

        private const float PlayerSpawnSafeRadius = 12f;

        private void SpawnPredators()
        {
            int predCount = GameManager.HasInstance
                ? GameManager.Instance.Modifiers.ApplyPredatorCount(_predatorCount)
                : _predatorCount;
            for (int i = 0; i < predCount; i++)
            {
                // Even-indexed predators patrol the food-rich AlgaeForest band; odd ones roam freely.
                bool territorial = (i % 2 == 0);

                Vector3 pos;
                if (territorial)
                {
                    pos = GetSeparatedEntitySpawnPoint(
                        () => GetRandomPointInArenaRing(TerritorialPredatorInnerFrac, TerritorialPredatorOuterFrac),
                        EntitySpawnMinSpacing * 1.2f);
                }
                else
                {
                    pos = GetSeparatedEntitySpawnPoint(
                        () =>
                        {
                            Vector3 candidate;
                            do
                            {
                                candidate = GetRandomPointInArena(0.8f);
                            }
                            while (Vector3.Distance(candidate, _arenaCenter) < PlayerSpawnSafeRadius);
                            return candidate;
                        },
                        EntitySpawnMinSpacing * 1.2f);
                }

                var pred = ProceduralCellGenerator.CreateCellFromGenome(pos, BuildPredatorGenome(i));
                if (pred == null)
                {
                    Debug.LogWarning("Predator spawn skipped: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                    continue;
                }
                pred.name = territorial ? $"TerritorialPredator_{i}" : $"Predator_{i}";

                var col = pred.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                if (EnsureCellRigidbody(pred, $"Predator spawn {i}") == null)
                {
                    Destroy(pred);
                    continue;
                }

                var predator = pred.AddComponent<PredatorCell>();
                float predatorScale = i == 0
                    ? Random.Range(0.9f, 1f)
                    : Random.Range(0.45f, 0.75f);
                predator.Initialize(predatorScale);
                if (territorial)
                    predator.SetTerritory(pos, TerritorialPredatorRadius);
                AttachCellWakeTrail(pred, baseWidth: 0.2f, lifetime: 1.4f, alpha: 0.34f);
                CellSwimVisual.Ensure(pred, CellSwimVisual.SwimProfile.Predator);
            }
        }

        private static Rigidbody EnsureCellRigidbody(GameObject go, string context)
        {
            if (go == null)
                return null;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"{context} failed: could not attach Rigidbody to '{go.name}'.");
                return null;
            }

            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            return rb;
        }

        private static void AttachCellWakeTrail(GameObject target, float baseWidth, float lifetime, float alpha)
        {
            if (target == null || target.GetComponent<TrailRenderer>() != null)
                return;

            var trail = target.AddComponent<TrailRenderer>();
            trail.time = lifetime;
            trail.minVertexDistance = 0.11f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 3;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.startWidth = baseWidth;
            trail.endWidth = baseWidth * 0.25f;
            trail.widthMultiplier = 1f;
            trail.startColor = new Color(0.92f, 0.98f, 1f, alpha);
            trail.endColor = new Color(0.82f, 0.95f, 1f, 0f);

            if (s_wakeTrailMaterial == null)
            {
                s_wakeTrailMaterial = new Material(Shader.Find("Sprites/Default"));
                s_wakeTrailMaterial.color = new Color(0.95f, 1f, 1f, 0.85f);
            }
            trail.material = s_wakeTrailMaterial;
        }

        private void SpawnToxinClouds()
        {
            for (int i = 0; i < _toxinCloudCount; i++)
            {
                var pos = GetRandomPointInArena(0.7f);
                var cloud = new GameObject($"ToxinCloud_{i}");
                cloud.transform.position = pos;

                cloud.AddComponent<SphereCollider>();
                var toxin = cloud.AddComponent<ToxinCloud>();
                float radius = Random.Range(2f, 5f);
                toxin.Initialize(radius, 0.15f, 30f);

                var visual = ModelFactory.CreateToxinCloud(Vector3.zero, radius);
                visual.transform.SetParent(cloud.transform, false);
            }
        }

        private void SpawnCurrentZones()
        {
            for (int i = 0; i < _currentZoneCount; i++)
            {
                var zone = new GameObject($"ArenaCurrent_{i}");
                zone.transform.position = GetRandomPointInArena(0.6f);

                var box = zone.AddComponent<BoxCollider>();
                box.size = new Vector3(Random.Range(6f, 12f), 2f, Random.Range(6f, 12f));

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var current = zone.AddComponent<ArenaCurrent>();
                current.Initialize(dir, Random.Range(3f, 7f));
            }
        }

        private void SpawnWhirlpools()
        {
            for (int i = 0; i < _whirlpoolCount; i++)
            {
                float radius = Random.Range(8f, 14f);
                var pos = GetRandomPointInArena(0.5f);

                var go = new GameObject($"Whirlpool_{i}");
                go.transform.position = pos;

                var whirlpool = go.AddComponent<WhirlpoolZone>();
                float spinDir = (i % 2 == 0) ? 1f : -1f; // alternate spin direction
                whirlpool.Initialize(radius, pullForce: 5f, spinForce: 3.5f * spinDir);

                // Visual: translucent tinted sphere to indicate zone
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "WhirlpoolVisual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
                Destroy(visual.GetComponent<Collider>());
                var mr = visual.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = ProceduralTextureFactory.CreateTransparentMaterial(
                        Texture2D.whiteTexture, new Color(0.3f, 0.6f, 1f, 0.15f));
                    mr.material = mat;
                }
            }
        }

        /// <summary>Add one food at position (environmental manipulation).</summary>
        public void SpawnFoodAtPosition(Vector3 position)
        {
            if (_foodManager == null) return;
            _foodManager.AddFoodAt(ClampToArena(position), FoodType.Plant, 5f);
        }

        /// <summary>Spawn a toxin cloud at position (environmental manipulation).</summary>
        public void SpawnToxinAtPosition(Vector3 position)
        {
            position = ClampToArena(position);
            var cloud = new GameObject("ToxinCloud_manual");
            cloud.transform.position = position;
            cloud.AddComponent<SphereCollider>().isTrigger = true;
            var toxin = cloud.AddComponent<ToxinCloud>();
            toxin.Initialize(3f, 0.15f, 30f);
            var visual = ModelFactory.CreateToxinCloud(Vector3.zero, 3f);
            visual.transform.SetParent(cloud.transform, false);
        }

        /// <summary>Spawn an arena current at position (environmental manipulation).</summary>
        public void SpawnCurrentAtPosition(Vector3 position, Vector3 direction)
        {
            position = ClampToArena(position);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            else direction.Normalize();
            var zone = new GameObject("ArenaCurrent_manual");
            zone.transform.position = position;
            var box = zone.AddComponent<BoxCollider>();
            box.size = new Vector3(8f, 2f, 8f);
            box.isTrigger = true;
            var current = zone.AddComponent<ArenaCurrent>();
            current.Initialize(direction, 5f);
        }

        private void OnEvolutionTriggered(CellEvolutionTriggeredEvent evt)
        {
            UIManager.Instance.ShowScreen(CellEvolutionUI.ID);
        }

        private void OnFinalEvolutionComplete(CellEvolutionCompleteEvent evt)
        {
            if (!_isActive) return;
            Debug.Log("[CellStage] Final evolution reached. Transitioning to Creature Stage.");
            RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("cell.stage.complete", 5f);
            StageManager.Instance?.TransitionTo(GameStageType.Creature);
        }

        private void OnCellOrganismDied(CellOrganismDiedEvent evt)
        {
            SpeciationSystem.UnregisterOrganism(evt.SpeciesId);

            if (!_isActive || _foodManager == null) return;
            if (_authority != null && !_authority.CanSpawnFood()) return;
            if (Random.value > BalanceConstants.CellCarcassDropChance) return;

            var dropPos = ClampToArena(evt.Position + GetRandomCarcassScatterOffset());
            float nutrition = ComputeCarcassNutrition(evt.VisualScale);
            _foodManager.AddFoodAt(dropPos, FoodType.Meat, nutrition);
        }

        /// <summary>Clamps a world-space position inside a safe margin of the arena boundary.</summary>
        public static Vector3 ClampPositionToArena(Vector3 arenaCenter, float arenaRadius, Vector3 worldPos)
        {
            if (arenaRadius <= 0f)
                return new Vector3(arenaCenter.x, 0f, arenaCenter.z);

            worldPos.y = 0f;
            Vector3 fromCenter = worldPos - arenaCenter;
            fromCenter.y = 0f;
            float maxRadius = Mathf.Max(0f, arenaRadius * 0.95f);
            if (fromCenter.sqrMagnitude > maxRadius * maxRadius && maxRadius > 0f)
                worldPos = arenaCenter + fromCenter.normalized * maxRadius;
            return worldPos;
        }

        public Vector3 ClampToArenaPosition(Vector3 worldPos) => ClampToArena(worldPos);

        private Vector3 ClampToArena(Vector3 worldPos) => ClampPositionToArena(_arenaCenter, _arenaRadius, worldPos);

        private static Vector3 GetRandomCarcassScatterOffset()
        {
            Vector2 circle = Random.insideUnitCircle * BalanceConstants.CellCarcassDropScatterRadius;
            return new Vector3(circle.x, 0f, circle.y);
        }

        private static Vector3 GetRandomEntityScatterOffset(float radius)
        {
            Vector2 circle = Random.insideUnitCircle;
            if (circle.sqrMagnitude < 0.001f)
                circle = Vector2.right;
            circle = circle.normalized * Mathf.Max(0f, radius);
            return new Vector3(circle.x, 0f, circle.y);
        }

        /// <summary>
        /// Converts a dead organism visual scale into carcass nutrition using clamped linear interpolation.
        /// Exposed for EditMode tests so balance tuning remains deterministic.
        /// </summary>
        public static float ComputeCarcassNutrition(float organismVisualScale)
        {
            float t = Mathf.InverseLerp(
                BalanceConstants.CellCarcassScaleMin,
                BalanceConstants.CellCarcassScaleMax,
                organismVisualScale);
            return Mathf.Lerp(
                BalanceConstants.CellCarcassNutritionMin,
                BalanceConstants.CellCarcassNutritionMax,
                t);
        }

        private void OnFoodConsumed(CellFoodConsumedEvent evt)
        {
            if (!_currentGoal.HasValue) return;

            CellGoal next;
            if (_currentGoal.Value.Type == CellGoal.GoalType.EatDuringNutrientBloom)
            {
                bool bloomActive = _envEventManager != null &&
                                   _envEventManager.ActiveEvent == CellEnvironmentEventType.NutrientBloom;
                next = _currentGoal.Value.ApplyEatDuringBloom(bloomActive);
            }
            else
            {
                next = _currentGoal.Value.ApplyFoodConsumed(evt.FoodType);
            }

            HandleGoalProgress(next);
        }

        private static Color GetPickupColor(CellPartType partType)
        {
            switch (partType)
            {
                case CellPartType.Flagella:
                    return new Color(0.2f, 0.9f, 0.4f);
                case CellPartType.Cilia:
                    return new Color(0.2f, 0.6f, 1f);
                case CellPartType.Spike:
                    return new Color(1f, 0.25f, 0.25f);
                case CellPartType.Jet:
                    return new Color(1f, 0.7f, 0.2f);
                default:
                    return Color.white;
            }
        }
    }
}


