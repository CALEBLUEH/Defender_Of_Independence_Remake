using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderOfIndependence.Level2;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelTwoDaySetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string GameplayRootName = "Level 2 Gameplay";
    private const string DaySystemName = "Level 2 Day System";
    private const string TunkuModelPath = "Assets/Level2/Art/Characters/TunkuAbdulRahman/Model/Tunku Abdul Rahman Baju Muskat Berjalan.fbx";
    private const string TunkuTexturePath = "Assets/Level2/Art/Characters/TunkuAbdulRahman/Textures/texture_0.png";
    private const string TunkuMaterialPath = "Assets/Level2/Art/Characters/TunkuAbdulRahman/Materials/Tunku Abdul Rahman.mat";
    private const string AlanModelPath = "Assets/Level2/Art/Characters/AlanLennoxBoyd/Model/scene.gltf";
    private const string TunkuPrefabPath = "Assets/Level2/Prefabs/Characters/Tunku Abdul Rahman.prefab";
    private const string AlanPrefabPath = "Assets/Level2/Prefabs/Characters/Alan Lennox-Boyd.prefab";
    private const string TitleFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
    private const string BodyFontPath = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private sealed class CharacterPlacement
    {
        public string MarkerName;
        public string InstanceName;
        public int Day;
        public string PrefabPath;
    }

    private static readonly CharacterPlacement[] Placements =
    {
        new CharacterPlacement { MarkerName = "TunkuAbdulRahman(Day1)", InstanceName = "Tunku Abdul Rahman - Day 1", Day = 1, PrefabPath = TunkuPrefabPath },
        new CharacterPlacement { MarkerName = "AlanLennox-Boyd(Day2)", InstanceName = "Alan Lennox-Boyd - Day 2", Day = 2, PrefabPath = AlanPrefabPath },
        new CharacterPlacement { MarkerName = "AlanLennox-Boyd(Day3)", InstanceName = "Alan Lennox-Boyd - Day 3", Day = 3, PrefabPath = AlanPrefabPath },
        new CharacterPlacement { MarkerName = "TunkuAbdulRahman(Day5)", InstanceName = "Tunku Abdul Rahman - Day 5", Day = 5, PrefabPath = TunkuPrefabPath },
        new CharacterPlacement { MarkerName = "TunkuAbdulRahman(Day6)", InstanceName = "Tunku Abdul Rahman - Day 6", Day = 6, PrefabPath = TunkuPrefabPath },
        new CharacterPlacement { MarkerName = "AlanLennox-Boyd(Day6)", InstanceName = "Alan Lennox-Boyd - Day 6", Day = 6, PrefabPath = AlanPrefabPath }
    };

    private static readonly (int day, string title, string recommendation)[] Briefings =
    {
        (1, "PREPARATION", "Tunku Abdul Rahman wants to meet you in the Planning Room."),
        (2, "INTERNAL SECURITY", "Alan Lennox-Boyd wants to meet you in the British Office."),
        (3, "FINANCE AND DEVELOPMENT", "Alan Lennox-Boyd wants to meet you in the British Office."),
        (4, "THE VOICE OF THE PEOPLE", "The radio host is waiting for you in the Radio Station Room."),
        (5, "DEFENSE AND FINAL POSITION", "Tunku Abdul Rahman wants to meet you in the Planning Room."),
        (6, "THE CONSTITUTIONAL CONFERENCE", "Tunku Abdul Rahman and Alan Lennox-Boyd await you in the British Office.")
    };

    [MenuItem("Project Tools/Level 2/Build Day System and Characters")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder("Assets/Level2/Art/Characters/TunkuAbdulRahman/Materials");
        EnsureFolder("Assets/Level2/Prefabs/Characters");

        ConfigureTunkuModel();
        Material tunkuMaterial = CreateTunkuMaterial();
        GameObject tunkuPrefab = CreateCharacterPrefab(TunkuModelPath, TunkuPrefabPath, "Tunku Abdul Rahman", tunkuMaterial);
        GameObject alanPrefab = CreateCharacterPrefab(AlanModelPath, AlanPrefabPath, "Alan Lennox-Boyd", null);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform gameplayRoot = FindUniqueTransform(scene, GameplayRootName);
        GameObject player = gameplayRoot.GetComponentInChildren<LevelTwoFirstPersonController>(true)?.gameObject
                            ?? throw new InvalidOperationException("Level 2 Gameplay has no first-person player.");
        LevelTwoDoorInteractor interactor = player.GetComponent<LevelTwoDoorInteractor>()
                                           ?? throw new InvalidOperationException("Level 2 player has no interaction component.");
        TMP_Text sharedPrompt = gameplayRoot.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.name == "Prompt Text")
            ?? throw new InvalidOperationException("Level 2 interaction prompt is missing.");

        Transform oldDaySystem = FindDirectChild(gameplayRoot, DaySystemName);
        if (oldDaySystem != null)
        {
            UnityEngine.Object.DestroyImmediate(oldDaySystem.gameObject);
        }

        RemovePreviousPlacements(scene);
        GameObject daySystemObject = new GameObject(DaySystemName);
        daySystemObject.transform.SetParent(gameplayRoot, false);
        LevelTwoDayController controller = daySystemObject.AddComponent<LevelTwoDayController>();

        CreateDayUi(daySystemObject.transform, out TMP_Text dayText, out TMP_Text recommendationText,
            out CanvasGroup dayCard, out TMP_Text cardDayText, out TMP_Text cardTitleText);

        List<LevelTwoScheduledCharacter> scheduledCharacters = new List<LevelTwoScheduledCharacter>();
        foreach (CharacterPlacement placement in Placements)
        {
            GameObject prefab = placement.PrefabPath == TunkuPrefabPath ? tunkuPrefab : alanPrefab;
            scheduledCharacters.Add(CreateScheduledCharacter(scene, placement, prefab));
        }

        ConfigureDayController(controller, player.GetComponent<LevelTwoFirstPersonController>(), scheduledCharacters,
            dayText, recommendationText, dayCard, cardDayText, cardTitleText);

        Transform clockTransform = FindUniqueTransform(scene, "NextDayClock");
        LevelTwoNextDayClock clock = clockTransform.GetComponent<LevelTwoNextDayClock>() ??
                                     clockTransform.gameObject.AddComponent<LevelTwoNextDayClock>();
        SetReference(clock, "dayController", controller);
        SetReference(interactor, "nextDayClock", clock);

        foreach (LevelTwoScheduledCharacter character in scheduledCharacters)
        {
            character.ApplyDay(1);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ValidatePersistedSetup(scene, controller, clock, interactor, scheduledCharacters);
        Debug.Log("LEVEL_TWO_DAY_SETUP_OK: six-day clock, HUD, day cards, recommendations, and scheduled characters are wired.");
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
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
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform dayRoot = FindUniqueTransform(scene, DaySystemName);
            LevelTwoDayController controller = dayRoot.GetComponent<LevelTwoDayController>();
            LevelTwoNextDayClock clock = FindUniqueTransform(scene, "NextDayClock").GetComponent<LevelTwoNextDayClock>();
            LevelTwoDoorInteractor interactor = FindUniqueTransform(scene, GameplayRootName)
                .GetComponentInChildren<LevelTwoDoorInteractor>(true);
            List<LevelTwoScheduledCharacter> characters = Placements
                .Select(placement => FindUniqueTransform(scene, placement.MarkerName).Find(placement.InstanceName)?.GetComponent<LevelTwoScheduledCharacter>())
                .ToList();
            ValidatePersistedSetup(scene, controller, clock, interactor, characters);
            Debug.Log("LEVEL_TWO_DAY_PERSISTENCE_OK: scene reload retained the clock, HUD, briefings, and six character placements.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void ConfigureTunkuModel()
    {
        AssetDatabase.ImportAsset(TunkuModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ModelImporter importer = AssetImporter.GetAtPath(TunkuModelPath) as ModelImporter
                                 ?? throw new FileNotFoundException("Tunku FBX was not imported.");
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.meshCompression = ModelImporterMeshCompression.Low;
        importer.addCollider = false;
        importer.SaveAndReimport();
    }

    private static Material CreateTunkuMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? throw new InvalidOperationException("URP/Lit shader is unavailable.");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TunkuTexturePath)
                            ?? throw new FileNotFoundException("Tunku base-color texture is missing.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TunkuMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Tunku Abdul Rahman" };
            AssetDatabase.CreateAsset(material, TunkuMaterialPath);
        }

        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.32f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCharacterPrefab(string modelPath, string prefabPath, string displayName, Material overrideMaterial)
    {
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath)
                           ?? throw new FileNotFoundException("Imported character model is missing: " + modelPath);
        GameObject root = new GameObject(displayName);
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject
                                ?? throw new InvalidOperationException("Could not instantiate character model: " + modelPath);
            visual.name = displayName + " Visual";
            visual.transform.SetParent(root.transform, false);
            RemoveImportedExtras(visual);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(displayName + " has no renderers.");
            }

            if (overrideMaterial != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = Enumerable.Repeat(overrideMaterial, renderer.sharedMaterials.Length).ToArray();
                    renderer.sharedMaterials = materials;
                }
            }

            NormalizeCharacter(visual, 1.75f);
            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath)
                   ?? throw new InvalidOperationException("Could not save character prefab: " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void RemoveImportedExtras(GameObject visual)
    {
        foreach (Camera camera in visual.GetComponentsInChildren<Camera>(true))
        {
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        foreach (Light light in visual.GetComponentsInChildren<Light>(true))
        {
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
    }

    private static void NormalizeCharacter(GameObject visual, float targetHeight)
    {
        Bounds bounds = CalculateBounds(visual);
        if (bounds.size.y <= 0.001f)
        {
            throw new InvalidOperationException(visual.name + " has invalid renderer bounds.");
        }

        float scale = targetHeight / bounds.size.y;
        visual.transform.localScale *= scale;
        bounds = CalculateBounds(visual);
        visual.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static LevelTwoScheduledCharacter CreateScheduledCharacter(Scene scene, CharacterPlacement placement, GameObject prefab)
    {
        Transform marker = FindUniqueTransform(scene, placement.MarkerName);
        GameObject wrapper = new GameObject(placement.InstanceName);
        wrapper.transform.SetParent(marker, false);
        LevelTwoScheduledCharacter scheduled = wrapper.AddComponent<LevelTwoScheduledCharacter>();

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
        visual.name = prefab.name;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        SerializedObject serialized = new SerializedObject(scheduled);
        serialized.FindProperty("activeDay").intValue = placement.Day;
        serialized.FindProperty("characterVisual").objectReferenceValue = visual;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return scheduled;
    }

    private static void ConfigureDayController(LevelTwoDayController controller, LevelTwoFirstPersonController player,
        IReadOnlyList<LevelTwoScheduledCharacter> characters, TMP_Text dayText, TMP_Text recommendationText,
        CanvasGroup card, TMP_Text cardDayText, TMP_Text cardTitleText)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("startingDay").intValue = 1;
        serialized.FindProperty("finalDay").intValue = 6;
        serialized.FindProperty("playerController").objectReferenceValue = player;
        serialized.FindProperty("currentDayText").objectReferenceValue = dayText;
        serialized.FindProperty("recommendationText").objectReferenceValue = recommendationText;
        serialized.FindProperty("dayCard").objectReferenceValue = card;
        serialized.FindProperty("dayCardDayText").objectReferenceValue = cardDayText;
        serialized.FindProperty("dayCardTitleText").objectReferenceValue = cardTitleText;

        SerializedProperty briefingArray = serialized.FindProperty("briefings");
        briefingArray.arraySize = Briefings.Length;
        for (int i = 0; i < Briefings.Length; i++)
        {
            SerializedProperty item = briefingArray.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("day").intValue = Briefings[i].day;
            item.FindPropertyRelative("title").stringValue = Briefings[i].title;
            item.FindPropertyRelative("recommendation").stringValue = Briefings[i].recommendation;
        }

        SerializedProperty characterArray = serialized.FindProperty("scheduledCharacters");
        characterArray.arraySize = characters.Count;
        for (int i = 0; i < characters.Count; i++)
        {
            characterArray.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDayUi(Transform parent, out TMP_Text dayText, out TMP_Text recommendationText,
        out CanvasGroup dayCard, out TMP_Text cardDayText, out TMP_Text cardTitleText)
    {
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
        GameObject canvasObject = new GameObject("Day HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject taskPanel = CreateUiObject("Today's Appointment", canvasObject.transform);
        Image taskBackground = taskPanel.AddComponent<Image>();
        taskBackground.color = new Color(0.025f, 0.035f, 0.055f, 0.9f);
        SetRect(taskPanel.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-294f, -118f), new Vector2(540f, 196f));

        dayText = CreateText(taskPanel.transform, "Current Day", titleFont, "DAY 1 / 6", 30f, Color.white, TextAlignmentOptions.Center);
        SetRect(dayText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(500f, 48f));

        TMP_Text heading = CreateText(taskPanel.transform, "Appointment Heading", bodyFont, "TODAY'S APPOINTMENT", 18f,
            new Color(0.83f, 0.69f, 0.35f), TextAlignmentOptions.Center);
        SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(500f, 32f));

        recommendationText = CreateText(taskPanel.transform, "Recommendation", bodyFont,
            Briefings[0].recommendation, 21f, new Color(0.94f, 0.94f, 0.92f), TextAlignmentOptions.Center);
        SetRect(recommendationText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 51f), new Vector2(492f, 76f));
        recommendationText.textWrappingMode = TextWrappingModes.Normal;

        GameObject cardObject = CreateUiObject("Day Transition Card", canvasObject.transform);
        Image cardBackground = cardObject.AddComponent<Image>();
        cardBackground.color = new Color(0.005f, 0.007f, 0.012f, 1f);
        Stretch(cardObject.GetComponent<RectTransform>());
        dayCard = cardObject.AddComponent<CanvasGroup>();
        dayCard.alpha = 0f;
        dayCard.blocksRaycasts = false;
        dayCard.interactable = false;

        cardDayText = CreateText(cardObject.transform, "Day Number", titleFont, "DAY 1", 88f, Color.white, TextAlignmentOptions.Center);
        SetRect(cardDayText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(1200f, 120f));
        cardTitleText = CreateText(cardObject.transform, "Day Theme", bodyFont, Briefings[0].title, 34f,
            new Color(0.83f, 0.69f, 0.35f), TextAlignmentOptions.Center);
        SetRect(cardTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(1400f, 60f));
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string value, float size,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void RemovePreviousPlacements(Scene scene)
    {
        foreach (CharacterPlacement placement in Placements)
        {
            Transform marker = FindUniqueTransform(scene, placement.MarkerName);
            Transform existing = marker.Find(placement.InstanceName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static Transform FindUniqueTransform(Scene scene, string name)
    {
        Transform[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name == name)
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"Expected exactly one '{name}' in Scene_Level2, found {matches.Length}.");
        }

        return matches[0];
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
        }

        return null;
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName)
                                      ?? throw new InvalidOperationException($"Missing field {fieldName} on {target.name}.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void ValidatePersistedSetup(Scene scene, LevelTwoDayController controller, LevelTwoNextDayClock clock,
        LevelTwoDoorInteractor interactor, IReadOnlyList<LevelTwoScheduledCharacter> characters)
    {
        if (controller == null || clock == null || interactor == null || clock.DayController != controller)
        {
            throw new InvalidOperationException("Level 2 day-controller or clock references are incomplete.");
        }

        if (characters.Count != 6 || characters.Any(character => character == null || character.CharacterVisual == null))
        {
            throw new InvalidOperationException("One or more scheduled character placements are missing.");
        }

        int[] expectedDays = { 1, 2, 3, 5, 6, 6 };
        if (!characters.Select(character => character.ActiveDay).OrderBy(day => day).SequenceEqual(expectedDays))
        {
            throw new InvalidOperationException("Scheduled character day assignments do not match the design.");
        }

        foreach (string prefabPath in new[] { TunkuPrefabPath, AlanPrefabPath })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Renderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            if (prefab == null || renderers.Length == 0)
            {
                throw new InvalidOperationException("Character prefab is missing or has no renderer: " + prefabPath);
            }

            if (renderers.Any(renderer => renderer.sharedMaterials.Any(material =>
                    material == null || material.shader == null || !material.shader.isSupported)))
            {
                throw new InvalidOperationException("Character prefab has a missing or unsupported material: " + prefabPath);
            }

            float height = CalculateBounds(prefab).size.y;
            if (height < 1.7f || height > 1.8f)
            {
                throw new InvalidOperationException($"Character prefab height is outside the expected range: {prefabPath} ({height:0.00}m).");
            }
        }

        SerializedObject interactorData = new SerializedObject(interactor);
        if (interactorData.FindProperty("nextDayClock").objectReferenceValue != clock)
        {
            throw new InvalidOperationException("The player interactor is not linked to NextDayClock.");
        }

        SerializedObject controllerData = new SerializedObject(controller);
        if (controllerData.FindProperty("briefings").arraySize != 6 ||
            controllerData.FindProperty("scheduledCharacters").arraySize != 6 ||
            controllerData.FindProperty("currentDayText").objectReferenceValue == null ||
            controllerData.FindProperty("recommendationText").objectReferenceValue == null ||
            controllerData.FindProperty("dayCard").objectReferenceValue == null)
        {
            throw new InvalidOperationException("The day HUD or briefing references are incomplete.");
        }
    }
}
