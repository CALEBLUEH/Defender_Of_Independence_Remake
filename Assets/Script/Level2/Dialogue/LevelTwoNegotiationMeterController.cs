using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoNegotiationMeterController : MonoBehaviour
    {
        public enum MeterType
        {
            None,
            BritishConfidence,
            DelegationUnity,
            PublicSupport
        }

        [System.Serializable]
        public struct MeterChange
        {
            public int britishConfidence;
            public int delegationUnity;
            public int publicSupport;

            public static MeterChange operator +(MeterChange left, MeterChange right)
            {
                return new MeterChange
                {
                    britishConfidence = left.britishConfidence + right.britishConfidence,
                    delegationUnity = left.delegationUnity + right.delegationUnity,
                    publicSupport = left.publicSupport + right.publicSupport
                };
            }
        }

        [Header("Meter Rules")]
        [SerializeField, Range(0, 100)] private int initialValue = 30;
        [SerializeField, Range(0, 100)] private int finalDayEntryRequirement = 50;
        [SerializeField, Range(0, 100)] private int passingRequirement = 65;
        [SerializeField] private string nextCutsceneScene = "Cutscene_Level3";

        [Header("Meter HUD")]
        [SerializeField] private Slider britishConfidenceSlider;
        [SerializeField] private Slider delegationUnitySlider;
        [SerializeField] private Slider publicSupportSlider;
        [SerializeField] private TMP_Text britishConfidenceValue;
        [SerializeField] private TMP_Text delegationUnityValue;
        [SerializeField] private TMP_Text publicSupportValue;

        [Header("Action Feedback")]
        [SerializeField] private CanvasGroup feedbackPanel;
        [SerializeField] private TMP_Text feedbackTitle;
        [SerializeField] private TMP_Text feedbackBody;
        [SerializeField, Min(0.1f)] private float feedbackFadeDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float feedbackHoldDuration = 2.4f;

        [Header("Final Result")]
        [SerializeField] private CanvasGroup finalOverlay;
        [SerializeField] private CanvasGroup finalContent;
        [SerializeField] private TMP_Text finalHeading;
        [SerializeField] private TMP_Text finalBody;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private LevelTwoFirstPersonController playerController;
        [SerializeField, Min(0.1f)] private float resultDelay = 1.15f;
        [SerializeField, Min(0.1f)] private float resultFadeDuration = 1.25f;

        private Coroutine _feedbackRoutine;
        private Coroutine _resultRoutine;
        private int _britishBonus;
        private int _delegationBonus;
        private int _publicBonus;

        public int BritishConfidence { get; private set; }
        public int DelegationUnity { get; private set; }
        public int PublicSupport { get; private set; }
        public int BritishConfidenceBonus => _britishBonus;
        public int DelegationUnityBonus => _delegationBonus;
        public int PublicSupportBonus => _publicBonus;
        public int PassingRequirement => passingRequirement;
        public int FinalDayEntryRequirement => finalDayEntryRequirement;
        public bool CanEnterFinalDay => BritishConfidence > finalDayEntryRequirement &&
                                        DelegationUnity > finalDayEntryRequirement &&
                                        PublicSupport > finalDayEntryRequirement;
        public bool HasPassed => BritishConfidence > passingRequirement &&
                                 DelegationUnity > passingRequirement &&
                                 PublicSupport > passingRequirement;
        public bool IsShowingFinalResult => _resultRoutine != null ||
                                            (finalOverlay != null && finalOverlay.alpha > 0.001f);
        public bool IsFinalResultInteractive => _resultRoutine == null && finalContent != null &&
                                                finalContent.alpha > 0.99f;

        private void Awake()
        {
            finalOverlay?.transform.SetAsLastSibling();
            ResetMeters();
            SetCanvasGroup(feedbackPanel, 0f, false);
            SetCanvasGroup(finalOverlay, 0f, false);
            SetCanvasGroup(finalContent, 0f, false);
        }

        private void OnEnable()
        {
            continueButton?.onClick.AddListener(ContinueToLevelThree);
            restartButton?.onClick.AddListener(RestartLevel);
        }

        private void OnDisable()
        {
            continueButton?.onClick.RemoveListener(ContinueToLevelThree);
            restartButton?.onClick.RemoveListener(RestartLevel);
        }

        public void ResetMeters()
        {
            BritishConfidence = initialValue;
            DelegationUnity = initialValue;
            PublicSupport = initialValue;
            _britishBonus = 0;
            _delegationBonus = 0;
            _publicBonus = 0;
            RefreshHud();
        }

        public MeterChange ApplyConversationResult(MeterChange baseChange)
        {
            MeterChange applied = new MeterChange
            {
                britishConfidence = ApplyPositiveBonus(baseChange.britishConfidence, _britishBonus),
                delegationUnity = ApplyPositiveBonus(baseChange.delegationUnity, _delegationBonus),
                publicSupport = ApplyPositiveBonus(baseChange.publicSupport, _publicBonus)
            };

            BritishConfidence = Mathf.Clamp(BritishConfidence + applied.britishConfidence, 0, 100);
            DelegationUnity = Mathf.Clamp(DelegationUnity + applied.delegationUnity, 0, 100);
            PublicSupport = Mathf.Clamp(PublicSupport + applied.publicSupport, 0, 100);
            RefreshHud();
            ShowFeedback("CONVERSATION RESULT", FormatChange(applied));
            return applied;
        }

        public void GrantDocumentBonus(MeterType meter, int amount)
        {
            if (meter == MeterType.None || amount <= 0) return;

            switch (meter)
            {
                case MeterType.BritishConfidence: _britishBonus += amount; break;
                case MeterType.DelegationUnity: _delegationBonus += amount; break;
                case MeterType.PublicSupport: _publicBonus += amount; break;
            }

            string meterName = GetMeterName(meter);
            ShowFeedback("DOCUMENT BONUS", $"{meterName}\n+{amount} to every future positive gain\nTOTAL BONUS  +{GetBonus(meter)}");
        }

        public void BeginFinalResult()
        {
            if (_resultRoutine == null) _resultRoutine = StartCoroutine(ShowFinalResult(false));
        }

        public void BeginFinalDayGateFailure()
        {
            if (_resultRoutine == null) _resultRoutine = StartCoroutine(ShowFinalResult(true));
        }

        public void ContinueToLevelThree()
        {
            if (HasPassed && !string.IsNullOrWhiteSpace(nextCutsceneScene))
            {
                SceneManager.LoadScene(nextCutsceneScene);
            }
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator ShowFinalResult(bool failedBeforeFinalDay)
        {
            playerController?.SetControlsEnabled(false);
            playerController?.SetUiCursorActive(false);
            yield return new WaitForSecondsRealtime(resultDelay);
            yield return Fade(finalOverlay, 1f, resultFadeDuration, true);

            bool passed = !failedBeforeFinalDay && HasPassed;
            if (finalHeading != null)
            {
                finalHeading.text = failedBeforeFinalDay
                    ? "PREPARATION FAILED"
                    : passed ? "NEGOTIATION SUCCESSFUL" : "NEGOTIATION FAILED";
            }
            if (finalBody != null)
            {
                finalBody.text = failedBeforeFinalDay
                    ? $"The delegation is not ready to enter the final conference.\n\nEach meter must be above {finalDayEntryRequirement} before Day 6.\n\n{FormatFinalScores()}"
                    : passed
                    ? $"CONGRATULATIONS\n\nThe delegation secured the support needed to continue the road to Merdeka.\n\n{FormatFinalScores()}"
                    : $"Each meter must be above {passingRequirement} to pass Level 2.\n\n{FormatFinalScores()}";
            }

            if (continueButton != null) continueButton.gameObject.SetActive(passed);
            if (restartButton != null) restartButton.gameObject.SetActive(!passed);
            yield return Fade(finalContent, 1f, resultFadeDuration, true);
            playerController?.SetUiCursorActive(true);
            _resultRoutine = null;
        }

        private void ShowFeedback(string title, string body)
        {
            if (feedbackPanel == null) return;
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            if (feedbackTitle != null) feedbackTitle.text = title;
            if (feedbackBody != null) feedbackBody.text = body;
            _feedbackRoutine = StartCoroutine(ShowFeedbackRoutine());
        }

        private IEnumerator ShowFeedbackRoutine()
        {
            yield return Fade(feedbackPanel, 1f, feedbackFadeDuration, false);
            yield return new WaitForSecondsRealtime(feedbackHoldDuration);
            yield return Fade(feedbackPanel, 0f, feedbackFadeDuration, false);
            _feedbackRoutine = null;
        }

        private static int ApplyPositiveBonus(int change, int bonus) => change > 0 ? change + bonus : change;

        private int GetBonus(MeterType meter)
        {
            return meter switch
            {
                MeterType.BritishConfidence => _britishBonus,
                MeterType.DelegationUnity => _delegationBonus,
                MeterType.PublicSupport => _publicBonus,
                _ => 0
            };
        }

        private static string GetMeterName(MeterType meter)
        {
            return meter switch
            {
                MeterType.BritishConfidence => "BRITISH CONFIDENCE",
                MeterType.DelegationUnity => "DELEGATION UNITY",
                MeterType.PublicSupport => "PUBLIC SUPPORT",
                _ => string.Empty
            };
        }

        private static string FormatChange(MeterChange change)
        {
            string result = string.Empty;
            AppendChange(ref result, "British Confidence", change.britishConfidence);
            AppendChange(ref result, "Delegation Unity", change.delegationUnity);
            AppendChange(ref result, "Public Support", change.publicSupport);
            return string.IsNullOrEmpty(result) ? "No meter change" : result.TrimEnd();
        }

        private static void AppendChange(ref string text, string label, int value)
        {
            if (value == 0) return;
            text += $"{label}  {(value > 0 ? "+" : string.Empty)}{value}\n";
        }

        private string FormatFinalScores()
        {
            return $"BRITISH CONFIDENCE  {BritishConfidence}\nDELEGATION UNITY  {DelegationUnity}\nPUBLIC SUPPORT  {PublicSupport}";
        }

        private void RefreshHud()
        {
            SetMeter(britishConfidenceSlider, britishConfidenceValue, BritishConfidence);
            SetMeter(delegationUnitySlider, delegationUnityValue, DelegationUnity);
            SetMeter(publicSupportSlider, publicSupportValue, PublicSupport);
        }

        private static void SetMeter(Slider slider, TMP_Text label, int value)
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 100f;
                slider.wholeNumbers = true;
                slider.SetValueWithoutNotify(value);
            }
            if (label != null) label.text = value.ToString();
        }

        private static IEnumerator Fade(CanvasGroup group, float target, float duration, bool interactiveAtEnd)
        {
            if (group == null) yield break;
            float start = group.alpha;
            float elapsed = 0f;
            group.gameObject.SetActive(true);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetCanvasGroup(group, Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)), false);
                yield return null;
            }
            SetCanvasGroup(group, target, interactiveAtEnd && target > 0.99f);
        }

        private static void SetCanvasGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
