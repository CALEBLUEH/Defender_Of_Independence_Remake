using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOneInstructionController : MonoBehaviour
    {
        [Header("Instruction Panel")]
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private ScrollRect instructionScroll;

        [Header("Gameplay Systems")]
        [SerializeField] private LevelOneEnemyDirector enemyDirector;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private FirstPersonWeaponController weapon;
        [SerializeField] private LevelOnePlayerInput gameplayInput;
        [SerializeField] private LevelOnePlayerInteractor interactor;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;

        private bool _gameplayStarted;

        public bool IsAwaitingStart => !_gameplayStarted;

        private void OnEnable()
        {
            startButton?.onClick.AddListener(BeginGameplay);
        }

        private void OnDisable()
        {
            startButton?.onClick.RemoveListener(BeginGameplay);
        }

        private void Start()
        {
            ShowInstructions();
        }

        private void ShowInstructions()
        {
            _gameplayStarted = false;
            SetPlayerControls(false);
            instructionPanel?.SetActive(true);
            if (instructionScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                instructionScroll.verticalNormalizedPosition = 1f;
            }

            Time.timeScale = 0f;
            SetCursorMode(false);
        }

        public void BeginGameplay()
        {
            if (_gameplayStarted)
            {
                return;
            }

            _gameplayStarted = true;
            instructionPanel?.SetActive(false);
            Time.timeScale = 1f;
            SetPlayerControls(true);
            SetCursorMode(true);
            enemyDirector?.BeginGameplay();
        }

        private void SetCursorMode(bool gameplayMode)
        {
            if (starterAssetsInputs == null && movement != null)
            {
                starterAssetsInputs = movement.GetComponent<StarterAssetsInputs>();
            }

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorLocked = gameplayMode;
                starterAssetsInputs.cursorInputForLook = gameplayMode;
            }

            Cursor.lockState = gameplayMode ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplayMode;
        }

        private void SetPlayerControls(bool enabled)
        {
            if (movement != null)
            {
                movement.enabled = enabled;
            }

            if (weapon != null)
            {
                weapon.enabled = enabled;
            }

            if (gameplayInput != null)
            {
                gameplayInput.enabled = enabled;
            }

            if (interactor != null)
            {
                interactor.enabled = enabled;
            }

            if (playerInput != null)
            {
                playerInput.enabled = enabled;
            }
        }
    }
}
