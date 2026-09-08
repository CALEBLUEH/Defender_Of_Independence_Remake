using System;
using System.Collections.Generic;
using System.Linq;
using DefenderOfIndependence.Cutscenes;
using DefenderOfIndependence.Level2;
using Fungus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelTwoConversationSetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
        private const string PanelName = "Level 2 Conversation Panel";
        private const string ConfirmationPanelName = "Level 2 Confirmation Panel";
        private const string MeterPanelName = "Level 2 Negotiation Meters";
        private const string FeedbackPanelName = "Level 2 Action Feedback";
        private const string ResultPanelName = "Level 2 Final Result";
        private const string TitleFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
        private const string BodyFontPath = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string PanelAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/panel_beige.png";
        private const string ButtonAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/buttonLong_brown.png";

        private readonly struct ChoiceData
        {
            public readonly string Text;
            public readonly string ResponseSpeaker;
            public readonly string Response;
            public readonly int BritishConfidence;
            public readonly int DelegationUnity;
            public readonly int PublicSupport;
            public ChoiceData(string text, string responseSpeaker, string response,
                int britishConfidence = 0, int delegationUnity = 0, int publicSupport = 0)
            {
                Text = text; ResponseSpeaker = responseSpeaker; Response = response;
                BritishConfidence = britishConfidence;
                DelegationUnity = delegationUnity;
                PublicSupport = publicSupport;
            }
        }

        private readonly struct LineData
        {
            public readonly string Speaker;
            public readonly string Text;
            public LineData(string speaker, string text) { Speaker = speaker; Text = text; }
        }

        private readonly struct StepData
        {
            public readonly LineData[] Lines;
            public readonly ChoiceData[] Choices;
            public StepData(string speaker, string prompt, params ChoiceData[] choices)
            {
                Lines = new[] { new LineData(speaker, prompt) }; Choices = choices;
            }
            public StepData(LineData[] lines, params ChoiceData[] choices)
            {
                Lines = lines; Choices = choices;
            }
        }

        [MenuItem("Project Tools/Level 2/Build Dialogue and Door Energy System")]
        public static void RunFromMenu() => Run();

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                Debug.Log("LEVEL2_CONVERSATION_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL2_CONVERSATION_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LevelTwoDayController dayController = FindUnique<LevelTwoDayController>(scene);
            LevelTwoDoorInteractor interactor = FindUnique<LevelTwoDoorInteractor>(scene);
            LevelTwoFirstPersonController player = FindUnique<LevelTwoFirstPersonController>(scene);
            Transform dayHud = FindUniqueTransform(scene, "Day HUD");
            Camera playerCamera = player.GetComponentInChildren<Camera>(true) ??
                                  throw new InvalidOperationException("The Level 2 player camera is missing.");

            DestroyChild(dayHud, PanelName);
            DestroyChild(dayHud, ConfirmationPanelName);
            DestroyChild(dayHud, MeterPanelName);
            DestroyChild(dayHud, FeedbackPanelName);
            DestroyChild(dayHud, ResultPanelName);
            CreateConversationPanel(dayHud, out GameObject panel, out SayDialog sayDialog, out Writer writer,
                out DialogInput dialogInput, out GameObject choiceRoot, out Button[] choiceButtons,
                out TMP_Text[] choiceLabels);
            CreateConfirmationPanel(dayHud, out GameObject confirmationPanelObject, out RectTransform confirmationWindow,
                out CanvasGroup confirmationWindowGroup, out TMP_Text confirmationTitle, out TMP_Text confirmationMessage,
                out GameObject warningRoot, out TMP_Text warningText, out Button confirmButton, out Button cancelButton);
            CreateNegotiationMeterPanel(dayHud, out Slider britishSlider, out Slider delegationSlider,
                out Slider publicSlider, out TMP_Text britishValue, out TMP_Text delegationValue, out TMP_Text publicValue);
            GameObject meterPanelObject = britishSlider.transform.parent.gameObject;
            CreateFeedbackPanel(dayHud, out CanvasGroup feedbackGroup, out TMP_Text feedbackTitle,
                out TMP_Text feedbackBody);
            CreateFinalResultPanel(dayHud, out CanvasGroup finalOverlay, out CanvasGroup finalContent,
                out TMP_Text finalHeading, out TMP_Text finalBody, out Button continueButton, out Button restartButton);

            LevelTwoNegotiationMeterController meterController =
                dayController.GetComponent<LevelTwoNegotiationMeterController>() ??
                dayController.gameObject.AddComponent<LevelTwoNegotiationMeterController>();
            SetReference(meterController, "britishConfidenceSlider", britishSlider);
            SetReference(meterController, "delegationUnitySlider", delegationSlider);
            SetReference(meterController, "publicSupportSlider", publicSlider);
            SetReference(meterController, "britishConfidenceValue", britishValue);
            SetReference(meterController, "delegationUnityValue", delegationValue);
            SetReference(meterController, "publicSupportValue", publicValue);
            SetReference(meterController, "feedbackPanel", feedbackGroup);
            SetReference(meterController, "feedbackTitle", feedbackTitle);
            SetReference(meterController, "feedbackBody", feedbackBody);
            SetReference(meterController, "finalOverlay", finalOverlay);
            SetReference(meterController, "finalContent", finalContent);
            SetReference(meterController, "finalHeading", finalHeading);
            SetReference(meterController, "finalBody", finalBody);
            SetReference(meterController, "continueButton", continueButton);
            SetReference(meterController, "restartButton", restartButton);
            SetReference(meterController, "playerController", player);
            SetReference(dayController, "negotiationMeters", meterController);

            Transform energyDisplay = dayHud.Find("Energy Display") ??
                                      throw new InvalidOperationException("The Level 2 Energy Display is missing.");
            Transform dayCard = dayHud.Find("Day Transition Card") ??
                                throw new InvalidOperationException("The Level 2 Day Transition Card is missing.");
            MoveBefore(energyDisplay, dayCard);
            MoveBefore(meterPanelObject.transform, dayCard);
            SetReferenceArray(dayController, "transitionHiddenHud",
                new[] { energyDisplay.gameObject, meterPanelObject });

            LevelTwoConversationCameraFocus cameraFocus = dayController.GetComponent<LevelTwoConversationCameraFocus>() ??
                                                          dayController.gameObject.AddComponent<LevelTwoConversationCameraFocus>();
            SetReference(cameraFocus, "playerCamera", playerCamera);

            LevelTwoConversationViewer viewer = dayController.GetComponent<LevelTwoConversationViewer>() ??
                                                dayController.gameObject.AddComponent<LevelTwoConversationViewer>();
            SetReference(viewer, "panel", panel);
            SetReference(viewer, "sayDialog", sayDialog);
            SetReference(viewer, "writer", writer);
            SetReference(viewer, "dialogInput", dialogInput);
            SetReference(viewer, "choiceRoot", choiceRoot);
            SetReferenceArray(viewer, "choiceButtons", choiceButtons);
            SetReferenceArray(viewer, "choiceLabels", choiceLabels);
            SetReference(viewer, "playerController", player);
            SetReference(viewer, "cameraFocus", cameraFocus);
            SetReference(viewer, "meterController", meterController);

            LevelTwoConfirmationPanel confirmationPanel = dayController.GetComponent<LevelTwoConfirmationPanel>() ??
                                                          dayController.gameObject.AddComponent<LevelTwoConfirmationPanel>();
            SetReference(confirmationPanel, "panel", confirmationPanelObject);
            SetReference(confirmationPanel, "window", confirmationWindow);
            SetReference(confirmationPanel, "windowCanvasGroup", confirmationWindowGroup);
            SetReference(confirmationPanel, "titleText", confirmationTitle);
            SetReference(confirmationPanel, "messageText", confirmationMessage);
            SetReference(confirmationPanel, "warningRoot", warningRoot);
            SetReference(confirmationPanel, "warningText", warningText);
            SetReference(confirmationPanel, "confirmButton", confirmButton);
            SetReference(confirmationPanel, "cancelButton", cancelButton);
            SetReference(confirmationPanel, "playerController", player);

            LevelTwoConversationTrigger[] triggers =
            {
                ConfigureCharacter(scene, "Tunku Abdul Rahman - Day 1", 1, "TUNKU ABDUL RAHMAN", DayOne()),
                ConfigureCharacter(scene, "Alan Lennox-Boyd - Day 2", 2, "ALAN LENNOX-BOYD", DayTwo()),
                ConfigureCharacter(scene, "Alan Lennox-Boyd - Day 3", 3, "ALAN LENNOX-BOYD", DayThree()),
                ConfigureExistingCollider(scene, "RadioStationCollider", 4, "THE RADIO BROADCAST", DayFour()),
                ConfigureCharacter(scene, "Tunku Abdul Rahman - Day 5", 5, "TUNKU ABDUL RAHMAN", DayFive()),
                ConfigureCharacter(scene, "Alan Lennox-Boyd - Day 6", 6, "ALAN LENNOX-BOYD", DaySix())
            };

            SetReferenceArray(dayController, "conversationTriggers", triggers);
            SetReference(interactor, "conversationViewer", viewer);
            SetReferenceArray(interactor, "conversations", triggers);
            SetReference(interactor, "confirmationPanel", confirmationPanel);

            ConfigureFinalDayDoor(scene, "TunkuAbdulRahmanDoor", false);
            ConfigureFinalDayDoor(scene, "RadioStationDoor", false);
            ConfigureFinalDayDoor(scene, "AlanLennox-BoydDoor", true);

            WireExistingDocuments(scene, meterController);

            panel.SetActive(false);
            confirmationPanelObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(scene, triggers);
        }

        private static LevelTwoConversationTrigger ConfigureCharacter(Scene scene, string objectName, int day,
            string displayName, StepData[] steps)
        {
            Transform target = FindUniqueTransform(scene, objectName);
            Transform colliderTransform = target.Find("Conversation Collider");
            if (colliderTransform == null)
            {
                colliderTransform = new GameObject("Conversation Collider").transform;
                colliderTransform.SetParent(target, false);
            }
            BoxCollider collider = colliderTransform.GetComponent<BoxCollider>();
            if (collider == null) collider = colliderTransform.gameObject.AddComponent<BoxCollider>();
            FitColliderToRenderers(target, collider);
            Transform focusTarget = CreateOrKeepCameraFocusTarget(target);
            return ConfigureTrigger(target, collider, day, displayName, steps, focusTarget);
        }

        private static LevelTwoConversationTrigger ConfigureExistingCollider(Scene scene, string objectName, int day,
            string displayName, StepData[] steps)
        {
            Transform target = FindUniqueTransform(scene, objectName);
            Collider collider = target.GetComponent<Collider>() ??
                                throw new InvalidOperationException(objectName + " needs its authored Collider.");
            return ConfigureTrigger(target, collider, day, displayName, steps, null);
        }

        private static LevelTwoConversationTrigger ConfigureTrigger(Transform target, Collider collider, int day,
            string displayName, StepData[] steps, Transform focusTarget)
        {
            LevelTwoConversationTrigger trigger = target.GetComponent<LevelTwoConversationTrigger>() ??
                                                  target.gameObject.AddComponent<LevelTwoConversationTrigger>();
            SerializedObject data = new SerializedObject(trigger);
            data.FindProperty("activeDay").intValue = day;
            data.FindProperty("conversationDisplayName").stringValue = displayName;
            data.FindProperty("interactionCollider").objectReferenceValue = collider;
            data.FindProperty("energyCost").intValue = 1;
            data.FindProperty("cameraFocusTarget").objectReferenceValue = focusTarget;
            data.FindProperty("calculateFinalResultOnComplete").boolValue = day == 6;
            SerializedProperty stepArray = data.FindProperty("steps");
            stepArray.arraySize = steps.Length;
            for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                StepData sourceStep = steps[stepIndex];
                SerializedProperty step = stepArray.GetArrayElementAtIndex(stepIndex);
                LineData fallback = sourceStep.Lines[0];
                step.FindPropertyRelative("speaker").stringValue = fallback.Speaker;
                step.FindPropertyRelative("prompt").stringValue = fallback.Text;
                SerializedProperty lines = step.FindPropertyRelative("lines");
                lines.arraySize = sourceStep.Lines.Length;
                for (int lineIndex = 0; lineIndex < sourceStep.Lines.Length; lineIndex++)
                {
                    SerializedProperty line = lines.GetArrayElementAtIndex(lineIndex);
                    line.FindPropertyRelative("speaker").stringValue = sourceStep.Lines[lineIndex].Speaker;
                    line.FindPropertyRelative("text").stringValue = sourceStep.Lines[lineIndex].Text;
                }
                SerializedProperty choices = step.FindPropertyRelative("choices");
                choices.arraySize = sourceStep.Choices.Length;
                for (int choiceIndex = 0; choiceIndex < sourceStep.Choices.Length; choiceIndex++)
                {
                    ChoiceData sourceChoice = sourceStep.Choices[choiceIndex];
                    SerializedProperty choice = choices.GetArrayElementAtIndex(choiceIndex);
                    choice.FindPropertyRelative("text").stringValue = sourceChoice.Text;
                    choice.FindPropertyRelative("responseSpeaker").stringValue = sourceChoice.ResponseSpeaker;
                    choice.FindPropertyRelative("response").stringValue = sourceChoice.Response;
                    SerializedProperty meterChange = choice.FindPropertyRelative("meterChange");
                    meterChange.FindPropertyRelative("britishConfidence").intValue = sourceChoice.BritishConfidence;
                    meterChange.FindPropertyRelative("delegationUnity").intValue = sourceChoice.DelegationUnity;
                    meterChange.FindPropertyRelative("publicSupport").intValue = sourceChoice.PublicSupport;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        private static Transform CreateOrKeepCameraFocusTarget(Transform character)
        {
            Transform existing = character.Find("Conversation Camera Focus");
            if (existing != null) return existing;

            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(character.name + " has no renderer for camera focus.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Transform focus = new GameObject("Conversation Camera Focus").transform;
            focus.SetParent(character, true);
            Vector3 lookPoint = bounds.center + Vector3.up * bounds.extents.y * 0.12f;
            Vector3 front = character.forward;
            float distance = Mathf.Max(1.5f, bounds.extents.z + 1.15f);
            focus.position = lookPoint + front * distance;
            focus.rotation = Quaternion.LookRotation(lookPoint - focus.position, Vector3.up);
            return focus;
        }

        private static void FitColliderToRenderers(Transform root, BoxCollider collider)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no renderers for a collider.");
            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);

            Vector3 localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 worldCorner = world.center + Vector3.Scale(world.extents, new Vector3(x, y, z));
                Vector3 localCorner = collider.transform.InverseTransformPoint(worldCorner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
            }

            collider.center = (localMin + localMax) * 0.5f;
            Vector3 size = localMax - localMin;
            collider.size = new Vector3(Mathf.Max(0.65f, size.x + 0.18f), Mathf.Max(1.65f, size.y), Mathf.Max(0.55f, size.z + 0.18f));
            collider.isTrigger = false;
        }

        private static void ConfigureFinalDayDoor(Scene scene, string name, bool available)
        {
            LevelTwoDoorTransition door = FindUniqueTransform(scene, name).GetComponent<LevelTwoDoorTransition>() ??
                                          throw new InvalidOperationException(name + " has no LevelTwoDoorTransition.");
            SerializedObject data = new SerializedObject(door);
            data.FindProperty("availableOnFinalDay").boolValue = available;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateConversationPanel(Transform parent, out GameObject panel, out SayDialog sayDialog,
            out Writer writer, out DialogInput dialogInput, out GameObject choiceRoot,
            out Button[] choiceButtons, out TMP_Text[] choiceLabels)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Sprite buttonSprite = Load<Sprite>(ButtonAsset);

            panel = CreateUiObject(PanelName, parent);
            Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            CanvasGroup dialogGroup = panel.AddComponent<CanvasGroup>();

            Image window = CreateImage(panel.transform, "Fungus Dialogue Window", panelSprite, new Color(0.86f, 0.79f, 0.63f, 1f));
            window.type = Image.Type.Sliced;
            SetRect(window.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1450f, 470f));

            TMP_Text speaker = CreateText(window.transform, "Speaker", titleFont, "SPEAKER", 31f,
                new Color(0.19f, 0.105f, 0.045f), TextAlignmentOptions.Center);
            SetRect(speaker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1280f, 58f));

            Image dialogueBackground = CreateImage(window.transform, "Dialogue Background", null, new Color(0.25f, 0.14f, 0.06f, 0.12f));
            SetRect(dialogueBackground.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1280f, 180f));
            TMP_Text dialogue = CreateText(dialogueBackground.transform, "Dialogue", bodyFont, "Dialogue text", 25f,
                new Color(0.14f, 0.075f, 0.03f), TextAlignmentOptions.Center);
            Stretch(dialogue.rectTransform, new Vector2(28f, 18f), new Vector2(-28f, -18f));
            dialogue.textWrappingMode = TextWrappingModes.Normal;

            GameObject promptObject = CreateUiObject("Next Prompt", window.transform);
            SetRect(promptObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(520f, 52f));
            CanvasGroup promptGroup = promptObject.AddComponent<CanvasGroup>();
            TMP_Text promptLabel = CreateText(promptObject.transform, "Prompt Text", titleFont, "PRESS SPACE TO CONTINUE", 19f,
                new Color(0.31f, 0.18f, 0.08f), TextAlignmentOptions.Center);
            Stretch(promptLabel.rectTransform, Vector2.zero, Vector2.zero);

            choiceRoot = CreateUiObject("Horizontal Choices", window.transform);
            SetRect(choiceRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(1330f, 142f));
            choiceButtons = new Button[3];
            choiceLabels = new TMP_Text[3];
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                Image choiceImage = CreateImage(choiceRoot.transform, $"Choice {index + 1}", buttonSprite, new Color(0.33f, 0.18f, 0.075f, 1f));
                choiceImage.type = Image.Type.Sliced;
                SetRect(choiceImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * 440f, 0f), new Vector2(410f, 128f));
                choiceButtons[index] = choiceImage.gameObject.AddComponent<Button>();
                choiceLabels[index] = CreateText(choiceImage.transform, "Label", bodyFont, "Choice", 17f,
                    new Color(0.98f, 0.94f, 0.84f), TextAlignmentOptions.Center);
                Stretch(choiceLabels[index].rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
                choiceLabels[index].textWrappingMode = TextWrappingModes.Normal;
            }

            writer = panel.AddComponent<Writer>();
            dialogInput = panel.AddComponent<DialogInput>();
            sayDialog = panel.AddComponent<SayDialog>();
            CutscenePromptController promptController = panel.AddComponent<CutscenePromptController>();

            SerializedObject writerData = new SerializedObject(writer);
            writerData.FindProperty("targetTextObject").objectReferenceValue = dialogue.gameObject;
            writerData.FindProperty("writingSpeed").floatValue = 42f;
            writerData.FindProperty("punctuationPause").floatValue = 0.18f;
            writerData.FindProperty("instantComplete").boolValue = true;
            writerData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject inputData = new SerializedObject(dialogInput);
            inputData.FindProperty("clickMode").enumValueIndex = (int)ClickMode.Disabled;
            inputData.FindProperty("nextClickDelay").floatValue = 0.12f;
            inputData.FindProperty("cancelEnabled").boolValue = false;
            inputData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject sayData = new SerializedObject(sayDialog);
            sayData.FindProperty("fadeDuration").floatValue = 0f;
            sayData.FindProperty("continueButton").objectReferenceValue = null;
            sayData.FindProperty("dialogCanvas").objectReferenceValue = parent.GetComponentInParent<Canvas>();
            sayData.FindProperty("nameText").objectReferenceValue = null;
            sayData.FindProperty("nameTextGO").objectReferenceValue = speaker.gameObject;
            sayData.FindProperty("storyText").objectReferenceValue = null;
            sayData.FindProperty("storyTextGO").objectReferenceValue = dialogue.gameObject;
            sayData.FindProperty("characterImage").objectReferenceValue = null;
            sayData.FindProperty("fitTextWithImage").boolValue = false;
            sayData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject promptData = new SerializedObject(promptController);
            promptData.FindProperty("writer").objectReferenceValue = writer;
            promptData.FindProperty("dialogInput").objectReferenceValue = dialogInput;
            promptData.FindProperty("promptLabel").objectReferenceValue = promptLabel;
            promptData.FindProperty("promptGroup").objectReferenceValue = promptGroup;
            promptData.FindProperty("continuePrompt").stringValue = "PRESS SPACE TO CONTINUE";
            promptData.FindProperty("finalPrompt").stringValue = "PRESS SPACE TO CONTINUE";
            promptData.FindProperty("pulseSpeed").floatValue = 1.5f;
            promptData.FindProperty("minimumAlpha").floatValue = 0.35f;
            promptData.ApplyModifiedPropertiesWithoutUndo();

            dialogGroup.alpha = 1f;
            choiceRoot.SetActive(false);
        }

        private static void CreateConfirmationPanel(Transform parent, out GameObject panel, out RectTransform window,
            out CanvasGroup windowGroup, out TMP_Text title, out TMP_Text message, out GameObject warningRoot,
            out TMP_Text warning, out Button confirm, out Button cancel)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Sprite buttonSprite = Load<Sprite>(ButtonAsset);

            Image blocker = CreateImage(parent, ConfirmationPanelName, null, new Color(0f, 0f, 0f, 0.18f));
            Stretch(blocker.rectTransform, Vector2.zero, Vector2.zero);
            blocker.raycastTarget = true;
            panel = blocker.gameObject;

            Image windowImage = CreateImage(blocker.transform, "Confirmation Window", panelSprite, new Color(0.86f, 0.79f, 0.63f, 1f));
            windowImage.type = Image.Type.Sliced;
            window = windowImage.rectTransform;
            SetRect(window, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(920f, 410f));
            windowGroup = windowImage.gameObject.AddComponent<CanvasGroup>();

            title = CreateText(window, "Title", titleFont, "CONFIRM", 32f, new Color(0.19f, 0.105f, 0.045f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 58f));
            message = CreateText(window, "Message", bodyFont, "Confirmation message", 24f, new Color(0.14f, 0.075f, 0.03f), TextAlignmentOptions.Center);
            SetRect(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 112f));
            message.textWrappingMode = TextWrappingModes.Normal;

            warningRoot = CreateUiObject("Warning", window);
            SetRect(warningRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(760f, 70f));
            warning = CreateText(warningRoot.transform, "Warning Text", bodyFont, string.Empty, 20f, new Color(0.62f, 0.12f, 0.08f), TextAlignmentOptions.Center);
            Stretch(warning.rectTransform, Vector2.zero, Vector2.zero);
            warning.textWrappingMode = TextWrappingModes.Normal;

            confirm = CreateConfirmationButton(window, "Confirm", "CONFIRM", buttonSprite, titleFont, new Vector2(-220f, 62f));
            cancel = CreateConfirmationButton(window, "Cancel", "CANCEL", buttonSprite, titleFont, new Vector2(220f, 62f));
        }

        private static Button CreateConfirmationButton(Transform parent, string name, string label, Sprite sprite,
            TMP_FontAsset font, Vector2 position)
        {
            Image image = CreateImage(parent, name, sprite, new Color(0.33f, 0.18f, 0.075f, 1f));
            image.type = Image.Type.Sliced;
            SetRect(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(330f, 78f));
            Button button = image.gameObject.AddComponent<Button>();
            TMP_Text text = CreateText(image.transform, "Label", font, label, 21f, Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            return button;
        }

        private static void CreateNegotiationMeterPanel(Transform parent, out Slider britishSlider,
            out Slider delegationSlider, out Slider publicSlider, out TMP_Text britishValue,
            out TMP_Text delegationValue, out TMP_Text publicValue)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Image panel = CreateImage(parent, MeterPanelName, null, new Color(0.025f, 0.035f, 0.055f, 0.92f));
            SetRect(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -232f), new Vector2(420f, 270f));

            TMP_Text heading = CreateText(panel.transform, "Heading", titleFont, "NEGOTIATION STATUS", 21f,
                new Color(0.95f, 0.82f, 0.43f), TextAlignmentOptions.Center);
            SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(380f, 38f));

            CreateMeterRow(panel.transform, "British Confidence", bodyFont, -80f, new Color(0.48f, 0.68f, 0.92f), out britishSlider, out britishValue);
            CreateMeterRow(panel.transform, "Delegation Unity", bodyFont, -148f, new Color(0.92f, 0.73f, 0.28f), out delegationSlider, out delegationValue);
            CreateMeterRow(panel.transform, "Public Support", bodyFont, -216f, new Color(0.45f, 0.82f, 0.52f), out publicSlider, out publicValue);
        }

        private static void CreateMeterRow(Transform parent, string label, TMP_FontAsset font, float y, Color fillColor,
            out Slider slider, out TMP_Text valueText)
        {
            TMP_Text title = CreateText(parent, label + " Label", font, label.ToUpperInvariant(), 17f,
                new Color(0.92f, 0.92f, 0.9f), TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(138f, y), new Vector2(270f, 30f));

            Image background = CreateImage(parent, label + " Slider", null, new Color(0.12f, 0.15f, 0.2f, 1f));
            SetRect(background.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(183f, y - 27f), new Vector2(330f, 18f));
            Image fill = CreateImage(background.transform, "Fill", null, fillColor);
            Stretch(fill.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            slider = background.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = 30f;
            slider.interactable = false;

            valueText = CreateText(parent, label + " Value", font, "30", 18f, Color.white, TextAlignmentOptions.Center);
            SetRect(valueText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(376f, y - 27f), new Vector2(54f, 30f));
        }

        private static void CreateFeedbackPanel(Transform parent, out CanvasGroup group, out TMP_Text title,
            out TMP_Text body)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Image panel = CreateImage(parent, FeedbackPanelName, panelSprite, new Color(0.86f, 0.79f, 0.63f, 0.98f));
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 115f), new Vector2(540f, 190f));
            panel.raycastTarget = false;
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            title = CreateText(panel.transform, "Title", titleFont, "ACTION RESULT", 22f,
                new Color(0.19f, 0.105f, 0.045f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(480f, 40f));
            body = CreateText(panel.transform, "Body", bodyFont, string.Empty, 20f,
                new Color(0.14f, 0.075f, 0.03f), TextAlignmentOptions.Center);
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(480f, 105f));
        }

        private static void CreateFinalResultPanel(Transform parent, out CanvasGroup overlayGroup,
            out CanvasGroup contentGroup, out TMP_Text heading, out TMP_Text body, out Button continueButton,
            out Button restartButton)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Sprite buttonSprite = Load<Sprite>(ButtonAsset);

            Image overlay = CreateImage(parent, ResultPanelName, null, Color.black);
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;

            Image window = CreateImage(overlay.transform, "Result Window", panelSprite, new Color(0.86f, 0.79f, 0.63f, 1f));
            window.type = Image.Type.Sliced;
            SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 580f));
            contentGroup = window.gameObject.AddComponent<CanvasGroup>();
            contentGroup.alpha = 0f;
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;

            heading = CreateText(window.transform, "Heading", titleFont, "NEGOTIATION RESULT", 39f,
                new Color(0.19f, 0.105f, 0.045f), TextAlignmentOptions.Center);
            SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(850f, 72f));
            body = CreateText(window.transform, "Result Details", bodyFont, string.Empty, 27f,
                new Color(0.14f, 0.075f, 0.03f), TextAlignmentOptions.Center);
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(820f, 285f));
            body.textWrappingMode = TextWrappingModes.Normal;

            continueButton = CreateConfirmationButton(window.transform, "Continue to Level 3", "CONTINUE", buttonSprite,
                titleFont, new Vector2(0f, 70f));
            restartButton = CreateConfirmationButton(window.transform, "Restart Level 2", "RESTART LEVEL", buttonSprite,
                titleFont, new Vector2(0f, 70f));
        }

        private static void WireExistingDocuments(Scene scene, LevelTwoNegotiationMeterController meterController)
        {
            foreach (LevelTwoDocumentLocation document in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<LevelTwoDocumentLocation>(true)))
            {
                SerializedObject data = new SerializedObject(document);
                data.FindProperty("meterController").objectReferenceValue = meterController;
                LevelTwoNegotiationMeterController.MeterType meter = document.Room switch
                {
                    LevelTwoDocumentLocation.RoomType.BritishOffice => LevelTwoNegotiationMeterController.MeterType.BritishConfidence,
                    LevelTwoDocumentLocation.RoomType.PlanningRoom => LevelTwoNegotiationMeterController.MeterType.DelegationUnity,
                    LevelTwoDocumentLocation.RoomType.RadioStation => LevelTwoNegotiationMeterController.MeterType.PublicSupport,
                    _ => LevelTwoNegotiationMeterController.MeterType.None
                };
                data.FindProperty("documentBonusMeter").enumValueIndex = (int)meter;
                data.FindProperty("documentBonusAmount").intValue = 5;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static StepData[] DayOne() => new[]
        {
            new StepData("TUNKU ABDUL RAHMAN",
                "Before we face the British government, our own delegation must agree on what we are asking for.",
                new ChoiceData("We should agree on one clear objective: self-government leading to independence, while preparing answers on security and administration.", "TUNKU ABDUL RAHMAN", "Good. Independence is our objective, but a united delegation must also show that we understand the responsibilities that come with governing a country. We cannot arrive in London with only a demand; we must arrive with a plan.", 5, 15),
                new ChoiceData("Demand immediate independence and refuse to discuss any British concerns.", "TUNKU ABDUL RAHMAN", "Determination is important, but refusing every discussion would make the conference pointless. Some may applaud a stronger demand, yet we still need an agreement that can actually move Malaya toward independence.", -15, -5, 10),
                new ChoiceData("Let every delegate present a different position.", "TUNKU ABDUL RAHMAN", "Then we would appear divided before negotiations have even begun. We may disagree during preparation, but once we enter the conference we must understand what we are trying to achieve together.", -5, -15))
        };

        private static StepData[] DayTwo() => new[]
        {
            new StepData("ALAN LENNOX-BOYD", "If Malaya becomes self-governing, how will its government maintain stability and security?",
                new ChoiceData("Responsibility should increasingly pass to Malayan ministers while organized institutions continue to handle internal security.", "ALAN LENNOX-BOYD", "That is a more practical position. Greater responsibility must be matched by institutions capable of carrying it. A planned transfer offers stronger grounds for confidence than simply assuming existing security concerns will disappear.", 15, 5),
                new ChoiceData("Malaya has functioning institutions and we are prepared to take greater responsibility gradually.", "ALAN LENNOX-BOYD", "That is encouraging, but we will require more detail. Readiness must be demonstrated through specific arrangements, especially while the Emergency continues.", 10),
                new ChoiceData("Security will solve itself after independence.", "ALAN LENNOX-BOYD", "Independence cannot make an existing security problem disappear. A future government must be prepared to inherit difficult responsibilities, not merely the authority that accompanies them.", -15))
        };

        private static StepData[] DayThree() => new[]
        {
            new StepData("ALAN LENNOX-BOYD", "An independent government must also support its administration and economy. How will Malaya approach this responsibility?",
                new ChoiceData("Take responsibility for national finances while maintaining economic stability and encouraging continued investment.", "ALAN LENNOX-BOYD", "That approach recognizes both political responsibility and economic continuity. Greater control over finance will carry obligations, but a stable transition can benefit Malaya after independence.", 15, 0, 5),
                new ChoiceData("Continue allowing Britain to control Malaya's finances indefinitely.", "ALAN LENNOX-BOYD", "Such an arrangement might reduce immediate uncertainty, but I question whether it is compatible with the full self-government your delegation is requesting.", 5, -10, -15),
                new ChoiceData("Immediately abandon existing financial arrangements without replacement.", "ALAN LENNOX-BOYD", "Replacing one system without preparing another would create uncertainty. Independence requires the ability to administer public finances, not the absence of financial administration.", -15))
        };

        private static StepData[] DayFour() => new[]
        {
            new StepData(new[]
                {
                    new LineData("RADIO HOST", "We are live. Citizens across Malaya want to know what the delegation's negotiations mean for them."),
                    new LineData("CITIZEN", "Why should the people trust negotiation rather than confrontation?")
                },
                new ChoiceData("Independence must be pursued through unity, negotiation and peaceful political action.", "CITIZEN", "Then the people must remain part of that effort. If negotiation is to represent Malaya, the public must understand what is being negotiated and why unity matters.", 0, 5, 15),
                new ChoiceData("There is no need for public participation; leave everything to the politicians.", "CITIZEN", "But independence concerns the future of everyone who lives here. If citizens are told their support does not matter, why should they feel represented by the negotiations?", 0, 0, -15),
                new ChoiceData("Only confrontation can achieve independence.", "CITIZEN", "Some listeners may find that message forceful, but others fear what confrontation could mean for Malaya's stability and unity.", -20, -10, 5))
        };

        private static StepData[] DayFive() => new[]
        {
            new StepData("TUNKU ABDUL RAHMAN", "Britain wants future defense cooperation even after Malaya becomes self-governing. Some members of the delegation worry that agreeing to cooperation could weaken our independence. What should our position be?",
                new ChoiceData("Separate temporary cooperation from permanent political control. Independence remains the objective.", "TUNKU ABDUL RAHMAN", "Exactly. Cooperation and political control are not the same thing. We can discuss arrangements that address practical defense concerns without abandoning the principle that Malaya must govern itself.", 5, 15),
                new ChoiceData("Reject every form of cooperation.", "TUNKU ABDUL RAHMAN", "That position is clear, but perhaps too absolute. Independence means making our own decisions; it does not require refusing every future agreement with another country.", -15, 5),
                new ChoiceData("Let Britain decide.", "TUNKU ABDUL RAHMAN", "Then what have we come here to negotiate? If Malaya is to become independent, Malayan representatives must be prepared to take responsibility for decisions that affect the country.", 0, -20, -10))
        };

        private static StepData[] DaySix() => new[]
        {
            new StepData(new[]
                {
                    new LineData("ALAN LENNOX-BOYD", "Over the past weeks, we have discussed security, finance, defense and the future form of government. We must now decide whether there is a workable path toward full self-government and independence."),
                    new LineData("TUNKU ABDUL RAHMAN", "The Federation delegation is ready."),
                    new LineData("ALAN LENNOX-BOYD", "Independence requires a constitution accepted by Malaya's institutions. How should that be prepared?")
                },
                new ChoiceData("Establish an independent constitutional commission and consult the communities of Malaya.", "ALAN LENNOX-BOYD", "That provides a structured way forward. The Commission can examine the Federation's constitutional arrangements and make recommendations before independence.", 10, 10),
                new ChoiceData("Keep the existing constitutional system permanently unchanged.", "ALAN LENNOX-BOYD", "Then full self-government would be difficult to achieve. Independence requires constitutional arrangements suited to an independent Federation.", 0, -10, -10),
                new ChoiceData("Write an entirely new constitution immediately without consultation or review.", "ALAN LENNOX-BOYD", "Constitutional change of this scale requires careful examination. Moving immediately without review would risk leaving major questions unresolved.", -10, -5)),
            new StepData("ALAN LENNOX-BOYD", "The delegation has asked for a clear target. When should full self-government and independence be achieved?",
                new ChoiceData("By August 1957, allowing time for constitutional preparation while setting a clear and near-term goal.", "ALAN LENNOX-BOYD", "It is an ambitious timetable, but a definite target provides direction. If the constitutional work proceeds successfully, every effort can be made to achieve independence by then.", 10, 0, 10),
                new ChoiceData("Leave the date completely undefined.", "ALAN LENNOX-BOYD", "Without a target, the delegation would return to Malaya unable to say when the transition is expected to occur.", 0, 0, -15),
                new ChoiceData("Declare independence immediately, before constitutional preparations are completed.", "ALAN LENNOX-BOYD", "The desire for independence is understood, but the constitutional and administrative arrangements cannot simply be ignored.", -15, -5)),
            new StepData(new[]
                {
                    new LineData("TUNKU ABDUL RAHMAN", "We have secured a path toward full self-government, constitutional reform and a target for independence. It is not the end of the work, but it may be the beginning of an independent Malaya."),
                    new LineData("TUNKU ABDUL RAHMAN", "Shall we move forward with the agreement?")
                },
                new ChoiceData("Yes. This gives Malaya a clear path toward independence.", "TUNKU ABDUL RAHMAN", "Then we move forward together. There is still much work ahead, but Malaya now has a destination - and a date toward which we can work.", 0, 10),
                new ChoiceData("We should abandon the conference and begin again.", "TUNKU ABDUL RAHMAN", "Then everything achieved during these negotiations is placed in doubt. Without accepting a path forward, there can be no agreement from this conference.", 0, -50))
        };

        private static void Validate(Scene scene, LevelTwoConversationTrigger[] triggers)
        {
            LevelTwoConversationViewer viewer = FindUnique<LevelTwoConversationViewer>(scene);
            LevelTwoConfirmationPanel confirmation = FindUnique<LevelTwoConfirmationPanel>(scene);
            LevelTwoConversationCameraFocus focus = FindUnique<LevelTwoConversationCameraFocus>(scene);
            LevelTwoNegotiationMeterController meters = FindUnique<LevelTwoNegotiationMeterController>(scene);
            LevelTwoDoorInteractor interactor = FindUnique<LevelTwoDoorInteractor>(scene);
            LevelTwoDayController dayController = FindUnique<LevelTwoDayController>(scene);
            SerializedObject interactorData = new SerializedObject(interactor);
            SerializedObject dayData = new SerializedObject(dayController);
            SerializedObject viewerData = new SerializedObject(viewer);
            SerializedObject meterData = new SerializedObject(meters);
            LevelTwoConversationTrigger finalTrigger = triggers.Single(item => item.ActiveDay == 6);
            if (viewer == null || confirmation == null || focus == null || meters == null || triggers.Length != 6 ||
                triggers.Any(item => item == null || item.InteractionCollider == null || item.StepCount == 0) ||
                triggers.Count(item => item.CameraFocusTarget != null) != 5 ||
                !finalTrigger.CalculatesFinalResult ||
                viewerData.FindProperty("meterController").objectReferenceValue != meters ||
                meterData.FindProperty("britishConfidenceSlider").objectReferenceValue == null ||
                meterData.FindProperty("delegationUnitySlider").objectReferenceValue == null ||
                meterData.FindProperty("publicSupportSlider").objectReferenceValue == null ||
                meterData.FindProperty("finalOverlay").objectReferenceValue == null ||
                meterData.FindProperty("finalDayEntryRequirement").intValue != 50 ||
                dayData.FindProperty("negotiationMeters").objectReferenceValue != meters ||
                dayData.FindProperty("transitionHiddenHud").arraySize != 2 ||
                interactorData.FindProperty("conversations").arraySize != 6 ||
                interactorData.FindProperty("confirmationPanel").objectReferenceValue == null)
                throw new InvalidOperationException("Level 2 conversation wiring is incomplete.");

            Transform hud = FindUniqueTransform(scene, "Day HUD");
            int meterIndex = hud.Find(MeterPanelName).GetSiblingIndex();
            int energyIndex = hud.Find("Energy Display").GetSiblingIndex();
            int dialogueIndex = hud.Find(PanelName).GetSiblingIndex();
            int documentIndex = hud.Find("Level 2 Document Panel").GetSiblingIndex();
            int dayCardIndex = hud.Find("Day Transition Card").GetSiblingIndex();
            if (meterIndex >= dialogueIndex || meterIndex >= documentIndex || meterIndex >= dayCardIndex ||
                energyIndex >= dayCardIndex)
                throw new InvalidOperationException("Persistent Level 2 HUD elements are not behind modal/day panels.");

            Debug.Log("LEVEL2_CONVERSATION_VALID dailyTriggers=6 meters=3 initial=30 day6Gate=>50 finalRequirement=>65 persistentHudBehindModals=true finalScene=Cutscene_Level3");
        }

        private static T FindUnique<T>(Scene scene) where T : Component
        {
            T[] matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one {typeof(T).Name}, found {matches.Length}.");
            return matches[0];
        }

        private static Transform FindUniqueTransform(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one '{name}', found {matches.Length}.");
            return matches[0];
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            return result;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = CreateUiObject(name, parent).AddComponent<Image>();
            image.sprite = sprite; image.color = color; return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string value,
            float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateUiObject(name, parent).AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = alignment; text.raycastTarget = false; return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; rect.localScale = Vector3.one;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void MoveBefore(Transform item, Transform reference)
        {
            if (item == null || reference == null || item.parent != reference.parent) return;
            item.SetSiblingIndex(reference.GetSiblingIndex());
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceArray<T>(UnityEngine.Object target, string field, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty array = data.FindProperty(field);
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
