using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class GalleryCreditPanelController : MonoBehaviour
{
    [SerializeField] private GalleryFirstPersonController playerController;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, TextArea(3, 10)] private string creditMessage =
        "Thank you for joining the journey toward Malayan independence.";
    [SerializeField] private Button quitButton;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        quitButton?.onClick.AddListener(Close);
        if (messageText != null) messageText.text = creditMessage;
        SetVisible(false);
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    public bool Open()
    {
        if (IsOpen) return false;
        IsOpen = true;
        playerController?.SetControlsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (messageText != null) messageText.text = creditMessage;
        SetVisible(true);
        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        SetVisible(false);
        playerController?.SetControlsEnabled(true);
    }

    private void SetVisible(bool visible)
    {
        if (panelGroup == null) return;
        panelGroup.gameObject.SetActive(true);
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }
}
