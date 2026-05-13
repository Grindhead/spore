using System.Text;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Steamworks.NET-backed platform provider. It compiles as a safe no-op until
    /// Steamworks.NET is imported and the ROOBSWORLD_STEAM scripting define is enabled.
    /// </summary>
    public class SteamPlatformProvider : IPlatformAchievementProvider, IPlatformUserProvider, IPlatformCloudSaveProvider, IPlatformRichPresenceProvider
    {
        public bool IsAvailable => SteamClientIntegration.IsInitialized;
        public string PlatformName => "Steam";

        public string UserId
        {
            get
            {
#if STEAMWORKS_NET && !UNITY_WEBGL
                return IsAvailable ? Steamworks.SteamUser.GetSteamID().m_SteamID.ToString() : null;
#else
                return null;
#endif
            }
        }

        public string DisplayName
        {
            get
            {
#if STEAMWORKS_NET && !UNITY_WEBGL
                return IsAvailable ? Steamworks.SteamFriends.GetPersonaName() : null;
#else
                return null;
#endif
            }
        }

        public bool IsOverlayEnabled
        {
            get
            {
#if STEAMWORKS_NET && !UNITY_WEBGL
                return IsAvailable && Steamworks.SteamUtils.IsOverlayEnabled();
#else
                return false;
#endif
            }
        }

        public bool RequestCurrentStats()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            return IsAvailable;
#else
            return false;
#endif
        }

        public void ReportUnlock(string achievementId)
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (!IsAvailable)
                return;

            string steamId = AchievementCatalog.GetSteamApiName(achievementId);
            if (string.IsNullOrWhiteSpace(steamId))
                return;

            if (!Steamworks.SteamUserStats.SetAchievement(steamId))
            {
                Debug.LogWarning($"[Steam] Failed to set achievement {steamId} from {achievementId}");
                return;
            }

            if (!Steamworks.SteamUserStats.StoreStats())
                Debug.LogWarning($"[Steam] Failed to store stats after unlocking {steamId}");
#else
            Debug.Log($"[Steam] Would unlock: {achievementId} (Steamworks.NET is not active for this target)");
#endif
        }

        public void SyncUnlockedAchievements(AchievementTracker tracker)
        {
            PlatformAchievementSync.ReportUnlockedAchievements(tracker, this);
        }

        public void ActivateGameOverlay(string dialog = "Friends")
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (IsAvailable)
                Steamworks.SteamFriends.ActivateGameOverlay(dialog);
#endif
        }

        public void ActivateStorePage()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (IsAvailable)
                Steamworks.SteamFriends.ActivateGameOverlayToStore(
                    new Steamworks.AppId_t(SteamClientIntegration.AppId),
                    Steamworks.EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
#endif
        }

        public bool TryWriteText(string remoteFileName, string contents)
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (!IsCloudAvailable() || string.IsNullOrWhiteSpace(remoteFileName))
                return false;

            byte[] bytes = Encoding.UTF8.GetBytes(contents ?? string.Empty);
            return Steamworks.SteamRemoteStorage.FileWrite(remoteFileName, bytes, bytes.Length);
#else
            return false;
#endif
        }

        public bool TryReadText(string remoteFileName, out string contents)
        {
            contents = null;
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (!IsCloudAvailable() || string.IsNullOrWhiteSpace(remoteFileName))
                return false;
            if (!Steamworks.SteamRemoteStorage.FileExists(remoteFileName))
                return false;

            int size = Steamworks.SteamRemoteStorage.GetFileSize(remoteFileName);
            if (size <= 0)
                return false;

            byte[] bytes = new byte[size];
            int read = Steamworks.SteamRemoteStorage.FileRead(remoteFileName, bytes, size);
            if (read <= 0)
                return false;

            contents = Encoding.UTF8.GetString(bytes, 0, read);
            return true;
#else
            return false;
#endif
        }

        public void SetRichPresence(GameStageType stage)
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (!IsAvailable)
                return;

            Steamworks.SteamFriends.SetRichPresence("steam_display", PlatformServicesConfig.SteamRichPresenceDisplayToken);
            Steamworks.SteamFriends.SetRichPresence("status", BuildRichPresenceStatus(stage));
            Steamworks.SteamFriends.SetRichPresence("stage", BuildRichPresenceStageKey(stage));
#endif
        }

        public void ClearRichPresence()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (!IsAvailable)
                return;

            Steamworks.SteamFriends.ClearRichPresence();
#endif
        }

        public static string BuildRichPresenceStatus(GameStageType stage)
        {
            return stage switch
            {
                GameStageType.Cell => "Exploring the primordial ocean",
                GameStageType.Creature => "Hunting in Creature Stage",
                _ => "In the main menu"
            };
        }

        public static string BuildRichPresenceStageKey(GameStageType stage)
        {
            return stage switch
            {
                GameStageType.Cell => "cell",
                GameStageType.Creature => "creature",
                _ => "menu"
            };
        }

        private bool IsCloudAvailable()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            return IsAvailable
                && Steamworks.SteamRemoteStorage.IsCloudEnabledForAccount()
                && Steamworks.SteamRemoteStorage.IsCloudEnabledForApp();
#else
            return false;
#endif
        }
    }

    public sealed class SteamAchievementProvider : SteamPlatformProvider { }

    public sealed class XboxAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId)
        {
            string xboxId = AchievementCatalog.GetXboxAchievementId(achievementId);
#if ROOBSWORLD_XBOX
            Debug.LogWarning($"[Xbox] SDK bridge is not linked in this workspace. Achievement queued for platform wiring: {xboxId}");
#else
            Debug.Log($"[Xbox] Would unlock: {xboxId} (enable ROOBSWORLD_XBOX after installing Xbox GDK/XSAPI)");
#endif
        }
    }

    public sealed class PlayStationAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId)
        {
            string trophyId = AchievementCatalog.GetPlayStationTrophyId(achievementId);
#if ROOBSWORLD_PLAYSTATION
            Debug.LogWarning($"[PlayStation] SDK bridge is not linked in this workspace. Trophy queued for platform wiring: {trophyId}");
#else
            Debug.Log($"[PlayStation] Would unlock: {trophyId} (enable ROOBSWORLD_PLAYSTATION after installing the PlayStation Unity SDK/plugin)");
#endif
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
