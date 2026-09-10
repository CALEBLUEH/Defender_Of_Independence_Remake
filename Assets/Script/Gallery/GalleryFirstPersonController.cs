using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class GalleryFirstPersonController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform initialSpawnPoint;
    [SerializeField] private Transform videoSpawnPoint;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Range(1f, 89f)] private float pitchLimit = 80f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float pitch;

    public bool ControlsEnabled { get; private set; } = true;
    public Transform InitialSpawnPoint => initialSpawnPoint;
    public Transform VideoSpawnPoint => videoSpawnPoint;

    private void Awake() => characterController = GetComponent<CharacterController>();

    private void Start()
    {
        GallerySpawnKind spawnKind = GallerySpawnSession.Consume();
        Transform destination = spawnKind == GallerySpawnKind.VideoReturn ? videoSpawnPoint : initialSpawnPoint;
        TeleportTo(destination);
        ApplyGameplayCursor();
    }

    private void Update()
    {
        if (!ControlsEnabled) return;
        UpdateLook();
        UpdateMovement();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && ControlsEnabled) ApplyGameplayCursor();
    }

    public void SetControlsEnabled(bool enabled)
    {
        ControlsEnabled = enabled;
        if (!enabled) verticalVelocity = 0f;
        ApplyGameplayCursor();
    }

    public void TeleportTo(Transform spawnPoint)
    {
        if (spawnPoint == null) return;
        if (characterController == null) characterController = GetComponent<CharacterController>();

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        pitch = 0f;
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity;
        verticalVelocity = 0f;
        characterController.enabled = wasEnabled;
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

        if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        characterController.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void UpdateLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cameraPivot == null) return;
        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up, delta.x, Space.Self);
        pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private static void ApplyGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
