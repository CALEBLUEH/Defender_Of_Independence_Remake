using UnityEngine;

public enum GalleryExhibitKind
{
    HistoricalPicture,
    CreditVideo,
    Quiz,
    CreditPanel
}

public sealed class GalleryExhibit : MonoBehaviour
{
    [SerializeField] private GalleryExhibitKind exhibitKind;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string[] dialogueLines;
    [SerializeField] private Collider[] interactionColliders;
    [SerializeField] private Renderer focusRenderer;

    public GalleryExhibitKind ExhibitKind => exhibitKind;
    public string DisplayName => displayName;
    public string[] DialogueLines => dialogueLines;

    public bool Contains(Collider candidate)
    {
        if (candidate == null || interactionColliders == null) return false;
        foreach (Collider item in interactionColliders)
            if (item == candidate) return true;
        return false;
    }

    public Bounds GetFocusBounds()
    {
        if (focusRenderer != null) return focusRenderer.bounds;
        if (interactionColliders != null)
            foreach (Collider item in interactionColliders)
                if (item != null) return item.bounds;
        return new Bounds(transform.position, Vector3.one);
    }
}
