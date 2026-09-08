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
    public static class LevelThreeCutsceneSetup
    {
        private const string TemplateScenePath = "Assets/Scene/Cutscene_Level1.unity";
        private const string TargetScenePath = "Assets/Scene/Cutscene_Level3.unity";
        private const string TargetLevelName = "Scene_Level3";
        private const string TemplateRootName = "Level 1 Cutscene";
        private const string TargetRootName = "Level 3 Cutscene";
        private const string FlowchartName = "Level 3 Prologue Flowchart";
        private const string BlockName = "Play Level 3 Prologue";

        private static readonly DialogueLine[] Dialogue =
        {
            new DialogueLine("Narrator", "February 1956. After weeks of negotiation in London, an agreement was reached on a path toward full self-government and independence for the Federation of Malaya."),
            new DialogueLine("Narrator", "A Constitutional Commission would prepare recommendations for a new constitution, while greater responsibilities passed to Malayan ministers."),
            new DialogueLine("Narrator", "On 8 February 1956, the agreement marked a decisive step on the road to Merdeka, with every effort directed toward independence by August 1957."),
            new DialogueLine("Narrator", "Twelve days later, Tunku Abdul Rahman returned from London to Padang Bandar Hilir in Malacca, where thousands waited for news."),
            new DialogueLine("Tunku Abdul Rahman", "The agreement has been secured. God willing, the Federation of Malaya will achieve independence on 31 August 1957."),
            new DialogueLine("Narrator", "Eighteen months passed as the nation prepared its constitution, government and independence ceremony."),
            new DialogueLine("Narrator", "31 August 1957. At Merdeka Square, the British flag would be lowered and the flag of the Federation of Malaya raised for the first time."),
            new DialogueLine("Tunku Abdul Rahman", "Today we stand at the threshold of freedom. The ceremony must carry the voice and hopes of our people."),
            new DialogueLine("Narrator", "Take your place as speech writer and ceremony director. Record Tunku's words, respond to technical problems, and guide the celebration through seven cries of Merdeka.")
        };

        [MenuItem("Project Tools/Cutscenes/Rebuild Level 3 Cutscene")]
        public static void RebuildFromMenu()
        {
            if (EditorUtility.DisplayDialog(
                    "Rebuild Level 3 Cutscene",
                    "This replaces only the generated Level 3 cutscene root using the Level 1 visual template. Continue?",
                    "Rebuild", "Cancel"))
            {
                Rebuild();
            }
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Rebuild();
                Debug.Log("LEVEL3_CUTSCENE_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL3_CUTSCENE_SETUP_FAILED");
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
            if (oldFlowchart != null) UnityEngine.Object.DestroyImmediate(oldFlowchart.gameObject);

            TMP_Text chapter = FindNamedComponent<TMP_Text>(root, "Chapter Label");
            TMP_Text title = FindNamedComponent<TMP_Text>(root, "Cutscene Title");
            chapter.text = "CHAPTER III";
            title.text = "FEBRUARY 1956  ·  AUGUST 1957\nTHE ROAD TO MERDEKA";

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
                    SerializedObject promptData = new SerializedObject(promptMode);
                    promptData.FindProperty("promptController").objectReferenceValue = promptController;
                    promptData.FindProperty("finalPrompt").boolValue = true;
                    promptData.ApplyModifiedPropertiesWithoutUndo();
                }

                DialogueLine line = Dialogue[index];
                Say say = AddCommand<Say>(flowchart, block);
                SerializedObject sayData = new SerializedObject(say);
                sayData.FindProperty("storyText").stringValue = line.Text;
                sayData.FindProperty("character").objectReferenceValue = line.Speaker == "Narrator" ? narrator : tunku;
                sayData.FindProperty("showAlways").boolValue = true;
                sayData.FindProperty("extendPrevious").boolValue = false;
                sayData.FindProperty("fadeWhenDone").boolValue = false;
                sayData.FindProperty("waitForClick").boolValue = true;
                sayData.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
                sayData.ApplyModifiedPropertiesWithoutUndo();
            }

            LoadSceneDirect load = AddCommand<LoadSceneDirect>(flowchart, block);
            SerializedObject loadData = new SerializedObject(load);
            loadData.FindProperty("targetScene").stringValue = TargetLevelName;
            loadData.ApplyModifiedPropertiesWithoutUndo();

            CanvasGroup canvasGroup = root.GetComponentInChildren<CanvasGroup>(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(templateScene, true);
            EditorSceneManager.SetActiveScene(targetScene);
            Validate(targetScene);
        }

        private static void Validate(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == TargetRootName) ??
                              throw new InvalidOperationException("The Level 3 cutscene root was not created.");
            Flowchart flowchart = root.GetComponentsInChildren<Flowchart>(true)
                .SingleOrDefault(item => item.gameObject.name == FlowchartName) ??
                                  throw new InvalidOperationException("The Level 3 Flowchart was not created.");
            Block block = flowchart.FindBlock(BlockName);
            if (block == null || block.CommandList.Count != Dialogue.Length + 2 ||
                block.CommandList.OfType<Say>().Count() != Dialogue.Length ||
                block.CommandList.OfType<SetCutscenePromptMode>().Count() != 1)
            {
                throw new InvalidOperationException("The Level 3 cutscene command sequence is incomplete.");
            }

            LoadSceneDirect load = block.CommandList.OfType<LoadSceneDirect>().SingleOrDefault();
            if (load == null || load.GetSummary() != TargetLevelName)
                throw new InvalidOperationException("The Level 3 cutscene does not load Scene_Level3.");

            if (!EditorBuildSettings.scenes.Any(item => item.enabled && item.path == TargetScenePath) ||
                !EditorBuildSettings.scenes.Any(item => item.enabled && item.path.EndsWith($"/{TargetLevelName}.unity", StringComparison.Ordinal)))
                throw new InvalidOperationException("Cutscene_Level3 or Scene_Level3 is missing from Build Settings.");

            Debug.Log($"LEVEL3_CUTSCENE_VALID dialogue={Dialogue.Length} prompt=space finalPrompt=start target={TargetLevelName}");
        }

        private static Character CreateCharacter(Transform parent, string displayName, SayDialog sayDialog, Color nameColor)
        {
            GameObject characterObject = new GameObject(displayName);
            characterObject.transform.SetParent(parent, false);
            Character character = characterObject.AddComponent<Character>();
            SerializedObject data = new SerializedObject(character);
            data.FindProperty("nameText").stringValue = displayName;
            data.FindProperty("nameColor").colorValue = nameColor;
            data.FindProperty("setSayDialog").objectReferenceValue = sayDialog;
            data.ApplyModifiedPropertiesWithoutUndo();
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

        private static T FindNamedComponent<T>(GameObject root, string objectName) where T : Component =>
            root.GetComponentsInChildren<T>(true).First(component => component.gameObject.name == objectName);

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
            public string Speaker { get; }
            public string Text { get; }
        }
    }
}
