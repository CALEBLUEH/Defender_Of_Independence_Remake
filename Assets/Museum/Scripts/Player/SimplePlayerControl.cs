using UnityEngine;

// 挂在 Player 物体上（需要有 CharacterController 组件）
// Camera 作为 Player 的子物体拖进 playerCamera 栏位
[RequireComponent(typeof(CharacterController))]
public class SimplePlayerControl : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("视角设置")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;

    [Header("互动设置")]
    public float interactDistance = 3f;
    public LayerMask interactableLayer = ~0; // 默认所有层都能互动

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
     if (Time.timeScale == 0f) return;

        HandleMouseLook();
        HandleMovement();
        HandleInteract();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
 
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 鼠标控制视角
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 左右转 -> 转整个玩家身体
        transform.Rotate(Vector3.up * mouseX);

        // 上下转 -> 只转摄像机，并限制角度避免翻转
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
    }

    // WASD 前后左右移动
    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 简单重力，避免角色悬空/掉落穿模
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Space 互动
    void HandleInteract()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
            {
                Debug.Log("互动到了: " + hit.collider.name);

                // 如果目标物体上挂了实现 IInteractable 的脚本，就调用它
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}

// 需要互动的物体可以实现这个接口，写自己的互动逻辑
public interface IInteractable
{
    void Interact();
}