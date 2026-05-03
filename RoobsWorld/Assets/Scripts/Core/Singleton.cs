using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Generic persistent MonoBehaviour singleton that lazily creates an instance on first access and survives scene loads.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting && Application.isPlaying)
                    return null;

                lock (_lock)
                {
                    if (_instance != null)
                        return _instance;

                    _instance = FindAnyObjectByType<T>();

                    if (_instance != null)
                        return _instance;

                    var go = new GameObject($"[{typeof(T).Name}]");
                    _instance = go.AddComponent<T>();
                    if (Application.isPlaying)
                        DontDestroyOnLoad(go);
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            _applicationIsQuitting = false;

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            OnInitialize();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnInitialize() { }
    }
}

