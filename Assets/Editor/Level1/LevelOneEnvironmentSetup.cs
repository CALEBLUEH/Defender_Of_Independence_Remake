using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOneEnvironmentSetup
    {
        private const string LevelScene = "Assets/Scene/Scene_Level1.unity";
        private const string EnvironmentRootName = "Level 1 Environment";
        private const string ModelPath = "Assets/Level1/Art/Environment/Dust2Map/Models/Dust2_Map.obj";
        private const string TextureFolder = "Assets/Level1/Art/Environment/Dust2Map/Textures";
        private const string MaterialFolder = "Assets/Level1/Art/Environment/Dust2Map/Materials";
        private const string PrefabPath = "Assets/Level1/Art/Environment/Dust2Map/Prefabs/Dust2_LevelMap.prefab";
        private const string LightingFolder = "Assets/Level1/Lighting";
        private const string VolumeProfilePath = LightingFolder + "/Level1_DaylightVolume.asset";

        [MenuItem("Project Tools/Level 1/Rebuild Dust2 Environment")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Level 1 Environment",
                    "This replaces only the generated Level 1 Environment root and its generated map prefab/materials. Player and enemy spawn points are not changed.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            Rebuild();
        }

        public static void RebuildFromCommandLine()
        {
            try
            {
                Rebuild();
                Debug.Log("LEVEL1_ENVIRONMENT_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_ENVIRONMENT_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        public static void InspectModelFromCommandLine()
        {
            try
            {
                AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (model == null)
                {
                    throw new InvalidOperationException($"Could not import model at {ModelPath}.");
                }

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Debug.Log($"LEVEL1_MODEL_INSPECTION renderers={renderers.Length}");
                foreach (Renderer renderer in renderers)
                {
                    string materials = string.Join(", ", renderer.sharedMaterials.Select(material => material == null ? "<null>" : material.name));
                    Debug.Log($"LEVEL1_MODEL_RENDERER path={GetPath(renderer.transform)} materials=[{materials}]");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CapturePreviewFromCommandLine()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(LevelScene, OpenSceneMode.Single);
                GameObject environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
                if (environment == null)
                {
                    throw new InvalidOperationException("The generated Level 1 environment is missing.");
                }

                Bounds bounds = CalculateBounds(environment);
                GameObject cameraObject = new GameObject("Temporary Level 1 Preview Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 52f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 500f;

                float distance = Mathf.Max(bounds.size.x, bounds.size.z) * 1.15f;
                cameraObject.transform.position = bounds.center + new Vector3(distance * 0.75f, distance * 0.55f, -distance);
                cameraObject.transform.LookAt(bounds.center + Vector3.up * bounds.extents.y * 0.1f);

                RenderTexture renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                Texture2D preview = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = renderTexture;
                    camera.Render();
                    RenderTexture.active = renderTexture;
                    preview.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
                    preview.Apply();
                    string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "LevelOneMapPreview.png");
                    File.WriteAllBytes(outputPath, preview.EncodeToPNG());
                    Debug.Log($"LEVEL1_ENVIRONMENT_PREVIEW_SAVED path={outputPath}");
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(preview);
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void Rebuild()
        {
            EnsureFolders();
            ConfigureModelImporter();
            ConfigureTextureImporters();

            Dictionary<string, Material> materials = CreateMaterials();
            GameObject prefab = CreateMapPrefab(materials);
            PlaceMapAndLighting(prefab);
            ValidateSetup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Level1");
            EnsureFolder("Assets/Level1", "Lighting");
            EnsureFolder("Assets/Level1/Art/Environment/Dust2Map", "Materials");
            EnsureFolder("Assets/Level1/Art/Environment/Dust2Map", "Prefabs");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void ConfigureModelImporter()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"No model importer was found at {ModelPath}.");
            }

            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.addCollider = false;
            importer.generateSecondaryUV = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureTextureImporters()
        {
            foreach (string texturePath in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                         .Select(AssetDatabase.GUIDToAssetPath))
            {
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("The URP/Lit shader is unavailable.");
            }

            Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (texturePaths.Length != 34)
            {
                throw new InvalidOperationException($"Expected 34 Dust2 base-colour textures, found {texturePaths.Length}.");
            }

            foreach (string texturePath in texturePaths)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                {
                    throw new FileNotFoundException($"Missing Dust2 texture: {texturePath}");
                }

                string materialName = Path.GetFileNameWithoutExtension(texturePath).Replace("_baseColor", string.Empty);
                string materialPath = $"{MaterialFolder}/{materialName}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else
                {
                    material.shader = shader;
                }

                material.name = materialName;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.12f);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.doubleSidedGI = true;
                EditorUtility.SetDirty(material);
                materials.Add(materialName, material);
            }

            return materials;
        }

        private static GameObject CreateMapPrefab(IReadOnlyDictionary<string, Material> materials)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not load imported FBX at {ModelPath}.");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate the converted Dust2 model.");
            }

            try
            {
                instance.name = "Dust2 Level Map";
                AssignMaterials(instance, materials);
                AddMeshColliders(instance);
                SetStaticFlags(instance);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Could not save generated prefab at {PrefabPath}.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssignMaterials(GameObject instance, IReadOnlyDictionary<string, Material> materials)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] current = renderer.sharedMaterials;
                Material[] replacements = new Material[current.Length];

                for (int slot = 0; slot < current.Length; slot++)
                {
                    string importedName = current[slot] == null ? string.Empty : current[slot].name;
                    if (!materials.TryGetValue(importedName, out Material replacement))
                    {
                        throw new InvalidOperationException(
                            $"No generated material matches imported slot '{importedName}' on {GetPath(renderer.transform)}.");
                    }

                    replacements[slot] = replacement;
                    Debug.Log($"LEVEL1_MATERIAL_ASSIGN renderer={GetPath(renderer.transform)} slot={slot} material={importedName}");
                }

                renderer.sharedMaterials = replacements;
            }
        }

        private static void AddMeshColliders(GameObject instance)
        {
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                {
                    continue;
                }

                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        private static void SetStaticFlags(GameObject instance)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic |
                                      StaticEditorFlags.ContributeGI |
                                      StaticEditorFlags.OccluderStatic |
                                      StaticEditorFlags.OccludeeStatic |
                                      StaticEditorFlags.ReflectionProbeStatic;

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            }
        }

        private static void PlaceMapAndLighting(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(LevelScene, OpenSceneMode.Single);
            GameObject existingRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            GameObject environmentRoot = new GameObject(EnvironmentRootName);
            GameObject map = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (map == null)
            {
                throw new InvalidOperationException("Could not place the generated Dust2 prefab into Scene_Level1.");
            }

            map.name = "Dust2 Map (Set Spawn Points Later)";
            map.transform.SetParent(environmentRoot.transform, false);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            map.transform.localScale = Vector3.one;

            ConfigureDaylight(scene, environmentRoot.transform, CalculateBounds(map));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureDaylight(Scene scene, Transform environmentRoot, Bounds mapBounds)
        {
            Light sun = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .FirstOrDefault(light => light.type == LightType.Directional);

            if (sun == null)
            {
                GameObject sunObject = new GameObject("Directional Light");
                SceneManager.MoveGameObjectToScene(sunObject, scene);
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.color = new Color(1f, 0.93f, 0.80f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.70f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.29f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.12f, 0.10f);
            RenderSettings.ambientIntensity = 0.85f;

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            profile.components.Clear();
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.05f);
            color.contrast.Override(4f);
            color.saturation.Override(-3f);
            EditorUtility.SetDirty(profile);

            GameObject lightingRoot = new GameObject("Level 1 Lighting");
            lightingRoot.transform.SetParent(environmentRoot, false);

            Volume volume = lightingRoot.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            ReflectionProbe probe = new GameObject("Reflection Probe - Bake When Layout Is Final").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(lightingRoot.transform, false);
            probe.transform.position = mapBounds.center;
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.size = new Vector3(
                Mathf.Max(10f, mapBounds.size.x + 10f),
                Mathf.Max(10f, mapBounds.size.y + 10f),
                Mathf.Max(10f, mapBounds.size.z + 10f));

            foreach (Camera camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            {
                UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = true;
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("The imported Dust2 model has no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void ValidateSetup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("The generated Dust2 map prefab is missing.");
            }

            int rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length;
            int colliderCount = prefab.GetComponentsInChildren<MeshCollider>(true).Length;
            if (rendererCount == 0 || colliderCount == 0)
            {
                throw new InvalidOperationException($"The map prefab is incomplete. renderers={rendererCount}, colliders={colliderCount}");
            }

            Scene scene = SceneManager.GetSceneByPath(LevelScene);
            GameObject environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
            if (environment == null || environment.GetComponentInChildren<Volume>(true) == null)
            {
                throw new InvalidOperationException("Scene_Level1 does not contain the generated map and lighting root.");
            }

            Bounds bounds = CalculateBounds(environment);
            Debug.Log($"LEVEL1_ENVIRONMENT_VALID renderers={rendererCount} colliders={colliderCount} boundsCenter={bounds.center} boundsSize={bounds.size}");
        }

        private static string GetPath(Transform transform)
        {
            List<string> names = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
