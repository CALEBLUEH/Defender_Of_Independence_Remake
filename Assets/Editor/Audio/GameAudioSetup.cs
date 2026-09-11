using System;
using System.Collections.Generic;
using System.Linq;
using DefenderOfIndependence.Audio;
using DefenderOfIndependence.Level1;
using DefenderOfIndependence.Level2;
using DefenderOfIndependence.SceneFlow;
using Fungus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameAudioSetup
{
    private const string LibraryPath = "Assets/Audio/Settings/GameAudioLibrary.asset";
    private const string PrefabPath = "Assets/Audio/Prefabs/Game Audio System.prefab";
    private const string HeadingFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
    private const string BodyFontPath = "Assets/Font/Spheris/Spheris-Regular SDF.asset";
    private const string BoldFontPath = "Assets/Font/Spheris/Spheris-Bold SDF.asset";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scene/Scene_MainMenu.unity", "Assets/Scene/Cutscene_Level1.unity", "Assets/Scene/Scene_Level1.unity",
        "Assets/Scene/Cutscene_Level2.unity", "Assets/Scene/Scene_Level2.unity", "Assets/Scene/Cutscene_Level3.unity",
        "Assets/Scene/Scene_Level3.unity", "Assets/Scene/Scene_Credits.unity", "Assets/Scene/Scene_Gallery.unity"
    };

    [MenuItem("Project Tools/Audio/Build Complete Audio and Options")]
    public static void Run()
    {
        EnsureFolder("Assets", "Audio"); EnsureFolder("Assets/Audio", "Settings"); EnsureFolder("Assets/Audio", "Prefabs");
        GameAudioLibrary library = BuildLibrary();
        GameObject prefab = BuildAudioPrefab(library);
        foreach (string path in ScenePaths) ConfigureScene(path, prefab, library);
        library = RequireAsset<GameAudioLibrary>(LibraryPath);
        ConfigureAudioImporters(library);
        AssetDatabase.SaveAssets();
        ValidateLibrary(library);
        Debug.Log("GAME_AUDIO_SETUP_OK: all supplied clips, scene music, effects, walking, dialogue, failure fades, and persistent option sliders are authored.");
    }

    public static void RunFromCommandLine()
    {
        try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static GameAudioLibrary BuildLibrary()
    {
        GameAudioLibrary library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<GameAudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        SetClip(library, "buttonClick", "buttonclicksoundeffect");
        SetClip(library, "nextDialogue", "nextdialoguesoundeffect");
        SetClip(library, "walking", "walksoundeffect");
        SetClip(library, "loseMusic", "losesoundeffect");
        SetClip(library, "keyboardTap", "keyboardtapsoundeffect");
        SetClip(library, "cheering", "cheeringsoundeffect");
        SetClip(library, "merdeka", "merdekasoundeffect");
        SetClip(library, "gunshot", "gunshotsoundeffect");
        SetClip(library, "mainMenuMusic", "terrariatitlescreen");
        SetClip(library, "levelOneMusic", "dragoncastle");
        SetClip(library, "levelTwoMusic", "celesteoriginalsoundtrack");
        SetClip(library, "levelThreeMusic", "deathbyglamour");
        SetClip(library, "galleryMusic", "kyrieeleison");
        EditorUtility.SetDirty(library);
        return library;
    }

    private static GameObject BuildAudioPrefab(GameAudioLibrary library)
    {
        GameObject root = new("Game Audio System");
        GameAudioService service = root.AddComponent<GameAudioService>();
        AudioSource music = root.AddComponent<AudioSource>();
        AudioSource effects = root.AddComponent<AudioSource>();
        music.playOnAwake = false; music.loop = true; music.spatialBlend = 0f;
        effects.playOnAwake = false; effects.loop = false; effects.spatialBlend = 0f;
        Assign(service, "library", library); Assign(service, "musicSource", music); Assign(service, "effectsSource", effects);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void ConfigureScene(string scenePath, GameObject audioPrefab, GameAudioLibrary library)
    {
        library = RequireAsset<GameAudioLibrary>(LibraryPath);
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        DestroyNamed(scene, "Game Audio System");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(audioPrefab, scene);
        instance.name = "Game Audio System";

        if (scene.name == "Scene_MainMenu") ConfigureMainMenuOptions(scene);
        else ConfigurePauseOptions(scene);

        if (scene.name.StartsWith("Cutscene_Level", StringComparison.Ordinal))
        {
            foreach (Writer writer in FindAll<Writer>(scene))
                if (writer.GetComponent<FungusDialogueAudioListener>() == null) writer.gameObject.AddComponent<FungusDialogueAudioListener>();
        }

        if (scene.name == "Scene_Level1")
        {
            ConfigureMovementAudio(FindOne<StarterAssets.FirstPersonController>(scene).gameObject, library.Walking);
            ConfigureLevelOneFailure(scene);
        }
        else if (scene.name == "Scene_Level2")
        {
            ConfigureMovementAudio(FindOne<LevelTwoFirstPersonController>(scene).gameObject, library.Walking);
        }
        else if (scene.name == "Scene_Gallery")
        {
            ConfigureMovementAudio(FindOne<GalleryFirstPersonController>(scene).gameObject, library.Walking);
        }
        else if (scene.name == "Scene_Level3")
        {
            ConfigureLevelThreeFailure(scene);
            ConfigureLevelThreeAudio(scene);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureMainMenuOptions(Scene scene)
    {
        MainMenuProgressionController menu = FindOne<MainMenuProgressionController>(scene);
        Transform flowUi = FindAll<Transform>(scene).Single(x => x.name == "Main Menu Flow UI");
        DestroyNamed(scene, "Audio Options Panel");
        AudioOptionsPanelController options = CreateOptionsPanel(flowUi);
        Transform fade = flowUi.Find("Main Menu Fade");
        if (fade != null) options.transform.SetSiblingIndex(fade.GetSiblingIndex());
        Assign(menu, "audioOptionsPanel", options);
    }

    private static void ConfigurePauseOptions(Scene scene)
    {
        PauseMenuController pause = FindOne<PauseMenuController>(scene);
        Canvas pauseCanvas = pause.GetComponent<Canvas>();
        if (pauseCanvas == null) pauseCanvas = pause.GetComponentInParent<Canvas>();
        if (pauseCanvas == null) throw new InvalidOperationException(scene.name + ": pause Canvas is missing.");
        DestroyNamed(scene, "Audio Options Panel");
        AudioOptionsPanelController options = CreateOptionsPanel(pauseCanvas.transform);
        Assign(pause, "audioOptionsPanel", options);
    }

    private static AudioOptionsPanelController CreateOptionsPanel(Transform parent)
    {
        TMP_FontAsset heading = RequireAsset<TMP_FontAsset>(HeadingFontPath);
        TMP_FontAsset body = RequireAsset<TMP_FontAsset>(BodyFontPath);
        TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);
        GameObject overlay = CreateStretch(parent, "Audio Options Panel");
        Image shade = overlay.AddComponent<Image>(); shade.color = new Color(0f, 0f, 0f, 0.84f);
        CanvasGroup group = overlay.AddComponent<CanvasGroup>();
        GameObject panel = CreateRect(overlay.transform, "Options", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 610f));
        Image background = panel.AddComponent<Image>(); background.color = new Color(0.018f, 0.035f, 0.065f, 0.99f);
        Outline outline = panel.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(4f, -4f);
        CreateText(panel.transform, "Title", heading, 38f, "AUDIO OPTIONS", TextAlignmentOptions.Center,
            new Vector2(65f, 500f), new Vector2(-65f, -40f), Gold);
        Button close = CreateButton(panel.transform, "Close", bold, "X", new Vector2(820f, 535f), new Vector2(54f, 54f));

        CreateText(panel.transform, "Effects Label", body, 25f, "SOUND EFFECTS", TextAlignmentOptions.Left,
            new Vector2(105f, 358f), new Vector2(-500f, -190f), Color.white);
        Slider effects = CreateSlider(panel.transform, "Sound Effects Slider", new Vector2(485f, 375f));
        TMP_Text effectsValue = CreateText(panel.transform, "Effects Value", bold, 22f, "90%", TextAlignmentOptions.Center,
            new Vector2(725f, 340f), new Vector2(-75f, -220f), Cyan);

        CreateText(panel.transform, "Music Label", body, 25f, "MUSIC", TextAlignmentOptions.Left,
            new Vector2(105f, 218f), new Vector2(-500f, -330f), Color.white);
        Slider music = CreateSlider(panel.transform, "Music Slider", new Vector2(485f, 235f));
        TMP_Text musicValue = CreateText(panel.transform, "Music Value", bold, 22f, "80%", TextAlignmentOptions.Center,
            new Vector2(725f, 200f), new Vector2(-75f, -360f), Cyan);
        CreateText(panel.transform, "Hint", body, 18f, "Settings are saved automatically and used in every scene.", TextAlignmentOptions.Center,
            new Vector2(70f, 65f), new Vector2(-70f, -485f), new Color(0.72f, 0.78f, 0.86f));

        AudioOptionsPanelController controller = overlay.AddComponent<AudioOptionsPanelController>();
        Assign(controller, "panelGroup", group); Assign(controller, "soundEffectsSlider", effects); Assign(controller, "musicSlider", music);
        Assign(controller, "soundEffectsValue", effectsValue); Assign(controller, "musicValue", musicValue); Assign(controller, "closeButton", close);
        return controller;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 position)
    {
        GameObject root = CreateRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(420f, 42f));
        root.GetComponent<RectTransform>().anchoredPosition = position;
        Slider slider = root.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.8f;
        GameObject background = CreateStretch(root.transform, "Background");
        RectTransform backgroundRect = background.GetComponent<RectTransform>(); backgroundRect.offsetMin = new Vector2(0f, 12f); backgroundRect.offsetMax = new Vector2(0f, -12f);
        Image backgroundImage = background.AddComponent<Image>(); backgroundImage.color = new Color(0.08f, 0.12f, 0.18f, 1f);
        GameObject fillArea = CreateStretch(root.transform, "Fill Area");
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>(); fillAreaRect.offsetMin = new Vector2(7f, 14f); fillAreaRect.offsetMax = new Vector2(-7f, -14f);
        GameObject fill = CreateStretch(fillArea.transform, "Fill"); Image fillImage = fill.AddComponent<Image>(); fillImage.color = Gold;
        GameObject handleArea = CreateStretch(root.transform, "Handle Slide Area");
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>(); handleAreaRect.offsetMin = new Vector2(12f, 0f); handleAreaRect.offsetMax = new Vector2(-12f, 0f);
        GameObject handle = CreateRect(handleArea.transform, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 42f));
        Image handleImage = handle.AddComponent<Image>(); handleImage.color = Cyan;
        slider.fillRect = fill.GetComponent<RectTransform>(); slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static void ConfigureMovementAudio(GameObject player, AudioClip walkingClip)
    {
        PlayerMovementLoopAudio movementAudio = player.GetComponent<PlayerMovementLoopAudio>() ?? player.AddComponent<PlayerMovementLoopAudio>();
        AudioSource source = player.GetComponents<AudioSource>().FirstOrDefault(x => x.clip == walkingClip);
        if (source == null) source = player.AddComponent<AudioSource>();
        source.clip = walkingClip; source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
        Assign(movementAudio, "characterController", player.GetComponent<CharacterController>());
        Assign(movementAudio, "audioSource", source); Assign(movementAudio, "walkingClip", walkingClip);
    }

    private static void ConfigureLevelOneFailure(Scene scene)
    {
        LevelOneGameOverController controller = FindOne<LevelOneGameOverController>(scene);
        SerializedObject serialized = new(controller);
        GameObject panel = serialized.FindProperty("gameOverPanel").objectReferenceValue as GameObject;
        if (panel == null || !(panel.transform.parent is RectTransform parent)) throw new InvalidOperationException("Level 1 game-over panel parent is missing.");
        DestroyChild(parent, "Lose Black Fade");
        GameObject overlay = CreateStretch(parent, "Lose Black Fade");
        Image image = overlay.AddComponent<Image>(); image.color = Color.black;
        CanvasGroup group = overlay.AddComponent<CanvasGroup>(); group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        overlay.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());
        Assign(controller, "loseFadeOverlay", group);
    }

    private static void ConfigureLevelThreeFailure(Scene scene)
    {
        LevelThreeTypingGameplay gameplay = FindOne<LevelThreeTypingGameplay>(scene);
        SerializedObject serialized = new(gameplay);
        CanvasGroup fade = serialized.FindProperty("restartFadeOverlay").objectReferenceValue as CanvasGroup;
        if (fade == null) throw new InvalidOperationException("Level 3 restart fade is missing.");
        Assign(gameplay, "failureFadeOverlay", fade);
    }

    private static void ConfigureLevelThreeAudio(Scene scene)
    {
        LevelThreeOpeningSequence opening = FindOne<LevelThreeOpeningSequence>(scene);
        if (opening.GetComponent<AudioListener>() == null) opening.gameObject.AddComponent<AudioListener>();

        LevelThreeTypingGameplay gameplay = FindOne<LevelThreeTypingGameplay>(scene);
        SerializedObject serialized = new(gameplay);
        serialized.FindProperty("loseMusicDelay").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Project Tools/Audio/Repair Level 3 Audio")]
    public static void RepairLevelThreeAudio()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scene/Scene_Level3.unity", OpenSceneMode.Single);
        ConfigureLevelThreeAudio(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LEVEL_THREE_AUDIO_REPAIR_OK: one stable AudioListener and a one-second loss-music delay are authored.");
    }

    public static void RepairLevelThreeAudioFromCommandLine()
    {
        try { RepairLevelThreeAudio(); if (Application.isBatchMode) EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
    }

    private static void ConfigureAudioImporters(GameAudioLibrary library)
    {
        HashSet<AudioClip> music = new() { library.LoseMusic, library.LevelThreeMusic };
        SerializedObject serialized = new(library);
        foreach (string field in new[] { "mainMenuMusic", "levelOneMusic", "levelTwoMusic", "galleryMusic" })
            music.Add(serialized.FindProperty(field).objectReferenceValue as AudioClip);
        foreach (AudioClip clip in AllLibraryClips(library))
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music.Contains(clip) ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music.Contains(clip) ? 0.72f : 0.82f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music.Contains(clip);
            importer.SaveAndReimport();
        }
    }

    private static IEnumerable<AudioClip> AllLibraryClips(GameAudioLibrary library)
    {
        SerializedObject serialized = new(library);
        foreach (string field in new[] { "buttonClick", "nextDialogue", "walking", "loseMusic", "keyboardTap", "cheering", "merdeka", "gunshot",
                     "mainMenuMusic", "levelOneMusic", "levelTwoMusic", "levelThreeMusic", "galleryMusic" })
        {
            AudioClip clip = serialized.FindProperty(field).objectReferenceValue as AudioClip;
            if (clip != null) yield return clip;
        }
    }

    private static void ValidateLibrary(GameAudioLibrary library)
    {
        SerializedObject serialized = new(library);
        List<string> missing = new();
        foreach (string field in new[] { "buttonClick", "nextDialogue", "walking", "loseMusic", "keyboardTap", "cheering", "merdeka", "gunshot",
                     "mainMenuMusic", "levelOneMusic", "levelTwoMusic", "levelThreeMusic", "galleryMusic" })
            if (serialized.FindProperty(field).objectReferenceValue == null) missing.Add(field);
        if (missing.Count > 0) throw new InvalidOperationException("Missing supplied audio clips: " + string.Join(", ", missing));
    }

    private static void SetClip(GameAudioLibrary library, string field, string expectedStem)
    {
        AudioClip[] matches = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(x => x != null && Normalize(x.name).Contains(expectedStem)).ToArray();
        SerializedObject serialized = new(library);
        serialized.FindProperty(field).objectReferenceValue = matches.Length == 1 ? matches[0] : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static void EnsureFolder(string parent, string child) { string path = parent + "/" + child; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child); }

    private static T FindOne<T>(Scene scene) where T : Component
    {
        T[] matches = FindAll<T>(scene).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"{scene.name}: expected one {typeof(T).Name}, found {matches.Length}.");
        return matches[0];
    }

    private static IEnumerable<T> FindAll<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true));

    private static void DestroyNamed(Scene scene, string name)
    {
        foreach (Transform match in FindAll<Transform>(scene).Where(x => x.name == name).ToArray()) UnityEngine.Object.DestroyImmediate(match.gameObject);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static Button CreateButton(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position, Vector2 size)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.zero, size); target.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = target.AddComponent<Image>(); image.color = new Color(0.055f, 0.115f, 0.19f, 1f);
        Outline outline = target.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(2f, -2f);
        Button button = target.AddComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(0.12f, 0.28f, 0.4f); colors.pressedColor = Gold; button.colors = colors;
        CreateText(target.transform, "Label", font, 22f, label, TextAlignmentOptions.Center, new Vector2(10f, 6f), new Vector2(-10f, -6f), Color.white);
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
        GameObject target = new(name, typeof(RectTransform)); target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.sizeDelta = size; rect.pivot = new Vector2(0.5f, 0.5f); return target;
    }

    private static GameObject CreateStretch(Transform parent, string name)
    {
        GameObject target = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero);
        RectTransform rect = target.GetComponent<RectTransform>(); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return target;
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new(target); SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no serialized field " + field);
        property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }

    private static Color Gold => new(0.82f, 0.63f, 0.24f, 1f);
    private static Color Cyan => new(0.35f, 0.86f, 1f, 1f);
}
