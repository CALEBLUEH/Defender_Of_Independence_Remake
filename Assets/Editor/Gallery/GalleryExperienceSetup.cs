using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GalleryExperienceSetup
{
    private const string ScenePath = "Assets/Scene/Scene_Gallery.unity";
    private const string GameplayRootName = "Gallery Gameplay";

    private static readonly Dictionary<string, ExhibitContent> ExhibitContents = new Dictionary<string, ExhibitContent>
    {
        ["JapanPicture"] = new ExhibitContent("Japanese Occupation", new[]
        {
            "Japan invaded Malaya in December 1941, and Singapore fell in February 1942 after a rapid campaign.",
            "Occupation brought shortages, fear and severe hardship to communities across Malaya.",
            "Japan surrendered in August 1945, ending the occupation but leaving Malaya's political future unresolved.",
            "The experience strengthened the belief among many people that Malaya should determine its own future."
        }),
        ["LondonPicture"] = new ExhibitContent("The London Conference", new[]
        {
            "In 1956, Tunku Abdul Rahman led a delegation representing political leaders and the Malay Rulers to London.",
            "They sought full self-government, a workable constitutional settlement and a definite date for independence.",
            "The discussions examined security, finance, defence, public administration and the transfer of responsibility to Malayan ministers.",
            "The agreement reached on 8 February 1956 established a path toward independence by August 1957."
        }),
        ["TunkuPicture"] = new ExhibitContent("Tunku Abdul Rahman", new[]
        {
            "Tunku Abdul Rahman Putra Al-Haj was born on 8 February 1903 and later studied law at the University of Cambridge.",
            "He became a central leader of Malaya's independence movement and guided the delegation that negotiated with Britain.",
            "His approach emphasized unity, preparation and peaceful constitutional negotiation.",
            "He remains widely remembered as the Father of Independence."
        }),
        ["MalaysiaPicture"] = new ExhibitContent("The Formation of Malaysia", new[]
        {
            "On 27 May 1961, Tunku Abdul Rahman publicly proposed a wider federation called Malaysia.",
            "Consultations and public assessments followed, including inquiries in Sabah and Sarawak.",
            "The Malaysia Agreement was signed on 9 July 1963 by Britain, Malaya, Singapore, Sabah and Sarawak.",
            "Malaysia was formally established on 16 September 1963; Singapore later separated on 9 August 1965."
        }),
        ["BritishPicture"] = new ExhibitContent("British Administration", new[]
        {
            "British administration returned after the Second World War and introduced the Malayan Union in 1946.",
            "The proposal generated strong opposition, especially over citizenship and the constitutional position of the Malay Rulers.",
            "At the same time, the Malayan Emergency and the Communist Party's armed struggle made internal security a major concern.",
            "These pressures shaped later negotiations over how authority could pass to a self-governing Malaya."
        }),
        ["ReturnPicture"] = new ExhibitContent("Return and Peaceful Opposition", new[]
        {
            "During the Japanese occupation, Tunku Abdul Rahman had limited political opportunity but continued to oppose foreign rule.",
            "After Japan surrendered in August 1945, calls for Malaya to shape its own future became stronger.",
            "When plans for the Malayan Union were announced, organizations in Kedah held meetings, rallies and protests.",
            "Tunku spoke firmly at these gatherings while urging organized and peaceful opposition."
        }),
        ["IndependencePicture"] = new ExhibitContent("Independence", new[]
        {
            "The 1956 London agreement turned independence from an aspiration into a defined constitutional process.",
            "Malayan leaders prepared to assume responsibility while a commission developed recommendations for a new constitution.",
            "The Federation of Malaya became independent on 31 August 1957.",
            "Independence meant both freedom from colonial rule and responsibility for governing a united country."
        }),
        ["NegotiationPicture"] = new ExhibitContent("Negotiation", new[]
        {
            "Successful negotiation began with a united delegation and a clear objective: self-government leading to independence.",
            "Malayan representatives answered difficult questions about security, finance, defence and constitutional government.",
            "Public support and cooperation among Malaya's communities strengthened the delegation's position.",
            "Compromise did not abandon the goal of independence; it created a practical route for achieving it peacefully."
        })
    };

    [MenuItem("Project Tools/Gallery/Build Player, Exhibits, and Credit Replay")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform initialSpawn = FindUnique(scene, "InitialSpawnPoint").transform;
        Transform videoSpawn = FindUnique(scene, "VideoSpawnPoint").transform;
        Camera camera = FindUnique(scene, "Main Camera").GetComponent<Camera>()
                        ?? throw new InvalidOperationException("Main Camera has no Camera component.");

        GameObject existingRoot = scene.GetRootGameObjects().FirstOrDefault(item => item.name == GameplayRootName);
        if (existingRoot != null)
        {
            camera.transform.SetParent(null, true);
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }

        GameObject root = new GameObject(GameplayRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        GameObject player = CreatePlayer(root.transform, camera, initialSpawn, videoSpawn);
        GalleryFirstPersonController controller = player.GetComponent<GalleryFirstPersonController>();
        GalleryPictureFocus focus = root.AddComponent<GalleryPictureFocus>();
        SetReference(focus, "playerCamera", camera);

        Canvas canvas = CreateCanvas(root.transform);
        TMP_FontAsset headingFont = RequireAsset<TMP_FontAsset>("Assets/Font/AudioWide/Audiowide-Regular SDF.asset");
        TMP_FontAsset bodyFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Regular SDF.asset");
        TMP_FontAsset boldFont = RequireAsset<TMP_FontAsset>("Assets/Font/Spheris/Spheris-Bold SDF.asset");
        TMP_Text prompt = CreateInteractionPrompt(canvas.transform, boldFont);
        (CanvasGroup dialogueGroup, TMP_Text title, TMP_Text content, TMP_Text continuePrompt) =
            CreateDialoguePanel(canvas.transform, headingFont, bodyFont, boldFont);
        CanvasGroup fadeOverlay = CreateFadeOverlay(canvas.transform);

        GalleryPictureViewer viewer = root.AddComponent<GalleryPictureViewer>();
        SetReference(viewer, "playerController", controller);
        SetReference(viewer, "pictureFocus", focus);
        SetReference(viewer, "dialogueGroup", dialogueGroup);
        SetReference(viewer, "titleText", title);
        SetReference(viewer, "contentText", content);
        SetReference(viewer, "continuePromptText", continuePrompt);

        GalleryScreenFader fader = root.AddComponent<GalleryScreenFader>();
        SetReference(fader, "playerController", controller);
        SetReference(fader, "blackOverlay", fadeOverlay);

        List<GalleryExhibit> exhibits = ConfigureExhibits(scene);
        GalleryExhibitInteractor interactor = player.AddComponent<GalleryExhibitInteractor>();
        SetReference(interactor, "playerController", controller);
        SetReference(interactor, "playerCamera", camera);
        SetReference(interactor, "pictureViewer", viewer);
        SetReference(interactor, "screenFader", fader);
        SetReference(interactor, "interactionPrompt", prompt);
        SetReferenceArray(interactor, "exhibits", exhibits.Cast<UnityEngine.Object>().ToArray());

        GalleryQuizSetup.ConfigureOpenScene(scene);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(focus);
        EditorUtility.SetDirty(viewer);
        EditorUtility.SetDirty(fader);
        EditorUtility.SetDirty(interactor);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("GALLERY_EXPERIENCE_SETUP_OK: no-jump player, two-route spawning, eight focus dialogues, and credit replay are wired without moving the user's exhibits.");
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
            Debug.Log("GALLERY_EXPERIENCE_PERSISTENCE_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static GameObject CreatePlayer(Transform parent, Camera camera, Transform initialSpawn, Transform videoSpawn)
    {
        GameObject player = new GameObject("Gallery First Person Player", typeof(CharacterController), typeof(GalleryFirstPersonController));
        player.transform.SetParent(parent, false);
        CharacterController character = player.GetComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.35f;
        character.center = new Vector3(0f, 0.9f, 0f);
        character.stepOffset = 0.3f;
        character.slopeLimit = 45f;

        Transform pivot = new GameObject("Camera Pivot").transform;
        pivot.SetParent(player.transform, false);
        pivot.localPosition = new Vector3(0f, 1.6f, 0f);
        camera.transform.SetParent(pivot, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.gameObject.tag = "MainCamera";
        camera.enabled = true;
        if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();

        GalleryFirstPersonController controller = player.GetComponent<GalleryFirstPersonController>();
        SetReference(controller, "cameraPivot", pivot);
        SetReference(controller, "initialSpawnPoint", initialSpawn);
        SetReference(controller, "videoSpawnPoint", videoSpawn);
        return player;
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Gallery Interaction UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static TMP_Text CreateInteractionPrompt(Transform parent, TMP_FontAsset font)
    {
        GameObject panel = CreateRect(parent, "Exhibit Interaction Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(720f, 62f), new Vector2(0f, 38f), new Vector2(0.5f, 0f));
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.018f, 0.035f, 0.065f, 0.9f);
        image.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        TMP_Text text = CreateText(panel.transform, "Prompt", font, 21f, TextAlignmentOptions.Center,
            Color.white, new Vector2(22f, 10f), new Vector2(-22f, -10f));
        text.text = "PRESS C TO VIEW EXHIBIT";
        panel.SetActive(false);
        return text;
    }

    private static (CanvasGroup, TMP_Text, TMP_Text, TMP_Text) CreateDialoguePanel(Transform parent,
        TMP_FontAsset headingFont, TMP_FontAsset bodyFont, TMP_FontAsset boldFont)
    {
        GameObject panel = CreateRect(parent, "Gallery Picture Dialogue", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(1420f, 330f), new Vector2(0f, 58f), new Vector2(0.5f, 0f));
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.018f, 0.035f, 0.065f, 0.93f);
        background.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.63f, 0.24f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        TMP_Text title = CreateText(panel.transform, "Exhibit Title", headingFont, 30f, TextAlignmentOptions.Center,
            new Color(0.96f, 0.78f, 0.36f, 1f), new Vector2(72f, 240f), new Vector2(-72f, -24f));
        TMP_Text content = CreateText(panel.transform, "Dialogue Content", bodyFont, 27f, TextAlignmentOptions.Center,
            Color.white, new Vector2(92f, 76f), new Vector2(-92f, -100f));
        TMP_Text prompt = CreateText(panel.transform, "Next Prompt", boldFont, 19f, TextAlignmentOptions.Center,
            new Color(0.65f, 0.9f, 1f, 1f), new Vector2(80f, 20f), new Vector2(-80f, -268f));
        return (group, title, content, prompt);
    }

    private static CanvasGroup CreateFadeOverlay(Transform parent)
    {
        GameObject overlay = CreateStretch(parent, "Gallery Screen Fade", Vector2.zero, Vector2.zero);
        Canvas canvas = overlay.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;
        Image image = overlay.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        CanvasGroup group = overlay.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        return group;
    }

    private static List<GalleryExhibit> ConfigureExhibits(Scene scene)
    {
        List<GalleryExhibit> result = new List<GalleryExhibit>();
        foreach (KeyValuePair<string, ExhibitContent> pair in ExhibitContents)
        {
            GameObject target = FindUnique(scene, pair.Key);
            result.Add(ConfigureExhibit(target, GalleryExhibitKind.HistoricalPicture, pair.Value.Title, pair.Value.Lines));
        }

        GameObject credit = FindUnique(scene, "CreditVideoPicture");
        result.Add(ConfigureExhibit(credit, GalleryExhibitKind.CreditVideo, "Credits", Array.Empty<string>()));
        return result;
    }

    private static GalleryExhibit ConfigureExhibit(GameObject target, GalleryExhibitKind kind, string title, string[] lines)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0) throw new InvalidOperationException(target.name + " has no Collider.");
        Renderer renderer = target.GetComponentInChildren<Renderer>(true)
                            ?? throw new InvalidOperationException(target.name + " has no Renderer.");
        GalleryExhibit exhibit = target.GetComponent<GalleryExhibit>() ?? target.AddComponent<GalleryExhibit>();
        SerializedObject serialized = new SerializedObject(exhibit);
        serialized.FindProperty("exhibitKind").enumValueIndex = (int)kind;
        serialized.FindProperty("displayName").stringValue = title;
        SerializedProperty dialogue = serialized.FindProperty("dialogueLines");
        dialogue.arraySize = lines.Length;
        for (int index = 0; index < lines.Length; index++) dialogue.GetArrayElementAtIndex(index).stringValue = lines[index];
        SerializedProperty colliderArray = serialized.FindProperty("interactionColliders");
        colliderArray.arraySize = colliders.Length;
        for (int index = 0; index < colliders.Length; index++) colliderArray.GetArrayElementAtIndex(index).objectReferenceValue = colliders[index];
        serialized.FindProperty("focusRenderer").objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(exhibit);
        return exhibit;
    }

    private static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == GameplayRootName)
                          ?? throw new InvalidOperationException("Gallery Gameplay root is missing.");
        GalleryFirstPersonController player = root.GetComponentInChildren<GalleryFirstPersonController>(true)
                                              ?? throw new InvalidOperationException("Gallery player is missing.");
        if (player.InitialSpawnPoint == null || player.VideoSpawnPoint == null || player.GetComponent<CharacterController>() == null)
            throw new InvalidOperationException("Gallery player spawn or CharacterController references are incomplete.");
        GalleryExhibit[] exhibits = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<GalleryExhibit>(true)).ToArray();
        if (exhibits.Length != 11 || exhibits.Count(item => item.ExhibitKind == GalleryExhibitKind.HistoricalPicture) != 8 ||
            exhibits.Count(item => item.ExhibitKind == GalleryExhibitKind.CreditVideo) != 1 ||
            exhibits.Count(item => item.ExhibitKind == GalleryExhibitKind.Quiz) != 1 ||
            exhibits.Count(item => item.ExhibitKind == GalleryExhibitKind.CreditPanel) != 1)
            throw new InvalidOperationException("Gallery must contain eight historical exhibits, credit video, quiz, and credit-panel exhibits.");
        if (exhibits.Where(item => item.ExhibitKind == GalleryExhibitKind.HistoricalPicture)
            .Any(item => item.DialogueLines == null || item.DialogueLines.Length < 2))
            throw new InvalidOperationException("One or more historical exhibits have no line-by-line dialogue.");
        GalleryExhibitInteractor interactor = root.GetComponentInChildren<GalleryExhibitInteractor>(true);
        GalleryPictureViewer viewer = root.GetComponent<GalleryPictureViewer>();
        GalleryScreenFader fader = root.GetComponent<GalleryScreenFader>();
        if (interactor == null || viewer == null || fader == null)
            throw new InvalidOperationException("Gallery interaction components are incomplete.");
        Camera camera = player.GetComponentInChildren<Camera>(true);
        if (camera == null || camera.transform.parent == null || camera.transform.parent.name != "Camera Pivot")
            throw new InvalidOperationException("Gallery first-person camera is not parented to its pivot.");
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(sceneRoot) > 0)
                throw new InvalidOperationException("Gallery scene contains a missing script under: " + sceneRoot.name);
    }

    private static GameObject FindUnique(Scene scene, string name)
    {
        GameObject[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"Expected one '{name}' object but found {matches.Length}.");
        return matches[0];
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
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName)
            ?? throw new InvalidOperationException("Missing serialized field: " + propertyName);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReferenceArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName)
            ?? throw new InvalidOperationException("Missing serialized field: " + propertyName);
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct ExhibitContent
    {
        public ExhibitContent(string title, string[] lines)
        {
            Title = title;
            Lines = lines;
        }

        public string Title { get; }
        public string[] Lines { get; }
    }
}
