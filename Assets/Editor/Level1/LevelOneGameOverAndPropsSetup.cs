using System;
using System.IO;
using System.Linq;
using DefenderOfIndependence.Level1;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOneGameOverAndPropsSetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level1.unity";
        private const string PlayerPrefabPath = "Assets/Level1/Prefabs/Player/LevelOnePlayer.prefab";
        private const string EnemyPrefabPath = "Assets/Level1/Prefabs/Enemy/LevelOneEnemy.prefab";
        private const string HostageModelPath = "Assets/Level1/Art/Hostage/Model/Prison.fbx";
        private const string HostageTexturePath = "Assets/Level1/Art/Hostage/Textures/PowerPlantA.png";
        private const string HostageMaterialPath = "Assets/Level1/Art/Hostage/Materials/Hostage.mat";
        private const string HostagePrefabPath = "Assets/Level1/Prefabs/Props/Hostage.prefab";
        private const string TentModelPath = "Assets/Level1/Art/Tent/Model/Tent.fbx";
        private const string TentColorPath = "Assets/Level1/Art/Tent/Textures/Tent_C.jpeg";
        private const string TentNormalPath = "Assets/Level1/Art/Tent/Textures/Tent_NRM.jpeg";
        private const string TentMaterialPath = "Assets/Level1/Art/Tent/Materials/Tent.mat";
        private const string TentPrefabPath = "Assets/Level1/Prefabs/Props/Tent.prefab";
        private const string FontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
        private const string ObjectiveControllerName = "Level 1 Objective Controller";

        [MenuItem("Project Tools/Level 1/Apply Game Over, Health, and Props")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                Debug.Log("LEVEL1_GAMEOVER_PROPS_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_GAMEOVER_PROPS_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureModel(HostageModelPath);
            ConfigureModel(TentModelPath);
            ConfigureTexture(HostageTexturePath, false);
            ConfigureTexture(TentColorPath, false);
            ConfigureTexture(TentNormalPath, true);

            Material hostageMaterial = CreateMaterial(HostageMaterialPath, HostageTexturePath, null, 0.25f);
            Material tentMaterial = CreateMaterial(TentMaterialPath, TentColorPath, TentNormalPath, 0.2f);
            GameObject hostagePrefab = CreatePropPrefab(HostageModelPath, hostageMaterial, HostagePrefabPath, "Hostage", 1.72f, true);
            GameObject tentPrefab = CreatePropPrefab(TentModelPath, tentMaterial, TentPrefabPath, "Tent", 4.8f, false);

            ConfigurePlayerPrefab();
            ConfigureEnemyPrefab();
            ConfigureScene(hostagePrefab, tentPrefab);
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                CombatHealth health = root.GetComponent<CombatHealth>() ?? throw new InvalidOperationException("Player prefab has no CombatHealth.");
                FirstPersonController movement = root.GetComponent<FirstPersonController>();
                FirstPersonWeaponController weapon = root.GetComponent<FirstPersonWeaponController>();
                PlayerInput playerInput = root.GetComponent<PlayerInput>();
                FirstPersonWeaponHud hud = root.GetComponentInChildren<FirstPersonWeaponHud>(true);
                if (movement == null || weapon == null || playerInput == null || hud == null)
                {
                    throw new InvalidOperationException("Player prefab is missing movement, weapon, input, or HUD components.");
                }

                SetFloat(weapon, "minimumDamage", 5f);
                SetFloat(weapon, "maximumDamage", 10f);

                Transform canvas = hud.transform;
                DestroyChild(canvas, "Damage Flash Overlay");
                DestroyChild(canvas, "Game Over Panel");
                DestroyChild(canvas, "Objective Panel");
                DestroyChild(canvas, "Hostage Warning Prompt");
                DestroyChild(canvas, "Victory Panel");
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

                Image damageOverlay = CreateImage(canvas, "Damage Flash Overlay", new Color(0.72f, 0f, 0f, 0f));
                Stretch(damageOverlay.rectTransform, Vector2.zero, Vector2.zero);
                damageOverlay.raycastTarget = false;
                damageOverlay.transform.SetAsFirstSibling();

                GameObject gameOverPanel = CreateGameOverPanel(canvas, font, out Button restartButton, out Button menuButton);
                CreateObjectiveHud(canvas, font);
                CreateVictoryPanel(canvas, font);
                PlayerDamageFeedback feedback = root.GetComponent<PlayerDamageFeedback>() ?? root.AddComponent<PlayerDamageFeedback>();
                SetReference(feedback, "health", health);
                SetReference(feedback, "damageOverlay", damageOverlay);
                SetFloat(feedback, "peakAlpha", 0.28f);
                SetFloat(feedback, "holdDuration", 0.08f);
                SetFloat(feedback, "fadeDuration", 0.65f);

                LevelOneGameOverController gameOver = root.GetComponent<LevelOneGameOverController>() ?? root.AddComponent<LevelOneGameOverController>();
                SetReference(gameOver, "health", health);
                SetReference(gameOver, "movement", movement);
                SetReference(gameOver, "weapon", weapon);
                SetReference(gameOver, "playerInput", playerInput);
                SetReference(gameOver, "gameOverPanel", gameOverPanel);
                SetReference(gameOver, "restartButton", restartButton);
                SetReference(gameOver, "backToMenuButton", menuButton);
                SetString(gameOver, "mainMenuSceneName", "Scene_MainMenu");

                Transform healthBar = canvas.Find("Player Health Bar Background");
                Slider playerHealthSlider = null;
                if (healthBar != null)
                {
                    healthBar.gameObject.SetActive(true);
                    Image fill = healthBar.Find("Player Health Bar Fill")?.GetComponent<Image>();
                    if (fill != null)
                    {
                        fill.type = Image.Type.Simple;
                        playerHealthSlider = ConfigureSlider(healthBar.gameObject, fill);
                    }
                }

                SetReference(hud, "playerHealth", health);
                SetReference(hud, "playerHealthSlider", playerHealthSlider);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateGameOverPanel(Transform canvas, TMP_FontAsset font, out Button restart, out Button menu)
        {
            Image panel = CreateImage(canvas, "Game Over Panel", new Color(0.015f, 0.02f, 0.03f, 0.9f));
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;

            TMP_Text title = CreateText(panel.transform, "Game Over Prompt", font, "GAME OVER", 68f);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 125f), new Vector2(700f, 100f));
            title.color = new Color(0.95f, 0.22f, 0.16f, 1f);

            TMP_Text hint = CreateText(panel.transform, "Game Over Hint", font, "The defence has fallen", 22f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(700f, 46f));
            hint.color = new Color(0.82f, 0.84f, 0.88f, 1f);

            restart = CreateButton(panel.transform, "Restart Button", font, "RESTART", new Vector2(0f, -28f));
            menu = CreateButton(panel.transform, "Back To Menu Button", font, "BACK TO MENU", new Vector2(0f, -108f));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static Button CreateButton(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position)
        {
            Image image = CreateImage(parent, name, new Color(0.14f, 0.17f, 0.21f, 0.98f));
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(360f, 58f));
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.78f, 0.42f, 1f);
            colors.pressedColor = new Color(0.9f, 0.42f, 0.24f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText(image.transform, "Label", font, label, 24f);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void CreateObjectiveHud(Transform canvas, TMP_FontAsset font)
        {
            Image panel = CreateImage(canvas, "Objective Panel", new Color(0.025f, 0.035f, 0.05f, 0.82f));
            SetRect(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(305f, -92f), new Vector2(570f, 130f));
            panel.raycastTarget = false;

            TMP_Text task = CreateText(panel.transform, "Task Text", font, "OBJECTIVE: FIND A HOSTAGE AND PRESS C", 21f);
            SetRect(task.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(530f, 42f));
            task.alignment = TextAlignmentOptions.Left;
            TMP_Text rescued = CreateText(panel.transform, "Saved Count Text", font, "HOSTAGES SAVED: 0 / 2", 20f);
            SetRect(rescued.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(530f, 38f));
            rescued.alignment = TextAlignmentOptions.Left;
            rescued.color = new Color(1f, 0.72f, 0.22f, 1f);

            TMP_Text warning = CreateText(canvas, "Hostage Warning Prompt", font, string.Empty, 25f);
            SetRect(warning.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(850f, 52f));
            warning.color = new Color(1f, 0.34f, 0.18f, 1f);
            warning.gameObject.SetActive(false);
        }

        private static void CreateVictoryPanel(Transform canvas, TMP_FontAsset font)
        {
            Image panel = CreateImage(canvas, "Victory Panel", new Color(0.015f, 0.04f, 0.035f, 0.92f));
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;

            TMP_Text title = CreateText(panel.transform, "Victory Prompt", font, "MISSION COMPLETE", 62f);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(850f, 90f));
            title.color = new Color(0.3f, 1f, 0.62f, 1f);
            TMP_Text message = CreateText(panel.transform, "Victory Message", font, "CONGRATULATIONS - BOTH HOSTAGES ARE SAFE", 22f);
            SetRect(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(850f, 46f));
            CreateButton(panel.transform, "Next Level Button", font, "NEXT LEVEL", new Vector2(0f, -48f));
            panel.gameObject.SetActive(false);
        }

        private static void ConfigureEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                LevelOneEnemyBrain brain = root.GetComponent<LevelOneEnemyBrain>() ?? throw new InvalidOperationException("Enemy prefab has no brain.");
                SetFloat(brain, "minimumWeaponDamage", 5f);
                SetFloat(brain, "maximumWeaponDamage", 10f);
                WorldHealthBar bar = root.GetComponentInChildren<WorldHealthBar>(true) ?? throw new InvalidOperationException("Enemy prefab has no health bar.");
                bar.gameObject.SetActive(true);
                Image fill = bar.transform.Find("Background/Fill")?.GetComponent<Image>();
                if (fill != null)
                {
                    fill.type = Image.Type.Simple;
                    Slider slider = ConfigureSlider(fill.transform.parent.gameObject, fill);
                    SetReference(bar, "slider", slider);
                }

                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureScene(GameObject hostagePrefab, GameObject tentPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Level 1 Player")
                                ?? throw new InvalidOperationException("Scene_Level1 has no Level 1 Player.");
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) == null)
            {
                GameObject eventSystem = new GameObject("Level 1 Event System", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            Transform siteA = FindRoot(scene, "Hostage1Location")?.transform;
            Transform siteB = FindRoot(scene, "Hostage2Location")?.transform;
            if (siteA == null || siteB == null)
            {
                throw new InvalidOperationException("Both hostage location markers are required.");
            }

            PlaceAtMarker(hostagePrefab, siteA, "Hostage A");
            PlaceAtMarker(hostagePrefab, siteB, "Hostage B");

            Transform tentLocation = FindRoot(scene, "TentLocation")?.transform;
            GameObject tentInstance = null;
            if (tentLocation != null)
            {
                tentInstance = PlaceAtMarker(tentPrefab, tentLocation, "Tent");
            }
            else
            {
                Debug.LogWarning("LEVEL1_TENT_NOT_PLACED: add a root marker named TentLocation, then rerun this setup command.");
            }

            ConfigureObjectiveController(scene, player, siteA, siteB, tentInstance);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject PlaceAtMarker(GameObject prefab, Transform marker, string instanceName)
        {
            foreach (Transform child in marker.Cast<Transform>().Where(item => item.name == instanceName).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, marker.gameObject.scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate {prefab.name}.");
            }

            instance.name = instanceName;
            instance.transform.SetParent(marker, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void ConfigureObjectiveController(Scene scene, GameObject player, Transform siteA, Transform siteB, GameObject tentInstance)
        {
            HostageEscort hostageA = siteA.Find("Hostage A")?.GetComponent<HostageEscort>();
            HostageEscort hostageB = siteB.Find("Hostage B")?.GetComponent<HostageEscort>();
            TentRescueZone tentZone = tentInstance?.GetComponent<TentRescueZone>();
            if (hostageA == null || hostageB == null || tentZone == null)
            {
                throw new InvalidOperationException("Hostage or tent gameplay components are missing from their scene instances.");
            }

            GameObject controllerObject = FindRoot(scene, ObjectiveControllerName);
            if (controllerObject == null)
            {
                controllerObject = new GameObject(ObjectiveControllerName);
                SceneManager.MoveGameObjectToScene(controllerObject, scene);
            }

            LevelOneObjectiveController controller = controllerObject.GetComponent<LevelOneObjectiveController>() ??
                                                     controllerObject.AddComponent<LevelOneObjectiveController>();
            Transform hud = player.transform.Find("Level 1 Player HUD") ?? throw new InvalidOperationException("Player HUD is missing.");
            SetReference(controller, "player", player.transform);
            SetReference(controller, "playerHealth", player.GetComponent<CombatHealth>());
            SetArrayReferences(controller, "hostages", hostageA, hostageB);
            SetReference(controller, "tentRescueZone", tentZone);
            SetReference(controller, "movement", player.GetComponent<FirstPersonController>());
            SetReference(controller, "weapon", player.GetComponent<FirstPersonWeaponController>());
            SetReference(controller, "playerInput", player.GetComponent<PlayerInput>());
            SetReference(controller, "taskText", hud.Find("Objective Panel/Task Text")?.GetComponent<TMP_Text>());
            SetReference(controller, "rescuedText", hud.Find("Objective Panel/Saved Count Text")?.GetComponent<TMP_Text>());
            SetReference(controller, "warningText", hud.Find("Hostage Warning Prompt")?.GetComponent<TMP_Text>());
            SetReference(controller, "victoryPanel", hud.Find("Victory Panel")?.gameObject);
            SetReference(controller, "nextLevelButton", hud.Find("Victory Panel/Next Level Button")?.GetComponent<Button>());
            SetString(controller, "nextCutsceneName", "Cutscene_Level2");
        }

        private static void Validate()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (player?.GetComponent<LevelOneGameOverController>() == null || player.GetComponent<PlayerDamageFeedback>() == null)
            {
                throw new InvalidOperationException("Player game-over or damage feedback component is missing.");
            }

            if (player.transform.Find("Level 1 Player HUD/Game Over Panel") == null ||
                player.transform.Find("Level 1 Player HUD/Damage Flash Overlay") == null)
            {
                throw new InvalidOperationException("Player HUD game-over or damage overlay is missing.");
            }

            WorldHealthBar enemyBar = enemy?.GetComponentInChildren<WorldHealthBar>(true);
            if (enemyBar == null || !enemyBar.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Enemy health bar is not active.");
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Transform siteA = FindRoot(scene, "Hostage1Location")?.transform;
            Transform siteB = FindRoot(scene, "Hostage2Location")?.transform;
            if (siteA?.Find("Hostage A") == null || siteB?.Find("Hostage B") == null)
            {
                throw new InvalidOperationException("Hostages were not placed at both location markers.");
            }

            LevelOneObjectiveController objective = FindRoot(scene, ObjectiveControllerName)?.GetComponent<LevelOneObjectiveController>();
            if (objective == null || FindRoot(scene, "TentLocation")?.transform.Find("Tent")?.GetComponent<TentRescueZone>() == null)
            {
                throw new InvalidOperationException("Level 1 hostage objective controller or tent rescue zone is missing.");
            }

            Debug.Log("LEVEL1_GAMEOVER_PROPS_VALID gameOver=ready victory=ready damage=5-10 healthBars=sliders hostages=2 tentRescue=ready next=Cutscene_Level2");
        }

        private static void ConfigureModel(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter
                                     ?? throw new FileNotFoundException($"Missing model: {path}");
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.SaveAndReimport();
        }

        private static void ConfigureTexture(string path, bool normalMap)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                                       ?? throw new FileNotFoundException($"Missing texture: {path}");
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string path, string colorPath, string normalPath, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP/Lit shader is unavailable.");
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

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            if (!string.IsNullOrEmpty(normalPath))
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePropPrefab(string modelPath, Material material, string prefabPath, string name, float targetSize, bool useHeight)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath)
                                    ?? throw new FileNotFoundException($"Imported model is missing: {modelPath}");
            GameObject root = new GameObject(name);
            try
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                visual.name = name + " Visual";
                visual.transform.SetParent(root.transform, false);
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                    renderer.receiveShadows = true;
                }

                FitModel(visual, targetSize, useHeight);
                if (name == "Hostage")
                {
                    CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                    collider.center = new Vector3(0f, 0.86f, 0f);
                    collider.height = 1.72f;
                    collider.radius = 0.34f;
                    collider.isTrigger = true;
                    Rigidbody body = root.AddComponent<Rigidbody>();
                    body.isKinematic = true;
                    body.useGravity = false;
                    root.AddComponent<HostageEscort>();
                }
                else if (name == "Tent")
                {
                    Bounds bounds = CalculateBounds(visual);
                    BoxCollider collider = root.AddComponent<BoxCollider>();
                    collider.center = root.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                    collider.isTrigger = true;
                    root.AddComponent<TentRescueZone>();
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return prefab ?? throw new InvalidOperationException($"Could not save prefab: {prefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void FitModel(GameObject visual, float targetSize, bool useHeight)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{visual.name} has no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float sourceSize = useHeight ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            visual.transform.localScale *= targetSize / Mathf.Max(0.0001f, sourceSize);
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 correction = -visual.transform.parent.InverseTransformPoint(bounds.center);
            correction.y = -visual.transform.parent.InverseTransformPoint(bounds.min).y;
            visual.transform.localPosition += correction;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static Slider ConfigureSlider(GameObject owner, Image fill)
        {
            Slider slider = owner.GetComponent<Slider>() ?? owner.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(1f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = null;
            slider.targetGraphic = fill;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string content, float size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(item => item.name == name);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name) ?? throw new InvalidOperationException($"Missing property {name} on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string name, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name) ?? throw new InvalidOperationException($"Missing property {name} on {target.GetType().Name}.");
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string name, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name) ?? throw new InvalidOperationException($"Missing property {name} on {target.GetType().Name}.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArrayReferences(UnityEngine.Object target, string name, params UnityEngine.Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name) ?? throw new InvalidOperationException($"Missing property {name} on {target.GetType().Name}.");
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
