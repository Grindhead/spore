using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RoobsWorld.Core
{
    public static class PlatformAchievementSync
    {
        public static int ReportUnlockedAchievements(AchievementTracker tracker, IPlatformAchievementProvider provider)
        {
            if (tracker == null || provider == null)
                return 0;

            int reported = 0;
            foreach (var achievement in AchievementCatalog.GetAll())
            {
                if (!tracker.IsUnlocked(achievement.Id))
                    continue;

                if (TryReport(provider, achievement.Id))
                    reported++;
            }

            return reported;
        }

        public static bool TryReport(IPlatformAchievementProvider provider, string achievementId)
        {
            if (provider == null || string.IsNullOrWhiteSpace(achievementId))
                return false;

            try
            {
                provider.ReportUnlock(achievementId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Achievements] Platform report failed for {achievementId}: {ex.Message}");
                return false;
            }
        }
    }

    public static class PlatformCloudSaveSync
    {
        private static IPlatformCloudSaveProvider _provider;

        public static IPlatformCloudSaveProvider Provider => _provider;

        public static void SetProvider(IPlatformCloudSaveProvider provider)
        {
            _provider = provider;
        }

        public static bool TryUploadJson(string slotName, string json)
        {
            if (_provider == null || !_provider.IsAvailable || string.IsNullOrWhiteSpace(json))
                return false;

            string remoteFile = BuildRemoteFileName(slotName);
            try
            {
                bool uploaded = _provider.TryWriteText(remoteFile, json);
                if (uploaded)
                    Debug.Log($"[CloudSave] Uploaded {remoteFile} to {_provider.PlatformName}.");
                return uploaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSave] Upload failed for {remoteFile}: {ex.Message}");
                return false;
            }
        }

        public static bool TryRestoreSlotToLocalFile(string slotName, string localPath)
        {
            if (_provider == null || !_provider.IsAvailable || string.IsNullOrWhiteSpace(localPath))
                return false;

            string remoteFile = BuildRemoteFileName(slotName);
            try
            {
                if (!_provider.TryReadText(remoteFile, out string json) || string.IsNullOrWhiteSpace(json))
                    return false;

                var data = JsonUtility.FromJson<SaveData>(json);
                if (!SaveManager.TryValidateLoadedData(data, out string error))
                {
                    Debug.LogWarning($"[CloudSave] Ignoring invalid {remoteFile}: {error}");
                    return false;
                }

                string directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(localPath, json, Encoding.UTF8);
                Debug.Log($"[CloudSave] Restored {remoteFile} from {_provider.PlatformName}.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSave] Restore failed for {remoteFile}: {ex.Message}");
                return false;
            }
        }

        public static string BuildRemoteFileName(string slotName)
        {
            return $"{PlatformServicesConfig.SteamCloudSaveDirectory}/{NormalizeSlotName(slotName)}.json";
        }

        public static string NormalizeSlotName(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                return "autosave";

            var builder = new StringBuilder(slotName.Length);
            foreach (char c in slotName.Trim())
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'
                    ? c
                    : '_');
            }

            return builder.Length > 0 ? builder.ToString() : "autosave";
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static class PlatformAchievementDebugTools
    {
        public static int ReportAllUnlockedTo(IPlatformAchievementProvider provider)
        {
            if (!GameManager.HasInstance)
                return 0;

            return PlatformAchievementSync.ReportUnlockedAchievements(GameManager.Instance.Achievements, provider);
        }

        public static void ResetLocalAchievements()
        {
            if (!GameManager.HasInstance)
                return;

            GameManager.Instance.Achievements.Reset();
            GameManager.Instance.SaveGame();
            Debug.Log("[Achievements] Local achievement state reset for QA.");
        }
    }
#endif
}
