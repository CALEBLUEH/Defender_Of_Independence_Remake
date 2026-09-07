using System;
using System.Linq;
using DefenderOfIndependence.Level2;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelTwoGameplaySetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string PrefabPath = "Assets/Level2/Prefabs/Player/Level Two Player.prefab";
    private const string GameplayRootName = "Level 2 Gameplay";

    private sealed class DoorSpec
    {
        public string DoorName;
        public string EnterMarkerName;
        public string ExitMarkerName;
        public string DisplayName;
    }

    private static readonly DoorSpec[] DoorSpecs =
    {
        new DoorSpec
        {
            DoorName = "TunkuAbdulRahmanDoor",
            EnterMarkerName = "TunkuAbdulRahmanRoomSpawnPoint(Enter)",
            ExitMarkerName = "TunkuAbdulRahmanRoomSpawnPoint(Exit)",
            DisplayName = "TUNKU ABDUL RAHMAN'S ROOM"
        },
        new DoorSpec
        {
            DoorName = "AlanLennox-BoydDoor",
            EnterMarkerName = "AlanLennox-BoydRoomSpawnPoint(Enter)",
            ExitMarkerName = "AlanLennox-BoydRoomSpawnPoint(Exit)",
            DisplayName = "ALAN LENNOX-BOYD'S ROOM"
        },
        new DoorSpec
        {
            DoorName = "RadioStationDoor",
            EnterMarkerName = "RadioStationRoomSpawnPoint(Enter)",
            ExitMarkerName = "RadioStationRoomSpawnPoint(Exit)",
            DisplayName = "THE RADIO STATION ROOM"
        }
    };

    [MenuItem("Project Tools/Level 2/Build Player and Door Transitions")]
    public static void Build()
    {
        EnsureFolder("Assets/Level2/Prefabs/Player");
        BuildPlayerPrefab();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform initialSpawn = FindRoot(scene, "PlayerInitialSpawnPoint");
        Transform doorRoot = FindRoot(scene, "Door");
        if (initialSpawn == null || doorRoot == null)
        {
            throw new InvalidOperationException("Level 2 needs the PlayerInitialSpawnPoint and Door root objects before gameplay can be built.");
        }

        Transform oldGameplay = FindRoot(scene, GameplayRootName);
        if (oldGameplay != null)
        {
            UnityEngine.Object.DestroyImmediate(oldGameplay.gameObject);
        }

        GameObject gameplayRoot = new GameObject(GameplayRootName);
        SceneManager.MoveGameObjectToScene(gameplayRoot, scene);

        LevelTwoDoorTransition[] doors = DoorSpecs.Select(spec => ConfigureDoor(scene, doorRoot, spec)).ToArray();
        GameObject player = InstantiatePlayer(gameplayRoot.transform, initialSpawn);
        CreateTransitionUi(gameplayRoot.transform, out LevelTwoScreenFader fader, out TMP_Text prompt);
        ConfigureInteractor(player, fader, prompt, doors);
        EnsureEventSystem(gameplayRoot.transform);
        DisableOldMainCamera(scene, gameplayRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Validate(scene, player, initialSpawn, doors, fader, prompt);
        Debug.Log("LEVEL_TWO_GAMEPLAY_SETUP_OK: model-less WASD player, initial spawn, three C-interaction doors, and fade UI are wired.");
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }

            throw;
        }
    }

    public static void ValidatePersistedSetupFromCommandLine()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform gameplayRoot = FindRoot(scene, GameplayRootName);
            Transform initialSpawn = FindRoot(scene, "PlayerInitialSpawnPoint");
            if (gameplayRoot == null || initialSpawn == null)
            {
                throw new InvalidOperationException("The persisted Level 2 gameplay root or initial spawn point is missing.");
            }

            LevelTwoFirstPersonController player = gameplayRoot.GetComponentInChildren<LevelTwoFirstPersonController>(true);
            LevelTwoDoorInteractor interactor = gameplayRoot.GetComponentInChildren<LevelTwoDoorInteractor>(true);
            LevelTwoScreenFader fader = gameplayRoot.GetComponentInChildren<LevelTwoScreenFader>(true);
            TMP_Text prompt = gameplayRoot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.name == "Prompt Text");
            LevelTwoDoorTransition[] doors = DoorSpecs
                .Select(spec => FindDirectChild(FindRoot(scene, "Door"), spec.DoorName)?.GetComponent<LevelTwoDoorTransition>())
                .ToArray();

            Validate(scene, player != null ? player.gameObject : null, initialSpawn, doors, fader, prompt);
            SerializedObject playerData = new SerializedObject(player);
            if (playerData.FindProperty("initialSpawnPoint").objectReferenceValue != initialSpawn)
            {
                throw new InvalidOperationException("The persisted player does not reference PlayerInitialSpawnPoint.");
            }

            SerializedObject interactorData = new SerializedObject(interactor);
            if (interactorData.FindProperty("doors").arraySize != DoorSpecs.Length ||
                interactorData.FindProperty("screenFader").objectReferenceValue == null ||
                interactorData.FindProperty("interactionPrompt").objectReferenceValue == null)
            {
                throw new InvalidOperationException("The persisted Level 2 door interactor references are incomplete.");
            }

            Debug.Log("LEVEL_TWO_PERSISTENCE_VALIDATION_OK: saved scene references and generated prefab reload correctly.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }

            throw;
        }
    }

    private static void BuildPlayerPrefab()
    {
        GameObject root = new GameObject("Level Two Player");
        try
        {
            root.tag = "Player";
            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.3f;

            Transform pivot = new GameObject("Camera Pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 1.65f, 0f);

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            LevelTwoFirstPersonController controller = root.AddComponent<LevelTwoFirstPersonController>();
            LevelTwoDoorInteractor interactor = root.AddComponent<LevelTwoDoorInteractor>();
            SetObjectReference(controller, "cameraPivot", pivot);
            SetObjectReference(interactor, "playerController", controller);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject InstantiatePlayer(Transform parent, Transform initialSpawn)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Could not load generated player prefab at {PrefabPath}.");
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Level Two Player";
        player.transform.SetParent(parent, true);
        player.transform.SetPositionAndRotation(initialSpawn.position, initialSpawn.rotation);

        LevelTwoFirstPersonController controller = player.GetComponent<LevelTwoFirstPersonController>();
        SetObjectReference(controller, "initialSpawnPoint", initialSpawn);
        return player;
    }

    private static LevelTwoDoorTransition ConfigureDoor(Scene scene, Transform doorRoot, DoorSpec spec)
    {
        Transform door = FindDirectChild(doorRoot, spec.DoorName);
        Transform enter = FindRoot(scene, spec.EnterMarkerName);
        Transform exit = FindRoot(scene, spec.ExitMarkerName);
        if (door == null || enter == null || exit == null)
        {
            throw new InvalidOperationException($"Missing Level 2 door wiring object for {spec.DisplayName}.");
        }

        LevelTwoDoorTransition transition = door.GetComponent<LevelTwoDoorTransition>();
        if (transition == null)
        {
            transition = door.gameObject.AddComponent<LevelTwoDoorTransition>();
        }

        SerializedObject serialized = new SerializedObject(transition);
        serialized.FindProperty("roomDisplayName").stringValue = spec.DisplayName;
        serialized.FindProperty("enterSpawnPoint").objectReferenceValue = enter;
        serialized.FindProperty("exitSpawnPoint").objectReferenceValue = exit;
        serialized.FindProperty("useSpawnPointRotation").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return transition;
    }

    private static void CreateTransitionUi(Transform parent, out LevelTwoScreenFader fader, out TMP_Text prompt)
    {
        GameObject canvasObject = new GameObject("Level 2 Transition UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject promptBackground = CreateUiObject("Door Interaction Prompt", canvasObject.transform);
        Image background = promptBackground.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.7f);
        SetRect(promptBackground.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(820f, 54f));

        GameObject promptObject = CreateUiObject("Prompt Text", promptBackground.transform);
        TextMeshProUGUI promptText = promptObject.AddComponent<TextMeshProUGUI>();
        promptText.text = "PRESS C TO ENTER";
        promptText.fontSize = 26f;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.enableWordWrapping = false;
        Stretch(promptObject.GetComponent<RectTransform>(), new Vector2(18f, 6f), new Vector2(-18f, -6f));
        prompt = promptText;
        promptBackground.SetActive(false);

        GameObject fadeObject = CreateUiObject("Black Fade", canvasObject.transform);
        Image fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        Stretch(fadeObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        CanvasGroup group = fadeObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        fader = fadeObject.AddComponent<LevelTwoScreenFader>();
    }

    private static void ConfigureInteractor(GameObject player, LevelTwoScreenFader fader, TMP_Text prompt, LevelTwoDoorTransition[] doors)
    {
        LevelTwoDoorInteractor interactor = player.GetComponent<LevelTwoDoorInteractor>();
        SerializedObject serialized = new SerializedObject(interactor);
        serialized.FindProperty("screenFader").objectReferenceValue = fader;
        serialized.FindProperty("interactionPrompt").objectReferenceValue = prompt;
        SerializedProperty doorArray = serialized.FindProperty("doors");
        doorArray.arraySize = doors.Length;
        for (int i = 0; i < doors.Length; i++)
        {
            doorArray.GetArrayElementAtIndex(i).objectReferenceValue = doors[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem(Transform parent)
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(parent, false);
    }

    private static void DisableOldMainCamera(Scene scene, Transform gameplayRoot)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Camera" && !root.transform.IsChildOf(gameplayRoot))
            {
                root.SetActive(false);
            }
        }
    }

    private static void Validate(Scene scene, GameObject player, Transform initialSpawn, LevelTwoDoorTransition[] doors, LevelTwoScreenFader fader, TMP_Text prompt)
    {
        if (player == null || player.GetComponent<CharacterController>() == null ||
            player.GetComponent<LevelTwoFirstPersonController>() == null || player.GetComponent<LevelTwoDoorInteractor>() == null)
        {
            throw new InvalidOperationException("Generated Level 2 player is incomplete.");
        }

        if (initialSpawn == null || doors.Length != 3 || doors.Any(door => door == null || door.EnterSpawnPoint == null || door.ExitSpawnPoint == null))
        {
            throw new InvalidOperationException("One or more Level 2 spawn/door references are missing.");
        }

        foreach (LevelTwoDoorTransition door in doors)
        {
            if (door.GetDestination(door.ExitSpawnPoint.position) != door.EnterSpawnPoint ||
                door.GetDestination(door.EnterSpawnPoint.position) != door.ExitSpawnPoint)
            {
                throw new InvalidOperationException($"Door direction check failed for {door.RoomDisplayName}.");
            }
        }

        if (fader == null || prompt == null || FindRoot(scene, GameplayRootName) == null)
        {
            throw new InvalidOperationException("Generated Level 2 transition UI is incomplete.");
        }

        if (player.GetComponentsInChildren<Renderer>(true).Length != 0)
        {
            throw new InvalidOperationException("The Level 2 player must remain model-less.");
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Transform FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name)?.transform;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static void SetObjectReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            throw new InvalidOperationException($"Could not find serialized field {fieldName} on {target.name}.");
        }

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
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
