using System.Collections;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Displays staged tutorial messages as an overlay, sequenced per <see cref="GameStageType"/>.
    /// Also exposes <see cref="ShowContextualTip"/> for one-off event-driven hints.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        private const float FADE_DURATION = 0.5f;

        /// <summary>Singleton reference — set in Awake, cleared in OnDestroy.</summary>
        public static TutorialOverlay Instance { get; private set; }

        private Canvas _canvas;
        private Text _messageText;
        private Image _bgImage;
        private CanvasGroup _canvasGroup;
        private TutorialProgress _progress;
        private Coroutine _activeSequence;
        private Coroutine _contextTipCoroutine;

        /// <summary>Read-only access to the persisted tutorial progress (for conditional tip logic).</summary>
        public TutorialProgress Progress => _progress;

        private void Awake()
        {
            Instance = this;
            _progress = TutorialProgress.LoadFromPlayerPrefs();
            BuildUI();
            Hide();

            EventBus.Subscribe<StageTransitionEvent>(OnStageTransition);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            EventBus.Unsubscribe<StageTransitionEvent>(OnStageTransition);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        /// <summary>
        /// Shows a one-off contextual tip only when no full tutorial sequence is playing.
        /// Safe to call even when the tutorial system is inactive (no-ops gracefully).
        /// </summary>
        public void ShowContextualTip(string messageKey, float duration = 4f)
        {
            if (_activeSequence != null) return;   // don't interrupt the main sequence
            if (_contextTipCoroutine != null) StopCoroutine(_contextTipCoroutine);
            _contextTipCoroutine = StartCoroutine(PlayContextTip(messageKey, duration));
        }

        private IEnumerator PlayContextTip(string messageKey, float duration)
        {
            ShowMessage(messageKey);
            yield return FadeIn();
            yield return new WaitForSecondsRealtime(duration);
            yield return FadeOut();
            Hide();
            _contextTipCoroutine = null;
        }

        /// <summary>Clears all tutorial progress so the full sequence plays again next run.</summary>
        public void ResetProgress()
        {
            _progress.Reset();
            _progress.SaveToPlayerPrefs();
        }

        private void OnStageTransition(StageTransitionEvent evt)
        {
            StartTutorial(evt.ToStage);
        }

        public void StartTutorial(GameStageType stage)
        {
            if (_progress.IsStageCompleted(stage)) return;

            var steps = TutorialCatalog.GetSteps(stage);
            if (steps == null) return;

            if (_activeSequence != null)
                StopCoroutine(_activeSequence);

            _activeSequence = StartCoroutine(PlaySequence(stage, steps));
        }

        private IEnumerator PlaySequence(GameStageType stage, TutorialStep[] steps)
        {
            int startIdx = _progress.GetStepIndex(stage);
            bool skipped = false;

            for (int i = startIdx; i < steps.Length && !skipped; i++)
            {
                var step = steps[i];
                ShowMessage(step.Message);
                yield return FadeIn();

                float remaining = step.DisplayDuration;
                while (remaining > 0f)
                {
                    var kb = Keyboard.current;
                    if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                    {
                        skipped = true;
                        break;
                    }
                    remaining -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (skipped) break;

                yield return FadeOut();
                _progress.AdvanceStep(stage);
                _progress.SaveToPlayerPrefs();
                yield return new WaitForSeconds(0.5f);
            }

            _progress.MarkStageCompleted(stage);
            _progress.SaveToPlayerPrefs();
            _activeSequence = null;
            if (skipped)
                yield return FadeOut();
            Hide();
        }


        private void ShowMessage(string messageKey)
        {
            _canvas.gameObject.SetActive(true);
            string message = Localization.Get(messageKey);
            string hint = Localization.Get("tutorial.skip_hint");
            _messageText.text = message + "\n\n" + hint;
        }

        private void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0;
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / FADE_DURATION);
                yield return null;
            }
            _canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / FADE_DURATION);
                yield return null;
            }
            _canvasGroup.alpha = 0;
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("TutorialCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 180;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var panelObj = new GameObject("TutorialPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.85f);
            panelRect.anchorMax = new Vector2(0.85f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            _bgImage = panelObj.AddComponent<Image>();
            _bgImage.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);

            var textObj = new GameObject("Message");
            textObj.transform.SetParent(panelObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 5);
            textRect.offsetMax = new Vector2(-20, -5);

            _messageText = textObj.AddComponent<Text>();
            _messageText.font = UIStyleGuide.GetDefaultFont();
            _messageText.fontSize = 22;
            _messageText.color = UIStyleGuide.Colors.BodyText;
            _messageText.alignment = TextAnchor.MiddleCenter;
        }
    }
}
