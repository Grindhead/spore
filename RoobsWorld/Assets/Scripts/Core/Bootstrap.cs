using RoobsWorld.Creature;
using RoobsWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Scene-level bootstrapper that ensures core singletons exist, wires optional input assets, and spawns global UI/audio/achievement helpers.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private bool _showMainMenuOnStart = true;
        [SerializeField] private bool _autoStartCellWhenMenuDisabled = true;
        [Tooltip("Assign InputSystem_Actions so input works without placing it in Resources/.")]
        [SerializeField] private InputActionAsset _inputActions;

        private void Awake()
        {
            // Ensure a renderable camera exists before the first rendered frame.
            _ = CameraManager.Instance;
        }

        private void Start()
        {
            EnsureManagers();
            RegisterCreatureParts();
            CreatePauseMenu();
            CreateStageDebugOverlay();
            CreateAdaptiveMusic();
            CreateTutorialOverlay();
            CreateAchievementToast();
            CreateAchievementService();

            if (_showMainMenuOnStart)
            {
                if (ShouldShowMainMenuOnStart())
                {
                    Debug.Log("[Bootstrap] Showing main menu on startup.");
                    CreateMainMenu();
                }
                else
                {
                    Debug.Log("[Bootstrap] Active run detected; skipping startup main menu.");
                }
            }
            else if (_autoStartCellWhenMenuDisabled)
            {
                Debug.Log("[Bootstrap] Auto-starting Cell Stage for testing");
                GameManager.Instance.StartNewGame();
            }
            else
            {
                Debug.Log("[Bootstrap] Auto-start disabled; loading saved Cell run.");
                GameManager.Instance.LoadGame();
            }
        }

        private static bool ShouldShowMainMenuOnStart()
        {
            // When reloading CellStage during an active run transition, keep gameplay active
            // instead of recreating the startup menu.
            return !GameManager.HasInstance || GameManager.Instance.CurrentSave == null;
        }

        private void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                DontDestroyOnLoad(esObj);
                eventSystem = esObj.AddComponent<EventSystem>();
                var uiModule = esObj.AddComponent<InputSystemUIInputModule>();
                AssignUIActions(uiModule, _inputActions ?? InputManager.Instance.LoadedInputActions);
                Debug.Log("[Bootstrap] EventSystem created with InputSystemUIInputModule");
                return;
            }

            var existingStandalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (existingStandalone != null)
            {
                Destroy(existingStandalone);
                Debug.Log("[Bootstrap] Removed unsupported StandaloneInputModule from existing EventSystem.");
            }

            var existingModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (existingModule == null)
            {
                existingModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[Bootstrap] Added InputSystemUIInputModule to existing EventSystem.");
            }
            existingModule.enabled = true;

            AssignUIActions(existingModule, _inputActions ?? InputManager.Instance.LoadedInputActions);
        }

        private void AssignUIActions(InputSystemUIInputModule uiModule, InputActionAsset inputAsset)
        {
            if (inputAsset != null)
            {
                uiModule.actionsAsset = inputAsset;
                EnsureUIActionReferences(uiModule, inputAsset);
                inputAsset.FindActionMap("UI", throwIfNotFound: false)?.Enable();
                Debug.Log("[Bootstrap] Assigned InputSystem_Actions to EventSystem UI module.");
                return;
            }

            if (uiModule.actionsAsset == null)
            {
                Debug.LogError("[Bootstrap] EventSystem UI module has no actions asset assigned. InputSystem_Actions is required.");
            }
        }

        private static void EnsureUIActionReferences(InputSystemUIInputModule uiModule, InputActionAsset inputAsset)
        {
            // Some scene-serialized EventSystems keep action references null even when actionsAsset is assigned.
            // Explicitly wire required UI actions so mouse/submit/navigation always work.
            uiModule.point = BuildActionReference(inputAsset, "UI/Point");
            uiModule.move = BuildActionReference(inputAsset, "UI/Navigate");
            uiModule.submit = BuildActionReference(inputAsset, "UI/Submit");
            uiModule.cancel = BuildActionReference(inputAsset, "UI/Cancel");
            uiModule.leftClick = BuildActionReference(inputAsset, "UI/Click");
            uiModule.middleClick = BuildActionReference(inputAsset, "UI/MiddleClick");
            uiModule.rightClick = BuildActionReference(inputAsset, "UI/RightClick");
            uiModule.scrollWheel = BuildActionReference(inputAsset, "UI/ScrollWheel");
            uiModule.trackedDevicePosition = BuildActionReference(inputAsset, "UI/TrackedDevicePosition");
            uiModule.trackedDeviceOrientation = BuildActionReference(inputAsset, "UI/TrackedDeviceOrientation");
        }

        private static InputActionReference BuildActionReference(InputActionAsset inputAsset, string actionPath)
        {
            var action = inputAsset.FindAction(actionPath, throwIfNotFound: false);
            return action != null ? InputActionReference.Create(action) : null;
        }

        private void EnsureManagers()
        {
            var inputMgr = InputManager.Instance;
            if (_inputActions != null)
                inputMgr.SetInputAsset(_inputActions);
            EnsureEventSystem();
            _ = GameManager.Instance;
            _ = StageManager.Instance;
            _ = SaveManager.Instance;
            _ = AudioManager.Instance;
            _ = CameraManager.Instance;
            _ = UIManager.Instance;
            _ = SettingsManager.Instance;
            _ = CreatureFactory.Instance;
            _ = PoolManager.Instance;

            Debug.Log("[Bootstrap] All managers initialized");
        }

        private void CreatePauseMenu()
        {
            if (FindAnyObjectByType<PauseMenuUI>() != null)
                return;

            var pauseObj = new GameObject("PauseMenu");
            DontDestroyOnLoad(pauseObj);
            pauseObj.AddComponent<PauseMenuUI>();
            Debug.Log("[Bootstrap] Pause menu created");
        }

        private void CreateMainMenu()
        {
            if (FindAnyObjectByType<MainMenuUI>() != null)
                return;

            var menuObj = new GameObject("MainMenu");
            menuObj.AddComponent<MainMenuUI>();
            Debug.Log("[Bootstrap] Main menu created");
        }

        private void CreateStageDebugOverlay()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            if (FindAnyObjectByType<StageDebugOverlay>() != null)
                return;

            var overlayObject = new GameObject("StageDebugOverlay");
            DontDestroyOnLoad(overlayObject);
            overlayObject.AddComponent<StageDebugOverlay>();
            Debug.Log("[Bootstrap] Stage debug overlay created");
        }

        private void CreateAdaptiveMusic()
        {
            if (FindAnyObjectByType<AdaptiveMusicController>() != null)
                return;

            var musicObj = new GameObject("AdaptiveMusic");
            DontDestroyOnLoad(musicObj);
            musicObj.AddComponent<AdaptiveMusicController>();
            musicObj.AddComponent<AudioClipLoader>();
        }

        private void CreateTutorialOverlay()
        {
            if (FindAnyObjectByType<TutorialOverlay>() != null)
                return;

            var tutorialObj = new GameObject("TutorialOverlay");
            DontDestroyOnLoad(tutorialObj);
            tutorialObj.AddComponent<TutorialOverlay>();
        }

        private void CreateAchievementToast()
        {
            if (FindAnyObjectByType<AchievementToast>() != null)
                return;

            var toastObj = new GameObject("AchievementToast");
            DontDestroyOnLoad(toastObj);
            toastObj.AddComponent<AchievementToast>();
        }

        private void CreateAchievementService()
        {
            if (FindAnyObjectByType<AchievementService>() != null)
                return;

            var serviceObj = new GameObject("AchievementService");
            DontDestroyOnLoad(serviceObj);
            serviceObj.AddComponent<AchievementService>();
        }

        private void RegisterCreatureParts()
        {
            var catalog = CreaturePartCatalog.GetDefaultCatalog();
            foreach (var kvp in catalog)
                CreatureFactory.Instance.RegisterPart(kvp.Value);

            Debug.Log($"[Bootstrap] Registered {catalog.Count} creature parts");
        }
    }
}

