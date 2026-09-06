using Fungus;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefenderOfIndependence.Cutscenes
{
    public sealed class CutscenePromptController : MonoBehaviour
    {
        [SerializeField] private Writer writer;
        [SerializeField] private DialogInput dialogInput;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private string continuePrompt = "PRESS SPACE TO CONTINUE";
        [SerializeField] private string finalPrompt = "PRESS SPACE TO START";
        [SerializeField, Min(0.1f)] private float pulseSpeed = 1.5f;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.35f;

        private bool useFinalPrompt;

        private void OnEnable()
        {
            SetFinalPrompt(false);
            SetPromptAlpha(0f);
        }

        private void Update()
        {
            if (dialogInput != null && Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                dialogInput.SetNextLineFlag();
            }

            bool shouldShow = writer != null && writer.IsWaitingForInput;
            if (!shouldShow)
            {
                SetPromptAlpha(0f);
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            SetPromptAlpha(Mathf.Lerp(minimumAlpha, 1f, pulse));
        }

        public void SetFinalPrompt(bool isFinal)
        {
            useFinalPrompt = isFinal;
            if (promptLabel != null)
            {
                promptLabel.text = useFinalPrompt ? finalPrompt : continuePrompt;
            }
        }

        private void SetPromptAlpha(float alpha)
        {
            if (promptGroup != null)
            {
                promptGroup.alpha = alpha;
            }
        }
    }
}
