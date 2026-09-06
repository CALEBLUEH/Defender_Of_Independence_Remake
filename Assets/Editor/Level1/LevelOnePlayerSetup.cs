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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOnePlayerSetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level1.unity";
        private const string SpawnName = "PlayerSpawnPoint";
        private const string PlayerName = "Level 1 Player";
        private const string StarterPlayerPrefab = "Assets/Starter Assets/Runtime/FirstPersonController/Prefabs/PlayerCapsule.prefab";
        private const string InputActionsPath = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
        private const string BodyModelPath = "Assets/Level1/Art/Player/CounterTerrorist/Model/CounterTerrorist.gltf";
        private const string GunModelPath = "Assets/Level1/Art/Player/Weapon/Deagle/Model/Deagle_CS2.fbx";
        private const string GunTextureFolder = "Assets/Level1/Art/Player/Weapon/Deagle/Textures";
        private const string GunMaterialPath = "Assets/Level1/Art/Player/Weapon/Deagle/Materials/Deagle_Player.mat";
        private const string PlayerPrefabPath = "Assets/Level1/Prefabs/Player/LevelOnePlayer.prefab";
        private const string FontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";

        [MenuItem("Project Tools/Level 1/Rebuild First Person Player")]
        public static void RebuildFromMenu()
        {
            if (EditorUtility.DisplayDialog(
                    "Rebuild Level 1 Player",
                    "This rebuilds the generated player prefab and replaces only the Level 1 Player instance. The PlayerSpawnPoint is preserved.",
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
                Debug.Log("LEVEL1_PLAYER_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_PLAYER_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        public static void CapturePreviewFromCommandLine()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject player = scene.GetRootGameObjects().FirstOrDefault(root => root.name == PlayerName);
                Camera camera = player == null ? null : player.GetComponentInChildren<Camera>(true);
                if (camera == null)
                {
                    throw new InvalidOperationException("The generated Level 1 player camera is missing.");
                }

                RenderTexture target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                Texture2D preview = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    preview.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    preview.Apply();
                    string output = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "LevelOnePlayerPreview.png");
                    File.WriteAllBytes(output, preview.EncodeToPNG());
                    Debug.Log($"LEVEL1_PLAYER_PREVIEW_SAVED path={output}");
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(preview);
                    UnityEngine.Object.DestroyImmediate(target);
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
            EnsureFolder("Assets/Level1", "Prefabs");
            EnsureFolder("Assets/Level1/Prefabs", "Player");
            EnsureFolder("Assets/Level1/Art/Player/Weapon/Deagle", "Materials");

            ConfigureGunImport();
            Material gunMaterial = CreateGunMaterial();
            GameObject playerPrefab = CreatePlayerPrefab(gunMaterial);
            PlacePlayer(playerPrefab);
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureGunImport()
        {
            AssetDatabase.ImportAsset(GunModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(GunModelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException($"Could not import pistol FBX at {GunModelPath}.");
            }

            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.SaveAndReimport();

            ConfigureTexture("deagle_default_color.png", false, true);
            ConfigureTexture("deagle_default_normal_tga_f25ac800.png", true, false);
            ConfigureTexture("deagle_default_ao_tga_b7d74bd_orm.png", false, false);
            ConfigureTexture("deagle_default_rough.png", false, false);
        }

        private static void ConfigureTexture(string fileName, bool normalMap, bool srgb)
        {
            string path = $"{GunTextureFolder}/{fileName}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new FileNotFoundException($"Missing pistol texture: {path}");
            }

            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = 2048;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material CreateGunMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP/Lit shader is unavailable.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(GunMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GunMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GunTextureFolder}/deagle_default_color.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GunTextureFolder}/deagle_default_normal_tga_f25ac800.png");
            Texture2D orm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GunTextureFolder}/deagle_default_ao_tga_b7d74bd_orm.png");
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", orm);
            material.SetFloat("_Metallic", 0.75f);
            material.SetFloat("_Smoothness", 0.62f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePlayerPrefab(Material gunMaterial)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(StarterPlayerPrefab);
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            GameObject bodyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BodyModelPath);
            GameObject gunAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GunModelPath);
            if (source == null || actions == null || bodyAsset == null || gunAsset == null)
            {
                throw new InvalidOperationException(
                    $"Required player assets are missing. starter={source != null}, actions={actions != null}, body={bodyAsset != null}, gun={gunAsset != null}");
            }

            GameObject player = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Could not instantiate the Starter Assets player prefab.");
            }

            try
            {
                player.name = PlayerName;
                Transform capsuleVisual = player.transform.Find("Capsule");
                if (capsuleVisual != null)
                {
                    UnityEngine.Object.DestroyImmediate(capsuleVisual.gameObject);
                }

                FirstPersonController movement = player.GetComponent<FirstPersonController>();
                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                CharacterController character = player.GetComponent<CharacterController>();
                Transform cameraRoot = player.GetComponentsInChildren<Transform>(true).First(transform => transform.name == "PlayerCameraRoot");
                movement.MoveSpeed = 5f;
                movement.SprintSpeed = 2.5f;
                movement.JumpHeight = 0f;
                movement.RotationSpeed = 1f;
                movement.CinemachineCameraTarget = cameraRoot.gameObject;
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Player";
                playerInput.defaultControlScheme = "KeyboardMouse";
                playerInput.notificationBehavior = PlayerNotifications.SendMessages;

                Camera camera = CreateCamera(cameraRoot);
                LevelOnePlayerInput levelInput = player.AddComponent<LevelOnePlayerInput>();
                FirstPersonWeaponHud hud = CreateHud(player.transform, out TMP_Text interactionPrompt);
                Transform weaponView = CreateWeaponView(camera.transform, gunAsset, gunMaterial);

                FirstPersonWeaponController weapon = player.AddComponent<FirstPersonWeaponController>();
                SetReference(weapon, "input", levelInput);
                SetReference(weapon, "aimCamera", camera);
                SetReference(weapon, "weaponView", weaponView);
                SetReference(weapon, "characterController", character);
                SetReference(weapon, "hud", hud);

                LevelOnePlayerInteractor interactor = player.AddComponent<LevelOnePlayerInteractor>();
                SetReference(interactor, "input", levelInput);
                SetReference(interactor, "playerCamera", camera);
                SetReference(interactor, "promptText", interactionPrompt);

                AddShadowBody(player.transform, bodyAsset);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Could not save player prefab at {PlayerPrefabPath}.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static Camera CreateCamera(Transform cameraRoot)
        {
            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraRoot, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 250f;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            return camera;
        }

        private static Transform CreateWeaponView(Transform camera, GameObject gunAsset, Material material)
        {
            GameObject mount = new GameObject("Pistol Viewmodel");
            mount.transform.SetParent(camera, false);
            mount.transform.localPosition = new Vector3(0.24f, -0.20f, 0.48f);
            mount.transform.localRotation = Quaternion.Euler(1f, 180f, 0f);

            GameObject geometry = PrefabUtility.InstantiatePrefab(gunAsset) as GameObject;
            geometry.name = "Deagle Geometry";
            geometry.transform.SetParent(mount.transform, false);
            geometry.transform.localPosition = Vector3.zero;
            geometry.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            FitModel(geometry, 0.44f, false);
            return mount.transform;
        }

        private static void AddShadowBody(Transform player, GameObject bodyAsset)
        {
            GameObject body = PrefabUtility.InstantiatePrefab(bodyAsset) as GameObject;
            body.name = "Counter Terrorist Body (Shadows Only)";
            body.transform.SetParent(player, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            FitModel(body, 1.78f, true);

            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.receiveShadows = true;
            }
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

            Vector3 centerInParent = model.transform.parent.InverseTransformPoint(bounds.center);
            Vector3 correction = -centerInParent;
            if (groundAtZero)
            {
                float bottomInParent = model.transform.parent.InverseTransformPoint(bounds.min).y;
                correction.y = -bottomInParent;
            }
            model.transform.localPosition += correction;
        }

        private static FirstPersonWeaponHud CreateHud(Transform player, out TMP_Text interactionPrompt)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject canvasObject = new GameObject("Level 1 Player HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(player, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            CreateCrosshair(canvasObject.transform);
            TMP_Text ammo = CreateText(canvasObject.transform, "Ammo", font, 30, TextAlignmentOptions.BottomRight);
            SetRect(ammo.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-310, 35), new Vector2(275, 55));
            TMP_Text mode = CreateText(canvasObject.transform, "Fire Mode", font, 20, TextAlignmentOptions.BottomRight);
            SetRect(mode.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-310, 92), new Vector2(275, 40));
            interactionPrompt = CreateText(canvasObject.transform, "Interaction Prompt", font, 24, TextAlignmentOptions.Center);
            SetRect(interactionPrompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -125), new Vector2(520, 50));
            interactionPrompt.gameObject.SetActive(false);

            FirstPersonWeaponHud hud = canvasObject.AddComponent<FirstPersonWeaponHud>();
            SetReference(hud, "ammoText", ammo);
            SetReference(hud, "fireModeText", mode);
            return hud;
        }

        private static void CreateCrosshair(Transform parent)
        {
            CreateCrosshairBar(parent, "Crosshair Horizontal", new Vector2(18, 2));
            CreateCrosshairBar(parent, "Crosshair Vertical", new Vector2(2, 18));
        }

        private static void CreateCrosshairBar(Transform parent, string name, Vector2 size)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            Image image = bar.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);
            SetRect(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void PlacePlayer(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject spawn = scene.GetRootGameObjects().FirstOrDefault(root => root.name == SpawnName);
            if (spawn == null)
            {
                throw new InvalidOperationException($"Scene_Level1 needs a root object named {SpawnName}.");
            }

            foreach (GameObject existing in scene.GetRootGameObjects().Where(root => root.name == PlayerName).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            foreach (Camera camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray())
            {
                if (camera.transform.root.name != PlayerName)
                {
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
            }

            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = PlayerName;
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null || prefab.GetComponent<FirstPersonController>() == null || prefab.GetComponent<PlayerInput>() == null)
            {
                throw new InvalidOperationException("Generated player prefab is incomplete.");
            }

            if (prefab.GetComponentInChildren<Camera>(true) == null || prefab.GetComponent<FirstPersonWeaponController>() == null)
            {
                throw new InvalidOperationException("Generated first-person camera or weapon controller is missing.");
            }

            Transform bodyRoot = prefab.transform.Find("Counter Terrorist Body (Shadows Only)");
            if (bodyRoot == null)
            {
                throw new InvalidOperationException("The hidden player body is missing from the generated prefab.");
            }

            Renderer[] bodyRenderers = prefab.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.transform.IsChildOf(bodyRoot))
                .ToArray();
            if (bodyRenderers.Length == 0 || bodyRenderers.Any(renderer => renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly))
            {
                throw new InvalidOperationException("The hidden player body is not configured for shadows-only rendering.");
            }

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            string[] requiredActions = { "Move", "Look", "Sprint", "Fire", "ToggleFireMode", "Reload", "Interact" };
            foreach (string action in requiredActions)
            {
                if (actions.FindAction($"Player/{action}") == null)
                {
                    throw new InvalidOperationException($"Missing Player/{action} input action.");
                }
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            GameObject player = scene.GetRootGameObjects().FirstOrDefault(root => root.name == PlayerName);
            if (player == null)
            {
                throw new InvalidOperationException("Scene_Level1 does not contain the generated player.");
            }

            Debug.Log($"LEVEL1_PLAYER_VALID position={player.transform.position} bodyRenderers={bodyRenderers.Length} controls=WASD,ShiftWalk,MouseAim,LMBFire,RMBMode,RReload,CInteract");
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property {propertyName} was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
