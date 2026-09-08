using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoConversationViewer : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueLabel;
        [SerializeField] private LevelTwoFirstPersonController playerController;

        private LevelTwoConversationTrigger.ConversationStep[] _steps;
        private int _stepIndex;
        private UnityAction[] _choiceActions;

        public bool IsOpen => panel != null && panel.activeSelf;
        public bool CanBegin => panel != null && !panel.activeSelf;
        public int CurrentStepIndex => _stepIndex;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (choiceButtons != null)
            {
                _choiceActions = new UnityAction[choiceButtons.Length];
                for (int index = 0; index < choiceButtons.Length; index++)
                {
                    int selectedIndex = index;
                    _choiceActions[index] = () => SelectChoice(selectedIndex);
                    choiceButtons[index]?.onClick.AddListener(_choiceActions[index]);
                }
            }

            continueButton?.onClick.AddListener(Continue);
        }

        private void OnDisable()
        {
            if (choiceButtons != null && _choiceActions != null)
            {
                for (int index = 0; index < choiceButtons.Length; index++)
                {
                    choiceButtons[index]?.onClick.RemoveListener(_choiceActions[index]);
                }
            }

            continueButton?.onClick.RemoveListener(Continue);
        }

        public bool Begin(LevelTwoConversationTrigger.ConversationStep[] steps)
        {
            if (!CanBegin || steps == null || steps.Length == 0)
            {
                return false;
            }

            _steps = steps;
            _stepIndex = 0;
            panel.SetActive(true);
            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(true);
            ShowQuestion();
            return true;
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!IsOpen || _steps == null || _stepIndex >= _steps.Length)
            {
                return;
            }

            LevelTwoConversationTrigger.ConversationChoice[] choices = _steps[_stepIndex].choices;
            if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Length)
            {
                return;
            }

            LevelTwoConversationTrigger.ConversationChoice choice = choices[choiceIndex];
            speakerText.text = choice.responseSpeaker;
            dialogueText.text = choice.response;
            SetChoicesVisible(false);
            continueButton.gameObject.SetActive(true);
            continueLabel.text = _stepIndex >= _steps.Length - 1 ? "END CONVERSATION" : "CONTINUE";
        }

        public void Continue()
        {
            if (!IsOpen)
            {
                return;
            }

            _stepIndex++;
            if (_steps == null || _stepIndex >= _steps.Length)
            {
                Close();
                return;
            }

            ShowQuestion();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            panel.SetActive(false);
            _steps = null;
            playerController?.SetUiCursorActive(false);
            playerController?.SetControlsEnabled(true);
        }

        private void ShowQuestion()
        {
            LevelTwoConversationTrigger.ConversationStep step = _steps[_stepIndex];
            speakerText.text = step.speaker;
            dialogueText.text = step.prompt;
            continueButton.gameObject.SetActive(false);

            for (int index = 0; index < choiceButtons.Length; index++)
            {
                bool visible = step.choices != null && index < step.choices.Length;
                choiceButtons[index].gameObject.SetActive(visible);
                if (visible) choiceLabels[index].text = step.choices[index].text;
            }
        }

        private void SetChoicesVisible(bool visible)
        {
            if (choiceButtons == null) return;
            foreach (Button button in choiceButtons) button?.gameObject.SetActive(visible);
        }
    }
}
