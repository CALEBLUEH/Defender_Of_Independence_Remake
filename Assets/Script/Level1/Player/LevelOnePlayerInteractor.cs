using TMPro;
using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOnePlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LevelOnePlayerInput input;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private TMP_Text promptText;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private IPlayerInteractable _current;

        private void Update()
        {
            _current = FindInteractable();
            if (promptText != null)
            {
                promptText.text = _current == null ? string.Empty : $"C  {_current.InteractionPrompt}";
                promptText.gameObject.SetActive(_current != null);
            }

            if (_current != null && input != null && input.ConsumeInteractPressed())
            {
                _current.Interact();
            }
            else
            {
                input?.ConsumeInteractPressed();
            }
        }

        private IPlayerInteractable FindInteractable()
        {
            if (playerCamera == null || !Physics.Raycast(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionLayers,
                    QueryTriggerInteraction.Collide))
            {
                return null;
            }

            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IPlayerInteractable interactable && interactable.CanInteract)
                {
                    return interactable;
                }
            }

            return null;
        }
    }
}
