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
        [SerializeField] private LevelTwoNextDayClock nextDayClock;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LevelTwoDayController dayController;
        [SerializeField] private LevelTwoDocumentViewer documentViewer;
        [SerializeField] private LevelTwoDocumentLocation[] documents;
        [SerializeField] private LevelTwoConversationViewer conversationViewer;
        [SerializeField] private LevelTwoConversationTrigger[] conversations;
        [SerializeField] private LevelTwoConfirmationPanel confirmationPanel;
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
            if (playerController == null || screenFader == null || !playerController.ControlsEnabled ||
                screenFader.IsTransitioning || (documentViewer != null && documentViewer.IsOpen) ||
                (confirmationPanel != null && confirmationPanel.IsOpen))
            {
                SetPromptVisible(false);
                return;
            }

            _nearestDoor = FindNearestDoor(out float doorDistance);
            float clockDistance = GetClockDistanceSquared();
            bool clockIsNearest = nextDayClock != null && clockDistance <= doorDistance;
            LevelTwoDocumentLocation aimedDocument = FindAimedDocument();
            LevelTwoConversationTrigger aimedConversation = FindAimedConversation();
            if (conversationViewer != null && conversationViewer.IsOpen)
            {
                SetPromptVisible(false);
                return;
            }

            if (aimedDocument == null && aimedConversation == null && _nearestDoor == null && !clockIsNearest)
            {
                SetPromptVisible(false);
                return;
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.text = aimedDocument != null
                    ? aimedDocument.GetPrompt(dayController)
                    : aimedConversation != null
                    ? aimedConversation.GetPrompt(dayController)
                    : clockIsNearest
                    ? nextDayClock.GetPrompt()
                    : _nearestDoor.GetPrompt(transform.position, dayController);
                GetPromptRoot().SetActive(true);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                if (aimedDocument != null)
                {
                    RequestDocument(aimedDocument);
                    SetPromptVisible(false);
                }
                else if (aimedConversation != null)
                {
                    RequestConversation(aimedConversation);
                    SetPromptVisible(false);
                }
                else if (clockIsNearest)
                {
                    RequestNextDay();
                }
                else
                {
                    RequestDoor(_nearestDoor);
                }
            }
        }

        private void RequestDocument(LevelTwoDocumentLocation document)
        {
            if (document == null || !document.CanExamine(dayController)) return;
            if (!document.RequiresConfirmation)
            {
                document.TryExamine(documentViewer, dayController);
                return;
            }

            confirmationPanel?.Show(
                "EXAMINE DOCUMENT?",
                $"Spend {document.EnergyCost} Energy to examine one historical document. Each document can only be found once.",
                string.Empty,
                () => document.TryExamine(documentViewer, dayController));
        }

        private void RequestConversation(LevelTwoConversationTrigger conversation)
        {
            if (conversation == null || !conversation.CanBegin(dayController)) return;
            confirmationPanel?.Show(
                "BEGIN CONVERSATION?",
                $"Spend {conversation.EnergyCost} Energy to speak with {conversation.ConversationDisplayName}. This conversation can only happen once.",
                string.Empty,
                () => conversation.TryBegin(conversationViewer, dayController));
        }

        private void RequestNextDay()
        {
            if (nextDayClock == null || dayController == null || !dayController.CanAdvanceDay) return;
            string warning = dayController.CurrentEnergy > 0
                ? $"WARNING: You still have {dayController.CurrentEnergy} Energy. Unused Energy will be lost when the day ends."
                : string.Empty;
            confirmationPanel?.Show(
                "END THE DAY?",
                $"Finish Day {dayController.CurrentDay} and continue to Day {dayController.CurrentDay + 1}?",
                warning,
                nextDayClock.TryUse);
        }

        private void RequestDoor(LevelTwoDoorTransition door)
        {
            if (door == null || door.IsLocked(transform.position, dayController)) return;
            int currentDay = dayController != null ? dayController.CurrentDay : -1;
            if (!door.RequiresEntryEnergy(transform.position, currentDay))
            {
                TryInteractDoor(door);
                return;
            }

            if (dayController == null || dayController.CurrentEnergy < 1) return;

            confirmationPanel?.Show(
                "ENTER ROOM?",
                $"Spend 1 Energy to enter {door.RoomDisplayName}. Re-entry is free for the rest of this day.",
                string.Empty,
                () => TryInteractDoor(door));
        }

        private LevelTwoConversationTrigger FindAimedConversation()
        {
            if (playerCamera == null || conversations == null)
            {
                return null;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                return null;
            }

            foreach (LevelTwoConversationTrigger conversation in conversations)
            {
                if (conversation != null && conversation.Contains(hit.collider))
                {
                    return conversation;
                }
            }

            return null;
        }

        private LevelTwoDocumentLocation FindAimedDocument()
        {
            if (playerCamera == null || documents == null)
            {
                return null;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                return null;
            }

            foreach (LevelTwoDocumentLocation document in documents)
            {
                if (document != null && document.Contains(hit.collider))
                {
                    return document;
                }
            }

            return null;
        }

        public bool TryInteractNearest()
        {
            if (playerController == null || screenFader == null || screenFader.IsTransitioning)
            {
                return false;
            }

            LevelTwoDoorTransition door = FindNearestDoor(out _);
            return TryInteractDoor(door);
        }

        private bool TryInteractDoor(LevelTwoDoorTransition door)
        {
            if (door == null)
            {
                return false;
            }

            if (door.IsLocked(transform.position, dayController))
            {
                return false;
            }

            int currentDay = dayController != null ? dayController.CurrentDay : -1;
            bool requiresEnergy = door.RequiresEntryEnergy(transform.position, currentDay);
            if (requiresEnergy && (dayController == null || dayController.CurrentEnergy < 1))
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
            else if (requiresEnergy)
            {
                dayController.TrySpendEnergy(1);
                door.MarkEntryPaid(currentDay);
            }

            return started;
        }

        private LevelTwoDoorTransition FindNearestDoor(out float nearestDistance)
        {
            LevelTwoDoorTransition nearest = null;
            nearestDistance = interactionRange * interactionRange;

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

        private float GetClockDistanceSquared()
        {
            if (nextDayClock == null)
            {
                return float.PositiveInfinity;
            }

            float distance = Vector3.SqrMagnitude(transform.position - nextDayClock.InteractionPosition);
            return distance <= interactionRange * interactionRange ? distance : float.PositiveInfinity;
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
