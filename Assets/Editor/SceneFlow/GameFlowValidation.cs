using System;
using System.Linq;
using DefenderOfIndependence.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameFlowValidation
{
    private static readonly string[] PauseScenePaths =
    {
        "Assets/Scene/Cutscene_Level1.unity", "Assets/Scene/Cutscene_Level2.unity", "Assets/Scene/Cutscene_Level3.unity",
        "Assets/Scene/Scene_Level1.unity", "Assets/Scene/Scene_Level2.unity", "Assets/Scene/Scene_Level3.unity",
        "Assets/Scene/Scene_Credits.unity", "Assets/Scene/Scene_Gallery.unity"
    };

    [MenuItem("Project Tools/Game Flow/Validate Authored Scenes")]
    public static void Run()
    {
        bool previous = GameProgressionState.IsPostGame;
        GameProgressionState.ResetToInitialState();
        if (GameProgressionState.IsPostGame) throw new InvalidOperationException("Reset did not restore initial state.");
        GameProgressionState.UnlockPostGame();
        if (!GameProgressionState.IsPostGame) throw new InvalidOperationException("Post-game state did not persist.");
        if (previous) GameProgressionState.UnlockPostGame(); else GameProgressionState.ResetToInitialState();

        Scene menuScene = EditorSceneManager.OpenScene("Assets/Scene/Scene_MainMenu.unity", OpenSceneMode.Single);
        MainMenuProgressionController menu = FindOne<MainMenuProgressionController>(menuScene);
        GalleryQuizController quiz = FindOne<GalleryQuizController>(menuScene);
        if (quiz.QuestionCount != 16) throw new InvalidOperationException("Main-menu Gallery challenge must contain 16 questions.");
        SerializedObject menuData = new(menu);
        RequireReference(menuData, "startButton"); RequireReference(menuData, "galleryButton"); RequireReference(menuData, "resetButton");
        RequireReference(menuData, "levelSelectionPanel"); RequireReference(menuData, "galleryQuiz"); RequireReference(menuData, "fadeOverlay");

        foreach (string scenePath in PauseScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PauseMenuController pause = FindOne<PauseMenuController>(scene);
            SerializedObject data = new(pause);
            RequireReference(data, "panel"); RequireReference(data, "resumeButton"); RequireReference(data, "optionsButton");
            RequireReference(data, "backToMenuButton");
            if (!scenePath.EndsWith("Scene_Gallery.unity", StringComparison.Ordinal)) RequireReference(data, "skipButton");
            if (scenePath.EndsWith("Scene_Credits.unity", StringComparison.Ordinal)) RequireReference(data, "creditVideo");
            if (scenePath.EndsWith("Scene_Level3.unity", StringComparison.Ordinal)) RequireReference(data, "levelThreeCreditTransition");
        }
        Debug.Log("GAME_FLOW_AUTHORED_VALIDATION_OK: progression storage, main-menu references, 16-question challenge, and all eight scene-aware pause menus passed.");
    }

    public static void RunFromCommandLine()
    {
        try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static T FindOne<T>(Scene scene) where T : Component
    {
        T[] matches = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"{scene.name}: expected one {typeof(T).Name}, found {matches.Length}.");
        return matches[0];
    }

    private static void RequireReference(SerializedObject target, string name)
    {
        SerializedProperty property = target.FindProperty(name);
        if (property == null || property.objectReferenceValue == null)
            throw new InvalidOperationException($"{target.targetObject.name}: missing required '{name}' reference.");
    }
}
