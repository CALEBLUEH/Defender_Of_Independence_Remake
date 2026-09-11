using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DefenderOfIndependence.Audio;

namespace DefenderOfIndependence.SceneFlow
{
    public enum PauseSceneKind { Cutscene, Gameplay, Credits, Gallery }

    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text optionsNotice;
        [SerializeField] private AudioOptionsPanelController audioOptionsPanel;

        [Header("Scene Behaviour")]
        [SerializeField] private PauseSceneKind sceneKind;
        [SerializeField] private string skipDestinationScene;
        [SerializeField] private bool lockCursorOnResume;
        [SerializeField] private Behaviour[] pauseTargets;
        [SerializeField] private CreditVideoController creditVideo;
        [SerializeField] private LevelThreeCreditTransition levelThreeCreditTransition;
        [SerializeField] private string mainMenuScene = "Scene_MainMenu";

        private readonly Dictionary<Behaviour, bool> targetStates = new();
        private float previousTimeScale = 1f;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1f;
            SetPanelVisible(false);
            if (optionsNotice != null) optionsNotice.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(sceneKind != PauseSceneKind.Gallery);
        }

        private void OnEnable()
        {
            resumeButton?.onClick.AddListener(Resume);
            optionsButton?.onClick.AddListener(OpenOptions);
            backToMenuButton?.onClick.AddListener(BackToMenu);
            skipButton?.onClick.AddListener(SkipCurrentContent);
        }

        private void OnDisable()
        {
            resumeButton?.onClick.RemoveListener(Resume);
            optionsButton?.onClick.RemoveListener(OpenOptions);
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
            skipButton?.onClick.RemoveListener(SkipCurrentContent);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            targetStates.Clear();
            if (pauseTargets != null)
            {
                foreach (Behaviour target in pauseTargets)
                {
                    if (target == null || target == this) continue;
                    targetStates[target] = target.enabled;
                    target.enabled = false;
                }
            }
            creditVideo?.SetPaused(true);
            GameAudioService.Instance?.PauseMusic();
            SetPanelVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            RestoreRuntimeState();
            audioOptionsPanel?.Close();
            SetPanelVisible(false);
            if (optionsNotice != null) optionsNotice.gameObject.SetActive(false);
            Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = false;
        }

        public void BackToMenu()
        {
            PrepareToLeave();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuScene);
        }

        public void SkipCurrentContent()
        {
            PrepareToLeave();
            if (sceneKind == PauseSceneKind.Credits && creditVideo != null)
            {
                creditVideo.SkipVideo();
                return;
            }
            if (levelThreeCreditTransition != null)
            {
                levelThreeCreditTransition.BeginCreditsFromLevelCompletion();
                return;
            }
            if (!string.IsNullOrWhiteSpace(skipDestinationScene)) SceneManager.LoadScene(skipDestinationScene);
        }

        private void ToggleOptionsNotice()
        {
            if (optionsNotice != null) optionsNotice.gameObject.SetActive(!optionsNotice.gameObject.activeSelf);
        }

        private void OpenOptions()
        {
            if (audioOptionsPanel != null) audioOptionsPanel.Open();
            else ToggleOptionsNotice();
        }

        private void PrepareToLeave()
        {
            if (IsPaused) RestoreRuntimeState();
            audioOptionsPanel?.Close();
            SetPanelVisible(false);
        }

        private void RestoreRuntimeState()
        {
            foreach (KeyValuePair<Behaviour, bool> entry in targetStates)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            targetStates.Clear();
            creditVideo?.SetPaused(false);
            GameAudioService.Instance?.ResumeMusic();
            Time.timeScale = previousTimeScale;
            IsPaused = false;
        }

        private void SetPanelVisible(bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
        }

        private void OnDestroy()
        {
            if (IsPaused) RestoreRuntimeState();
        }
    }
}
