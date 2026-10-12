using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RoobsWorld.Multiplayer;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Global game state singleton responsible for save data, stage transitions, pause state, DNA currency, and achievements.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        private SaveData _currentSave;
        private bool _isPaused;
        private AchievementTracker _achievementTracker;
        private IPlatformAchievementProvider _platformAchievementProvider;
        private IPlatformUserProvider _platformUserProvider;
        private IPlatformRichPresenceProvider _platformRichPresenceProvider;

        /// <summary>
        /// The active multiplayer session, or null when playing offline.
        /// Set via <see cref="SetSession"/> before a stage transition to apply host-authority rules.
        /// </summary>
        public MultiplayerSessionManager Session         { get; private set; }

        /// <summary>
        /// The network transport backing the active session, or null when offline.
        /// Passed to <see cref="RoobsWorld.Multiplayer.NetworkTickManager"/> so it can send/receive packets.
        /// </summary>
        public INetworkTransport SessionTransport { get; private set; }

        /// <summary>Replaces the active session. Pass null to return to offline (always-authoritative) mode.</summary>
        public void SetSession(MultiplayerSessionManager session)
        {
            Session = session;
            if (session == null)
                SessionTransport = null;
        }

        /// <summary>Sets both the session and its transport together (typical call from lobby UI after connect).</summary>
        public void SetSession(MultiplayerSessionManager session, INetworkTransport transport)
        {
            Session          = session;
            SessionTransport = transport;
        }

        public SaveData CurrentSave => _currentSave;
        public bool IsPaused => _isPaused;
        public AchievementTracker Achievements => _achievementTracker ??= new AchievementTracker();
        public IPlatformUserProvider PlatformUser => _platformUserProvider;
        /// <summary>Baseline balance multipliers used by every run.</summary>
        public GameBalanceModifiers Modifiers => GameBalanceModifiers.Default;

        /// <summary>
        /// Initializes global runtime settings and ensures achievement tracker exists before a save is loaded.
        /// </summary>
        protected override void OnInitialize()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            if (_achievementTracker == null)
                _achievementTracker = new AchievementTracker();
        }

        /// <summary>
        /// Creates a new save using the given world seed (or a timestamp-based seed when zero) and transitions to the Cell stage.
        /// </summary>
        /// <param name="seed">World seed to use for deterministic generation; 0 generates a new seed.</param>
        public void StartNewGame(long seed = 0)
        {
            if (seed == 0)
                seed = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Carry forward only current-build compatible data.
            int carried = _currentSave?.DiscoveredTraitFlags ?? 0;
            string carriedCreatureDna = null;
            if (!string.IsNullOrWhiteSpace(_currentSave?.CreatureDNAJson))
            {
                try
                {
                    var dna = RoobsWorld.Creature.CreatureDNA.FromJson(_currentSave.CreatureDNAJson);
                    if (TryValidateCreatureDNA(dna, out string validationError))
                        carriedCreatureDna = _currentSave.CreatureDNAJson;
                    else
                        Debug.LogError($"[GameManager] Dropping incompatible CreatureDNAJson while starting new game: {validationError}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameManager] Dropping unparsable CreatureDNAJson while starting new game: {ex.Message}");
                }
            }
            _currentSave = SaveManager.Instance.CreateNewSave(seed);
            _currentSave.DiscoveredTraitFlags = carried;
            _currentSave.CreatureDNAJson = !string.IsNullOrWhiteSpace(carriedCreatureDna)
                ? carriedCreatureDna
                : RoobsWorld.Creature.CreatureDNA.CreateDefault().ToJson();
            _achievementTracker = new AchievementTracker();
            StageManager.Instance.TransitionTo(GameStageType.Cell);
            SaveGame();
        }

        /// <summary>
        /// Loads a save from the given slot name (or default slot) and transitions to the saved stage.
        /// Falls back to starting a new game when no save exists.
        /// </summary>
        /// <param name="slotName">Optional named save slot.</param>
        public void LoadGame(string slotName = null)
        {
            // Local-only and synchronous: never waits on the network.
            ApplyLoadedSave(SaveManager.Instance.Load(slotName));
        }

        private CancellationTokenSource _loadCts;

        /// <summary>True while <see cref="LoadGameAsync"/> is waiting for the cloud.</summary>
        public bool IsLoadingSave => _loadCts != null;

        /// <summary>
        /// Loads a slot, consulting the cloud copy without blocking the main thread.
        /// Shows a cancellable loading overlay; cancelling, a timeout or a network error
        /// fall back to the newest valid local save (or a new game when none exists).
        /// </summary>
        public async Task LoadGameAsync(string slotName = null)
        {
            if (_loadCts != null)
                return;

            var cts = new CancellationTokenSource();
            _loadCts = cts;
            EventBus.Publish(new SaveLoadStateEvent { IsLoading = true });
            SaveData save;
            try
            {
                save = await SaveManager.Instance.LoadAsync(slotName, cts.Token);
            }
            finally
            {
                _loadCts = null;
                cts.Dispose();
                EventBus.Publish(new SaveLoadStateEvent { IsLoading = false });
            }

            if (this == null)
                return; // destroyed while awaiting
            ApplyLoadedSave(save);
        }

        /// <summary>Cancels an in-flight <see cref="LoadGameAsync"/>; it then continues with local data.</summary>
        public void CancelLoadGame()
        {
            _loadCts?.Cancel();
        }

        private void ApplyLoadedSave(SaveData save)
        {
            if (save == null)
            {
                Debug.LogWarning("[GameManager] No save data found, starting new game.");
                StartNewGame();
                return;
            }

            _currentSave = save;
            _achievementTracker = AchievementTracker.FromJson(save.AchievementJson);

            var targetStage = StageManager.NormalizeRequestedStage(save.CurrentStage);
            _currentSave.CurrentStage = targetStage;
            StageManager.Instance.TransitionTo(targetStage);
        }

        /// <summary>
        /// Persists the current save (including current stage and achievement state) to disk.
        /// </summary>
        /// <param name="slotName">Optional named save slot.</param>
        public void SaveGame(string slotName = null)
        {
            if (_currentSave == null) return;
            _currentSave.CurrentStage = StageManager.Instance.CurrentStageType;
            _currentSave.AchievementJson = (_achievementTracker ?? new AchievementTracker()).ToJson();
            SaveManager.Instance.Save(_currentSave, slotName);
        }

        /// <summary>
        /// Sets the global paused state, updates <see cref="Time.timeScale"/>, and publishes a <see cref="PauseEvent"/>.
        /// </summary>
        /// <param name="paused">Whether the game should be paused.</param>
        /// <param name="suppressPauseMenu">Whether pause UI should stay hidden for modal gameplay screens.</param>
        public void SetPaused(bool paused, bool suppressPauseMenu = false)
        {
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            EventBus.Publish(new PauseEvent { IsPaused = paused, SuppressPauseMenu = suppressPauseMenu });
        }

        /// <summary>
        /// Toggles the global paused state and publishes a corresponding <see cref="PauseEvent"/>.
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!_isPaused);
        }

        /// <summary>
        /// Genetic traits the player has ever unlocked across all runs (meta-progression).
        /// Carried forward into each new game so known traits appear in mutation choices.
        /// </summary>
        public RoobsWorld.Stages.Cell.GeneticTraits DiscoveredTraits =>
            _currentSave != null
                ? (RoobsWorld.Stages.Cell.GeneticTraits)_currentSave.DiscoveredTraitFlags
                : RoobsWorld.Stages.Cell.GeneticTraits.None;

        /// <summary>
        /// Records newly discovered traits into the persistent meta-progression flags.
        /// Only traits not previously seen are stored; call after each mutation choice.
        /// </summary>
        public void RecordTraitDiscovery(RoobsWorld.Stages.Cell.GeneticTraits traits)
        {
            if (_currentSave == null || traits == RoobsWorld.Stages.Cell.GeneticTraits.None) return;
            int before = _currentSave.DiscoveredTraitFlags;
            _currentSave.DiscoveredTraitFlags |= (int)traits;
            if (_currentSave.DiscoveredTraitFlags != before)
                SaveGame();
        }

        /// <summary>
        /// Adds DNA points to the current save and publishes a <see cref="DNAPointsChangedEvent"/>.
        /// No-op when no save is loaded.
        /// </summary>
        /// <param name="amount">Amount of DNA to add (can be negative for costs).</param>
        public void AddDNAPoints(int amount)
        {
            if (_currentSave == null) return;
            int old = _currentSave.DNAPoints;
            _currentSave.DNAPoints += amount;
            EventBus.Publish(new DNAPointsChangedEvent
            {
                OldAmount = old,
                NewAmount = _currentSave.DNAPoints
            });
            SaveGame();
        }

        /// <summary>
        /// Sets the platform achievement provider (Steam, Xbox, PlayStation, Nintendo). Null = local-only; no report to platform.
        /// </summary>
        public void SetPlatformAchievementProvider(IPlatformAchievementProvider provider)
        {
            _platformAchievementProvider = provider;
        }

        public void SetPlatformUserProvider(IPlatformUserProvider provider)
        {
            _platformUserProvider = provider;
        }

        public void SetPlatformRichPresenceProvider(IPlatformRichPresenceProvider provider)
        {
            _platformRichPresenceProvider = provider;
            UpdatePlatformRichPresence(StageManager.HasInstance ? StageManager.Instance.CurrentStageType : GameStageType.None);
        }

        public void UpdatePlatformRichPresence(GameStageType stage)
        {
            if (_platformRichPresenceProvider == null || !_platformRichPresenceProvider.IsAvailable)
                return;

            _platformRichPresenceProvider.SetRichPresence(stage);
        }

        /// <summary>
        /// Attempts to unlock an achievement in the current <see cref="AchievementTracker"/> and reports to platform when available.
        /// </summary>
        /// <param name="id">Achievement identifier defined by <see cref="AchievementService"/>.</param>
        public void UnlockAchievement(string id)
        {
            bool hadBefore = Achievements.IsUnlocked(id);
            UnlockAchievementCore(Achievements, _platformAchievementProvider, id);
            if (!hadBefore && Achievements.IsUnlocked(id))
                SaveGame();
        }

        /// <summary>
        /// Core unlock logic: update tracker then report to platform on success. Used by instance and by tests.
        /// </summary>
        public static void UnlockAchievementCore(AchievementTracker tracker, IPlatformAchievementProvider provider, string id)
        {
            if (tracker == null) return;
            bool unlocked = tracker.TryUnlock(id);
            if (unlocked)
                provider?.ReportUnlock(id);
        }

        /// <summary>
        /// Returns the creature DNA currently stored in save data, or null when none is available/valid.
        /// </summary>
        public RoobsWorld.Creature.CreatureDNA GetPlayerCreatureDNA()
        {
            if (_currentSave == null || string.IsNullOrWhiteSpace(_currentSave.CreatureDNAJson))
                return null;

            try
            {
                var dna = RoobsWorld.Creature.CreatureDNA.FromJson(_currentSave.CreatureDNAJson);
                if (!TryValidateCreatureDNA(dna, out string validationError))
                {
                    Debug.LogError($"[GameManager] Invalid CreatureDNAJson: {validationError}");
                    return null;
                }

                return dna;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Failed to parse CreatureDNAJson: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates parsed creature DNA without logging. Use <see cref="GetPlayerCreatureDNA"/> for saved-data loading.
        /// </summary>
        public static bool TryValidateCreatureDNA(RoobsWorld.Creature.CreatureDNA dna, out string error)
        {
            if (dna == null)
            {
                error = "deserialization returned no data.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dna.CreatureId))
            {
                error = "missing creature id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dna.CreatureName))
            {
                error = "missing creature name.";
                return false;
            }

            if (dna.Body == null)
            {
                error = "missing body.";
                return false;
            }

            if (dna.Body.SpineSegments <= 0)
            {
                error = $"invalid spine segment count: {dna.Body.SpineSegments}.";
                return false;
            }

            if (!System.Enum.IsDefined(typeof(RoobsWorld.Creature.SymmetryType), dna.Body.Symmetry))
            {
                error = $"invalid symmetry value: {dna.Body.Symmetry}.";
                return false;
            }

            if (dna.Body.BodyScale.x <= 0f || dna.Body.BodyScale.y <= 0f || dna.Body.BodyScale.z <= 0f)
            {
                error = $"invalid body scale: {dna.Body.BodyScale}.";
                return false;
            }

            if (dna.Parts == null)
            {
                error = "missing parts list.";
                return false;
            }

            if (dna.Parts.Count == 0)
            {
                error = "missing creature parts.";
                return false;
            }

            int maxBone = dna.Body.SpineSegments - 1;
            for (int i = 0; i < dna.Parts.Count; i++)
            {
                var part = dna.Parts[i];
                if (part == null)
                {
                    error = $"parts[{i}] is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(part.PartId))
                {
                    error = $"parts[{i}] is missing part id.";
                    return false;
                }

                if (part.AttachBone < 0 || part.AttachBone > maxBone)
                {
                    error = $"parts[{i}] attach bone {part.AttachBone} is outside body range 0..{maxBone}.";
                    return false;
                }

                if (part.Scale <= 0f)
                {
                    error = $"parts[{i}] has invalid scale: {part.Scale}.";
                    return false;
                }
            }

            if (dna.Upgrades == null)
            {
                error = "missing upgrades list.";
                return false;
            }

            for (int i = 0; i < dna.Upgrades.Count; i++)
            {
                var upgrade = dna.Upgrades[i];
                if (upgrade == null)
                {
                    error = $"upgrades[{i}] is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(upgrade.UpgradeId))
                {
                    error = $"upgrades[{i}] is missing upgrade id.";
                    return false;
                }
            }

            if (dna.Colors == null)
            {
                error = "missing color palette.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Returns the saved creature DNA or creates/stores a default starter DNA when missing.
        /// </summary>
        public RoobsWorld.Creature.CreatureDNA GetPlayerCreatureDNAOrDefault()
        {
            var dna = GetPlayerCreatureDNA();
            if (dna != null)
                return dna;

            var starter = RoobsWorld.Creature.CreatureDNA.CreateDefault();
            SetPlayerCreatureDNA(starter, persistToDisk: false);
            return starter;
        }

        /// <summary>
        /// Updates creature DNA stored in save data for the current run.
        /// Optionally writes immediately to disk.
        /// </summary>
        public void SetPlayerCreatureDNA(RoobsWorld.Creature.CreatureDNA dna, bool persistToDisk = false)
        {
            if (dna == null)
                return;

            if (!TryValidateCreatureDNA(dna, out string validationError))
            {
                Debug.LogError($"[GameManager] Refusing to store incompatible CreatureDNA: {validationError}");
                return;
            }

            if (_currentSave == null)
            {
                _currentSave = new SaveData
                {
                    CurrentStage = GameStageType.Cell,
                    WorldSeed = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            _currentSave.CreatureDNAJson = dna.ToJson();

            if (persistToDisk)
                SaveManager.Instance.Save(_currentSave);
            else
                SaveGame();
        }

        // ---- Run Records (meta-progression) ----

        /// <summary>
        /// Returns the current run record (personal bests) from the active save, or an empty record
        /// when no save is loaded.
        /// </summary>
        public RunRecord GetRunRecord()
        {
            return _currentSave != null
                ? RunRecord.FromJson(_currentSave.RunRecordJson)
                : RunRecord.Empty();
        }

        /// <summary>
        /// Updates personal bests with the stats from the just-finished run, persists them to the
        /// active save, and returns flags indicating which records (if any) were broken.
        /// </summary>
        public NewBestFlags UpdateRunRecord(float timeSurvived, float peakMass, int dnaEarned,
                                            int speciesCount, int traitCount)
        {
            if (_currentSave == null) return default;
            var record = RunRecord.FromJson(_currentSave.RunRecordJson);
            var flags  = record.Update(timeSurvived, peakMass, dnaEarned, speciesCount, traitCount);
            _currentSave.RunRecordJson = record.ToJson();
            // Persist immediately so records survive even if the player force-quits
            SaveManager.Instance?.Save(_currentSave);
            return flags;
        }

        /// <summary>
        /// Increments the total-leviathans-survived counter in the run record.
        /// </summary>
        public void RecordLeviathanSurvived()
        {
            if (_currentSave == null) return;
            var record = RunRecord.FromJson(_currentSave.RunRecordJson);
            record.TotalLeviathansSurvived++;
            _currentSave.RunRecordJson = record.ToJson();
            SaveGame();
        }

        /// <summary>
        /// Increments the total-leviathans-defeated counter in the run record.
        /// </summary>
        public void RecordLeviathanDefeated()
        {
            if (_currentSave == null) return;
            var record = RunRecord.FromJson(_currentSave.RunRecordJson);
            record.TotalLeviathanDefeated++;
            _currentSave.RunRecordJson = record.ToJson();
            SaveGame();
        }

        /// <summary>
        /// Quits play mode in the editor or closes the application in a build.
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
