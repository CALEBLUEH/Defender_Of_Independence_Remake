using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenderOfIndependence.Level1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOneEnemySetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level1.unity";
        private const string EnemyModelPath = "Assets/Level1/Art/Enemy/Terrorist/Model/Terrorist.fbx";
        private const string EnemyTextureFolder = "Assets/Level1/Art/Enemy/Terrorist/Textures";
        private const string EnemyMaterialFolder = "Assets/Level1/Art/Enemy/Terrorist/Materials";
        private const string GunModelPath = "Assets/Level1/Art/Player/Weapon/Deagle/Model/Deagle_CS2.fbx";
        private const string GunMaterialPath = "Assets/Level1/Art/Player/Weapon/Deagle/Materials/Deagle_Player.mat";
        private const string EnemyPrefabPath = "Assets/Level1/Prefabs/Enemy/LevelOneEnemy.prefab";
        private const string NavMeshDataPath = "Assets/Scene/Scene_Level1/NavMesh.asset";
        private const string PlayerName = "Level 1 Player";
        private const string DirectorName = "Level 1 Enemy Director";
        private const string NavigationSurfaceName = "Level 1 Navigation Surface";
        private const string PlayerLayerName = "Player";
        private const string EnemyLayerName = "Enemy";

        [MenuItem("Project Tools/Level 1/Rebuild Enemy Combat System")]
        public static void RebuildFromMenu()
        {
            if (EditorUtility.DisplayDialog(
                    "Rebuild Level 1 Enemy Combat",
                    "This updates the Level 1 player, rebuilds the enemy prefab, places the enemy director, and rebuilds the Level 1 NavMesh.",
                    "Rebuild",
                    "Cancel"))
            {
                Rebuild();
            }
        }

        public static void RebuildFromCommandLine()
        {
            try
            {
                Rebuild();
                Debug.Log("LEVEL1_ENEMY_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_ENEMY_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Project Tools/Level 1/Repair Enemy Scene Wiring")]
        public static void RepairSceneFromMenu()
        {
            RepairSceneWiring();
        }

        public static void RepairSceneFromCommandLine()
        {
            try
            {
                RepairSceneWiring();
                Debug.Log("LEVEL1_ENEMY_SCENE_REPAIR_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_ENEMY_SCENE_REPAIR_FAILED");
                EditorApplication.Exit(1);
            }
        }

        public static void CapturePreviewFromCommandLine()
        {
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("The generated enemy prefab is missing.");
                }

                GameObject enemy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                GameObject cameraObject = new GameObject("Enemy Preview Camera");
                GameObject lightObject = new GameObject("Enemy Preview Light");
                try
                {
                    enemy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    EnemyAimPose previewPose = enemy.GetComponent<EnemyAimPose>();
                    for (int iteration = 0; iteration < 30; iteration++)
                    {
                        previewPose?.ApplyPose();
                    }

                    Camera camera = cameraObject.AddComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0.09f, 0.11f, 0.14f);
                    camera.fieldOfView = 42f;
                    camera.transform.position = new Vector3(0f, 1.25f, 3.4f);
                    camera.transform.LookAt(new Vector3(0f, 1.05f, 0f));

                    Light light = lightObject.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.4f;
                    lightObject.transform.rotation = Quaternion.Euler(35f, 145f, 0f);

                    RenderTexture target = new RenderTexture(1000, 1000, 24, RenderTextureFormat.ARGB32);
                    Texture2D preview = new Texture2D(1000, 1000, TextureFormat.RGB24, false);
                    try
                    {
                        camera.targetTexture = target;
                        camera.Render();
                        RenderTexture.active = target;
                        preview.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0);
                        preview.Apply();
                        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "LevelOneEnemyPreview.png");
                        File.WriteAllBytes(output, preview.EncodeToPNG());
                        Debug.Log($"LEVEL1_ENEMY_PREVIEW_SAVED path={output}");
                    }
                    finally
                    {
                        camera.targetTexture = null;
                        RenderTexture.active = null;
                        UnityEngine.Object.DestroyImmediate(preview);
                        UnityEngine.Object.DestroyImmediate(target);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(enemy);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                    UnityEngine.Object.DestroyImmediate(lightObject);
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
            LevelOnePlayerSetup.Rebuild();
            EnsureFolder("Assets/Level1/Art/Enemy/Terrorist", "Materials");
            EnsureFolder("Assets/Level1/Prefabs", "Enemy");

            ConfigurePhysicsLayers();
            ConfigureEnemyModel();
            Material[] materials = CreateEnemyMaterials();
            GameObject enemyPrefab = CreateEnemyPrefab(materials);
            PlaceDirectorAndBakeNavigation(enemyPrefab);
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RepairSceneWiring()
        {
            ConfigurePhysicsLayers();
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (enemyPrefab == null || enemyPrefab.GetComponent<LevelOneEnemyBrain>() == null)
            {
                throw new InvalidOperationException(
                    "The LevelOneEnemy prefab is missing. Run Rebuild Enemy Combat System once before repairing the scene.");
            }

            ApplyLayerToPrefab(EnemyPrefabPath, LayerMask.NameToLayer(EnemyLayerName));
            enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            PlaceDirectorAndBakeNavigation(enemyPrefab);
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureEnemyModel()
        {
            AssetDatabase.ImportAsset(EnemyModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(EnemyModelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException($"Could not import enemy FBX at {EnemyModelPath}.");
            }

            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.SaveAndReimport();

            ConfigureTexture("tm_leet_v2_body_variantd_color.png");
            ConfigureTexture("tm_leet_v2_lower_body_variantd_color.png");
            ConfigureTexture("tm_leet_v2_shemagh_variantd_color.png");
        }

        private static void ConfigureTexture(string fileName)
        {
            string path = $"{EnemyTextureFolder}/{fileName}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new FileNotFoundException($"Missing enemy texture: {path}");
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = 2048;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material[] CreateEnemyMaterials()
        {
            return new[]
            {
                CreateMaterial("Terrorist_Body", "tm_leet_v2_body_variantd_color.png"),
                CreateMaterial("Terrorist_LowerBody", "tm_leet_v2_lower_body_variantd_color.png"),
                CreateMaterial("Terrorist_Shemagh", "tm_leet_v2_shemagh_variantd_color.png")
            };
        }

        private static Material CreateMaterial(string name, string textureName)
        {
            string path = $"{EnemyMaterialFolder}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP/Lit shader is unavailable.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{EnemyTextureFolder}/{textureName}"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateEnemyPrefab(Material[] enemyMaterials)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyModelPath);
            GameObject gunAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GunModelPath);
            Material gunMaterial = AssetDatabase.LoadAssetAtPath<Material>(GunMaterialPath);
            if (modelAsset == null || gunAsset == null || gunMaterial == null)
            {
                throw new InvalidOperationException("Enemy model or shared pistol assets are missing.");
            }

            GameObject root = new GameObject("LevelOneEnemy");
            try
            {
                SetLayerRecursively(root, LayerMask.NameToLayer(EnemyLayerName));
                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.35f;
                agent.height = 1.78f;
                agent.baseOffset = 0f;
                agent.speed = 4.5f;
                agent.angularSpeed = 720f;
                agent.acceleration = 18f;
                agent.stoppingDistance = 8.5f;
                agent.autoBraking = true;

                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.radius = 0.34f;
                collider.height = 1.78f;
                collider.center = new Vector3(0f, 0.89f, 0f);

                Transform aimPoint = CreateMarker(root.transform, "Enemy Damage Aim Point", new Vector3(0f, 1.35f, 0f));
                Transform eye = CreateMarker(root.transform, "Enemy Eye", new Vector3(0f, 1.62f, 0.08f));
                Transform shootOrigin = CreateMarker(root.transform, "Enemy Muzzle", new Vector3(0.11f, 1.3f, 0.82f));
                Transform rightGrip = CreateMarker(root.transform, "Right Hand Grip", new Vector3(0.18f, 1.29f, 0.42f));
                Transform leftGrip = CreateMarker(root.transform, "Left Hand Grip", new Vector3(0.08f, 1.28f, 0.46f));

                CombatHealth health = root.AddComponent<CombatHealth>();
                SetInteger(health, "team", (int)CombatTeam.Enemy);
                SetString(health, "displayName", "Enemy");
                SetFloat(health, "maximumHealth", 100f);
                SetReference(health, "aimPoint", aimPoint);

                GameObject body = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                body.name = "Terrorist Body";
                body.transform.SetParent(root.transform, false);
                FitModel(body, 1.78f, true);
                AssignEnemyMaterials(body, enemyMaterials);

                Transform gunMount = CreateMarker(root.transform, "Enemy Pistol", new Vector3(0.1f, 1.27f, 0.4f));
                gunMount.localRotation = Quaternion.Euler(0f, 180f, 0f);
                GameObject gun = PrefabUtility.InstantiatePrefab(gunAsset) as GameObject;
                gun.name = "Deagle Geometry";
                gun.transform.SetParent(gunMount, false);
                gun.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = Enumerable.Repeat(gunMaterial, renderer.sharedMaterials.Length).ToArray();
                }
                FitModel(gun, 0.42f, false);

                EnemyAimPose pose = root.AddComponent<EnemyAimPose>();
                Transform leftUpper = FindBone(body, "leftarm", "lupperarm", "leftupperarm");
                Transform leftForearm = FindBone(body, "lforearm", "leftforearm", "llowerarm");
                Transform leftHand = FindBone(body, "lhand", "lefthand");
                Transform rightUpper = FindBone(body, "rightarm", "rupperarm", "rightupperarm");
                Transform rightForearm = FindBone(body, "rforearm", "rightforearm", "rlowerarm");
                Transform rightHand = FindBone(body, "rhand", "righthand");
                SetReference(pose, "leftUpperArm", leftUpper);
                SetReference(pose, "leftForearm", leftForearm);
                SetReference(pose, "leftHand", leftHand);
                SetReference(pose, "rightUpperArm", rightUpper);
                SetReference(pose, "rightForearm", rightForearm);
                SetReference(pose, "rightHand", rightHand);
                SetReference(pose, "leftGripTarget", leftGrip);
                SetReference(pose, "rightGripTarget", rightGrip);
                if (!pose.IsConfigured)
                {
                    string available = string.Join(", ", body.GetComponentsInChildren<Transform>(true).Select(item => item.name));
                    throw new InvalidOperationException($"Could not find the enemy arm rig required for the aiming pose. Bones: {available}");
                }
                pose.ApplyPose();

                LevelOneEnemyBrain brain = root.AddComponent<LevelOneEnemyBrain>();
                SetReference(brain, "agent", agent);
                SetReference(brain, "health", health);
                SetReference(brain, "eye", eye);
                SetReference(brain, "shootOrigin", shootOrigin);

                CreateWorldHealthBar(root.transform, health);
                SetLayerRecursively(root, LayerMask.NameToLayer(EnemyLayerName));

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Could not save enemy prefab at {EnemyPrefabPath}.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssignEnemyMaterials(GameObject body, Material[] materials)
        {
            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                Material[] assigned = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int index = 0; index < assigned.Length; index++)
                {
                    string sourceName = index < renderer.sharedMaterials.Length && renderer.sharedMaterials[index] != null
                        ? Normalize(renderer.sharedMaterials[index].name)
                        : string.Empty;
                    assigned[index] = sourceName.Contains("lower")
                        ? materials[1]
                        : sourceName.Contains("shemagh") || sourceName.Contains("scarf")
                            ? materials[2]
                            : materials[Mathf.Min(index, materials.Length - 1)];
                }

                renderer.sharedMaterials = assigned;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void CreateWorldHealthBar(Transform parent, CombatHealth health)
        {
            GameObject canvasObject = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = new Vector3(0f, 2f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.005f;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 18f);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Stretch(backgroundObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.03f, 0.03f, 0.03f, 0.9f);
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backgroundObject.transform, false);
            Stretch(fillObject.GetComponent<RectTransform>(), new Vector2(3f, 3f), new Vector2(-3f, -3f));
            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.85f, 0.15f, 0.12f, 0.95f);
            fill.type = Image.Type.Simple;
            fill.raycastTarget = false;

            Slider slider = backgroundObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(1f);
            slider.fillRect = fillObject.GetComponent<RectTransform>();
            slider.targetGraphic = fill;
            slider.handleRect = null;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            WorldHealthBar healthBar = canvasObject.AddComponent<WorldHealthBar>();
            SetReference(healthBar, "health", health);
            SetReference(healthBar, "slider", slider);
            SetReference(healthBar, "fill", fill);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void PlaceDirectorAndBakeNavigation(GameObject enemyPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject player = roots.FirstOrDefault(root => root.name == PlayerName);
            GameObject spawn = roots.FirstOrDefault(root => root.name == "EnemySpawnPoint");
            GameObject siteA = roots.FirstOrDefault(root => root.name == "Hostage1Location");
            GameObject siteB = roots.FirstOrDefault(root => root.name == "Hostage2Location");
            GameObject environment = roots.FirstOrDefault(root => root.name == "Level 1 Environment");
            if (player == null || spawn == null || siteA == null || siteB == null || environment == null)
            {
                throw new InvalidOperationException("Scene_Level1 needs the player, enemy spawn, both hostage locations, and Level 1 Environment.");
            }

            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
            SetLayerRecursively(player, playerLayer);

            GameObject directorObject = roots.FirstOrDefault(root => root.name == DirectorName);
            bool createdDirector = directorObject == null;
            if (createdDirector)
            {
                directorObject = new GameObject(DirectorName);
                SceneManager.MoveGameObjectToScene(directorObject, scene);
            }

            LevelOneEnemyDirector director = directorObject.GetComponent<LevelOneEnemyDirector>() ??
                                             directorObject.AddComponent<LevelOneEnemyDirector>();
            CombatHealth playerHealth = player.GetComponent<CombatHealth>();
            if (playerHealth == null)
            {
                throw new InvalidOperationException("The Level 1 Player does not contain CombatHealth.");
            }

            SetReference(director, "enemyPrefab", enemyPrefab.GetComponent<LevelOneEnemyBrain>());
            SetReference(director, "enemySpawnPoint", spawn.transform);
            SetReference(director, "hostageSiteA", siteA.transform);
            SetReference(director, "hostageSiteB", siteB.transform);
            SetReference(director, "player", player.transform);
            SetReference(director, "playerHealth", playerHealth);
            SetFloat(director, "spawnRadius", 0f);

            GameObject navigationObject = roots.FirstOrDefault(root => root.name == NavigationSurfaceName);
            if (navigationObject == null)
            {
                navigationObject = new GameObject(NavigationSurfaceName);
                SceneManager.MoveGameObjectToScene(navigationObject, scene);
            }

            NavMeshSurface surface = navigationObject.GetComponent<NavMeshSurface>() ??
                                     navigationObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = ~((1 << playerLayer) | (1 << enemyLayer) | (1 << 2) | (1 << 5));
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            BakeNavigationSurface(surface);

            if (!NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit spawnHit, 3f, NavMesh.AllAreas))
            {
                throw new InvalidOperationException(
                    $"EnemySpawnPoint at {spawn.transform.position} is not within 3 metres of the baked NavMesh.");
            }

            float horizontalOffset = Vector2.Distance(
                new Vector2(spawn.transform.position.x, spawn.transform.position.z),
                new Vector2(spawnHit.position.x, spawnHit.position.z));
            if (horizontalOffset > 0.5f)
            {
                throw new InvalidOperationException(
                    $"EnemySpawnPoint is {horizontalOffset:F2} metres horizontally away from the baked NavMesh. Move it onto a walkable floor.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"LEVEL1_ENEMY_SCENE_WIRED createdDirector={createdDirector} spawn={spawn.transform.position} navPoint={spawnHit.position}");
        }

        private static void BakeNavigationSurface(NavMeshSurface surface)
        {
            surface.RemoveData();
            ClearLegacyNavMeshRegistration();
            surface.navMeshData = null;
            surface.BuildNavMesh();
            NavMeshData builtData = surface.navMeshData;
            if (builtData == null)
            {
                throw new InvalidOperationException("AI Navigation could not build Level 1 NavMesh data.");
            }

            EnsureFolder("Assets/Scene", "Scene_Level1");
            NavMeshData existingData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath);
            if (existingData == null)
            {
                AssetDatabase.CreateAsset(builtData, NavMeshDataPath);
            }
            else if (existingData != builtData)
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(builtData, existingData);
                UnityEngine.Object.DestroyImmediate(builtData);
                surface.navMeshData = existingData;
                EditorUtility.SetDirty(existingData);
                surface.AddData();
            }

            EditorUtility.SetDirty(surface);
        }

        private static void ClearLegacyNavMeshRegistration()
        {
            Type builderType = Type.GetType("UnityEditor.AI.NavMeshBuilder, UnityEditor");
            MethodInfo clear = builderType?.GetMethod("ClearAllNavMeshes", BindingFlags.Public | BindingFlags.Static);
            clear?.Invoke(null, null);
        }

        private static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab == null || prefab.GetComponent<LevelOneEnemyBrain>() == null ||
                prefab.GetComponent<CombatHealth>() == null || prefab.GetComponent<NavMeshAgent>() == null)
            {
                throw new InvalidOperationException("Generated enemy prefab is incomplete.");
            }

            if (!prefab.GetComponent<EnemyAimPose>().IsConfigured || prefab.GetComponentInChildren<WorldHealthBar>(true) == null)
            {
                throw new InvalidOperationException("Enemy aiming pose or health bar is not configured.");
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            GameObject director = scene.GetRootGameObjects().FirstOrDefault(root => root.name == DirectorName);
            if (director == null || director.GetComponent<LevelOneEnemyDirector>() == null)
            {
                throw new InvalidOperationException("Scene_Level1 does not contain the enemy director.");
            }

            NavMeshSurface surface = scene.GetRootGameObjects()
                .Select(root => root.GetComponent<NavMeshSurface>())
                .FirstOrDefault(candidate => candidate != null);
            if (surface == null || surface.navMeshData == null)
            {
                throw new InvalidOperationException("Scene_Level1 does not contain a baked AI Navigation NavMeshSurface.");
            }

            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
            GameObject player = scene.GetRootGameObjects().First(root => root.name == PlayerName);
            if (player.layer != playerLayer || prefab.layer != enemyLayer ||
                !Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer))
            {
                throw new InvalidOperationException("Player/Enemy layers or their collision-matrix exclusion are not configured.");
            }

            NavMeshTriangulation navigation = NavMesh.CalculateTriangulation();
            if (navigation.vertices == null || navigation.vertices.Length == 0)
            {
                throw new InvalidOperationException("Level 1 NavMesh contains no walkable vertices.");
            }

            FirstPersonControllerJumpCheck();
            Debug.Log($"LEVEL1_ENEMY_VALID navVertices={navigation.vertices.Length} surface={surface.name} initial=5 reinforcement=2 interval=60 retreatChance=0.10 retreatSeconds=3 playerEnemyCollision=ignored");
        }

        private static void FirstPersonControllerJumpCheck()
        {
            GameObject player = SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects().First(root => root.name == PlayerName);
            StarterAssets.FirstPersonController movement = player.GetComponent<StarterAssets.FirstPersonController>();
            if (movement == null || movement.JumpHeight <= 0f)
            {
                throw new InvalidOperationException("Level 1 player jump is not enabled.");
            }
        }

        private static Transform FindBone(GameObject body, params string[] tokens)
        {
            Transform[] transforms = body.GetComponentsInChildren<Transform>(true);

            // Mixamo rigs use names such as "mixamorig:LeftArm". Prefer a suffix
            // match so LeftArm cannot be confused with LeftForeArm.
            foreach (string token in tokens)
            {
                string normalizedToken = Normalize(token);
                Transform exact = transforms.FirstOrDefault(item =>
                    Normalize(item.name).EndsWith(normalizedToken, StringComparison.Ordinal));
                if (exact != null)
                {
                    return exact;
                }
            }

            return transforms.FirstOrDefault(item =>
                tokens.Any(token => Normalize(item.name).Contains(Normalize(token))));
        }

        private static string Normalize(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
        {
            Transform marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = localPosition;
            return marker;
        }

        private static void FitModel(GameObject model, float targetLargestDimension, bool groundAtZero)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Model {model.name} contains no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float sourceDimension = groundAtZero ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= targetLargestDimension / Mathf.Max(0.0001f, sourceDimension);

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 correction = -model.transform.parent.InverseTransformPoint(bounds.center);
            if (groundAtZero)
            {
                correction.y = -model.transform.parent.InverseTransformPoint(bounds.min).y;
            }
            model.transform.localPosition += correction;
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Serialized property {propertyName} was not found on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePhysicsLayers()
        {
            int playerLayer = EnsureLayer(PlayerLayerName);
            int enemyLayer = EnsureLayer(EnemyLayerName);
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);

            UnityEngine.Object physicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")
                .FirstOrDefault();
            if (physicsSettings != null)
            {
                EditorUtility.SetDirty(physicsSettings);
            }

            AssetDatabase.SaveAssets();
        }

        private static int EnsureLayer(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")
                .FirstOrDefault() ?? throw new InvalidOperationException("Unity TagManager settings could not be loaded.");
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int index = 8; index < layers.arraySize; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrWhiteSpace(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssets();
                    return index;
                }
            }

            throw new InvalidOperationException($"No free Unity layer is available for {layerName}.");
        }

        private static void ApplyLayerToPrefab(string prefabPath, int layer)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SetLayerRecursively(prefabRoot, layer);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
