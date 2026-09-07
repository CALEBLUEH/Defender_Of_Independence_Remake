using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDoorInteractor : MonoBehaviour
    {
        [SerializeField] private LevelTwoFirstPersonController playerController;
        [SerializeField] private LevelTwoScreenFader screenFader;
        [SerializeField] private TMP_Text interactionPrompt;
        [SerializeField] private LevelTwoDoorTransition[] doors;
        [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;

        private LevelTwoDoorTransition _nearestDoor;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<LevelTwoFirstPersonController>();
            }

            SetPromptVisible(false);
        }

        private void Update()
        {
            if (playerController == null || screenFader == null || !playerController.ControlsEnabled || screenFader.IsTransitioning)
            {
                SetPromptVisible(false);
                return;
            }

            _nearestDoor = FindNearestDoor();
            if (_nearestDoor == null)
            {
                SetPromptVisible(false);
                return;
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.text = _nearestDoor.GetPrompt(transform.position);
                GetPromptRoot().SetActive(true);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                TryInteractNearest();
            }
        }

        public bool TryInteractNearest()
        {
            if (playerController == null || screenFader == null || screenFader.IsTransitioning)
            {
                return false;
            }

            LevelTwoDoorTransition door = FindNearestDoor();
            if (door == null)
            {
                return false;
            }

            Transform destination = door.GetDestination(transform.position);
            if (destination == null)
            {
                return false;
            }

            playerController.SetControlsEnabled(false);
            SetPromptVisible(false);
            bool started = screenFader.TryBeginTransition(
                () => playerController.TeleportTo(destination, door.GetArrivalLookDirection(destination)),
                () => playerController.SetControlsEnabled(true));

            if (!started)
            {
                playerController.SetControlsEnabled(true);
            }

            return started;
        }

        private LevelTwoDoorTransition FindNearestDoor()
        {
            LevelTwoDoorTransition nearest = null;
            float nearestDistance = interactionRange * interactionRange;

            if (doors == null)
            {
                return null;
            }

            foreach (LevelTwoDoorTransition door in doors)
            {
                if (door == null)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(transform.position - door.transform.position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = door;
                }
            }

            return nearest;
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                GetPromptRoot().SetActive(visible);
            }
        }

        private GameObject GetPromptRoot()
        {
            return interactionPrompt.transform.parent != null
                ? interactionPrompt.transform.parent.gameObject
                : interactionPrompt.gameObject;
        }
    }
}
