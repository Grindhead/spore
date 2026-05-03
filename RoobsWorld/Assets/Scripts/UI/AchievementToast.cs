using System.Collections;
using System.Collections.Generic;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Listens for game milestone events and shows temporary, non-interactive toast banners.
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        private const float DISPLAY_DURATION = 4f;
        private const float SLIDE_DURATION = 0.5f;
        private const float OFFSCREEN_Y = 120f;

        private Canvas _canvas;
        private RectTransform _toastRect;
        private Text _headerText;
        private Text _titleText;
        private Text _descText;
        private Text _pointsText;
        private CanvasGroup _canvasGroup;

        private readonly Queue<ToastMessage> _queue = new Queue<ToastMessage>();
        private bool _isShowing;

        private struct ToastMessage
        {
            public string Header;
            public string Title;
            public string Description;
            public string Meta;
        }

        private void Awake()
        {
            BuildUI();
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            EventBus.Subscribe<PlayerCharacterImprovedEvent>(OnPlayerCharacterImproved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            EventBus.Unsubscribe<PlayerCharacterImprovedEvent>(OnPlayerCharacterImproved);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            var def = AchievementCatalog.Get(evt.AchievementId);
            _queue.Enqueue(new ToastMessage
            {
                Header = Localization.Get("achievement.unlocked"),
                Title = evt.Title,
                Description = def != null ? def.Description : "",
                Meta = $"+{evt.Points} pts"
            });
            if (!_isShowing)
                StartCoroutine(ShowNext());
        }

        private void OnPlayerCharacterImproved(PlayerCharacterImprovedEvent evt)
        {
            _queue.Enqueue(new ToastMessage
            {
                Header = Localization.Get("character.improved"),
                Title = string.IsNullOrWhiteSpace(evt.Title) ? Localization.Get("character.improved") : evt.Title,
                Description = evt.Description ?? "",
                Meta = evt.StatSummary ?? ""
            });
            if (!_isShowing)
                StartCoroutine(ShowNext());
        }

        private IEnumerator ShowNext()
        {
            _isShowing = true;

            while (_queue.Count > 0)
            {
                var message = _queue.Dequeue();

                _headerText.text = message.Header;
                _titleText.text = message.Title;
                _descText.text = message.Description;
                _pointsText.text = message.Meta;

                yield return SlideIn();
                yield return new WaitForSecondsRealtime(DISPLAY_DURATION);
                yield return SlideOut();

                yield return new WaitForSecondsRealtime(0.3f);
            }

            _isShowing = false;
        }

        private IEnumerator SlideIn()
        {
            _canvas.gameObject.SetActive(true);
            float elapsed = 0;
            while (elapsed < SLIDE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / SLIDE_DURATION;
                float smoothT = t * t * (3f - 2f * t);
                _toastRect.anchoredPosition = new Vector2(0, Mathf.Lerp(OFFSCREEN_Y, 0, smoothT));
                _canvasGroup.alpha = smoothT;
                yield return null;
            }
            _toastRect.anchoredPosition = Vector2.zero;
            _canvasGroup.alpha = 1;
        }

        private IEnumerator SlideOut()
        {
            float elapsed = 0;
            while (elapsed < SLIDE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / SLIDE_DURATION;
                _toastRect.anchoredPosition = new Vector2(0, Mathf.Lerp(0, OFFSCREEN_Y, t));
                _canvasGroup.alpha = 1 - t;
                yield return null;
            }
            _canvas.gameObject.SetActive(false);
        }

        private void BuildUI()
        {
            var font = UIStyleGuide.GetDefaultFont();

            var canvasObj = new GameObject("AchievementToastCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);

            _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var toast = new GameObject("Toast");
            toast.transform.SetParent(canvasObj.transform, false);
            _toastRect = toast.AddComponent<RectTransform>();
            _toastRect.anchorMin = new Vector2(0.5f, 1);
            _toastRect.anchorMax = new Vector2(0.5f, 1);
            _toastRect.pivot = new Vector2(0.5f, 1);
            _toastRect.anchoredPosition = new Vector2(0, OFFSCREEN_Y);
            _toastRect.sizeDelta = new Vector2(420, 80);

            var bg = toast.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.15f, 0.3f, 0.92f);

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(toast.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(50, 50);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = UIStyleGuide.Colors.UnlockedGold;

            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(toast.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.5f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(70, 0);
            headerRect.offsetMax = new Vector2(-80, -5);
            _headerText = headerObj.AddComponent<Text>();
            _headerText.font = font;
            _headerText.fontSize = 18;
            _headerText.color = UIStyleGuide.Colors.Warning;
            _headerText.fontStyle = FontStyle.Bold;
            _headerText.alignment = TextAnchor.LowerLeft;
            _headerText.text = Localization.Get("achievement.unlocked");

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(toast.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 0.5f);
            titleRect.offsetMin = new Vector2(70, 5);
            titleRect.offsetMax = new Vector2(-80, 0);
            _titleText = titleObj.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 16;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.UpperLeft;

            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(toast.transform, false);
            var descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.35f);
            descRect.offsetMin = new Vector2(70, 2);
            descRect.offsetMax = new Vector2(-80, 0);
            _descText = descObj.AddComponent<Text>();
            _descText.font = font;
            _descText.fontSize = 12;
            _descText.color = UIStyleGuide.Colors.Subtitle;
            _descText.alignment = TextAnchor.UpperLeft;

            var ptsObj = new GameObject("Points");
            ptsObj.transform.SetParent(toast.transform, false);
            var ptsRect = ptsObj.AddComponent<RectTransform>();
            ptsRect.anchorMin = new Vector2(1, 0);
            ptsRect.anchorMax = new Vector2(1, 1);
            ptsRect.pivot = new Vector2(1, 0.5f);
            ptsRect.anchoredPosition = new Vector2(-10, 0);
            ptsRect.sizeDelta = new Vector2(65, 0);
            _pointsText = ptsObj.AddComponent<Text>();
            _pointsText.font = font;
            _pointsText.fontSize = 16;
            _pointsText.color = UIStyleGuide.Colors.Warning;
            _pointsText.alignment = TextAnchor.MiddleRight;

            _canvas.gameObject.SetActive(false);
        }
    }
}
