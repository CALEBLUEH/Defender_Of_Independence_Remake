using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelThreeGameplaySetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string HeartPath = "Assets/Level3/UI/Health/heart-inside.png";
    private const string HeadingFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
    private const string BodyFontPath = "Assets/Font/Spheris/Spheris-Regular SDF.asset";
    private const string BoldFontPath = "Assets/Font/Spheris/Spheris-Bold SDF.asset";

    private static readonly string[] MainLines =
    {
        "Today marks a new beginning for our [TYPE:nation].",
        "After many years of effort, the Federation of Malaya stands [TYPE:independent].",
        "This achievement belongs not to one person, but to [TYPE:all our people].",
        "Our future must be built through [TYPE:peace and unity].",
        "Independence gives us freedom, but it also gives us [TYPE:responsibility].",
        "We must govern our country with fairness and protect [TYPE:the rights of our people].",
        "Malays, Chinese, Indians and all communities must work [TYPE:together for our common future].",
        "We have shown that change can be achieved through [TYPE:determination, cooperation and peaceful negotiation].",
        "We remember those who endured hardship, and [TYPE:those who worked peacefully for this day].",
        "Independence is not the end of our journey; [TYPE:it is the beginning of our responsibility].",
        "From this day forward, [TYPE:we must shape the future of our country through our own decisions].",
        "Let us preserve peace, strengthen friendship between our communities, [TYPE:build a nation worthy of this freedom].",
        "Our independence carries a promise: [TYPE:the people of Malaya shall determine the future of their own country.]",
        "[TYPE:Together, we step forward as a free and independent people, united by hope for the future.]",
        "[TYPE:MERDEKA]", "[TYPE:MERDEKA]", "[TYPE:MERDEKA]", "[TYPE:MERDEKA]",
        "[TYPE:MERDEKA]", "[TYPE:MERDEKA]", "[TYPE:MERDEKA]"
    };

    private static readonly string[] IncidentPrompts =
    {
        "Microphone volume is too low",
        "Audience noise is interrupting the ceremony",
        "Recording machine has stopped",
        "Federation flag needs attention",
        "Radio transmission lost",
        "Loud feedback from podium microphone",
        "The next ceremony note cannot be found",
        "Press photographer is blocking the official view",
        "Podium lighting is too dim",
        "Crowd volume is overloading the recorder"
    };

    private static readonly string[] IncidentWords =
    {
        "raise volume", "maintain order", "restart recorder", "secure flag", "restore broadcast",
        "reduce microphone feedback", "find speech notes", "clear camera view", "adjust podium lights", "protect recording"
    };

    private static readonly float[] MainLineDurations =
    {
        4f, 5f, 6f, 6f, 6f, 8f, 9f, 12f, 10f, 10f, 14f, 10f, 15f, 16f,
        4f, 4f, 4f, 4f, 4f, 4f, 4f
    };

    [MenuItem("Project Tools/Level 3/Set Up Main Typing Gameplay")]
    public static void Run()
    {
        ConfigureHeartImporter();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasObject = FindNamedObject(scene, "Level 3 Gameplay UI");
        if (canvasObject == null) throw new InvalidOperationException("Level 3 Gameplay UI was not found.");

        LevelThreeTutorialPanel tutorial = canvasObject.GetComponent<LevelThreeTutorialPanel>();
        if (tutorial == null) throw new InvalidOperationException("Level Three Tutorial Panel was not found.");
        LevelThreeOpeningSequence opening = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelThreeOpeningSequence>(true)).FirstOrDefault();
        if (opening == null) throw new InvalidOperationException("Level Three Opening Sequence was not found.");

        GameObject mainPanel = FindDescendant(canvasObject.transform, "Tutorial Panel");
        GameObject mainHeader = FindDescendant(mainPanel.transform, "Header Band");
        GameObject mainContent = FindDescendant(mainPanel.transform, "Tutorial Content");
        GameObject mainTimer = FindDescendant(canvasObject.transform, "Next Message Timer");
        GameObject mainRail = FindDescendant(mainTimer.transform, "Timer Rail");
        GameObject incidentPanel = FindDescendant(canvasObject.transform, "Incident Tutorial Panel");
        GameObject incidentHeader = FindDescendant(incidentPanel.transform, "Header Band");
        GameObject incidentContent = FindDescendant(incidentPanel.transform, "Incident Content");
        GameObject incidentRail = FindDescendant(incidentPanel.transform, "Incident Timer Rail");

        TMP_FontAsset headingFont = RequireAsset<TMP_FontAsset>(HeadingFontPath);
        TMP_FontAsset bodyFont = RequireAsset<TMP_FontAsset>(BodyFontPath);
        TMP_FontAsset boldFont = RequireAsset<TMP_FontAsset>(BoldFontPath);
        Sprite heartSprite = RequireAsset<Sprite>(HeartPath);

        LevelThreeHealthDisplay health = CreateHealthPanel(canvasObject.transform, headingFont, boldFont, heartSprite);
        (CanvasGroup failureGroup, TMP_Text failureText) = CreateFailurePanel(canvasObject.transform, headingFont, bodyFont);
        (CanvasGroup skipGroup, TMP_Text skipText, Slider skipSlider) = CreateSkipPrompt(canvasObject.transform, boldFont);
        CanvasGroup restartFade = FindNamedObject(scene, "Level 3 Opening Fade")?.GetComponent<CanvasGroup>();
        if (restartFade == null) throw new InvalidOperationException("Level 3 Opening Fade CanvasGroup was not found.");

        LevelThreeTypingGameplay gameplay = GetOrAdd<LevelThreeTypingGameplay>(canvasObject);
        ConfigureTutorial(tutorial, health, gameplay, skipGroup, skipText, skipSlider);
        ConfigureGameplay(gameplay,
            FindText(mainHeader.transform, "Heading"), FindText(mainContent.transform, "Message"), FindText(mainHeader.transform, "Page"),
            FindText(mainTimer.transform, "Timer Label"), FindSlider(mainRail.transform, "Left Timer Fill Track"),
            FindSlider(mainRail.transform, "Right Timer Fill Track"), mainPanel.GetComponent<Outline>(),
            incidentPanel.GetComponent<CanvasGroup>(), incidentPanel.GetComponent<RectTransform>(),
            FindText(incidentHeader.transform, "Heading"), FindText(incidentContent.transform, "Message"),
            FindSlider(incidentRail.transform, "Left Timer Fill Track"), FindSlider(incidentRail.transform, "Right Timer Fill Track"),
            incidentPanel.GetComponent<Outline>(), health, failureGroup, failureText, tutorial, restartFade);

        EditorUtility.SetDirty(tutorial);
        EditorUtility.SetDirty(gameplay);
        EditorUtility.SetDirty(health);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL3_MAIN_GAMEPLAY_SETUP_OK: Assignment speech, incidents, three-hit health, and failure flow are wired without repositioning the existing panels.");
    }

    public static void RunFromCommandLine()
    {
        try
        {
            Run();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static LevelThreeHealthDisplay CreateHealthPanel(Transform canvas, TMP_FontAsset headingFont,
        TMP_FontAsset boldFont, Sprite heartSprite)
    {
        GameObject panel = CreateRect(canvas, "Ceremony Health Panel", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(420f, 132f), new Vector2(-38f, -38f), new Vector2(1f, 1f));
        Canvas healthCanvas = GetOrAdd<Canvas>(panel);
        healthCanvas.overrideSorting = true;
        healthCanvas.sortingOrder = 300;
        Image background = GetOrAdd<Image>(panel);
        background.color = new Color(0.018f, 0.035f, 0.065f, 0.9f);
        background.raycastTarget = false;
        Outline outline = GetOrAdd<Outline>(panel);
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup group = GetOrAdd<CanvasGroup>(panel);
        group.alpha = 0f;

        TMP_Text heading = CreateText(panel.transform, "Heading", headingFont, 18f, TextAlignmentOptions.MidlineLeft,
            new Color(0.96f, 0.78f, 0.36f, 1f), new Vector2(108f, 80f), new Vector2(-18f, -10f));
        heading.text = "CEREMONY HEALTH";

        GameObject iconObject = CreateRect(panel.transform, "Health Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(72f, 72f), new Vector2(57f, -8f), new Vector2(0.5f, 0.5f));
        Image icon = GetOrAdd<Image>(iconObject);
        icon.sprite = heartSprite;
        icon.color = new Color(0.95f, 0.23f, 0.28f, 1f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject trackObject = CreateRect(panel.transform, "Health Slider", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(244f, 28f), new Vector2(226f, 31f), new Vector2(0.5f, 0.5f));
        Image track = GetOrAdd<Image>(trackObject);
        track.color = new Color(0.12f, 0.18f, 0.22f, 1f);
        track.raycastTarget = false;
        Slider slider = GetOrAdd<Slider>(trackObject);
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 3f;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        GameObject fillObject = CreateStretch(trackObject.transform, "Fill", new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Image fill = GetOrAdd<Image>(fillObject);
        fill.color = new Color(0.92f, 0.2f, 0.24f, 1f);
        fill.raycastTarget = false;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = track;
        slider.handleRect = null;
        slider.SetValueWithoutNotify(3f);

        TMP_Text value = CreateText(panel.transform, "Health Value", boldFont, 20f, TextAlignmentOptions.MidlineRight,
            Color.white, new Vector2(310f, 10f), new Vector2(-18f, -70f));
        value.text = "3 / 3";

        LevelThreeHealthDisplay display = GetOrAdd<LevelThreeHealthDisplay>(panel);
        SerializedObject serialized = new SerializedObject(display);
        SetReference(serialized, "canvasGroup", group);
        SetReference(serialized, "healthSlider", slider);
        SetReference(serialized, "valueText", value);
        serialized.FindProperty("maximumHealth").intValue = 3;
        serialized.FindProperty("showDuration").floatValue = 0.35f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return display;
    }

    private static (CanvasGroup, TMP_Text) CreateFailurePanel(Transform canvas, TMP_FontAsset headingFont, TMP_FontAsset bodyFont)
    {
        GameObject overlay = CreateRect(canvas, "Level 3 Failure Overlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Canvas failureCanvas = GetOrAdd<Canvas>(overlay);
        failureCanvas.overrideSorting = true;
        failureCanvas.sortingOrder = 900;
        Image shade = GetOrAdd<Image>(overlay);
        shade.color = new Color(0f, 0f, 0f, 0.78f);
        shade.raycastTarget = false;
        CanvasGroup group = GetOrAdd<CanvasGroup>(overlay);
        group.alpha = 0f;

        GameObject panel = CreateRect(overlay.transform, "Failure Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(860f, 360f), Vector2.zero, new Vector2(0.5f, 0.5f));
        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0.018f, 0.035f, 0.065f, 0.96f);
        panelImage.raycastTarget = false;
        Outline outline = GetOrAdd<Outline>(panel);
        outline.effectColor = new Color(0.92f, 0.2f, 0.24f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);

        TMP_Text heading = CreateText(panel.transform, "Heading", headingFont, 32f, TextAlignmentOptions.Center,
            new Color(0.96f, 0.3f, 0.32f, 1f), new Vector2(45f, 265f), new Vector2(-45f, -28f));
        heading.text = "CEREMONY RECORDING FAILED";
        TMP_Text message = CreateText(panel.transform, "Message", bodyFont, 26f, TextAlignmentOptions.Center,
            Color.white, new Vector2(55f, 40f), new Vector2(-55f, -110f));
        return (group, message);
    }

    private static (CanvasGroup, TMP_Text, Slider) CreateSkipPrompt(Transform canvas, TMP_FontAsset font)
    {
        GameObject panel = CreateRect(canvas, "Skip Tutorial Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(680f, 64f), new Vector2(0f, 18f), new Vector2(0.5f, 0f));
        Canvas promptCanvas = GetOrAdd<Canvas>(panel);
        promptCanvas.overrideSorting = true;
        promptCanvas.sortingOrder = 700;
        Image background = GetOrAdd<Image>(panel);
        background.color = new Color(0.018f, 0.035f, 0.065f, 0.9f);
        background.raycastTarget = false;
        Outline outline = GetOrAdd<Outline>(panel);
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup group = GetOrAdd<CanvasGroup>(panel);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        TMP_Text text = CreateText(panel.transform, "Prompt", font, 20f, TextAlignmentOptions.Center,
            Color.white, new Vector2(18f, 12f), new Vector2(-18f, -12f));
        text.text = "HOLD SPACE FOR 3 SECONDS TO SKIP TUTORIAL";

        GameObject trackObject = CreateRect(panel.transform, "Skip Hold Progress", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(-28f, 6f), new Vector2(0f, 7f), new Vector2(0.5f, 0.5f));
        Image track = GetOrAdd<Image>(trackObject);
        track.color = new Color(0.12f, 0.18f, 0.22f, 1f);
        track.raycastTarget = false;
        Slider slider = GetOrAdd<Slider>(trackObject);
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        GameObject fillObject = CreateStretch(trackObject.transform, "Fill", Vector2.zero, Vector2.zero);
        Image fill = GetOrAdd<Image>(fillObject);
        fill.color = new Color(0.15f, 0.85f, 1f, 1f);
        fill.raycastTarget = false;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = track;
        slider.handleRect = null;
        slider.SetValueWithoutNotify(0f);
        return (group, text, slider);
    }

    private static void ConfigureTutorial(LevelThreeTutorialPanel tutorial, LevelThreeHealthDisplay health,
        LevelThreeTypingGameplay gameplay, CanvasGroup skipGroup, TMP_Text skipText, Slider skipSlider)
    {
        SerializedObject serialized = new SerializedObject(tutorial);
        SetReference(serialized, "healthDisplay", health);
        SetReference(serialized, "skipPromptGroup", skipGroup);
        SetReference(serialized, "skipPromptText", skipText);
        SetReference(serialized, "skipHoldSlider", skipSlider);
        serialized.FindProperty("healthIntroductionLineIndex").intValue = 11;
        serialized.FindProperty("incidentSpawnDelayRange").vector2Value = new Vector2(1f, 2f);
        serialized.FindProperty("skipHoldDuration").floatValue = 3f;
        serialized.FindProperty("skipPromptPulseSpeed").floatValue = 1.2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEvent completion = tutorial.TutorialFinishedEvent;
        for (int index = completion.GetPersistentEventCount() - 1; index >= 0; index--)
            if (completion.GetPersistentTarget(index) == gameplay && completion.GetPersistentMethodName(index) == nameof(LevelThreeTypingGameplay.BeginGameplay))
                UnityEventTools.RemovePersistentListener(completion, index);
        UnityEventTools.AddPersistentListener(completion, gameplay.BeginGameplay);
    }

    private static void ConfigureGameplay(LevelThreeTypingGameplay gameplay, TMP_Text mainHeading, TMP_Text mainMessage,
        TMP_Text mainPage, TMP_Text timerLabel, Slider mainLeft, Slider mainRight, Outline mainOutline,
        CanvasGroup incidentGroup, RectTransform incidentRect, TMP_Text incidentHeading, TMP_Text incidentMessage,
        Slider incidentLeft, Slider incidentRight, Outline incidentOutline, LevelThreeHealthDisplay health,
        CanvasGroup failureGroup, TMP_Text failureText, LevelThreeTutorialPanel tutorial, CanvasGroup restartFade)
    {
        SerializedObject serialized = new SerializedObject(gameplay);
        SetReference(serialized, "mainHeadingText", mainHeading);
        SetReference(serialized, "mainMessageText", mainMessage);
        SetReference(serialized, "mainPageText", mainPage);
        SetReference(serialized, "mainTimerLabel", timerLabel);
        SetReference(serialized, "mainLeftTimerSlider", mainLeft);
        SetReference(serialized, "mainRightTimerSlider", mainRight);
        SetReference(serialized, "mainSelectionOutline", mainOutline);
        SetReference(serialized, "incidentPanelGroup", incidentGroup);
        SetReference(serialized, "incidentPanelRect", incidentRect);
        SetReference(serialized, "incidentHeadingText", incidentHeading);
        SetReference(serialized, "incidentMessageText", incidentMessage);
        SetReference(serialized, "incidentLeftTimerSlider", incidentLeft);
        SetReference(serialized, "incidentRightTimerSlider", incidentRight);
        SetReference(serialized, "incidentSelectionOutline", incidentOutline);
        SetReference(serialized, "healthDisplay", health);
        SetReference(serialized, "failurePanelGroup", failureGroup);
        SetReference(serialized, "failureMessageText", failureText);
        SetReference(serialized, "tutorialPanel", tutorial);
        SetReference(serialized, "restartFadeOverlay", restartFade);
        SetStringArray(serialized.FindProperty("mainLines"), MainLines);
        EnsureFloatArray(serialized.FindProperty("mainLineDurations"), MainLineDurations);
        SetStringArray(serialized.FindProperty("incidentPrompts"), IncidentPrompts);
        SetStringArray(serialized.FindProperty("incidentRequiredWords"), IncidentWords);
        serialized.FindProperty("mainSecondsPerLine").floatValue = 5f;
        serialized.FindProperty("incidentSeconds").floatValue = 5f;
        serialized.FindProperty("incidentSpawnDelayRange").vector2Value = new Vector2(1f, 2f);
        serialized.FindProperty("restartFadeHalfDuration").floatValue = 0.75f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureHeartImporter()
    {
        AssetDatabase.ImportAsset(HeartPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(HeartPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Health icon could not be imported.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, Vector2 pivot)
    {
        GameObject target = FindDirectChild(parent, name) ?? new GameObject(name, typeof(RectTransform));
        if (target.transform.parent != parent) target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.pivot = pivot;
        rect.localScale = Vector3.one;
        return target;
    }

    private static GameObject CreateStretch(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return target;
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size,
        TextAlignmentOptions alignment, Color color, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject target = CreateStretch(parent, name, offsetMin, offsetMax);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(target);
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject FindDescendant(Transform parent, string name) =>
        parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name)?.gameObject
        ?? throw new InvalidOperationException($"Missing Level 3 UI object: {name}");

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
            if (parent.GetChild(index).name == name) return parent.GetChild(index).gameObject;
        return null;
    }

    private static GameObject FindNamedObject(Scene scene, string name) =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name)?.gameObject;

    private static TMP_Text FindText(Transform parent, string name) =>
        FindDescendant(parent, name).GetComponent<TMP_Text>()
        ?? throw new InvalidOperationException($"Missing TMP text: {name}");

    private static Slider FindSlider(Transform parent, string name) =>
        FindDescendant(parent, name).GetComponent<Slider>()
        ?? throw new InvalidOperationException($"Missing Slider: {name}");

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        return asset != null ? asset : throw new InvalidOperationException($"Missing required asset: {path}");
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name)
            ?? throw new InvalidOperationException($"Missing serialized field: {name}");
        property.objectReferenceValue = value;
    }

    private static void SetStringArray(SerializedProperty property, IReadOnlyList<string> values)
    {
        property.arraySize = values.Count;
        for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).stringValue = values[index];
    }

    private static void EnsureFloatArray(SerializedProperty property, IReadOnlyList<float> values)
    {
        int previousSize = property.arraySize;
        property.arraySize = values.Count;
        for (int index = previousSize; index < values.Count; index++)
            property.GetArrayElementAtIndex(index).floatValue = values[index];
    }
}
