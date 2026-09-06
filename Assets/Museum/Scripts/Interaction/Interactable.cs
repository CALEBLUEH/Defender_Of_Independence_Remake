using UnityEngine;

// 挂在任何物品上，Collider 记得勾 Is Trigger（范围就是互动范围）
public class Interactable : MonoBehaviour
{
    public GameObject panel; // 直接拖这个物品对应的 Panel 进来就行

    private bool playerInRange = false;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            panel.SetActive(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            panel.SetActive(false); // 离开范围自动关闭
        }
    }
}