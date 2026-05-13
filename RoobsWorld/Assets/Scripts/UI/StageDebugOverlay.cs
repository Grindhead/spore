using RoobsWorld.Core;
using RoobsWorld.Creature.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Persistent debug-only overlay that allows quick switching between major game screens.
    /// </summary>
    public class StageDebugOverlay : MonoBehaviour
    {
        private const string CreatureEditorSceneName = "CreatureEditor";
        private const int SortingOrder = 9000;

        private Canvas _canvas;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("StageDebugOverlayCanvas");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("StageDebugOverlayPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(10f, -10f);
            panelRect.sizeDelta = new Vector2(560f, 42f);

            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.08f, 0.11f, 0.82f);

            CreateNavButton(panelObject.transform, "Main Menu", 8f, ShowMainMenu);
            CreateNavButton(panelObject.transform, "Cell Stage", 144f, GoToCellStage);
            CreateNavButton(panelObject.transform, "Creature Editor", 280f, GoToCreatureEditor);
            CreateNavButton(panelObject.transform, "Creature Mode", 432f, GoToCreatureMode);
        }

        private void CreateNavButton(Transform parent, string label, float xOffset, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject($"StageDebug_{label.Replace(" ", string.Empty)}");
            buttonObject.transform.SetParent(parent, false);

            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(xOffset, 0f);
            buttonRect.sizeDelta = new Vector2(128f, -8f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.17f, 0.44f, 0.66f, 0.95f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = UIStyleGuide.GetDefaultFont();
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
        }

        private static void ShowMainMenu()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SetPaused(true, suppressPauseMenu: true);

            if (Object.FindAnyObjectByType<MainMenuUI>() == null)
            {
                var menuObject = new GameObject("MainMenu");
                menuObject.AddComponent<MainMenuUI>();
            }
        }

        private static void GoToCellStage()
        {
            CloseMainMenuIfPresent();

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetPaused(false);
                if (GameManager.Instance.CurrentSave == null)
                {
                    GameManager.Instance.StartNewGame();
                    return;
                }
            }

            StageManager.Instance.TransitionTo(GameStageType.Cell);
        }

        private static void GoToCreatureMode()
        {
            CloseMainMenuIfPresent();

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetPaused(false);
                if (GameManager.Instance.CurrentSave == null)
                    GameManager.Instance.StartNewGame();
            }

            StageManager.Instance.TransitionTo(GameStageType.Creature);
        }

        private static void GoToCreatureEditor()
        {
            CloseMainMenuIfPresent();
            DestroyExistingEditorRuntime();

            if (GameManager.HasInstance)
                GameManager.Instance.SetPaused(false);

            if (Application.CanStreamedLevelBeLoaded(CreatureEditorSceneName))
            {
                SceneManager.LoadScene(CreatureEditorSceneName);
                return;
            }

            var editorObject = new GameObject("CreatureEditor");
            var editor = editorObject.AddComponent<CreatureEditorScene>();
            editor.Initialize(
                GameManager.HasInstance
                    ? GameManager.Instance.GetPlayerCreatureDNAOrDefault()
                    : RoobsWorld.Creature.CreatureDNA.CreateDefault());
        }

        private static void CloseMainMenuIfPresent()
        {
            var menu = Object.FindAnyObjectByType<MainMenuUI>();
            if (menu != null)
                Object.Destroy(menu.gameObject);
        }

        private static void DestroyExistingEditorRuntime()
        {
            var editor = Object.FindAnyObjectByType<CreatureEditorScene>();
            if (editor != null)
                Object.Destroy(editor.gameObject);
        }
    }
}
