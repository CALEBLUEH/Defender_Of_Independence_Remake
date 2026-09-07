using System;
using System.Linq;
using DefenderOfIndependence.Cutscenes;
using Fungus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefenderOfIndependence.EditorTools
{
    public static class LevelTwoCutsceneSetup
    {
        private const string TemplateScenePath = "Assets/Scene/Cutscene_Level1.unity";
        private const string TargetScenePath = "Assets/Scene/Cutscene_Level2.unity";
        private const string TargetLevelName = "Scene_Level2";
        private const string TemplateRootName = "Level 1 Cutscene";
        private const string TargetRootName = "Level 2 Cutscene";
        private const string FlowchartName = "Level 2 Prologue Flowchart";
        private const string BlockName = "Play Level 2 Prologue";

        private static readonly DialogueLine[] Dialogue =
        {
            new DialogueLine("Narrator", "August 1945. Japan surrendered, ending the occupation of Malaya after years of war, fear and hardship."),
            new DialogueLine("Narrator", "British administration returned, but the experience of occupation had changed the country. More people now believed that Malaya must determine its own future."),
            new DialogueLine("Narrator", "The Malayan Union proposal of 1946 brought widespread opposition. Political leaders answered through organised rallies, public discussion and peaceful action."),
            new DialogueLine("Narrator", "Among them was Tunku Abdul Rahman, born in 1903 and educated in law in England. He helped unite communities behind a constitutional path to independence."),
            new DialogueLine("Narrator", "By 1956, Tunku led a delegation to London to negotiate full self-government, constitutional reform and a clear date for independence."),
            new DialogueLine("Tunku Abdul Rahman", "You will serve as a junior member of our delegation. Study the documents, listen carefully and help us present a united and practical plan for Malaya."),
            new DialogueLine("Tunku Abdul Rahman", "British confidence, delegation unity and public support must all remain strong. The road to Merdeka now depends on the choices we make together.")
        };

        [MenuItem("Project Tools/Cutscenes/Rebuild Level 2 Cutscene")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Level 2 Cutscene",
                    "This replaces only the generated Level 2 cutscene root using the Level 1 visual template. Continue?",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            Rebuild();
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Rebuild();
                Debug.Log("LEVEL2_CUTSCENE_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL2_CUTSCENE_SETUP_FAILED");
                EditorApplication.Exit(1);
            }
        }

        public static void Rebuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene templateScene = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Single);
            GameObject templateRoot = templateScene.GetRootGameObjects().FirstOrDefault(root => root.name == TemplateRootName) ??
                                      throw new InvalidOperationException("The Level 1 cutscene template root was not found.");
            Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);

            foreach (GameObject oldRoot in targetScene.GetRootGameObjects()
                         .Where(root => root.name == TargetRootName || root.name == TemplateRootName || root.name == "EventSystem")
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            GameObject root = UnityEngine.Object.Instantiate(templateRoot);
            root.name = TargetRootName;
            SceneManager.MoveGameObjectToScene(root, targetScene);

            Transform oldFlowchart = root.transform.Find("Level 1 Prologue Flowchart");
            if (oldFlowchart != null)
            {
                UnityEngine.Object.DestroyImmediate(oldFlowchart.gameObject);
            }

            TMP_Text chapter = FindNamedComponent<TMP_Text>(root, "Chapter Label");
            TMP_Text title = FindNamedComponent<TMP_Text>(root, "Cutscene Title");
            chapter.text = "CHAPTER II";
            title.text = "MALAYA  ·  1945-1956\nTHE ROAD TO NEGOTIATION";

            SayDialog sayDialog = root.GetComponentInChildren<SayDialog>(true) ??
                                  throw new InvalidOperationException("The cutscene template has no SayDialog.");
            CutscenePromptController promptController = root.GetComponentInChildren<CutscenePromptController>(true) ??
                                                          throw new InvalidOperationException("The cutscene template has no prompt controller.");

            GameObject flowchartObject = new GameObject(FlowchartName);
            flowchartObject.transform.SetParent(root.transform, false);
            Flowchart flowchart = flowchartObject.AddComponent<Flowchart>();
            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = BlockName;
            FlowchartEnabled enabledHandler = flowchartObject.AddComponent<FlowchartEnabled>();
            enabledHandler.ParentBlock = block;
            block._EventHandler = enabledHandler;

            Character narrator = CreateCharacter(flowchartObject.transform, "Narrator", sayDialog, new Color(0.25f, 0.15f, 0.08f));
            Character tunku = CreateCharacter(flowchartObject.transform, "Tunku Abdul Rahman", sayDialog, new Color(0.31f, 0.12f, 0.07f));

            for (int index = 0; index < Dialogue.Length; index++)
            {
                if (index == Dialogue.Length - 1)
                {
                    SetCutscenePromptMode promptMode = AddCommand<SetCutscenePromptMode>(flowchart, block);
                    SerializedObject promptModeObject = new SerializedObject(promptMode);
                    promptModeObject.FindProperty("promptController").objectReferenceValue = promptController;
                    promptModeObject.FindProperty("finalPrompt").boolValue = true;
                    promptModeObject.ApplyModifiedPropertiesWithoutUndo();
                }

                DialogueLine line = Dialogue[index];
                Say say = AddCommand<Say>(flowchart, block);
                SerializedObject sayObject = new SerializedObject(say);
                sayObject.FindProperty("storyText").stringValue = line.Text;
                sayObject.FindProperty("character").objectReferenceValue = line.Speaker == "Narrator" ? narrator : tunku;
                sayObject.FindProperty("showAlways").boolValue = true;
                sayObject.FindProperty("extendPrevious").boolValue = false;
                sayObject.FindProperty("fadeWhenDone").boolValue = false;
                sayObject.FindProperty("waitForClick").boolValue = true;
                sayObject.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
                sayObject.ApplyModifiedPropertiesWithoutUndo();
            }

            LoadSceneDirect loadScene = AddCommand<LoadSceneDirect>(flowchart, block);
            SerializedObject loadSceneObject = new SerializedObject(loadScene);
            loadSceneObject.FindProperty("targetScene").stringValue = TargetLevelName;
            loadSceneObject.ApplyModifiedPropertiesWithoutUndo();

            CanvasGroup canvasGroup = root.GetComponentInChildren<CanvasGroup>(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(templateScene, true);
            EditorSceneManager.SetActiveScene(targetScene);
            Validate(targetScene);
        }

        private static void Validate(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == TargetRootName) ??
                              throw new InvalidOperationException("The Level 2 cutscene root was not created.");
            Flowchart flowchart = root.GetComponentsInChildren<Flowchart>(true).FirstOrDefault(item => item.gameObject.name == FlowchartName) ??
                                  throw new InvalidOperationException("The Level 2 Flowchart was not created.");
            Block block = flowchart.FindBlock(BlockName);
            int expectedCommands = Dialogue.Length + 2;
            if (block == null || block.CommandList.Count != expectedCommands)
            {
                throw new InvalidOperationException($"Expected {expectedCommands} Level 2 cutscene commands.");
            }

            LoadSceneDirect load = block.CommandList.OfType<LoadSceneDirect>().SingleOrDefault();
            if (load == null || load.GetSummary() != TargetLevelName)
            {
                throw new InvalidOperationException("The Level 2 cutscene does not load Scene_Level2.");
            }

            if (!EditorBuildSettings.scenes.Any(item => item.enabled && item.path == TargetScenePath) ||
                !EditorBuildSettings.scenes.Any(item => item.enabled && item.path.EndsWith($"/{TargetLevelName}.unity", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Cutscene_Level2 or Scene_Level2 is missing from Build Settings.");
            }

            Debug.Log($"LEVEL2_CUTSCENE_VALID dialogue={Dialogue.Length} prompt=space finalPrompt=start target={TargetLevelName}");
        }

        private static Character CreateCharacter(Transform parent, string displayName, SayDialog sayDialog, Color nameColor)
        {
            GameObject characterObject = new GameObject(displayName);
            characterObject.transform.SetParent(parent, false);
            Character character = characterObject.AddComponent<Character>();
            SerializedObject serialized = new SerializedObject(character);
            serialized.FindProperty("nameText").stringValue = displayName;
            serialized.FindProperty("nameColor").colorValue = nameColor;
            serialized.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static T FindNamedComponent<T>(GameObject root, string objectName) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).First(component => component.gameObject.name == objectName);
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
