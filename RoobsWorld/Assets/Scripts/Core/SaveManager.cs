using System;
using System.IO;
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
    /// Handles creation, persistence, and loading of <see cref="SaveData"/> JSON files in <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_DIR = "Saves";
        private const string AUTO_SAVE_NAME = "autosave";
        private const string FILE_EXTENSION = ".json";

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, SAVE_DIR);

        /// <summary>
        /// Ensures the save directory exists at startup.
        /// </summary>
        protected override void OnInitialize()
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        /// <summary>
        /// Writes the given <paramref name="data"/> to a JSON file for the specified slot and publishes <see cref="GameSavedEvent"/> on success.
        /// </summary>
        public void Save(SaveData data, string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            data.Timestamp = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slotName);

            try
            {
                Directory.CreateDirectory(SaveDirectory);
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Game saved to {path}");
                EventBus.Publish(new GameSavedEvent { SavePath = path });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads save data for the specified slot if it exists and publishes <see cref="GameLoadedEvent"/> on success.
        /// Returns null and logs on missing file or read/parse failure.
        /// </summary>
        public SaveData Load(string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            string path = GetSavePath(slotName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] No save found at {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (!TryValidateLoadedData(data, out string validationError))
                {
                    Debug.LogError($"[SaveManager] Invalid save data: {validationError}");
                    return null;
                }
                Debug.Log($"[SaveManager] Game loaded from {path}");
                EventBus.Publish(new GameLoadedEvent { SavePath = path });
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return null;
            }
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
        /// Returns true if a save file exists for the given slot name.
        /// </summary>
        public bool SaveExists(string slotName = null)
        {
            slotName ??= AUTO_SAVE_NAME;
            return File.Exists(GetSavePath(slotName));
        }

        /// <summary>
        /// Deletes the save file for the given slot name if it exists.
        /// </summary>
        public void DeleteSave(string slotName)
        {
            string path = GetSavePath(slotName);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>
        /// Enumerates all save slot names currently present on disk. Returns empty array on error.
        /// </summary>
        public string[] GetAllSaveSlots()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    return Array.Empty<string>();

                string[] files = Directory.GetFiles(SaveDirectory, $"*{FILE_EXTENSION}");
                string[] names = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                    names[i] = Path.GetFileNameWithoutExtension(files[i]);
                return names;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] GetAllSaveSlots failed: {ex.Message}");
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

        private string GetSavePath(string slotName)
        {
            return Path.Combine(SaveDirectory, slotName + FILE_EXTENSION);
        }
    }
}
