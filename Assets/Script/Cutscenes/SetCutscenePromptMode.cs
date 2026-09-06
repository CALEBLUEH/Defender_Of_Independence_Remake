using Fungus;
using UnityEngine;

namespace DefenderOfIndependence.Cutscenes
{
    [CommandInfo("Cutscene", "Set Final Prompt", "Changes the dialogue prompt for the final cutscene line.")]
    [AddComponentMenu("")]
    public sealed class SetCutscenePromptMode : Command
    {
        [SerializeField] private CutscenePromptController promptController;
        [SerializeField] private bool finalPrompt = true;

        public override void OnEnter()
        {
            if (promptController == null)
            {
                promptController = FindFirstObjectByType<CutscenePromptController>();
            }

            if (promptController == null)
            {
                Debug.LogError("No CutscenePromptController was found in the scene.", this);
            }
            else
            {
                promptController.SetFinalPrompt(finalPrompt);
            }

            Continue();
        }

        public override string GetSummary()
        {
            return finalPrompt ? "PRESS SPACE TO START" : "PRESS SPACE TO CONTINUE";
        }
    }
}
