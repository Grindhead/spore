using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Stub implementation for Steam achievement reporting via Steamworks.NET.
    /// Replace the body of <see cref="ReportUnlock"/> with actual SDK calls when Steamworks.NET is integrated.
    /// Typical integration: Steamworks.SteamUserStats.SetAchievement(MapToSteamId(achievementId)); Steamworks.SteamUserStats.StoreStats();
    /// </summary>
    public sealed class SteamAchievementProvider : IPlatformAchievementProvider
    {
        /// <summary>
        /// Maps an in-game achievement id (e.g. "cell_first_eat") to the Steam achievement API name
        /// as configured in the Steamworks dashboard. Override this mapping to match your Steam app config.
        /// </summary>
        private static string MapToSteamId(string gameId) => gameId.ToUpperInvariant().Replace('.', '_');

        public void ReportUnlock(string achievementId)
        {
            // TODO: integrate Steamworks.NET
            // var steamId = MapToSteamId(achievementId);
            // Steamworks.SteamUserStats.SetAchievement(steamId);
            // Steamworks.SteamUserStats.StoreStats();
            Debug.Log($"[SteamAchievements] Would unlock: {achievementId} (Steamworks.NET not yet integrated)");
        }
    }

    /// <summary>
    /// Stub implementation for Xbox Live / GDK achievement reporting.
    /// Replace the body of <see cref="ReportUnlock"/> with actual GDK calls when the Xbox GDK is integrated.
    /// Typical integration: XblAchievementsManagerUpdateAchievement(xboxUserId, achievementId, 100);
    /// </summary>
    public sealed class XboxAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId)
        {
            // TODO: integrate Xbox GDK / XSAPI
            // XblAchievementsManagerUpdateAchievement(userId, MapToXboxId(achievementId), 100 /*percent complete*/);
            Debug.Log($"[XboxAchievements] Would unlock: {achievementId} (Xbox GDK not yet integrated)");
        }
    }

    /// <summary>
    /// Stub implementation for PlayStation trophy reporting via the PS5 SDK / Unity PS Plugin.
    /// Replace the body of <see cref="ReportUnlock"/> with actual Sony SDK calls when the PS plugin is integrated.
    /// Typical integration: Sony.PS5.Trophies.UnlockTrophyRequest; or equivalent PS4/PS5 API.
    /// </summary>
    public sealed class PlayStationAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId)
        {
            // TODO: integrate Sony PS5 / PS4 trophy SDK
            // var trophyId = MapToTrophyId(achievementId);
            // Sony.PS5.Trophies.UnlockTrophy(trophyId, onComplete: (res) => { ... });
            Debug.Log($"[PSNTrophies] Would unlock: {achievementId} (Sony SDK not yet integrated)");
        }
    }

    /// <summary>
    /// Stub implementation for Nintendo Switch achievement reporting via the Nintendo SDK.
    /// Replace the body of <see cref="ReportUnlock"/> with actual Nintendo SDK calls.
    /// Note: Nintendo Switch does not have a global achievement system. This stub can be wired
    /// to custom in-game milestone tracking or a future Nintendo-specific feature.
    /// </summary>
    public sealed class NintendoAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId)
        {
            // TODO: wire to in-game milestone system or Nintendo SDK when available
            Debug.Log($"[Nintendo] Achievement milestone: {achievementId} (no platform-wide achievement system on Switch)");
        }
    }
}
