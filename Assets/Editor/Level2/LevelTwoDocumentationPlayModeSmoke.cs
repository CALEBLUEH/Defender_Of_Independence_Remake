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
    private static LevelTwoDayController _day;
    private static LevelTwoDocumentViewer _viewer;
    private static LevelTwoFirstPersonController _player;
    private static LevelTwoNegotiationMeterController _meters;
    private static LevelTwoDocumentLocation _lobby;
    private static LevelTwoDocumentLocation[] _paidLocations;
    private static int _phase;
    private static int _lobbyOpening;
    private static int _paidLocationIndex;
    private static int _paidPage;
    private static int _expectedEnergy;
    private static int _bonusBefore;

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

        _day = UnityEngine.Object.FindAnyObjectByType<LevelTwoDayController>();
        if (_day == null || _day.IsTransitioning) return;

        _viewer = UnityEngine.Object.FindAnyObjectByType<LevelTwoDocumentViewer>();
        _player = UnityEngine.Object.FindAnyObjectByType<LevelTwoFirstPersonController>();
        _meters = UnityEngine.Object.FindAnyObjectByType<LevelTwoNegotiationMeterController>();
        LevelTwoDocumentLocation[] locations = UnityEngine.Object.FindObjectsByType<LevelTwoDocumentLocation>(FindObjectsInactive.Include);
        if (_viewer == null || _player == null || _meters == null || locations.Length != 4)
        {
            Fail("The scene did not load one viewer, one player, and four document locations.");
            return;
        }

        if (_lobby == null)
        {
            _lobby = locations.Single(item => item.IsReusable);
            _paidLocations = locations.Where(item => !item.IsReusable).OrderBy(item => item.name).ToArray();
            _meters.ResetMeters();
            _day.ResetToStartingDay();
            _phase = 0;
        }

        if (_phase == 0)
        {
            int lobbyEnergy = _day.CurrentEnergy;
            if (!_lobby.TryExamine(_viewer, _day) || _day.CurrentEnergy != lobbyEnergy || _lobby.IsExhausted ||
                !_viewer.IsOpen || !_player.UiCursorActive || _player.ControlsEnabled)
            {
                Fail("The Main Lobby guide was not reusable, free, or pointer-enabled.");
                return;
            }
            _viewer.Close();
            _phase = 1;
            return;
        }

        if (_phase == 1)
        {
            if (_viewer.IsOpen) return;
            if (!_player.ControlsEnabled || _player.UiCursorActive)
            {
                Fail("The document slide-down did not restore gameplay and cursor locking.");
                return;
            }

            _lobbyOpening++;
            if (_lobbyOpening < 3) { _phase = 0; return; }
            for (int i = 0; i < 4; i++)
            {
                if (!_day.TrySpendEnergy(1)) { Fail("Energy could not be spent four times."); return; }
            }
            if (_day.CurrentEnergy != 0 || !_paidLocations[0].GetPrompt(_day).StartsWith("NO ENERGY"))
            {
                Fail("The zero-energy state or prompt is incorrect.");
                return;
            }
            _paidLocationIndex = 0;
            _paidPage = 0;
            _phase = 2;
            return;
        }

        if (_phase == 2)
        {
            if (_paidLocationIndex >= _paidLocations.Length) { Pass(); return; }
            if (_paidPage == 0) _day.ResetToStartingDay();
            LevelTwoDocumentLocation location = _paidLocations[_paidLocationIndex];
            _bonusBefore = GetBonus(location.DocumentBonusMeter);
            _expectedEnergy = _day.CurrentEnergy - 1;
            if (!location.TryExamine(_viewer, _day) || !_viewer.IsOpen || _player.ControlsEnabled ||
                location.RemainingCount != 2 - _paidPage || _day.CurrentEnergy != _expectedEnergy)
            {
                Fail($"{location.name} failed while opening document {_paidPage + 1}.");
                return;
            }
            _viewer.Close();
            _phase = 3;
            return;
        }

        if (_phase == 3)
        {
            if (_viewer.IsOpen) return;
            if (!_player.ControlsEnabled || _player.UiCursorActive)
            {
                Fail($"{_paidLocations[_paidLocationIndex].name} did not restore gameplay after its slide-down.");
                return;
            }
            LevelTwoDocumentLocation current = _paidLocations[_paidLocationIndex];
            if (GetBonus(current.DocumentBonusMeter) != _bonusBefore + current.DocumentBonusAmount)
            {
                Fail($"{current.name} did not grant its +{current.DocumentBonusAmount} document bonus after closing.");
                return;
            }

            _paidPage++;
            if (_paidPage < 3) { _phase = 2; return; }
            LevelTwoDocumentLocation completed = _paidLocations[_paidLocationIndex];
            int exhaustedEnergy = _day.CurrentEnergy;
            if (!completed.IsExhausted || completed.TryExamine(_viewer, _day) || _day.CurrentEnergy != exhaustedEnergy ||
                completed.GetPrompt(_day) != "NO DOCUMENTS REMAIN HERE")
            {
                Fail($"{completed.name} did not stay exhausted after all three pages.");
                return;
            }
            _paidLocationIndex++;
            _paidPage = 0;
            _phase = 2;
        }
    }

    private static int GetBonus(LevelTwoNegotiationMeterController.MeterType meter)
    {
        return meter switch
        {
            LevelTwoNegotiationMeterController.MeterType.BritishConfidence => _meters.BritishConfidenceBonus,
            LevelTwoNegotiationMeterController.MeterType.DelegationUnity => _meters.DelegationUnityBonus,
            LevelTwoNegotiationMeterController.MeterType.PublicSupport => _meters.PublicSupportBonus,
            _ => 0
        };
    }

    private static void Pass()
    {
        if (_meters.BritishConfidenceBonus != 15 || _meters.DelegationUnityBonus != 15 ||
            _meters.PublicSupportBonus != 15)
        {
            Fail("Reading all nine historical documents did not stack three +5 bonuses per meter.");
            return;
        }
        Debug.Log("LEVEL_TWO_DOCUMENTATION_PLAYMODE_OK: free reusable lobby guide, nine one-time historical pages, three stackable +5 preparation bonuses per meter, pointer/scroll UI, rise-up/slide-down lifecycle, energy reset, exhausted prompts.");
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
        _day = null;
        _viewer = null;
        _player = null;
        _meters = null;
        _lobby = null;
        _paidLocations = null;
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
