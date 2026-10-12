using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Shows "Loading save..." with a Cancel button while <see cref="GameManager.LoadGameAsync"/>
    /// waits for the cloud. Cancel falls back to the newest valid local save.
    /// </summary>
    public class SaveLoadingOverlay : MonoBehaviour
    {
        private const int SortingOrder = 9500;
        private GameObject _root;

        private void Awake()
        {
            BuildUI();
            _root.SetActive(GameManager.HasInstance && GameManager.Instance.IsLoadingSave);
            EventBus.Subscribe<SaveLoadStateEvent>(OnLoadStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SaveLoadStateEvent>(OnLoadStateChanged);
        }

        private void OnLoadStateChanged(SaveLoadStateEvent evt)
        {
            if (_root != null)
                _root.SetActive(evt.IsLoading);
        }

        private static void Cancel()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.CancelLoadGame();
        }

        private void BuildUI()
        {
            _root = new GameObject("SaveLoadingOverlayCanvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            UIStyleGuide.ApplyCanvasScalerDefaults(_root.AddComponent<CanvasScaler>());
            _root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320f, 120f);
            panel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.9f);

            var label = CreateText(panel.transform, "Loading save...", 18);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var buttonObject = new GameObject("CancelButton");
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.25f);
            buttonRect.sizeDelta = new Vector2(140f, 36f);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.17f, 0.44f, 0.66f, 0.95f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Cancel);

            var buttonLabel = CreateText(buttonObject.transform, "Use local save", 14);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = buttonLabel.rectTransform.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string value, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.font = UIStyleGuide.GetDefaultFont();
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            return text;
        }
    }
}
