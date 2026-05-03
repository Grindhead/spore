using RoobsWorld.Core;
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

        private GameObject _playerCreature;
        private CreatureStageHUD _hud;
        private CreatureStageEncounterManager _encounterManager;
        private CreatureStageArena _arena;
        private float _respawnTimer = -1f;
        private bool _objectiveCompleteAnnounced;
        private int _desiredActiveRivals = MinDesiredActiveRivals;
        private int _nextUpgradeDefeatMilestone = RoobsWorld.Creature.CreatureUpgradeCatalog.RivalDefeatsPerUpgrade;
        private bool _upgradeChoiceOpen;

        public GameStageType StageType => GameStageType.Creature;

        private void Start()
        {
            StageManager.Instance.RegisterStage(this);
        }

        private void OnDestroy()
        {
            if (StageManager.HasInstance)
                StageManager.Instance.UnregisterStage(GameStageType.Creature);
        }

        public void OnEnter()
        {
            _objectiveCompleteAnnounced = false;
            _desiredActiveRivals = MinDesiredActiveRivals;
            EnsureArena();
            SpawnPlayerIfNeeded();
            InitializeUpgradeMilestone();
            CameraManager.Instance.ActivateRig(CameraRigType.ThirdPerson);
            if (_playerCreature != null)
            {
                CameraManager.Instance.SetFollowTarget(_playerCreature.transform);
                EnsureEncounterManager(_playerCreature.transform);
            }
        }

        public void OnExit()
        {
            CameraManager.Instance.SetFollowTarget(null);
            _hud?.SetVisible(false);
        }

        public void OnUpdate()
        {
            if (_encounterManager != null && !_objectiveCompleteAnnounced)
            {
                int desiredActive = ComputeDesiredActiveRivals(
                    _encounterManager.DefeatedCount,
                    ObjectiveDefeats,
                    MinDesiredActiveRivals,
                    MaxDesiredActiveRivals);
                _desiredActiveRivals = desiredActive;
                _encounterManager.UpdatePopulation(Time.deltaTime, desiredActive, ObjectiveDefeats);
            }

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

        private void EnsureArena()
        {
            if (_arena == null)
                _arena = FindAnyObjectByType<CreatureStageArena>();
            if (_arena == null)
            {
                var arenaRoot = new GameObject("CreatureArena");
                _arena = arenaRoot.AddComponent<CreatureStageArena>();
            }

            _arena.EnsureBuilt();
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
        }

        private void UpdateEncounterProgress()
        {
            if (_encounterManager == null)
                return;

            if (_hud != null)
            {
                int displayDefeated = _encounterManager.DefeatedCount;
                int active = _encounterManager.ActiveRivalCount;
                int streak = _encounterManager.KillStreakCount;
                _hud.SetEncounterProgress(displayDefeated, active, ObjectiveDefeats, _desiredActiveRivals, streak);
            }

            int defeated = _encounterManager.DefeatedCount;

            if (!_objectiveCompleteAnnounced && defeated >= ObjectiveDefeats)
            {
                _objectiveCompleteAnnounced = true;

                bool alreadyRewarded = GameManager.HasInstance
                    && GameManager.Instance.Achievements.IsUnlocked("creature_pack_breaker");
                if (alreadyRewarded)
                    return;

                RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.encounter.complete", 5f);
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.AddDNAPoints(BalanceConstants.CreatureDNAPointsObjectiveComplete);
                    GameManager.Instance.UnlockAchievement("creature_pack_breaker");
                    GameManager.Instance.SaveGame();
                }
            }
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
            ApplyUpgradeChoice(RoobsWorld.Creature.CreatureUpgradeCatalog.GetCoreOption());
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

        public static int ComputeNextUpgradeMilestone(int completedUpgradeCount, int defeatsPerUpgrade)
        {
            int completed = Mathf.Max(0, completedUpgradeCount);
            int step = Mathf.Max(1, defeatsPerUpgrade);
            return (completed + 1) * step;
        }
    }
}
