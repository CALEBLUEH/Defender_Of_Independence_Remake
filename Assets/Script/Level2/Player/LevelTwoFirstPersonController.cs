using UnityEngine;
using UnityEngine.InputSystem;

namespace DefenderOfIndependence.Level2
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LevelTwoFirstPersonController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform initialSpawnPoint;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField] private float gravity = -20f;

        [Header("Mouse Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 80f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _pitch;
        private bool _controlsEnabled = true;

        public bool ControlsEnabled => _controlsEnabled;
        public Transform InitialSpawnPoint => initialSpawnPoint;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (initialSpawnPoint != null)
            {
                TeleportTo(initialSpawnPoint, initialSpawnPoint.forward);
            }

            ApplyCursorState();
        }

        private void Update()
        {
            if (!_controlsEnabled)
            {
                return;
            }

            UpdateLook();
            UpdateMovement();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyCursorState();
            }
        }

        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
            if (!enabled)
            {
                _verticalVelocity = 0f;
            }
        }

        public void ReturnToInitialSpawn()
        {
            if (initialSpawnPoint != null)
            {
                TeleportTo(initialSpawnPoint, initialSpawnPoint.forward);
            }
        }

        public void TeleportTo(Transform spawnPoint, Vector3 lookDirection)
        {
            if (spawnPoint == null)
            {
                return;
            }

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            bool wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.position = spawnPoint.position;

            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            _pitch = 0f;
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }

            _verticalVelocity = 0f;
            _characterController.enabled = wasEnabled;
        }

        private void UpdateMovement()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                input = Vector2.ClampMagnitude(input, 1f);
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 horizontalVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cameraPivot == null)
            {
                return;
            }

            Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up, mouseDelta.x, Space.Self);
            _pitch = Mathf.Clamp(_pitch - mouseDelta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private static void ApplyCursorState()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
