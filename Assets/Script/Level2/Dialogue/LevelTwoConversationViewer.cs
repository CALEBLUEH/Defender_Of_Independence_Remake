using Fungus;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoConversationViewer : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private SayDialog sayDialog;
        [SerializeField] private Writer writer;
        [SerializeField] private DialogInput dialogInput;
        [SerializeField] private GameObject choiceRoot;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;
        [SerializeField] private LevelTwoFirstPersonController playerController;
        [SerializeField] private LevelTwoConversationCameraFocus cameraFocus;
        [SerializeField] private LevelTwoNegotiationMeterController meterController;
        [SerializeField, Min(100f)] private float choiceSpacing = 440f;

        private LevelTwoConversationTrigger.ConversationStep[] _steps;
        private int _stepIndex;
        private int _lineIndex;
        private UnityAction[] _choiceActions;
        private bool _isOpen;
        private int _session;
        private int[] _displayedChoiceIndices;
        private LevelTwoNegotiationMeterController.MeterChange _pendingMeterChange;
        private bool _calculateFinalResult;

        public bool IsOpen => _isOpen;
        public bool CanBegin => panel != null && sayDialog != null && writer != null && !_isOpen;
        public int CurrentStepIndex => _stepIndex;
        public bool IsWaitingForChoice => _isOpen && choiceRoot != null && choiceRoot.activeSelf;
        public int DisplayedChoiceCount => _displayedChoiceIndices?.Length ?? 0;

        public int GetDisplayedChoiceSourceIndex(int displayedIndex)
        {
            return _displayedChoiceIndices != null && displayedIndex >= 0 && displayedIndex < _displayedChoiceIndices.Length
                ? _displayedChoiceIndices[displayedIndex]
                : -1;
        }

        public float GetDisplayedChoicePositionX(int displayedIndex)
        {
            if (choiceButtons == null || displayedIndex < 0 || displayedIndex >= choiceButtons.Length ||
                choiceButtons[displayedIndex] == null) return float.NaN;
            RectTransform rect = choiceButtons[displayedIndex].transform as RectTransform;
            return rect != null ? rect.anchoredPosition.x : float.NaN;
        }

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (choiceButtons == null) return;
            _choiceActions = new UnityAction[choiceButtons.Length];
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                int selectedIndex = index;
                _choiceActions[index] = () => SelectChoice(selectedIndex);
                choiceButtons[index]?.onClick.AddListener(_choiceActions[index]);
            }
        }

        private void OnDisable()
        {
            if (choiceButtons == null || _choiceActions == null) return;
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                choiceButtons[index]?.onClick.RemoveListener(_choiceActions[index]);
            }
        }

        public bool Begin(LevelTwoConversationTrigger.ConversationStep[] steps, Transform focusTarget = null,
            bool calculateFinalResult = false)
        {
            if (!CanBegin || steps == null || steps.Length == 0) return false;

            _steps = steps;
            _stepIndex = 0;
            _lineIndex = 0;
            _isOpen = true;
            _pendingMeterChange = default;
            _calculateFinalResult = calculateFinalResult;
            _session++;
            panel.SetActive(true);
            SetChoicesVisible(false);
            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(false);
            cameraFocus?.Focus(focusTarget);
            ShowNextOpeningLine(_session);
            return true;
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!IsWaitingForChoice || _steps == null || _stepIndex >= _steps.Length) return;

            LevelTwoConversationTrigger.ConversationChoice[] choices = _steps[_stepIndex].choices;
            if (choices == null || _displayedChoiceIndices == null || choiceIndex < 0 ||
                choiceIndex >= _displayedChoiceIndices.Length) return;

            SetChoicesVisible(false);
            playerController?.SetUiCursorActive(false);
            int sourceChoiceIndex = _displayedChoiceIndices[choiceIndex];
            LevelTwoConversationTrigger.ConversationChoice choice = choices[sourceChoiceIndex];
            _pendingMeterChange += choice.meterChange;
            int session = _session;
            SayLine(choice.responseSpeaker, choice.response, () => CompleteResponse(session));
        }

        public void Continue()
        {
            if (_isOpen) dialogInput?.SetNextLineFlag();
        }

        public void Close()
        {
            Close(null);
        }

        private void Close(System.Action onCameraRestored)
        {
            if (!_isOpen) return;
            _session++;
            _isOpen = false;
            _steps = null;
            SetChoicesVisible(false);
            sayDialog?.Stop();
            if (panel != null) panel.SetActive(false);
            playerController?.SetUiCursorActive(false);
            System.Action restoreComplete = () =>
            {
                playerController?.SetControlsEnabled(true);
                onCameraRestored?.Invoke();
            };
            if (cameraFocus != null) cameraFocus.Restore(restoreComplete);
            else restoreComplete();
        }

        private void ShowNextOpeningLine(int session)
        {
            if (!_isOpen || session != _session || _steps == null || _stepIndex >= _steps.Length) return;

            LevelTwoConversationTrigger.ConversationStep step = _steps[_stepIndex];
            LevelTwoConversationTrigger.ConversationLine[] lines = step.lines;
            if (lines != null && lines.Length > 0)
            {
                if (_lineIndex < lines.Length)
                {
                    LevelTwoConversationTrigger.ConversationLine line = lines[_lineIndex++];
                    SayLine(line.speaker, line.text, () => ShowNextOpeningLine(session));
                    return;
                }
            }
            else if (_lineIndex == 0)
            {
                _lineIndex = 1;
                SayLine(step.speaker, step.prompt, () => ShowNextOpeningLine(session));
                return;
            }

            ShowChoices(step.choices);
        }

        private void ShowChoices(LevelTwoConversationTrigger.ConversationChoice[] choices)
        {
            if (!_isOpen || choices == null || choices.Length == 0)
            {
                CompleteResponse(_session);
                return;
            }

            int displayedCount = Mathf.Min(choices.Length, choiceButtons.Length);
            _displayedChoiceIndices = new int[displayedCount];
            for (int index = 0; index < displayedCount; index++) _displayedChoiceIndices[index] = index;
            for (int index = displayedCount - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                (_displayedChoiceIndices[index], _displayedChoiceIndices[swapIndex]) =
                    (_displayedChoiceIndices[swapIndex], _displayedChoiceIndices[index]);
            }

            float startX = -(displayedCount - 1) * choiceSpacing * 0.5f;
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                bool visible = index < displayedCount;
                choiceButtons[index].gameObject.SetActive(visible);
                if (!visible) continue;
                if (index < choiceLabels.Length) choiceLabels[index].text = choices[_displayedChoiceIndices[index]].text;
                RectTransform rect = choiceButtons[index].transform as RectTransform;
                if (rect != null)
                {
                    Vector2 position = rect.anchoredPosition;
                    position.x = startX + index * choiceSpacing;
                    rect.anchoredPosition = position;
                }
            }

            if (choiceRoot != null) choiceRoot.SetActive(true);
            playerController?.SetUiCursorActive(true);
        }

        private void CompleteResponse(int session)
        {
            if (!_isOpen || session != _session) return;

            _stepIndex++;
            _lineIndex = 0;
            if (_steps == null || _stepIndex >= _steps.Length)
            {
                CompleteConversation();
                return;
            }

            ShowNextOpeningLine(session);
        }

        private void CompleteConversation()
        {
            meterController?.ApplyConversationResult(_pendingMeterChange);
            bool showFinalResult = _calculateFinalResult;
            System.Action finalAction = showFinalResult && meterController != null
                ? meterController.BeginFinalResult
                : null;
            Close(finalAction);
        }

        private void SayLine(string speaker, string text, System.Action onComplete)
        {
            sayDialog.SetCharacterName(speaker ?? string.Empty, new Color(0.22f, 0.13f, 0.07f));
            sayDialog.Say(text ?? string.Empty, true, true, false, true, false, null, onComplete);
        }

        private void SetChoicesVisible(bool visible)
        {
            if (choiceRoot != null) choiceRoot.SetActive(visible);
            if (choiceButtons == null) return;
            foreach (Button button in choiceButtons)
            {
                if (button != null) button.gameObject.SetActive(visible);
            }
        }
    }
}
