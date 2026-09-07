using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LevelTwoEnvironmentSetup
{
    private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
    private const string EnvironmentRootName = "Level 2 Environment";
    private const string HousePrefabPath = "Assets/Level2/Prefabs/Environment/Interior House.prefab";
    private const string RadioPrefabPath = "Assets/Level2/Prefabs/Props/Radio Station.prefab";
    private const string ClockPrefabPath = "Assets/Level2/Prefabs/Props/Grandfather Clock.prefab";

    private sealed class ModelSpec
    {
        public string ModelPath;
        public string TextureFolder;
        public string MaterialFolder;
        public string PrefabFolder;
        public float ImportScale = 1f;
        public Dictionary<string, string> MaterialAliases;
        public Dictionary<string, string> Pieces;
    }

    private static readonly ModelSpec House = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Environment/InteriorHouse/Model/building_house.fbx",
        TextureFolder = "Assets/Level2/Art/Environment/InteriorHouse/Textures",
        MaterialFolder = "Assets/Level2/Art/Environment/InteriorHouse/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Environment",
        MaterialAliases = Aliases(
            "exterior", "Siding", "interior", "interior", "trim", "trim",
            "floor", "FLOOR1", "roof", "Roof", "ceiling", "ceiling1")
    };

    private static readonly ModelSpec Furniture01 = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Furniture/Set01/Model/chairsscene.fbx",
        TextureFolder = "Assets/Level2/Art/Furniture/Set01/Textures",
        MaterialFolder = "Assets/Level2/Art/Furniture/Set01/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Furniture/Set01",
        MaterialAliases = Aliases(
            "chair1", "chair1_low_chair1", "endtable1", "endtable1_low_endtable1",
            "lamp01", "lamp01_low_lamp01"),
        Pieces = Pieces(
            "chairlegs_low.001", "Armchair",
            "endtable1 top _low", "End Table A",
            "endtable1 top _low.003", "End Table B",
            "endtable1 top _low.006", "End Table C",
            "lampscrew_low.001", "Table Lamp")
    };

    private static readonly ModelSpec Furniture02 = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Furniture/Set02/Model/Medieval furniture.fbx",
        TextureFolder = "Assets/Level2/Art/Furniture/Set02/Textures",
        MaterialFolder = "Assets/Level2/Art/Furniture/Set02/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Furniture/Set02",
        ImportScale = 10f,
        MaterialAliases = Aliases(
            "axe", "multiple_object_lowpoly_axe", "big_table", "big_table_lowpoly_big_table",
            "book", "bottle_vase_books_lowpoly_book", "aiStandardSurface2", "bottle_vase_books_lowpoly_book",
            "bottle", "bottle_vase_books_lowpoly_bottle", "vasai", "bottle_vase_books_lowpoly_vase",
            "box", "box_lowpoly1_box", "aiStandardSurface1", "box_lowpoly1_box", "chair", "chair_lowpoly_chair",
            "drum", "multiple_object_lowpoly_drum", "glass", "multiple_object_lowpoly_glass",
            "plate", "multiple_object_lowpoly_plate", "shell", "shell_lowpoly_shell",
            "small_table", "small_table_lowpoly_small_table", "sword", "multiple_object_lowpoly_sword",
            "table01", "table_01_lowpoly_table_01"),
        Pieces = Pieces(
            "axe1", "Axe 01", "axe2", "Axe 02", "big_table1", "Big Table",
            "book1", "Book 01", "book2", "Book 02", "book_set", "Book Set 01",
            "book_set1", "Book Set 02", "book_set2", "Book Set 03", "book_set3", "Book Set 04",
            "book_set4", "Book Set 05", "book_set5", "Book Set 06", "book_set6", "Book Set 07",
            "bottles", "Bottle Collection", "box", "Wooden Box", "chair1", "Chair 01",
            "chair2", "Chair 02", "chair3", "Chair 03", "drum1", "Drum",
            "glas", "Drinking Glass Set", "knife", "Knife", "plate1", "Plate Stack",
            "shell1", "Shelf", "small_table1", "Small Table", "sword1", "Sword 01",
            "sword2", "Sword 02", "table_01", "Table With Cabinet", "vase", "Vase Set")
    };

    private static readonly ModelSpec Furniture03 = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Furniture/Set03/Model/RoomFurniture.fbx",
        TextureFolder = "Assets/Level2/Art/Furniture/Set03/Textures",
        MaterialFolder = "Assets/Level2/Art/Furniture/Set03/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Furniture/Set03",
        MaterialAliases = Aliases(
            "DeskA", "TableA", "DeskB", "TableB", "Mirror", "Mirror",
            "Cabinet", "Cabinet", "Board", "Chalkboard", "JarLid", "Jar",
            "Jar", "Jar", "Chair", "Chair", "Lamp", "CeilingLamp"),
        Pieces = Pieces(
            "pCube12", "Table A", "pCube14", "Table B", "pCube15", "Mirror",
            "pCube24", "Cabinet", "pCylinder24", "Chalkboard", "pCylinder32", "Jar",
            "polySurface11", "Chair", "pSphere5", "Ceiling Lamp")
    };

    private static readonly ModelSpec Radio = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Props/RadioStation/Model/yaesu_lowpoly.fbx",
        TextureFolder = "Assets/Level2/Art/Props/RadioStation/Textures",
        MaterialFolder = "Assets/Level2/Art/Props/RadioStation/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Props",
        ImportScale = 0.2f,
        MaterialAliases = Aliases("Lit", "DefaultMaterial", "No Name", "DefaultMaterial")
    };

    private static readonly ModelSpec Clock = new ModelSpec
    {
        ModelPath = "Assets/Level2/Art/Props/Clock/Model/TallClock.fbx",
        TextureFolder = "Assets/Level2/Art/Props/Clock/Textures",
        MaterialFolder = "Assets/Level2/Art/Props/Clock/Materials",
        PrefabFolder = "Assets/Level2/Prefabs/Props",
        MaterialAliases = Aliases(
            "ClockCase", "ClockCase", "Clockface", "Clockface", "Glass", "Glass", "MoonDial", "MoonDial")
    };

    private static readonly ModelSpec[] AllModels = { House, Furniture01, Furniture02, Furniture03, Radio, Clock };

    [MenuItem("Project Tools/Level 2/Import House and Furniture Prefabs")]
    public static void BuildFromMenu()
    {
        try
        {
            BuildAll();
            EditorUtility.DisplayDialog("Level 2 Environment", "House, furniture, radio, and clock assets are ready.", "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Level 2 Environment", exception.Message, "OK");
            throw;
        }
    }

    public static void BuildFromCommandLine()
    {
        BuildAll();
        Debug.Log("LEVEL2_ENVIRONMENT_SETUP_SUCCESS");
    }

    private static void BuildAll()
    {
        EnsureFolder("Assets/Level2/Prefabs/Environment");
        EnsureFolder("Assets/Level2/Prefabs/Furniture/Set01");
        EnsureFolder("Assets/Level2/Prefabs/Furniture/Set02");
        EnsureFolder("Assets/Level2/Prefabs/Furniture/Set03");
        EnsureFolder("Assets/Level2/Prefabs/Props");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (ModelSpec spec in AllModels)
        {
            EnsureFolder(spec.MaterialFolder);
            ConfigureModel(spec);
            ConfigureTextures(spec.TextureFolder);
        }

        Dictionary<ModelSpec, Dictionary<string, Material>> materialSets =
            AllModels.ToDictionary(spec => spec, CreateMaterials);

        GameObject housePrefab = CreateWholePrefab(House, materialSets[House], "Interior House", HousePrefabPath, true);
        CreateFurniturePrefabs(Furniture01, materialSets[Furniture01]);
        CreateFurniturePrefabs(Furniture02, materialSets[Furniture02]);
        CreateFurniturePrefabs(Furniture03, materialSets[Furniture03]);
        CreateWholePrefab(Radio, materialSets[Radio], "Radio Station", RadioPrefabPath, false);
        CreateWholePrefab(Clock, materialSets[Clock], "Grandfather Clock", ClockPrefabPath, false);
        PlaceHouse(housePrefab);
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static void ConfigureModel(ModelSpec spec)
    {
        AssetDatabase.ImportAsset(spec.ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ModelImporter importer = AssetImporter.GetAtPath(spec.ModelPath) as ModelImporter
                                 ?? throw new FileNotFoundException("Missing FBX: " + spec.ModelPath);
        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.meshCompression = ModelImporterMeshCompression.Low;
        importer.addCollider = false;
        importer.generateSecondaryUV = spec == House;
        importer.globalScale = spec.ImportScale;
        importer.SaveAndReimport();
    }

    private static void ConfigureTextures(string folder)
    {
        foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            bool normal = name.EndsWith("_Normal", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_Normal_OpenGL", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_N", StringComparison.OrdinalIgnoreCase);
            bool linear = normal || name.EndsWith("_AO", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_Mixed_AO", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_Metallic", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_M", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_Roughness", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith("_R", StringComparison.OrdinalIgnoreCase);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType == (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) &&
                importer.sRGBTexture == !linear && importer.mipmapEnabled && importer.streamingMipmaps &&
                importer.wrapMode == TextureWrapMode.Repeat && importer.filterMode == FilterMode.Trilinear &&
                importer.anisoLevel == 4 && importer.maxTextureSize == 2048 &&
                importer.textureCompression == TextureImporterCompression.Compressed)
                continue;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<string, Material> CreateMaterials(ModelSpec spec)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? throw new InvalidOperationException("URP/Lit shader is unavailable.");
        Dictionary<string, Material> result = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        foreach (string group in spec.MaterialAliases.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string materialPath = spec.MaterialFolder + "/" + SafeName(group) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = group };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D baseMap = FindTexture(spec.TextureFolder, group, "_BaseColor", "_Base_Color", "_Diffuse", "_C");
            Texture2D normalMap = FindTexture(spec.TextureFolder, group, "_Normal_OpenGL", "_Normal", "_N");
            Texture2D occlusionMap = FindTexture(spec.TextureFolder, group, "_Mixed_AO", "_AO");
            Texture2D metallicMap = FindTexture(spec.TextureFolder, group, "_Metallic", "_M");
            Texture2D roughnessMap = FindTexture(spec.TextureFolder, group, "_Roughness", "_R");

            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", group.Equals("Glass", StringComparison.OrdinalIgnoreCase)
                ? new Color(0.72f, 0.88f, 0.92f, 0.28f)
                : Color.white);
            material.SetFloat("_Metallic", metallicMap == null ? 0f : 0.18f);
            material.SetFloat("_Smoothness", roughnessMap == null ? 0.28f : 0.32f);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            if (occlusionMap != null)
            {
                material.SetTexture("_OcclusionMap", occlusionMap);
                material.SetFloat("_OcclusionStrength", 1f);
            }
            else
            {
                material.SetTexture("_OcclusionMap", null);
            }

            if (group.Equals("Glass", StringComparison.OrdinalIgnoreCase)) ConfigureTransparent(material);
            else ConfigureOpaque(material);
            EditorUtility.SetDirty(material);
            result[group] = material;
        }
        return result;
    }

    private static Texture2D FindTexture(string folder, string group, params string[] suffixes)
    {
        foreach (string suffix in suffixes)
        {
            string wanted = group + suffix;
            string path = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate)
                    .Equals(wanted, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }

    private static void ConfigureOpaque(Material material)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_ZWrite", 1f);
        material.renderQueue = -1;
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private static void ConfigureTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private static GameObject CreateWholePrefab(ModelSpec spec, IReadOnlyDictionary<string, Material> materials,
        string name, string prefabPath, bool house)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath)
                                ?? throw new FileNotFoundException("Imported model missing: " + spec.ModelPath);
        GameObject root = new GameObject(name);
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject
                                ?? throw new InvalidOperationException("Could not instantiate " + spec.ModelPath);
            visual.name = name + " Visual";
            visual.transform.SetParent(root.transform, false);
            AssignMaterials(visual, spec.MaterialAliases, materials);
            RecenterOnGround(visual);
            if (house)
            {
                AddMeshColliders(visual);
                SetStatic(root);
            }
            else
            {
                AddBoxCollider(root, visual);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath)
                   ?? throw new InvalidOperationException("Could not save prefab " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateFurniturePrefabs(ModelSpec spec, IReadOnlyDictionary<string, Material> materials)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath)
                                ?? throw new FileNotFoundException("Imported model missing: " + spec.ModelPath);
        GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject
                           ?? throw new InvalidOperationException("Could not instantiate " + spec.ModelPath);
        try
        {
            AssignMaterials(model, spec.MaterialAliases, materials);
            Dictionary<string, Transform> children = model.transform.Cast<Transform>()
                .ToDictionary(child => child.name, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> piece in spec.Pieces)
            {
                if (!children.TryGetValue(piece.Key, out Transform source))
                    throw new InvalidOperationException($"Furniture piece '{piece.Key}' is missing from {spec.ModelPath}.");
                CreatePiecePrefab(source, spec.PrefabFolder + "/" + SafeName(piece.Value) + ".prefab", piece.Value);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(model);
        }
    }

    private static void CreatePiecePrefab(Transform source, string prefabPath, string displayName)
    {
        GameObject root = new GameObject(displayName);
        try
        {
            GameObject visual = UnityEngine.Object.Instantiate(source.gameObject);
            visual.name = displayName + " Visual";
            visual.transform.SetParent(root.transform, true);
            visual.transform.localScale = source.lossyScale;
            visual.transform.SetPositionAndRotation(source.position, source.rotation);
            RecenterOnGround(visual);
            AddBoxCollider(root, visual);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AssignMaterials(GameObject instance, IReadOnlyDictionary<string, string> aliases,
        IReadOnlyDictionary<string, Material> materials)
    {
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] replacements = new Material[renderer.sharedMaterials.Length];
            for (int slot = 0; slot < replacements.Length; slot++)
            {
                string importedName = renderer.sharedMaterials[slot] == null ? string.Empty : renderer.sharedMaterials[slot].name;
                if (!aliases.TryGetValue(importedName, out string group) || !materials.TryGetValue(group, out Material material))
                    throw new InvalidOperationException($"No Level 2 material mapping for '{importedName}' on {renderer.name}.");
                replacements[slot] = material;
            }
            renderer.sharedMaterials = replacements;
            renderer.receiveShadows = true;
        }
    }

    private static void RecenterOnGround(GameObject visual)
    {
        Bounds bounds = CalculateBounds(visual);
        visual.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    private static void AddBoxCollider(GameObject root, GameObject visual)
    {
        Bounds bounds = CalculateBounds(visual);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        collider.size = bounds.size;
    }

    private static void AddMeshColliders(GameObject visual)
    {
        foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no renderers.");
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void SetStatic(GameObject root)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                  StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                  StaticEditorFlags.ReflectionProbeStatic;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
    }

    private static void PlaceHouse(GameObject housePrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        GameObject environment = new GameObject(EnvironmentRootName);
        SceneManager.MoveGameObjectToScene(environment, scene);
        GameObject house = PrefabUtility.InstantiatePrefab(housePrefab, scene) as GameObject
                           ?? throw new InvalidOperationException("Could not place the Level 2 house prefab.");
        house.name = "Interior House (Decorate Rooms Here)";
        house.transform.SetParent(environment.transform, false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Validate()
    {
        string[] furniture = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Level2/Prefabs/Furniture" });
        if (furniture.Length != 40) throw new InvalidOperationException($"Expected 40 furniture prefabs, found {furniture.Length}.");
        foreach (string guid in furniture)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponentInChildren<Renderer>(true) == null || prefab.GetComponent<Collider>() == null)
                throw new InvalidOperationException("Furniture prefab is incomplete: " + path);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RadioPrefabPath)?.GetComponent<Collider>() == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ClockPrefabPath)?.GetComponent<Collider>() == null)
            throw new InvalidOperationException("Radio or clock prefab is incomplete.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        GameObject environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
        if (environment?.transform.Find("Interior House (Decorate Rooms Here)") == null)
            throw new InvalidOperationException("Interior House is not present in Scene_Level2.");
        Debug.Log("LEVEL2_ENVIRONMENT_VALID house=scene furniturePrefabs=40 radio=prefab clock=prefab materials=URP colliders=ready");
    }

    private static Dictionary<string, string> Aliases(params string[] values) => PairDictionary(values);
    private static Dictionary<string, string> Pieces(params string[] values) => PairDictionary(values);

    private static Dictionary<string, string> PairDictionary(string[] values)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < values.Length; index += 2) result.Add(values[index], values[index + 1]);
        return result;
    }

    private static string SafeName(string value)
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
