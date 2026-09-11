using System.Collections;
using StarterAssets;
using DefenderOfIndependence.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOneGameOverController : MonoBehaviour
    {
        [SerializeField] private CombatHealth health;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private FirstPersonWeaponController weapon;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private string mainMenuSceneName = "Scene_MainMenu";
        [SerializeField] private CanvasGroup loseFadeOverlay;
        [SerializeField, Min(0f)] private float loseFadeDuration = 0.8f;

        private bool _gameOver;

        public bool GameOverShown => gameOverPanel != null && gameOverPanel.activeSelf;
        public float LoseFadeAlpha => loseFadeOverlay != null ? loseFadeOverlay.alpha : 0f;

        private void Awake()
        {
            Time.timeScale = 1f;
            LockGameplayCursor();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            if (loseFadeOverlay != null)
            {
                loseFadeOverlay.alpha = 0f;
                loseFadeOverlay.interactable = false;
                loseFadeOverlay.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += ShowGameOver;
            }

            restartButton?.onClick.AddListener(RestartLevel);
            backToMenuButton?.onClick.AddListener(BackToMenu);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= ShowGameOver;
            }

            restartButton?.onClick.RemoveListener(RestartLevel);
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
        }

        private void ShowGameOver(CombatHealth sender)
        {
            if (_gameOver)
            {
                return;
            }

            _gameOver = true;
            if (movement != null)
            {
                movement.enabled = false;
            }

            if (weapon != null)
            {
                weapon.enabled = false;
            }

            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            GameAudioService.Instance?.PlayLossMusic();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            StartCoroutine(FadeToGameOver());
        }

        private IEnumerator FadeToGameOver()
        {
            if (loseFadeOverlay != null)
            {
                loseFadeOverlay.gameObject.SetActive(true);
                loseFadeOverlay.blocksRaycasts = true;
                loseFadeOverlay.interactable = false;
                float elapsed = 0f;
                while (elapsed < loseFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    loseFadeOverlay.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, loseFadeDuration));
                    yield return null;
                }
                loseFadeOverlay.alpha = 1f;
            }

            gameOverPanel?.SetActive(true);
            if (loseFadeOverlay != null)
            {
                float elapsed = 0f;
                while (elapsed < loseFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    loseFadeOverlay.alpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, loseFadeDuration));
                    yield return null;
                }
                loseFadeOverlay.alpha = 0f;
                loseFadeOverlay.blocksRaycasts = false;
            }
        }

        public void RestartLevel()
        {
            ResumeTime();
            LockGameplayCursor();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void BackToMenu()
        {
            ResumeTime();
            UnlockCursor();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private static void ResumeTime()
        {
            Time.timeScale = 1f;
        }

        private static void LockGameplayCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
