using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDoorTransition : MonoBehaviour
    {
        [SerializeField] private string roomDisplayName = "ROOM";
        [SerializeField] private Transform enterSpawnPoint;
        [SerializeField] private Transform exitSpawnPoint;
        [SerializeField] private bool useSpawnPointRotation;
        [SerializeField] private bool availableOnFinalDay;

        private int _paidEntryDay = -1;

        public string RoomDisplayName => roomDisplayName;
        public Transform EnterSpawnPoint => enterSpawnPoint;
        public Transform ExitSpawnPoint => exitSpawnPoint;

        public Transform GetDestination(Vector3 playerPosition)
        {
            if (enterSpawnPoint == null || exitSpawnPoint == null)
            {
                return null;
            }

            return IsPlayerOutside(playerPosition) ? enterSpawnPoint : exitSpawnPoint;
        }

        public bool IsEntering(Vector3 playerPosition) => IsPlayerOutside(playerPosition);

        public bool IsLocked(Vector3 playerPosition, LevelTwoDayController dayController)
        {
            return IsPlayerOutside(playerPosition) && dayController != null &&
                   dayController.CurrentDay == dayController.FinalDay && !availableOnFinalDay;
        }

        public bool RequiresEntryEnergy(Vector3 playerPosition, int currentDay)
        {
            return IsPlayerOutside(playerPosition) && _paidEntryDay != currentDay;
        }

        public void MarkEntryPaid(int currentDay)
        {
            _paidEntryDay = currentDay;
        }

        public string GetPrompt(Vector3 playerPosition, LevelTwoDayController dayController)
        {
            if (IsLocked(playerPosition, dayController))
            {
                return "THIS ROOM IS CLOSED FOR THE FINAL CONFERENCE";
            }

            if (!IsPlayerOutside(playerPosition))
            {
                return $"PRESS C TO EXIT {roomDisplayName}";
            }

            int day = dayController != null ? dayController.CurrentDay : -1;
            if (RequiresEntryEnergy(playerPosition, day))
            {
                if (dayController == null || dayController.CurrentEnergy < 1)
                {
                    return "NO ENERGY REMAINING - END THE DAY AT THE CLOCK";
                }

                return $"PRESS C TO SPEND 1 ENERGY AND ENTER {roomDisplayName}";
            }

            return $"PRESS C TO ENTER {roomDisplayName}";
        }

        public Vector3 GetArrivalLookDirection(Transform destination)
        {
            if (destination == null)
            {
                return transform.forward;
            }

            if (useSpawnPointRotation)
            {
                return destination.forward;
            }

            Vector3 awayFromDoor = destination.position - transform.position;
            awayFromDoor.y = 0f;
            return awayFromDoor.sqrMagnitude > 0.001f ? awayFromDoor.normalized : destination.forward;
        }

        private bool IsPlayerOutside(Vector3 playerPosition)
        {
            if (enterSpawnPoint == null || exitSpawnPoint == null)
            {
                return true;
            }

            float distanceToOutside = Vector3.SqrMagnitude(playerPosition - exitSpawnPoint.position);
            float distanceToInside = Vector3.SqrMagnitude(playerPosition - enterSpawnPoint.position);
            return distanceToOutside <= distanceToInside;
        }
    }
}
