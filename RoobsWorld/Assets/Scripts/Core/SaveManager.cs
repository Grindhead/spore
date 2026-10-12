using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Serializable snapshot of player progress, world seed, currency, and achievements.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const string CurrentVersion = "0.1.0";
        public string Version = CurrentVersion;
        public GameStageType CurrentStage = GameStageType.Cell;
        public long WorldSeed;
        public int DNAPoints;
        public string CreatureDNAJson = RoobsWorld.Creature.CreatureDNA.CreateDefault().ToJson();
        public string AchievementJson = new AchievementTracker().ToJson();
        public string Timestamp = DateTime.UtcNow.ToString("o");
        /// <summary>
        /// Bitmask of <see cref="RoobsWorld.Stages.Cell.GeneticTraits"/> the player has ever unlocked
        /// across all runs. Carried forward into each new run so known traits are guaranteed to appear
        /// in at least one mutation-choice slot. Zero = no discoveries yet.
        /// </summary>
        public int DiscoveredTraitFlags = 0;

        /// <summary>
        /// JSON-serialized <see cref="RunRecord"/> tracking personal bests across all cell-stage runs.
        /// A zeroed record means no completed runs yet.
        /// </summary>
        public string RunRecordJson = RunRecord.Empty().ToJson();
    }

    /// <summary>
    /// Handles creation, persistence, and loading of <see cref="SaveData"/>.
    ///
    /// Storage policy (see <see cref="SaveCandidateSelector"/> for the load decision):
    ///   - Every save is written locally first (atomic replace, previous copy kept as
    ///     <c>.bak</c>), then uploaded to the per-player cloud slot in the background.
    ///   - Slots are local files; <see cref="GetAllSaveSlots"/> and <see cref="SaveExists"/>
    ///     report local state. A cloud-only slot is pulled down the first time it is loaded.
    ///   - <see cref="DeleteSave"/> removes the local files, leaves a local tombstone and
    ///     deletes the cloud slot (server tombstone), so a deleted slot cannot reappear.
    ///   - <see cref="LoadAsync"/> never blocks the main thread; when the cloud is slow,
    ///     unreachable or the caller cancels, the newest valid local copy is used.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string AUTO_SAVE_NAME = "autosave";
        private const string LocalSaveDirectoryName = "saves";
        private const string SaveFileExtension = ".json";
        private const string BackupExtension = ".bak";
        private const string TombstoneExtension = ".deleted";
        private const string TempExtension = ".tmp";
        /// <summary>How long <see cref="LoadAsync"/> waits for the cloud before using local data.</summary>
        public static readonly TimeSpan CloudLoadTimeout = TimeSpan.FromSeconds(4);

        protected override void OnInitialize() { }

        /// <summary>
        /// Writes <paramref name="data"/> locally and queues a cloud upload for the specified slot.
        /// </summary>
        public void Save(SaveData data, string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            data.Timestamp = DateTime.UtcNow.ToString("o");

            try
            {
                string localPath = SaveLocal(data, slotName);
                ServerProgressAutosaveClient.TryUpload(data, slotName);
                EventBus.Publish(new GameSavedEvent { SavePath = localPath });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the newest valid local copy of a slot without contacting the cloud.
        /// Safe to call from the main thread; does only local file IO.
        /// </summary>
        public SaveData Load(string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            try
            {
                var selection = SelectAndReconcile(slotName, null, null);
                return Publish(slotName, selection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a slot, consulting the cloud asynchronously and picking the newest valid
        /// snapshot. Cancellation, timeout or network failure fall back to local data.
        /// Never throws; returns null when no valid snapshot exists.
        /// </summary>
        public async Task<SaveData> LoadAsync(string slotName = null, CancellationToken ct = default)
        {
            slotName ??= AUTO_SAVE_NAME;

            var cloud = ServerProgressAutosaveClient.CloudSlotResult.Unavailable;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(CloudLoadTimeout);
                try
                {
                    cloud = await ServerProgressAutosaveClient.TryLoadAsync(slotName, timeout.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Cloud load skipped: {ex.Message}");
                }
            }

            try
            {
                SaveData cloudData = cloud.Status == ServerProgressAutosaveClient.CloudStatus.Ok ? cloud.Save : null;
                DateTime? cloudDeletedAt = cloud.Status == ServerProgressAutosaveClient.CloudStatus.Deleted
                    ? cloud.DeletedAtUtc ?? DateTime.MaxValue
                    : (DateTime?)null;
                var selection = SelectAndReconcile(slotName, cloudData, cloudDeletedAt);
                return Publish(slotName, selection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return null;
            }
        }

        private SaveData Publish(string slotName, SaveCandidateSelector.Selection selection)
        {
            if (selection.Data == null)
                return null;

            if (selection.Source != SaveCandidateSelector.Source.LocalPrimary)
                Debug.Log($"[SaveManager] Loaded slot '{slotName}' from {selection.Source}.");

            EventBus.Publish(new GameLoadedEvent { SavePath = GetLocalSavePath(slotName) });
            return selection.Data;
        }

        private static SaveCandidateSelector.Selection SelectAndReconcile(string slotName, SaveData cloud, DateTime? cloudDeletedAtUtc)
        {
            string path = GetLocalSavePath(slotName);
            SaveData primary = TryReadLocal(path, "primary");
            SaveData backup = TryReadLocal(path + BackupExtension, "backup");
            DateTime? localDeletedAt = ReadTombstone(path + TombstoneExtension);

            var selection = SaveCandidateSelector.Select(
                primary, backup, cloud, localDeletedAt, cloudDeletedAtUtc, IsValidCandidate);

            if (selection.ShouldDeleteLocal)
                DeleteLocalFiles(path);
            if (selection.ShouldRewriteLocal && selection.Data != null)
            {
                // Recovering from the backup: drop the corrupt primary so it does not
                // overwrite the good backup during the rewrite.
                if (selection.Source == SaveCandidateSelector.Source.LocalBackup && File.Exists(path))
                    File.Delete(path);
                SaveLocal(selection.Data, slotName);
            }
            if (selection.ShouldResendCloudDelete)
                ServerProgressAutosaveClient.TryDelete(slotName);

            return selection;
        }

        private static bool IsValidCandidate(SaveData data)
        {
            if (TryValidateLoadedData(data, out string error))
                return true;

            Debug.LogWarning($"[SaveManager] Ignoring invalid save candidate: {error}");
            return false;
        }

        /// <summary>
        /// Validates loaded save data without logging. Use <see cref="Load"/> for file IO and error reporting.
        /// </summary>
        public static bool TryValidateLoadedData(SaveData data, out string error)
        {
            if (data == null)
            {
                error = "deserialization returned no data.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.Version))
            {
                error = "missing version.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.Timestamp))
            {
                error = "missing timestamp.";
                return false;
            }

            if (!StageManager.IsStageSupported(data.CurrentStage))
            {
                error = $"unsupported current stage: {data.CurrentStage}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.CreatureDNAJson))
            {
                error = "missing creature dna json.";
                return false;
            }

            try
            {
                var dna = RoobsWorld.Creature.CreatureDNA.FromJson(data.CreatureDNAJson);
                if (!GameManager.TryValidateCreatureDNA(dna, out string dnaError))
                {
                    error = $"invalid creature dna json: {dnaError}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"invalid creature dna json: {ex.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.AchievementJson))
            {
                error = "missing achievement json.";
                return false;
            }

            try
            {
                var achievements = JsonUtility.FromJson<AchievementSaveData>(data.AchievementJson);
                if (achievements?.UnlockedIds == null)
                {
                    error = "invalid achievement json: missing unlocked id list.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"invalid achievement json: {ex.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.RunRecordJson))
            {
                error = "missing run record json.";
                return false;
            }

            if (!RunRecord.TryFromJson(data.RunRecordJson, out _, out string runRecordError))
            {
                error = $"invalid run record json: {runRecordError}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Returns true if a local save (primary or backup) exists for the given slot name.
        /// </summary>
        public bool SaveExists(string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            string path = GetLocalSavePath(slotName);
            return File.Exists(path) || File.Exists(path + BackupExtension);
        }

        /// <summary>
        /// Deletes the slot locally (leaving a tombstone) and in the cloud.
        /// </summary>
        public void DeleteSave(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                return;

            string path = GetLocalSavePath(slotName);
            try
            {
                WriteAllTextAtomic(path + TombstoneExtension, DateTime.UtcNow.ToString("o"));
                DeleteLocalFiles(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Delete failed: {ex.Message}");
            }

            ServerProgressAutosaveClient.TryDelete(slotName);
        }

        /// <summary>
        /// Enumerates local save slot names. Returns empty array on error.
        /// </summary>
        public string[] GetAllSaveSlots()
        {
            try
            {
                string directory = GetLocalSaveDirectory();
                if (!Directory.Exists(directory))
                    return Array.Empty<string>();

                string[] files = Directory.GetFiles(directory, "*" + SaveFileExtension);
                for (int i = 0; i < files.Length; i++)
                    files[i] = Path.GetFileNameWithoutExtension(files[i]);
                return files;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Creates a new <see cref="SaveData"/> instance initialized for a fresh game at the Cell stage.
        /// </summary>
        public SaveData CreateNewSave(long worldSeed)
        {
            return new SaveData
            {
                WorldSeed = worldSeed,
                CurrentStage = GameStageType.Cell,
                DNAPoints = 0
            };
        }

        private static string SaveLocal(SaveData data, string slotName)
        {
            string path = GetLocalSavePath(slotName);
            WriteAllTextAtomic(path, JsonUtility.ToJson(data, true), path + BackupExtension);

            string tombstone = path + TombstoneExtension;
            if (File.Exists(tombstone))
                File.Delete(tombstone);
            return path;
        }

        /// <summary>
        /// Writes to a temp file then swaps it in. With <paramref name="backupPath"/>, the
        /// previous file is kept there, so an interrupted write never loses the last snapshot.
        /// </summary>
        internal static void WriteAllTextAtomic(string path, string contents, string backupPath = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + TempExtension;
            File.WriteAllText(temp, contents);

            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            try
            {
                File.Replace(temp, path, backupPath, true);
            }
            catch (PlatformNotSupportedException)
            {
                // Fallback keeps a complete copy on disk at every step.
                if (backupPath != null)
                    File.Copy(path, backupPath, true);
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
        }

        private static SaveData TryReadLocal(string path, string label)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Unreadable local {label} save: {ex.Message}");
                return null;
            }
        }

        private static DateTime? ReadTombstone(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                return SaveCandidateSelector.ParseTimestamp(File.ReadAllText(path).Trim()) ?? DateTime.MaxValue;
            }
            catch
            {
                return DateTime.MaxValue;
            }
        }

        private static void DeleteLocalFiles(string path)
        {
            foreach (string file in new[] { path, path + BackupExtension, path + TempExtension })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        private static string GetLocalSavePath(string slotName)
        {
            string safeSlot = SanitizeSlotName(string.IsNullOrWhiteSpace(slotName) ? AUTO_SAVE_NAME : slotName);
            return Path.Combine(GetLocalSaveDirectory(), safeSlot + SaveFileExtension);
        }

        private static string GetLocalSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, LocalSaveDirectoryName);
        }

        private static string SanitizeSlotName(string slotName)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var chars = slotName.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }

            return new string(chars);
        }

    }
}
