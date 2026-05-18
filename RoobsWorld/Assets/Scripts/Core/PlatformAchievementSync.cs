using System;
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
