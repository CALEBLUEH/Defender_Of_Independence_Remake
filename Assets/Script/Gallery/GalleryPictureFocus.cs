using System;
using System.Collections;
using UnityEngine;

public sealed class GalleryPictureFocus : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.1f)] private float viewingDistance = 1.6f;
    [SerializeField, Min(0.05f)] private float moveDuration = 0.45f;

    private Coroutine movement;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;

    public bool IsFocused { get; private set; }

    public void Focus(GalleryExhibit exhibit, Action onComplete = null)
    {
        if (playerCamera == null || exhibit == null)
        {
            onComplete?.Invoke();
            return;
        }

        Transform cameraTransform = playerCamera.transform;
        if (!IsFocused)
        {
            restLocalPosition = cameraTransform.localPosition;
            restLocalRotation = cameraTransform.localRotation;
        }

        Bounds bounds = exhibit.GetFocusBounds();
        Vector3 center = bounds.center;
        Vector3 viewingSide = cameraTransform.position - center;
        viewingSide.y = 0f;
        if (viewingSide.sqrMagnitude < 0.01f) viewingSide = -exhibit.transform.forward;
        viewingSide.Normalize();
        float distance = Mathf.Max(viewingDistance, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.25f);
        Vector3 targetPosition = center + viewingSide * distance;
        Quaternion targetRotation = Quaternion.LookRotation(center - targetPosition, Vector3.up);
        IsFocused = true;
        StartMovement(cameraTransform.position, targetPosition, cameraTransform.rotation, targetRotation, onComplete);
    }

    public void Restore(Action onComplete = null)
    {
        if (playerCamera == null || !IsFocused)
        {
            onComplete?.Invoke();
            return;
        }

        Transform cameraTransform = playerCamera.transform;
        Transform parent = cameraTransform.parent;
        Vector3 position = parent != null ? parent.TransformPoint(restLocalPosition) : restLocalPosition;
        Quaternion rotation = parent != null ? parent.rotation * restLocalRotation : restLocalRotation;
        StartMovement(cameraTransform.position, position, cameraTransform.rotation, rotation, () =>
        {
            cameraTransform.localPosition = restLocalPosition;
            cameraTransform.localRotation = restLocalRotation;
            IsFocused = false;
            onComplete?.Invoke();
        });
    }

    private void StartMovement(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation,
        Quaternion endRotation, Action onComplete)
    {
        if (movement != null) StopCoroutine(movement);
        movement = StartCoroutine(Move(startPosition, endPosition, startRotation, endRotation, onComplete));
    }

    private IEnumerator Move(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation,
        Quaternion endRotation, Action onComplete)
    {
        Transform cameraTransform = playerCamera.transform;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            cameraTransform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPosition, endPosition, amount),
                Quaternion.SlerpUnclamped(startRotation, endRotation, amount));
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(endPosition, endRotation);
        movement = null;
        onComplete?.Invoke();
    }
}
