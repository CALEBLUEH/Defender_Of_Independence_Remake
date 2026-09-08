using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoDayController : MonoBehaviour
    {
        [Serializable]
        public struct DayBriefing
        {
            [Range(1, 6)] public int day;
            public string title;
            [TextArea(2, 4)] public string recommendation;
        }

        [Header("Progression")]
        [SerializeField, Range(1, 6)] private int startingDay = 1;
        [SerializeField, Range(1, 6)] private int finalDay = 6;
        [SerializeField] private DayBriefing[] briefings;
        [SerializeField] private LevelTwoScheduledCharacter[] scheduledCharacters;
        [SerializeField] private LevelTwoConversationTrigger[] conversationTriggers;
        [SerializeField] private LevelTwoNegotiationMeterController negotiationMeters;

        [Header("Player")]
        [SerializeField] private LevelTwoFirstPersonController playerController;

        [Header("Persistent HUD")]
        [SerializeField] private TMP_Text currentDayText;
        [SerializeField] private TMP_Text recommendationText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private GameObject[] transitionHiddenHud;

        [Header("Daily Energy")]
        [SerializeField, Min(1)] private int maximumEnergy = 4;

        [Header("Day Card")]
        [SerializeField] private CanvasGroup dayCard;
        [SerializeField] private TMP_Text dayCardDayText;
        [SerializeField] private TMP_Text dayCardTitleText;
        [SerializeField] private bool showOpeningDayCard = true;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.55f;
        [SerializeField, Min(0f)] private float cardHoldDuration = 1.35f;

        private Coroutine _transitionRoutine;
        private bool _progressionFailed;

        public int CurrentDay { get; private set; }
        public int CurrentEnergy { get; private set; }
        public int FinalDay => finalDay;
        public int MaximumEnergy => maximumEnergy;
        public string CurrentBriefingTitle => GetBriefing(CurrentDay).title;
        public string CurrentRecommendation => GetBriefing(CurrentDay).recommendation;
        public bool CanAdvanceDay => _transitionRoutine == null && !_progressionFailed && CurrentDay < finalDay;
        public bool IsTransitioning => _transitionRoutine != null;

        private void Awake()
        {
            CurrentDay = Mathf.Clamp(startingDay, 1, finalDay);
            CurrentEnergy = maximumEnergy;
            SetDayCardAlpha(0f);
            ApplyCurrentDay();
        }

        private void Start()
        {
            if (showOpeningDayCard)
            {
                _transitionRoutine = StartCoroutine(PresentOpeningDay());
            }
        }

        public bool TryAdvanceDay()
        {
            if (!CanAdvanceDay)
            {
                return false;
            }

            if (CurrentDay + 1 == finalDay && negotiationMeters != null && !negotiationMeters.CanEnterFinalDay)
            {
                _progressionFailed = true;
                negotiationMeters.BeginFinalDayGateFailure();
                return true;
            }

            _transitionRoutine = StartCoroutine(AdvanceDayRoutine());
            return true;
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0 || CurrentEnergy < amount || IsTransitioning)
            {
                return false;
            }

            CurrentEnergy -= amount;
            RefreshEnergyHud();
            return true;
        }

        public void ResetToStartingDay()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            CurrentDay = Mathf.Clamp(startingDay, 1, finalDay);
            _progressionFailed = false;
            CurrentEnergy = maximumEnergy;
            playerController?.ReturnToInitialSpawn();
            playerController?.SetControlsEnabled(true);
            SetDayCardAlpha(0f);
            SetTransitionHudVisible(true);
            ApplyCurrentDay();
        }

        private IEnumerator PresentOpeningDay()
        {
            playerController?.SetControlsEnabled(false);
            SetTransitionHudVisible(false);
            RefreshDayCard();
            yield return FadeDayCard(1f);
            yield return new WaitForSecondsRealtime(cardHoldDuration);
            yield return FadeDayCard(0f);
            SetTransitionHudVisible(true);
            playerController?.SetControlsEnabled(true);
            _transitionRoutine = null;
        }

        private IEnumerator AdvanceDayRoutine()
        {
            playerController?.SetControlsEnabled(false);
            SetTransitionHudVisible(false);
            yield return FadeDayCard(1f);

            CurrentDay++;
            CurrentEnergy = maximumEnergy;
            playerController?.ReturnToInitialSpawn();
            ApplyCurrentDay();
            RefreshDayCard();

            yield return new WaitForSecondsRealtime(cardHoldDuration);
            yield return FadeDayCard(0f);
            SetTransitionHudVisible(true);
            playerController?.SetControlsEnabled(true);
            _transitionRoutine = null;
        }

        private void ApplyCurrentDay()
        {
            if (scheduledCharacters != null)
            {
                foreach (LevelTwoScheduledCharacter scheduledCharacter in scheduledCharacters)
                {
                    scheduledCharacter?.ApplyDay(CurrentDay);
                }
            }

            if (conversationTriggers != null)
            {
                foreach (LevelTwoConversationTrigger conversation in conversationTriggers)
                {
                    conversation?.ApplyDay(CurrentDay);
                }
            }

            DayBriefing briefing = GetBriefing(CurrentDay);
            if (currentDayText != null)
            {
                currentDayText.text = $"DAY {CurrentDay} / {finalDay}";
            }

            if (recommendationText != null)
            {
                recommendationText.text = briefing.recommendation;
            }

            RefreshEnergyHud();

            RefreshDayCard();
        }

        private void RefreshEnergyHud()
        {
            if (energyText != null)
            {
                energyText.text = $"ENERGY  {CurrentEnergy} / {maximumEnergy}";
            }
        }

        private void SetTransitionHudVisible(bool visible)
        {
            if (transitionHiddenHud == null) return;
            foreach (GameObject hud in transitionHiddenHud)
            {
                if (hud != null) hud.SetActive(visible);
            }
        }

        private void RefreshDayCard()
        {
            DayBriefing briefing = GetBriefing(CurrentDay);
            if (dayCardDayText != null)
            {
                dayCardDayText.text = CurrentDay == finalDay ? "FINAL DAY" : $"DAY {CurrentDay}";
            }

            if (dayCardTitleText != null)
            {
                dayCardTitleText.text = briefing.title;
            }
        }

        private DayBriefing GetBriefing(int day)
        {
            if (briefings != null)
            {
                foreach (DayBriefing briefing in briefings)
                {
                    if (briefing.day == day)
                    {
                        return briefing;
                    }
                }
            }

            return new DayBriefing
            {
                day = day,
                title = "NEGOTIATIONS",
                recommendation = "Review today's preparations and meet the delegation."
            };
        }

        private IEnumerator FadeDayCard(float targetAlpha)
        {
            if (dayCard == null)
            {
                yield break;
            }

            float startAlpha = dayCard.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetDayCardAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration)));
                yield return null;
            }

            SetDayCardAlpha(targetAlpha);
        }

        private void SetDayCardAlpha(float alpha)
        {
            if (dayCard == null)
            {
                return;
            }

            dayCard.alpha = alpha;
            bool visible = alpha > 0.001f;
            dayCard.blocksRaycasts = visible;
            dayCard.interactable = false;
        }
    }
}
