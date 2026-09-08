using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoConfirmationPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform window;
        [SerializeField] private CanvasGroup windowCanvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject warningRoot;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private LevelTwoFirstPersonController playerController;
        [SerializeField, Min(0.05f)] private float slideDuration = 0.22f;
        [SerializeField, Min(100f)] private float hiddenOffset = 700f;

        private Vector2 _shownPosition;
        private Func<bool> _onConfirm;
        private Coroutine _animation;
        private bool _isOpen;
        private bool _resolving;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (window != null) _shownPosition = window.anchoredPosition;
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            confirmButton?.onClick.AddListener(Confirm);
            cancelButton?.onClick.AddListener(Cancel);
        }

        private void OnDisable()
        {
            confirmButton?.onClick.RemoveListener(Confirm);
            cancelButton?.onClick.RemoveListener(Cancel);
        }

        public bool Show(string title, string message, string warning, Func<bool> onConfirm)
        {
            if (_isOpen || panel == null || window == null || onConfirm == null)
            {
                return false;
            }

            _isOpen = true;
            _resolving = false;
            _onConfirm = onConfirm;
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            bool hasWarning = !string.IsNullOrWhiteSpace(warning);
            if (warningRoot != null) warningRoot.SetActive(hasWarning);
            if (warningText != null) warningText.text = warning ?? string.Empty;

            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(true);
            panel.SetActive(true);
            window.anchoredPosition = _shownPosition + Vector2.down * hiddenOffset;
            SetWindowAlpha(0f);
            StartAnimation(SlideWindow(window.anchoredPosition, _shownPosition, 0f, 1f, null));
            return true;
        }

        public void Confirm()
        {
            if (_isOpen && !_resolving) Resolve(true);
        }

        public void Cancel()
        {
            if (_isOpen && !_resolving) Resolve(false);
        }

        private void Resolve(bool confirmed)
        {
            _resolving = true;
            Vector2 hiddenPosition = _shownPosition + Vector2.down * hiddenOffset;
            StartAnimation(SlideWindow(window.anchoredPosition, hiddenPosition, 1f, 0f, () => FinishResolve(confirmed)));
        }

        private void FinishResolve(bool confirmed)
        {
            panel.SetActive(false);
            _isOpen = false;
            _resolving = false;
            Func<bool> action = _onConfirm;
            _onConfirm = null;
            playerController?.SetUiCursorActive(false);

            if (confirmed && action != null && action())
            {
                return;
            }

            playerController?.SetControlsEnabled(true);
        }

        private IEnumerator SlideWindow(Vector2 from, Vector2 to, float alphaFrom, float alphaTo, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
                window.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                SetWindowAlpha(Mathf.Lerp(alphaFrom, alphaTo, t));
                yield return null;
            }

            window.anchoredPosition = to;
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
            if (windowCanvasGroup == null) return;
            windowCanvasGroup.alpha = alpha;
            windowCanvasGroup.interactable = alpha > 0.99f;
            windowCanvasGroup.blocksRaycasts = alpha > 0.01f;
        }
    }
}
