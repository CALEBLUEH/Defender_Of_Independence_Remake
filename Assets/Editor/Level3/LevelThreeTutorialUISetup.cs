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

public static class LevelThreeTutorialUISetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string HeadingFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
    private const string BodyFontPath = "Assets/Font/Spheris/Spheris-Regular SDF.asset";
    private const string BoldFontPath = "Assets/Font/Spheris/Spheris-Bold SDF.asset";

    private static readonly string[] TutorialLines =
    {
        "Welcome to the Independence Ceremony!",
        "This is the final level in the game.",
        "You are the ceremony's speech writer and director.",
        "Tunku Abdul Rahman will deliver a speech on the stage, but parts of the speech are incomplete.",
        "Words shown in <color=#AEB8C4>grey</color> must be typed before the message timer reaches the centre.",
        "Try to type [TYPE:this word.]",
        "Great job!",
        "There is a catch: unexpected ceremony problems will also appear, and it is your job to fix them.",
        "A separate incident panel will ask you to type its repair command before its timer runs out.",
        "Press <color=#AEB8C4>Left Tab</color> to switch panels. The selected panel will be identified by its glowing border.",
        "Great job! You now know how the panel system will work.",
        "A health bar will be added above the panel. Missing a message before time expires will reduce it.",
        "If health reaches zero, the ceremony recording will fail, so complete every line in time.",
        "That is all. [TYPE:I'm ready!]"
    };

    [MenuItem("Project Tools/Level 3/Set Up Tutorial Gameplay UI")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        LevelThreeOpeningSequence opening = FindSceneComponent<LevelThreeOpeningSequence>(scene);
        if (opening == null) throw new InvalidOperationException("Level 3 Opening Sequence is missing. Run its setup first.");

        TMP_FontAsset headingFont = RequireAsset<TMP_FontAsset>(HeadingFontPath);
        TMP_FontAsset bodyFont = RequireAsset<TMP_FontAsset>(BodyFontPath);
        TMP_FontAsset boldFont = RequireAsset<TMP_FontAsset>(BoldFontPath);

        GameObject canvasObject = FindNamedObject(scene, "Level 3 Gameplay UI");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Level 3 Gameplay UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(LevelThreeTutorialPanel));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
        }
        canvasObject.SetActive(true);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;
        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(canvasObject);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        GameObject panel = CreateRect(canvasObject.transform, "Tutorial Panel", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(1240f, 430f), new Vector2(0f, 50f));
        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0.018f, 0.035f, 0.065f, 0.88f);
        panelImage.raycastTarget = false;
        Outline outline = GetOrAdd<Outline>(panel);
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject headerBand = CreateStretch(panel.transform, "Header Band", new Vector2(22f, -82f), new Vector2(-22f, -20f));
        Image headerBandImage = GetOrAdd<Image>(headerBand);
        headerBandImage.color = new Color(0.055f, 0.11f, 0.17f, 0.9f);
        headerBandImage.raycastTarget = false;

        TMP_Text heading = CreateText(headerBand.transform, "Heading", headingFont, 28f,
            TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.78f, 0.36f, 1f));
        SetOffsets(heading.rectTransform, new Vector2(22f, 0f), new Vector2(-180f, 0f));
        TMP_Text page = CreateText(headerBand.transform, "Page", boldFont, 22f,
            TextAlignmentOptions.MidlineRight, new Color(0.62f, 0.77f, 0.83f, 1f));
        SetOffsets(page.rectTransform, new Vector2(980f, 0f), new Vector2(-22f, 0f));

        GameObject content = CreateStretch(panel.transform, "Tutorial Content", new Vector2(55f, -365f), new Vector2(-55f, -105f));
        CanvasGroup contentGroup = GetOrAdd<CanvasGroup>(content);
        TMP_Text message = CreateText(content.transform, "Message", bodyFont, 34f,
            TextAlignmentOptions.Center, new Color(0.94f, 0.96f, 0.96f, 1f));
        SetOffsets(message.rectTransform, Vector2.zero, Vector2.zero);
        message.enableWordWrapping = true;
        message.richText = true;
        message.lineSpacing = 8f;

        GameObject timerDock = CreateRect(canvasObject.transform, "Next Message Timer", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(1320f, 92f), new Vector2(0f, 54f));
        TMP_Text timerLabel = CreateText(timerDock.transform, "Timer Label", headingFont, 18f,
            TextAlignmentOptions.Center, new Color(0.74f, 0.84f, 0.86f, 1f));
        SetAnchoredRect(timerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(270f, 28f), new Vector2(0f, -14f));

        GameObject rail = CreateRect(timerDock.transform, "Timer Rail", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(1220f, 24f), new Vector2(0f, 16f));
        Image railImage = GetOrAdd<Image>(rail);
        railImage.color = new Color(0.01f, 0.02f, 0.035f, 0.88f);
        railImage.raycastTarget = false;

        Slider leftSlider = CreateTimerHalf(rail.transform, "Left Timer Fill", true);
        Slider rightSlider = CreateTimerHalf(rail.transform, "Right Timer Fill", false);

        GameObject centreMarker = CreateRect(rail.transform, "Centre Marker", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), Vector2.zero);
        centreMarker.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, 45f);
        Image markerImage = GetOrAdd<Image>(centreMarker);
        markerImage.color = new Color(0.96f, 0.78f, 0.36f, 1f);
        markerImage.raycastTarget = false;

        GameObject incidentPanel = CreateRect(canvasObject.transform, "Incident Tutorial Panel", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(1040f, 290f), new Vector2(0f, 350f));
        Canvas incidentCanvas = GetOrAdd<Canvas>(incidentPanel);
        incidentCanvas.overrideSorting = true;
        incidentCanvas.sortingOrder = 500;
        CanvasGroup incidentGroup = GetOrAdd<CanvasGroup>(incidentPanel);
        incidentGroup.alpha = 0f;
        incidentGroup.interactable = false;
        incidentGroup.blocksRaycasts = false;
        Image incidentImage = GetOrAdd<Image>(incidentPanel);
        incidentImage.color = new Color(0.018f, 0.035f, 0.065f, 0.94f);
        incidentImage.raycastTarget = false;
        Outline incidentOutline = GetOrAdd<Outline>(incidentPanel);
        incidentOutline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
        incidentOutline.effectDistance = new Vector2(2f, -2f);

        GameObject incidentHeaderBand = CreateStretch(incidentPanel.transform, "Header Band", new Vector2(18f, -66f), new Vector2(-18f, -16f));
        Image incidentHeaderImage = GetOrAdd<Image>(incidentHeaderBand);
        incidentHeaderImage.color = new Color(0.055f, 0.11f, 0.17f, 0.96f);
        incidentHeaderImage.raycastTarget = false;
        TMP_Text incidentHeading = CreateText(incidentHeaderBand.transform, "Heading", headingFont, 22f,
            TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.78f, 0.36f, 1f));
        SetOffsets(incidentHeading.rectTransform, new Vector2(20f, 0f), new Vector2(-20f, 0f));

        GameObject incidentContent = CreateStretch(incidentPanel.transform, "Incident Content", new Vector2(42f, -215f), new Vector2(-42f, -82f));
        TMP_Text incidentMessage = CreateText(incidentContent.transform, "Message", bodyFont, 29f,
            TextAlignmentOptions.Center, new Color(0.94f, 0.96f, 0.96f, 1f));
        SetOffsets(incidentMessage.rectTransform, Vector2.zero, Vector2.zero);
        incidentMessage.enableWordWrapping = true;
        incidentMessage.richText = true;

        GameObject incidentRail = CreateRect(incidentPanel.transform, "Incident Timer Rail", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(930f, 24f), new Vector2(0f, 28f));
        Image incidentRailImage = GetOrAdd<Image>(incidentRail);
        incidentRailImage.color = new Color(0.01f, 0.02f, 0.035f, 0.92f);
        incidentRailImage.raycastTarget = false;
        Slider incidentLeftSlider = CreateTimerHalf(incidentRail.transform, "Left Timer Fill", true);
        Slider incidentRightSlider = CreateTimerHalf(incidentRail.transform, "Right Timer Fill", false);
        GameObject incidentMarker = CreateRect(incidentRail.transform, "Centre Marker", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), Vector2.zero);
        incidentMarker.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, 45f);
        Image incidentMarkerImage = GetOrAdd<Image>(incidentMarker);
        incidentMarkerImage.color = new Color(0.96f, 0.78f, 0.36f, 1f);
        incidentMarkerImage.raycastTarget = false;
        incidentPanel.transform.SetAsLastSibling();

        LevelThreeTutorialPanel tutorial = GetOrAdd<LevelThreeTutorialPanel>(canvasObject);
        ConfigureTutorial(tutorial, panelGroup, contentGroup, heading, message, page, timerLabel, leftSlider, rightSlider,
            outline, incidentGroup, incidentHeading, incidentMessage, incidentLeftSlider, incidentRightSlider, incidentOutline);
        WireCompletionEvent(opening, tutorial);

        EditorUtility.SetDirty(opening);
        EditorUtility.SetDirty(tutorial);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL3_TUTORIAL_UI_SETUP_OK: Slider timers, typing gates, and the incident tutorial panel are wired.");
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

    private static void ConfigureTutorial(LevelThreeTutorialPanel tutorial, CanvasGroup panelGroup, CanvasGroup contentGroup,
        TMP_Text heading, TMP_Text message, TMP_Text page, TMP_Text timerLabel, Slider leftSlider, Slider rightSlider,
        Outline outline, CanvasGroup incidentGroup, TMP_Text incidentHeading, TMP_Text incidentMessage,
        Slider incidentLeftSlider, Slider incidentRightSlider, Outline incidentOutline)
    {
        SerializedObject serialized = new SerializedObject(tutorial);
        SetReference(serialized, "panelGroup", panelGroup);
        SetReference(serialized, "contentGroup", contentGroup);
        SetReference(serialized, "headingText", heading);
        SetReference(serialized, "messageText", message);
        SetReference(serialized, "pageText", page);
        SetReference(serialized, "timerLabel", timerLabel);
        SetReference(serialized, "leftTimerSlider", leftSlider);
        SetReference(serialized, "rightTimerSlider", rightSlider);
        SetReference(serialized, "selectionOutline", outline);
        SetReference(serialized, "incidentPanelGroup", incidentGroup);
        SetReference(serialized, "incidentHeadingText", incidentHeading);
        SetReference(serialized, "incidentMessageText", incidentMessage);
        SetReference(serialized, "incidentLeftTimerSlider", incidentLeftSlider);
        SetReference(serialized, "incidentRightTimerSlider", incidentRightSlider);
        SetReference(serialized, "incidentSelectionOutline", incidentOutline);

        SerializedProperty lines = serialized.FindProperty("tutorialLines");
        lines.arraySize = TutorialLines.Length;
        for (int index = 0; index < TutorialLines.Length; index++)
            lines.GetArrayElementAtIndex(index).stringValue = TutorialLines[index];
        serialized.FindProperty("panelFadeInDuration").floatValue = 1.25f;
        serialized.FindProperty("secondsPerLine").floatValue = 5f;
        serialized.FindProperty("contentFadeDuration").floatValue = 0.2f;
        serialized.FindProperty("incidentFadeInDuration").floatValue = 0.3f;
        serialized.FindProperty("incidentTriggerLineIndex").intValue = 9;
        serialized.FindProperty("incidentTutorialLine").stringValue =
            "TEST INCIDENT // Switch here with Left Tab and type [TYPE:left tab] to restore panel control.";
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireCompletionEvent(LevelThreeOpeningSequence opening, LevelThreeTutorialPanel tutorial)
    {
        UnityEvent completion = opening.SequenceFinishedEvent;
        for (int index = completion.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            if (completion.GetPersistentTarget(index) == tutorial && completion.GetPersistentMethodName(index) == nameof(LevelThreeTutorialPanel.ShowTutorial))
                UnityEventTools.RemovePersistentListener(completion, index);
        }
        UnityEventTools.AddPersistentListener(completion, tutorial.ShowTutorial);
    }

    private static Slider CreateTimerHalf(Transform rail, string name, bool left)
    {
        GameObject half = FindDirectChild(rail, name + " Track");
        if (half == null)
        {
            half = new GameObject(name + " Track", typeof(RectTransform), typeof(Image), typeof(Slider));
            half.transform.SetParent(rail, false);
        }
        RectTransform halfRect = half.GetComponent<RectTransform>();
        halfRect.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
        halfRect.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
        halfRect.offsetMin = new Vector2(left ? 5f : 3f, 4f);
        halfRect.offsetMax = new Vector2(left ? -3f : -5f, -4f);
        Image track = half.GetComponent<Image>();
        track.color = new Color(0.12f, 0.18f, 0.22f, 0.9f);
        track.raycastTarget = false;
        Slider slider = GetOrAdd<Slider>(half);
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = left ? Slider.Direction.RightToLeft : Slider.Direction.LeftToRight;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        GameObject fillObject = CreateStretch(half.transform, name, Vector2.zero, Vector2.zero);
        Image fill = GetOrAdd<Image>(fillObject);
        fill.color = new Color(0.15f, 0.8f, 0.9f, 1f);
        fill.type = Image.Type.Simple;
        fill.raycastTarget = false;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = null;
        slider.targetGraphic = track;
        slider.SetValueWithoutNotify(1f);
        return slider;
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateStretch(parent, name, Vector2.zero, Vector2.zero);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(textObject);
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 anchoredPosition)
    {
        GameObject target = FindDirectChild(parent, name);
        if (target == null)
        {
            target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
        }
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return target;
    }

    private static GameObject CreateStretch(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return target;
    }

    private static void SetOffsets(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
            if (parent.GetChild(index).name == name) return parent.GetChild(index).gameObject;
        return null;
    }

    private static GameObject FindNamedObject(Scene scene, string name)
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name)?.gameObject;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"Missing required asset: {path}");
        return asset;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) throw new InvalidOperationException($"Missing serialized field: {name}");
        property.objectReferenceValue = value;
    }
}
