using System;
using System.Linq;
using DefenderOfIndependence.Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelTwoDocumentationPlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string ActiveKey = "Defender.Level2.DocumentSmoke.Active";
    private const string ResultKey = "Defender.Level2.DocumentSmoke.Result";
    private static double _deadline;

    static LevelTwoDocumentationPlayModeSmoke()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Level 2/Validate Documentation in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the documentation smoke test.");

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
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying)
        {
            Finish(result == "PASS");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            _deadline = EditorApplication.timeSinceStartup + 30d;
            EditorApplication.update += Tick;
        }
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline)
        {
            Fail("Timed out waiting for the opening day card.");
            return;
        }

        LevelTwoDayController day = UnityEngine.Object.FindAnyObjectByType<LevelTwoDayController>();
        if (day == null || day.IsTransitioning) return;

        LevelTwoDocumentViewer viewer = UnityEngine.Object.FindAnyObjectByType<LevelTwoDocumentViewer>();
        LevelTwoFirstPersonController player = UnityEngine.Object.FindAnyObjectByType<LevelTwoFirstPersonController>();
        LevelTwoDocumentLocation[] locations = UnityEngine.Object.FindObjectsByType<LevelTwoDocumentLocation>(FindObjectsInactive.Include);
        if (viewer == null || player == null || locations.Length != 4)
        {
            Fail("The scene did not load one viewer, one player, and four document locations.");
            return;
        }

        LevelTwoDocumentLocation lobby = locations.Single(item => item.IsReusable);
        day.ResetToStartingDay();
        int lobbyEnergy = day.CurrentEnergy;
        for (int opening = 0; opening < 3; opening++)
        {
            if (!lobby.TryExamine(viewer, day) || day.CurrentEnergy != lobbyEnergy || lobby.IsExhausted ||
                !viewer.IsOpen || !player.UiCursorActive)
            {
                Fail("The Main Lobby guide was not reusable, free, or pointer-enabled.");
                return;
            }
            viewer.Close();
        }

        LevelTwoDocumentLocation paidLocation = locations.First(item => !item.IsReusable);
        for (int i = 0; i < 4; i++)
        {
            if (!day.TrySpendEnergy(1)) { Fail("Energy could not be spent four times."); return; }
        }
        if (day.CurrentEnergy != 0 || !paidLocation.GetPrompt(day).StartsWith("NO ENERGY"))
        {
            Fail("The zero-energy state or prompt is incorrect.");
            return;
        }

        foreach (LevelTwoDocumentLocation location in locations.Where(item => !item.IsReusable).OrderBy(item => item.name))
        {
            day.ResetToStartingDay();
            for (int page = 0; page < 3; page++)
            {
                int energyBefore = day.CurrentEnergy;
                if (!location.TryExamine(viewer, day) || !viewer.IsOpen || player.ControlsEnabled ||
                    location.RemainingCount != 2 - page || day.CurrentEnergy != energyBefore - 1)
                {
                    Fail($"{location.name} failed while opening document {page + 1}.");
                    return;
                }

                viewer.Close();
                if (viewer.IsOpen || !player.ControlsEnabled || player.UiCursorActive)
                {
                    Fail($"{location.name} did not restore gameplay after closing.");
                    return;
                }
            }

            int exhaustedEnergy = day.CurrentEnergy;
            if (!location.IsExhausted || location.TryExamine(viewer, day) || day.CurrentEnergy != exhaustedEnergy ||
                location.GetPrompt(day) != "NO DOCUMENTS REMAIN HERE")
            {
                Fail($"{location.name} did not stay exhausted after all three pages.");
                return;
            }
        }

        Pass();
    }

    private static void Pass()
    {
        Debug.Log("LEVEL_TWO_DOCUMENTATION_PLAYMODE_OK: free reusable lobby guide, nine one-time historical pages, pointer/scroll UI, energy reset, exhausted prompts.");
        Complete("PASS");
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL_TWO_DOCUMENTATION_PLAYMODE_FAILED: " + message);
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
