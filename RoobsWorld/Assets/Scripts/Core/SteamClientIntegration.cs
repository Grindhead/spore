using UnityEngine;

#if STEAMWORKS_NET && !UNITY_WEBGL
using Steamworks;
#endif

namespace RoobsWorld.Core
{
    /// <summary>
    /// Owns the Steamworks.NET lifecycle for desktop builds.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SteamClientIntegration : MonoBehaviour
    {
        public const uint AppId = PlatformServicesConfig.SteamAppId;

        private static SteamClientIntegration _instance;
        private static bool _initialized;
        private static bool _didAttemptInit;

        private readonly SteamPlatformProvider _provider = new SteamPlatformProvider();
        private bool _providerBound;
        private bool _statsRequested;

        private void OnEnable()
        {
            EventBus.Subscribe<StageTransitionEvent>(OnStageTransition);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StageTransitionEvent>(OnStageTransition);
        }

        public static bool IsInitialized => _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
                return;

            var go = new GameObject("SteamClientIntegration");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SteamClientIntegration>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            TryInitialize();
        }

        private void Update()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (_initialized)
                SteamAPI.RunCallbacks();
#endif

            if (!_providerBound && GameManager.HasInstance)
            {
                GameManager.Instance.SetPlatformAchievementProvider(_provider);
                GameManager.Instance.SetPlatformUserProvider(_provider);
                GameManager.Instance.SetPlatformRichPresenceProvider(_provider);
                PlatformCloudSaveSync.SetProvider(_provider);
                PlatformAchievementSync.ReportUnlockedAchievements(GameManager.Instance.Achievements, _provider);
                _providerBound = true;
            }

            if (_initialized && !_statsRequested)
            {
                _statsRequested = _provider.RequestCurrentStats();
                if (!_statsRequested)
                    Debug.LogWarning("[Steam] RequestCurrentStats failed; achievement writes may be delayed until Steam stats are available.");
            }
        }

        private static void TryInitialize()
        {
            if (_didAttemptInit)
                return;

            _didAttemptInit = true;

#if STEAMWORKS_NET && !UNITY_WEBGL
            try
            {
                if (!Packsize.Test())
                    Debug.LogError("[Steam] Steamworks.NET Packsize.Test failed.");

                if (!DllCheck.Test())
                    Debug.LogError("[Steam] Steamworks.NET DllCheck.Test failed.");

                var appId = new AppId_t(AppId);
                if (SteamAPI.RestartAppIfNecessary(appId))
                {
                    Debug.Log("[Steam] Restarting through Steam client.");
                    Application.Quit();
                    return;
                }

                _initialized = SteamAPI.Init();
                if (!_initialized)
                {
                    Debug.LogWarning("[Steam] SteamAPI.Init failed. Steam features are disabled for this session.");
                    return;
                }

                Debug.Log($"[Steam] Initialized for AppId {AppId} as {SteamFriends.GetPersonaName()}.");
            }
            catch (System.DllNotFoundException ex)
            {
                Debug.LogWarning($"[Steam] Steamworks native library missing: {ex.Message}");
                _initialized = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Steam] Initialization failed: {ex.Message}");
                _initialized = false;
            }
#else
            Debug.Log("[Steam] STEAMWORKS_NET is not defined. Steam integration is in no-op mode.");
#endif
        }

        private void OnApplicationQuit()
        {
            _provider.ClearRichPresence();
            Shutdown();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                Shutdown();
        }

        private static void Shutdown()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            if (_initialized)
                SteamAPI.Shutdown();
#endif
            _initialized = false;
        }

        private void OnStageTransition(StageTransitionEvent evt)
        {
            if (_providerBound)
            {
                _provider.SetRichPresence(evt.ToStage);
                GameManager.Instance?.UpdatePlatformRichPresence(evt.ToStage);
            }
        }
    }
}
