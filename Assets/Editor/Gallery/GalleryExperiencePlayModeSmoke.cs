using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[InitializeOnLoad]
public static class GalleryExperiencePlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Gallery.unity";
    private const string ActiveKey = "Defender.GalleryExperienceSmoke.Active";
    private const string ResultKey = "Defender.GalleryExperienceSmoke.Result";

    private static int stage;
    private static double deadline;
    private static Vector3 cameraStart;

    static GalleryExperiencePlayModeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Gallery/Validate Experience in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before validating the Gallery experience.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = true;
    }

    public static void RunFromCommandLine()
    {
        try { Run(); }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void ResumeAfterReload()
    {
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying)
        {
            Finish(result == "PASS");
            return;
        }
        if (!EditorApplication.isPlaying) return;
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 35d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline)
        {
            Fail("Timed out while exercising the Gallery dialogue and credit replay route.");
            return;
        }

        if (stage == 0)
        {
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            GalleryScreenFader fader = UnityEngine.Object.FindFirstObjectByType<GalleryScreenFader>();
            if (player == null || fader == null || fader.IsTransitioning || !player.ControlsEnabled) return;
            if (HorizontalDistance(player.transform.position, player.InitialSpawnPoint.position) > 0.05f)
            {
                Fail("Direct Gallery entry did not use InitialSpawnPoint.");
                return;
            }

            GalleryExhibitInteractor interactor = player.GetComponent<GalleryExhibitInteractor>();
            GalleryExhibit japan = FindExhibit("JapanPicture");
            Camera camera = player.GetComponentInChildren<Camera>();
            if (interactor == null || japan == null || camera == null)
            {
                Fail("Gallery picture interaction references are incomplete.");
                return;
            }

            Bounds pictureBounds = japan.GetFocusBounds();
            Vector3 viewingDirection = camera.transform.position - pictureBounds.center;
            if (viewingDirection.sqrMagnitude < 0.01f) viewingDirection = Vector3.forward;
            camera.transform.SetPositionAndRotation(pictureBounds.center + viewingDirection.normalized * 10f,
                Quaternion.LookRotation(-viewingDirection.normalized, Vector3.up));
            Physics.SyncTransforms();
            MethodInfo resolver = typeof(GalleryExhibitInteractor).GetMethod("FindAimedExhibit", BindingFlags.Instance | BindingFlags.NonPublic);
            GalleryExhibit resolved = resolver?.Invoke(interactor, null) as GalleryExhibit;
            if (resolved != japan)
            {
                RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, 30f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                string details = string.Join(", ", hits.OrderBy(hit => hit.distance)
                    .Select(hit => $"{hit.collider.name}@{hit.distance:0.000}"));
                Fail("A camera-centre ray from 10 units away did not resolve the aimed picture MeshCollider. Hits: " + details);
                return;
            }

            if (!interactor.Interact(japan))
            {
                Fail("Could not begin the Japan picture dialogue.");
                return;
            }
            cameraStart = camera.transform.position;
            stage = 1;
            return;
        }

        if (stage == 1)
        {
            GalleryPictureViewer viewer = UnityEngine.Object.FindFirstObjectByType<GalleryPictureViewer>();
            GalleryPictureFocus focus = UnityEngine.Object.FindFirstObjectByType<GalleryPictureFocus>();
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            if (viewer == null || focus == null || player == null || !viewer.IsOpen || !focus.IsFocused) return;
            Camera camera = player.GetComponentInChildren<Camera>();
            if (camera == null || Vector3.Distance(cameraStart, camera.transform.position) < 0.1f) return;

            GalleryExhibit japan = FindExhibit("JapanPicture");
            for (int index = 0; index < japan.DialogueLines.Length; index++) viewer.Advance();
            stage = 2;
            return;
        }

        if (stage == 2)
        {
            GalleryPictureViewer viewer = UnityEngine.Object.FindFirstObjectByType<GalleryPictureViewer>();
            GalleryPictureFocus focus = UnityEngine.Object.FindFirstObjectByType<GalleryPictureFocus>();
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            if (viewer == null || focus == null || player == null || viewer.IsOpen || focus.IsFocused || !player.ControlsEnabled) return;
            GalleryExhibitInteractor interactor = player.GetComponent<GalleryExhibitInteractor>();
            GalleryExhibit quizExhibit = FindExhibit("QuizTrigger");
            if (interactor == null || quizExhibit == null || !interactor.Interact(quizExhibit))
            {
                Fail("Could not open the Tugu Negara quiz.");
                return;
            }
            stage = 3;
            return;
        }

        if (stage == 3)
        {
            GalleryQuizController quiz = UnityEngine.Object.FindFirstObjectByType<GalleryQuizController>();
            if (quiz == null || !quiz.IsOpen || quiz.QuestionCount != 16) return;
            for (int index = 0; index < quiz.QuestionCount; index++) quiz.SelectAnswer(0);
            if (!quiz.IsComplete)
            {
                Fail("The quiz did not reach its score result after 16 answers.");
                return;
            }
            quiz.Close();
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            GalleryExhibitInteractor interactor = player.GetComponent<GalleryExhibitInteractor>();
            GalleryExhibit creditPanel = FindExhibit("CreditPicture");
            if (creditPanel == null || !interactor.Interact(creditPanel))
            {
                Fail("Could not open the Gallery credit panel.");
                return;
            }
            stage = 4;
            return;
        }

        if (stage == 4)
        {
            GalleryCreditPanelController creditPanel = UnityEngine.Object.FindFirstObjectByType<GalleryCreditPanelController>();
            if (creditPanel == null || !creditPanel.IsOpen || !Cursor.visible) return;
            creditPanel.Close();
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            GalleryExhibitInteractor interactor = player.GetComponent<GalleryExhibitInteractor>();
            GalleryExhibit credits = FindExhibit("CreditVideoPicture");
            if (interactor == null || credits == null || !interactor.Interact(credits))
            {
                Fail("Could not start Gallery credit replay.");
                return;
            }
            stage = 5;
            return;
        }

        if (stage == 5)
        {
            if (SceneManager.GetActiveScene().name != "Scene_Credits") return;
            CreditVideoController controller = UnityEngine.Object.FindFirstObjectByType<CreditVideoController>();
            if (controller == null || !controller.AcceptsSkipInput) return;
            if (controller.ShowsCompletionMessage)
            {
                Fail("Gallery replay incorrectly enabled the completion thank-you message.");
                return;
            }
            VideoPlayer player = controller.GetComponent<VideoPlayer>();
            if (player == null || !player.isPlaying) return;
            MethodInfo finish = typeof(CreditVideoController).GetMethod("BeginFinish", BindingFlags.Instance | BindingFlags.NonPublic);
            finish?.Invoke(controller, null);
            stage = 6;
            deadline = EditorApplication.timeSinceStartup + 8d;
            return;
        }

        if (stage == 6 && SceneManager.GetActiveScene().name == "Scene_Gallery")
        {
            GalleryFirstPersonController player = UnityEngine.Object.FindFirstObjectByType<GalleryFirstPersonController>();
            GalleryScreenFader fader = UnityEngine.Object.FindFirstObjectByType<GalleryScreenFader>();
            if (player == null || fader == null || fader.IsTransitioning) return;
            if (HorizontalDistance(player.transform.position, player.VideoSpawnPoint.position) > 0.05f)
            {
                Fail("Credit replay did not return the player to VideoSpawnPoint.");
                return;
            }
            Debug.Log("GALLERY_EXPERIENCE_PLAYMODE_OK: picture dialogue, 16-question quiz/result/quit, Gallery credit panel, credit replay, and both spawn routes passed.");
            Complete("PASS");
        }
    }

    private static GalleryExhibit FindExhibit(string objectName)
    {
        foreach (GalleryExhibit exhibit in UnityEngine.Object.FindObjectsByType<GalleryExhibit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (exhibit.name == objectName) return exhibit;
        return null;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }

    private static void Fail(string message)
    {
        Debug.LogError("GALLERY_EXPERIENCE_PLAYMODE_FAILED: " + message);
        Complete("FAIL");
    }

    private static void Complete(string result)
    {
        EditorApplication.update -= Tick;
        SessionState.SetString(ResultKey, result);
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result)) Finish(result == "PASS");
    }

    private static void Finish(bool passed)
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        if (!passed) throw new InvalidOperationException("Gallery experience Play Mode smoke test failed.");
    }
}
