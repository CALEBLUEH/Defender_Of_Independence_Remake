using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GalleryExhibitInteractor : MonoBehaviour
{
    [SerializeField] private GalleryFirstPersonController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GalleryPictureViewer pictureViewer;
    [SerializeField] private GalleryScreenFader screenFader;
    [SerializeField] private GalleryQuizController quizController;
    [SerializeField] private GalleryCreditPanelController creditPanelController;
    [SerializeField] private TMP_Text interactionPrompt;
    [SerializeField] private GalleryExhibit[] exhibits;
    [SerializeField, Min(0.1f)] private float interactionRange = 30f;
    [SerializeField, Min(0f)] private float thinOccluderTolerance = 0.75f;
    [SerializeField] private string creditsSceneName = "Scene_Credits";
    [SerializeField] private string gallerySceneName = "Scene_Gallery";

    public GalleryExhibit AimedExhibit { get; private set; }

    private void Awake() => SetPromptVisible(false);

    private void Update()
    {
        if (playerController == null || playerCamera == null || !playerController.ControlsEnabled ||
            (pictureViewer != null && pictureViewer.IsOpen) || (screenFader != null && screenFader.IsTransitioning))
        {
            AimedExhibit = null;
            SetPromptVisible(false);
            return;
        }

        AimedExhibit = FindAimedExhibit();
        if (AimedExhibit == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.text = GetPrompt(AimedExhibit);
            SetPromptVisible(true);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame) Interact(AimedExhibit);
    }

    public bool Interact(GalleryExhibit exhibit)
    {
        if (exhibit == null || !playerController.ControlsEnabled) return false;
        SetPromptVisible(false);
        switch (exhibit.ExhibitKind)
        {
            case GalleryExhibitKind.HistoricalPicture:
                return pictureViewer != null && pictureViewer.Begin(exhibit);
            case GalleryExhibitKind.Quiz:
                return quizController != null && quizController.Open();
            case GalleryExhibitKind.CreditPanel:
                return creditPanelController != null && creditPanelController.Open();
            case GalleryExhibitKind.CreditVideo:
                playerController.SetControlsEnabled(false);
                bool started = screenFader != null && screenFader.TryFadeOut(() =>
                {
                    CreditPlaybackSession.RequestFromGallery(gallerySceneName);
                    SceneManager.LoadScene(creditsSceneName);
                });
                if (!started) playerController.SetControlsEnabled(true);
                return started;
            default:
                return false;
        }
    }

    private GalleryExhibit FindAimedExhibit()
    {
        if (exhibits == null || exhibits.Length == 0) return null;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, interactionRange, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);
        if (hitCount == 0) return null;
        Array.Sort(raycastHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

        float blockerDistance = float.PositiveInfinity;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            RaycastHit hit = raycastHits[hitIndex];
            GalleryExhibit exhibit = FindExhibitForCollider(hit.collider);
            if (exhibit != null)
            {
                if (float.IsPositiveInfinity(blockerDistance) || hit.distance - blockerDistance <= thinOccluderTolerance)
                    return exhibit;
                return null;
            }

            if (hit.collider != null && !hit.collider.isTrigger && float.IsPositiveInfinity(blockerDistance))
                blockerDistance = hit.distance;
        }
        return null;
    }

    private readonly RaycastHit[] raycastHits = new RaycastHit[64];

    private GalleryExhibit FindExhibitForCollider(Collider candidate)
    {
        foreach (GalleryExhibit exhibit in exhibits)
            if (exhibit != null && exhibit.Contains(candidate)) return exhibit;
        return null;
    }

    private static string GetPrompt(GalleryExhibit exhibit)
    {
        return exhibit.ExhibitKind switch
        {
            GalleryExhibitKind.CreditVideo => "PRESS C TO WATCH THE CREDIT VIDEO",
            GalleryExhibitKind.Quiz => "PRESS C TO START THE INDEPENDENCE QUIZ",
            GalleryExhibitKind.CreditPanel => "PRESS C TO VIEW THE CREDITS",
            _ => $"PRESS C TO VIEW {exhibit.DisplayName.ToUpperInvariant()}"
        };
    }

    private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
        public int Compare(RaycastHit left, RaycastHit right) => left.distance.CompareTo(right.distance);
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt == null) return;
        GameObject root = interactionPrompt.transform.parent != null
            ? interactionPrompt.transform.parent.gameObject
            : interactionPrompt.gameObject;
        root.SetActive(visible);
    }
}
