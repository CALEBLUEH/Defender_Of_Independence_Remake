using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GalleryQuizSetup
{
    private const string ScenePath = "Assets/Scene/Scene_Gallery.unity";
    private static readonly QuestionData[] Questions =
    {
        new("Who is known as the Father of Independence of Malaya?", new[] { "Tun Razak", "Tun Dr. Ismail", "Tunku Abdul Rahman", "Tun V.T. Sambanthan" }, 2),
        new("When was Tunku Abdul Rahman born?", new[] { "January 1, 1900", "February 8, 1903", "August 31, 1957", "October 10, 1945" }, 1),
        new("Where did Tunku Abdul Rahman study law?", new[] { "Oxford University", "University of Malaya", "Harvard University", "University of Cambridge" }, 3),
        new("When did Japan invade Malaya?", new[] { "1939", "1940", "1941", "1945" }, 2),
        new("When did Singapore fall to Japan?", new[] { "January 1942", "February 1942", "March 1943", "August 1945" }, 1),
        new("When did Japan surrender and leave Malaya?", new[] { "1944", "1945", "1946", "1950" }, 1),
        new("What policy did the British introduce in 1945 that caused protests?", new[] { "New Economic Policy", "Malaysia Proposal", "Malayan Union", "Emergency Act" }, 2),
        new("How did Tunku Abdul Rahman suggest people oppose the Malayan Union?", new[] { "Armed rebellion", "Violent protests", "Peaceful opposition", "Leave the country" }, 2),
        new("Which political party turned to armed struggle against the British?", new[] { "UMNO", "MCA", "MIC", "Malayan Communist Party" }, 3),
        new("Who was the British Prime Minister who agreed to grant independence?", new[] { "Winston Churchill", "Anthony Eden", "Margaret Thatcher", "Tony Blair" }, 1),
        new("When was the Merdeka Agreement signed?", new[] { "August 31, 1957", "February 8, 1956", "July 9, 1963", "October 10, 1945" }, 1),
        new("When is Malaysia's Independence Day?", new[] { "September 16, 1963", "August 31, 1957", "February 8, 1956", "May 27, 1961" }, 1),
        new("Where did the independence ceremony take place?", new[] { "Putrajaya", "Penang", "Merdeka Square", "Johor Bahru" }, 2),
        new("How many times did Tunku shout 'Merdeka'?", new[] { "3 times", "5 times", "7 times", "10 times" }, 2),
        new("When was Malaysia officially formed?", new[] { "August 31, 1957", "July 9, 1963", "September 16, 1963", "August 9, 1965" }, 2),
        new("Which region left Malaysia in 1965?", new[] { "Sabah", "Sarawak", "Brunei", "Singapore" }, 3)
    };

    [MenuItem("Project Tools/Gallery/Build Quiz and Credit Panel")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureOpenScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log("GALLERY_QUIZ_SETUP_OK: long-range aimed interactions, 16 shuffled-answer questions, and the Gallery credit panel are wired.");
    }

    public static void RunFromCommandLine()
    {
        try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    public static void ConfigureOpenScene(Scene scene)
    {
        GameObject gameplayRoot = FindUnique(scene, "Gallery Gameplay");
        GalleryFirstPersonController player = gameplayRoot.GetComponentInChildren<GalleryFirstPersonController>(true);
        GalleryExhibitInteractor interactor = player.GetComponent<GalleryExhibitInteractor>();
        Canvas canvas = FindUnique(scene, "Gallery Interaction UI").GetComponent<Canvas>();
        TMP_FontAsset headingFont = RequireAsset<TMP_FontAsset>("Assets/Font/AudioWide/Audiowide-Regular SDF.asset");
        TMP_FontAsset bodyFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Regular SDF.asset");
        TMP_FontAsset boldFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Bold SDF.asset");

        DestroyChild(canvas.transform, "Gallery Quiz UI");
        DestroyChild(canvas.transform, "Gallery Credit Panel UI");
        EnsureEventSystem(scene);

        GalleryQuizController quiz = CreateQuiz(canvas.transform, gameplayRoot, player, headingFont, bodyFont, boldFont);
        GalleryCreditPanelController credits = CreateCreditPanel(canvas.transform, gameplayRoot, player, headingFont, bodyFont, boldFont);
        GalleryExhibit quizExhibit = ConfigureExhibit(FindUnique(scene, "QuizTrigger"), GalleryExhibitKind.Quiz, "Independence Quiz");
        GalleryExhibit creditExhibit = ConfigureExhibit(FindUnique(scene, "CreditPicture"), GalleryExhibitKind.CreditPanel, "Game Credits");

        GalleryExhibit[] exhibits = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<GalleryExhibit>(true)).Distinct().ToArray();
        SetReference(interactor, "quizController", quiz);
        SetReference(interactor, "creditPanelController", credits);
        SetFloat(interactor, "interactionRange", 30f);
        SetFloat(interactor, "thinOccluderTolerance", 0.75f);
        SetReferenceArray(interactor, "exhibits", exhibits.Cast<UnityEngine.Object>().ToArray());
        EditorUtility.SetDirty(interactor);
        EditorUtility.SetDirty(quizExhibit);
        EditorUtility.SetDirty(creditExhibit);
    }

    private static GalleryQuizController CreateQuiz(Transform canvas, GameObject root, GalleryFirstPersonController player,
        TMP_FontAsset headingFont, TMP_FontAsset bodyFont, TMP_FontAsset boldFont)
    {
        GameObject overlay = CreateStretch(canvas, "Gallery Quiz UI");
        Image shade = overlay.AddComponent<Image>(); shade.color = new Color(0f, 0f, 0f, 0.78f);
        CanvasGroup group = overlay.AddComponent<CanvasGroup>();
        GameObject panel = CreateRect(overlay.transform, "Quiz Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1320f, 820f));
        Image background = panel.AddComponent<Image>(); background.color = new Color(0.018f, 0.035f, 0.065f, 0.98f);
        AddOutline(panel, new Color(0.82f, 0.63f, 0.24f, 1f), 4f);
        CreateText(panel.transform, "Title", headingFont, 38f, "INDEPENDENCE QUIZ", TextAlignmentOptions.Center,
            new Vector2(80f, 720f), new Vector2(-80f, -30f), new Color(0.96f, 0.78f, 0.36f));
        TMP_Text progress = CreateText(panel.transform, "Progress", boldFont, 20f, "QUESTION 1 OF 16", TextAlignmentOptions.Center,
            new Vector2(80f, 665f), new Vector2(-80f, -94f), new Color(0.65f, 0.9f, 1f));
        TMP_Text question = CreateText(panel.transform, "Question", bodyFont, 31f, "Question", TextAlignmentOptions.Center,
            new Vector2(110f, 500f), new Vector2(-110f, -165f), Color.white);
        Button quit = CreateButton(panel.transform, "Quit", boldFont, "X", new Vector2(1238f, 742f), new Vector2(58f, 58f));

        GameObject answersRoot = CreateRect(panel.transform, "Answers 2x2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1080f, 300f));
        answersRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 165f);
        GridLayoutGroup grid = answersRoot.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(520f, 126f); grid.spacing = new Vector2(28f, 28f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;
        Button[] buttons = new Button[4]; TMP_Text[] labels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            buttons[i] = CreateButton(answersRoot.transform, $"Answer {i + 1}", bodyFont, "Answer", Vector2.zero, grid.cellSize, false);
            labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
        }

        GameObject resultRoot = CreateRect(panel.transform, "Quiz Result", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(850f, 310f));
        resultRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 180f);
        TMP_Text result = CreateText(resultRoot.transform, "Score", headingFont, 44f, "SCORE\n0 / 16", TextAlignmentOptions.Center,
            new Vector2(20f, 95f), new Vector2(-20f, -5f), Color.white);
        Button retry = CreateButton(resultRoot.transform, "Retry", boldFont, "RETAKE QUIZ", new Vector2(425f, 50f), new Vector2(350f, 72f));

        GalleryQuizController controller = root.GetComponent<GalleryQuizController>() ?? root.AddComponent<GalleryQuizController>();
        SetReference(controller, "playerController", player); SetReference(controller, "panelGroup", group);
        SetReference(controller, "progressText", progress); SetReference(controller, "questionText", question);
        SetReferenceArray(controller, "answerButtons", buttons.Cast<UnityEngine.Object>().ToArray());
        SetReferenceArray(controller, "answerLabels", labels.Cast<UnityEngine.Object>().ToArray());
        SetReference(controller, "answersRoot", answersRoot); SetReference(controller, "resultRoot", resultRoot);
        SetReference(controller, "resultText", result); SetReference(controller, "retryButton", retry); SetReference(controller, "quitButton", quit);
        ApplyStandardQuestions(controller); EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GalleryCreditPanelController CreateCreditPanel(Transform canvas, GameObject root, GalleryFirstPersonController player,
        TMP_FontAsset headingFont, TMP_FontAsset bodyFont, TMP_FontAsset boldFont)
    {
        GameObject overlay = CreateStretch(canvas, "Gallery Credit Panel UI");
        Image shade = overlay.AddComponent<Image>(); shade.color = new Color(0f, 0f, 0f, 0.78f);
        CanvasGroup group = overlay.AddComponent<CanvasGroup>();
        GameObject panel = CreateRect(overlay.transform, "Credit Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1060f, 520f));
        Image background = panel.AddComponent<Image>(); background.color = new Color(0.018f, 0.035f, 0.065f, 0.98f);
        AddOutline(panel, new Color(0.82f, 0.63f, 0.24f, 1f), 4f);
        CreateText(panel.transform, "Title", headingFont, 38f, "DEFENDER OF INDEPENDENCE", TextAlignmentOptions.Center,
            new Vector2(70f, 405f), new Vector2(-70f, -35f), new Color(0.96f, 0.78f, 0.36f));
        TMP_Text message = CreateText(panel.transform, "Editable Credit Message", bodyFont, 29f,
            "Thank you for joining the journey toward Malayan independence.", TextAlignmentOptions.Center,
            new Vector2(100f, 120f), new Vector2(-100f, -150f), Color.white);
        Button quit = CreateButton(panel.transform, "Quit", boldFont, "X", new Vector2(978f, 442f), new Vector2(58f, 58f));
        GalleryCreditPanelController controller = root.GetComponent<GalleryCreditPanelController>() ?? root.AddComponent<GalleryCreditPanelController>();
        SetReference(controller, "playerController", player); SetReference(controller, "panelGroup", group);
        SetReference(controller, "messageText", message); SetReference(controller, "quitButton", quit);
        EditorUtility.SetDirty(controller); return controller;
    }

    private static GalleryExhibit ConfigureExhibit(GameObject target, GalleryExhibitKind kind, string title)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0) throw new InvalidOperationException(target.name + " requires at least one Collider on itself or its children.");
        Renderer renderer = target.GetComponentInChildren<Renderer>(true);
        GalleryExhibit exhibit = target.GetComponent<GalleryExhibit>() ?? target.AddComponent<GalleryExhibit>();
        SerializedObject serialized = new SerializedObject(exhibit);
        serialized.FindProperty("exhibitKind").enumValueIndex = (int)kind;
        serialized.FindProperty("displayName").stringValue = title;
        serialized.FindProperty("dialogueLines").arraySize = 0;
        SerializedProperty refs = serialized.FindProperty("interactionColliders"); refs.arraySize = colliders.Length;
        for (int i = 0; i < colliders.Length; i++) refs.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
        serialized.FindProperty("focusRenderer").objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo(); return exhibit;
    }

    public static void ApplyStandardQuestions(GalleryQuizController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty questions = serialized.FindProperty("questions"); questions.arraySize = Questions.Length;
        for (int i = 0; i < Questions.Length; i++)
        {
            SerializedProperty item = questions.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("question").stringValue = Questions[i].Question;
            item.FindPropertyRelative("correctAnswerIndex").intValue = Questions[i].CorrectIndex;
            SerializedProperty answers = item.FindPropertyRelative("answers"); answers.arraySize = 4;
            for (int a = 0; a < 4; a++) answers.GetArrayElementAtIndex(a).stringValue = Questions[i].Answers[a];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<EventSystem>(true) != null)) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static Button CreateButton(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position, Vector2 size, bool positionManually = true)
    {
        GameObject target = CreateRect(parent, name, new Vector2(0f, 0f), new Vector2(0f, 0f), size);
        if (positionManually) target.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = target.AddComponent<Image>(); image.color = new Color(0.055f, 0.115f, 0.19f, 1f);
        AddOutline(target, new Color(0.82f, 0.63f, 0.24f, 0.95f), 2f);
        Button button = target.AddComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(0.12f, 0.28f, 0.4f); colors.pressedColor = new Color(0.82f, 0.63f, 0.24f); button.colors = colors;
        CreateText(target.transform, "Label", font, 22f, label, TextAlignmentOptions.Center, new Vector2(18f, 10f), new Vector2(-18f, -10f), Color.white);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size, string value,
        TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject target = CreateStretch(parent, name); RectTransform rect = target.GetComponent<RectTransform>(); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        TextMeshProUGUI text = target.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
        text.alignment = alignment; text.color = color; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false; return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject target = new GameObject(name, typeof(RectTransform)); target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.sizeDelta = size; rect.pivot = new Vector2(0.5f, 0.5f); return target;
    }

    private static GameObject CreateStretch(Transform parent, string name)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return target;
    }

    private static void AddOutline(GameObject target, Color color, float distance)
    {
        Outline outline = target.AddComponent<Outline>(); outline.effectColor = color; outline.effectDistance = new Vector2(distance, -distance);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static GameObject FindUnique(Scene scene, string name)
    {
        GameObject[] matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"Expected one '{name}' object but found {matches.Length}.");
        return matches[0];
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

    private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target); serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReferenceArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(name); property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string name, float value)
    {
        SerializedObject serialized = new SerializedObject(target); serialized.FindProperty(name).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Validate(Scene scene)
    {
        GalleryExhibit[] exhibits = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GalleryExhibit>(true)).ToArray();
        if (exhibits.Length != 11 || exhibits.Count(x => x.ExhibitKind == GalleryExhibitKind.Quiz) != 1 || exhibits.Count(x => x.ExhibitKind == GalleryExhibitKind.CreditPanel) != 1)
            throw new InvalidOperationException("Gallery exhibit configuration is incomplete.");
        GalleryQuizController quiz = UnityEngine.Object.FindFirstObjectByType<GalleryQuizController>(FindObjectsInactive.Include);
        SerializedObject serialized = new SerializedObject(quiz);
        if (serialized.FindProperty("questions").arraySize != 16) throw new InvalidOperationException("Gallery quiz must contain exactly 16 questions.");
    }

    private readonly struct QuestionData
    {
        public QuestionData(string question, string[] answers, int correctIndex) { Question = question; Answers = answers; CorrectIndex = correctIndex; }
        public string Question { get; } public string[] Answers { get; } public int CorrectIndex { get; }
    }
}
