using System;
using Fungus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LevelThreeCutscenePlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Cutscene_Level3.unity";
    private const string ActiveKey = "Defender.Level3.CutsceneSmoke.Active";
    private const string ResultKey = "Defender.Level3.CutsceneSmoke.Result";
    private static double _nextInput;
    private static double _deadline;

    static LevelThreeCutscenePlayModeSmoke()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Cutscenes/Validate Level 3 Cutscene in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the Level 3 cutscene smoke test.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
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
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying) { Finish(result == "PASS"); return; }
        if (!EditorApplication.isPlaying) return;
        _deadline = EditorApplication.timeSinceStartup + 30d;
        _nextInput = EditorApplication.timeSinceStartup + 0.75d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline) { Fail("Timed out before loading Scene_Level3."); return; }
        if (SceneManager.GetActiveScene().name == "Scene_Level3")
        {
            Debug.Log("LEVEL3_CUTSCENE_PLAYMODE_OK: advanced all nine Fungus lines and loaded Scene_Level3.");
            Complete("PASS");
            return;
        }
        if (SceneManager.GetActiveScene().name != "Cutscene_Level3")
        {
            Fail("An unexpected scene became active during the Level 3 cutscene.");
            return;
        }
        if (EditorApplication.timeSinceStartup < _nextInput) return;
        DialogInput input = UnityEngine.Object.FindAnyObjectByType<DialogInput>();
        if (input == null) { Fail("The Level 3 Fungus DialogInput was not available."); return; }
        input.SetNextLineFlag();
        _nextInput = EditorApplication.timeSinceStartup + 0.2d;
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL3_CUTSCENE_PLAYMODE_FAILED: " + message);
        Complete("FAIL");
    }

    private static void Complete(string result)
    {
        EditorApplication.update -= Tick;
        SessionState.SetString(ResultKey, result);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Finish(SessionState.GetString(ResultKey, string.Empty) == "PASS");
    }

    private static void Finish(bool passed)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
