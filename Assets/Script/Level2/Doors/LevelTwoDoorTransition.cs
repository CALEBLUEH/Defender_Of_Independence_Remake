using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDoorTransition : MonoBehaviour
    {
        [SerializeField] private string roomDisplayName = "ROOM";
        [SerializeField] private Transform enterSpawnPoint;
        [SerializeField] private Transform exitSpawnPoint;
        [SerializeField] private bool useSpawnPointRotation;

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

        public string GetPrompt(Vector3 playerPosition)
        {
            string action = IsPlayerOutside(playerPosition) ? "ENTER" : "EXIT";
            return $"PRESS C TO {action} {roomDisplayName}";
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
