using System;
using System.Linq;
using DefenderOfIndependence.Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelTwoDayPlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string ActiveKey = "Defender.Level2.DaySmoke.Active";
    private const string ResultKey = "Defender.Level2.DaySmoke.Result";

    private static LevelTwoDayController _controller;
    private static LevelTwoNextDayClock _clock;
    private static LevelTwoFirstPersonController _player;
    private static LevelTwoScheduledCharacter[] _characters;
    private static LevelTwoNegotiationMeterController _meters;
    private static GameObject _energyHud;
    private static GameObject _meterHud;
    private static int _expectedDay;
    private static double _deadline;
    private static bool _advanceRequested;
    private static bool _preparedForFinalDay;

    static LevelTwoDayPlayModeSmoke()
    {
        if (SessionState.GetBool(ActiveKey, false))
        {
            EditorApplication.delayCall += ResumeAfterReload;
        }
    }

    [MenuItem("Project Tools/Level 2/Validate Day System in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode before starting the Level 2 day smoke test.");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        EditorApplication.isPlaying = true;
    }

    public static void RunFromCommandLine()
    {
        try
        {
            Run();
        }
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
            EditorApplication.update += BeginWhenReady;
        }
    }

    private static void BeginWhenReady()
    {
        _controller = UnityEngine.Object.FindFirstObjectByType<LevelTwoDayController>();
        _clock = UnityEngine.Object.FindFirstObjectByType<LevelTwoNextDayClock>();
        _player = UnityEngine.Object.FindFirstObjectByType<LevelTwoFirstPersonController>();
        _characters = UnityEngine.Object.FindObjectsByType<LevelTwoScheduledCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _meters = UnityEngine.Object.FindFirstObjectByType<LevelTwoNegotiationMeterController>();
        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _energyHud = allTransforms.Single(item => item.name == "Energy Display").gameObject;
        _meterHud = allTransforms.Single(item => item.name == "Level 2 Negotiation Meters").gameObject;
        if (_controller == null || _clock == null || _player == null || _meters == null || _characters.Length != 6)
        {
            Fail("Play Mode scene did not load all Level 2 day-system components.");
            return;
        }

        EditorApplication.update -= BeginWhenReady;
        _expectedDay = 1;
        _advanceRequested = false;
        _preparedForFinalDay = false;
        _deadline = EditorApplication.timeSinceStartup + 35d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline)
        {
            Fail("Timed out while advancing the six-day sequence.");
            return;
        }

        if (_controller.IsTransitioning)
        {
            if (_energyHud.activeSelf || _meterHud.activeSelf)
            {
                Fail("Energy or negotiation meters remained visible during a day transition.");
            }
            return;
        }

        if (!_energyHud.activeSelf || !_meterHud.activeSelf)
        {
            Fail("Energy or negotiation meters did not return after the day transition.");
            return;
        }

        if (_controller.CurrentDay != _expectedDay)
        {
            Fail($"Expected Day {_expectedDay}, observed Day {_controller.CurrentDay}.");
            return;
        }

        if (!ValidateVisibleCharacters(_expectedDay))
        {
            return;
        }
        if (_expectedDay > 1 && Vector3.Distance(_player.transform.position, _player.InitialSpawnPoint.position) > 0.05f)
        {
            Fail($"Player did not return to PlayerInitialSpawnPoint on Day {_expectedDay}.");
            return;
        }

        if (_expectedDay == 6)
        {
            if (_clock.TryUse())
            {
                Fail("NextDayClock advanced beyond the final day.");
                return;
            }

            Pass();
            return;
        }

        if (!_advanceRequested)
        {
            if (_expectedDay == 5 && !_preparedForFinalDay)
            {
                _meters.ApplyConversationResult(new LevelTwoNegotiationMeterController.MeterChange
                    { britishConfidence = 100, delegationUnity = 100, publicSupport = 100 });
                _preparedForFinalDay = true;
            }
            if (!_clock.TryUse())
            {
                Fail($"NextDayClock refused to advance from Day {_expectedDay}.");
                return;
            }

            _expectedDay++;
            _advanceRequested = true;
            return;
        }

        _advanceRequested = false;
    }

    private static bool ValidateVisibleCharacters(int day)
    {
        foreach (LevelTwoScheduledCharacter character in _characters)
        {
            bool expected = character.ActiveDay == day;
            if (character.CharacterVisual.activeSelf != expected)
            {
                Fail($"{character.name} visibility is incorrect on Day {day}.");
                return false;
            }
        }

        int expectedCount = day == 4 ? 0 : day == 6 ? 2 : 1;
        int visibleCount = _characters.Count(character => character.CharacterVisual.activeSelf);
        if (visibleCount != expectedCount)
        {
            Fail($"Expected {expectedCount} visible character(s) on Day {day}, observed {visibleCount}.");
            return false;
        }

        return true;
    }

    private static void Pass()
    {
        Debug.Log("LEVEL_TWO_DAY_PLAYMODE_OK: advanced Days 1-6 with qualifying meters, hid persistent HUD during every day card, restored HUD afterward, returned to lobby spawn, matched schedules, and stopped at Day 6.");
        Complete("PASS");
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL_TWO_DAY_PLAYMODE_FAILED: " + message);
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
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

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
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
