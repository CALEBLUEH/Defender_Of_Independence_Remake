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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelOneInstructionSetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level1.unity";
        private const string PanelAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/panel_beige.png";
        private const string ButtonAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/buttonLong_brown.png";
        private const string TitleFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
        private const string BodyFontPath = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string ControllerName = "Level 1 Instruction Controller";
        private const string PanelName = "Instruction Panel";

        private const string BriefingText =
            "<size=31><b>YOUR OBJECTIVE</b></size>\n" +
            "Japanese forces control the battlefield, and two civilians are trapped at Sites A and B. Reach each hostage, guide one at a time back to the safety tent, and rescue both before the defence collapses. Defeating every enemy is not required.\n\n" +
            "<size=31><b>CONTROLS</b></size>\n" +
            "- <b>W A S D</b>  -  Move forward, left, backward and right\n" +
            "- <b>MOUSE</b>  -  Look and aim\n" +
            "- <b>LEFT SHIFT</b>  -  Walk slowly while held\n" +
            "- <b>SPACE</b>  -  Jump\n" +
            "- <b>LEFT MOUSE</b>  -  Fire once in Manual mode; hold to fire in Auto mode\n" +
            "- <b>RIGHT MOUSE</b>  -  Switch between Manual and Auto fire\n" +
            "- <b>R</b>  -  Reload the 12-round magazine\n" +
            "- <b>C</b>  -  Ask a nearby hostage to follow\n\n" +
            "<size=31><b>MISSION NOTES</b></size>\n" +
            "- Only one hostage can follow you at a time.\n" +
            "- Keep the hostage behind you and lead them into the tent.\n" +
            "- Enemy reinforcements arrive as the battle continues.\n" +
            "- Watch your health and ammunition displays.\n" +
            "- Rescue both hostages to complete Level 1.";

        [MenuItem("Project Tools/Level 1/Build Instruction Panel")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                Debug.Log("LEVEL1_INSTRUCTION_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL1_INSTRUCTION_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Project Tools/Level 1/Repair Instruction Cursor Wiring")]
        public static void RepairCursorWiring()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Level 1 Player") ??
                                throw new InvalidOperationException("Scene_Level1 has no Level 1 Player.");
            LevelOneInstructionController controller = FindRoot(scene, ControllerName)?.GetComponent<LevelOneInstructionController>() ??
                                                       throw new InvalidOperationException("Scene_Level1 has no instruction controller.");
            StarterAssetsInputs starterInputs = player.GetComponent<StarterAssetsInputs>() ??
                                               throw new InvalidOperationException("The Level 1 player has no StarterAssetsInputs component.");

            SetReference(controller, "starterAssetsInputs", starterInputs);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL1_CURSOR_WIRING_OK: instruction panel owns Starter Assets cursor lock until the mission starts.");
        }

        public static void RepairCursorWiringFromCommandLine()
        {
            try
            {
                RepairCursorWiring();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Level 1 Player") ??
                                throw new InvalidOperationException("Scene_Level1 has no Level 1 Player.");
            LevelOneEnemyDirector director = FindRoot(scene, "Level 1 Enemy Director")?.GetComponent<LevelOneEnemyDirector>() ??
                                             throw new InvalidOperationException("Scene_Level1 has no enemy director.");
            Transform hud = player.transform.Find("Level 1 Player HUD") ??
                            throw new InvalidOperationException("The Level 1 player HUD is missing.");

            DestroyChild(hud, PanelName);
            GameObject panel = CreateInstructionPanel(hud, out Button startButton, out ScrollRect scrollRect);
            panel.transform.SetAsLastSibling();

            GameObject controllerObject = FindRoot(scene, ControllerName);
            if (controllerObject == null)
            {
                controllerObject = new GameObject(ControllerName);
                SceneManager.MoveGameObjectToScene(controllerObject, scene);
            }

            LevelOneInstructionController controller = controllerObject.GetComponent<LevelOneInstructionController>() ??
                                                       controllerObject.AddComponent<LevelOneInstructionController>();
            SetReference(controller, "instructionPanel", panel);
            SetReference(controller, "startButton", startButton);
            SetReference(controller, "instructionScroll", scrollRect);
            SetReference(controller, "enemyDirector", director);
            SetReference(controller, "movement", player.GetComponent<FirstPersonController>());
            SetReference(controller, "weapon", player.GetComponent<FirstPersonWeaponController>());
            SetReference(controller, "gameplayInput", player.GetComponent<LevelOnePlayerInput>());
            SetReference(controller, "interactor", player.GetComponent<LevelOnePlayerInteractor>());
            SetReference(controller, "playerInput", player.GetComponent<PlayerInput>());
            SetReference(controller, "starterAssetsInputs", player.GetComponent<StarterAssetsInputs>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(scene);
        }

        private static GameObject CreateInstructionPanel(Transform hud, out Button startButton, out ScrollRect scrollRect)
        {
            TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath) ??
                                      throw new FileNotFoundException($"Missing title font: {TitleFontPath}");
            TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath) ??
                                     throw new FileNotFoundException($"Missing body font: {BodyFontPath}");
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelAsset) ??
                                 throw new FileNotFoundException($"Missing panel sprite: {PanelAsset}");
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonAsset) ??
                                  throw new FileNotFoundException($"Missing button sprite: {ButtonAsset}");

            Image overlay = CreateImage(hud, PanelName, null, new Color(0.012f, 0.018f, 0.03f, 0.82f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;

            Image window = CreateImage(overlay.transform, "Briefing Window", panelSprite, new Color(0.84f, 0.76f, 0.58f, 1f));
            window.type = Image.Type.Sliced;
            SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 760f));

            TMP_Text title = CreateText(window.transform, "Title", titleFont, "MISSION BRIEFING", 43f, new Color(0.2f, 0.11f, 0.055f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(900f, 66f));
            title.fontStyle = FontStyles.Bold;

            TMP_Text subtitle = CreateText(window.transform, "Subtitle", bodyFont, "LEVEL 1  /  RESCUE UNDER OCCUPATION", 21f, new Color(0.35f, 0.2f, 0.1f, 1f));
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(900f, 38f));
            subtitle.fontStyle = FontStyles.Bold;

            GameObject scrollObject = CreateUiObject("Instruction Scroll View", window.transform);
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -4f), new Vector2(940f, 500f));
            scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.12f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 44f;

            Image viewportImage = CreateImage(scrollObject.transform, "Viewport", null, new Color(0.12f, 0.075f, 0.04f, 0.1f));
            RectTransform viewport = viewportImage.rectTransform;
            Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-48f, -8f));
            viewportImage.raycastTarget = true;
            viewportImage.gameObject.AddComponent<RectMask2D>();

            GameObject contentObject = CreateUiObject("Content", viewport);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 825f);

            TMP_Text body = CreateText(content, "Instructions", bodyFont, BriefingText, 25f, new Color(0.16f, 0.095f, 0.05f, 1f));
            Stretch(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 20f), new Vector2(-28f, -18f));
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 8f;
            body.richText = true;

            Image scrollbarImage = CreateImage(scrollObject.transform, "Vertical Scrollbar", null, new Color(0.19f, 0.11f, 0.055f, 0.5f));
            RectTransform scrollbarRect = scrollbarImage.rectTransform;
            Stretch(scrollbarRect, new Vector2(1f, 0f), Vector2.one, new Vector2(-34f, 8f), new Vector2(-8f, -8f));
            Scrollbar scrollbar = scrollbarImage.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            GameObject slidingAreaObject = CreateUiObject("Sliding Area", scrollbarImage.transform);
            RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
            Stretch(slidingArea, Vector2.zero, Vector2.one, new Vector2(4f, 8f), new Vector2(-4f, -8f));

            Image handleImage = CreateImage(slidingArea, "Handle", null, new Color(0.48f, 0.25f, 0.1f, 1f));
            RectTransform handle = handleImage.rectTransform;
            Stretch(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handleImage.raycastTarget = true;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.value = 1f;
            scrollbar.size = 0.58f;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 8f;

            Image buttonImage = CreateImage(window.transform, "Start Mission Button", buttonSprite, new Color(0.31f, 0.18f, 0.09f, 1f));
            buttonImage.type = Image.Type.Sliced;
            SetRect(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(350f, 72f));
            startButton = buttonImage.gameObject.AddComponent<Button>();
            ColorBlock colors = startButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.82f, 0.48f, 1f);
            colors.pressedColor = new Color(0.9f, 0.55f, 0.25f, 1f);
            startButton.colors = colors;

            TMP_Text buttonText = CreateText(buttonImage.transform, "Label", titleFont, "START MISSION", 23f, new Color(0.97f, 0.91f, 0.77f, 1f));
            Stretch(buttonText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 6f), new Vector2(-16f, -6f));
            buttonText.fontStyle = FontStyles.Bold;

            return overlay.gameObject;
        }

        private static void Validate(Scene scene)
        {
            LevelOneInstructionController controller = FindRoot(scene, ControllerName)?.GetComponent<LevelOneInstructionController>();
            Transform panel = FindRoot(scene, "Level 1 Player")?.transform.Find($"Level 1 Player HUD/{PanelName}");
            ScrollRect scroll = panel?.GetComponentInChildren<ScrollRect>(true);
            Scrollbar scrollbar = panel?.GetComponentInChildren<Scrollbar>(true);
            Button button = panel?.GetComponentsInChildren<Button>(true).FirstOrDefault(item => item.gameObject.name == "Start Mission Button");
            if (controller == null || scroll?.content == null || scroll.viewport == null || scrollbar?.handleRect == null || button == null)
            {
                throw new InvalidOperationException("The Level 1 instruction panel is missing required wiring.");
            }

            Debug.Log("LEVEL1_INSTRUCTION_VALID panel=ready scroll=vertical startGate=player+enemy controls=8 objective=hostage-rescue");
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string content, float size, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
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
            if (value == null)
            {
                throw new InvalidOperationException($"Cannot assign null to {target.GetType().Name}.{name}.");
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing property {name} on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
