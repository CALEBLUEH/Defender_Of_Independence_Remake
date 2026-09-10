using System;
using System.Linq;
using DefenderOfIndependence.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameFlowPlayModeSmoke
{
    private const string ActiveKey = "Defender.GameFlowSmoke.Active";
    private const string ResultKey = "Defender.GameFlowSmoke.Result";
    private const string PreviousStateKey = "Defender.GameFlowSmoke.PreviousPostGame";
    private static int stage;
    private static double deadline;

    static GameFlowPlayModeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Game Flow/Validate in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(PreviousStateKey, GameProgressionState.IsPostGame);
        GameProgressionState.ResetToInitialState();
        EditorSceneManager.OpenScene("Assets/Scene/Scene_MainMenu.unity", OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = true;
    }

    public static void RunFromCommandLine()
    {
        try { Run(); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static void ResumeAfterReload()
    {
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying) { Finish(result == "PASS"); return; }
        if (!EditorApplication.isPlaying) return;
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 75d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline) { Fail("Timed out at stage " + stage + " in " + SceneManager.GetActiveScene().name); return; }
        switch (stage)
        {
            case 0: ValidateInitialGalleryChallenge(); break;
            case 1: ValidatePostGameMainMenu(); break;
            case 2: SkipScene("Cutscene_Level1", "Scene_Level1"); break;
            case 3: SkipScene("Scene_Level1", "Cutscene_Level2"); break;
            case 4: SkipScene("Cutscene_Level2", "Scene_Level2"); break;
            case 5: SkipScene("Scene_Level2", "Cutscene_Level3"); break;
            case 6: SkipScene("Cutscene_Level3", "Scene_Level3"); break;
            case 7: SkipLevelThree(); break;
            case 8: SkipCredits(); break;
            case 9: ValidateGalleryArrival(); break;
        }
    }

    private static void ValidateInitialGalleryChallenge()
    {
        if (SceneManager.GetActiveScene().name != "Scene_MainMenu") return;
        MainMenuProgressionController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuProgressionController>();
        GalleryQuizController quiz = UnityEngine.Object.FindFirstObjectByType<GalleryQuizController>();
        if (menu == null || quiz == null) { Fail("Main menu progression or quiz controller is missing."); return; }
        FindButton("Gallery").onClick.Invoke();
        FindButton("Continue").onClick.Invoke();
        if (!quiz.IsOpen || quiz.QuestionCount != 16) { Fail("Initial Gallery challenge did not open the 16-question quiz."); return; }
        for (int i = 0; i < quiz.QuestionCount; i++)
        {
            int slot = quiz.CurrentCorrectDisplaySlot;
            if (slot < 0) { Fail("Could not locate the shuffled correct answer at question " + (i + 1)); return; }
            quiz.SelectAnswer(slot);
        }
        quiz.ConfirmResult();
        if (!GameProgressionState.IsPostGame) { Fail("A perfect Gallery challenge did not unlock post-game state."); return; }
        stage = 1;
    }

    private static void ValidatePostGameMainMenu()
    {
        MainMenuProgressionController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuProgressionController>();
        if (menu == null || !menu.IsPostGame) { Fail("Post-game state was not visible to the main menu."); return; }
        FindButton("Start").onClick.Invoke();
        CanvasGroup selection = FindCanvasGroup("Level Selection");
        if (selection == null || !selection.interactable || selection.alpha < 0.99f) { Fail("Post-game START did not open level selection."); return; }
        FindButton("Level 1").onClick.Invoke();
        stage = 2;
    }

    private static void SkipScene(string current, string expected)
    {
        if (SceneManager.GetActiveScene().name != current) return;
        PauseMenuController pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
        if (pause == null) { Fail(current + " has no Escape menu."); return; }
        float timeScaleBeforePause = Time.timeScale;
        pause.Pause();
        if (!pause.IsPaused || Time.timeScale != 0f || !Cursor.visible) { Fail(current + " did not pause correctly."); return; }
        pause.Resume();
        if (pause.IsPaused || !Mathf.Approximately(Time.timeScale, timeScaleBeforePause))
        { Fail(current + " did not restore its pre-pause time scale."); return; }
        pause.Pause();
        pause.SkipCurrentContent();
        stage++;
    }

    private static void SkipLevelThree()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Level3") return;
        PauseMenuController pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
        if (pause == null) { Fail("Level 3 has no Escape menu."); return; }
        pause.Pause(); pause.SkipCurrentContent(); stage = 8;
    }

    private static void SkipCredits()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Credits") return;
        if (!GameProgressionState.IsPostGame) { Fail("Finishing/skipping Level 3 did not save post-game state."); return; }
        PauseMenuController pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
        CreditVideoController credits = UnityEngine.Object.FindFirstObjectByType<CreditVideoController>();
        if (pause == null || credits == null) { Fail("Credits skip controls are missing."); return; }
        pause.Pause(); pause.SkipCurrentContent(); stage = 9;
    }

    private static void ValidateGalleryArrival()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Gallery") return;
        if (UnityEngine.Object.FindFirstObjectByType<PauseMenuController>() == null) { Fail("Gallery base Escape menu is missing."); return; }
        Debug.Log("GAME_FLOW_PLAYMODE_OK: perfect-quiz unlock, post-game level select, pause/resume, all cutscene and level skips, Level 3 unlock, credit skip, and Gallery arrival passed.");
        Complete("PASS");
    }

    private static Button FindButton(string name)
    {
        Button[] matches = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.name == name).ToArray();
        if (matches.Length == 0) throw new InvalidOperationException("Missing button: " + name);
        return matches[0];
    }

    private static CanvasGroup FindCanvasGroup(string name)
        => UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == name);

    private static void Fail(string message) { Debug.LogError("GAME_FLOW_PLAYMODE_FAILED: " + message); Complete("FAIL"); }
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
        if (SessionState.GetBool(PreviousStateKey, false)) GameProgressionState.UnlockPostGame();
        else GameProgressionState.ResetToInitialState();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey); SessionState.EraseBool(PreviousStateKey); SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        if (!passed) throw new InvalidOperationException("Game flow Play Mode smoke test failed.");
    }
}
