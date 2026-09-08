using System;
using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoConversationTrigger : MonoBehaviour
    {
        [Serializable]
        public struct ConversationChoice
        {
            [TextArea(2, 5)] public string text;
            public string responseSpeaker;
            [TextArea(3, 8)] public string response;
            public LevelTwoNegotiationMeterController.MeterChange meterChange;
        }

        [Serializable]
        public struct ConversationLine
        {
            public string speaker;
            [TextArea(3, 8)] public string text;
        }

        [Serializable]
        public struct ConversationStep
        {
            [Tooltip("One Fungus Say line per entry. Legacy speaker/prompt values remain as a safe fallback.")]
            public ConversationLine[] lines;
            public string speaker;
            [TextArea(3, 10)] public string prompt;
            public ConversationChoice[] choices;
        }

        [SerializeField, Range(1, 6)] private int activeDay = 1;
        [SerializeField] private string conversationDisplayName = "CONVERSATION";
        [SerializeField] private Collider interactionCollider;
        [SerializeField, Min(0)] private int energyCost = 1;
        [SerializeField] private Transform cameraFocusTarget;
        [SerializeField] private bool calculateFinalResultOnComplete;
        [SerializeField] private ConversationStep[] steps;

        private bool _consumed;

        public int ActiveDay => activeDay;
        public bool WasConsumed => _consumed;
        public Collider InteractionCollider => interactionCollider;
        public int StepCount => steps?.Length ?? 0;
        public int EnergyCost => energyCost;
        public string ConversationDisplayName => conversationDisplayName;
        public Transform CameraFocusTarget => cameraFocusTarget;
        public bool RequiresConfirmation => energyCost > 0;
        public bool CalculatesFinalResult => calculateFinalResultOnComplete;

        public bool CanBegin(LevelTwoDayController dayController)
        {
            return !_consumed && dayController != null && dayController.CurrentDay == activeDay &&
                   steps != null && steps.Length > 0 && dayController.CurrentEnergy >= energyCost;
        }

        public void ApplyDay(int day)
        {
            if (interactionCollider != null)
            {
                interactionCollider.enabled = day == activeDay;
            }
        }

        public bool Contains(Collider candidate)
        {
            return candidate != null && interactionCollider == candidate;
        }

        public string GetPrompt(LevelTwoDayController dayController)
        {
            if (_consumed)
            {
                return "NO CONVERSATION REMAINS HERE TODAY";
            }

            if (dayController == null || dayController.CurrentDay != activeDay)
            {
                return "NO CONVERSATION IS AVAILABLE HERE TODAY";
            }

            if (dayController.CurrentEnergy < energyCost)
            {
                return "NO ENERGY REMAINING - END THE DAY AT THE CLOCK";
            }

            return $"PRESS C TO SPEND {energyCost} ENERGY AND SPEAK WITH {conversationDisplayName}";
        }

        public bool TryBegin(LevelTwoConversationViewer viewer, LevelTwoDayController dayController)
        {
            if (viewer == null || !viewer.CanBegin || !CanBegin(dayController))
            {
                return false;
            }

            if (!dayController.TrySpendEnergy(energyCost))
            {
                return false;
            }

            if (!viewer.Begin(steps, cameraFocusTarget, calculateFinalResultOnComplete))
            {
                return false;
            }

            _consumed = true;
            return true;
        }
    }
}
