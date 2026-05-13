using UnityEngine;

namespace RoobsWorld.Core
{
    [DefaultExecutionOrder(-9990)]
    public sealed class PlatformAchievementBootstrap : MonoBehaviour
    {
        private static PlatformAchievementBootstrap _instance;
        private IPlatformAchievementProvider _provider;
        private bool _bound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
#if ROOBSWORLD_PLAYSTATION || ROOBSWORLD_XBOX
            if (_instance != null)
                return;

            var go = new GameObject("PlatformAchievementBootstrap");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlatformAchievementBootstrap>();
#endif
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

#if ROOBSWORLD_PLAYSTATION
            _provider = new PlayStationAchievementProvider();
#elif ROOBSWORLD_XBOX
            _provider = new XboxAchievementProvider();
#else
            _provider = null;
#endif
        }

        private void Update()
        {
            if (_bound || _provider == null || !GameManager.HasInstance)
                return;

            GameManager.Instance.SetPlatformAchievementProvider(_provider);
            PlatformAchievementSync.ReportUnlockedAchievements(GameManager.Instance.Achievements, _provider);
            _bound = true;
        }
    }
}
