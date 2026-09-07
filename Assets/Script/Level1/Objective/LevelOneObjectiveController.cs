using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOneObjectiveController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private CombatHealth playerHealth;
        [SerializeField] private HostageEscort[] hostages;
        [SerializeField] private TentRescueZone tentRescueZone;

        [Header("Player Control")]
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private FirstPersonWeaponController weapon;
        [SerializeField] private PlayerInput playerInput;

        [Header("HUD")]
        [SerializeField] private TMP_Text taskText;
        [SerializeField] private TMP_Text rescuedText;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField, Min(0.25f)] private float warningDuration = 2.5f;
        [SerializeField] private string nextCutsceneName = "Cutscene_Level2";

        private HostageEscort _currentHostage;
        private int _savedHostages;
        private float _warningEndsAt;
        private bool _victoryShown;

        public HostageEscort CurrentHostage => _currentHostage;
        public int SavedHostages => _savedHostages;
        public int TotalHostages => hostages == null ? 0 : hostages.Length;
        public bool VictoryShown => _victoryShown;

        private void Awake()
        {
            foreach (HostageEscort hostage in hostages)
            {
                hostage?.Initialize(this, player);
            }

            tentRescueZone?.Initialize(this);
            victoryPanel?.SetActive(false);
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }

            RefreshHud();
        }

        private void OnEnable()
        {
            nextLevelButton?.onClick.AddListener(LoadNextCutscene);
        }

        private void OnDisable()
        {
            nextLevelButton?.onClick.RemoveListener(LoadNextCutscene);
        }

        private void Update()
        {
            if (warningText != null && warningText.gameObject.activeSelf && Time.unscaledTime >= _warningEndsAt)
            {
                warningText.gameObject.SetActive(false);
            }
        }

        public void RequestEscort(HostageEscort hostage)
        {
            if (hostage == null || hostage.IsSaved || _victoryShown || (playerHealth != null && playerHealth.IsDead))
            {
                return;
            }

            if (_currentHostage != null && _currentHostage != hostage)
            {
                ShowWarning("ONLY ONE HOSTAGE CAN FOLLOW AT A TIME");
                return;
            }

            if (_currentHostage == hostage)
            {
                ShowWarning("THIS HOSTAGE IS ALREADY FOLLOWING YOU");
                return;
            }

            _currentHostage = hostage;
            hostage.BeginFollowing();
            RefreshHud();
        }

        public void TryRescue(HostageEscort hostage)
        {
            if (hostage == null || hostage != _currentHostage || !hostage.IsFollowing || hostage.IsSaved || _victoryShown)
            {
                return;
            }

            hostage.MarkSaved();
            _currentHostage = null;
            _savedHostages++;
            RefreshHud();
            if (_savedHostages >= TotalHostages && TotalHostages > 0)
            {
                ShowVictory();
            }
        }

        private void RefreshHud()
        {
            if (taskText != null)
            {
                taskText.text = _currentHostage == null
                    ? "OBJECTIVE: FIND A HOSTAGE AND PRESS C"
                    : "OBJECTIVE: ESCORT THE HOSTAGE TO THE TENT";
            }

            if (rescuedText != null)
            {
                rescuedText.text = $"HOSTAGES SAVED: {_savedHostages} / {TotalHostages}";
            }
        }

        private void ShowWarning(string message)
        {
            if (warningText == null)
            {
                return;
            }

            warningText.text = message;
            warningText.gameObject.SetActive(true);
            _warningEndsAt = Time.unscaledTime + warningDuration;
        }

        private void ShowVictory()
        {
            _victoryShown = true;
            if (taskText != null)
            {
                taskText.text = "OBJECTIVE COMPLETE: BOTH HOSTAGES ARE SAFE";
            }

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

            victoryPanel?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        public void LoadNextCutscene()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(nextCutsceneName);
        }

        private void OnValidate()
        {
            warningDuration = Mathf.Max(0.25f, warningDuration);
        }
    }
}
