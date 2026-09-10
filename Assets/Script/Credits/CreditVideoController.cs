using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class CreditVideoController : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Camera playbackCamera;
    [SerializeField] private string fallbackDestinationScene = "Scene_Gallery";
    [SerializeField, Min(1f)] private float prepareTimeout = 15f;

    [Header("Completion Message")]
    [SerializeField] private CanvasGroup completionMessageGroup;
    [SerializeField] private TMP_Text completionMessageText;
    [SerializeField, TextArea(3, 8)] private string completionMessage =
        "Thank you for joining the journey toward Malayan independence.";
    [SerializeField, Min(0f)] private float completionMessageFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float completionMessageHoldDuration = 2.5f;

    [Header("Skip")]
    [SerializeField] private CanvasGroup skipPromptGroup;
    [SerializeField] private Slider skipHoldSlider;
    [SerializeField, Min(0.25f)] private float skipHoldDuration = 3f;
    [SerializeField, Min(0f)] private float skipPromptPulseSpeed = 1.2f;

    [Header("Transition")]
    [SerializeField] private CanvasGroup blackFadeOverlay;
    [SerializeField, Min(0f)] private float revealFadeDuration = 1f;
    [SerializeField, Min(0f)] private float exitFadeDuration = 1f;

    private CreditPlaybackRequest playbackRequest;
    private Coroutine flowRoutine;
    private float skipHoldElapsed;
    private bool acceptsSkipInput;
    private bool isFinishing;

    public bool ShowsCompletionMessage => playbackRequest.ShowCompletionMessage;
    public bool AcceptsSkipInput => acceptsSkipInput;
    public float SkipProgress => skipHoldDuration <= 0f ? 0f : Mathf.Clamp01(skipHoldElapsed / skipHoldDuration);

    private void Awake()
    {
        playbackRequest = CreditPlaybackSession.Consume(fallbackDestinationScene);
        ConfigurePlayback();
        SetGroup(completionMessageGroup, 0f, false);
        SetGroup(skipPromptGroup, 0f, false);
        SetFade(1f, true);
        if (completionMessageText != null) completionMessageText.text = completionMessage;
        if (skipHoldSlider != null) skipHoldSlider.SetValueWithoutNotify(0f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived += HandleVideoError;
        flowRoutine = StartCoroutine(PlayCredits());
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }
    }

    private void Update()
    {
        if (!acceptsSkipInput || isFinishing) return;

        if (Input.GetKey(KeyCode.Space))
        {
            skipHoldElapsed += Time.unscaledDeltaTime;
            if (skipHoldElapsed >= skipHoldDuration)
            {
                BeginFinish();
                return;
            }
        }
        else
        {
            skipHoldElapsed = 0f;
        }

        if (skipHoldSlider != null) skipHoldSlider.SetValueWithoutNotify(SkipProgress);
        if (skipPromptGroup != null)
            skipPromptGroup.alpha = 0.72f + Mathf.Sin(Time.unscaledTime * skipPromptPulseSpeed * Mathf.PI * 2f) * 0.18f;
    }

    private IEnumerator PlayCredits()
    {
        videoPlayer.Prepare();

        if (playbackRequest.ShowCompletionMessage)
        {
            yield return FadeGroup(completionMessageGroup, 0f, 1f, completionMessageFadeDuration);
            yield return WaitUnscaled(completionMessageHoldDuration);
            yield return FadeGroup(completionMessageGroup, 1f, 0f, completionMessageFadeDuration);
        }

        float elapsed = 0f;
        while (!videoPlayer.isPrepared && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("Credit video could not be prepared before the timeout.", this);
            BeginFinish();
            yield break;
        }

        videoPlayer.Play();
        acceptsSkipInput = true;
        SetGroup(skipPromptGroup, 1f, false);
        yield return FadeGroup(blackFadeOverlay, 1f, 0f, revealFadeDuration, true);
        flowRoutine = null;
    }

    private void ConfigurePlayback()
    {
        if (videoPlayer == null || audioSource == null || playbackCamera == null)
        {
            Debug.LogError("Credit video references are incomplete.", this);
            enabled = false;
            return;
        }

        playbackCamera.clearFlags = CameraClearFlags.SolidColor;
        playbackCamera.backgroundColor = Color.black;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = playbackCamera;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void HandleVideoFinished(VideoPlayer _) => BeginFinish();

    private void HandleVideoError(VideoPlayer _, string message)
    {
        Debug.LogError("Credit video playback error: " + message, this);
        BeginFinish();
    }

    private void BeginFinish()
    {
        if (isFinishing || !isActiveAndEnabled) return;
        isFinishing = true;
        acceptsSkipInput = false;
        skipHoldElapsed = 0f;
        if (skipHoldSlider != null) skipHoldSlider.SetValueWithoutNotify(0f);
        SetGroup(skipPromptGroup, 0f, false);
        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(FadeOutAndLoadDestination());
    }

    private IEnumerator FadeOutAndLoadDestination()
    {
        yield return FadeGroup(blackFadeOverlay, blackFadeOverlay != null ? blackFadeOverlay.alpha : 0f, 1f,
            exitFadeDuration, true);
        videoPlayer.Stop();
        SceneManager.LoadScene(playbackRequest.DestinationScene);
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration, bool blockRaycasts = false)
    {
        if (group == null) yield break;
        group.gameObject.SetActive(true);
        group.blocksRaycasts = blockRaycasts;
        group.interactable = false;
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

    private void SetFade(float alpha, bool blockRaycasts)
    {
        SetGroup(blackFadeOverlay, alpha, blockRaycasts);
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool blockRaycasts)
    {
        if (group == null) return;
        group.gameObject.SetActive(true);
        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = blockRaycasts;
    }
}
