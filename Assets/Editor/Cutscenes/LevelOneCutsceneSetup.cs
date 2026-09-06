using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderOfIndependence.Cutscenes;
using DefenderOfIndependence.SceneFlow;
using Fungus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOneCutsceneSetup
    {
        private const string MainMenuScene = "Assets/Scene/Scene_MainMenu.unity";
        private const string CutsceneScene = "Assets/Scene/Cutscene_Level1.unity";
        private const string LevelSceneName = "Scene_Level1";
        private const string CutsceneSceneName = "Cutscene_Level1";
        private const string PanelAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/panel_beige.png";
        private const string PromptAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/buttonLong_brown.png";
        private const string FontAsset = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string SmokeActiveKey = "DOI.LevelOneCutsceneSmoke.Active";
        private const string SmokePhaseKey = "DOI.LevelOneCutsceneSmoke.Phase";
        private const string SmokeDeadlineKey = "DOI.LevelOneCutsceneSmoke.Deadline";

        private static double nextSmokeActionTime;

        private static readonly string[] BuildScenePaths =
        {
            "Assets/Scene/Scene_MainMenu.unity",
            "Assets/Scene/Cutscene_Level1.unity",
            "Assets/Scene/Scene_Level1.unity",
            "Assets/Scene/Cutscene_Level2.unity",
            "Assets/Scene/Scene_Level2.unity",
            "Assets/Scene/Cutscene_Level3.unity",
            "Assets/Scene/Scene_Level3.unity",
            "Assets/Scene/Scene_Gallery.unity"
        };

        private static readonly DialogueLine[] LevelOneDialogue =
        {
            new DialogueLine("Narrator", "Malaya, December 1941. For years, the country had been administered under British colonial rule while calls for a future shaped by its own people continued to grow."),
            new DialogueLine("Narrator", "Then war reached the peninsula. Japanese forces landed in northern Malaya and advanced rapidly through towns, roads and plantations."),
            new DialogueLine("Narrator", "British defenses collapsed under the attack. By February 1942, Singapore had fallen and Malaya was under Japanese occupation."),
            new DialogueLine("Narrator", "Families endured fear, shortages and uncertainty. The hardship of occupation strengthened the belief that Malaya must one day determine its own future."),
            new DialogueLine("Narrator", "Tonight, the fighting has reached your sector. Two civilians are trapped beyond the defensive line, and the route back to camp is surrounded."),
            new DialogueLine("Commanding Officer", "Soldier, locate both hostages and escort them safely to the camp. You do not need to defeat every enemy; bringing the civilians home is the mission."),
            new DialogueLine("Commanding Officer", "Stay alert, conserve your ammunition and protect those who follow you. Move out when you are ready.")
        };

        [MenuItem("Project Tools/Cutscenes/Rebuild Level 1 Cutscene")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Level 1 Cutscene",
                    "This replaces only the generated Level 1 cutscene root and rewires the Main Menu Start button. Continue?",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            Rebuild();
        }

        public static void Rebuild()
        {
            ConfigureSprite(PanelAsset, new Vector4(10f, 10f, 10f, 10f));
            ConfigureSprite(PromptAsset, new Vector4(10f, 10f, 10f, 10f));
            ConfigureMainMenu();
            ConfigureLevelOneCutscene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateSetup();
            Debug.Log("LEVEL1_CUTSCENE_SETUP_SUCCESS");
        }

        public static void CapturePreview()
        {
            Scene scene = EditorSceneManager.OpenScene(CutsceneScene, OpenSceneMode.Single);
            GameObject cutsceneRoot = scene.GetRootGameObjects().First(root => root.name == "Level 1 Cutscene");
            Canvas canvas = cutsceneRoot.GetComponentInChildren<Canvas>(true);
            Camera camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .First();

            TMP_Text speaker = FindNamedComponent<TMP_Text>(cutsceneRoot, "Speaker");
            TMP_Text dialogue = FindNamedComponent<TMP_Text>(cutsceneRoot, "Dialogue Text");
            TMP_Text prompt = FindNamedComponent<TMP_Text>(cutsceneRoot, "Prompt Text");
            CanvasGroup promptGroup = FindNamedComponent<CanvasGroup>(cutsceneRoot, "Next Prompt");

            speaker.text = "COMMANDING OFFICER";
            dialogue.text = "Soldier, locate both hostages and escort them safely to the camp. You do not need to defeat every enemy; bringing the civilians home is the mission.";
            prompt.text = "PRESS SPACE TO CONTINUE";
            promptGroup.alpha = 1f;
            canvas.GetComponent<CanvasGroup>().alpha = 1f;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            const int width = 1920;
            const int height = 1080;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                preview.Apply();

                string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../LevelOneCutscenePreview.png"));
                File.WriteAllBytes(outputPath, preview.EncodeToPNG());
                Debug.Log($"LEVEL1_CUTSCENE_PREVIEW:{outputPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        [MenuItem("Project Tools/Cutscenes/Run Level 1 Flow Smoke Test")]
        public static void RunSmokeTest()
        {
            SessionState.SetBool(SmokeActiveKey, true);
            SessionState.SetString(SmokePhaseKey, "EnterPlayMode");
            SessionState.SetString(SmokeDeadlineKey, DateTime.UtcNow.AddSeconds(360).Ticks.ToString());
            EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            RegisterSmokeTestUpdate();
        }

        [InitializeOnLoadMethod]
        private static void ResumeSmokeTestAfterReload()
        {
            if (SessionState.GetBool(SmokeActiveKey, false))
            {
                RegisterSmokeTestUpdate();
            }
        }

        private static void RegisterSmokeTestUpdate()
        {
            EditorApplication.update -= UpdateSmokeTest;
            EditorApplication.update += UpdateSmokeTest;
        }

        private static void UpdateSmokeTest()
        {
            if (!SessionState.GetBool(SmokeActiveKey, false))
            {
                EditorApplication.update -= UpdateSmokeTest;
                return;
            }

            string phase = SessionState.GetString(SmokePhaseKey, string.Empty);
            if (phase != "ExitSuccess" && phase != "ExitFailure" &&
                (!long.TryParse(SessionState.GetString(SmokeDeadlineKey, "0"), out long deadlineTicks) ||
                 DateTime.UtcNow.Ticks > deadlineTicks))
            {
                FinishSmokeTest(false, "Timed out before reaching Scene_Level1.");
                return;
            }

            if (phase == "EnterPlayMode" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(SmokePhaseKey, "StartFromMenu");
                EditorApplication.isPlaying = true;
                return;
            }

            if (phase == "ExitSuccess" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(SmokeActiveKey, false);
                EditorApplication.update -= UpdateSmokeTest;
                Debug.Log("LEVEL1_CUTSCENE_RUNTIME_SUCCESS");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
                return;
            }

            if (phase == "ExitFailure" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(SmokeActiveKey, false);
                EditorApplication.update -= UpdateSmokeTest;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            try
            {
                string activeScene = SceneManager.GetActiveScene().name;
                if (phase == "StartFromMenu" && activeScene == "Scene_MainMenu")
                {
                    SceneTransitionButton transition = UnityEngine.Object.FindAnyObjectByType<SceneTransitionButton>();
                    if (transition == null)
                    {
                        throw new InvalidOperationException("The Main Menu Start transition component was not found at runtime.");
                    }

                    transition.LoadTargetScene();
                    SessionState.SetString(SmokePhaseKey, "AdvanceCutscene");
                    nextSmokeActionTime = EditorApplication.timeSinceStartup + 0.75d;
                    return;
                }

                if (phase == "AdvanceCutscene" && activeScene == CutsceneSceneName &&
                    EditorApplication.timeSinceStartup >= nextSmokeActionTime)
                {
                    DialogInput dialogInput = UnityEngine.Object.FindAnyObjectByType<DialogInput>();
                    if (dialogInput != null)
                    {
                        dialogInput.SetNextLineFlag();
                    }

                    nextSmokeActionTime = EditorApplication.timeSinceStartup + 0.2d;
                    return;
                }

                if (phase == "AdvanceCutscene" && activeScene == LevelSceneName)
                {
                    SessionState.SetString(SmokePhaseKey, "ExitSuccess");
                    EditorApplication.isPlaying = false;
                }
            }
            catch (Exception exception)
            {
                FinishSmokeTest(false, exception.ToString());
            }
        }

        private static void FinishSmokeTest(bool success, string message)
        {
            if (!success)
            {
                Debug.LogError($"LEVEL1_CUTSCENE_RUNTIME_FAILED: {message}");
            }

            SessionState.SetString(SmokePhaseKey, success ? "ExitSuccess" : "ExitFailure");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }
            else if (Application.isBatchMode)
            {
                SessionState.SetBool(SmokeActiveKey, false);
                EditorApplication.Exit(success ? 0 : 1);
            }
        }

        private static void ConfigureMainMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            Button startButton = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .FirstOrDefault(button => button.gameObject.name == "Start");

            if (startButton == null)
            {
                throw new InvalidOperationException("The Main Menu scene does not contain a Button named Start.");
            }

            Canvas canvas = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .FirstOrDefault();
            if (canvas == null)
            {
                throw new InvalidOperationException("The Main Menu scene does not contain a Canvas.");
            }

            Transform oldOverlay = canvas.transform.Find("Scene Transition Overlay");
            if (oldOverlay != null)
            {
                UnityEngine.Object.DestroyImmediate(oldOverlay.gameObject);
            }

            GameObject overlayObject = CreateUiObject("Scene Transition Overlay", canvas.transform);
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.color = Color.black;
            overlayImage.raycastTarget = true;
            CanvasGroup overlayGroup = overlayObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayObject.transform.SetAsLastSibling();

            SceneTransitionButton transition = startButton.GetComponent<SceneTransitionButton>();
            if (transition == null)
            {
                transition = startButton.gameObject.AddComponent<SceneTransitionButton>();
            }

            SerializedObject transitionObject = new SerializedObject(transition);
            transitionObject.FindProperty("targetScene").stringValue = CutsceneSceneName;
            transitionObject.FindProperty("fadeOverlay").objectReferenceValue = overlayGroup;
            transitionObject.FindProperty("fadeDuration").floatValue = 0.35f;
            transitionObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureLevelOneCutscene()
        {
            Scene scene = EditorSceneManager.OpenScene(CutsceneScene, OpenSceneMode.Single);
            GameObject existingRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Level 1 Cutscene");
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            foreach (GameObject root in scene.GetRootGameObjects().Where(root => root.name == "EventSystem").ToArray())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            GameObject rootObject = new GameObject("Level 1 Cutscene");
            SceneManager.MoveGameObjectToScene(rootObject, scene);

            GameObject canvasObject = CreateUiObject("Cutscene Canvas", rootObject.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup dialogCanvasGroup = canvasObject.AddComponent<CanvasGroup>();

            GameObject colorBackdrop = CreateImage("Archival Navy Backdrop", canvasObject.transform, null, new Color(0.025f, 0.04f, 0.07f, 1f));
            Stretch(colorBackdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject imageBackdrop = CreateImage("Backdrop Image Assign Future Art Here", canvasObject.transform, null, new Color(1f, 1f, 1f, 0f));
            Stretch(imageBackdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            imageBackdrop.GetComponent<Image>().preserveAspect = true;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            if (font == null)
            {
                throw new InvalidOperationException($"Unable to load the cutscene font at {FontAsset}.");
            }

            TMP_Text chapterLabel = CreateText(
                "Chapter Label",
                canvasObject.transform,
                font,
                "CHAPTER I",
                30f,
                new Color(0.78f, 0.64f, 0.36f),
                FontStyles.Bold);
            Stretch(chapterLabel.rectTransform, new Vector2(0.12f, 0.77f), new Vector2(0.88f, 0.85f), Vector2.zero, Vector2.zero);

            TMP_Text titleLabel = CreateText(
                "Cutscene Title",
                canvasObject.transform,
                font,
                "MALAYA  ·  DECEMBER 1941\nTHE OCCUPATION BEGINS",
                48f,
                new Color(0.93f, 0.89f, 0.78f),
                FontStyles.Bold);
            Stretch(titleLabel.rectTransform, new Vector2(0.12f, 0.58f), new Vector2(0.88f, 0.77f), Vector2.zero, Vector2.zero);

            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelAsset);
            Sprite promptSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptAsset);
            if (panelSprite == null || promptSprite == null)
            {
                throw new InvalidOperationException("The Kenney cutscene UI sprites did not import as Sprite assets.");
            }

            GameObject panelObject = CreateImage("Dialogue Box", canvasObject.transform, panelSprite, new Color(0.84f, 0.76f, 0.58f, 0.98f));
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.type = Image.Type.Sliced;
            Stretch(panelObject.GetComponent<RectTransform>(), new Vector2(0.08f, 0.045f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero);

            TMP_Text nameText = CreateText(
                "Speaker",
                panelObject.transform,
                font,
                string.Empty,
                30f,
                new Color(0.22f, 0.13f, 0.07f),
                FontStyles.Bold);
            Stretch(nameText.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            TMP_Text storyText = CreateText(
                "Dialogue Text",
                panelObject.transform,
                font,
                string.Empty,
                31f,
                new Color(0.12f, 0.085f, 0.055f),
                FontStyles.Normal);
            Stretch(storyText.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
            storyText.textWrappingMode = TextWrappingModes.Normal;

            GameObject promptObject = CreateImage("Next Prompt", panelObject.transform, promptSprite, new Color(0.30f, 0.18f, 0.10f, 0.96f));
            Image promptImage = promptObject.GetComponent<Image>();
            promptImage.type = Image.Type.Sliced;
            Stretch(promptObject.GetComponent<RectTransform>(), new Vector2(0.36f, 0.035f), new Vector2(0.64f, 0.21f), Vector2.zero, Vector2.zero);
            CanvasGroup promptGroup = promptObject.AddComponent<CanvasGroup>();

            TMP_Text promptLabel = CreateText(
                "Prompt Text",
                promptObject.transform,
                font,
                "PRESS SPACE TO CONTINUE",
                22f,
                new Color(0.96f, 0.91f, 0.78f),
                FontStyles.Bold);
            Stretch(promptLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-14f, -4f));

            Writer writer = canvasObject.AddComponent<Writer>();
            DialogInput dialogInput = canvasObject.AddComponent<DialogInput>();
            SayDialog sayDialog = canvasObject.AddComponent<SayDialog>();
            CutscenePromptController promptController = canvasObject.AddComponent<CutscenePromptController>();

            SerializedObject writerObject = new SerializedObject(writer);
            writerObject.FindProperty("targetTextObject").objectReferenceValue = storyText.gameObject;
            writerObject.FindProperty("writingSpeed").floatValue = 42f;
            writerObject.FindProperty("punctuationPause").floatValue = 0.18f;
            writerObject.FindProperty("instantComplete").boolValue = true;
            writerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject inputObject = new SerializedObject(dialogInput);
            inputObject.FindProperty("clickMode").enumValueIndex = (int)ClickMode.Disabled;
            inputObject.FindProperty("nextClickDelay").floatValue = 0.12f;
            inputObject.FindProperty("cancelEnabled").boolValue = false;
            inputObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject dialogObject = new SerializedObject(sayDialog);
            // This custom dialog lives on the Canvas root. A non-zero Fungus fade can
            // deactivate that root before its Writer and DialogInput receive a frame.
            // The dedicated prompt controller supplies the requested visual fade.
            dialogObject.FindProperty("fadeDuration").floatValue = 0f;
            dialogObject.FindProperty("continueButton").objectReferenceValue = null;
            dialogObject.FindProperty("dialogCanvas").objectReferenceValue = canvas;
            dialogObject.FindProperty("nameText").objectReferenceValue = null;
            dialogObject.FindProperty("nameTextGO").objectReferenceValue = nameText.gameObject;
            dialogObject.FindProperty("storyText").objectReferenceValue = null;
            dialogObject.FindProperty("storyTextGO").objectReferenceValue = storyText.gameObject;
            dialogObject.FindProperty("characterImage").objectReferenceValue = null;
            dialogObject.FindProperty("fitTextWithImage").boolValue = false;
            dialogObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject promptControllerObject = new SerializedObject(promptController);
            promptControllerObject.FindProperty("writer").objectReferenceValue = writer;
            promptControllerObject.FindProperty("dialogInput").objectReferenceValue = dialogInput;
            promptControllerObject.FindProperty("promptLabel").objectReferenceValue = promptLabel;
            promptControllerObject.FindProperty("promptGroup").objectReferenceValue = promptGroup;
            promptControllerObject.FindProperty("continuePrompt").stringValue = "PRESS SPACE TO CONTINUE";
            promptControllerObject.FindProperty("finalPrompt").stringValue = "PRESS SPACE TO START";
            promptControllerObject.FindProperty("pulseSpeed").floatValue = 1.5f;
            promptControllerObject.FindProperty("minimumAlpha").floatValue = 0.35f;
            promptControllerObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Fungus/Resources/Prefabs/EventSystem_NewInputSystem.prefab");
            if (eventSystemPrefab == null)
            {
                throw new InvalidOperationException("The Fungus Input System EventSystem prefab was not found.");
            }

            GameObject eventSystem = (GameObject)PrefabUtility.InstantiatePrefab(eventSystemPrefab, scene);
            eventSystem.name = "EventSystem";
            eventSystem.transform.SetParent(rootObject.transform, false);

            GameObject flowchartObject = new GameObject("Level 1 Prologue Flowchart");
            flowchartObject.transform.SetParent(rootObject.transform, false);
            Flowchart flowchart = flowchartObject.AddComponent<Flowchart>();
            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = "Play Level 1 Prologue";
            FlowchartEnabled enabledHandler = flowchartObject.AddComponent<FlowchartEnabled>();
            enabledHandler.ParentBlock = block;
            block._EventHandler = enabledHandler;

            Character narrator = CreateCharacter(flowchartObject.transform, "Narrator", sayDialog, new Color(0.25f, 0.15f, 0.08f));
            Character officer = CreateCharacter(flowchartObject.transform, "Commanding Officer", sayDialog, new Color(0.36f, 0.12f, 0.08f));

            for (int i = 0; i < LevelOneDialogue.Length; i++)
            {
                if (i == LevelOneDialogue.Length - 1)
                {
                    SetCutscenePromptMode promptCommand = AddCommand<SetCutscenePromptMode>(flowchart, block);
                    SerializedObject promptCommandObject = new SerializedObject(promptCommand);
                    promptCommandObject.FindProperty("promptController").objectReferenceValue = promptController;
                    promptCommandObject.FindProperty("finalPrompt").boolValue = true;
                    promptCommandObject.ApplyModifiedPropertiesWithoutUndo();
                }

                DialogueLine line = LevelOneDialogue[i];
                Say say = AddCommand<Say>(flowchart, block);
                SerializedObject sayObject = new SerializedObject(say);
                sayObject.FindProperty("storyText").stringValue = line.Text;
                sayObject.FindProperty("character").objectReferenceValue = line.Speaker == "Narrator" ? narrator : officer;
                sayObject.FindProperty("showAlways").boolValue = true;
                sayObject.FindProperty("extendPrevious").boolValue = false;
                sayObject.FindProperty("fadeWhenDone").boolValue = false;
                sayObject.FindProperty("waitForClick").boolValue = true;
                sayObject.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
                sayObject.ApplyModifiedPropertiesWithoutUndo();
            }

            LoadSceneDirect loadScene = AddCommand<LoadSceneDirect>(flowchart, block);
            SerializedObject loadSceneObject = new SerializedObject(loadScene);
            loadSceneObject.FindProperty("targetScene").stringValue = LevelSceneName;
            loadSceneObject.ApplyModifiedPropertiesWithoutUndo();

            dialogCanvasGroup.alpha = 1f;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Character CreateCharacter(Transform parent, string displayName, SayDialog sayDialog, Color nameColor)
        {
            GameObject characterObject = new GameObject(displayName);
            characterObject.transform.SetParent(parent, false);
            Character character = characterObject.AddComponent<Character>();
            SerializedObject characterSerializedObject = new SerializedObject(character);
            characterSerializedObject.FindProperty("nameText").stringValue = displayName;
            characterSerializedObject.FindProperty("nameColor").colorValue = nameColor;
            characterSerializedObject.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
            characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            return character;
        }

        private static T AddCommand<T>(Flowchart flowchart, Block block) where T : Command
        {
            T command = flowchart.gameObject.AddComponent<T>();
            command.ParentBlock = block;
            command.ItemId = flowchart.NextItemId();
            command.OnCommandAdded(block);
            block.CommandList.Add(command);
            return command;
        }

        private static void ConfigureBuildSettings()
        {
            foreach (string path in BuildScenePaths)
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new InvalidOperationException($"Required scene is missing: {path}");
                }
            }

            EditorBuildSettings.scenes = BuildScenePaths
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        private static void ValidateSetup()
        {
            Scene mainMenu = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            SceneTransitionButton transition = mainMenu.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTransitionButton>(true))
                .FirstOrDefault(component => component.gameObject.name == "Start");
            if (transition == null)
            {
                throw new InvalidOperationException("Start button transition was not created.");
            }

            Scene cutscene = EditorSceneManager.OpenScene(CutsceneScene, OpenSceneMode.Single);
            Flowchart flowchart = cutscene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Flowchart>(true))
                .FirstOrDefault(component => component.gameObject.name == "Level 1 Prologue Flowchart");
            if (flowchart == null)
            {
                throw new InvalidOperationException("Level 1 Flowchart was not created.");
            }

            Block block = flowchart.FindBlock("Play Level 1 Prologue");
            int expectedCommandCount = LevelOneDialogue.Length + 2;
            if (block == null || block.CommandList.Count != expectedCommandCount)
            {
                throw new InvalidOperationException($"Expected {expectedCommandCount} commands in the Level 1 prologue.");
            }

            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == CutsceneScene) ||
                !EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path.EndsWith($"/{LevelSceneName}.unity", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The cutscene or Level 1 scene is missing from Build Settings.");
            }
        }

        private static void ConfigureSprite(string assetPath, Vector4 border)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to configure texture importer for {assetPath}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return gameObject;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            float fontSize,
            Color color,
            FontStyles fontStyle)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static T FindNamedComponent<T>(GameObject root, string objectName) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).First(component => component.gameObject.name == objectName);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }

            public string Speaker { get; }
            public string Text { get; }
        }
    }
}
