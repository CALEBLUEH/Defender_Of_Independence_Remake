using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelThreeOpeningSequenceSetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string Camera1ControllerPath = "Assets/Level3/Animation/Camera1.controller";
    private const string Camera2ControllerPath = "Assets/Level3/Animation/Camera2.controller";
    private const string Camera3ControllerPath = "Assets/Level3/Animation/Camera3.controller";
    private const string TunkuControllerPath = "Assets/Level3/Animation/Tunku Abdul Rahman.controller";
    private const string Camera1ClipPath = "Assets/Level3/Animation/CameraAnimation.anim";
    private const string Camera2ClipPath = "Assets/Level3/Animation/CameraAnimation2.anim";
    private const string Camera3ClipPath = "Assets/Level3/Animation/CameraAnimation3.anim";
    private const string TunkuClipPath = "Assets/Level3/Animation/TunkuAbdulRahmanAnimation.anim";

    [MenuItem("Project Tools/Level 3/Set Up Opening Camera Sequence")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera1 = RequireNamedComponent<Camera>(scene, "Camera1");
        Camera camera2 = RequireNamedComponent<Camera>(scene, "Camera2");
        Camera camera3 = RequireNamedComponent<Camera>(scene, "Camera3");
        GameObject tunku = RequireNamedObject(scene, "Tunku Abdul Rahman");

        Animator camera1Animator = ConfigureAnimator(camera1.gameObject, Camera1ControllerPath);
        Animator camera2Animator = ConfigureAnimator(camera2.gameObject, Camera2ControllerPath);
        Animator camera3Animator = ConfigureAnimator(camera3.gameObject, Camera3ControllerPath);
        Animator tunkuAnimator = ConfigureAnimator(tunku, TunkuControllerPath);

        RemoveIncompatibleLegacyAnimation(camera1.gameObject);
        RemoveIncompatibleLegacyAnimation(camera2.gameObject);
        RemoveIncompatibleLegacyAnimation(camera3.gameObject);

        GameObject sequenceObject = FindNamedObject(scene, "Level 3 Opening Sequence");
        if (sequenceObject == null)
        {
            sequenceObject = new GameObject("Level 3 Opening Sequence");
            SceneManager.MoveGameObjectToScene(sequenceObject, scene);
        }

        LevelThreeOpeningSequence sequence = sequenceObject.GetComponent<LevelThreeOpeningSequence>();
        if (sequence == null) sequence = sequenceObject.AddComponent<LevelThreeOpeningSequence>();

        CanvasGroup fadeOverlay = CreateOrUpdateFadeOverlay(scene);
        ConfigureSequence(sequence, camera1, camera2, camera3, camera1Animator, camera2Animator,
            camera3Animator, tunkuAnimator, fadeOverlay);

        camera1.gameObject.SetActive(true);
        camera2.gameObject.SetActive(false);
        camera3.gameObject.SetActive(false);
        camera1.gameObject.tag = "MainCamera";
        camera2.gameObject.tag = "Untagged";
        camera3.gameObject.tag = "Untagged";
        camera1Animator.speed = 0f;
        camera2Animator.speed = 0f;
        camera3Animator.speed = 0f;
        tunkuAnimator.speed = 0f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("LEVEL3_OPENING_SETUP_OK: Camera1 -> Camera2 + Tunku -> Camera3 is wired with an authored black fade overlay.");
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

    [MenuItem("Project Tools/Level 3/Inspect Opening Camera Objects")]
    public static void Inspect()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (Camera camera in GetSceneComponents<Camera>(scene))
        {
            Animator animator = camera.GetComponent<Animator>();
            UnityEngine.Animation legacyAnimation = camera.GetComponent<UnityEngine.Animation>();
            Debug.Log($"LEVEL3_CAMERA: {GetPath(camera.transform)} | active={camera.gameObject.activeSelf} | " +
                      $"cameraEnabled={camera.enabled} | animator={(animator != null ? animator.runtimeAnimatorController?.name : "none")} | " +
                      $"legacyAnimation={(legacyAnimation != null ? legacyAnimation.clip?.name : "none")}");
        }
    }

    public static void InspectFromCommandLine()
    {
        try
        {
            Inspect();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static Animator ConfigureAnimator(GameObject target, string controllerPath)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) throw new InvalidOperationException($"Missing Animator Controller: {controllerPath}");

        Animator animator = target.GetComponent<Animator>();
        if (animator == null) animator = target.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return animator;
    }

    private static void RemoveIncompatibleLegacyAnimation(GameObject target)
    {
        UnityEngine.Animation legacyAnimation = target.GetComponent<UnityEngine.Animation>();
        if (legacyAnimation != null) UnityEngine.Object.DestroyImmediate(legacyAnimation);
    }

    private static CanvasGroup CreateOrUpdateFadeOverlay(Scene scene)
    {
        GameObject canvasObject = FindNamedObject(scene, "Level 3 Opening Fade");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Level 3 Opening Fade", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
        }

        canvasObject.SetActive(true);
        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = GetOrAdd<CanvasGroup>(canvasObject);
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject imageObject = FindDirectChild(canvasObject.transform, "Black Fade");
        if (imageObject == null)
        {
            imageObject = new GameObject("Black Fade", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
        }

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        return group;
    }

    private static void ConfigureSequence(LevelThreeOpeningSequence sequence, Camera camera1, Camera camera2,
        Camera camera3, Animator camera1Animator, Animator camera2Animator, Animator camera3Animator,
        Animator tunkuAnimator, CanvasGroup fadeOverlay)
    {
        SerializedObject serialized = new SerializedObject(sequence);
        SetObject(serialized, "camera1", camera1);
        SetObject(serialized, "camera2", camera2);
        SetObject(serialized, "camera3", camera3);
        SetObject(serialized, "camera1Animator", camera1Animator);
        SetObject(serialized, "camera2Animator", camera2Animator);
        SetObject(serialized, "camera3Animator", camera3Animator);
        SetObject(serialized, "tunkuAbdulRahmanAnimator", tunkuAnimator);
        SetObject(serialized, "camera1Clip", LoadClip(Camera1ClipPath));
        SetObject(serialized, "camera2Clip", LoadClip(Camera2ClipPath));
        SetObject(serialized, "camera3Clip", LoadClip(Camera3ClipPath));
        SetObject(serialized, "tunkuAbdulRahmanClip", LoadClip(TunkuClipPath));
        SetObject(serialized, "fadeOverlay", fadeOverlay);
        serialized.FindProperty("sceneEntryFadeInDuration").floatValue = 2f;
        serialized.FindProperty("transitionHalfDuration").floatValue = 1f;
        serialized.FindProperty("playbackSpeed").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) throw new InvalidOperationException($"Missing Animation Clip: {path}");
        return clip;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException($"Missing serialized field: {propertyName}");
        property.objectReferenceValue = value;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static T RequireNamedComponent<T>(Scene scene, string name) where T : Component
    {
        GameObject target = RequireNamedObject(scene, name);
        T component = target.GetComponent<T>();
        if (component == null) throw new InvalidOperationException($"'{name}' has no {typeof(T).Name} component.");
        return component;
    }

    private static GameObject RequireNamedObject(Scene scene, string name)
    {
        GameObject[] matches = GetSceneTransforms(scene)
            .Where(item => item.name == name)
            .Select(item => item.gameObject)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected exactly one active or inactive scene object named '{name}', found {matches.Length}.");
        return matches[0];
    }

    private static GameObject FindNamedObject(Scene scene, string name)
    {
        return GetSceneTransforms(scene).FirstOrDefault(item => item.name == name)?.gameObject;
    }

    private static Transform[] GetSceneTransforms(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
    }

    private static T[] GetSceneComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name) return child.gameObject;
        }
        return null;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
