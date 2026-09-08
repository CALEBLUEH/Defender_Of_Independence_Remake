using System;
using DefenderOfIndependence.Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelTwoFinalDayGatePlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string ActiveKey = "Defender.Level2.FinalDayGateSmoke.Active";
    private const string ResultKey = "Defender.Level2.FinalDayGateSmoke.Result";

    private static LevelTwoDayController _day;
    private static LevelTwoNextDayClock _clock;
    private static LevelTwoNegotiationMeterController _meters;
    private static LevelTwoFirstPersonController _player;
    private static int _targetDay;
    private static bool _advanceRequested;
    private static bool _boundaryPrepared;
    private static bool _gateRequested;
    private static double _deadline;

    static LevelTwoFinalDayGatePlayModeSmoke()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Level 2/Validate Final Day Entry Gate in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the final-day gate smoke test.");
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
        if (EditorApplication.isPlaying) EditorApplication.update += BeginWhenReady;
    }

    private static void BeginWhenReady()
    {
        _day = UnityEngine.Object.FindFirstObjectByType<LevelTwoDayController>();
        _clock = UnityEngine.Object.FindFirstObjectByType<LevelTwoNextDayClock>();
        _meters = UnityEngine.Object.FindFirstObjectByType<LevelTwoNegotiationMeterController>();
        _player = UnityEngine.Object.FindFirstObjectByType<LevelTwoFirstPersonController>();
        if (_day == null || _clock == null || _meters == null || _player == null)
        {
            Fail("The final-day gate dependencies did not load.");
            return;
        }
        EditorApplication.update -= BeginWhenReady;
        _targetDay = 1;
        _deadline = EditorApplication.timeSinceStartup + 40d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline) { Fail("Timed out testing the final-day gate."); return; }
        if (_day.IsTransitioning) return;

        if (!_boundaryPrepared)
        {
            _meters.ApplyConversationResult(new LevelTwoNegotiationMeterController.MeterChange
                { britishConfidence = 20, delegationUnity = 20, publicSupport = 20 });
            if (_meters.BritishConfidence != 50 || _meters.CanEnterFinalDay)
            {
                Fail("The strict over-50 final-day requirement was not enforced at the boundary.");
                return;
            }
            _boundaryPrepared = true;
        }

        if (_day.CurrentDay != _targetDay) { Fail($"Expected Day {_targetDay}, observed Day {_day.CurrentDay}."); return; }
        if (_targetDay < 5)
        {
            if (!_advanceRequested)
            {
                if (!_clock.TryUse()) { Fail($"Could not advance from Day {_targetDay}."); return; }
                _targetDay++;
                _advanceRequested = true;
            }
            else _advanceRequested = false;
            return;
        }

        if (!_gateRequested)
        {
            if (!_clock.TryUse()) { Fail("The Day 5 clock did not resolve the failed entry attempt."); return; }
            _gateRequested = true;
            return;
        }

        if (!_meters.IsFinalResultInteractive) return;
        if (_day.CurrentDay != 5 || _day.CanAdvanceDay || _player.ControlsEnabled || !_player.UiCursorActive ||
            !_meters.IsShowingFinalResult)
        {
            Fail("The failed Day 6 entry did not stay on Day 5 with a locked, interactive failure panel.");
            return;
        }

        Debug.Log("LEVEL_TWO_FINAL_DAY_GATE_PLAYMODE_OK: exactly 50 failed the strict threshold, Day 6 remained locked, and the restart result panel became interactive.");
        Complete("PASS");
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL_TWO_FINAL_DAY_GATE_PLAYMODE_FAILED: " + message);
        Complete("FAIL");
    }

    private static void Complete(string result)
    {
        EditorApplication.update -= Tick;
        EditorApplication.update -= BeginWhenReady;
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
        EditorApplication.update -= BeginWhenReady;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
