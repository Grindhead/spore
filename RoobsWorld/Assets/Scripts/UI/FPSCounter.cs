using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Simple on-screen FPS display, toggled by <see cref="GameSettings.ShowFPS"/>.
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        private const float UPDATE_INTERVAL = 0.5f;

        private Text _fpsText;
        private Canvas _canvas;
        private float _elapsed;
        private int _frameCount;

        private void Start()
        {
            var canvasObj = new GameObject("FPSCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);

            var textObj = new GameObject("FPSText");
            textObj.transform.SetParent(canvasObj.transform, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(120, 30);

            _fpsText = textObj.AddComponent<Text>();
            _fpsText.font = UIStyleGuide.GetDefaultFont();
            _fpsText.fontSize = 18;
            _fpsText.color = Color.green;
            _fpsText.fontStyle = FontStyle.Bold;
        }

        private void Update()
        {
            bool show = SettingsManager.HasInstance && SettingsManager.Instance.Current.ShowFPS;
            if (_canvas != null)
                _canvas.gameObject.SetActive(show);

            if (!show) return;

            _frameCount++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= UPDATE_INTERVAL)
            {
                float fps = _frameCount / _elapsed;
                _fpsText.text = $"FPS: {fps:F0}";
                _fpsText.color = fps >= 55 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
                _frameCount = 0;
                _elapsed = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
