using System;
using System.Collections;
using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoConversationCameraFocus : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.05f)] private float moveDuration = 0.45f;

        private Coroutine _movement;
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private bool _hasFocusPose;

        public bool IsFocused => _hasFocusPose;

        public void Focus(Transform target)
        {
            if (playerCamera == null || target == null) return;

            Transform cameraTransform = playerCamera.transform;
            if (!_hasFocusPose)
            {
                _restLocalPosition = cameraTransform.localPosition;
                _restLocalRotation = cameraTransform.localRotation;
            }

            _hasFocusPose = true;
            StartMovement(cameraTransform.position, target.position, cameraTransform.rotation, target.rotation, null);
        }

        public void Restore(Action onComplete)
        {
            if (playerCamera == null || !_hasFocusPose)
            {
                onComplete?.Invoke();
                return;
            }

            Transform cameraTransform = playerCamera.transform;
            Transform parent = cameraTransform.parent;
            Vector3 worldPosition = parent != null ? parent.TransformPoint(_restLocalPosition) : _restLocalPosition;
            Quaternion worldRotation = parent != null ? parent.rotation * _restLocalRotation : _restLocalRotation;
            StartMovement(cameraTransform.position, worldPosition, cameraTransform.rotation, worldRotation, () =>
            {
                cameraTransform.localPosition = _restLocalPosition;
                cameraTransform.localRotation = _restLocalRotation;
                _hasFocusPose = false;
                onComplete?.Invoke();
            });
        }

        private void StartMovement(Vector3 fromPosition, Vector3 toPosition, Quaternion fromRotation,
            Quaternion toRotation, Action onComplete)
        {
            if (_movement != null) StopCoroutine(_movement);
            _movement = StartCoroutine(Move(fromPosition, toPosition, fromRotation, toRotation, onComplete));
        }

        private IEnumerator Move(Vector3 fromPosition, Vector3 toPosition, Quaternion fromRotation,
            Quaternion toRotation, Action onComplete)
        {
            Transform cameraTransform = playerCamera.transform;
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
                cameraTransform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(fromPosition, toPosition, t),
                    Quaternion.SlerpUnclamped(fromRotation, toRotation, t));
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(toPosition, toRotation);
            _movement = null;
            onComplete?.Invoke();
        }
    }
}
