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
        }

        [Serializable]
        public struct ConversationStep
        {
            public string speaker;
            [TextArea(3, 10)] public string prompt;
            public ConversationChoice[] choices;
        }

        [SerializeField, Range(1, 6)] private int activeDay = 1;
        [SerializeField] private string conversationDisplayName = "CONVERSATION";
        [SerializeField] private Collider interactionCollider;
        [SerializeField, Min(0)] private int energyCost = 1;
        [SerializeField] private ConversationStep[] steps;

        private bool _consumed;

        public int ActiveDay => activeDay;
        public bool WasConsumed => _consumed;
        public Collider InteractionCollider => interactionCollider;
        public int StepCount => steps?.Length ?? 0;

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
            if (_consumed || viewer == null || !viewer.CanBegin || dayController == null || dayController.CurrentDay != activeDay ||
                steps == null || steps.Length == 0 || dayController.CurrentEnergy < energyCost)
            {
                return false;
            }

            if (!dayController.TrySpendEnergy(energyCost))
            {
                return false;
            }

            if (!viewer.Begin(steps))
            {
                return false;
            }

            _consumed = true;
            return true;
        }
    }
}
