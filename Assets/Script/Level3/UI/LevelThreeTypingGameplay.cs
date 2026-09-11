using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DefenderOfIndependence.Audio;

public sealed class LevelThreeTypingGameplay : MonoBehaviour
{
    public enum PanelSelection { Main, Incident }

    [Header("Main Speech Panel")]
    [SerializeField] private TMP_Text mainHeadingText;
    [SerializeField] private TMP_Text mainMessageText;
    [SerializeField] private TMP_Text mainPageText;
    [SerializeField] private TMP_Text mainTimerLabel;
    [SerializeField] private Slider mainLeftTimerSlider;
    [SerializeField] private Slider mainRightTimerSlider;
    [SerializeField] private Outline mainSelectionOutline;

    [Header("Incident Panel")]
    [SerializeField] private CanvasGroup incidentPanelGroup;
    [SerializeField] private RectTransform incidentPanelRect;
    [SerializeField] private TMP_Text incidentHeadingText;
    [SerializeField] private TMP_Text incidentMessageText;
    [SerializeField] private Slider incidentLeftTimerSlider;
    [SerializeField] private Slider incidentRightTimerSlider;
    [SerializeField] private Outline incidentSelectionOutline;

    [Header("Health and Failure")]
    [SerializeField] private LevelThreeHealthDisplay healthDisplay;
    [SerializeField] private CanvasGroup failurePanelGroup;
    [SerializeField] private TMP_Text failureMessageText;
    [SerializeField] private CanvasGroup failureFadeOverlay;

    [Header("Assignment Content")]
    [SerializeField, TextArea(2, 5)] private List<string> mainLines = new List<string>();
    [SerializeField, TextArea(2, 4)] private List<string> incidentPrompts = new List<string>();
    [SerializeField] private List<string> incidentRequiredWords = new List<string>();

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float mainSecondsPerLine = 5f;
    [Tooltip("One duration per Main Lines entry. Missing or non-positive entries use Main Seconds Per Line.")]
    [SerializeField] private List<float> mainLineDurations = new List<float>();
    [SerializeField, Min(0.1f)] private float incidentSeconds = 5f;
    [SerializeField] private Vector2 incidentSpawnDelayRange = new Vector2(1f, 2f);
    [SerializeField, Min(0f)] private float incidentCanvasMargin = 28f;

