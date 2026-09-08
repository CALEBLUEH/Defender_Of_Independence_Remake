using System;
using System.Linq;
using DefenderOfIndependence.Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelTwoConversationPlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string ActiveKey = "Defender.Level2.ConversationSmoke.Active";
    private const string ResultKey = "Defender.Level2.ConversationSmoke.Result";

    private static LevelTwoDayController _day;
    private static LevelTwoConversationViewer _viewer;
    private static LevelTwoConversationTrigger[] _triggers;
    private static LevelTwoDoorTransition[] _doors;
    private static LevelTwoDoorInteractor _interactor;
    private static LevelTwoFirstPersonController _player;
    private static LevelTwoScreenFader _fader;
    private static int _expectedDay;
    private static bool _conversationTested;
    private static int _doorStage;
    private static double _deadline;

    static LevelTwoConversationPlayModeSmoke()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Level 2/Validate Dialogue and Door Energy in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the dialogue smoke test.");

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

        if (EditorApplication.isPlaying) EditorApplication.update += BeginWhenReady;
    }

    private static void BeginWhenReady()
    {
        _day = UnityEngine.Object.FindAnyObjectByType<LevelTwoDayController>();
        _viewer = UnityEngine.Object.FindAnyObjectByType<LevelTwoConversationViewer>();
        _triggers = UnityEngine.Object.FindObjectsByType<LevelTwoConversationTrigger>(FindObjectsInactive.Include);
        _doors = UnityEngine.Object.FindObjectsByType<LevelTwoDoorTransition>(FindObjectsInactive.Include);
        _interactor = UnityEngine.Object.FindAnyObjectByType<LevelTwoDoorInteractor>();
        _player = UnityEngine.Object.FindAnyObjectByType<LevelTwoFirstPersonController>();
        _fader = UnityEngine.Object.FindAnyObjectByType<LevelTwoScreenFader>();
        if (_day == null || _viewer == null || _triggers.Length != 6 || _doors.Length != 3 ||
            _interactor == null || _player == null || _fader == null)
        {
            Fail("The Level 2 dialogue or door components did not load completely.");
            return;
        }

        EditorApplication.update -= BeginWhenReady;
        _expectedDay = 1;
        _conversationTested = false;
        _doorStage = 0;
        _deadline = EditorApplication.timeSinceStartup + 75d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline)
        {
            Fail("Timed out while testing six days of dialogue and door access.");
            return;
        }

        if (_day.IsTransitioning || _fader.IsTransitioning) return;
        if (_day.CurrentDay != _expectedDay)
        {
            Fail($"Expected Day {_expectedDay}, observed Day {_day.CurrentDay}.");
            return;
        }

        LevelTwoConversationTrigger[] enabled = _triggers.Where(item => item.InteractionCollider.enabled).ToArray();
        if (enabled.Length != 1 || enabled[0].ActiveDay != _expectedDay)
        {
            Fail($"Day {_expectedDay} did not expose exactly its scheduled conversation collider.");
            return;
        }

        if (_expectedDay == 1 && _doorStage < 4)
        {
            TestDoorSequence();
            return;
        }

        if (!_conversationTested)
        {
            TestConversation(enabled[0]);
            if (!_conversationTested) return;
        }

        if (_expectedDay == 6)
        {
            LevelTwoDoorTransition alanDoor = _doors.Single(item => item.name == "AlanLennox-BoydDoor");
            foreach (LevelTwoDoorTransition door in _doors)
            {
                Vector3 outside = door.ExitSpawnPoint.position;
                bool expectedLocked = door != alanDoor;
                if (door.IsLocked(outside, _day) != expectedLocked)
                {
                    Fail("Final-day room lockdown did not leave only Alan Lennox-Boyd's door available.");
                    return;
                }
            }

            Pass();
            return;
        }

        if (!_day.TryAdvanceDay())
        {
            Fail($"Could not advance from Day {_expectedDay}.");
            return;
        }

        _expectedDay++;
        _conversationTested = false;
    }

    private static void TestDoorSequence()
    {
        LevelTwoDoorTransition door = _doors.Single(item => item.name == "TunkuAbdulRahmanDoor");
        int energyBefore = _day.CurrentEnergy;
        switch (_doorStage)
        {
            case 0:
                _player.TeleportTo(door.ExitSpawnPoint, door.transform.forward);
                if (!_interactor.TryInteractNearest() || _day.CurrentEnergy != energyBefore - 1)
                {
                    Fail("First room entry did not start or spend exactly 1 Energy.");
                    return;
                }
                break;
            case 1:
                if (!_interactor.TryInteractNearest() || _day.CurrentEnergy != energyBefore)
                {
                    Fail("Leaving a room was not free.");
                    return;
                }
                break;
            case 2:
                if (!_interactor.TryInteractNearest() || _day.CurrentEnergy != energyBefore)
                {
                    Fail("Re-entering a paid room on the same day was not free.");
                    return;
                }
                break;
            case 3:
                if (!_interactor.TryInteractNearest() || _day.CurrentEnergy != energyBefore)
                {
                    Fail("Second exit from a paid room was not free.");
                    return;
                }
                break;
        }

        _doorStage++;
    }

    private static void TestConversation(LevelTwoConversationTrigger trigger)
    {
        int energyBefore = _day.CurrentEnergy;
        if (!trigger.TryBegin(_viewer, _day) || !_viewer.IsOpen || _player.ControlsEnabled ||
            _day.CurrentEnergy != energyBefore - 1 || !_player.UiCursorActive)
        {
            Fail($"Day {_expectedDay} conversation did not open with pointer control and a 1-Energy cost.");
            return;
        }

        int expectedSteps = _expectedDay == 6 ? 3 : 1;
        for (int step = 0; step < expectedSteps; step++)
        {
            if (_viewer.CurrentStepIndex != step)
            {
                Fail($"Day {_expectedDay} dialogue step order is incorrect.");
                return;
            }
            _viewer.SelectChoice(0);
            _viewer.Continue();
        }

        if (_viewer.IsOpen || !_player.ControlsEnabled || _player.UiCursorActive || !trigger.WasConsumed ||
            trigger.TryBegin(_viewer, _day) || !trigger.GetPrompt(_day).StartsWith("NO CONVERSATION"))
        {
            Fail($"Day {_expectedDay} conversation did not close and remain consumed.");
            return;
        }

        _conversationTested = true;
    }

    private static void Pass()
    {
        Debug.Log("LEVEL_TWO_CONVERSATION_PLAYMODE_OK: six one-time paid dialogues, pointer choices, Day 6 three-step finale, daily door payment, free exit/re-entry, final-day lockdown.");
        Complete("PASS");
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL_TWO_CONVERSATION_PLAYMODE_FAILED: " + message);
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
