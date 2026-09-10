using System;
using System.Collections.Generic;
using System.Linq;
using DefenderOfIndependence.SceneFlow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameFlowSetup
{
    private const string MainMenuPath = "Assets/Scene/Scene_MainMenu.unity";
    private const string HeadingFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
    private const string BodyFontPath = "Assets/Font/Spheris/Spheris-Regular SDF.asset";
    private const string BoldFontPath = "Assets/Font/Spheris/Spheris-Bold SDF.asset";

    private static readonly PauseSceneData[] PauseScenes =
    {
        new("Assets/Scene/Cutscene_Level1.unity", PauseSceneKind.Cutscene, "Scene_Level1", false),
        new("Assets/Scene/Cutscene_Level2.unity", PauseSceneKind.Cutscene, "Scene_Level2", false),
        new("Assets/Scene/Cutscene_Level3.unity", PauseSceneKind.Cutscene, "Scene_Level3", false),
        new("Assets/Scene/Scene_Level1.unity", PauseSceneKind.Gameplay, "Cutscene_Level2", true),
        new("Assets/Scene/Scene_Level2.unity", PauseSceneKind.Gameplay, "Cutscene_Level3", true),
        new("Assets/Scene/Scene_Level3.unity", PauseSceneKind.Gameplay, string.Empty, false),
        new("Assets/Scene/Scene_Credits.unity", PauseSceneKind.Credits, string.Empty, false),
        new("Assets/Scene/Scene_Gallery.unity", PauseSceneKind.Gallery, string.Empty, true)
    };

    [MenuItem("Project Tools/Game Flow/Build Main Menu and Pause Menus")]
    public static void Run()
    {
        BuildMainMenu();
        foreach (PauseSceneData data in PauseScenes) BuildPauseMenu(data);
        AssetDatabase.SaveAssets();
        Debug.Log("GAME_FLOW_SETUP_OK: initial/post-game menu flow, reset, Gallery unlock quiz, and scene-aware Escape menus are authored.");
    }

    public static void RunFromCommandLine()
    {
        try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        Canvas canvas = FindUnique<Canvas>(scene, "Canvas");
        Button start = FindUnique<Button>(scene, "Start");
        Button gallery = FindUnique<Button>(scene, "Gallery");
        Button option = FindUnique<Button>(scene, "Option");
        Button quit = FindUnique<Button>(scene, "Quit");
        TMP_FontAsset heading = RequireAsset<TMP_FontAsset>(HeadingFontPath);
        TMP_FontAsset body = RequireAsset<TMP_FontAsset>(BodyFontPath);
        TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);

        DestroyNamed(scene, "Main Menu Progression");
        DestroyChild(canvas.transform, "Main Menu Flow UI");
        RemoveComponent<SceneTransitionButton>(start.gameObject);

        GameObject root = new("Main Menu Progression");
        SceneManager.MoveGameObjectToScene(root, scene);
        MainMenuProgressionController controller = root.AddComponent<MainMenuProgressionController>();

        Transform ui = CreateStretch(canvas.transform, "Main Menu Flow UI").transform;
        Button reset = CloneMenuButton(start, canvas.transform, "Reset", "RESET");
        RectTransform resetRect = reset.GetComponent<RectTransform>();
        resetRect.anchorMin = resetRect.anchorMax = resetRect.pivot = new Vector2(1f, 0f);
        resetRect.anchoredPosition = new Vector2(-42f, 34f);

        CanvasGroup levelSelection = CreateOverlay(ui, "Level Selection", new Color(0f, 0f, 0f, 0.78f));
        GameObject levelPanel = CreatePanel(levelSelection.transform, "Level Selection Panel", new Vector2(930f, 620f));
        CreateText(levelPanel.transform, "Title", heading, 38f, "SELECT A CHAPTER", TextAlignmentOptions.Center,
            new Vector2(50f, 500f), new Vector2(-50f, -40f), Gold);
        CreateText(levelPanel.transform, "Subtitle", body, 23f, "Each chapter begins with its story cutscene.", TextAlignmentOptions.Center,
            new Vector2(55f, 440f), new Vector2(-55f, -108f), SoftWhite);
        Button closeLevels = CreateButton(levelPanel.transform, "Close", bold, "X", new Vector2(850f, 535f), new Vector2(54f, 54f));
        Button levelOne = CreateButton(levelPanel.transform, "Level 1", bold, "LEVEL 1\nRESCUE", new Vector2(195f, 205f), new Vector2(240f, 190f));
        Button levelTwo = CreateButton(levelPanel.transform, "Level 2", bold, "LEVEL 2\nNEGOTIATION", new Vector2(465f, 205f), new Vector2(240f, 190f));
        Button levelThree = CreateButton(levelPanel.transform, "Level 3", bold, "LEVEL 3\nDECLARATION", new Vector2(735f, 205f), new Vector2(240f, 190f));

        CanvasGroup message = CreateOverlay(ui, "Message Panel", new Color(0f, 0f, 0f, 0.8f));
        GameObject messagePanel = CreatePanel(message.transform, "Message", new Vector2(960f, 560f));
        TMP_Text messageTitle = CreateText(messagePanel.transform, "Title", heading, 35f, "MESSAGE", TextAlignmentOptions.Center,
            new Vector2(70f, 445f), new Vector2(-70f, -45f), Gold);
        TMP_Text messageBody = CreateText(messagePanel.transform, "Body", body, 25f, string.Empty, TextAlignmentOptions.Center,
            new Vector2(95f, 175f), new Vector2(-95f, -135f), SoftWhite);
        Button continueMessage = CreateButton(messagePanel.transform, "Continue", bold, "CONTINUE", new Vector2(600f, 60f), new Vector2(270f, 70f));
        Button cancelMessage = CreateButton(messagePanel.transform, "Cancel", bold, "CANCEL", new Vector2(320f, 60f), new Vector2(230f, 70f));

        GalleryQuizController quiz = CreateQuiz(ui, root, heading, body, bold);

        CanvasGroup fade = CreateOverlay(ui, "Main Menu Fade", Color.black);
        fade.alpha = 0f; fade.blocksRaycasts = false; fade.interactable = false;
        fade.transform.SetAsLastSibling();

        Assign(controller, "startButton", start); Assign(controller, "galleryButton", gallery);
        Assign(controller, "optionButton", option); Assign(controller, "quitButton", quit); Assign(controller, "resetButton", reset);
        Assign(controller, "levelSelectionPanel", levelSelection); Assign(controller, "levelOneButton", levelOne);
        Assign(controller, "levelTwoButton", levelTwo); Assign(controller, "levelThreeButton", levelThree);
        Assign(controller, "closeLevelSelectionButton", closeLevels); Assign(controller, "messagePanel", message);
        Assign(controller, "messageTitle", messageTitle); Assign(controller, "messageBody", messageBody);
        Assign(controller, "messageContinueButton", continueMessage); Assign(controller, "messageCancelButton", cancelMessage);
        Assign(controller, "galleryQuiz", quiz); Assign(controller, "fadeOverlay", fade);
        EditorUtility.SetDirty(controller);
        EnsureEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GalleryQuizController CreateQuiz(Transform parent, GameObject root, TMP_FontAsset heading, TMP_FontAsset body, TMP_FontAsset bold)
    {
        CanvasGroup overlay = CreateOverlay(parent, "Main Menu Gallery Quiz", new Color(0f, 0f, 0f, 0.88f));
        GameObject panel = CreatePanel(overlay.transform, "Quiz Panel", new Vector2(1320f, 820f));
        CreateText(panel.transform, "Title", heading, 38f, "INDEPENDENCE QUIZ", TextAlignmentOptions.Center,
            new Vector2(80f, 720f), new Vector2(-80f, -30f), Gold);
        TMP_Text progress = CreateText(panel.transform, "Progress", bold, 20f, "QUESTION 1 OF 16", TextAlignmentOptions.Center,
            new Vector2(80f, 665f), new Vector2(-80f, -94f), Cyan);
        TMP_Text question = CreateText(panel.transform, "Question", body, 31f, "Question", TextAlignmentOptions.Center,
            new Vector2(110f, 500f), new Vector2(-110f, -165f), SoftWhite);
        Button quit = CreateButton(panel.transform, "Quit", bold, "X", new Vector2(1238f, 742f), new Vector2(58f, 58f));

        GameObject answersRoot = CreateRect(panel.transform, "Answers 2x2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1080f, 300f));
        answersRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 165f);
        GridLayoutGroup grid = answersRoot.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(520f, 126f); grid.spacing = new Vector2(28f, 28f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2; grid.childAlignment = TextAnchor.MiddleCenter;
        Button[] buttons = new Button[4]; TMP_Text[] labels = new TMP_Text[4];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = CreateButton(answersRoot.transform, $"Answer {i + 1}", body, "Answer", Vector2.zero, grid.cellSize, false);
            labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
        }

        GameObject resultRoot = CreateRect(panel.transform, "Quiz Result", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(900f, 330f));
        resultRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 180f);
        TMP_Text result = CreateText(resultRoot.transform, "Score", heading, 43f, "SCORE\n0 / 16", TextAlignmentOptions.Center,
            new Vector2(20f, 115f), new Vector2(-20f, -5f), SoftWhite);
        Button retry = CreateButton(resultRoot.transform, "Retry", bold, "RETAKE", new Vector2(300f, 52f), new Vector2(260f, 72f));
        Button continueButton = CreateButton(resultRoot.transform, "Continue", bold, "CONTINUE", new Vector2(600f, 52f), new Vector2(260f, 72f));

        GalleryQuizController controller = root.AddComponent<GalleryQuizController>();
        Assign(controller, "panelGroup", overlay); Assign(controller, "progressText", progress); Assign(controller, "questionText", question);
        AssignArray(controller, "answerButtons", buttons.Cast<UnityEngine.Object>().ToArray());
        AssignArray(controller, "answerLabels", labels.Cast<UnityEngine.Object>().ToArray());
        Assign(controller, "answersRoot", answersRoot); Assign(controller, "resultRoot", resultRoot); Assign(controller, "resultText", result);
        Assign(controller, "retryButton", retry); Assign(controller, "continueButton", continueButton); Assign(controller, "quitButton", quit);
        GalleryQuizSetup.ApplyStandardQuestions(controller);
        return controller;
    }

    private static void BuildPauseMenu(PauseSceneData data)
    {
        Scene scene = EditorSceneManager.OpenScene(data.ScenePath, OpenSceneMode.Single);
        DestroyNamed(scene, "Global Pause Menu");
        TMP_FontAsset heading = RequireAsset<TMP_FontAsset>(HeadingFontPath);
        TMP_FontAsset body = RequireAsset<TMP_FontAsset>(BodyFontPath);
        TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);

        GameObject root = new("Global Pause Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(root, scene);
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup overlay = CreateOverlay(root.transform, "Pause Overlay", new Color(0f, 0f, 0f, 0.78f));
        GameObject panel = CreatePanel(overlay.transform, "Pause Panel", new Vector2(700f, data.Kind == PauseSceneKind.Gallery ? 610f : 710f));
        CreateText(panel.transform, "Title", heading, 43f, "PAUSED", TextAlignmentOptions.Center,
            new Vector2(50f, panel.GetComponent<RectTransform>().sizeDelta.y - 105f), new Vector2(-50f, -35f), Gold);

        float top = panel.GetComponent<RectTransform>().sizeDelta.y - 205f;
        Button resume = CreateCenteredButton(panel.transform, "Resume", bold, "RESUME", top);
        Button options = CreateCenteredButton(panel.transform, "Options", bold, "OPTIONS", top - 105f);
        Button back = CreateCenteredButton(panel.transform, "Back To Menu", bold, "BACK TO MAIN MENU", top - 210f);
        Button skip = null;
        if (data.Kind != PauseSceneKind.Gallery)
        {
            string label = data.Kind == PauseSceneKind.Cutscene ? "SKIP CUTSCENE" : data.Kind == PauseSceneKind.Credits ? "SKIP VIDEO" : "SKIP LEVEL";
            skip = CreateCenteredButton(panel.transform, "Skip", bold, label, top - 315f);
        }
        TMP_Text notice = CreateText(panel.transform, "Options Notice", body, 19f,
            "Sound and music options will be added in the next polish pass.", TextAlignmentOptions.Center,
            new Vector2(45f, 15f), new Vector2(-45f, -(panel.GetComponent<RectTransform>().sizeDelta.y - 65f)), Cyan);

        PauseMenuController controller = root.AddComponent<PauseMenuController>();
        Assign(controller, "panel", overlay); Assign(controller, "resumeButton", resume); Assign(controller, "optionsButton", options);
        Assign(controller, "backToMenuButton", back); Assign(controller, "skipButton", skip); Assign(controller, "optionsNotice", notice);
        SetEnum(controller, "sceneKind", (int)data.Kind); SetString(controller, "skipDestinationScene", data.SkipDestination);
        SetBool(controller, "lockCursorOnResume", data.LockCursor);
        AssignArray(controller, "pauseTargets", FindPauseTargets(scene, data.Kind).Cast<UnityEngine.Object>().ToArray());
        if (data.Kind == PauseSceneKind.Credits) Assign(controller, "creditVideo", FindSingleOrNull<CreditVideoController>(scene));
        if (data.ScenePath.EndsWith("Scene_Level3.unity", StringComparison.Ordinal))
            Assign(controller, "levelThreeCreditTransition", FindSingleOrNull<LevelThreeCreditTransition>(scene));
        EditorUtility.SetDirty(controller);
        EnsureEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Behaviour[] FindPauseTargets(Scene scene, PauseSceneKind kind)
    {
        HashSet<string> names = kind switch
        {
            PauseSceneKind.Cutscene => new HashSet<string> { "CutscenePromptController", "DialogInput", "Writer" },
            PauseSceneKind.Credits => new HashSet<string>(),
            PauseSceneKind.Gallery => new HashSet<string> { "GalleryFirstPersonController", "GalleryExhibitInteractor", "GalleryPictureViewer" },
            _ => new HashSet<string> { "LevelOnePlayerInput", "FirstPersonWeaponController", "LevelOnePlayerInteractor",
                "LevelTwoFirstPersonController", "LevelTwoDoorInteractor", "LevelTwoDocumentViewer", "LevelTwoConversationViewer",
                "LevelTwoConfirmationPanel", "LevelThreeOpeningSequence", "LevelThreeTutorialPanel", "LevelThreeTypingGameplay" }
        };
        return scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Behaviour>(true))
            .Where(x => names.Contains(x.GetType().Name)).Distinct().ToArray();
    }

    private static Button CloneMenuButton(Button source, Transform parent, string name, string label)
    {
        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent);
        clone.name = name;
        foreach (SceneTransitionButton transition in clone.GetComponents<SceneTransitionButton>()) UnityEngine.Object.DestroyImmediate(transition);
        Button button = clone.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
        TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true); if (text != null) text.text = label;
        return button;
    }

    private static CanvasGroup CreateOverlay(Transform parent, string name, Color shadeColor)
    {
        GameObject overlay = CreateStretch(parent, name);
        Image shade = overlay.AddComponent<Image>(); shade.color = shadeColor;
        return overlay.AddComponent<CanvasGroup>();
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size);
        Image image = panel.AddComponent<Image>(); image.color = new Color(0.018f, 0.035f, 0.065f, 0.985f);
        Outline outline = panel.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(4f, -4f);
        return panel;
    }

    private static Button CreateCenteredButton(Transform parent, string name, TMP_FontAsset font, string label, float centerY)
        => CreateButton(parent, name, font, label, new Vector2(parent.GetComponent<RectTransform>().sizeDelta.x * 0.5f, centerY), new Vector2(430f, 76f));

    private static Button CreateButton(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position, Vector2 size, bool positionManually = true)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.zero, size);
        if (positionManually) target.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = target.AddComponent<Image>(); image.color = new Color(0.055f, 0.115f, 0.19f, 1f);
        Outline outline = target.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(2f, -2f);
        Button button = target.AddComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(0.12f, 0.28f, 0.4f); colors.pressedColor = Gold; button.colors = colors;
        CreateText(target.transform, "Label", font, 22f, label, TextAlignmentOptions.Center, new Vector2(14f, 8f), new Vector2(-14f, -8f), SoftWhite);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size, string value,
        TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject target = CreateStretch(parent, name); RectTransform rect = target.GetComponent<RectTransform>();
        rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        TextMeshProUGUI text = target.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
        text.alignment = alignment; text.color = color; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject target = new(name, typeof(RectTransform)); target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.sizeDelta = size; rect.pivot = new Vector2(0.5f, 0.5f); return target;
    }

    private static GameObject CreateStretch(Transform parent, string name)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return target;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (scene.GetRootGameObjects().Any(x => x.GetComponentInChildren<EventSystem>(true) != null)) return;
        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static void DestroyNamed(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform match in root.GetComponentsInChildren<Transform>(true).Where(x => x.name == name).ToArray())
                UnityEngine.Object.DestroyImmediate(match.gameObject);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static T FindUnique<T>(Scene scene, string name) where T : Component
    {
        T[] matches = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).Where(x => x.name == name).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"Expected one {typeof(T).Name} named '{name}', found {matches.Length}.");
        return matches[0];
    }

    private static T FindSingleOrNull<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).FirstOrDefault();

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

    private static void RemoveComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>(); if (component != null) UnityEngine.Object.DestroyImmediate(component);
    }

    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new(target); SerializedProperty property = serialized.FindProperty(field); property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string field, string value)
    {
        SerializedObject serialized = new(target); serialized.FindProperty(field).stringValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        SerializedObject serialized = new(target); serialized.FindProperty(field).boolValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(UnityEngine.Object target, string field, int value)
    {
        SerializedObject serialized = new(target); serialized.FindProperty(field).enumValueIndex = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Color Gold => new(0.82f, 0.63f, 0.24f, 1f);
    private static Color Cyan => new(0.65f, 0.9f, 1f, 1f);
    private static Color SoftWhite => new(0.95f, 0.97f, 1f, 1f);

    private readonly struct PauseSceneData
    {
        public PauseSceneData(string scenePath, PauseSceneKind kind, string skipDestination, bool lockCursor)
        { ScenePath = scenePath; Kind = kind; SkipDestination = skipDestination; LockCursor = lockCursor; }
        public string ScenePath { get; }
        public PauseSceneKind Kind { get; }
        public string SkipDestination { get; }
        public bool LockCursor { get; }
    }
}
