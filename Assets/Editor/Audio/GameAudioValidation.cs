using System;
using System.Linq;
using DefenderOfIndependence.Audio;
using DefenderOfIndependence.Level1;
using DefenderOfIndependence.SceneFlow;
using Fungus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameAudioValidation
{
    private const string LibraryPath = "Assets/Audio/Settings/GameAudioLibrary.asset";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scene/Scene_MainMenu.unity", "Assets/Scene/Cutscene_Level1.unity", "Assets/Scene/Scene_Level1.unity",
        "Assets/Scene/Cutscene_Level2.unity", "Assets/Scene/Scene_Level2.unity", "Assets/Scene/Cutscene_Level3.unity",
        "Assets/Scene/Scene_Level3.unity", "Assets/Scene/Scene_Credits.unity", "Assets/Scene/Scene_Gallery.unity"
    };

    [MenuItem("Project Tools/Audio/Validate Authored Audio")]
    public static void Run()
    {
        GameAudioLibrary library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath)
            ?? throw new InvalidOperationException("Audio library is missing.");
        SerializedObject libraryData = new(library);
        string[] clipFields = { "buttonClick", "nextDialogue", "walking", "loseMusic", "keyboardTap", "cheering", "merdeka", "gunshot",
            "mainMenuMusic", "levelOneMusic", "levelTwoMusic", "levelThreeMusic", "galleryMusic" };
        foreach (string field in clipFields)
        {
            AudioClip clip = libraryData.FindProperty(field).objectReferenceValue as AudioClip;
            if (clip == null || clip.length <= 0f) throw new InvalidOperationException("Invalid audio library clip: " + field);
        }

        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (FindAll<GameAudioService>(scene).Count() != 1) throw new InvalidOperationException(scene.name + " requires one GameAudioService.");
            if (FindAll<AudioOptionsPanelController>(scene).Count() != 1) throw new InvalidOperationException(scene.name + " requires one Audio Options panel.");
            if (scene.name == "Scene_MainMenu") RequireReference(FindOne<MainMenuProgressionController>(scene), "audioOptionsPanel");
            else RequireReference(FindOne<PauseMenuController>(scene), "audioOptionsPanel");

            if (scene.name.StartsWith("Cutscene_Level", StringComparison.Ordinal))
            {
                Writer[] writers = FindAll<Writer>(scene).ToArray();
                if (writers.Length == 0 || writers.Any(x => x.GetComponent<FungusDialogueAudioListener>() == null))
                    throw new InvalidOperationException(scene.name + ": a Fungus Writer is missing its dialogue audio listener.");
            }
            if (scene.name is "Scene_Level1" or "Scene_Level2" or "Scene_Gallery")
            {
                PlayerMovementLoopAudio movement = FindOne<PlayerMovementLoopAudio>(scene);
                RequireReference(movement, "walkingClip"); RequireReference(movement, "audioSource");
            }
            if (scene.name == "Scene_Level1") RequireReference(FindOne<LevelOneGameOverController>(scene), "loseFadeOverlay");
            if (scene.name == "Scene_Level3")
            {
                LevelThreeTypingGameplay gameplay = FindOne<LevelThreeTypingGameplay>(scene);
                RequireReference(gameplay, "failureFadeOverlay");
                if (!Mathf.Approximately(gameplay.LoseMusicDelay, 1f))
                    throw new InvalidOperationException("Level 3 requires a one-second loss-music delay.");

                LevelThreeOpeningSequence opening = FindOne<LevelThreeOpeningSequence>(scene);
                AudioListener[] listeners = FindAll<AudioListener>(scene).ToArray();
                if (listeners.Length != 1 || opening.GetComponent<AudioListener>() != listeners[0])
                    throw new InvalidOperationException("Level 3 requires one stable AudioListener on its opening-sequence object.");
            }
        }
        Debug.Log("GAME_AUDIO_AUTHORED_VALIDATION_OK: 13 clips, 9 scene services/options panels, Level 3 listener/delayed loss, 3 walking emitters, cutscene dialogue listeners, and failure fades passed.");
    }

    public static void RunFromCommandLine()
    {
        try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static T FindOne<T>(Scene scene) where T : Component
    {
        T[] matches = FindAll<T>(scene).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"{scene.name}: expected one {typeof(T).Name}, found {matches.Length}.");
        return matches[0];
    }

    private static System.Collections.Generic.IEnumerable<T> FindAll<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true));

    private static void RequireReference(UnityEngine.Object target, string field)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(field);
        if (property == null || property.objectReferenceValue == null)
            throw new InvalidOperationException(target.name + ": missing " + field);
    }
}
