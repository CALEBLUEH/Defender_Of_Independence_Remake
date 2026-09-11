using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DefenderOfIndependence.Audio;

public sealed class LevelThreeTutorialPanel : MonoBehaviour
{
    public enum TutorialPanelSelection { Main, Incident }

    private const string TypeMarkerStart = "[TYPE:";
    private const string TypeMarkerEnd = "]";

    [Header("Main Tutorial Panel")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Outline selectionOutline;

    [Header("Main Message Timer")]
    [SerializeField] private Slider leftTimerSlider;
    [SerializeField] private Slider rightTimerSlider;
    [SerializeField] private TMP_Text timerLabel;

    [Header("Incident Tutorial Panel")]
    [SerializeField] private CanvasGroup incidentPanelGroup;
    [SerializeField] private TMP_Text incidentHeadingText;
    [SerializeField] private TMP_Text incidentMessageText;
    [SerializeField] private Slider incidentLeftTimerSlider;
    [SerializeField] private Slider incidentRightTimerSlider;
    [SerializeField] private Outline incidentSelectionOutline;
    [SerializeField, TextArea(2, 5)] private string incidentTutorialLine =
        "Switch here with Left Tab, then type [TYPE:left tab] to restore panel control.";
    [SerializeField, Min(0)] private int incidentTriggerLineIndex = 9;
    [SerializeField] private Vector2 incidentSpawnDelayRange = new Vector2(1f, 2f);

    [Header("Health Introduction")]
    [SerializeField] private LevelThreeHealthDisplay healthDisplay;
    [SerializeField, Min(0)] private int healthIntroductionLineIndex = 11;

    [Header("Skip Tutorial")]
    [SerializeField] private CanvasGroup skipPromptGroup;
    [SerializeField] private TMP_Text skipPromptText;
    [SerializeField] private Slider skipHoldSlider;
    [SerializeField, Min(0.1f)] private float skipHoldDuration = 3f;
    [SerializeField, Min(0f)] private float skipPromptPulseSpeed = 1.2f;

    [Header("Completion")]
    [SerializeField] private UnityEvent onTutorialFinished = new UnityEvent();

    [Header("Selection Styling")]
    [SerializeField] private Color normalBorderColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
    [SerializeField] private Color selectedBorderColor = new Color(0.15f, 0.85f, 1f, 1f);
    [SerializeField] private Color typedWordColor = new Color(0.3f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color pendingWordColor = new Color(0.68f, 0.72f, 0.77f, 1f);

    [Header("Tutorial Content")]
    [SerializeField, TextArea(2, 5)] private List<string> tutorialLines = new List<string>();
    [SerializeField, Min(0f)] private float panelFadeInDuration = 1.25f;
    [SerializeField, Min(0.1f)] private float secondsPerLine = 5f;
    [SerializeField, Min(0f)] private float contentFadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float incidentFadeInDuration = 0.3f;

    private Coroutine showRoutine;
    private Coroutine lineChangeRoutine;
    private Coroutine incidentSpawnRoutine;
    private ParsedTypingLine mainLine;
    private ParsedTypingLine incidentLine;
    private float mainElapsed;
    private float incidentElapsed;
    private int mainTypedCharacters;
    private int incidentTypedCharacters;
    private bool lineIsActive;
    private bool incidentPending;
    private float skipHoldElapsed;
    private bool interactionPaused;

    public int CurrentLineIndex { get; private set; } = -1;
    public float TimerRemaining { get; private set; } = 1f;
    public float MainTimerRemaining => TimerRemaining;
    public float IncidentTimerRemaining { get; private set; } = 1f;
    public float PanelAlpha => panelGroup != null ? panelGroup.alpha : 0f;
    public bool IsVisible { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IncidentPanelVisible { get; private set; }
    public bool IncidentResolved { get; private set; }
    public TutorialPanelSelection SelectedPanel { get; private set; } = TutorialPanelSelection.Main;
    public int MainTypedCharacterCount => mainTypedCharacters;
    public int IncidentTypedCharacterCount => incidentTypedCharacters;
    public float SkipHoldProgress => skipHoldDuration > 0f ? Mathf.Clamp01(skipHoldElapsed / skipHoldDuration) : 1f;
    public float SkipPromptAlpha => skipPromptGroup != null ? skipPromptGroup.alpha : 0f;
    public bool IsInteractionPaused => interactionPaused;
    public UnityEvent TutorialFinishedEvent => onTutorialFinished;

    private void Awake()
    {
        ConfigureHiddenGroup(panelGroup);
        ConfigureHiddenGroup(incidentPanelGroup);
        if (contentGroup != null) contentGroup.alpha = 1f;
        SetMainTimer(1f);
        SetIncidentTimer(1f);
        SetSkipProgress(0f);
        HideSkipPrompt();
        ApplySelection(TutorialPanelSelection.Main);
    }

    private void Update()
    {
        if (!IsVisible || !lineIsActive || IsFinished || interactionPaused) return;

        UpdateSkipPrompt(Time.unscaledDeltaTime);

        if (IncidentPanelVisible && !IncidentResolved && Input.GetKeyDown(KeyCode.Tab))
            ToggleSelectedPanel();

        string input = Input.inputString;
        for (int index = 0; index < input.Length; index++) SubmitCharacter(input[index]);

        float delta = Time.unscaledDeltaTime;
        UpdateIncidentTimer(delta);
        UpdateMainTimer(delta);
    }

    private void OnDisable()
    {
        StopTutorialCoroutines();
        HideSkipPrompt();
    }

    public void ShowTutorial()
    {
        if (!isActiveAndEnabled || showRoutine != null || IsVisible || tutorialLines.Count == 0) return;
        GameAudioService.Instance?.StartLevelThreeMusic();
        showRoutine = StartCoroutine(FadeInAndStart());
    }

    public void ToggleSelectedPanel()
    {
        if (!IncidentPanelVisible || IncidentResolved || !lineIsActive) return;

        if (SelectedPanel == TutorialPanelSelection.Main)
        {
            mainTypedCharacters = 0;
            RefreshMainMessage();
            ApplySelection(TutorialPanelSelection.Incident);
        }
        else
        {
            incidentTypedCharacters = 0;
            RefreshIncidentMessage();
            ApplySelection(TutorialPanelSelection.Main);
        }
    }

    public void SubmitCharacter(char character)
    {
        if (!IsVisible || !lineIsActive || IsFinished || character == '\t' || character == '\r' || character == '\n') return;
        if (!char.IsControl(character)) GameAudioService.Instance?.PlayKeyboardTap();

        if (SelectedPanel == TutorialPanelSelection.Incident && IncidentPanelVisible && !IncidentResolved)
        {
            incidentTypedCharacters = ApplyTypedCharacter(incidentLine.Target, incidentTypedCharacters, character);
            RefreshIncidentMessage();
            if (IsTargetComplete(incidentLine.Target, incidentTypedCharacters)) ResolveIncidentImmediately();
            return;
        }

        mainTypedCharacters = ApplyTypedCharacter(mainLine.Target, mainTypedCharacters, character);
        RefreshMainMessage();
    }

    public void SetSelected(bool selected) => ApplyOutline(selectionOutline, selected);

    public void SetTutorialTiming(float fadeInSeconds, float lineSeconds, float contentFadeSeconds, float incidentFadeSeconds)
    {
        panelFadeInDuration = Mathf.Max(0f, fadeInSeconds);
        secondsPerLine = Mathf.Max(0.1f, lineSeconds);
        contentFadeDuration = Mathf.Max(0f, contentFadeSeconds);
        incidentFadeInDuration = Mathf.Max(0f, incidentFadeSeconds);
    }

    public void SetIncidentSpawnDelay(float minimumSeconds, float maximumSeconds)
    {
        float minimum = Mathf.Max(0f, minimumSeconds);
        incidentSpawnDelayRange = new Vector2(minimum, Mathf.Max(minimum, maximumSeconds));
    }

    public void SetSkipHoldDuration(float seconds) => skipHoldDuration = Mathf.Max(0.1f, seconds);

    public void SetInteractionPaused(bool paused) => interactionPaused = paused;

    public void SubmitSkipHold(float unscaledDeltaTime)
    {
        if (!CanSkipTutorial()) return;
        skipHoldElapsed += Mathf.Max(0f, unscaledDeltaTime);
        SetSkipProgress(SkipHoldProgress);
        if (skipHoldElapsed >= skipHoldDuration) SkipToFinalLine();
    }

    public void CancelSkipHold()
    {
        if (skipHoldElapsed <= 0f) return;
        skipHoldElapsed = 0f;
        SetSkipProgress(0f);
    }

    public void SkipToFinalLine()
    {
        if (!CanSkipTutorial()) return;
        StopTutorialCoroutines();
        ResetIncidentState();
        healthDisplay?.Show();
        StartLine(tutorialLines.Count - 1);
    }

    public void RestartFromFinalLine()
    {
        if (!isActiveAndEnabled || tutorialLines.Count == 0) return;
        StopTutorialCoroutines();
        interactionPaused = false;
        IsVisible = true;
        IsFinished = false;
        if (panelGroup != null) panelGroup.alpha = 1f;
        if (contentGroup != null) contentGroup.alpha = 1f;
        if (headingText != null) headingText.text = "CEREMONY OPERATIONS // FINAL CHECK";
        if (timerLabel != null) timerLabel.text = "READY CHECK";
        ResetIncidentState();
        healthDisplay?.Show();
        StartLine(tutorialLines.Count - 1);
    }

    private IEnumerator FadeInAndStart()
    {
        IsVisible = true;
        IsFinished = false;
        interactionPaused = false;
        skipHoldElapsed = 0f;
        if (headingText != null) headingText.text = "CEREMONY OPERATIONS // TUTORIAL";
        if (timerLabel != null) timerLabel.text = "NEXT MESSAGE";
        yield return FadeGroup(panelGroup, 0f, 1f, panelFadeInDuration);
        showRoutine = null;
        StartLine(0);
    }

    private void StartLine(int index)
    {
        CurrentLineIndex = index;
        mainLine = ParseTypingLine(tutorialLines[index]);
        mainTypedCharacters = 0;
        mainElapsed = 0f;
        SetMainTimer(1f);
        if (pageText != null) pageText.text = $"{index + 1:00} / {tutorialLines.Count:00}";
        if (contentGroup != null) contentGroup.alpha = 1f;
        RefreshMainMessage();
        lineIsActive = true;

        if (index >= tutorialLines.Count - 1) HideSkipPrompt();
        else ShowSkipPrompt();

        if (index == healthIntroductionLineIndex) healthDisplay?.Show();
        if (!IncidentPanelVisible && !incidentPending && index == incidentTriggerLineIndex)
        {
            incidentPending = true;
            incidentSpawnRoutine = StartCoroutine(ShowIncidentAfterDelay());
        }
    }

    private IEnumerator ShowIncidentAfterDelay()
    {
        float minimum = Mathf.Max(0f, Mathf.Min(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y));
        float delay = Random.Range(minimum, maximum);
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        incidentSpawnRoutine = null;
        incidentPending = false;
        ShowIncidentPanel();
    }

    private void ShowIncidentPanel()
    {
        if (incidentPanelGroup != null) incidentPanelGroup.gameObject.SetActive(true);
        IncidentPanelVisible = true;
        IncidentResolved = false;
        incidentLine = ParseTypingLine(incidentTutorialLine);
        incidentTypedCharacters = 0;
        incidentElapsed = 0f;
        SetIncidentTimer(1f);
        if (incidentHeadingText != null) incidentHeadingText.text = "CEREMONY INCIDENT // TRAINING";
        RefreshIncidentMessage();
        StartCoroutine(FadeGroup(incidentPanelGroup, 0f, 1f, incidentFadeInDuration));
        ApplySelection(TutorialPanelSelection.Main);
    }

    private void UpdateMainTimer(float delta)
    {
        mainElapsed += delta;
        SetMainTimer(1f - Mathf.Clamp01(mainElapsed / secondsPerLine));
        if (mainElapsed < secondsPerLine) return;

        bool targetComplete = IsTargetComplete(mainLine.Target, mainTypedCharacters);
        bool incidentAllowsAdvance = (!incidentPending && !IncidentPanelVisible) || IncidentResolved;
        if (!targetComplete || !incidentAllowsAdvance)
        {
            mainElapsed = 0f;
            SetMainTimer(1f);
            return;
        }

        SetMainTimer(0f);
        if (CurrentLineIndex >= tutorialLines.Count - 1)
        {
            IsFinished = true;
            lineIsActive = false;
            HideSkipPrompt();
            onTutorialFinished?.Invoke();
            return;
        }

        lineIsActive = false;
        lineChangeRoutine = StartCoroutine(ChangeToLine(CurrentLineIndex + 1));
    }

    private void UpdateIncidentTimer(float delta)
    {
        if (!IncidentPanelVisible || IncidentResolved) return;

        incidentElapsed += delta;
        SetIncidentTimer(1f - Mathf.Clamp01(incidentElapsed / secondsPerLine));
        if (incidentElapsed < secondsPerLine) return;

        incidentElapsed = 0f;
        SetIncidentTimer(1f);
    }

    private void ResolveIncidentImmediately()
    {
        IncidentResolved = true;
        IncidentPanelVisible = false;
        SetIncidentTimer(0f);
        ApplySelection(TutorialPanelSelection.Main);
        if (incidentPanelGroup == null) return;
        incidentPanelGroup.alpha = 0f;
        incidentPanelGroup.gameObject.SetActive(false);
    }

    private IEnumerator ChangeToLine(int nextIndex)
    {
        yield return FadeGroup(contentGroup, 1f, 0f, contentFadeDuration);
        lineChangeRoutine = null;
        StartLine(nextIndex);
    }

    private void ApplySelection(TutorialPanelSelection selection)
    {
        SelectedPanel = selection;
        ApplyOutline(selectionOutline, selection == TutorialPanelSelection.Main);
        ApplyOutline(incidentSelectionOutline, selection == TutorialPanelSelection.Incident);
    }

    private void UpdateSkipPrompt(float delta)
    {
        if (!CanSkipTutorial())
        {
            HideSkipPrompt();
            return;
        }

        ShowSkipPrompt();
        if (Input.GetKey(KeyCode.Space))
        {
            if (skipPromptGroup != null) skipPromptGroup.alpha = 1f;
            SubmitSkipHold(delta);
            return;
        }

        CancelSkipHold();
        if (skipPromptGroup != null)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * skipPromptPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            skipPromptGroup.alpha = Mathf.Lerp(0.35f, 1f, wave);
        }
    }

    private bool CanSkipTutorial() =>
        IsVisible && !IsFinished && lineIsActive && tutorialLines.Count > 0 && CurrentLineIndex < tutorialLines.Count - 1;

    private void ShowSkipPrompt()
    {
        if (skipPromptGroup != null) skipPromptGroup.gameObject.SetActive(true);
        if (skipPromptText != null) skipPromptText.text = $"HOLD SPACE FOR {skipHoldDuration:0.#} SECONDS TO SKIP TUTORIAL";
    }

    private void HideSkipPrompt()
    {
        skipHoldElapsed = 0f;
        SetSkipProgress(0f);
        if (skipPromptGroup == null) return;
        skipPromptGroup.alpha = 0f;
        skipPromptGroup.gameObject.SetActive(false);
    }

    private void SetSkipProgress(float progress)
    {
        skipHoldSlider?.SetValueWithoutNotify(Mathf.Clamp01(progress));
    }

    private void StopTutorialCoroutines()
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        if (lineChangeRoutine != null) StopCoroutine(lineChangeRoutine);
        if (incidentSpawnRoutine != null) StopCoroutine(incidentSpawnRoutine);
        showRoutine = null;
        lineChangeRoutine = null;
        incidentSpawnRoutine = null;
    }

    private void ResetIncidentState()
    {
        incidentPending = false;
        IncidentPanelVisible = false;
        IncidentResolved = true;
        incidentTypedCharacters = 0;
        SetIncidentTimer(0f);
        ApplySelection(TutorialPanelSelection.Main);
        if (incidentPanelGroup == null) return;
        incidentPanelGroup.alpha = 0f;
        incidentPanelGroup.gameObject.SetActive(false);
    }

    private void ApplyOutline(Outline outline, bool selected)
    {
        if (outline == null) return;
        outline.effectColor = selected ? selectedBorderColor : normalBorderColor;
        outline.effectDistance = selected ? new Vector2(5f, -5f) : new Vector2(2f, -2f);
    }

    private void RefreshMainMessage()
    {
        if (messageText != null) messageText.text = BuildDisplayText(mainLine, mainTypedCharacters);
    }

    private void RefreshIncidentMessage()
    {
        if (incidentMessageText != null) incidentMessageText.text = BuildDisplayText(incidentLine, incidentTypedCharacters);
    }

    private string BuildDisplayText(ParsedTypingLine line, int typedCharacters)
    {
        if (string.IsNullOrEmpty(line.Target)) return line.Prefix;

        int count = Mathf.Clamp(typedCharacters, 0, line.Target.Length);
        string typed = EscapeRichText(line.Target.Substring(0, count));
        string pending = EscapeRichText(line.Target.Substring(count));
        return line.Prefix + ColorTag(typedWordColor, typed) + ColorTag(pendingWordColor, pending) + line.Suffix;
    }

    private static ParsedTypingLine ParseTypingLine(string source)
    {
        if (string.IsNullOrEmpty(source)) return new ParsedTypingLine(string.Empty, string.Empty, string.Empty);
        int start = source.IndexOf(TypeMarkerStart, System.StringComparison.Ordinal);
        if (start < 0) return new ParsedTypingLine(source, string.Empty, string.Empty);
        int targetStart = start + TypeMarkerStart.Length;
        int end = source.IndexOf(TypeMarkerEnd, targetStart, System.StringComparison.Ordinal);
        if (end < 0) return new ParsedTypingLine(source, string.Empty, string.Empty);
        return new ParsedTypingLine(source.Substring(0, start), source.Substring(targetStart, end - targetStart), source.Substring(end + 1));
    }

    private static int ApplyTypedCharacter(string target, int typedCharacters, char character)
    {
        if (string.IsNullOrEmpty(target)) return 0;
        if (character == '\b') return Mathf.Max(0, typedCharacters - 1);
        if (typedCharacters >= target.Length) return typedCharacters;
        return char.ToUpperInvariant(character) == char.ToUpperInvariant(target[typedCharacters]) ? typedCharacters + 1 : typedCharacters;
    }

    private static bool IsTargetComplete(string target, int typedCharacters) =>
        string.IsNullOrEmpty(target) || typedCharacters >= target.Length;

    private static string ColorTag(Color color, string text) =>
        $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";

    private static string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        StringBuilder builder = new StringBuilder(value.Length);
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
        TimerRemaining = Mathf.Clamp01(remaining);
        if (leftTimerSlider != null) leftTimerSlider.SetValueWithoutNotify(TimerRemaining);
        if (rightTimerSlider != null) rightTimerSlider.SetValueWithoutNotify(TimerRemaining);
    }

