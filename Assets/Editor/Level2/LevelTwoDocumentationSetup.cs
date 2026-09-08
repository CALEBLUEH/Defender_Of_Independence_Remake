using System;
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
    public static class LevelTwoDocumentationSetup
    {
        private const string ScenePath = "Assets/Scene/Scene_Level2.unity";
        private const string TitleFontPath = "Assets/Font/AudioWide/Audiowide-Regular SDF.asset";
        private const string BodyFontPath = "Assets/Plugins/Fungus/Thirdparty/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string PanelAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/panel_beige.png";
        private const string ButtonAsset = "Assets/ThirdParty/Kenney/UI Pack RPG Expansion/buttonLong_brown.png";
        private const string PanelName = "Level 2 Document Panel";
        private const string EnergyName = "Energy Display";

        private static readonly Page[] LobbyPages =
        {
            new Page("Level 2 Instructions",
                "You have five days to prepare for the final conference.\n\nEach major activity costs 1 Energy. You begin every day with 4 Energy. The first entry into each room during a day costs 1 Energy; after paying once, you may leave and re-enter that room freely until the next day.\n\nYour decisions affect:\nBritish Confidence\nDelegation Unity\nPublic Support\n\nRead documents before important conversations to strengthen related positive outcomes. Conversations cost 1 Energy and can only be completed once.\n\nCONTROLS\nW A S D – Move around the conference building\nMOUSE – Look around; use the pointer in documents and conversations\nC – Enter or leave a room, examine a document, or begin a conversation\nMOUSE WHEEL / SCROLLBAR – Scroll an open document\nX / C / ESC – Close an open document\n\nUse the clock in the Main Lobby when you are ready to end the day and restore your Energy.")
        };

        private static readonly Page[] BritishPages =
        {
            new Page("Federation of Malaya Constitutional Conference",
                "The London Constitutional Conference concerns more than the date of independence.\n\nMajor issues include:\n• Internal security\n• Finance and economic development\n• External defense\n• Public services\n• Future constitutional arrangements\n\nThe conference must determine how greater responsibility can be transferred to Malayan leaders while creating a workable path toward full self-government."),
            new Page("Interim Self-Government",
                "Independence could not be achieved through a single ceremonial declaration alone.\n\nThe London Conference proposed immediate constitutional changes before full independence.\n\nThese included transferring greater responsibility to Malayan ministers for finance and development and for internal defense and security.\n\nSuch changes would allow Malayan leaders to take greater responsibility for government while preparations for a new constitution continued."),
            new Page("Preparing a New Constitution",
                "The London Conference agreed that an independent Constitutional Commission should make recommendations for the future constitution of the Federation of Malaya.\n\nThe Commission would consider a federal system, parliamentary democracy, the constitutional position of the Malay Rulers, common nationality and the interests of Malaya's communities.\n\nLord Reid later became chairman of the Commission.")
        };

        private static readonly Page[] PlanningPages =
        {
            new Page("From the Malayan Union to Political Organization",
                "After British administration returned following the Second World War, the Malayan Union was established in 1946.\n\nThe proposal generated strong Malay opposition, particularly because of changes affecting the Malay Rulers and citizenship.\n\nPolitical opposition developed through organized rallies, associations and political activity rather than making armed struggle the central strategy.\n\nThis period contributed to the growth of organized politics that would later play an important role in Malaya's path toward self-government."),
            new Page("The Emergency and Internal Security",
                "Malaya's constitutional development took place while the country was also facing the Malayan Emergency.\n\nThe Malayan Communist Party had turned to armed struggle, making internal security an important concern during discussions about greater self-government.\n\nBecause of this situation, the transfer of responsibility for security could not simply happen without planning. The London discussions therefore considered how Malayan ministers could assume greater responsibility while security institutions continued to operate."),
            new Page("Federation Delegation – London, 1956",
                "The Federation delegation was broader than one individual.\n\nFour representatives came on behalf of the Malay Rulers.\n\nThe Alliance representatives included:\nTunku Abdul Rahman – Chief Minister\nH. S. Lee – Minister for Transport\nDr. Ismail Abdul Rahman – Minister for Natural Resources\nAbdul Razak Hussein – Minister for Education\n\nTheir participation reflected the need for agreement between political leaders and the Malay Rulers when discussing Malaya's constitutional future.")
        };

        private static readonly Page[] RadioPages =
        {
            new Page("The Alliance",
                "Malaya's independence movement depended not only on negotiation with Britain but also on cooperation within Malaya.\n\nThe Alliance brought together three major political organizations:\n• UMNO – United Malays National Organization\n• MCA – Malayan Chinese Association\n• MIC – Malayan Indian Congress\n\nCooperation between different communities strengthened the claim that Malaya could establish a representative government."),
            new Page("Persuasion Instead of Confrontation",
                "Malaya's struggle for independence did not depend on only one kind of political action.\n\nPolitical leaders organised parties, elections, public meetings and negotiations to build support for self-government.\n\nFor the delegation, maintaining public support for a peaceful constitutional route is important. Violent confrontation could weaken both unity at home and confidence during negotiations abroad."),
            new Page("The Formation of Malaysia",
                "Independence in 1957 concerned the Federation of Malaya.\n\nMalaysia was formed later.\n\nOn 16 September 1963, the Federation of Malaya, Sabah, Sarawak and Singapore formed Malaysia.\n\nSingapore later separated from Malaysia on 9 August 1965.")
        };

        private readonly struct Page
        {
            public readonly string Title;
            public readonly string Body;
            public Page(string title, string body) { Title = title; Body = body; }
        }

        [MenuItem("Project Tools/Level 2/Build Documentation System")]
        public static void RunFromMenu() => Run();

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                Debug.Log("LEVEL2_DOCUMENTATION_SETUP_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("LEVEL2_DOCUMENTATION_SETUP_FAILED");
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
            Camera playerCamera = player.GetComponentInChildren<Camera>(true) ??
                                  throw new InvalidOperationException("The Level 2 player camera is missing.");
            Transform dayHud = FindUniqueTransform(scene, "Day HUD");
            if (dayHud.GetComponent<GraphicRaycaster>() == null) dayHud.gameObject.AddComponent<GraphicRaycaster>();

            DestroyChild(dayHud, PanelName);
            DestroyChild(dayHud, EnergyName);
            TMP_Text energyText = CreateEnergyDisplay(dayHud);
            CreateDocumentPanel(dayHud, out GameObject panel, out TMP_Text title, out GameObject dayPromptRoot,
                out TMP_Text dayPrompt, out TMP_Text body, out ScrollRect scroll, out Button closeButton);

            LevelTwoDocumentViewer viewer = dayController.GetComponent<LevelTwoDocumentViewer>() ??
                                           dayController.gameObject.AddComponent<LevelTwoDocumentViewer>();
            SetReference(viewer, "panel", panel);
            SetReference(viewer, "titleText", title);
            SetReference(viewer, "dayPromptRoot", dayPromptRoot);
            SetReference(viewer, "dayPromptText", dayPrompt);
            SetReference(viewer, "bodyText", body);
            SetReference(viewer, "documentScroll", scroll);
            SetReference(viewer, "closeButton", closeButton);
            SetReference(viewer, "playerController", player);

            LevelTwoDocumentLocation[] documents =
            {
                ConfigureLocation(scene, "MainLobbyDocumentation(Tutorial)", LevelTwoDocumentLocation.RoomType.MainLobby, LobbyPages),
                ConfigureLocation(scene, "BritishOfficeNegotiateDocumentation", LevelTwoDocumentLocation.RoomType.BritishOffice, BritishPages),
                ConfigureLocation(scene, "PlanningRoomDocumentation", LevelTwoDocumentLocation.RoomType.PlanningRoom, PlanningPages),
                ConfigureLocation(scene, "RadioStationDocumentation", LevelTwoDocumentLocation.RoomType.RadioStation, RadioPages)
            };

            SetReference(dayController, "energyText", energyText);
            SerializedObject interactorData = new SerializedObject(interactor);
            interactorData.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            interactorData.FindProperty("dayController").objectReferenceValue = dayController;
            interactorData.FindProperty("documentViewer").objectReferenceValue = viewer;
            SerializedProperty array = interactorData.FindProperty("documents");
            array.arraySize = documents.Length;
            for (int i = 0; i < documents.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = documents[i];
            interactorData.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(scene, documents);
        }

        private static LevelTwoDocumentLocation ConfigureLocation(Scene scene, string name,
            LevelTwoDocumentLocation.RoomType room, Page[] pages)
        {
            Transform locationTransform = FindUniqueTransform(scene, name);
            Collider collider = locationTransform.GetComponent<Collider>() ??
                                throw new InvalidOperationException($"{name} needs a Collider.");
            LevelTwoDocumentLocation location = locationTransform.GetComponent<LevelTwoDocumentLocation>() ??
                                                locationTransform.gameObject.AddComponent<LevelTwoDocumentLocation>();
            SerializedObject data = new SerializedObject(location);
            data.FindProperty("room").enumValueIndex = (int)room;
            data.FindProperty("interactionCollider").objectReferenceValue = collider;
            bool isLobby = room == LevelTwoDocumentLocation.RoomType.MainLobby;
            data.FindProperty("reusable").boolValue = isLobby;
            data.FindProperty("energyCost").intValue = isLobby ? 0 : 1;
            SerializedProperty pageArray = data.FindProperty("pages");
            pageArray.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++)
            {
                SerializedProperty page = pageArray.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("title").stringValue = pages[i].Title;
                page.FindPropertyRelative("body").stringValue = pages[i].Body;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return location;
        }

        private static TMP_Text CreateEnergyDisplay(Transform parent)
        {
            TMP_FontAsset font = Load<TMP_FontAsset>(BodyFontPath);
            Image background = CreateImage(parent, EnergyName, null, new Color(0.025f, 0.035f, 0.055f, 0.9f));
            SetRect(background.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -54f), new Vector2(260f, 62f));
            TMP_Text text = CreateText(background.transform, "Energy Text", font, "ENERGY  4 / 4", 24f,
                new Color(0.95f, 0.82f, 0.43f), TextAlignmentOptions.Center);
            Stretch(text.rectTransform, new Vector2(14f, 5f), new Vector2(-14f, -5f));
            return text;
        }

        private static void CreateDocumentPanel(Transform parent, out GameObject panel, out TMP_Text title,
            out GameObject dayPromptRoot, out TMP_Text dayPrompt, out TMP_Text body, out ScrollRect scroll,
            out Button closeButton)
        {
            TMP_FontAsset titleFont = Load<TMP_FontAsset>(TitleFontPath);
            TMP_FontAsset bodyFont = Load<TMP_FontAsset>(BodyFontPath);
            Sprite panelSprite = Load<Sprite>(PanelAsset);
            Sprite buttonSprite = Load<Sprite>(ButtonAsset);

            Image overlay = CreateImage(parent, PanelName, null, new Color(0.008f, 0.012f, 0.02f, 0.86f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            panel = overlay.gameObject;

            Image window = CreateImage(overlay.transform, "Document Window", panelSprite, new Color(0.86f, 0.79f, 0.63f, 1f));
            window.type = Image.Type.Sliced;
            SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 820f));

            title = CreateText(window.transform, "Document Title", titleFont, "DOCUMENT", 38f,
                new Color(0.18f, 0.10f, 0.045f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -63f), new Vector2(930f, 74f));
            title.textWrappingMode = TextWrappingModes.Normal;

            Image closeImage = CreateImage(window.transform, "Close Button", buttonSprite, new Color(0.34f, 0.18f, 0.08f, 1f));
            closeImage.type = Image.Type.Sliced;
            SetRect(closeImage.rectTransform, Vector2.one, Vector2.one, new Vector2(-54f, -48f), new Vector2(72f, 58f));
            closeButton = closeImage.gameObject.AddComponent<Button>();
            TMP_Text closeLabel = CreateText(closeImage.transform, "X", titleFont, "X", 25f, Color.white, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.zero);

            GameObject scrollObject = CreateUiObject("Document Scroll View", window.transform);
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -20f), new Vector2(1000f, 615f));
            scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 45f;

            Image viewportImage = CreateImage(scrollObject.transform, "Viewport", null, new Color(0.16f, 0.09f, 0.04f, 0.08f));
            Stretch(viewportImage.rectTransform, new Vector2(8f, 8f), new Vector2(-50f, -8f));
            viewportImage.gameObject.AddComponent<RectMask2D>();

            GameObject contentObject = CreateUiObject("Content", viewportImage.transform);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 24f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image promptBackground = CreateImage(content, "Daily Prompt", null, new Color(0.3f, 0.18f, 0.08f, 0.16f));
            dayPromptRoot = promptBackground.gameObject;
            promptBackground.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
            dayPrompt = CreateText(promptBackground.transform, "Daily Prompt Text", bodyFont, "DAY 1 - PREPARATION", 23f,
                new Color(0.22f, 0.12f, 0.055f), TextAlignmentOptions.Left);
            Stretch(dayPrompt.rectTransform, new Vector2(18f, 12f), new Vector2(-18f, -12f));
            dayPrompt.textWrappingMode = TextWrappingModes.Normal;

            body = CreateText(content, "Document Body", bodyFont, "Document content", 25f,
                new Color(0.16f, 0.09f, 0.04f), TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 8f;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image scrollbarImage = CreateImage(scrollObject.transform, "Vertical Scrollbar", null, new Color(0.2f, 0.11f, 0.05f, 0.45f));
            Stretch(scrollbarImage.rectTransform, new Vector2(-36f, 8f), new Vector2(-8f, -8f), true);
            Scrollbar scrollbar = scrollbarImage.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            GameObject sliding = CreateUiObject("Sliding Area", scrollbarImage.transform);
            Stretch(sliding.GetComponent<RectTransform>(), new Vector2(4f, 8f), new Vector2(-4f, -8f));
            Image handle = CreateImage(sliding.transform, "Handle", null, new Color(0.48f, 0.25f, 0.1f, 1f));
            Stretch(handle.rectTransform, Vector2.zero, Vector2.zero);
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.value = 1f;
            scroll.viewport = viewportImage.rectTransform;
            scroll.content = content;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            TMP_Text closeHint = CreateText(window.transform, "Close Hint", bodyFont, "CLICK X, PRESS C, OR PRESS ESC TO CLOSE", 18f,
                new Color(0.31f, 0.18f, 0.08f), TextAlignmentOptions.Center);
            SetRect(closeHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 37f), new Vector2(600f, 32f));
        }

        private static void Validate(Scene scene, LevelTwoDocumentLocation[] documents)
        {
            LevelTwoDocumentViewer viewer = FindUnique<LevelTwoDocumentViewer>(scene);
            LevelTwoDoorInteractor interactor = FindUnique<LevelTwoDoorInteractor>(scene);
            LevelTwoDayController day = FindUnique<LevelTwoDayController>(scene);
            SerializedObject interactorData = new SerializedObject(interactor);
            SerializedObject dayData = new SerializedObject(day);
            if (documents.Length != 4 || documents.Any(item => item == null || item.InteractionCollider == null) ||
                viewer == null || interactorData.FindProperty("documents").arraySize != 4 ||
                dayData.FindProperty("energyText").objectReferenceValue == null)
            {
                throw new InvalidOperationException("Level 2 documentation wiring is incomplete.");
            }
            Debug.Log("LEVEL2_DOCUMENTATION_VALID lobby=free+reusable historicalPages=9 random=without-replacement pointer=enabled close=X/C/Escape");
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
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = CreateUiObject(name, parent).AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string value,
            float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateUiObject(name, parent).AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
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

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, bool rightAligned = false)
        {
            rect.anchorMin = rightAligned ? new Vector2(1f, 0f) : Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field) ?? throw new InvalidOperationException($"Missing {field} on {target.name}.");
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
