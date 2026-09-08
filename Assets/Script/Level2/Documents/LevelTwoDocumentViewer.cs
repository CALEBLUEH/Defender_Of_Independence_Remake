using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDocumentViewer : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform documentWindow;
        [SerializeField] private CanvasGroup documentWindowCanvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject dayPromptRoot;
        [SerializeField] private TMP_Text dayPromptText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private ScrollRect documentScroll;
        [SerializeField] private Button closeButton;
        [SerializeField] private LevelTwoFirstPersonController playerController;
        [SerializeField, Min(0.05f)] private float slideDuration = 0.25f;
        [SerializeField, Min(100f)] private float hiddenOffset = 1000f;

        private int _openedFrame = -1;
        private Vector2 _shownPosition;
        private Coroutine _animation;
        private bool _isOpen;
        private bool _isClosing;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (documentWindow != null) _shownPosition = documentWindow.anchoredPosition;
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= _openedFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.cKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        public void Show(string title, string body, bool showDayPrompt, int day, string dayTitle, string recommendation)
        {
            if (panel == null)
            {
                return;
            }

            titleText.text = title;
            bodyText.text = body;
            dayPromptRoot.SetActive(showDayPrompt);
            if (showDayPrompt)
            {
                string dayLabel = day >= 6 ? "FINAL DAY" : $"DAY {day}";
                dayPromptText.text = $"<b>{dayLabel} - {dayTitle.ToUpperInvariant()}</b>\n{recommendation}";
            }

            _isOpen = true;
            _isClosing = false;
            panel.SetActive(true);
            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(true);
            _openedFrame = Time.frameCount;
            if (documentWindow != null)
            {
                documentWindow.anchoredPosition = _shownPosition + Vector2.down * hiddenOffset;
                SetWindowAlpha(0f);
                StartAnimation(SlideWindow(documentWindow.anchoredPosition, _shownPosition, 0f, 1f, null));
            }
            Canvas.ForceUpdateCanvases();
            if (documentScroll != null)
            {
                documentScroll.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            if (!IsOpen || _isClosing)
            {
                return;
            }

            _isClosing = true;
            if (documentWindow == null)
            {
                FinishClose();
                return;
            }

            StartAnimation(SlideWindow(documentWindow.anchoredPosition, _shownPosition + Vector2.down * hiddenOffset,
                1f, 0f, FinishClose));
        }

        private void FinishClose()
        {
            _isOpen = false;
            _isClosing = false;
            panel.SetActive(false);
            playerController?.SetUiCursorActive(false);
            playerController?.SetControlsEnabled(true);
        }

        private IEnumerator SlideWindow(Vector2 from, Vector2 to, float alphaFrom, float alphaTo,
            System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
                documentWindow.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                SetWindowAlpha(Mathf.Lerp(alphaFrom, alphaTo, t));
                yield return null;
            }

            documentWindow.anchoredPosition = to;
            SetWindowAlpha(alphaTo);
            _animation = null;
            onComplete?.Invoke();
        }

        private void StartAnimation(IEnumerator routine)
        {
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(routine);
        }

        private void SetWindowAlpha(float alpha)
        {
            if (documentWindowCanvasGroup == null) return;
            documentWindowCanvasGroup.alpha = alpha;
            documentWindowCanvasGroup.interactable = alpha > 0.99f;
            documentWindowCanvasGroup.blocksRaycasts = alpha > 0.01f;
        }
    }
}
