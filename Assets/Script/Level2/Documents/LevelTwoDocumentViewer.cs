using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDocumentViewer : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject dayPromptRoot;
        [SerializeField] private TMP_Text dayPromptText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private ScrollRect documentScroll;
        [SerializeField] private Button closeButton;
        [SerializeField] private LevelTwoFirstPersonController playerController;

        private int _openedFrame = -1;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= _openedFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.cKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        public void Show(string title, string body, bool showDayPrompt, int day, string dayTitle, string recommendation)
        {
            if (panel == null)
            {
                return;
            }

            titleText.text = title;
            bodyText.text = body;
            dayPromptRoot.SetActive(showDayPrompt);
            if (showDayPrompt)
            {
                string dayLabel = day >= 6 ? "FINAL DAY" : $"DAY {day}";
                dayPromptText.text = $"<b>{dayLabel} - {dayTitle.ToUpperInvariant()}</b>\n{recommendation}";
            }

            panel.SetActive(true);
            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(true);
            _openedFrame = Time.frameCount;
            Canvas.ForceUpdateCanvases();
            if (documentScroll != null)
            {
                documentScroll.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            panel.SetActive(false);
            playerController?.SetUiCursorActive(false);
            playerController?.SetControlsEnabled(true);
        }
    }
}
