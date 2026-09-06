using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.SceneFlow
{
    [RequireComponent(typeof(Button))]
    public sealed class SceneTransitionButton : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

        private Button button;
        private bool isLoading;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(LoadTargetScene);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(LoadTargetScene);
            }
        }

        public void LoadTargetScene()
        {
            if (isLoading)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError($"Cannot load scene '{targetScene}'. Add it to Build Settings first.", this);
                return;
            }

            StartCoroutine(LoadAfterFade());
        }

        private IEnumerator LoadAfterFade()
        {
            isLoading = true;
            button.interactable = false;

            if (fadeOverlay != null && fadeDuration > 0f)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.blocksRaycasts = true;

                float startAlpha = fadeOverlay.alpha;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeOverlay.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
                    yield return null;
                }

                fadeOverlay.alpha = 1f;
            }

            SceneManager.LoadScene(targetScene);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            fadeDuration = Mathf.Max(0f, fadeDuration);
        }
#endif
    }
}
