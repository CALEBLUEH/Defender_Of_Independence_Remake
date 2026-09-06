using UnityEngine;
using UnityEngine.InputSystem;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOnePlayerInput : MonoBehaviour
    {
        public bool FireHeld { get; private set; }

        private bool _firePressed;
        private bool _toggleFireModePressed;
        private bool _reloadPressed;
        private bool _interactPressed;

        public void OnFire(InputValue value)
        {
            FireHeld = value.isPressed;
            _firePressed |= value.isPressed;
        }

        public void OnToggleFireMode(InputValue value)
        {
            _toggleFireModePressed |= value.isPressed;
        }

        public void OnReload(InputValue value)
        {
            _reloadPressed |= value.isPressed;
        }

        public void OnInteract(InputValue value)
        {
            _interactPressed |= value.isPressed;
        }

        public bool ConsumeFirePressed() => Consume(ref _firePressed);
        public bool ConsumeToggleFireModePressed() => Consume(ref _toggleFireModePressed);
        public bool ConsumeReloadPressed() => Consume(ref _reloadPressed);
        public bool ConsumeInteractPressed() => Consume(ref _interactPressed);

        private void OnDisable()
        {
            FireHeld = false;
            _firePressed = false;
            _toggleFireModePressed = false;
            _reloadPressed = false;
            _interactPressed = false;
        }

        private static bool Consume(ref bool value)
        {
            bool result = value;
            value = false;
            return result;
        }
    }
}
