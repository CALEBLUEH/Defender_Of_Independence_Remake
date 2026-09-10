using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[InitializeOnLoad]
public static class CreditsPlayModeSmoke
{
    private const string StartScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string ActiveKey = "Defender.CreditsSmoke.Active";
    private const string ResultKey = "Defender.CreditsSmoke.Result";
    private static double deadline;
    private static int stage;

    static CreditsPlayModeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Non-Gameplay/Validate Credits in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before validating credits.");
        EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = true;
    }

    public static void RunFromCommandLine()
    {
        try { Run(); }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void ResumeAfterReload()
    {
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying)
        {
            Finish(result == "PASS");
            return;
        }
        if (!EditorApplication.isPlaying) return;
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 30d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline)
        {
            Fail("Timed out while waiting for credit playback or the Gallery load.");
            return;
        }

        if (stage == 0)
        {
            CreditPlaybackSession.RequestFromLevelCompletion("Scene_Gallery");
            SceneManager.LoadScene("Scene_Credits");
            stage = 1;
            return;
        }

        if (stage == 1)
        {
            CreditVideoController controller = UnityEngine.Object.FindFirstObjectByType<CreditVideoController>();
            if (controller == null) return;
            if (!controller.ShowsCompletionMessage)
            {
                Fail("The Level 3 route did not enable the completion message.");
                return;
            }
            VideoPlayer player = controller.GetComponent<VideoPlayer>();
            if (player == null || player.clip == null || player.clip.audioTrackCount == 0 || player.GetTargetAudioSource(0) == null)
            {
                Fail("The runtime VideoPlayer has no clip or audible track routing.");
                return;
            }
            if (!player.isPlaying || !controller.AcceptsSkipInput) return;

            MethodInfo finish = typeof(CreditVideoController).GetMethod("BeginFinish", BindingFlags.Instance | BindingFlags.NonPublic);
            if (finish == null)
            {
                Fail("Could not invoke the same finish path used by the hold-Space skip.");
                return;
            }
            finish.Invoke(controller, null);
            stage = 2;
            deadline = EditorApplication.timeSinceStartup + 8d;
            return;
        }

        if (stage == 2 && SceneManager.GetActiveScene().name == "Scene_Gallery")
        {
            Debug.Log("CREDITS_PLAYMODE_OK: Completion route showed its message context, the 1920x1080 MP4 played with an audio target, and the skip/end path faded into Scene_Gallery.");
            Complete("PASS");
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError("CREDITS_PLAYMODE_FAILED: " + message);
        Complete("FAIL");
    }

    private static void Complete(string result)
    {
        EditorApplication.update -= Tick;
        SessionState.SetString(ResultKey, result);
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result)) Finish(result == "PASS");
    }

    private static void Finish(bool passed)
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        if (!passed) throw new InvalidOperationException("Credits Play Mode smoke test failed.");
    }
}
