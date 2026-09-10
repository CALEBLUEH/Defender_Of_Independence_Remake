using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GalleryPictureViewer : MonoBehaviour
{
    [SerializeField] private GalleryFirstPersonController playerController;
    [SerializeField] private GalleryPictureFocus pictureFocus;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TMP_Text continuePromptText;
    [SerializeField, Min(0f)] private float promptPulseSpeed = 1.2f;

    private GalleryExhibit currentExhibit;
    private int lineIndex;

    public bool IsOpen => currentExhibit != null;
    public int CurrentLineIndex => lineIndex;

    private void Awake() => SetVisible(false);

    private void Update()
    {
        if (!IsOpen) return;
        if (continuePromptText != null)
        {
            Color color = continuePromptText.color;
            color.a = 0.64f + Mathf.Sin(Time.unscaledTime * promptPulseSpeed * Mathf.PI * 2f) * 0.28f;
            continuePromptText.color = color;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) Advance();
    }

    public bool Begin(GalleryExhibit exhibit)
    {
        if (IsOpen || exhibit == null || exhibit.ExhibitKind != GalleryExhibitKind.HistoricalPicture ||
            exhibit.DialogueLines == null || exhibit.DialogueLines.Length == 0) return false;

        currentExhibit = exhibit;
        lineIndex = 0;
        playerController?.SetControlsEnabled(false);
        pictureFocus?.Focus(exhibit);
        SetVisible(true);
        RefreshLine();
        return true;
    }

    public void Advance()
    {
        if (!IsOpen) return;
        if (lineIndex < currentExhibit.DialogueLines.Length - 1)
        {
            lineIndex++;
            RefreshLine();
            return;
        }

        currentExhibit = null;
        SetVisible(false);
        pictureFocus?.Restore(() => playerController?.SetControlsEnabled(true));
        if (pictureFocus == null) playerController?.SetControlsEnabled(true);
    }

    private void RefreshLine()
    {
        if (currentExhibit == null) return;
        if (titleText != null) titleText.text = currentExhibit.DisplayName.ToUpperInvariant();
        if (contentText != null) contentText.text = currentExhibit.DialogueLines[lineIndex];
        if (continuePromptText != null)
            continuePromptText.text = lineIndex >= currentExhibit.DialogueLines.Length - 1
                ? "PRESS SPACE TO RETURN TO THE GALLERY"
                : "PRESS SPACE TO CONTINUE";
    }

    private void SetVisible(bool visible)
    {
        if (dialogueGroup == null) return;
        dialogueGroup.gameObject.SetActive(true);
        dialogueGroup.alpha = visible ? 1f : 0f;
        dialogueGroup.interactable = false;
        dialogueGroup.blocksRaycasts = false;
    }
}
