using System;
using System.Linq;
using DefenderOfIndependence.Audio;
using DefenderOfIndependence.Level1;
using DefenderOfIndependence.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameAudioPlayModeSmoke
{
    private const string ActiveKey = "Defender.AudioSmoke.Active";
    private const string ResultKey = "Defender.AudioSmoke.Result";
    private static int stage;
    private static double deadline;
    private static double stageStarted;
    private static double levelThreeFailureDetected;

    static GameAudioPlayModeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Audio/Validate in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scene/Scene_MainMenu.unity", OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true); SessionState.SetString(ResultKey, string.Empty);
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
        stage = 0; stageStarted = EditorApplication.timeSinceStartup; levelThreeFailureDetected = 0d; deadline = stageStarted + 60d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline) { Fail("Timed out at stage " + stage + " in " + SceneManager.GetActiveScene().name); return; }
        switch (stage)
        {
            case 0: ValidateMainMenu(); break;
            case 1: ValidateSilentCutscene(); break;
            case 2: ValidateLevelOneMusicAndPause(); break;
            case 3: ValidateLevelOneFailure(); break;
            case 4: ValidateLevelThreeDelayedMusic(); break;
            case 5: ValidateLevelThreeDelayedLoss(); break;
            case 6: ValidateGalleryAndPersistence(); break;
        }
    }

    private static void ValidateMainMenu()
    {
        if (SceneManager.GetActiveScene().name != "Scene_MainMenu") return;
        GameAudioService audio = GameAudioService.Instance;
        if (audio == null || audio.Library == null || audio.CurrentMusic != audio.Library.GetAutomaticSceneMusic("Scene_MainMenu")) return;
        Button option = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Option");
        option?.onClick.Invoke();
        AudioOptionsPanelController panel = UnityEngine.Object.FindFirstObjectByType<AudioOptionsPanelController>(FindObjectsInactive.Include);
        if (panel == null || !panel.IsOpen) { Fail("Main-menu Options did not open the audio sliders."); return; }
        Slider effects = FindSlider("Sound Effects Slider"); Slider music = FindSlider("Music Slider");
        effects.value = 0.31f; music.value = 0.42f;
        if (!Mathf.Approximately(audio.EffectsVolume, 0.31f) || !Mathf.Approximately(audio.MusicVolume, 0.42f))
        { Fail("Audio sliders did not update separate persisted volumes."); return; }
        panel.Close();
        SceneManager.LoadScene("Cutscene_Level1"); stage = 1; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateSilentCutscene()
    {
        if (SceneManager.GetActiveScene().name != "Cutscene_Level1") return;
        GameAudioService audio = GameAudioService.Instance;
        if (audio == null) return;
        if (audio.CurrentMusic != null) { Fail("Cutscene Level 1 incorrectly started background music."); return; }
        audio.PlayNextDialogue();
        if (!audio.IsEffectPlaying) { Fail("Next-dialogue effect did not play."); return; }
        SceneManager.LoadScene("Scene_Level1"); stage = 2; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateLevelOneMusicAndPause()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Level1") return;
        GameAudioService audio = GameAudioService.Instance;
        if (audio == null || audio.CurrentMusic != audio.Library.GetAutomaticSceneMusic("Scene_Level1")) return;
        PauseMenuController pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();
        PlayerMovementLoopAudio walking = UnityEngine.Object.FindFirstObjectByType<PlayerMovementLoopAudio>();
        if (pause == null || walking == null) { Fail("Level 1 pause or grounded walking audio is missing."); return; }
        pause.Pause();
        if (!audio.IsMusicPaused) { Fail("Opening the Escape menu did not pause music."); return; }
        pause.Resume();
        if (audio.IsMusicPaused) { Fail("Resume did not continue the paused music source."); return; }
        CombatHealth health = UnityEngine.Object.FindObjectsByType<CombatHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(x => x.Team == CombatTeam.Player);
        if (health == null) { Fail("Level 1 player health is missing."); return; }
        health.ApplyDamage(10000f, health.transform.position, Vector3.forward);
        stage = 3; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateLevelOneFailure()
    {
        LevelOneGameOverController gameOver = UnityEngine.Object.FindFirstObjectByType<LevelOneGameOverController>();
        GameAudioService audio = GameAudioService.Instance;
        if (gameOver == null || audio == null) { Fail("Level 1 failure controllers are missing."); return; }
        if (audio.CurrentMusic != audio.Library.LoseMusic) { Fail("Level 1 failure did not switch to looping lose music."); return; }
        if (EditorApplication.timeSinceStartup - stageStarted < 1.8d) return;
        if (!gameOver.GameOverShown || gameOver.LoseFadeAlpha > 0.05f)
        { Fail("Level 1 lose panel did not complete its black fade transition."); return; }
        SceneManager.LoadScene("Scene_Level3"); stage = 4; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateLevelThreeDelayedMusic()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Level3") return;
        GameAudioService audio = GameAudioService.Instance;
        if (audio == null) return;
        if (audio.CurrentMusic != null) { Fail("Level 3 music started before the tutorial."); return; }
        LevelThreeOpeningSequence opening = UnityEngine.Object.FindFirstObjectByType<LevelThreeOpeningSequence>();
        AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (opening == null || listeners.Length != 1 || opening.GetComponent<AudioListener>() != listeners[0] || !listeners[0].isActiveAndEnabled)
        { Fail("Level 3 does not have exactly one active listener on its opening sequence."); return; }
        if (opening.ActiveShot != 1 || !audio.IsEffectPlaying)
        { Fail("The non-looping cheering effect did not begin during Camera 1."); return; }
        LevelThreeTutorialPanel tutorial = UnityEngine.Object.FindFirstObjectByType<LevelThreeTutorialPanel>();
        if (tutorial == null) { Fail("Level 3 tutorial is missing."); return; }
        tutorial.ShowTutorial();
        if (audio.CurrentMusic != audio.Library.LevelThreeMusic) { Fail("Level 3 tutorial did not start Death By Glamour."); return; }
        audio.PlayKeyboardTap();
        if (!audio.IsEffectPlaying) { Fail("Keyboard tap effect did not play."); return; }
        LevelThreeTypingGameplay gameplay = UnityEngine.Object.FindFirstObjectByType<LevelThreeTypingGameplay>();
        LevelThreeHealthDisplay health = UnityEngine.Object.FindFirstObjectByType<LevelThreeHealthDisplay>();
        if (gameplay == null || health == null) { Fail("Level 3 gameplay or health display is missing."); return; }
        gameplay.SetTiming(0.1f, 0.1f, 10f, 10f);
        gameplay.BeginGameplay();
        health.LoseHealth(2);
        stage = 5; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateLevelThreeDelayedLoss()
    {
        LevelThreeTypingGameplay gameplay = UnityEngine.Object.FindFirstObjectByType<LevelThreeTypingGameplay>();
        GameAudioService audio = GameAudioService.Instance;
        if (gameplay == null || audio == null) { Fail("Level 3 delayed-loss dependencies are missing."); return; }
        if (!gameplay.IsFailed) return;

        if (levelThreeFailureDetected <= 0d)
        {
            levelThreeFailureDetected = EditorApplication.timeSinceStartup;
            if (audio.CurrentMusic != null || !gameplay.IsLossMusicDelayPending)
            { Fail("Level 3 did not begin its one-second silent loss delay."); return; }
        }

        double elapsed = EditorApplication.timeSinceStartup - levelThreeFailureDetected;
        if (elapsed < 0.8d)
        {
            if (audio.CurrentMusic != null) Fail("Level 3 loss music started before the one-second delay elapsed.");
            return;
        }
        if (elapsed < 1.15d) return;
        if (audio.CurrentMusic != audio.Library.LoseMusic || !audio.IsMusicPlaying)
        { Fail("Level 3 loss music did not start and loop after the silent delay."); return; }
        SceneManager.LoadScene("Scene_Gallery"); stage = 6; stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void ValidateGalleryAndPersistence()
    {
        if (SceneManager.GetActiveScene().name != "Scene_Gallery") return;
        GameAudioService audio = GameAudioService.Instance;
        if (audio == null || audio.CurrentMusic != audio.Library.GetAutomaticSceneMusic("Scene_Gallery")) return;
        if (audio.IsEffectPlaying) { Fail("A Level 3 one-shot effect leaked into the Gallery scene."); return; }
        if (!Mathf.Approximately(audio.EffectsVolume, 0.31f) || !Mathf.Approximately(audio.MusicVolume, 0.42f))
        { Fail("Separate volume settings did not survive scene changes."); return; }
        audio.SetEffectsVolume(0.9f); audio.SetMusicVolume(0.8f);
        Debug.Log("GAME_AUDIO_PLAYMODE_OK: scene music, silent cutscene, dialogue effect, separate persistent sliders, pause/resume, walking emitter, Level 3 listener/Camera 1 cheer/one-second delayed loss loop, delayed Level 3 gameplay music, typing effect, and Gallery music passed.");
        Complete("PASS");
    }

    private static Slider FindSlider(string name)
        => UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == name);

    private static void Fail(string message) { Debug.LogError("GAME_AUDIO_PLAYMODE_FAILED: " + message); Complete("FAIL"); }
    private static void Complete(string result)
    {
        EditorApplication.update -= Tick; SessionState.SetString(ResultKey, result); EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
        string result = SessionState.GetString(ResultKey, string.Empty); if (!string.IsNullOrEmpty(result)) Finish(result == "PASS");
    }

    private static void Finish(bool passed)
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey); SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        if (!passed) throw new InvalidOperationException("Game audio Play Mode smoke test failed.");
    }
}
