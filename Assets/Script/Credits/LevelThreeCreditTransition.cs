using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelThreeCreditTransition : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string creditsSceneName = "Scene_Credits";
    [SerializeField] private string gallerySceneName = "Scene_Gallery";

    [Header("Fade")]
    [SerializeField] private CanvasGroup blackFadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;

    public bool IsTransitioning { get; private set; }

    public void BeginCreditsFromLevelCompletion()
    {
        if (IsTransitioning || !isActiveAndEnabled) return;
        StartCoroutine(FadeAndLoadCredits());
    }

    private IEnumerator FadeAndLoadCredits()
    {
        IsTransitioning = true;
        if (blackFadeOverlay != null)
        {
            blackFadeOverlay.gameObject.SetActive(true);
            blackFadeOverlay.interactable = false;
            blackFadeOverlay.blocksRaycasts = true;
            float from = blackFadeOverlay.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                blackFadeOverlay.alpha = Mathf.Lerp(from, 1f,
                    fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            blackFadeOverlay.alpha = 1f;
            // Keep one fully black rendered frame before replacing the scene.
            yield return null;
        }

        CreditPlaybackSession.RequestFromLevelCompletion(gallerySceneName);
        SceneManager.LoadScene(creditsSceneName);
    }
}