    private void SetIncidentTimer(float remaining)
    {
        IncidentTimerRemaining = Mathf.Clamp01(remaining);
        if (incidentLeftTimerSlider != null) incidentLeftTimerSlider.SetValueWithoutNotify(IncidentTimerRemaining);
        if (incidentRightTimerSlider != null) incidentRightTimerSlider.SetValueWithoutNotify(IncidentTimerRemaining);
    }

    private static void ConfigureHiddenGroup(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        group.alpha = from;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        panelFadeInDuration = Mathf.Max(0f, panelFadeInDuration);
        secondsPerLine = Mathf.Max(0.1f, secondsPerLine);
        contentFadeDuration = Mathf.Max(0f, contentFadeDuration);
        incidentFadeInDuration = Mathf.Max(0f, incidentFadeInDuration);
        skipHoldDuration = Mathf.Max(0.1f, skipHoldDuration);
        skipPromptPulseSpeed = Mathf.Max(0f, skipPromptPulseSpeed);
        incidentTriggerLineIndex = Mathf.Max(0, incidentTriggerLineIndex);
        healthIntroductionLineIndex = Mathf.Max(0, healthIntroductionLineIndex);
        incidentSpawnDelayRange.x = Mathf.Max(0f, incidentSpawnDelayRange.x);
        incidentSpawnDelayRange.y = Mathf.Max(incidentSpawnDelayRange.x, incidentSpawnDelayRange.y);
    }
#endif

    private readonly struct ParsedTypingLine
    {
        public ParsedTypingLine(string prefix, string target, string suffix)
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
