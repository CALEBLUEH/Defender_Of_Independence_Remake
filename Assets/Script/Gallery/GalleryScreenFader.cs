using System;
using System.Collections;
using UnityEngine;

public sealed class GalleryScreenFader : MonoBehaviour
{
    [SerializeField] private GalleryFirstPersonController playerController;
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField, Min(0f)] private float sceneEntryFadeDuration = 0.75f;
    [SerializeField, Min(0f)] private float sceneExitFadeDuration = 0.75f;

    private Coroutine fadeRoutine;
    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        playerController?.SetControlsEnabled(false);
        if (blackOverlay == null) return;
        blackOverlay.gameObject.SetActive(true);
        blackOverlay.alpha = 1f;
        blackOverlay.interactable = false;
        blackOverlay.blocksRaycasts = true;
    }

    private void Start() => fadeRoutine = StartCoroutine(Fade(1f, 0f, sceneEntryFadeDuration,
        () => playerController?.SetControlsEnabled(true)));

    public bool TryFadeOut(Action atBlack)
    {
        if (IsTransitioning || !isActiveAndEnabled) return false;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(blackOverlay != null ? blackOverlay.alpha : 0f, 1f,
            sceneExitFadeDuration, atBlack));
        return true;
    }

    private IEnumerator Fade(float from, float to, float duration, Action onComplete)
    {
        IsTransitioning = true;
        if (blackOverlay != null)
        {
            blackOverlay.blocksRaycasts = true;
            blackOverlay.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                blackOverlay.alpha = Mathf.Lerp(from, to,
                    duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            blackOverlay.alpha = to;
            blackOverlay.blocksRaycasts = to > 0.001f;
        }
        IsTransitioning = false;
        fadeRoutine = null;
        onComplete?.Invoke();
    }
}
