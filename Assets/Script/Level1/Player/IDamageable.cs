using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}
