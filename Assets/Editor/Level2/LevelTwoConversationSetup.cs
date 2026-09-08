using System;
using System.Collections.Generic;
using System.Linq;
using DefenderOfIndependence.Level2;
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
        private const string TitleFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
        private const string BodyFontPath = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string PanelAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/panel_beige.png";
        private const string ButtonAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/buttonLong_brown.png";

        private readonly struct ChoiceData
        {
            public readonly string Text;
            public readonly string ResponseSpeaker;
            public readonly string Response;
            public ChoiceData(string text, string responseSpeaker, string response)
            {
                Text = text; ResponseSpeaker = responseSpeaker; Response = response;
            }
        }

        private readonly struct StepData
        {
            public readonly string Speaker;
            public readonly string Prompt;
            public readonly ChoiceData[] Choices;
            public StepData(string speaker, string prompt, params ChoiceData[] choices)
            {
                Speaker = speaker; Prompt = prompt; Choices = choices;
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

            DestroyChild(dayHud, PanelName);
            CreateConversationPanel(dayHud, out GameObject panel, out TMP_Text speakerText, out TMP_Text dialogueText,
                out Button[] choiceButtons, out TMP_Text[] choiceLabels, out Button continueButton, out TMP_Text continueLabel);

            LevelTwoConversationViewer viewer = dayController.GetComponent<LevelTwoConversationViewer>() ??
                                                dayController.gameObject.AddComponent<LevelTwoConversationViewer>();
            SetReference(viewer, "panel", panel);
            SetReference(viewer, "speakerText", speakerText);
            SetReference(viewer, "dialogueText", dialogueText);
            SetReferenceArray(viewer, "choiceButtons", choiceButtons);
            SetReferenceArray(viewer, "choiceLabels", choiceLabels);
            SetReference(viewer, "continueButton", continueButton);
            SetReference(viewer, "continueLabel", continueLabel);
            SetReference(viewer, "playerController", player);

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

            ConfigureFinalDayDoor(scene, "TunkuAbdulRahmanDoor", false);
            ConfigureFinalDayDoor(scene, "RadioStationDoor", false);
            ConfigureFinalDayDoor(scene, "AlanLennox-BoydDoor", true);

            panel.SetActive(false);
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
            return ConfigureTrigger(target, collider, day, displayName, steps);
        }

        private static LevelTwoConversationTrigger ConfigureExistingCollider(Scene scene, string objectName, int day,
            string displayName, StepData[] steps)
        {
            Transform target = FindUniqueTransform(scene, objectName);
            Collider collider = target.GetComponent<Collider>() ??
                                throw new InvalidOperationException(objectName + " needs its authored Collider.");
            return ConfigureTrigger(target, collider, day, displayName, steps);
        }

        private static LevelTwoConversationTrigger ConfigureTrigger(Transform target, Collider collider, int day,
            string displayName, StepData[] steps)
        {
            LevelTwoConversationTrigger trigger = target.GetComponent<LevelTwoConversationTrigger>() ??
                                                  target.gameObject.AddComponent<LevelTwoConversationTrigger>();
            SerializedObject data = new SerializedObject(trigger);
            data.FindProperty("activeDay").intValue = day;
            data.FindProperty("conversationDisplayName").stringValue = displayName;
            data.FindProperty("interactionCollider").objectReferenceValue = collider;
            data.FindProperty("energyCost").intValue = 1;
            SerializedProperty stepArray = data.FindProperty("steps");
            stepArray.arraySize = steps.Length;
            for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                StepData sourceStep = steps[stepIndex];
                SerializedProperty step = stepArray.GetArrayElementAtIndex(stepIndex);
                step.FindPropertyRelative("speaker").stringValue = sourceStep.Speaker;
                step.FindPropertyRelative("prompt").stringValue = sourceStep.Prompt;
                SerializedProperty choices = step.FindPropertyRelative("choices");
                choices.arraySize = sourceStep.Choices.Length;
                for (int choiceIndex = 0; choiceIndex < sourceStep.Choices.Length; choiceIndex++)
                {
                    ChoiceData sourceChoice = sourceStep.Choices[choiceIndex];
                    SerializedProperty choice = choices.GetArrayElementAtIndex(choiceIndex);
                    choice.FindPropertyRelative("text").stringValue = sourceChoice.Text;
                    choice.FindPropertyRelative("responseSpeaker").stringValue = sourceChoice.ResponseSpeaker;
                    choice.FindPropertyRelative("response").stringValue = sourceChoice.Response;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
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

        private static void CreateConversationPanel(Transform parent, out GameObject panel, out TMP_Text speaker,
            out TMP_Text dialogue, out Button[] choiceButtons, out TMP_Text[] choiceLabels,
            out Button continueButton, out TMP_Text continueLabel)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Sprite buttonSprite = Load<Sprite>(ButtonAsset);

            Image overlay = CreateImage(parent, PanelName, null, new Color(0.006f, 0.01f, 0.018f, 0.74f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            panel = overlay.gameObject;

            Image window = CreateImage(overlay.transform, "Conversation Window", panelSprite, new Color(0.86f, 0.79f, 0.63f, 1f));
            window.type = Image.Type.Sliced;
            SetRect(window.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 410f), new Vector2(1360f, 760f));

            speaker = CreateText(window.transform, "Speaker", titleFont, "SPEAKER", 31f,
                new Color(0.19f, 0.105f, 0.045f), TextAlignmentOptions.Center);
            SetRect(speaker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1160f, 58f));

            Image dialogueBackground = CreateImage(window.transform, "Dialogue Background", null, new Color(0.25f, 0.14f, 0.06f, 0.12f));
            SetRect(dialogueBackground.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(1180f, 210f));
            dialogue = CreateText(dialogueBackground.transform, "Dialogue", bodyFont, "Dialogue text", 25f,
                new Color(0.14f, 0.075f, 0.03f), TextAlignmentOptions.MidlineLeft);
            Stretch(dialogue.rectTransform, new Vector2(28f, 18f), new Vector2(-28f, -18f));
            dialogue.textWrappingMode = TextWrappingModes.Normal;

            choiceButtons = new Button[3];
            choiceLabels = new TMP_Text[3];
            float[] choiceY = { 335f, 225f, 115f };
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                Image choiceImage = CreateImage(window.transform, $"Choice {index + 1}", buttonSprite, new Color(0.33f, 0.18f, 0.075f, 1f));
                choiceImage.type = Image.Type.Sliced;
                SetRect(choiceImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, choiceY[index]), new Vector2(1160f, 94f));
                choiceButtons[index] = choiceImage.gameObject.AddComponent<Button>();
                choiceLabels[index] = CreateText(choiceImage.transform, "Label", bodyFont, "Choice", 21f,
                    new Color(0.98f, 0.94f, 0.84f), TextAlignmentOptions.Center);
                Stretch(choiceLabels[index].rectTransform, new Vector2(24f, 10f), new Vector2(-24f, -10f));
                choiceLabels[index].textWrappingMode = TextWrappingModes.Normal;
            }

            Image continueImage = CreateImage(window.transform, "Continue Button", buttonSprite, new Color(0.33f, 0.18f, 0.075f, 1f));
            continueImage.type = Image.Type.Sliced;
            SetRect(continueImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 205f), new Vector2(430f, 78f));
            continueButton = continueImage.gameObject.AddComponent<Button>();
            continueLabel = CreateText(continueImage.transform, "Label", titleFont, "CONTINUE", 21f, Color.white, TextAlignmentOptions.Center);
            Stretch(continueLabel.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));

            TMP_Text hint = CreateText(window.transform, "Choice Hint", bodyFont, "CHOOSE A RESPONSE WITH THE MOUSE", 18f,
                new Color(0.31f, 0.18f, 0.08f), TextAlignmentOptions.Center);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(700f, 32f));
        }

        private static StepData[] DayOne() => new[]
        {
            new StepData("TUNKU ABDUL RAHMAN",
                "Before we face the British government, our own delegation must agree on what we are asking for.",
                new ChoiceData("We should agree on one clear objective: self-government leading to independence, while preparing answers on security and administration.", "TUNKU ABDUL RAHMAN", "Good. Independence is our objective, but a united delegation must also show that we understand the responsibilities that come with governing a country. We cannot arrive in London with only a demand; we must arrive with a plan."),
                new ChoiceData("Demand immediate independence and refuse to discuss any British concerns.", "TUNKU ABDUL RAHMAN", "Determination is important, but refusing every discussion would make the conference pointless. Some may applaud a stronger demand, yet we still need an agreement that can actually move Malaya toward independence."),
                new ChoiceData("Let every delegate present a different position.", "TUNKU ABDUL RAHMAN", "Then we would appear divided before negotiations have even begun. We may disagree during preparation, but once we enter the conference we must understand what we are trying to achieve together."))
        };

        private static StepData[] DayTwo() => new[]
        {
            new StepData("ALAN LENNOX-BOYD", "If Malaya becomes self-governing, how will its government maintain stability and security?",
                new ChoiceData("Responsibility should increasingly pass to Malayan ministers while organized institutions continue to handle internal security.", "ALAN LENNOX-BOYD", "That is a more practical position. Greater responsibility must be matched by institutions capable of carrying it. A planned transfer offers stronger grounds for confidence than simply assuming existing security concerns will disappear."),
                new ChoiceData("Malaya has functioning institutions and we are prepared to take greater responsibility gradually.", "ALAN LENNOX-BOYD", "That is encouraging, but we will require more detail. Readiness must be demonstrated through specific arrangements, especially while the Emergency continues."),
                new ChoiceData("Security will solve itself after independence.", "ALAN LENNOX-BOYD", "Independence cannot make an existing security problem disappear. A future government must be prepared to inherit difficult responsibilities, not merely the authority that accompanies them."))
        };

        private static StepData[] DayThree() => new[]
        {
            new StepData("ALAN LENNOX-BOYD", "An independent government must also support its administration and economy. How will Malaya approach this responsibility?",
                new ChoiceData("Take responsibility for national finances while maintaining economic stability and encouraging continued investment.", "ALAN LENNOX-BOYD", "That approach recognizes both political responsibility and economic continuity. Greater control over finance will carry obligations, but a stable transition can benefit Malaya after independence."),
                new ChoiceData("Continue allowing Britain to control Malaya's finances indefinitely.", "ALAN LENNOX-BOYD", "Such an arrangement might reduce immediate uncertainty, but I question whether it is compatible with the full self-government your delegation is requesting."),
                new ChoiceData("Immediately abandon existing financial arrangements without replacement.", "ALAN LENNOX-BOYD", "Replacing one system without preparing another would create uncertainty. Independence requires the ability to administer public finances, not the absence of financial administration."))
        };

        private static StepData[] DayFour() => new[]
        {
            new StepData("LIVE RADIO BROADCAST", "Radio Host: We are live. Citizens across Malaya want to know what the delegation's negotiations mean for them.\n\nCitizen: Why should the people trust negotiation rather than confrontation?",
                new ChoiceData("Independence must be pursued through unity, negotiation and peaceful political action.", "CITIZEN", "Then the people must remain part of that effort. If negotiation is to represent Malaya, the public must understand what is being negotiated and why unity matters."),
                new ChoiceData("There is no need for public participation; leave everything to the politicians.", "CITIZEN", "But independence concerns the future of everyone who lives here. If citizens are told their support does not matter, why should they feel represented by the negotiations?"),
                new ChoiceData("Only confrontation can achieve independence.", "CITIZEN", "Some listeners may find that message forceful, but others fear what confrontation could mean for Malaya's stability and unity."))
        };

        private static StepData[] DayFive() => new[]
        {
            new StepData("TUNKU ABDUL RAHMAN", "Britain wants future defense cooperation even after Malaya becomes self-governing. Some members of the delegation worry that agreeing to cooperation could weaken our independence. What should our position be?",
                new ChoiceData("Separate temporary cooperation from permanent political control. Independence remains the objective.", "TUNKU ABDUL RAHMAN", "Exactly. Cooperation and political control are not the same thing. We can discuss arrangements that address practical defense concerns without abandoning the principle that Malaya must govern itself."),
                new ChoiceData("Reject every form of cooperation.", "TUNKU ABDUL RAHMAN", "That position is clear, but perhaps too absolute. Independence means making our own decisions; it does not require refusing every future agreement with another country."),
                new ChoiceData("Let Britain decide.", "TUNKU ABDUL RAHMAN", "Then what have we come here to negotiate? If Malaya is to become independent, Malayan representatives must be prepared to take responsibility for decisions that affect the country."))
        };

        private static StepData[] DaySix() => new[]
        {
            new StepData("FINAL CONSTITUTIONAL CONFERENCE", "Alan Lennox-Boyd: Over the past weeks, we have discussed security, finance, defense and the future form of government. We must now decide whether there is a workable path toward full self-government and independence.\n\nTunku: The Federation delegation is ready.\n\nAlan Lennox-Boyd: Independence requires a constitution accepted by Malaya's institutions. How should that be prepared?",
                new ChoiceData("Establish an independent constitutional commission and consult the communities of Malaya.", "ALAN LENNOX-BOYD", "That provides a structured way forward. The Commission can examine the Federation's constitutional arrangements and make recommendations before independence."),
                new ChoiceData("Keep the existing constitutional system permanently unchanged.", "ALAN LENNOX-BOYD", "Then full self-government would be difficult to achieve. Independence requires constitutional arrangements suited to an independent Federation."),
                new ChoiceData("Write an entirely new constitution immediately without consultation or review.", "ALAN LENNOX-BOYD", "Constitutional change of this scale requires careful examination. Moving immediately without review would risk leaving major questions unresolved.")),
            new StepData("ALAN LENNOX-BOYD", "The delegation has asked for a clear target. When should full self-government and independence be achieved?",
                new ChoiceData("By August 1957, allowing time for constitutional preparation while setting a clear and near-term goal.", "ALAN LENNOX-BOYD", "It is an ambitious timetable, but a definite target provides direction. If the constitutional work proceeds successfully, every effort can be made to achieve independence by then."),
                new ChoiceData("Leave the date completely undefined.", "ALAN LENNOX-BOYD", "Without a target, the delegation would return to Malaya unable to say when the transition is expected to occur."),
                new ChoiceData("Declare independence immediately, before constitutional preparations are completed.", "ALAN LENNOX-BOYD", "The desire for independence is understood, but the constitutional and administrative arrangements cannot simply be ignored.")),
            new StepData("TUNKU ABDUL RAHMAN", "We have secured a path toward full self-government, constitutional reform and a target for independence. It is not the end of the work, but it may be the beginning of an independent Malaya.\n\nShall we move forward with the agreement?",
                new ChoiceData("Yes. This gives Malaya a clear path toward independence.", "TUNKU ABDUL RAHMAN", "Then we move forward together. There is still much work ahead, but Malaya now has a destination - and a date toward which we can work."),
                new ChoiceData("We should abandon the conference and begin again.", "TUNKU ABDUL RAHMAN", "Then everything achieved during these negotiations is placed in doubt. Without accepting a path forward, there can be no agreement from this conference."))
        };

        private static void Validate(Scene scene, LevelTwoConversationTrigger[] triggers)
        {
            LevelTwoConversationViewer viewer = FindUnique<LevelTwoConversationViewer>(scene);
            LevelTwoDoorInteractor interactor = FindUnique<LevelTwoDoorInteractor>(scene);
            SerializedObject interactorData = new SerializedObject(interactor);
            if (viewer == null || triggers.Length != 6 || triggers.Any(item => item == null || item.InteractionCollider == null || item.StepCount == 0) ||
                interactorData.FindProperty("conversations").arraySize != 6)
                throw new InvalidOperationException("Level 2 conversation wiring is incomplete.");

            Debug.Log("LEVEL2_CONVERSATION_VALID dailyTriggers=6 choices=mouse energy=1 once=true doors=paid-per-day finalDay=Alan-only");
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
