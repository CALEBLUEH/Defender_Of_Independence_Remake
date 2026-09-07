using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefenderOfIndependence.Level1
{
    public sealed class PlayerLifeController : MonoBehaviour
    {
        [SerializeField] private CombatHealth health;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private FirstPersonWeaponController weapon;
        [SerializeField] private PlayerInput playerInput;

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied(CombatHealth sender)
        {
            if (movement != null)
            {
                movement.enabled = false;
            }

            if (weapon != null)
            {
                weapon.enabled = false;
            }

            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
