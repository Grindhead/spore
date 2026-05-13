using System.Collections;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Pause overlay with resume, settings, save, new-run, and quit options driven by <see cref="PauseEvent"/>.
    /// Shows "Saved!" feedback on <see cref="GameSavedEvent"/> and respects <see cref="GameSettings.UITextScale"/>.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private const float SaveToastDuration = 2f;

        private Canvas _canvas;
        private SettingsUI _settingsUI;
        private AchievementsUI _achievementsUI;
        private Font _font;
        private GameObject _savedToast;
        private Coroutine _saveToastRoutine;

        private bool _isShowing;

        public bool IsShowing => _isShowing;

        private void Start()
        {
            EventBus.Subscribe<PauseEvent>(OnPauseEvent);
            EventBus.Subscribe<GameSavedEvent>(OnGameSaved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PauseEvent>(OnPauseEvent);
            EventBus.Unsubscribe<GameSavedEvent>(OnGameSaved);
            if (_saveToastRoutine != null) StopCoroutine(_saveToastRoutine);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void Update()
        {
            bool pausePressed = InputManager.HasInstance && InputManager.Instance.PausePressed;
            bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (pausePressed || escPressed)
            {
                if (_settingsUI != null && _settingsUI.gameObject.activeInHierarchy)
                    return;

                if (GameManager.HasInstance)
                    GameManager.Instance.TogglePause();
            }
        }

        private void OnPauseEvent(PauseEvent evt)
        {
            if (evt.IsPaused && !evt.SuppressPauseMenu)
                Show();
            else
                Hide();
        }

        public void Show()
        {
            _isShowing = true;
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                return;
            }
            BuildUI();
        }

        public void Hide()
        {
            _isShowing = false;
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
            if (_settingsUI != null)
                _settingsUI.Hide();
            if (_achievementsUI != null)
                _achievementsUI.Hide();
        }

        private void OnGameSaved(GameSavedEvent evt)
        {
            if (_savedToast == null) return;
            _savedToast.SetActive(true);
            if (_saveToastRoutine != null) StopCoroutine(_saveToastRoutine);
            _saveToastRoutine = StartCoroutine(HideSavedToastAfter(SaveToastDuration));
        }

        private IEnumerator HideSavedToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (_savedToast != null) _savedToast.SetActive(false);
            _saveToastRoutine = null;
        }

        private void BuildUI()
        {
            _font = UIStyleGuide.GetDefaultFont();
            float textScale = SettingsManager.GetUITextScale();

            var canvasObj = new GameObject("PauseMenuCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 210;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            var dimmer = CreateFullRect(canvasObj.transform, "Dimmer");
            dimmer.AddComponent<Image>().color = UIStyleGuide.Colors.OverlayDim;

            UIStyleGuide.AddScreenAtmosphere(canvasObj.transform, "Pause", 1.1f);

            var panelBorder = new GameObject("PanelBorder");
            panelBorder.transform.SetParent(canvasObj.transform, false);
            var pbRect = panelBorder.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0.5f);
            pbRect.anchorMax = new Vector2(0.5f, 0.5f);
            pbRect.sizeDelta = new Vector2(430, 562);
            var borderImage = panelBorder.AddComponent<Image>();
            borderImage.sprite = UIStyleGuide.GetPaintedPanelSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.22f);
            borderImage.raycastTarget = false;

            var panel = new GameObject("PausePanel");
            panel.transform.SetParent(canvasObj.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(404, 536);
            UIStyleGuide.ApplyPaintedPanelChrome(panel, strong: true, alpha: 0.97f);

            var headerBar = new GameObject("HeaderBar");
            headerBar.transform.SetParent(panel.transform, false);
            var hbRect = headerBar.AddComponent<RectTransform>();
            hbRect.anchorMin = new Vector2(0.12f, 0.88f);
            hbRect.anchorMax = new Vector2(0.88f, 0.895f);
            hbRect.pivot = new Vector2(0.5f, 1f);
            hbRect.anchoredPosition = Vector2.zero;
            hbRect.sizeDelta = Vector2.zero;
            headerBar.AddComponent<Image>().color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.72f);

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -24);
            titleRect.sizeDelta = new Vector2(-36, 54);
            var title = titleObj.AddComponent<Text>();
            title.font = _font;
            title.text = Localization.Get("pause.title").ToUpperInvariant();
            title.fontSize = Mathf.RoundToInt(UIStyleGuide.Typography.TitleXL * textScale);
            title.color = UIStyleGuide.Colors.FossilGoldBright;
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;
            var titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.8f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Shadow, 0.3f);
            titleShadow.effectDistance = new Vector2(0f, -1.4f);

            float startY = 0.69f;
            float spacing = 0.105f;

            CreatePauseButton(panel.transform, Localization.Get("pause.resume"), startY, OnResume, textScale, isPrimary: true);
            CreatePauseButton(panel.transform, Localization.Get("menu.settings"), startY - spacing, OnSettings, textScale);
            CreatePauseButton(panel.transform, Localization.Get("menu.achievements"), startY - spacing * 2, OnAchievements, textScale);
            CreatePauseButton(panel.transform, Localization.Get("pause.save_game"), startY - spacing * 3, OnSave, textScale);
            CreatePauseButton(panel.transform, Localization.Get("pause.main_menu"), startY - spacing * 4, OnNewRun, textScale);
            CreatePauseButton(panel.transform, Localization.Get("menu.quit"), startY - spacing * 5, OnQuit, textScale);

            _savedToast = new GameObject("SavedToast");
            _savedToast.transform.SetParent(panel.transform, false);
            var toastRect = _savedToast.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.04f);
            toastRect.anchorMax = new Vector2(0.5f, 0.105f);
            toastRect.sizeDelta = new Vector2(230, 34);
            var toastBg = _savedToast.AddComponent<Image>();
            toastBg.sprite = UIStyleGuide.GetPaintedFieldSprite();
            toastBg.type = Image.Type.Sliced;
            toastBg.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.86f);

            var toastTextObj = new GameObject("Text");
            toastTextObj.transform.SetParent(_savedToast.transform, false);
            var toastTextRect = toastTextObj.AddComponent<RectTransform>();
            toastTextRect.anchorMin = Vector2.zero;
            toastTextRect.anchorMax = Vector2.one;
            toastTextRect.offsetMin = Vector2.zero;
            toastTextRect.offsetMax = Vector2.zero;
            var toastText = toastTextObj.AddComponent<Text>();
            toastText.font = _font;
            toastText.text = Localization.Get("pause.saved");
            toastText.fontSize = Mathf.RoundToInt(19 * textScale);
            toastText.color = UIStyleGuide.Colors.Success;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.fontStyle = FontStyle.Bold;
            UIStyleGuide.ApplyTextShadow(toastTextObj, 0.32f);
            _savedToast.SetActive(false);
        }

        private void CreatePauseButton(Transform parent, string label, float yAnchor, System.Action onClick,
                                        float textScale = 1f, bool isPrimary = false)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.11f, yAnchor - 0.037f);
            rect.anchorMax = new Vector2(0.89f, yAnchor + 0.037f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = btnObj.AddComponent<Image>();
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            var variant = isPrimary ? UIButtonVariant.Primary : UIButtonVariant.Secondary;
            UIStyleGuide.ApplyPaintedButtonChrome(btn, variant);
            UIStyleGuide.AddButtonPearl(btnObj, variant, isPrimary ? 32f : 23f);
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(34, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = Mathf.RoundToInt(22 * textScale);
            UIStyleGuide.ApplyButtonLabel(text, variant);
            text.alignment = TextAnchor.MiddleCenter;
            UIStyleGuide.ApplyTextShadow(textObj, isPrimary ? 0.18f : 0.34f);
        }

        private static void AddButtonInteractionAnimation(GameObject btnObj, bool isPrimary)
        {
            var trigger = btnObj.AddComponent<EventTrigger>();
            Vector3 normalScale = Vector3.one;
            Vector3 hoverScale = isPrimary ? new Vector3(1.018f, 1.018f, 1f) : new Vector3(1.012f, 1.012f, 1f);
            Vector3 pressScale = new Vector3(0.988f, 0.988f, 1f);

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => btnObj.transform.localScale = hoverScale);
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => btnObj.transform.localScale = normalScale);
            trigger.triggers.Add(exit);

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => btnObj.transform.localScale = pressScale);
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => btnObj.transform.localScale = hoverScale);
            trigger.triggers.Add(up);
        }

        private static void AddPanelAtmosphere(Transform parent)
        {
            var atmosphere = new GameObject("PauseAtmosphere");
            atmosphere.transform.SetParent(parent, false);
            var rect = atmosphere.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateGlowRect(atmosphere.transform, "TopAura", new Vector2(0f, 0.62f), new Vector2(1f, 1f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.1f));
            CreateGlowRect(atmosphere.transform, "CentreAura", new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.09f));
        }

        private static void CreateGlowRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void OnResume()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SetPaused(false);
        }

        private void OnSettings()
        {
            if (_settingsUI == null)
            {
                var settingsObj = new GameObject("SettingsUI");
                settingsObj.transform.SetParent(transform, false);
                _settingsUI = settingsObj.AddComponent<SettingsUI>();
                _settingsUI.OnClosed = () => _settingsUI.Hide();
            }
            _settingsUI.Show();
        }

        private void OnAchievements()
        {
            if (_achievementsUI == null)
            {
                var achievementsObj = new GameObject("AchievementsUI");
                achievementsObj.transform.SetParent(transform, false);
                _achievementsUI = achievementsObj.AddComponent<AchievementsUI>();
                _achievementsUI.OnClosed = () => _achievementsUI.Hide();
            }
            _achievementsUI.Show();
        }

        private void OnSave()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SaveGame();
            Debug.Log("[PauseMenu] Game saved");
        }

        private void OnNewRun()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetPaused(false);
                GameManager.Instance.StartNewGame();
            }
        }

        private void OnQuit()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.QuitGame();
        }

        private GameObject CreateFullRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }
    }
}
