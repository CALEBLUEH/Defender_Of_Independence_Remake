using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-1000)]
public sealed class LevelThreeOpeningSequence : MonoBehaviour
{
    [Header("Shot Cameras")]
    [SerializeField] private Camera camera1;
    [SerializeField] private Camera camera2;
    [SerializeField] private Camera camera3;

    [Header("Shot Animation")]
    [SerializeField] private Animator camera1Animator;
    [SerializeField] private Animator camera2Animator;
    [SerializeField] private Animator camera3Animator;
    [SerializeField] private Animator tunkuAbdulRahmanAnimator;
    [SerializeField] private AnimationClip camera1Clip;
    [SerializeField] private AnimationClip camera2Clip;
    [SerializeField] private AnimationClip camera3Clip;
    [SerializeField] private AnimationClip tunkuAbdulRahmanClip;

    [Header("Black Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float sceneEntryFadeInDuration = 2f;
    [Tooltip("One second out plus one second in gives an approximately two-second camera transition.")]
    [SerializeField, Min(0f)] private float transitionHalfDuration = 1f;

    [Header("Sequence")]
    [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;
    [SerializeField] private UnityEvent onSequenceFinished = new UnityEvent();

    private Coroutine sequenceRoutine;

    public int ActiveShot { get; private set; }
    public bool IsTransitioning { get; private set; }
    public bool IsComplete { get; private set; }
    public float FadeAlpha => fadeOverlay != null ? fadeOverlay.alpha : 0f;
    public UnityEvent SequenceFinishedEvent => onSequenceFinished;
    public event Action SequenceCompleted;

    private void Awake()
    {
        PrepareInitialState();
    }

    private void Start()
    {
        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private void OnDisable()
    {
        if (sequenceRoutine == null) return;
        StopCoroutine(sequenceRoutine);
        sequenceRoutine = null;
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Max(0.01f, speed);
        SetAnimatorSpeed(camera1Animator);
        SetAnimatorSpeed(camera2Animator);
        SetAnimatorSpeed(camera3Animator);
        SetAnimatorSpeed(tunkuAbdulRahmanAnimator);
    }

    private IEnumerator PlaySequence()
    {
        IsComplete = false;
        SetShot(1);
        yield return Fade(1f, 0f, sceneEntryFadeInDuration);

        PlayClip(camera1Animator, camera1Clip);
        yield return WaitForSequenceSeconds(GetClipLength(camera1Clip));
        FreezeAtLastFrame(camera1Animator, camera1Clip);

        float shot2Length = Mathf.Max(GetClipLength(camera2Clip), GetClipLength(tunkuAbdulRahmanClip));
        yield return TransitionToShot(2, () =>
        {
            PlayClip(camera2Animator, camera2Clip);
            PlayClip(tunkuAbdulRahmanAnimator, tunkuAbdulRahmanClip);
        });
        yield return WaitForSequenceSeconds(Mathf.Max(0f, shot2Length - transitionHalfDuration));
        FreezeAtLastFrame(camera2Animator, camera2Clip);
        HoldAnimator(tunkuAbdulRahmanAnimator);

        float shot3Length = GetClipLength(camera3Clip);
        yield return TransitionToShot(3, () => PlayClip(camera3Animator, camera3Clip));
        yield return WaitForSequenceSeconds(Mathf.Max(0f, shot3Length - transitionHalfDuration));
        FreezeAtLastFrame(camera3Animator, camera3Clip);

        IsComplete = true;
        sequenceRoutine = null;
        onSequenceFinished?.Invoke();
        SequenceCompleted?.Invoke();
    }

    private IEnumerator TransitionToShot(int shotNumber, Action startShotAtBlack)
    {
        IsTransitioning = true;
        yield return Fade(0f, 1f, transitionHalfDuration);

        // Keep one rendered frame fully black before and after the camera swap.
        // This prevents an Animator end-state evaluation from ever becoming visible.
        yield return null;
        SetShot(shotNumber);
        startShotAtBlack?.Invoke();
        yield return null;
        yield return Fade(1f, 0f, transitionHalfDuration);
        IsTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.alpha = from;
        if (duration <= 0f)
        {
            fadeOverlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime * playbackSpeed;
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeOverlay.alpha = to;
    }

    private IEnumerator WaitForSequenceSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime * playbackSpeed;
            yield return null;
        }
    }

    private void PrepareInitialState()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.interactable = false;
            fadeOverlay.blocksRaycasts = false;
        }

        HoldAnimator(camera1Animator);
        HoldAnimator(camera2Animator);
        HoldAnimator(camera3Animator);
        HoldAnimator(tunkuAbdulRahmanAnimator);
        SetShot(1);
    }

    private void SetShot(int shotNumber)
    {
        ActiveShot = shotNumber;
        SetCameraActive(camera1, shotNumber == 1);
        SetCameraActive(camera2, shotNumber == 2);
        SetCameraActive(camera3, shotNumber == 3);

        if (shotNumber != 2) HoldAnimator(tunkuAbdulRahmanAnimator);
    }

    private static void SetCameraActive(Camera shotCamera, bool active)
    {
        if (shotCamera == null) return;
        shotCamera.gameObject.SetActive(active);
        shotCamera.enabled = active;
        shotCamera.gameObject.tag = active ? "MainCamera" : "Untagged";
    }

    private void PlayClip(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null) return;

        animator.gameObject.SetActive(true);
        animator.enabled = true;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Rebind();
        // Rebind restores authored Animator state, including the previously held speed.
        // Apply playback speed afterwards so the shot starts on this fully black frame.
        animator.speed = playbackSpeed;

        int stateHash = Animator.StringToHash(clip.name);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError($"Animator on '{animator.name}' has no state named '{clip.name}'.", animator);
            return;
        }

        animator.Play(stateHash, 0, 0f);
        animator.Update(0f);
    }

    private static void HoldAnimator(Animator animator)
    {
        if (animator == null) return;
        animator.speed = 0f;
    }

    private static void FreezeAtLastFrame(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null) return;

        animator.speed = 0f;
        animator.enabled = false;
        // Sampling exactly at a non-looping clip's length can wrap to its first frame.
        // Stay just inside the clip so the authored final pose is held during the fade.
        clip.SampleAnimation(animator.gameObject, Mathf.Max(0f, clip.length - 0.0001f));
    }

    private void SetAnimatorSpeed(Animator animator)
    {
        if (animator != null && animator.speed > 0f) animator.speed = playbackSpeed;
    }

    private static float GetClipLength(AnimationClip clip)
    {
        return clip != null ? clip.length : 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
        sceneEntryFadeInDuration = Mathf.Max(0f, sceneEntryFadeInDuration);
        transitionHalfDuration = Mathf.Max(0f, transitionHalfDuration);
    }
#endif
}
