using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class NonGameplayModelSetup
{
    private const string MainMenuScenePath = "Assets/Scene/Scene_MainMenu.unity";
    private const string GalleryScenePath = "Assets/Scene/Scene_Gallery.unity";
    private const string SkyModelPath = "Assets/MainMenu/Art/Environment/SkyDome/scene.gltf";
    private const string TuguModelPath = "Assets/MainMenu/Art/Monuments/TuguNegara/scene.gltf";
    private const string HallModelPath = "Assets/Gallery/Art/Environment/HintzeHall/hintze-hall_UV_pack01.fbx";
    private const string FrameModelPath = "Assets/Gallery/Art/Props/PictureFrame/scene.gltf";
    private const string SkyPrefabPath = "Assets/MainMenu/Prefabs/Environment/Main Menu Sky Dome.prefab";
    private const string TuguPrefabPath = "Assets/MainMenu/Prefabs/Monuments/Tugu Negara Monument.prefab";
    private const string HallPrefabPath = "Assets/Gallery/Prefabs/Environment/Hintze Hall.prefab";
    private const string FramePrefabPath = "Assets/Gallery/Prefabs/Props/Picture Frame.prefab";
    private const string HallMaterialFolder = "Assets/Gallery/Art/Environment/HintzeHall/Materials";

    private static readonly string[] HallTexturePaths =
    {
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Deco_MetalCorona_Beauty.jpeg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Deco001_Metal001Corona_Beauty.jpeg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Murs_int_UVIM2_1Corona_Beauty_U1.jpg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Murs_int_UVIM2_1Corona_Beauty_U2.jpg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Murs_int001_UVIM2Corona_Beauty_U1.jpg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Murs_int001_UVIM2Corona_Beauty_U2.jpg",
        "Assets/Gallery/Art/Environment/HintzeHall/textures/Sol_renduCorona_Beauty_Terrazzo.jpeg"
    };

    [MenuItem("Project Tools/Non-Gameplay/Import Main Menu and Gallery Models")]
    public static void Run()
    {
        EnsureFolder("Assets/MainMenu/Prefabs/Environment");
        EnsureFolder("Assets/MainMenu/Prefabs/Monuments");
        EnsureFolder("Assets/Gallery/Prefabs/Environment");
        EnsureFolder("Assets/Gallery/Prefabs/Props");
        EnsureFolder(HallMaterialFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureGltfTextures("Assets/MainMenu/Art/Environment/SkyDome", 4096);
        ConfigureGltfTextures("Assets/MainMenu/Art/Monuments/TuguNegara", 4096);
        ConfigureGltfTextures("Assets/Gallery/Art/Props/PictureFrame", 2048);
        ConfigureHallImport();
        foreach (string texturePath in HallTexturePaths) ConfigureTexture(texturePath, 4096);
        AssetDatabase.ImportAsset(SkyModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(TuguModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(FrameModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        GameObject sky = CreateGltfPrefab(SkyModelPath, "Main Menu Sky Dome", "Sky Dome Visual", 250f, true, false, SkyPrefabPath);
        GameObject tugu = CreateGltfPrefab(TuguModelPath, "Tugu Negara Monument", "Tugu Negara Visual", 12f, false, false, TuguPrefabPath);
        GameObject frame = CreateGltfPrefab(FrameModelPath, "Picture Frame", "Picture Frame Visual", 1f, false, true, FramePrefabPath);
        GameObject hall = CreateHallPrefab();

        PlaceMainMenu(sky, tugu);
        PlaceGallery(hall);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("NON_GAMEPLAY_MODELS_SETUP_OK: Main menu sky/Tugu and Gallery hall are placed; Picture Frame is prefab-only.");
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
            Debug.Log("NON_GAMEPLAY_MODELS_PERSISTENCE_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void ConfigureHallImport()
    {
        AssetDatabase.ImportAsset(HallModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ModelImporter importer = AssetImporter.GetAtPath(HallModelPath) as ModelImporter
                                 ?? throw new FileNotFoundException("Gallery hall FBX is missing.", HallModelPath);
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.meshCompression = ModelImporterMeshCompression.Low;
        importer.addCollider = false;
        importer.generateSecondaryUV = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureGltfTextures(string root, int maximumSize)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root + "/textures" }))
            ConfigureTexture(AssetDatabase.GUIDToAssetPath(guid), maximumSize);
    }

    private static void ConfigureTexture(string path, int maximumSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                                   ?? throw new FileNotFoundException("Texture is missing.", path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = path.IndexOf("normal", StringComparison.OrdinalIgnoreCase) < 0 &&
                               path.IndexOf("metallicRoughness", StringComparison.OrdinalIgnoreCase) < 0;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = maximumSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject CreateGltfPrefab(string modelPath, string rootName, string visualName, float targetSize,
        bool normalizeHorizontal, bool addCollider, string prefabPath)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath)
                           ?? throw new FileNotFoundException("Imported glTF model is missing.", modelPath);
        GameObject root = new GameObject(rootName);
        try
        {
            GameObject visual = InstantiateVisual(model, root.transform, visualName);
            RequireRenderers(visual, rootName);
            if (normalizeHorizontal) NormalizeHorizontalSizeAndGround(visual, targetSize);
            else NormalizeHeightAndGround(visual, targetSize);
            if (addCollider) AddBoundsCollider(root, visual);
            return SavePrefab(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateHallPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HallModelPath)
                           ?? throw new FileNotFoundException("Imported gallery hall is missing.", HallModelPath);
        Material[] hallMaterials = HallTexturePaths.Select((path, index) => CreateHallMaterial(path, index))
            .Concat(new[] { CreateHallGlassMaterial() }).ToArray();
        GameObject root = new GameObject("Hintze Hall");
        try
        {
            GameObject visual = InstantiateVisual(model, root.transform, "Hintze Hall Visual");
            Renderer[] renderers = RequireRenderers(visual, "Hintze Hall");
            Dictionary<string, Material> assignedBySource = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            int fallbackIndex = 0;
            foreach (Renderer renderer in renderers)
            {
                Material[] slots = renderer.sharedMaterials;
                for (int index = 0; index < slots.Length; index++)
                {
                    string sourceName = slots[index] == null ? renderer.name + "_" + index : slots[index].name;
                    if (!assignedBySource.TryGetValue(sourceName, out Material material))
                    {
                        material = SelectHallMaterial(sourceName, hallMaterials, fallbackIndex++);
                        assignedBySource[sourceName] = material;
                        Debug.Log($"HALL_MATERIAL_MAP: {sourceName} -> {material.name}");
                    }
                    slots[index] = material;
                }
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            NormalizeHorizontalSizeAndGround(visual, 80f);
            SetStatic(root);
            return SavePrefab(root, HallPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Material CreateHallMaterial(string texturePath, int index)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? throw new InvalidOperationException("URP Lit shader is unavailable.");
        Texture2D texture = RequireAsset<Texture2D>(texturePath);
        string filename = Path.GetFileNameWithoutExtension(texturePath);
        string materialPath = $"{HallMaterialFolder}/{index + 1:00} {SanitizeFileName(filename)}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = filename };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", index >= 6 ? 0.42f : 0.2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateHallGlassMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? throw new InvalidOperationException("URP Lit shader is unavailable.");
        string path = HallMaterialFolder + "/08 Gallery Glass.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = "Gallery Glass" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_BaseColor", new Color(0.72f, 0.88f, 0.94f, 0.22f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Smoothness", 0.92f);
        material.SetFloat("_Metallic", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material SelectHallMaterial(string sourceName, IReadOnlyList<Material> materials, int fallbackIndex)
    {
        string value = sourceName.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        if (value.Contains("sol") || value.Contains("terrazzo") || value.Contains("floor")) return materials[6];
        if (value.Contains("deco001") || value.Contains("metal001")) return materials[1];
        if (value.Contains("deco") || value.Contains("metal")) return materials[0];
        if (value.Contains("vitre") || value.Contains("glass")) return materials[7];
        if (value.Contains("murext") || value.Contains("wallexterior")) return materials[2];
        if (value.Contains("murint001") && (value.Contains("uv2") || value.Contains("u2") || value.EndsWith("2"))) return materials[5];
        if (value.Contains("murint001")) return materials[4];
        if (value.Contains("murint") && (value.Contains("uv2") || value.Contains("u2") || value.EndsWith("2"))) return materials[3];
        if (value.Contains("murint") || value.Contains("wall")) return materials[2];
        return materials[Mathf.Abs(fallbackIndex) % materials.Count];
    }

    private static void PlaceMainMenu(GameObject skyPrefab, GameObject tuguPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject root = GetOrCreateRoot(scene, "Main Menu Environment");
        PlacePrefabIfMissing(scene, root.transform, skyPrefab, "Main Menu Background (Position Here)");
        PlacePrefabIfMissing(scene, root.transform, tuguPrefab, "Tugu Negara Monument (Position Here)");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PlaceGallery(GameObject hallPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(GalleryScenePath, OpenSceneMode.Single);
        GameObject root = GetOrCreateRoot(scene, "Gallery Environment");
        PlacePrefabIfMissing(scene, root.transform, hallPrefab, "Gallery Hall (Position and Scale Here)");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject GetOrCreateRoot(Scene scene, string name)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == name);
        if (root != null) return root;
        root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static void PlacePrefabIfMissing(Scene scene, Transform parent, GameObject prefab, string name)
    {
        if (parent.Find(name) != null) return;
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject
                              ?? throw new InvalidOperationException("Could not place prefab: " + prefab.name);
        instance.name = name;
        instance.transform.SetParent(parent, false);
    }

    private static GameObject InstantiateVisual(GameObject model, Transform parent, string name)
    {
        GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject
                            ?? throw new InvalidOperationException("Could not instantiate model: " + model.name);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        foreach (Camera camera in visual.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        foreach (Light light in visual.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light.gameObject);
        return visual;
    }

    private static Renderer[] RequireRenderers(GameObject target, string displayName)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(displayName + " has no renderers.");
        return renderers;
    }

    private static GameObject SavePrefab(GameObject root, string path) =>
        PrefabUtility.SaveAsPrefabAsset(root, path) ?? throw new InvalidOperationException("Could not save prefab: " + path);

    private static void NormalizeHorizontalSizeAndGround(GameObject visual, float targetSize)
    {
        Bounds bounds = CalculateBounds(visual);
        float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontal <= 0.001f) throw new InvalidOperationException(visual.name + " has invalid bounds.");
        visual.transform.localScale *= targetSize / horizontal;
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
        Renderer[] renderers = RequireRenderers(root, root.name);
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

    private static void Validate()
    {
        GameObject sky = RequireAsset<GameObject>(SkyPrefabPath);
        GameObject tugu = RequireAsset<GameObject>(TuguPrefabPath);
        GameObject hall = RequireAsset<GameObject>(HallPrefabPath);
        GameObject frame = RequireAsset<GameObject>(FramePrefabPath);
        foreach (GameObject prefab in new[] { sky, tugu, hall, frame })
            if (prefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException(prefab.name + " prefab has no renderer.");
        if (frame.GetComponent<BoxCollider>() == null)
            throw new InvalidOperationException("Picture Frame prefab has no placement collider.");

        Scene menu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Transform menuRoot = menu.GetRootGameObjects().FirstOrDefault(item => item.name == "Main Menu Environment")?.transform;
        if (menuRoot?.Find("Main Menu Background (Position Here)") == null || menuRoot.Find("Tugu Negara Monument (Position Here)") == null)
            throw new InvalidOperationException("Main menu models are not placed.");
        if (PrefabUtility.GetCorrespondingObjectFromSource(menuRoot.Find("Main Menu Background (Position Here)").gameObject) != sky ||
            PrefabUtility.GetCorrespondingObjectFromSource(menuRoot.Find("Tugu Negara Monument (Position Here)").gameObject) != tugu)
            throw new InvalidOperationException("Main menu model instances are not connected to their prefabs.");
        Scene gallery = EditorSceneManager.OpenScene(GalleryScenePath, OpenSceneMode.Single);
        Transform galleryRoot = gallery.GetRootGameObjects().FirstOrDefault(item => item.name == "Gallery Environment")?.transform;
        if (galleryRoot?.Find("Gallery Hall (Position and Scale Here)") == null)
            throw new InvalidOperationException("Gallery Hall is not placed.");
        if (PrefabUtility.GetCorrespondingObjectFromSource(galleryRoot.Find("Gallery Hall (Position and Scale Here)").gameObject) != hall)
            throw new InvalidOperationException("Gallery Hall instance is not connected to its prefab.");
        if (galleryRoot.GetComponentsInChildren<Transform>(true).Any(item => item.name == "Picture Frame"))
            throw new InvalidOperationException("Picture Frame should remain prefab-only for now.");

        foreach (GameObject prefab in new[] { sky, tugu, hall, frame })
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) > 0)
                throw new InvalidOperationException(prefab.name + " contains a missing script.");
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new FileNotFoundException("Missing required asset.", path);

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        return value;
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
