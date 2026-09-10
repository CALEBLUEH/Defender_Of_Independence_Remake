using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class CreditsSetup
{
    private const string LevelThreeScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string CreditsScenePath = "Assets/Scene/Scene_Credits.unity";
    private const string GalleryScenePath = "Assets/Scene/Scene_Gallery.unity";
    private const string VideoPath = "Assets/PictureSpriteAndCredit/CreditVideo.mp4";

    [MenuItem("Project Tools/Non-Gameplay/Build Credits Flow")]
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        BuildCreditsScene();
        WireLevelThree();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("CREDITS_SETUP_OK: Level 3 fade, completion-only thank-you message, full-screen video/audio, hold-Space skip, and Gallery load are wired.");
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

    public static void ValidateFromCommandLine()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate();
            Debug.Log("CREDITS_PERSISTENCE_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void BuildCreditsScene()
    {
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath)
                         ?? throw new FileNotFoundException("Credit video is missing or failed to import.", VideoPath);
        TMP_FontAsset headingFont = RequireAsset<TMP_FontAsset>("Assets/Font/AudioWide/Audiowide-Regular SDF.asset");
        TMP_FontAsset bodyFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Regular SDF.asset");
        TMP_FontAsset boldFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Bold SDF.asset");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Scene_Credits";

        GameObject cameraObject = new GameObject("Credit Playback Camera", typeof(Camera), typeof(AudioListener), typeof(AudioSource), typeof(VideoPlayer));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.depth = 0f;
        cameraObject.tag = "MainCamera";

        AudioSource audioSource = cameraObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        VideoPlayer player = cameraObject.GetComponent<VideoPlayer>();
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.CameraNearPlane;
        player.targetCamera = camera;
        player.aspectRatio = VideoAspectRatio.FitInside;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, audioSource);

        GameObject canvasObject = new GameObject("Credit UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup blackFade = CreateBlackFade(canvasObject.transform);
        (CanvasGroup messageGroup, TMP_Text messageText) = CreateCompletionMessage(canvasObject.transform, headingFont, bodyFont);
        (CanvasGroup skipGroup, Slider skipSlider) = CreateSkipPrompt(canvasObject.transform, boldFont);

        CreditVideoController controller = cameraObject.AddComponent<CreditVideoController>();
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "videoPlayer", player);
        SetReference(serialized, "audioSource", audioSource);
        SetReference(serialized, "playbackCamera", camera);
        SetReference(serialized, "completionMessageGroup", messageGroup);
        SetReference(serialized, "completionMessageText", messageText);
        SetReference(serialized, "skipPromptGroup", skipGroup);
        SetReference(serialized, "skipHoldSlider", skipSlider);
        SetReference(serialized, "blackFadeOverlay", blackFade);
        serialized.FindProperty("fallbackDestinationScene").stringValue = "Scene_Gallery";
        serialized.FindProperty("completionMessage").stringValue =
            "Thank you for joining the journey toward Malayan independence.";
        serialized.FindProperty("completionMessageFadeDuration").floatValue = 0.5f;
        serialized.FindProperty("completionMessageHoldDuration").floatValue = 2.5f;
        serialized.FindProperty("skipHoldDuration").floatValue = 3f;
        serialized.FindProperty("revealFadeDuration").floatValue = 1f;
        serialized.FindProperty("exitFadeDuration").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, CreditsScenePath);
        Debug.Log($"CREDIT_VIDEO_METADATA: {clip.width}x{clip.height}, {clip.length:F2}s, audioTracks={clip.audioTrackCount}");
    }

    private static CanvasGroup CreateBlackFade(Transform parent)
    {
        GameObject fade = CreateStretch(parent, "Black Video Fade", Vector2.zero, Vector2.zero);
        Image image = fade.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        CanvasGroup group = fade.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        return group;
    }

    private static (CanvasGroup, TMP_Text) CreateCompletionMessage(Transform parent, TMP_FontAsset headingFont, TMP_FontAsset bodyFont)
    {
        GameObject panel = CreateRect(parent, "Completion Message Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 430f), Vector2.zero, new Vector2(0.5f, 0.5f));
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.018f, 0.035f, 0.065f, 0.97f);
        background.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.9f);
        outline.effectDistance = new Vector2(4f, -4f);
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        TMP_Text heading = CreateText(panel.transform, "Heading", headingFont, 42f, TextAlignmentOptions.Center,
            new Color(0.96f, 0.78f, 0.36f, 1f), new Vector2(70f, 275f), new Vector2(-70f, -38f));
        heading.text = "THANK YOU FOR PLAYING";
        TMP_Text message = CreateText(panel.transform, "Editable Completion Message", bodyFont, 29f, TextAlignmentOptions.Center,
            Color.white, new Vector2(85f, 55f), new Vector2(-85f, -145f));
        message.text = "Thank you for joining the journey toward Malayan independence.";
        return (group, message);
    }

    private static (CanvasGroup, Slider) CreateSkipPrompt(Transform parent, TMP_FontAsset font)
    {
        GameObject panel = CreateRect(parent, "Skip Credit Video Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(720f, 74f), new Vector2(0f, 24f), new Vector2(0.5f, 0f));
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.018f, 0.035f, 0.065f, 0.88f);
        background.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        TMP_Text prompt = CreateText(panel.transform, "Prompt", font, 20f, TextAlignmentOptions.Center,
            Color.white, new Vector2(20f, 18f), new Vector2(-20f, -14f));
        prompt.text = "HOLD SPACE FOR 3 SECONDS TO SKIP CREDITS";

        GameObject trackObject = CreateRect(panel.transform, "Skip Hold Progress", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(-34f, 7f), new Vector2(0f, 8f), new Vector2(0.5f, 0.5f));
        Image track = trackObject.AddComponent<Image>();
        track.color = new Color(0.12f, 0.18f, 0.22f, 1f);
        track.raycastTarget = false;
        Slider slider = trackObject.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        GameObject fillObject = CreateStretch(trackObject.transform, "Fill", Vector2.zero, Vector2.zero);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.15f, 0.85f, 1f, 1f);
        fill.raycastTarget = false;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = track;
        slider.handleRect = null;
        slider.SetValueWithoutNotify(0f);
        return (group, slider);
    }

    private static void WireLevelThree()
    {
        Scene scene = EditorSceneManager.OpenScene(LevelThreeScenePath, OpenSceneMode.Single);
        LevelThreeTypingGameplay gameplay = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelThreeTypingGameplay>(true)).FirstOrDefault()
            ?? throw new InvalidOperationException("LevelThreeTypingGameplay is missing from Scene_Level3.");
        CanvasGroup fade = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CanvasGroup>(true))
            .FirstOrDefault(group => group.name == "Level 3 Opening Fade")
            ?? throw new InvalidOperationException("Level 3 Opening Fade is missing from Scene_Level3.");

        LevelThreeCreditTransition transition = gameplay.GetComponent<LevelThreeCreditTransition>()
                                               ?? gameplay.gameObject.AddComponent<LevelThreeCreditTransition>();
        SerializedObject serialized = new SerializedObject(transition);
        SetReference(serialized, "blackFadeOverlay", fade);
        serialized.FindProperty("creditsSceneName").stringValue = "Scene_Credits";
        serialized.FindProperty("gallerySceneName").stringValue = "Scene_Gallery";
        serialized.FindProperty("fadeDuration").floatValue = 2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEvent completion = gameplay.GameplayCompletedEvent;
        for (int index = completion.GetPersistentEventCount() - 1; index >= 0; index--)
            if (completion.GetPersistentTarget(index) == transition &&
                completion.GetPersistentMethodName(index) == nameof(LevelThreeCreditTransition.BeginCreditsFromLevelCompletion))
                UnityEventTools.RemovePersistentListener(completion, index);
        UnityEventTools.AddPersistentListener(completion, transition.BeginCreditsFromLevelCompletion);
        EditorUtility.SetDirty(gameplay);
        EditorUtility.SetDirty(transition);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes
            .Where(item => item.path != CreditsScenePath).ToArray();
        int galleryIndex = Array.FindIndex(scenes, item => item.path == GalleryScenePath);
        int insertionIndex = galleryIndex >= 0 ? galleryIndex : scenes.Length;
        EditorBuildSettingsScene[] result = new EditorBuildSettingsScene[scenes.Length + 1];
        Array.Copy(scenes, 0, result, 0, insertionIndex);
        result[insertionIndex] = new EditorBuildSettingsScene(CreditsScenePath, true);
        Array.Copy(scenes, insertionIndex, result, insertionIndex + 1, scenes.Length - insertionIndex);
        EditorBuildSettings.scenes = result;
    }

    private static void Validate()
    {
        Scene credits = EditorSceneManager.OpenScene(CreditsScenePath, OpenSceneMode.Single);
        CreditVideoController controller = credits.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CreditVideoController>(true)).FirstOrDefault()
            ?? throw new InvalidOperationException("CreditVideoController did not persist.");
        SerializedObject controllerData = new SerializedObject(controller);
        string[] requiredReferences =
        {
            "videoPlayer", "audioSource", "playbackCamera", "completionMessageGroup", "completionMessageText",
            "skipPromptGroup", "skipHoldSlider", "blackFadeOverlay"
        };
        foreach (string reference in requiredReferences)
            if (controllerData.FindProperty(reference)?.objectReferenceValue == null)
                throw new InvalidOperationException("CreditVideoController reference is missing: " + reference);
        VideoPlayer player = controller.GetComponent<VideoPlayer>();
        if (player == null || player.clip == null || player.renderMode != VideoRenderMode.CameraNearPlane ||
            player.audioOutputMode != VideoAudioOutputMode.AudioSource || player.GetTargetAudioSource(0) == null)
            throw new InvalidOperationException("Credit video or audio routing is incomplete.");
        if (player.clip.audioTrackCount == 0)
            Debug.LogWarning("The imported credit video reports no audio track. Verify the source MP4 contains sound.");
        foreach (GameObject root in credits.GetRootGameObjects())
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) > 0)
                throw new InvalidOperationException("Scene_Credits contains a missing script under: " + root.name);

        Scene levelThree = EditorSceneManager.OpenScene(LevelThreeScenePath, OpenSceneMode.Single);
        LevelThreeTypingGameplay gameplay = levelThree.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelThreeTypingGameplay>(true)).FirstOrDefault();
        LevelThreeCreditTransition transition = gameplay != null ? gameplay.GetComponent<LevelThreeCreditTransition>() : null;
        bool listenerFound = gameplay != null && Enumerable.Range(0, gameplay.GameplayCompletedEvent.GetPersistentEventCount())
            .Any(index => gameplay.GameplayCompletedEvent.GetPersistentTarget(index) == transition &&
                          gameplay.GameplayCompletedEvent.GetPersistentMethodName(index) == nameof(LevelThreeCreditTransition.BeginCreditsFromLevelCompletion));
        if (transition == null || !listenerFound)
            throw new InvalidOperationException("Level 3 completion is not wired to credits.");
        SerializedObject transitionData = new SerializedObject(transition);
        if (transitionData.FindProperty("blackFadeOverlay")?.objectReferenceValue == null)
            throw new InvalidOperationException("The Level 3 credit transition has no fade overlay.");

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        int levelThreeIndex = Array.FindIndex(buildScenes, item => item.enabled && item.path == LevelThreeScenePath);
        int creditsIndex = Array.FindIndex(buildScenes, item => item.enabled && item.path == CreditsScenePath);
        int galleryIndex = Array.FindIndex(buildScenes, item => item.enabled && item.path == GalleryScenePath);
        if (levelThreeIndex < 0 || creditsIndex <= levelThreeIndex || galleryIndex <= creditsIndex)
            throw new InvalidOperationException("Build Settings must order Level 3, Credits, then Gallery.");

    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, Vector2 pivot)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        target.transform.SetParent(parent, false);
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
        TextMeshProUGUI text = target.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new FileNotFoundException("Missing required asset.", path);

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name)
            ?? throw new InvalidOperationException("Missing serialized field: " + name);
        property.objectReferenceValue = value;
    }
}
