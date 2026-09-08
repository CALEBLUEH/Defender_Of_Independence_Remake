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
    private static LevelTwoConfirmationPanel _confirmation;
    private static LevelTwoNegotiationMeterController _meters;
    private static int _expectedDay;
    private static bool _conversationTested;
    private static bool _conversationStarted;
    private static int _choicesMade;
    private static int _expectedConversationSteps;
    private static double _nextDialogueInput;
    private static int _confirmationStage;
    private static bool _confirmationActionRan;
    private static int _doorStage;
    private static bool _preparedForFinalDay;
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
        _confirmation = UnityEngine.Object.FindAnyObjectByType<LevelTwoConfirmationPanel>();
        _meters = UnityEngine.Object.FindAnyObjectByType<LevelTwoNegotiationMeterController>();
        if (_day == null || _viewer == null || _triggers.Length != 6 || _doors.Length != 3 ||
            _interactor == null || _player == null || _fader == null || _confirmation == null || _meters == null)
        {
            Fail("The Level 2 dialogue or door components did not load completely.");
            return;
        }

        if (_meters.BritishConfidence != 30 || _meters.DelegationUnity != 30 || _meters.PublicSupport != 30)
        {
            Fail("Negotiation meters did not initialize at 30.");
            return;
        }
        _meters.ApplyConversationResult(new LevelTwoNegotiationMeterController.MeterChange
            { britishConfidence = 35, delegationUnity = 35, publicSupport = 35 });
        if (_meters.HasPassed)
        {
            Fail("A score of exactly 65 incorrectly passed the strict final requirement.");
            return;
        }
        _meters.ResetMeters();
        _meters.ApplyConversationResult(new LevelTwoNegotiationMeterController.MeterChange
            { britishConfidence = 36, delegationUnity = 36, publicSupport = 36 });
        if (!_meters.HasPassed)
        {
            Fail("A score of 66 in every category did not pass the final requirement.");
            return;
        }
        _meters.ResetMeters();

        EditorApplication.update -= BeginWhenReady;
        _expectedDay = 1;
        _conversationTested = false;
        _conversationStarted = false;
        _doorStage = 0;
        _confirmationStage = 0;
        _preparedForFinalDay = false;
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

        if (_confirmationStage < 2)
        {
            TestConfirmationPanel();
            return;
        }
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

        if (_expectedDay == 5 && !_preparedForFinalDay)
        {
            _meters.ApplyConversationResult(new LevelTwoNegotiationMeterController.MeterChange
                { britishConfidence = 100, delegationUnity = 100, publicSupport = 100 });
            _preparedForFinalDay = true;
        }
        if (!_day.TryAdvanceDay())
        {
            Fail($"Could not advance from Day {_expectedDay}.");
            return;
        }

        _expectedDay++;
        _conversationTested = false;
        _conversationStarted = false;
    }

    private static void TestConfirmationPanel()
    {
        if (_confirmationStage == 0)
        {
            _confirmationActionRan = false;
            if (!_confirmation.Show("TEST CONFIRMATION", "Confirm this interaction.", "WARNING TEST", () =>
                {
                    _confirmationActionRan = true;
                    return false;
                }) || !_confirmation.IsOpen || !_player.UiCursorActive || _player.ControlsEnabled)
            {
                Fail("Confirmation panel did not open with gameplay paused and the pointer enabled.");
                return;
            }

            _confirmation.Confirm();
            _confirmationStage = 1;
            return;
        }

        if (_confirmation.IsOpen) return;
        if (!_confirmationActionRan || !_player.ControlsEnabled || _player.UiCursorActive)
        {
            Fail("Confirmation panel did not run its confirmed action and restore gameplay.");
            return;
        }

        _confirmationStage = 2;
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
        if (!_conversationStarted)
        {
            int energyBefore = _day.CurrentEnergy;
            if (!trigger.TryBegin(_viewer, _day) || !_viewer.IsOpen || _player.ControlsEnabled ||
                _day.CurrentEnergy != energyBefore - 1 || _player.UiCursorActive)
            {
                Fail($"Day {_expectedDay} conversation did not open as a keyboard-driven Fungus dialogue with a 1-Energy cost.");
                return;
            }

            _expectedConversationSteps = _expectedDay == 6 ? 3 : 1;
            _choicesMade = 0;
            _conversationStarted = true;
            _nextDialogueInput = EditorApplication.timeSinceStartup + 0.08d;
            return;
        }

        if (_viewer.IsOpen)
        {
            if (_viewer.IsWaitingForChoice)
            {
                if (!_player.UiCursorActive || _viewer.CurrentStepIndex != _choicesMade)
                {
                    Fail($"Day {_expectedDay} did not enable the pointer for its horizontal response choices.");
                    return;
                }

                if (!ValidateChoiceLayout(_viewer))
                {
                    Fail($"Day {_expectedDay} choices were not a centered, valid randomized mapping.");
                    return;
                }

                _viewer.SelectChoice(0);
                _choicesMade++;
                _nextDialogueInput = EditorApplication.timeSinceStartup + 0.08d;
                return;
            }

            if (EditorApplication.timeSinceStartup >= _nextDialogueInput)
            {
                _viewer.Continue();
                _nextDialogueInput = EditorApplication.timeSinceStartup + 0.08d;
            }
            return;
        }

        if (_expectedDay == 6)
        {
            if (!_meters.IsFinalResultInteractive) return;
            if (_player.ControlsEnabled || !_player.UiCursorActive || !_meters.IsShowingFinalResult ||
                !trigger.WasConsumed || _choicesMade != _expectedConversationSteps)
            {
                Fail("Day 6 did not fade into an interactive final result while gameplay remained locked.");
                return;
            }
            _conversationTested = true;
            return;
        }

        if (!_player.ControlsEnabled) return;
        if (_choicesMade != _expectedConversationSteps || _player.UiCursorActive || !trigger.WasConsumed ||
            trigger.TryBegin(_viewer, _day) || !trigger.GetPrompt(_day).StartsWith("NO CONVERSATION"))
        {
            Fail($"Day {_expectedDay} conversation did not close and remain consumed.");
            return;
        }

        _conversationTested = true;
    }

    private static bool ValidateChoiceLayout(LevelTwoConversationViewer viewer)
    {
        int count = viewer.DisplayedChoiceCount;
        if (count < 2 || count > 3) return false;
        int[] mapping = Enumerable.Range(0, count).Select(viewer.GetDisplayedChoiceSourceIndex).ToArray();
        if (mapping.Any(index => index < 0 || index >= count) || mapping.Distinct().Count() != count) return false;
        float first = viewer.GetDisplayedChoicePositionX(0);
        float last = viewer.GetDisplayedChoicePositionX(count - 1);
        return !float.IsNaN(first) && !float.IsNaN(last) && Mathf.Abs(first + last) < 0.01f;
    }

    private static void Pass()
    {
        Debug.Log("LEVEL_TWO_CONVERSATION_PLAYMODE_OK: meters start at 30, strict >65 pass rule, randomized centered choices including Day 6 two-choice row, scoring, final fade/result lock, confirmations, camera restore, door energy, final-day lockdown.");
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
