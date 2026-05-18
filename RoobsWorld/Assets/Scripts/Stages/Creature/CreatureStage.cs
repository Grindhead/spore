using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Minimal runtime stage for creature gameplay. Spawns a player creature and switches to third-person camera.
    /// </summary>
    public class CreatureStage : MonoBehaviour, IGameStage
    {
        private const int ObjectiveDefeats = 5;
        private const int MinDesiredActiveRivals = 2;
        private const int MaxDesiredActiveRivals = 4;
        private const float RespawnDelaySeconds = 2.5f;
        private const float RespawnProtectionSeconds = 2.2f;
        private const float NextObjectiveDelaySeconds = 6f;
        private const int MultiplayerRivalsPerExtraPlayer = 1;
        private const int MultiplayerRivalsMaxBonus = 3;
        private const int MultiplayerObjectiveDefeatsPerExtraPlayer = 2;
        private const int MultiplayerObjectiveDefeatsMaxBonus = 6;

        private GameObject _playerCreature;
        private CreatureStageHUD _hud;
        private CreatureStageEncounterManager _encounterManager;
        private CreatureStageArena _arena;
        private GameplayTelemetryTracker _telemetryTracker;
        private float _respawnTimer = -1f;
        private bool _objectiveCompleteAnnounced;
        private int _desiredActiveRivals = MinDesiredActiveRivals;
        private int _nextUpgradeDefeatMilestone = RoobsWorld.Creature.CreatureUpgradeCatalog.RivalDefeatsPerUpgrade;
        private bool _upgradeChoiceOpen;
        private CreatureStageObjective _currentObjective;
        private float _holdObjectiveSeconds;
        private int _objectiveStartDefeatedCount;
        private int _objectiveStartAlphaDefeatedCount;
        private int _objectiveStartSpawnedCount;
        private int _objectivePacifyCount;
        private int _objectivesCompleted;
        private float _objectiveStartedAt;
        private float _nextObjectiveTimer = -1f;
        private GameObject _objectiveMarker;
        private bool _playerInsideHoldGround;
        private readonly Dictionary<string, float> _speciesRelationshipScores = new Dictionary<string, float>(8);
        private int _minDesiredActiveRivals = MinDesiredActiveRivals;
        private int _maxDesiredActiveRivals = MaxDesiredActiveRivals;

        public GameStageType StageType => GameStageType.Creature;

        private void Start()
        {
            StageManager.Instance.RegisterStage(this);
        }

        private void OnDestroy()
        {
            if (StageManager.HasInstance)
                StageManager.Instance.UnregisterStage(GameStageType.Creature);
            if (_objectiveMarker != null)
                Destroy(_objectiveMarker);
        }

        public void OnEnter()
        {
            _objectiveCompleteAnnounced = false;
            _objectivesCompleted = 0;
            _nextObjectiveTimer = -1f;
            var session = GameManager.HasInstance ? GameManager.Instance.Session : null;
            int activePlayerCount = GetActiveCreaturePlayerCount(session);
            _minDesiredActiveRivals = ComputeMultiplayerScaledCount(
                MinDesiredActiveRivals,
                activePlayerCount,
                MultiplayerRivalsPerExtraPlayer,
                MultiplayerRivalsMaxBonus);
            _maxDesiredActiveRivals = ComputeMultiplayerScaledCount(
                MaxDesiredActiveRivals,
                activePlayerCount,
                MultiplayerRivalsPerExtraPlayer,
                MultiplayerRivalsMaxBonus);
            _desiredActiveRivals = _minDesiredActiveRivals;
            EnsureArena();
            SpawnPlayerIfNeeded();
            InitializeUpgradeMilestone();
            CameraManager.Instance.ActivateRig(CameraRigType.ThirdPerson);
            if (_playerCreature != null)
            {
                CameraManager.Instance.SetFollowTarget(_playerCreature.transform);
                EnsureEncounterManager(_playerCreature.transform);
                EnsureHud(_playerCreature);
            }
            InitializeObjective();
            EventBus.Subscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
            EventBus.Subscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            _telemetryTracker = GameplayTelemetryTracker.Attach(
                "CreatureGameplayTelemetry",
                panelVisible: Application.isEditor || Debug.isDebugBuild);
        }

        public void OnExit()
        {
            EventBus.Unsubscribe<CreatureCombatFeedbackEvent>(OnCombatFeedback);
            EventBus.Unsubscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            _speciesRelationshipScores.Clear();
            CameraManager.Instance.SetFollowTarget(null);
            _hud?.SetVisible(false);
            if (_objectiveMarker != null)
                _objectiveMarker.SetActive(false);
            if (_telemetryTracker != null) { Destroy(_telemetryTracker.gameObject); _telemetryTracker = null; }
        }

        public void OnUpdate()
        {
            UpdateNextObjectiveTimer();

            if (_encounterManager != null && !_objectiveCompleteAnnounced)
            {
                int objectivePopulationProgress = GetObjectivePopulationProgress();
                int desiredActive = ComputeDesiredActiveRivals(
                    objectivePopulationProgress,
                    GetObjectiveDefeatTarget(),
                    _minDesiredActiveRivals,
                    _maxDesiredActiveRivals);
                _desiredActiveRivals = desiredActive;
                _encounterManager.UpdatePopulation(
                    Time.deltaTime,
                    desiredActive,
                    GetObjectiveDefeatTarget(),
                    objectivePopulationProgress,
                    GetObjectiveSpawnProgress());
            }

            UpdateObjectiveTimer();
            UpdateEncounterProgress();
            OfferUpgradeChoiceIfReady();
            HandlePlayerRespawn();
        }

        private void SpawnPlayerIfNeeded(bool fromRespawn = false, Vector3? spawnPosition = null)
        {
            if (_playerCreature != null)
                return;

            var playerDna = GameManager.HasInstance
                ? GameManager.Instance.GetPlayerCreatureDNAOrDefault()
                : RoobsWorld.Creature.CreatureDNA.CreateDefault();

            if (string.IsNullOrWhiteSpace(playerDna.CreatureName))
                playerDna.CreatureName = "Player";

            Vector3 spawnPos = spawnPosition ?? Vector3.zero;
            if (_arena != null)
                spawnPos = _arena.SnapToIslandSurface(spawnPos);
            _playerCreature = RoobsWorld.Creature.CreatureFactory.Instance.SpawnCreature(playerDna, spawnPos, true);
            EnsurePlayerController(_playerCreature);

            if (GameManager.HasInstance)
                GameManager.Instance.SetPlayerCreatureDNA(playerDna, persistToDisk: false);

            ApplySpawnProtection(_playerCreature, fromRespawn);
            _respawnTimer = -1f;
        }

        private void InitializeUpgradeMilestone()
        {
            var dna = GameManager.HasInstance
                ? GameManager.Instance.GetPlayerCreatureDNAOrDefault()
                : _playerCreature?.GetComponent<RoobsWorld.Creature.CreatureInstance>()?.DNA;

            int evolutionLevel = RoobsWorld.Creature.CreatureUpgradeCatalog.GetEvolutionLevel(dna);
            _nextUpgradeDefeatMilestone = ComputeNextUpgradeMilestone(
                evolutionLevel,
                RoobsWorld.Creature.CreatureUpgradeCatalog.RivalDefeatsPerUpgrade);
            _upgradeChoiceOpen = false;
        }

        private void InitializeObjective()
        {
            var dna = GameManager.HasInstance
                ? GameManager.Instance.GetPlayerCreatureDNAOrDefault()
                : _playerCreature?.GetComponent<RoobsWorld.Creature.CreatureInstance>()?.DNA;
            _currentObjective = CreatureStageObjectivePlanner.CreateObjective(dna, _objectivesCompleted);
            _objectiveStartDefeatedCount = _encounterManager != null ? _encounterManager.DefeatedCount : 0;
            _objectiveStartAlphaDefeatedCount = _encounterManager != null ? _encounterManager.AlphaDefeatedCount : 0;
            _objectiveStartSpawnedCount = _encounterManager != null ? _encounterManager.TotalSpawnedRivals : 0;
            _objectiveStartedAt = Time.time;
            _objectiveCompleteAnnounced = false;
            _nextObjectiveTimer = -1f;
            _holdObjectiveSeconds = 0f;
            _playerInsideHoldGround = false;
            _objectivePacifyCount = 0;
            _encounterManager?.SetObjectiveFocus(_currentObjective);
            UpdateObjectiveMarker();
        }

        public static CreatureSpeciesDisposition EvaluateSpeciesDisposition(float relationshipScore)
        {
            if (relationshipScore <= -2f) return CreatureSpeciesDisposition.Hostile;
            if (relationshipScore < 0f) return CreatureSpeciesDisposition.Annoyed;
            if (relationshipScore < 1.5f) return CreatureSpeciesDisposition.Curious;
            if (relationshipScore < 3f) return CreatureSpeciesDisposition.Friendly;
            return CreatureSpeciesDisposition.Allied;
        }

        public bool TryGetSpeciesDispositionForRival(string creatureId, out CreatureSpeciesDisposition disposition)
        {
            disposition = CreatureSpeciesDisposition.Curious;
            if (_encounterManager == null || string.IsNullOrWhiteSpace(creatureId))
                return false;
            if (!_encounterManager.TryGetTrackedRivalLabel(creatureId, out string speciesLabel) || string.IsNullOrWhiteSpace(speciesLabel))
                return false;

            _speciesRelationshipScores.TryGetValue(speciesLabel, out float score);
            disposition = EvaluateSpeciesDisposition(score);
            return true;
        }

        public static float ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType feedbackType)
        {
            return feedbackType switch
            {
                CreatureCombatFeedbackType.Kill => -1f,
                CreatureCombatFeedbackType.Hit => -0.45f,
                CreatureCombatFeedbackType.Counter => 0.7f,
                CreatureCombatFeedbackType.Guard => 0.4f,
                _ => 0f
            };
        }

        private void EnsureArena()
        {
            if (_arena == null)
                _arena = FindAnyObjectByType<CreatureStageArena>();
            if (_arena == null)
            {
                var arenaRoot = new GameObject("CreatureArena");
                _arena = arenaRoot.AddComponent<CreatureStageArena>();
            }

            ApplySessionMapToArena(_arena);
            _arena.EnsureBuilt();
        }

        private static void ApplySessionMapToArena(CreatureStageArena arena)
        {
            if (arena == null || !GameManager.HasInstance)
                return;

            var session = GameManager.Instance.Session;
            if (session != null && session.TryGetSessionMap(out var map))
                arena.ApplyMapDefinition(map);
        }

        private void EnsureHud(GameObject playerCreature)
        {
            if (playerCreature == null)
                return;

            if (_hud == null)
                _hud = gameObject.GetComponent<CreatureStageHUD>() ?? gameObject.AddComponent<CreatureStageHUD>();

            var creature = playerCreature.GetComponent<RoobsWorld.Creature.CreatureInstance>();
            _hud.Initialize(creature);
        }

        private void EnsureEncounterManager(Transform playerTransform)
        {
            if (_encounterManager == null)
                _encounterManager = gameObject.GetComponent<CreatureStageEncounterManager>() ?? gameObject.AddComponent<CreatureStageEncounterManager>();

            if (_encounterManager.ActiveRivalCount <= 0 && _encounterManager.DefeatedCount <= 0)
                _encounterManager.Initialize(playerTransform);
            else
                _encounterManager.SetPlayerTarget(playerTransform);
            _encounterManager.SetArena(_arena);
        }

        private void UpdateEncounterProgress()
        {
            if (_encounterManager == null)
                return;

            int objectiveDefeatProgress = GetObjectiveDefeatProgress();
            if (_hud != null)
            {
                int active = _encounterManager.ActiveRivalCount;
                int streak = _encounterManager.KillStreakCount;
                float streakSecondsRemaining = _encounterManager.KillStreakSecondsRemaining;
                float guidanceDistance = GetObjectiveGuidanceDistance();
                int displayProgress = _currentObjective.UsesHoldTimer
                    ? GetHoldObjectivePressureProgress()
                    : objectiveDefeatProgress;
                int displayDefeated = _objectiveCompleteAnnounced
                    ? GetObjectiveDefeatTarget()
                    : displayProgress;
                _hud.SetEncounterProgress(displayDefeated, active, GetObjectiveDefeatTarget(), _desiredActiveRivals, streak);
                _hud.SetObjectiveStatus(
                    Localization.Get(_currentObjective.LocalizationKey),
                    _objectiveCompleteAnnounced
                        ? BuildObjectiveCooldownText(_nextObjectiveTimer)
                        : GetObjectiveProgressText(objectiveDefeatProgress, active, streak, streakSecondsRemaining),
                    _currentObjective.Type,
                    _playerInsideHoldGround,
                    !_objectiveCompleteAnnounced,
                    guidanceDistance);
            }

            if (_objectiveCompleteAnnounced)
                return;

            if (IsCurrentObjectiveComplete(objectiveDefeatProgress))
                CompleteCurrentObjective();
        }

        private void OfferUpgradeChoiceIfReady()
        {
            if (_upgradeChoiceOpen || _encounterManager == null || _playerCreature == null)
                return;
            if (_objectiveCompleteAnnounced)
                return;
            if (_encounterManager.DefeatedCount < _nextUpgradeDefeatMilestone)
                return;

            _upgradeChoiceOpen = true;
            CreatureUpgradeChoiceUI.Show(
                RoobsWorld.Creature.CreatureUpgradeCatalog.GetCoreOptions(),
                ApplyUpgradeChoice);
        }

        private void ApplyUpgradeChoice(RoobsWorld.Creature.CreatureUpgradeOption option)
        {
            _upgradeChoiceOpen = false;

            var player = _playerCreature != null
                ? _playerCreature.GetComponent<RoobsWorld.Creature.CreatureInstance>()
                : null;
            var dna = player?.DNA ?? (GameManager.HasInstance ? GameManager.Instance.GetPlayerCreatureDNAOrDefault() : null);
            if (dna == null)
                return;

            RoobsWorld.Creature.CreatureUpgradeCatalog.ApplyUpgrade(dna, option);
            var stats = RoobsWorld.Creature.StatCalculator.Calculate(
                dna,
                RoobsWorld.Creature.CreaturePartCatalog.GetDefaultCatalog());
            player?.ApplyEvolution(dna, stats);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetPlayerCreatureDNA(dna, persistToDisk: true);
                GameManager.Instance.SaveGame();
            }

            int evolutionLevel = RoobsWorld.Creature.CreatureUpgradeCatalog.GetEvolutionLevel(dna);
            _nextUpgradeDefeatMilestone = ComputeNextUpgradeMilestone(
                evolutionLevel,
                RoobsWorld.Creature.CreatureUpgradeCatalog.RivalDefeatsPerUpgrade);

            EventBus.Publish(new CreatureEvolvedEvent
            {
                CreatureId = dna.CreatureId,
                EvolutionLevel = evolutionLevel
            });

            AudioManager.Instance?.PlaySFX(SFXType.LevelUp);
            EventBus.Publish(new PlayerCharacterImprovedEvent
            {
                Title = option.Title,
                Description = Localization.Get("character.improved.creature"),
                StatSummary = string.Format(
                    Localization.Get("character.improved.creature_stats"),
                    option.HealthDelta,
                    option.SpeedDelta)
            });

        }

        private void HandlePlayerRespawn()
        {
            if (_playerCreature == null)
                return;

            var creature = _playerCreature.GetComponent<RoobsWorld.Creature.CreatureInstance>();
            if (creature == null || creature.IsAlive)
            {
                _respawnTimer = -1f;
                return;
            }

            if (_respawnTimer < 0f)
            {
                _respawnTimer = RespawnDelaySeconds;
                RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.player.down", 2.2f);
                return;
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer > 0f)
                return;

            if (_playerCreature != null)
                Destroy(_playerCreature);
            _playerCreature = null;

            Vector3 respawnPos = Vector3.zero;
            bool foundSafeRespawn = _encounterManager != null
                && _encounterManager.TryGetSafeRespawnPosition(Vector3.zero, out respawnPos);
            SpawnPlayerIfNeeded(fromRespawn: true, spawnPosition: respawnPos);
            if (foundSafeRespawn)
                RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.player.safe_respawn", 1.8f);

            if (_playerCreature != null)
            {
                CameraManager.Instance.SetFollowTarget(_playerCreature.transform);
                _encounterManager?.SetPlayerTarget(_playerCreature.transform);
                EnsureHud(_playerCreature);
            }
        }

        private void ApplySpawnProtection(GameObject playerCreature, bool showTip)
        {
            if (playerCreature == null)
                return;

            var creature = playerCreature.GetComponent<RoobsWorld.Creature.CreatureInstance>();
            if (creature == null)
                return;

            creature.GrantInvulnerability(RespawnProtectionSeconds);
            if (showTip)
                RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.player.shielded", 2.1f);
        }

        private static void EnsurePlayerController(GameObject playerCreature)
        {
            if (playerCreature == null)
                return;

            var creatureInstance = playerCreature.GetComponent<RoobsWorld.Creature.CreatureInstance>();

            var controller = playerCreature.GetComponent<CharacterController>();
            if (controller == null)
                controller = playerCreature.AddComponent<CharacterController>();

            float height = 1.8f;
            if (creatureInstance?.DNA != null)
            {
                float fromSpine = creatureInstance.DNA.Body.SpineSegments * 0.45f;
                height = Mathf.Clamp(fromSpine, 1.2f, 2.8f);
            }

            controller.height = height;
            controller.radius = Mathf.Clamp(height * 0.25f, 0.25f, 0.6f);
            controller.center = Vector3.up * (height * 0.5f);
            controller.stepOffset = Mathf.Min(0.4f, height * 0.3f);
            controller.slopeLimit = 50f;
            controller.minMoveDistance = 0f;

            if (playerCreature.GetComponent<CreaturePlayerController>() == null)
                playerCreature.AddComponent<CreaturePlayerController>();
        }

        /// <summary>
        /// Scales active rival target count with encounter progress.
        /// </summary>
        public static int ComputeDesiredActiveRivals(
            int defeatedRivals,
            int objectiveDefeats,
            int minActiveRivals,
            int maxActiveRivals)
        {
            int min = Mathf.Max(0, minActiveRivals);
            int max = Mathf.Max(min, maxActiveRivals);
            int defeated = Mathf.Max(0, defeatedRivals);
            int objective = Mathf.Max(1, objectiveDefeats);

            int tierCount = Mathf.Max(1, max - min + 1);
            int defeatsPerTier = Mathf.Max(1, Mathf.CeilToInt(objective / (float)tierCount));
            int tier = Mathf.Min(max - min, defeated / defeatsPerTier);

            return min + tier;
        }

        public static int GetActiveCreaturePlayerCount(MultiplayerSessionManager session)
        {
            return Mathf.Max(1, session?.PlayerCount ?? 1);
        }

        public static int ComputeMultiplayerScaledCount(int baseCount, int playerCount, int perExtraPlayer, int maxBonus)
        {
            int count = Mathf.Max(0, baseCount);
            int players = Mathf.Max(1, playerCount);
            int extras = players - 1;
            int bonus = Mathf.Clamp(extras * Mathf.Max(0, perExtraPlayer), 0, Mathf.Max(0, maxBonus));
            return count + bonus;
        }

        public static int ComputeNextUpgradeMilestone(int completedUpgradeCount, int defeatsPerUpgrade)
        {
            int completed = Mathf.Max(0, completedUpgradeCount);
            int step = Mathf.Max(1, defeatsPerUpgrade);
            return (completed + 1) * step;
        }

        public static int ComputeObjectiveDnaReward(CreatureStageObjective objective, int baseReward)
        {
            int reward = Mathf.Max(1, baseReward);
            return objective.Type switch
            {
                CreatureStageObjectiveType.AlphaHunt => reward + 6,
                CreatureStageObjectiveType.HoldFeedingGround => reward + 4,
                CreatureStageObjectiveType.PacifyRivals => reward + 5,
                _ => reward
            };
        }

        public static float ComputeObjectivePaceTargetSeconds(
            CreatureStageObjective objective,
            float defeatSecondsPerTarget,
            float holdGraceFraction)
        {
            if (objective.UsesHoldTimer)
                return Mathf.Max(0.1f, objective.HoldSecondsTarget) * (1f + Mathf.Max(0f, holdGraceFraction));

            return Mathf.Max(1, objective.RivalDefeatTarget) * Mathf.Max(1f, defeatSecondsPerTarget);
        }

        public static int ComputeObjectivePaceBonusDna(
            CreatureStageObjective objective,
            float completionSeconds,
            float defeatSecondsPerTarget,
            float holdGraceFraction,
            int maxBonus)
        {
            int cap = Mathf.Max(0, maxBonus);
            if (cap == 0)
                return 0;

            float targetSeconds = ComputeObjectivePaceTargetSeconds(objective, defeatSecondsPerTarget, holdGraceFraction);
            if (completionSeconds <= 0f)
                return cap;
            if (completionSeconds >= targetSeconds)
                return 0;

            float remainingFraction = Mathf.Clamp01(1f - (completionSeconds / targetSeconds));
            return Mathf.Clamp(Mathf.CeilToInt(cap * remainingFraction), 0, cap);
        }

        public static int ComputeObjectivePaceBonusDna(CreatureStageObjective objective, float completionSeconds)
        {
            return ComputeObjectivePaceBonusDna(
                objective,
                completionSeconds,
                BalanceConstants.CreatureObjectivePaceBonusSecondsPerDefeat,
                BalanceConstants.CreatureObjectivePaceBonusHoldGraceFraction,
                BalanceConstants.CreatureObjectivePaceBonusMaxDna);
        }

        public static float ComputeObjectiveMomentumDelaySeconds(
            float baseDelaySeconds,
            int completedObjectiveCount,
            float reductionPerClear,
            float minimumDelaySeconds)
        {
            float safeBase = Mathf.Max(0f, baseDelaySeconds);
            float safeMinimum = Mathf.Clamp(Mathf.Max(0f, minimumDelaySeconds), 0f, safeBase);
            float reduction = Mathf.Max(0, completedObjectiveCount) * Mathf.Max(0f, reductionPerClear);
            return Mathf.Max(safeMinimum, safeBase - reduction);
        }

        public static float ComputeObjectiveStaminaRestore(
            int paceBonusDna,
            float baseRestore,
            float paceBonusMultiplier,
            float maxRestore)
        {
            float restore = Mathf.Max(0f, baseRestore) + Mathf.Max(0, paceBonusDna) * Mathf.Max(0f, paceBonusMultiplier);
            return Mathf.Min(Mathf.Max(0f, maxRestore), restore);
        }

        public static float ComputeObjectiveStaminaRestore(int paceBonusDna)
        {
            return ComputeObjectiveStaminaRestore(
                paceBonusDna,
                BalanceConstants.CreatureObjectiveStaminaRestoreBase,
                BalanceConstants.CreatureObjectiveStaminaRestorePaceBonusMultiplier,
                BalanceConstants.CreatureObjectiveStaminaRestoreMax);
        }

        public static int ComputeObjectiveDefeatProgress(int totalDefeatedRivals, int objectiveStartDefeatedRivals)
        {
            return Mathf.Max(0, totalDefeatedRivals - Mathf.Max(0, objectiveStartDefeatedRivals));
        }

        public static int ComputeObjectiveAlphaProgress(int totalAlphaDefeatedRivals, int objectiveStartAlphaDefeatedRivals)
        {
            return Mathf.Max(0, totalAlphaDefeatedRivals - Mathf.Max(0, objectiveStartAlphaDefeatedRivals));
        }

        public static int ComputeHoldObjectivePressureProgress(float holdSeconds, float holdTargetSeconds, int pressureSteps)
        {
            int steps = Mathf.Max(1, pressureSteps);
            float target = Mathf.Max(0.1f, holdTargetSeconds);
            float normalized = Mathf.Clamp01(Mathf.Max(0f, holdSeconds) / target);
            return Mathf.Clamp(Mathf.FloorToInt(normalized * steps), 0, steps);
        }

        public static int ComputeObjectiveSpawnProgress(int totalSpawnedRivals, int objectiveStartSpawnedRivals)
        {
            return Mathf.Max(0, totalSpawnedRivals - Mathf.Max(0, objectiveStartSpawnedRivals));
        }

        public static float ComputeObjectiveCompletionSeconds(float currentTime, float objectiveStartedAt)
        {
            return Mathf.Max(0f, currentTime - Mathf.Max(0f, objectiveStartedAt));
        }

        public static string BuildObjectiveRewardLabel(CreatureStageObjective objective, int baseReward)
        {
            return string.Format(
                Localization.Get("creature.objective.reward"),
                ComputeObjectiveDnaReward(objective, baseReward));
        }

        public static string BuildObjectiveCooldownText(float secondsRemaining)
        {
            return string.Format(
                Localization.Get("creature.objective.next_timer"),
                Mathf.Max(0, Mathf.CeilToInt(secondsRemaining)));
        }

        public static string BuildHoldObjectiveProgressText(
            string rewardLabel,
            float holdSeconds,
            float holdTargetSeconds,
            int activeRivals,
            int desiredActiveRivals,
            int streak,
            bool isInsideHoldGround,
            float streakSecondsRemaining = 0f)
        {
            string stateKey = isInsideHoldGround
                ? "creature.objective.hold_state.secure"
                : "creature.objective.hold_state.return";
            string progress = string.Format(
                Localization.Get("creature.objective.hold_progress"),
                rewardLabel,
                Localization.Get(stateKey),
                Mathf.Max(0f, holdSeconds),
                Mathf.Max(1f, holdTargetSeconds),
                Mathf.Max(0, activeRivals),
                Mathf.Max(1, desiredActiveRivals),
                Mathf.Max(0, streak));
            return progress + BuildStreakTimerSuffix(streak, streakSecondsRemaining);
        }

        public static string BuildStreakTimerSuffix(int streak, float secondsRemaining)
        {
            if (streak <= 0 || secondsRemaining <= 0f)
                return string.Empty;

            return string.Format(
                Localization.Get("creature.streak.timer_suffix"),
                Mathf.Max(0, Mathf.CeilToInt(secondsRemaining)));
        }

        public static float ComputeHoldGroundDistanceToMarker(Vector3 playerPosition)
        {
            return ComputeDistanceOutsideRadius(
                playerPosition,
                Vector3.zero,
                CreatureStageObjectivePlanner.FeedingGroundRadius);
        }

        public static float ComputeDistanceOutsideRadius(Vector3 position, Vector3 center, float radius)
        {
            Vector2 pos = new Vector2(position.x, position.z);
            Vector2 target = new Vector2(center.x, center.z);
            return Mathf.Max(0f, Vector2.Distance(pos, target) - Mathf.Max(0f, radius));
        }

        private int GetObjectiveDefeatTarget()
        {
            return Mathf.Max(1, ComputeMultiplayerScaledCount(
                _currentObjective.RivalDefeatTarget,
                GetActiveCreaturePlayerCount(GameManager.HasInstance ? GameManager.Instance.Session : null),
                MultiplayerObjectiveDefeatsPerExtraPlayer,
                MultiplayerObjectiveDefeatsMaxBonus));
        }

        private int GetObjectivePopulationProgress()
        {
            if (_currentObjective.UsesHoldTimer)
                return GetHoldObjectivePressureProgress();

            return GetObjectiveDefeatProgress();
        }

        private int GetObjectiveDefeatProgress()
        {
            if (_currentObjective.Type == CreatureStageObjectiveType.AlphaHunt)
                return GetObjectiveAlphaProgress();
            if (_currentObjective.Type == CreatureStageObjectiveType.PacifyRivals)
                return Mathf.Max(0, _objectivePacifyCount);

            int totalDefeats = _encounterManager != null ? _encounterManager.DefeatedCount : 0;
            return ComputeObjectiveDefeatProgress(totalDefeats, _objectiveStartDefeatedCount);
        }

        private int GetObjectiveAlphaProgress()
        {
            int totalAlphaDefeats = _encounterManager != null ? _encounterManager.AlphaDefeatedCount : 0;
            return ComputeObjectiveAlphaProgress(totalAlphaDefeats, _objectiveStartAlphaDefeatedCount);
        }

        private int GetHoldObjectivePressureProgress()
        {
            return ComputeHoldObjectivePressureProgress(
                _holdObjectiveSeconds,
                _currentObjective.HoldSecondsTarget,
                GetObjectiveDefeatTarget());
        }

        private int GetObjectiveSpawnProgress()
        {
            int totalSpawned = _encounterManager != null ? _encounterManager.TotalSpawnedRivals : 0;
            return ComputeObjectiveSpawnProgress(totalSpawned, _objectiveStartSpawnedCount);
        }

        private void UpdateObjectiveTimer()
        {
            if (!_currentObjective.UsesHoldTimer || _objectiveCompleteAnnounced || _playerCreature == null)
                return;

            _playerInsideHoldGround = IsPlayerInsideCurrentHoldGround();
            if (_playerInsideHoldGround)
                _holdObjectiveSeconds += Time.deltaTime;
        }

        private bool IsPlayerInsideCurrentHoldGround()
        {
            return _currentObjective.UsesHoldTimer &&
                _playerCreature != null &&
                CreatureStageObjectivePlanner.IsInsideFeedingGround(_playerCreature.transform.position);
        }

        private void UpdateNextObjectiveTimer()
        {
            if (!_objectiveCompleteAnnounced || _nextObjectiveTimer < 0f)
                return;

            _nextObjectiveTimer -= Time.deltaTime;
            if (_nextObjectiveTimer > 0f)
                return;

            InitializeObjective();
            RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.objective.next", 2f);
        }

        private void CompleteCurrentObjective()
        {
            _objectiveCompleteAnnounced = true;
            _objectivesCompleted++;
            _nextObjectiveTimer = ComputeObjectiveMomentumDelaySeconds(
                NextObjectiveDelaySeconds,
                _objectivesCompleted,
                BalanceConstants.CreatureObjectiveMomentumDelayReductionPerClear,
                BalanceConstants.CreatureObjectiveMomentumMinimumDelaySeconds);
            float completionSeconds = ComputeObjectiveCompletionSeconds(Time.time, _objectiveStartedAt);
            int paceBonus = ComputeObjectivePaceBonusDna(_currentObjective, completionSeconds);
            int dnaReward = ComputeObjectiveDnaReward(
                _currentObjective,
                BalanceConstants.CreatureDNAPointsObjectiveComplete)
                + paceBonus;
            float staminaRestore = ComputeObjectiveStaminaRestore(paceBonus);

            EventBus.Publish(new CreatureObjectiveCompletedEvent
            {
                ObjectiveType = _currentObjective.Type,
                ObjectiveLocalizationKey = _currentObjective.LocalizationKey,
                DnaReward = dnaReward,
                PaceBonusDna = paceBonus,
                CompletedObjectiveCount = _objectivesCompleted,
                CompletionSeconds = completionSeconds,
                KillStreak = _encounterManager != null ? _encounterManager.KillStreakCount : 0
            });

            RestorePlayerStaminaForObjective(staminaRestore);
            RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.encounter.complete", 5f);
            if (!GameManager.HasInstance)
                return;

            GameManager.Instance.AddDNAPoints(dnaReward);

            if (!GameManager.Instance.Achievements.IsUnlocked("creature_pack_breaker"))
                GameManager.Instance.UnlockAchievement("creature_pack_breaker");

            GameManager.Instance.SaveGame();
        }

        private void RestorePlayerStaminaForObjective(float amount)
        {
            if (_playerCreature == null)
                return;

            var controller = _playerCreature.GetComponent<CreaturePlayerController>();
            controller?.RestoreStaminaFromObjective(amount);
        }

        private bool IsCurrentObjectiveComplete(int defeatedRivals)
        {
            if (_currentObjective.UsesHoldTimer)
                return _holdObjectiveSeconds >= _currentObjective.HoldSecondsTarget;
            return defeatedRivals >= GetObjectiveDefeatTarget();
        }

        private float GetObjectiveGuidanceDistance()
        {
            if (_objectiveCompleteAnnounced)
                return -1f;

            if (_currentObjective.Type == CreatureStageObjectiveType.HoldFeedingGround)
            {
                if (_playerCreature == null || _playerInsideHoldGround)
                    return -1f;

                return ComputeHoldGroundDistanceToMarker(_playerCreature.transform.position);
            }

            if (_encounterManager == null)
                return -1f;

            if (_currentObjective.Type == CreatureStageObjectiveType.PacifyRivals)
                return _encounterManager.GetNearestTrackedRivalDistance(false);

            return _encounterManager.GetNearestTrackedRivalDistance(_currentObjective.Type == CreatureStageObjectiveType.AlphaHunt);
        }

        private string GetObjectiveProgressText(int defeatedRivals, int activeRivals, int streak, float streakSecondsRemaining)
        {
            string rewardLabel = BuildObjectiveRewardLabel(
                _currentObjective,
                BalanceConstants.CreatureDNAPointsObjectiveComplete);
            if (_currentObjective.UsesHoldTimer)
            {
                return BuildHoldObjectiveProgressText(
                    rewardLabel,
                    _holdObjectiveSeconds,
                    _currentObjective.HoldSecondsTarget,
                    activeRivals,
                    Mathf.Max(1, _desiredActiveRivals),
                    streak,
                    _playerInsideHoldGround,
                    streakSecondsRemaining);
            }

            if (_currentObjective.Type == CreatureStageObjectiveType.AlphaHunt)
            {
                string alphaProgress = string.Format(
                    Localization.Get("creature.objective.alpha_progress"),
                    rewardLabel,
                    defeatedRivals,
                    GetObjectiveDefeatTarget(),
                    activeRivals,
                    Mathf.Max(1, _desiredActiveRivals),
                    streak);
                return alphaProgress + BuildStreakTimerSuffix(streak, streakSecondsRemaining);
            }
            if (_currentObjective.Type == CreatureStageObjectiveType.PacifyRivals)
            {
                string pacifyProgress = string.Format(
                    Localization.Get("creature.objective.pacify_progress"),
                    rewardLabel,
                    defeatedRivals,
                    GetObjectiveDefeatTarget(),
                    activeRivals,
                    Mathf.Max(1, _desiredActiveRivals),
                    streak);
                return pacifyProgress + BuildStreakTimerSuffix(streak, streakSecondsRemaining);
            }

            string rivalProgress = string.Format(
                Localization.Get("creature.encounter.progress"),
                defeatedRivals,
                GetObjectiveDefeatTarget(),
                activeRivals,
                Mathf.Max(1, _desiredActiveRivals),
                streak);
            return $"{rewardLabel}   {rivalProgress}{BuildStreakTimerSuffix(streak, streakSecondsRemaining)}";
        }

        private void UpdateObjectiveMarker()
        {
            if (!_currentObjective.UsesHoldTimer)
            {
                if (_objectiveMarker != null)
                    _objectiveMarker.SetActive(false);
                return;
            }

            if (_objectiveMarker == null)
            {
                _objectiveMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _objectiveMarker.name = "HoldFeedingGroundMarker";
                var collider = _objectiveMarker.GetComponent<Collider>();
                if (collider != null)
                    collider.isTrigger = true;
                var renderer = _objectiveMarker.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial.color = new Color(0.12f, 0.72f, 0.62f, 0.42f);
            }

            float radius = CreatureStageObjectivePlanner.FeedingGroundRadius;
            _objectiveMarker.SetActive(true);
            Vector3 markerPos = new Vector3(0f, 0.08f, 0f);
            if (_arena != null)
                markerPos = _arena.SnapToIslandSurface(Vector3.zero) + new Vector3(0f, 0.08f, 0f);
            _objectiveMarker.transform.position = markerPos;
            _objectiveMarker.transform.localScale = new Vector3(radius * 2f, 0.08f, radius * 2f);
        }

        private void OnCombatFeedback(CreatureCombatFeedbackEvent evt)
        {
            if (_objectiveCompleteAnnounced || _currentObjective.Type != CreatureStageObjectiveType.PacifyRivals)
            {
                TrackSpeciesRelationship(evt);
                return;
            }

            bool playerSourcedControl = evt.IsPlayerSource
                && (evt.Type == CreatureCombatFeedbackType.Guard || evt.Type == CreatureCombatFeedbackType.Counter);
            if (playerSourcedControl)
            {
                _objectivePacifyCount = Mathf.Min(
                    GetObjectiveDefeatTarget(),
                    _objectivePacifyCount + 1);
            }

            TrackSpeciesRelationship(evt);
        }

        private void OnRivalDefeated(CreatureRivalDefeatedEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.ArchetypeName))
                return;

            ApplySpeciesRelationshipDelta(evt.ArchetypeName, -0.75f);
        }

        private void TrackSpeciesRelationship(CreatureCombatFeedbackEvent evt)
        {
            if (!evt.IsPlayerSource || _encounterManager == null)
                return;

            float delta = ComputeSpeciesRelationshipDelta(evt.Type);
            if (Mathf.Abs(delta) <= 0.001f)
                return;

            if (!TryResolveSpeciesLabel(evt, out string speciesLabel))
                return;

            ApplySpeciesRelationshipDelta(speciesLabel, delta);
        }

        private bool TryResolveSpeciesLabel(CreatureCombatFeedbackEvent evt, out string speciesLabel)
        {
            speciesLabel = string.Empty;
            if (_encounterManager == null)
                return false;

            if (!string.IsNullOrWhiteSpace(evt.TargetCreatureId) &&
                _encounterManager.TryGetTrackedRivalLabel(evt.TargetCreatureId, out speciesLabel))
                return true;

            Vector3 reference = _playerCreature != null ? _playerCreature.transform.position : evt.Position;
            return _encounterManager.TryGetNearestTrackedRivalLabel(reference, out speciesLabel);
        }

        private void ApplySpeciesRelationshipDelta(string speciesLabel, float delta)
        {
            if (string.IsNullOrWhiteSpace(speciesLabel) || Mathf.Abs(delta) <= 0.001f)
                return;

            _speciesRelationshipScores.TryGetValue(speciesLabel, out float oldScore);
            var oldDisposition = EvaluateSpeciesDisposition(oldScore);
            float newScore = Mathf.Clamp(oldScore + delta, -4f, 4f);
            _speciesRelationshipScores[speciesLabel] = newScore;
            var newDisposition = EvaluateSpeciesDisposition(newScore);

            if (newDisposition == oldDisposition)
                return;

            EventBus.Publish(new CreatureSpeciesRelationshipChangedEvent
            {
                SpeciesLabel = speciesLabel,
                PreviousDisposition = oldDisposition,
                NewDisposition = newDisposition,
                Score = newScore,
                Delta = delta
            });
        }
    }
}
