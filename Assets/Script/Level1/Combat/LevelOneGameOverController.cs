using StarterAssets;
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

        private bool _gameOver;

        private void Awake()
        {
            Time.timeScale = 1f;
            LockGameplayCursor();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
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

            gameOverPanel?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
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
