using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DefenderOfIndependence.Audio;

namespace DefenderOfIndependence.SceneFlow
{
    public sealed class MainMenuProgressionController : MonoBehaviour
    {
        [Header("Existing Menu Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button galleryButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private AudioOptionsPanelController audioOptionsPanel;

        [Header("Level Selection")]
        [SerializeField] private CanvasGroup levelSelectionPanel;
        [SerializeField] private Button levelOneButton;
        [SerializeField] private Button levelTwoButton;
        [SerializeField] private Button levelThreeButton;
        [SerializeField] private Button closeLevelSelectionButton;

        [Header("Messages")]
        [SerializeField] private CanvasGroup messagePanel;
        [SerializeField] private TMP_Text messageTitle;
        [SerializeField] private TMP_Text messageBody;
        [SerializeField] private Button messageContinueButton;
        [SerializeField] private Button messageCancelButton;

        [Header("Initial Gallery Challenge")]
        [SerializeField] private GalleryQuizController galleryQuiz;

        [Header("Scene Flow")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField] private string cutsceneLevelOne = "Cutscene_Level1";
        [SerializeField] private string cutsceneLevelTwo = "Cutscene_Level2";
        [SerializeField] private string cutsceneLevelThree = "Cutscene_Level3";
        [SerializeField] private string galleryScene = "Scene_Gallery";

        private System.Action pendingMessageAction;
        private bool isLoading;

        public bool IsPostGame => GameProgressionState.IsPostGame;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetVisible(levelSelectionPanel, false);
            SetVisible(messagePanel, false);
        }

        private void OnEnable()
        {
            startButton?.onClick.AddListener(HandleStart);
            galleryButton?.onClick.AddListener(HandleGallery);
            optionButton?.onClick.AddListener(OpenOptions);
            quitButton?.onClick.AddListener(QuitGame);
            resetButton?.onClick.AddListener(ConfirmReset);
            levelOneButton?.onClick.AddListener(() => LoadScene(cutsceneLevelOne));
            levelTwoButton?.onClick.AddListener(() => LoadScene(cutsceneLevelTwo));
            levelThreeButton?.onClick.AddListener(() => LoadScene(cutsceneLevelThree));
            closeLevelSelectionButton?.onClick.AddListener(() => SetVisible(levelSelectionPanel, false));
            messageContinueButton?.onClick.AddListener(ContinueMessage);
            messageCancelButton?.onClick.AddListener(CloseMessage);
            if (galleryQuiz != null) galleryQuiz.Completed += HandleQuizCompleted;
        }

        private void OnDisable()
        {
            startButton?.onClick.RemoveListener(HandleStart);
            galleryButton?.onClick.RemoveListener(HandleGallery);
            optionButton?.onClick.RemoveListener(OpenOptions);
            quitButton?.onClick.RemoveListener(QuitGame);
            resetButton?.onClick.RemoveListener(ConfirmReset);
            closeLevelSelectionButton?.onClick.RemoveAllListeners();
            levelOneButton?.onClick.RemoveAllListeners();
            levelTwoButton?.onClick.RemoveAllListeners();
            levelThreeButton?.onClick.RemoveAllListeners();
            messageContinueButton?.onClick.RemoveListener(ContinueMessage);
            messageCancelButton?.onClick.RemoveListener(CloseMessage);
            if (galleryQuiz != null) galleryQuiz.Completed -= HandleQuizCompleted;
        }

        private void HandleStart()
        {
            if (IsPostGame) SetVisible(levelSelectionPanel, true);
            else LoadScene(cutsceneLevelOne);
        }

        private void HandleGallery()
        {
            if (IsPostGame)
            {
                GallerySpawnSession.RequestInitialSpawn();
                LoadScene(galleryScene);
                return;
            }

            ShowMessage("THE ARCHIVE IS LOCKED",
                "The Gallery opens after the independence journey is complete. You may unlock it early by proving that you understand Malaya's path to independence. Answer every question correctly to continue.",
                () => galleryQuiz?.Open(), true);
        }

        private void HandleQuizCompleted(int correct, int total)
        {
            if (total > 0 && correct == total)
            {
                GameProgressionState.UnlockPostGame();
                ShowMessage("GALLERY UNLOCKED",
                    "Perfect score. Post-game mode is now active. START lets you revisit any level, and GALLERY opens the museum directly.", null, false);
            }
            else
            {
                ShowMessage("KEEP LEARNING",
                    $"You answered {correct} of {total} correctly. Review the story and try again when you are ready.", null, false);
            }
        }

        private void ConfirmReset()
        {
            ShowMessage("RESET STORY PROGRESS?",
                "This returns the game to its initial state and locks post-game level selection and the Gallery. Audio and option settings are not affected.",
                ResetProgress, true);
        }

        private void ResetProgress()
        {
            GameProgressionState.ResetToInitialState();
            SetVisible(levelSelectionPanel, false);
            ShowMessage("PROGRESS RESET", "The next game will begin from the Level 1 cutscene.", null, false);
        }

        private void ShowOptionsMessage()
        {
            ShowMessage("OPTIONS", "Sound, music, and display settings will be added in the next polish pass.", null, false);
        }

        private void OpenOptions()
        {
            if (audioOptionsPanel != null) audioOptionsPanel.Open();
            else ShowOptionsMessage();
        }

        private void ShowMessage(string title, string body, System.Action continueAction, bool showCancel)
        {
            pendingMessageAction = continueAction;
            if (messageTitle != null) messageTitle.text = title;
            if (messageBody != null) messageBody.text = body;
            if (messageContinueButton != null)
                messageContinueButton.GetComponentInChildren<TMP_Text>(true).text = continueAction == null ? "CLOSE" : "CONTINUE";
            if (messageCancelButton != null) messageCancelButton.gameObject.SetActive(showCancel);
            SetVisible(messagePanel, true);
        }

        private void ContinueMessage()
        {
            System.Action action = pendingMessageAction;
            CloseMessage();
            action?.Invoke();
        }

        private void CloseMessage()
        {
            pendingMessageAction = null;
            SetVisible(messagePanel, false);
        }

        private void LoadScene(string sceneName)
        {
            if (!isLoading) StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot load scene '{sceneName}'.", this);
                yield break;
            }

            isLoading = true;
            SetVisible(levelSelectionPanel, false);
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.blocksRaycasts = true;
                float from = fadeOverlay.alpha;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeOverlay.alpha = Mathf.Lerp(from, 1f, fadeDuration <= 0f ? 1f : elapsed / fadeDuration);
                    yield return null;
                }
            }
            SceneManager.LoadScene(sceneName);
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.gameObject.SetActive(true);
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
