using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDocumentLocation : MonoBehaviour
    {
        public enum RoomType
        {
            MainLobby,
            BritishOffice,
            PlanningRoom,
            RadioStation
        }

        [Serializable]
        public struct DocumentPage
        {
            public string title;
            [TextArea(6, 24)] public string body;
        }

        [SerializeField] private RoomType room;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private DocumentPage[] pages;
        [SerializeField] private bool reusable;
        [SerializeField, Min(0)] private int energyCost = 1;

        private readonly List<int> _remainingPages = new List<int>();
        private bool _poolInitialized;

        public Collider InteractionCollider => interactionCollider;
        public int RemainingCount
        {
            get
            {
                EnsurePoolInitialized();
                return _remainingPages.Count;
            }
        }

        public bool IsExhausted => !reusable && RemainingCount == 0;
        public bool IsReusable => reusable;
        public int EnergyCost => energyCost;
        public bool RequiresConfirmation => !reusable && energyCost > 0;

        public bool CanExamine(LevelTwoDayController dayController)
        {
            return dayController != null && (reusable || !IsExhausted) &&
                   (energyCost <= 0 || dayController.CurrentEnergy >= energyCost);
        }

        private void Awake()
        {
            EnsurePoolInitialized();
        }

        public string GetPrompt(LevelTwoDayController dayController)
        {
            if (!reusable && IsExhausted)
            {
                return "NO DOCUMENTS REMAIN HERE";
            }

            if (energyCost > 0 && (dayController == null || dayController.CurrentEnergy < energyCost))
            {
                return "NO ENERGY REMAINING - END THE DAY AT THE CLOCK";
            }

            return reusable
                ? "PRESS C TO VIEW LEVEL 2 INSTRUCTIONS"
                : $"PRESS C TO SPEND {energyCost} ENERGY AND EXAMINE DOCUMENT";
        }

        public bool TryExamine(LevelTwoDocumentViewer viewer, LevelTwoDayController dayController)
        {
            if (viewer == null || !CanExamine(dayController))
            {
                return false;
            }

            if (energyCost > 0 && !dayController.TrySpendEnergy(energyCost))
            {
                return false;
            }

            int pageIndex;
            if (reusable)
            {
                if (pages == null || pages.Length == 0)
                {
                    return false;
                }

                pageIndex = 0;
            }
            else
            {
                int poolIndex = UnityEngine.Random.Range(0, _remainingPages.Count);
                pageIndex = _remainingPages[poolIndex];
                _remainingPages.RemoveAt(poolIndex);
            }

            DocumentPage page = pages[pageIndex];
            bool showDayPrompt = room == RoomType.MainLobby;
            viewer.Show(page.title, page.body, showDayPrompt, dayController.CurrentDay,
                dayController.CurrentBriefingTitle, dayController.CurrentRecommendation);
            return true;
        }

        public bool Contains(Collider candidate)
        {
            return candidate != null && interactionCollider == candidate;
        }

        private void EnsurePoolInitialized()
        {
            if (_poolInitialized || reusable)
            {
                return;
            }

            _poolInitialized = true;
            if (pages == null)
            {
                return;
            }

            for (int index = 0; index < pages.Length; index++)
            {
                _remainingPages.Add(index);
            }
        }
    }
}
