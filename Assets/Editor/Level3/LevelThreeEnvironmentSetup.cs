using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LevelThreeEnvironmentSetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string EnvironmentRootName = "Level 3 Environment";
    private const string StadiumInstanceName = "Stadium (Position and Scale Here)";

    private const string StadiumModelPath = "Assets/Level3/Art/Environment/Stadium/Model/Stadium.fbx";
    private const string StadiumTexturePath = "Assets/Level3/Art/Environment/Stadium/Textures/Stadium_BaseColor.jpg";
    private const string StadiumMaterialPath = "Assets/Level3/Art/Environment/Stadium/Materials/Stadium.mat";
    private const string StadiumPrefabPath = "Assets/Level3/Prefabs/Environment/Stadium.prefab";

    private const string FlagModelPath = "Assets/Level3/Art/Props/MalaysiaFlag/Model/MalaysiaFlag.gltf";
    private const string FlagTexturePath = "Assets/Level3/Art/Props/MalaysiaFlag/Textures/Malaysia_Flag_BaseColor.png";
    private const string FlagMaterialPath = "Assets/Level3/Art/Props/MalaysiaFlag/Materials/Malaysia Flag.mat";
    private const string LegacyFlagClipPath = "Assets/Level3/Art/Props/MalaysiaFlag/Malaysia Flag Wave Loop.anim";
    private const string FlagControllerPath = "Assets/Level3/Art/Props/MalaysiaFlag/Malaysia Flag.controller";
    private const string FlagPrefabPath = "Assets/Level3/Prefabs/Props/Malaysia Flag.prefab";

    private const string PodiumModelPath = "Assets/Level3/Art/Props/Podium/Model/Podium.gltf";
    private const string PodiumPrefabPath = "Assets/Level3/Prefabs/Props/Debate Podium.prefab";

    [MenuItem("Project Tools/Level 3/Import Stadium, Flag, and Podium")]
    public static void BuildFromMenu()
    {
        try
        {
            BuildAll();
            EditorUtility.DisplayDialog(
                "Level 3 Environment",
                "The stadium is in Scene_Level3. The looping Malaysia flag and podium are ready as prefabs.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Level 3 Environment", exception.Message, "OK");
            throw;
        }
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            BuildAll();
            Debug.Log("LEVEL3_ENVIRONMENT_SETUP_SUCCESS");
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
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            Debug.Log("LEVEL3_ENVIRONMENT_PERSISTENCE_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void BuildAll()
    {
        EnsureFolder("Assets/Level3/Art/Environment/Stadium/Materials");
        EnsureFolder("Assets/Level3/Art/Props/MalaysiaFlag/Materials");
        EnsureFolder("Assets/Level3/Prefabs/Environment");
        EnsureFolder("Assets/Level3/Prefabs/Props");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureStadiumModel();
        ConfigureTexture(StadiumTexturePath, 4096);
        ConfigureTexture(FlagTexturePath, 2048);
        AssetDatabase.ImportAsset(FlagModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(PodiumModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        Material stadiumMaterial = CreateLitMaterial(StadiumMaterialPath, "Stadium", StadiumTexturePath, false);
        Material flagMaterial = CreateLitMaterial(FlagMaterialPath, "Malaysia Flag", FlagTexturePath, true);
        AnimationClip flagClip = GetLoopingFlagClip();
        AnimatorController flagController = CreateFlagController(flagClip);

        GameObject stadiumPrefab = CreateStadiumPrefab(stadiumMaterial);
        CreateFlagPrefab(flagMaterial, flagController);
        CreatePodiumPrefab();

        Scene scene = PlaceStadium(stadiumPrefab);
        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log("LEVEL3_ENVIRONMENT_VALID stadium=scene flag=prefab-looping podium=prefab materials=URP");
    }

    private static void ConfigureStadiumModel()
    {
        AssetDatabase.ImportAsset(StadiumModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ModelImporter importer = AssetImporter.GetAtPath(StadiumModelPath) as ModelImporter
                                 ?? throw new FileNotFoundException("The stadium FBX is missing.", StadiumModelPath);
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.meshCompression = ModelImporterMeshCompression.Low;
        importer.addCollider = false;
        importer.generateSecondaryUV = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path, int maximumSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                                   ?? throw new FileNotFoundException("Texture is missing.", path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = path == FlagTexturePath;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = maximumSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static Material CreateLitMaterial(string path, string displayName, string texturePath, bool twoSided)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? throw new InvalidOperationException("The URP Lit shader is unavailable.");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)
                            ?? throw new FileNotFoundException("Texture is missing.", texturePath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = displayName };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", displayName == "Stadium" ? 0.18f : 0.28f);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Cull", twoSided ? (float)CullMode.Off : (float)CullMode.Back);
        material.doubleSidedGI = twoSided;
        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip GetLoopingFlagClip()
    {
        AnimationClip importedClip = AssetDatabase.LoadAllAssetsAtPath(FlagModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        if (importedClip == null)
            throw new InvalidOperationException("The supplied Malaysia flag glTF contains no imported animation clip.");
        if (!importedClip.isLooping)
            throw new InvalidOperationException("glTFast did not import the supplied flag animation as a loop.");

        // Older setup revisions copied this dense clip and expanded it by more than 100 MB.
        // glTFast already imports Mecanim clips with loopTime enabled, so keep the compact source sub-asset.
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(LegacyFlagClipPath) != null)
            AssetDatabase.DeleteAsset(LegacyFlagClipPath);
        return importedClip;
    }

    private static AnimatorController CreateFlagController(AnimationClip clip)
    {
        AssetDatabase.DeleteAsset(FlagControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(FlagControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.AddState("Wave Loop");
        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateStadiumPrefab(Material material)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(StadiumModelPath)
                           ?? throw new FileNotFoundException("The imported stadium model is missing.", StadiumModelPath);
        GameObject root = new GameObject("Stadium");
        try
        {
            GameObject visual = InstantiateVisual(model, root.transform, "Stadium Visual");
            Renderer[] renderers = RequireRenderers(visual, "Stadium");
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            NormalizeHorizontalSizeAndGround(visual, 150f);
            SetStatic(root);
            return SavePrefab(root, StadiumPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateFlagPrefab(Material flagMaterial, RuntimeAnimatorController controller)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(FlagModelPath)
                           ?? throw new FileNotFoundException("The imported Malaysia flag model is missing.", FlagModelPath);
        GameObject root = new GameObject("Malaysia Flag");
        try
        {
            GameObject visual = InstantiateVisual(model, root.transform, "Malaysia Flag Visual");
            Renderer[] renderers = RequireRenderers(visual, "Malaysia Flag");
            int replacedSlots = 0;
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    string materialName = materials[index] == null ? string.Empty : materials[index].name;
                    bool isPole = renderer.name.IndexOf("pole", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  materialName.IndexOf("pole", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isPole) continue;
                    materials[index] = flagMaterial;
                    replacedSlots++;
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (replacedSlots == 0)
                throw new InvalidOperationException("No flag-cloth material slot was found for the Malaysia texture.");

            Animator animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            NormalizeHeightAndGround(visual, 6f);
            return SavePrefab(root, FlagPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreatePodiumPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PodiumModelPath)
                           ?? throw new FileNotFoundException("The imported podium model is missing.", PodiumModelPath);
        GameObject root = new GameObject("Debate Podium");
        try
        {
            GameObject visual = InstantiateVisual(model, root.transform, "Debate Podium Visual");
            RequireRenderers(visual, "Debate Podium");
            NormalizeHeightAndGround(visual, 1.2f);
            AddBoundsCollider(root, visual);
            return SavePrefab(root, PodiumPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject InstantiateVisual(GameObject model, Transform parent, string displayName)
    {
        GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject
                            ?? throw new InvalidOperationException("Could not instantiate model: " + model.name);
        visual.name = displayName;
        visual.transform.SetParent(parent, false);
        foreach (Camera camera in visual.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        foreach (Light light in visual.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        return visual;
    }

    private static Renderer[] RequireRenderers(GameObject visual, string displayName)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(displayName + " has no renderers.");
        return renderers;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        return PrefabUtility.SaveAsPrefabAsset(root, path)
               ?? throw new InvalidOperationException("Could not save prefab: " + path);
    }

    private static void NormalizeHorizontalSizeAndGround(GameObject visual, float targetSize)
    {
        Bounds bounds = CalculateBounds(visual);
        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontalSize <= 0.001f) throw new InvalidOperationException(visual.name + " has invalid bounds.");
        visual.transform.localScale *= targetSize / horizontalSize;
        RecenterOnGround(visual);
    }

    private static void NormalizeHeightAndGround(GameObject visual, float targetHeight)
    {
        Bounds bounds = CalculateBounds(visual);
        if (bounds.size.y <= 0.001f) throw new InvalidOperationException(visual.name + " has invalid bounds.");
        visual.transform.localScale *= targetHeight / bounds.size.y;
        RecenterOnGround(visual);
    }

    private static void RecenterOnGround(GameObject visual)
    {
        Bounds bounds = CalculateBounds(visual);
        visual.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no renderer bounds.");
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void AddBoundsCollider(GameObject root, GameObject visual)
    {
        Bounds bounds = CalculateBounds(visual);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        collider.size = bounds.size;
    }

    private static void SetStatic(GameObject root)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                  StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
    }

    private static Scene PlaceStadium(GameObject stadiumPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
        if (environment == null)
        {
            environment = new GameObject(EnvironmentRootName);
            SceneManager.MoveGameObjectToScene(environment, scene);
        }

        Transform existing = environment.transform.Find(StadiumInstanceName);
        if (existing == null)
        {
            GameObject stadium = PrefabUtility.InstantiatePrefab(stadiumPrefab, scene) as GameObject
                                 ?? throw new InvalidOperationException("Could not place the stadium prefab.");
            stadium.name = StadiumInstanceName;
            stadium.transform.SetParent(environment.transform, false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene;
    }

    private static void Validate(Scene scene)
    {
        GameObject stadiumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StadiumPrefabPath);
        GameObject flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlagPrefabPath);
        GameObject podiumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PodiumPrefabPath);
        if (stadiumPrefab == null || flagPrefab == null || podiumPrefab == null)
            throw new InvalidOperationException("One or more Level 3 prefabs are missing.");
        if (stadiumPrefab.GetComponentInChildren<Renderer>(true) == null ||
            flagPrefab.GetComponentInChildren<Renderer>(true) == null ||
            podiumPrefab.GetComponentInChildren<Renderer>(true) == null)
            throw new InvalidOperationException("One or more Level 3 prefabs have no renderer.");

        Animator animator = flagPrefab.GetComponentInChildren<Animator>(true);
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(FlagModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        if (animator?.runtimeAnimatorController == null || clip == null || !clip.isLooping)
            throw new InvalidOperationException("The Malaysia flag loop animation is not configured.");
        if (podiumPrefab.GetComponent<BoxCollider>() == null)
            throw new InvalidOperationException("The podium prefab has no placement collider.");

        GameObject environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
        Transform stadium = environment == null ? null : environment.transform.Find(StadiumInstanceName);
        if (stadium == null || PrefabUtility.GetCorrespondingObjectFromSource(stadium.gameObject) != stadiumPrefab)
            throw new InvalidOperationException("The stadium prefab is not present in Scene_Level3.");
        if (environment.transform.Find("Malaysia Flag") != null || environment.transform.Find("Debate Podium") != null)
            throw new InvalidOperationException("Flag or podium was placed even though only the stadium should be in the scene now.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) return;
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