    [Header("Presentation")]
    [SerializeField] private Color normalBorderColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
    [SerializeField] private Color selectedBorderColor = new Color(0.15f, 0.85f, 1f, 1f);
    [SerializeField] private Color typedWordColor = new Color(0.3f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color pendingWordColor = new Color(0.68f, 0.72f, 0.77f, 1f);
    [SerializeField] private UnityEvent onGameplayCompleted = new UnityEvent();

    [Header("Failure Recovery")]
    [SerializeField] private LevelThreeTutorialPanel tutorialPanel;
    [SerializeField] private CanvasGroup restartFadeOverlay;
    [SerializeField, Min(0f)] private float restartFadeHalfDuration = 0.75f;
    [Tooltip("Real-time silence after the third miss before the looping loss music begins.")]
    [SerializeField, Min(0f)] private float loseMusicDelay = 1f;

    private readonly List<int> randomMediumIncidentPool = new List<int>();
    private ParsedLine mainLine;
    private ParsedLine incidentLine;
    private Coroutine incidentSpawnRoutine;
    private Coroutine restartRoutine;
    private Coroutine failureRevealRoutine;
    private Coroutine lossMusicDelayRoutine;
    private float mainElapsed;
    private float currentMainLineDuration = 5f;
    private float incidentElapsed;
    private int mainTypedCharacters;
    private int incidentTypedCharacters;
    private int activeIncidentIndex = -1;

    public bool IsRunning { get; private set; }
    public bool IsFailed { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IncidentIsVisible { get; private set; }
    public int CurrentMainLineIndex { get; private set; } = -1;
    public int MissCount => healthDisplay != null ? healthDisplay.MaximumHealth - healthDisplay.CurrentHealth : 0;
    public float MainTimerRemaining { get; private set; } = 1f;
    public float IncidentTimerRemaining { get; private set; } = 1f;
    public float CurrentMainLineDuration => currentMainLineDuration;
    public int MainTypedCharacterCount => mainTypedCharacters;
    public int IncidentTypedCharacterCount => incidentTypedCharacters;
    public PanelSelection SelectedPanel { get; private set; } = PanelSelection.Main;
    public bool IsRestartTransitioning { get; private set; }
    public bool IsFailureTransitioning => failureRevealRoutine != null;
    public bool IsLossMusicDelayPending => lossMusicDelayRoutine != null;
    public float LoseMusicDelay => loseMusicDelay;
    public float RestartFadeAlpha => restartFadeOverlay != null ? restartFadeOverlay.alpha : 0f;
    public UnityEvent GameplayCompletedEvent => onGameplayCompleted;

    private void Awake()
    {
        HideGroup(failurePanelGroup);
    }

    private void Update()
    {
        if (IsFailed)
        {
            if (!IsRestartTransitioning && !IsFailureTransitioning && Input.GetKeyDown(KeyCode.R)) RetryFromFailure();
            return;
        }
        if (!IsRunning) return;

        if (IncidentIsVisible && Input.GetKeyDown(KeyCode.Tab)) ToggleSelectedPanel();
        string input = Input.inputString;
        for (int index = 0; index < input.Length; index++) SubmitCharacter(input[index]);

        float delta = Time.unscaledDeltaTime;
        UpdateIncidentTimer(delta);
        if (!IsRunning) return;
        UpdateMainTimer(delta);
    }

    private void OnDisable()
    {
        if (incidentSpawnRoutine != null) StopCoroutine(incidentSpawnRoutine);
        if (restartRoutine != null) StopCoroutine(restartRoutine);
        if (failureRevealRoutine != null) StopCoroutine(failureRevealRoutine);
        if (lossMusicDelayRoutine != null) StopCoroutine(lossMusicDelayRoutine);
        incidentSpawnRoutine = null;
        restartRoutine = null;
        failureRevealRoutine = null;
        lossMusicDelayRoutine = null;
        IsRestartTransitioning = false;
        tutorialPanel?.SetInteractionPaused(false);
        if (restartFadeOverlay != null) restartFadeOverlay.blocksRaycasts = false;
    }

    public void BeginGameplay()
    {
        if (IsRunning || IsComplete || mainLines.Count == 0) return;

        IsFailed = false;
        IsComplete = false;
        IsRunning = true;
        healthDisplay?.ResetHealth();
        healthDisplay?.Show();
        HideGroup(failurePanelGroup);
        HideIncident();
        RefillRandomIncidentPool();
        ShowMainLine(0);
    }

    public void ToggleSelectedPanel()
    {
        if (!IsRunning || !IncidentIsVisible) return;
        if (SelectedPanel == PanelSelection.Main)
        {
            mainTypedCharacters = 0;
            RefreshMainMessage();
            ApplySelection(PanelSelection.Incident);
        }
        else
        {
            incidentTypedCharacters = 0;
            RefreshIncidentMessage();
            ApplySelection(PanelSelection.Main);
        }
    }

    public void SubmitCharacter(char character)
    {
        if (!IsRunning || character == '\t' || character == '\r' || character == '\n') return;
        if (!char.IsControl(character)) GameAudioService.Instance?.PlayKeyboardTap();

        if (SelectedPanel == PanelSelection.Incident && IncidentIsVisible)
        {
            incidentTypedCharacters = ApplyTypedCharacter(incidentLine.Target, incidentTypedCharacters, character);
            RefreshIncidentMessage();
            if (IsCompleteTarget(incidentLine.Target, incidentTypedCharacters)) HideIncident();
            return;
        }

        mainTypedCharacters = ApplyTypedCharacter(mainLine.Target, mainTypedCharacters, character);
        RefreshMainMessage();
    }

    public void SetTiming(float mainLineSeconds, float incidentLineSeconds, float minimumSpawnDelay, float maximumSpawnDelay)
    {
        mainSecondsPerLine = Mathf.Max(0.1f, mainLineSeconds);
        for (int index = 0; index < mainLineDurations.Count; index++) mainLineDurations[index] = mainSecondsPerLine;
        incidentSeconds = Mathf.Max(0.1f, incidentLineSeconds);
        incidentSpawnDelayRange = new Vector2(Mathf.Max(0f, minimumSpawnDelay), Mathf.Max(minimumSpawnDelay, maximumSpawnDelay));
    }

    public void SetRestartFadeDuration(float halfDuration) => restartFadeHalfDuration = Mathf.Max(0f, halfDuration);

    public void RetryFromFailure()
    {
        if (!IsFailed || IsRestartTransitioning || !isActiveAndEnabled) return;
        restartRoutine = StartCoroutine(RestartFromFinalTutorialLine());
    }

    private void ShowMainLine(int index)
    {
        CurrentMainLineIndex = index;
        mainLine = ParseLine(mainLines[index]);
        mainTypedCharacters = 0;
        mainElapsed = 0f;
        currentMainLineDuration = GetMainLineDuration(index);
        SetMainTimer(1f);
        if (mainHeadingText != null) mainHeadingText.text = "LIVE CEREMONY SCRIPT";
        if (mainTimerLabel != null) mainTimerLabel.text = "SPEECH TIMER";
        if (mainPageText != null) mainPageText.text = $"{index + 1:00} / {mainLines.Count:00}";
        RefreshMainMessage();
        if (mainLines[index].IndexOf("Merdeka", System.StringComparison.OrdinalIgnoreCase) >= 0)
            GameAudioService.Instance?.PlayMerdeka();
        ApplySelection(IncidentIsVisible ? SelectedPanel : PanelSelection.Main);
    }

    private void UpdateMainTimer(float delta)
    {
        mainElapsed += delta;
        SetMainTimer(1f - Mathf.Clamp01(mainElapsed / currentMainLineDuration));
        if (mainElapsed < currentMainLineDuration) return;

        int completedLine = CurrentMainLineIndex;
        if (!IsCompleteTarget(mainLine.Target, mainTypedCharacters) && ApplyMiss()) return;

        SetMainTimer(0f);
        if (completedLine >= mainLines.Count - 1)
        {
            IsRunning = false;
            IsComplete = true;
            HideIncident();
            onGameplayCompleted?.Invoke();
            return;
        }

        ScheduleIncidentAfterMainLine(completedLine);
        ShowMainLine(completedLine + 1);
    }

    private void UpdateIncidentTimer(float delta)
    {
        if (!IncidentIsVisible) return;
        incidentElapsed += delta;
        SetIncidentTimer(1f - Mathf.Clamp01(incidentElapsed / incidentSeconds));
        if (incidentElapsed < incidentSeconds) return;

        bool missed = !IsCompleteTarget(incidentLine.Target, incidentTypedCharacters);
        HideIncident();
        if (missed) ApplyMiss();
    }

    private bool ApplyMiss()
    {
        if (healthDisplay == null || !healthDisplay.LoseHealth()) return false;

        IsRunning = false;
        IsFailed = true;
        SetMainTimer(0f);
        if (incidentSpawnRoutine != null) StopCoroutine(incidentSpawnRoutine);
        incidentSpawnRoutine = null;
        HideIncident();
        if (failureMessageText != null)
            failureMessageText.text = "CEREMONY RECORDING FAILED\n\nThree required entries were missed.\n\nPRESS R TO RETRY FROM THE FINAL TUTORIAL CHECK";
        GameAudioService.Instance?.StopMusic();
        HideGroup(failurePanelGroup);
        failureRevealRoutine = StartCoroutine(RevealFailureAfterBlackFade());
        lossMusicDelayRoutine = StartCoroutine(PlayLossMusicAfterDelay());
        return true;
    }

    private IEnumerator PlayLossMusicAfterDelay()
    {
        if (loseMusicDelay > 0f) yield return new WaitForSecondsRealtime(loseMusicDelay);
        GameAudioService.Instance?.PlayLossMusic();
        lossMusicDelayRoutine = null;
    }

    private IEnumerator RevealFailureAfterBlackFade()
    {
        CanvasGroup fade = failureFadeOverlay != null ? failureFadeOverlay : restartFadeOverlay;
        yield return FadeOverlay(fade, fade != null ? fade.alpha : 0f, 1f, restartFadeHalfDuration);
        ShowGroup(failurePanelGroup);
        yield return FadeOverlay(fade, 1f, 0f, restartFadeHalfDuration);
        if (fade != null) fade.blocksRaycasts = false;
        failureRevealRoutine = null;
    }

    private IEnumerator RestartFromFinalTutorialLine()
    {
        IsRestartTransitioning = true;
        if (lossMusicDelayRoutine != null)
        {
            StopCoroutine(lossMusicDelayRoutine);
            lossMusicDelayRoutine = null;
        }
        GameAudioService.Instance?.RestoreCurrentSceneMusic();
        yield return FadeRestartOverlay(RestartFadeAlpha, 1f, restartFadeHalfDuration);

        IsRunning = false;
        IsFailed = false;
        IsComplete = false;
        CurrentMainLineIndex = -1;
        mainTypedCharacters = 0;
        SetMainTimer(0f);
        if (incidentSpawnRoutine != null) StopCoroutine(incidentSpawnRoutine);
        incidentSpawnRoutine = null;
        HideIncident();
        HideGroup(failurePanelGroup);
        healthDisplay?.ResetHealth();
        tutorialPanel?.RestartFromFinalLine();
        tutorialPanel?.SetInteractionPaused(true);

        yield return null;
        yield return FadeRestartOverlay(1f, 0f, restartFadeHalfDuration);
        if (restartFadeOverlay != null) restartFadeOverlay.blocksRaycasts = false;
        tutorialPanel?.SetInteractionPaused(false);
        IsRestartTransitioning = false;
        restartRoutine = null;
    }

    private IEnumerator FadeRestartOverlay(float from, float to, float duration)
        => FadeOverlay(restartFadeOverlay, from, to, duration);

    private static IEnumerator FadeOverlay(CanvasGroup overlay, float from, float to, float duration)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        overlay.alpha = from;
        overlay.interactable = false;
        overlay.blocksRaycasts = true;
        if (duration <= 0f)
        {
            overlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        overlay.alpha = to;
    }

    private float GetMainLineDuration(int index)
    {
        if (index >= 0 && index < mainLineDurations.Count && mainLineDurations[index] > 0f)
            return Mathf.Max(0.1f, mainLineDurations[index]);
        return Mathf.Max(0.1f, mainSecondsPerLine);
    }

    private void ScheduleIncidentAfterMainLine(int completedLine)
    {
        int incidentIndex = -1;
        if (completedLine == 1) incidentIndex = 0;
        else if (completedLine == 3) incidentIndex = 1;
        else if (completedLine == 5) incidentIndex = 2;
        else if (completedLine == 7 || completedLine == 10) incidentIndex = TakeRandomMediumIncident();
        else if (completedLine == 9) incidentIndex = 4;
        else if (completedLine == 12) incidentIndex = 5;
        else if (completedLine == 16) incidentIndex = 9;

        if (incidentIndex < 0 || incidentIndex >= incidentPrompts.Count || incidentIndex >= incidentRequiredWords.Count) return;
        if (incidentSpawnRoutine != null) StopCoroutine(incidentSpawnRoutine);
        incidentSpawnRoutine = StartCoroutine(SpawnIncidentAfterDelay(incidentIndex));
    }

    private IEnumerator SpawnIncidentAfterDelay(int incidentIndex)
    {
        float minimum = Mathf.Max(0f, Mathf.Min(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y));
        float elapsed = 0f;
        float delay = Random.Range(minimum, maximum);
        while (elapsed < delay)
        {
            if (!IsRunning) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        while (IncidentIsVisible)
        {
            if (!IsRunning) yield break;
            yield return null;
        }

        incidentSpawnRoutine = null;
        ShowIncident(incidentIndex);
    }

    private void ShowIncident(int incidentIndex)
    {
        activeIncidentIndex = incidentIndex;
        incidentLine = new ParsedLine(incidentPrompts[incidentIndex] + "\n\n", incidentRequiredWords[incidentIndex], string.Empty);
        incidentTypedCharacters = 0;
        incidentElapsed = 0f;
        IncidentIsVisible = true;
        if (incidentPanelGroup != null)
        {
            incidentPanelGroup.gameObject.SetActive(true);
            incidentPanelGroup.alpha = 1f;
        }
        if (incidentHeadingText != null) incidentHeadingText.text = "CEREMONY INCIDENT";
        RandomizeIncidentPosition();
        SetIncidentTimer(1f);
        RefreshIncidentMessage();
        ApplySelection(PanelSelection.Main);
    }

    private void HideIncident()
    {
        IncidentIsVisible = false;
        activeIncidentIndex = -1;
        incidentTypedCharacters = 0;
        SetIncidentTimer(0f);
        ApplySelection(PanelSelection.Main);
        if (incidentPanelGroup != null)
        {
            incidentPanelGroup.alpha = 0f;
            incidentPanelGroup.gameObject.SetActive(false);
        }
    }

    private void RandomizeIncidentPosition()
    {
        if (incidentPanelRect == null || !(incidentPanelRect.parent is RectTransform parentRect)) return;
        Rect parent = parentRect.rect;
        Rect panel = incidentPanelRect.rect;
        Vector2 anchor = (incidentPanelRect.anchorMin + incidentPanelRect.anchorMax) * 0.5f;
        float anchorX = Mathf.Lerp(parent.xMin, parent.xMax, anchor.x);
        float anchorY = Mathf.Lerp(parent.yMin, parent.yMax, anchor.y);
        float minimumX = parent.xMin - anchorX + panel.width * incidentPanelRect.pivot.x + incidentCanvasMargin;
        float maximumX = parent.xMax - anchorX - panel.width * (1f - incidentPanelRect.pivot.x) - incidentCanvasMargin;
        float minimumY = parent.yMin - anchorY + panel.height * incidentPanelRect.pivot.y + incidentCanvasMargin;
        float maximumY = parent.yMax - anchorY - panel.height * (1f - incidentPanelRect.pivot.y) - incidentCanvasMargin;
        incidentPanelRect.anchoredPosition = new Vector2(
            minimumX <= maximumX ? Random.Range(minimumX, maximumX) : 0f,
            minimumY <= maximumY ? Random.Range(minimumY, maximumY) : 0f);
    }

    private void RefillRandomIncidentPool()
    {
        randomMediumIncidentPool.Clear();
        int[] mediumIndices = { 3, 6, 7, 8 };
        foreach (int index in mediumIndices)
            if (index < incidentPrompts.Count && index < incidentRequiredWords.Count) randomMediumIncidentPool.Add(index);
    }

    private int TakeRandomMediumIncident()
    {
        if (randomMediumIncidentPool.Count == 0) RefillRandomIncidentPool();
        if (randomMediumIncidentPool.Count == 0) return -1;
        int poolIndex = Random.Range(0, randomMediumIncidentPool.Count);
        int incidentIndex = randomMediumIncidentPool[poolIndex];
        randomMediumIncidentPool.RemoveAt(poolIndex);
        return incidentIndex;
    }

    private void ApplySelection(PanelSelection selection)
    {
        SelectedPanel = selection;
        ApplyOutline(mainSelectionOutline, selection == PanelSelection.Main);
        ApplyOutline(incidentSelectionOutline, selection == PanelSelection.Incident);
    }

    private void ApplyOutline(Outline outline, bool selected)
    {
        if (outline == null) return;
        outline.effectColor = selected ? selectedBorderColor : normalBorderColor;
        outline.effectDistance = selected ? new Vector2(5f, -5f) : new Vector2(2f, -2f);
    }

    private void RefreshMainMessage()
    {
        if (mainMessageText != null) mainMessageText.text = BuildDisplayText(mainLine, mainTypedCharacters);
    }

    private void RefreshIncidentMessage()
    {
        if (incidentMessageText != null) incidentMessageText.text = BuildDisplayText(incidentLine, incidentTypedCharacters);
    }

    private string BuildDisplayText(ParsedLine line, int typedCharacters)
    {
        int count = Mathf.Clamp(typedCharacters, 0, line.Target.Length);
        string typed = EscapeRichText(line.Target.Substring(0, count));
        string pending = EscapeRichText(line.Target.Substring(count));
        return line.Prefix + ColorTag(typedWordColor, typed) + ColorTag(pendingWordColor, pending) + line.Suffix;
    }

    private static ParsedLine ParseLine(string source)
    {
        const string marker = "[TYPE:";
        if (string.IsNullOrEmpty(source)) return new ParsedLine(string.Empty, string.Empty, string.Empty);
        int start = source.IndexOf(marker, System.StringComparison.Ordinal);
        if (start < 0) return new ParsedLine(source, string.Empty, string.Empty);
        int targetStart = start + marker.Length;
        int end = source.IndexOf(']', targetStart);
        if (end < 0) return new ParsedLine(source, string.Empty, string.Empty);
        return new ParsedLine(source.Substring(0, start), source.Substring(targetStart, end - targetStart), source.Substring(end + 1));
    }

    private static int ApplyTypedCharacter(string target, int count, char character)
    {
        if (string.IsNullOrEmpty(target)) return 0;
        if (character == '\b') return Mathf.Max(0, count - 1);
        if (count >= target.Length) return count;
        return char.ToUpperInvariant(character) == char.ToUpperInvariant(target[count]) ? count + 1 : count;
    }

    private static bool IsCompleteTarget(string target, int count) => string.IsNullOrEmpty(target) || count >= target.Length;
    private static string ColorTag(Color color, string text) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";

    private static string EscapeRichText(string value)
    {
        StringBuilder builder = new StringBuilder(value != null ? value.Length : 0);
        if (value == null) return string.Empty;
        foreach (char character in value)
        {
            if (character == '<') builder.Append("&lt;");
            else if (character == '>') builder.Append("&gt;");
            else if (character == '&') builder.Append("&amp;");
            else builder.Append(character);
        }
        return builder.ToString();
    }

    private void SetMainTimer(float remaining)
    {
        MainTimerRemaining = Mathf.Clamp01(remaining);
        mainLeftTimerSlider?.SetValueWithoutNotify(MainTimerRemaining);
        mainRightTimerSlider?.SetValueWithoutNotify(MainTimerRemaining);
    }

    private void SetIncidentTimer(float remaining)
    {
        IncidentTimerRemaining = Mathf.Clamp01(remaining);
        incidentLeftTimerSlider?.SetValueWithoutNotify(IncidentTimerRemaining);
        incidentRightTimerSlider?.SetValueWithoutNotify(IncidentTimerRemaining);
    }

    private static void HideGroup(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private static void ShowGroup(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        mainSecondsPerLine = Mathf.Max(0.1f, mainSecondsPerLine);
        for (int index = 0; index < mainLineDurations.Count; index++)
            mainLineDurations[index] = Mathf.Max(0.1f, mainLineDurations[index]);
        incidentSeconds = Mathf.Max(0.1f, incidentSeconds);
        restartFadeHalfDuration = Mathf.Max(0f, restartFadeHalfDuration);
        loseMusicDelay = Mathf.Max(0f, loseMusicDelay);
        incidentCanvasMargin = Mathf.Max(0f, incidentCanvasMargin);
        incidentSpawnDelayRange.x = Mathf.Max(0f, incidentSpawnDelayRange.x);
        incidentSpawnDelayRange.y = Mathf.Max(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y);
    }
#endif

    private readonly struct ParsedLine
    {
        public ParsedLine(string prefix, string target, string suffix)
        {
            Prefix = prefix;
            Target = target;
            Suffix = suffix;
        }
        public string Prefix { get; }
        public string Target { get; }
        public string Suffix { get; }
    }
}
