using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoNextDayClock : MonoBehaviour
    {
        [SerializeField] private LevelTwoDayController dayController;
        [SerializeField] private Transform interactionPoint;

        public LevelTwoDayController DayController => dayController;
        public Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;

        public string GetPrompt()
        {
            return dayController != null && dayController.CanAdvanceDay
                ? "PRESS C TO END THE DAY"
                : "FINAL DAY - PROCEED TO THE BRITISH OFFICE";
        }

        public bool TryUse()
        {
            return dayController != null && dayController.TryAdvanceDay();
        }
    }
}
