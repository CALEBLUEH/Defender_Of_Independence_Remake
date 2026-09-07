using System;
using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    public enum CombatTeam
    {
        Neutral,
        Player,
        Enemy
    }

    public sealed class CombatHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [SerializeField] private string displayName = "Target";
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private Transform aimPoint;

        private float _currentHealth;

        public event Action<CombatHealth> Changed;
        public event Action<CombatHealth> Damaged;
        public event Action<CombatHealth> Died;

        public CombatTeam Team => team;
        public string DisplayName => displayName;
        public float CurrentHealth => _currentHealth;
        public float MaximumHealth => maximumHealth;
        public float HealthPercent => maximumHealth <= 0f ? 0f : _currentHealth / maximumHealth;
        public bool IsDead => _currentHealth <= 0f;
        public Vector3 AimPointPosition => aimPoint == null ? transform.position + Vector3.up : aimPoint.position;

        private void Awake()
        {
            _currentHealth = maximumHealth;
        }

        private void Start()
        {
            // Presenters subscribe in OnEnable, so publish the initialized value once
            // after every scene object has finished Awake.
            Changed?.Invoke(this);
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            Changed?.Invoke(this);
            Damaged?.Invoke(this);

            if (IsDead)
            {
                Died?.Invoke(this);
            }
        }

        public void RestoreToFull()
        {
            _currentHealth = maximumHealth;
            Changed?.Invoke(this);
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
        }
    }
}
