using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    [RequireComponent(typeof(Collider))]
    public sealed class TentRescueZone : MonoBehaviour
    {
        private LevelOneObjectiveController _objective;

        public void Initialize(LevelOneObjectiveController objective)
        {
            _objective = objective;
        }

        private void OnTriggerEnter(Collider other)
        {
            HostageEscort hostage = other.GetComponentInParent<HostageEscort>();
            if (hostage != null)
            {
                _objective?.TryRescue(hostage);
            }
        }
    }
}
