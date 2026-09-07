using System;
using System.Collections;
using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LevelTwoScreenFader : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.1f;

        private CanvasGroup _canvasGroup;
        private Coroutine _transitionRoutine;

        public bool IsTransitioning => _transitionRoutine != null;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetAlpha(0f);
        }

        public bool TryBeginTransition(Action atBlack, Action onComplete = null)
        {
            if (_transitionRoutine != null)
            {
                return false;
            }

            _transitionRoutine = StartCoroutine(TransitionRoutine(atBlack, onComplete));
            return true;
        }

        private IEnumerator TransitionRoutine(Action atBlack, Action onComplete)
        {
            yield return FadeTo(1f);
            atBlack?.Invoke();

            if (blackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(blackHoldDuration);
            }

            yield return FadeTo(0f);
            _transitionRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;
            bool blocksInput = alpha > 0.001f;
            _canvasGroup.blocksRaycasts = blocksInput;
            _canvasGroup.interactable = blocksInput;
        }
    }
}
