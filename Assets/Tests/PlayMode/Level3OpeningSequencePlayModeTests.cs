using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Defender.Level3.Tests
{
    public sealed class Level3OpeningSequencePlayModeTests
    {
        [UnityTest]
        public IEnumerator OpeningSequence_PlaysThreeShotsAndHoldsCameraThree()
        {
            SceneManager.LoadScene("Scene_Level3", LoadSceneMode.Single);
            yield return null;

            Type sequenceType = Type.GetType("LevelThreeOpeningSequence, Assembly-CSharp");
            Assert.That(sequenceType, Is.Not.Null, "LevelThreeOpeningSequence type was not compiled.");
            Component sequence = UnityEngine.Object.FindFirstObjectByType(sequenceType) as Component;
            Assert.That(sequence, Is.Not.Null, "Scene_Level3 has no opening sequence component.");

            Camera camera1 = FindSceneCamera("Camera1");
            Camera camera2 = FindSceneCamera("Camera2");
            Camera camera3 = FindSceneCamera("Camera3");
            GameObject tunku = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == "Tunku Abdul Rahman")?.gameObject;
            Assert.That(tunku, Is.Not.Null, "Tunku Abdul Rahman was not found.");
            Animator tunkuAnimator = tunku.GetComponent<Animator>();
            Assert.That(tunkuAnimator, Is.Not.Null);
            Vector3 camera1StartPosition = camera1.transform.position;

            Type tutorialType = Type.GetType("LevelThreeTutorialPanel, Assembly-CSharp");
            Assert.That(tutorialType, Is.Not.Null, "LevelThreeTutorialPanel type was not compiled.");
            Component tutorial = UnityEngine.Object.FindFirstObjectByType(tutorialType) as Component;
            Assert.That(tutorial, Is.Not.Null, "Scene_Level3 has no tutorial panel component.");
            tutorialType.GetMethod("SetTutorialTiming", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 0.05f, 0.25f, 0.05f, 0.02f });
            tutorialType.GetMethod("SetIncidentSpawnDelay", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 0.02f, 0.03f });

            sequenceType.GetMethod("SetPlaybackSpeed", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(sequence, new object[] { 20f });

            PropertyInfo activeShotProperty = sequenceType.GetProperty("ActiveShot");
            PropertyInfo transitioningProperty = sequenceType.GetProperty("IsTransitioning");
            PropertyInfo completeProperty = sequenceType.GetProperty("IsComplete");
            PropertyInfo fadeAlphaProperty = sequenceType.GetProperty("FadeAlpha");
            bool sawShot2 = false;
            bool sawShot2MovingUnderFade = false;
            Vector3 camera2ActivationPosition = Vector3.zero;
            Vector3 tunkuActivationPosition = Vector3.zero;
            float camera2MotionUnderFade = 0f;
            float tunkuMotionUnderFade = 0f;
            bool sawOpaqueTransition = false;
            bool sawCamera1Transition = false;
            Vector3 frozenCamera1Position = Vector3.zero;
            float timeout = Time.realtimeSinceStartup + 15f;

            while (!(bool)completeProperty.GetValue(sequence))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Opening sequence timed out.");
                int activeShot = (int)activeShotProperty.GetValue(sequence);
                bool transitioning = (bool)transitioningProperty.GetValue(sequence);
                float fadeAlpha = (float)fadeAlphaProperty.GetValue(sequence);
                if (transitioning && fadeAlpha > 0.8f) sawOpaqueTransition = true;

                if (transitioning && activeShot == 1)
                {
                    if (!sawCamera1Transition)
                    {
                        sawCamera1Transition = true;
                        frozenCamera1Position = camera1.transform.position;
                        Assert.That(Vector3.Distance(frozenCamera1Position, camera1StartPosition), Is.GreaterThan(0.01f),
                            "Camera 1 wrapped to its first frame before the black camera swap.");
                    }
                    else
                    {
                        Assert.That(Vector3.Distance(camera1.transform.position, frozenCamera1Position), Is.LessThan(0.0001f),
                            "Camera 1 moved or reset while fading to Camera 2.");
                    }
                }

                if (activeShot == 2)
                {
                    if (!sawShot2)
                    {
                        Assert.That(fadeAlpha, Is.GreaterThanOrEqualTo(0.99f),
                            "Camera 2 became active before the screen was fully black.");
                        camera2ActivationPosition = camera2.transform.position;
                        tunkuActivationPosition = tunku.transform.position;
                    }
                    sawShot2 = true;
                    AssertOnlyCameraActive(camera2, camera1, camera2, camera3);
                    if (transitioning && fadeAlpha < 0.8f && fadeAlpha > 0.1f)
                    {
                        sawShot2MovingUnderFade = true;
                        camera2MotionUnderFade = Mathf.Max(camera2MotionUnderFade,
                            Vector3.Distance(camera2.transform.position, camera2ActivationPosition));
                        tunkuMotionUnderFade = Mathf.Max(tunkuMotionUnderFade,
                            Vector3.Distance(tunku.transform.position, tunkuActivationPosition));
                    }
                }
                yield return null;
            }

            Assert.That(sawShot2, Is.True, "Camera 2 was never active.");
            Assert.That(sawShot2MovingUnderFade, Is.True, "Camera 2 fade-in motion was not observed.");
            Assert.That(camera2MotionUnderFade, Is.GreaterThan(0.001f),
                "Camera 2 did not move while the black overlay was becoming transparent.");
            Assert.That(tunkuMotionUnderFade, Is.GreaterThan(0.001f),
                "Tunku did not move while the black overlay was becoming transparent.");
            Assert.That(sawCamera1Transition, Is.True, "The Camera 1 to Camera 2 transition was not observed.");
            Assert.That(sawOpaqueTransition, Is.True, "An opaque black transition was not observed.");
            AssertOnlyCameraActive(camera3, camera1, camera2, camera3);
            Assert.That(tunkuAnimator.speed, Is.EqualTo(0f), "Tunku animation continued beyond Camera 2.");
            Assert.That((float)fadeAlphaProperty.GetValue(sequence), Is.LessThanOrEqualTo(0.01f));

            yield return new WaitForSecondsRealtime(0.5f);
            AssertOnlyCameraActive(camera3, camera1, camera2, camera3);

            PropertyInfo visibleProperty = tutorialType.GetProperty("IsVisible");
            PropertyInfo panelAlphaProperty = tutorialType.GetProperty("PanelAlpha");
            PropertyInfo currentLineProperty = tutorialType.GetProperty("CurrentLineIndex");
            PropertyInfo timerProperty = tutorialType.GetProperty("MainTimerRemaining");
            PropertyInfo incidentTimerProperty = tutorialType.GetProperty("IncidentTimerRemaining");
            PropertyInfo incidentVisibleProperty = tutorialType.GetProperty("IncidentPanelVisible");
            PropertyInfo incidentResolvedProperty = tutorialType.GetProperty("IncidentResolved");
            PropertyInfo mainTypedProperty = tutorialType.GetProperty("MainTypedCharacterCount");
            PropertyInfo incidentTypedProperty = tutorialType.GetProperty("IncidentTypedCharacterCount");
            PropertyInfo tutorialFinishedProperty = tutorialType.GetProperty("IsFinished");
            Component healthDisplay = GetPrivateField<Component>(tutorialType, tutorial, "healthDisplay");
            PropertyInfo healthVisibleProperty = healthDisplay.GetType().GetProperty("IsVisible");
            Assert.That((bool)healthVisibleProperty.GetValue(healthDisplay), Is.False,
                "Health was visible before its tutorial introduction.");
            float uiDeadline = Time.realtimeSinceStartup + 3f;
            while (!(bool)visibleProperty.GetValue(tutorial) || (float)panelAlphaProperty.GetValue(tutorial) < 0.99f ||
                   (int)currentLineProperty.GetValue(tutorial) < 0)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(uiDeadline), "Tutorial UI did not fade in after Camera 3.");
                yield return null;
            }

            Slider leftSlider = GetPrivateField<Slider>(tutorialType, tutorial, "leftTimerSlider");
            Slider rightSlider = GetPrivateField<Slider>(tutorialType, tutorial, "rightTimerSlider");
            float initialTimer = (float)timerProperty.GetValue(tutorial);
            yield return new WaitForSecondsRealtime(0.05f);
            float reducedTimer = (float)timerProperty.GetValue(tutorial);
            Assert.That(reducedTimer, Is.LessThan(initialTimer), "Tutorial timer did not count down.");
            Assert.That(leftSlider.value, Is.EqualTo(reducedTimer).Within(0.001f));
            Assert.That(rightSlider.value, Is.EqualTo(reducedTimer).Within(0.001f));

            yield return WaitForLine(tutorial, currentLineProperty, 5, 4f);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That((int)currentLineProperty.GetValue(tutorial), Is.EqualTo(5),
                "A required typing page advanced before its grey phrase was entered.");
            SubmitText(tutorialType, tutorial, "this word.");
            Assert.That((int)mainTypedProperty.GetValue(tutorial), Is.EqualTo("this word.".Length));
            int typedLine = (int)currentLineProperty.GetValue(tutorial);
            yield return null;
            Assert.That((int)currentLineProperty.GetValue(tutorial), Is.EqualTo(typedLine),
                "A completed phrase advanced immediately instead of waiting for the timer.");
            yield return WaitForLine(tutorial, currentLineProperty, 6, 2f);

            yield return WaitForLine(tutorial, currentLineProperty, 9, 3f);
            float incidentSpawnDeadline = Time.realtimeSinceStartup + 1f;
            while (!(bool)incidentVisibleProperty.GetValue(tutorial))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(incidentSpawnDeadline),
                    "Incident tutorial panel did not appear after its independent delay.");
                yield return null;
            }
            Slider incidentLeftSlider = GetPrivateField<Slider>(tutorialType, tutorial, "incidentLeftTimerSlider");
            Slider incidentRightSlider = GetPrivateField<Slider>(tutorialType, tutorial, "incidentRightTimerSlider");
            float incidentTimerBefore = (float)incidentTimerProperty.GetValue(tutorial);
            yield return new WaitForSecondsRealtime(0.03f);
            float incidentTimerAfter = (float)incidentTimerProperty.GetValue(tutorial);
            Assert.That(incidentTimerAfter, Is.LessThan(incidentTimerBefore), "Incident timer did not count down.");
            Assert.That(incidentLeftSlider.value, Is.EqualTo(incidentTimerAfter).Within(0.001f));
            Assert.That(incidentRightSlider.value, Is.EqualTo(incidentTimerAfter).Within(0.001f));
            tutorialType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tutorial, null);
            SubmitText(tutorialType, tutorial, "lef");
            Assert.That((int)incidentTypedProperty.GetValue(tutorial), Is.EqualTo(3));
            tutorialType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tutorial, null);
            Assert.That((int)incidentTypedProperty.GetValue(tutorial), Is.EqualTo(0),
                "Switching away did not reset the incident panel's partial typing progress.");
            tutorialType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tutorial, null);
            SubmitText(tutorialType, tutorial, "left tab");
            Assert.That((bool)incidentResolvedProperty.GetValue(tutorial), Is.True,
                "A completed incident did not resolve immediately.");
            Assert.That((bool)incidentVisibleProperty.GetValue(tutorial), Is.False,
                "A completed incident remained visible until its timer expired.");
            CanvasGroup incidentGroup = GetPrivateField<CanvasGroup>(tutorialType, tutorial, "incidentPanelGroup");
            Assert.That(incidentGroup.gameObject.activeSelf, Is.False,
                "The completed incident GameObject was not deactivated.");
            yield return WaitForLine(tutorial, currentLineProperty, 10, 2f);
            Assert.That((bool)healthVisibleProperty.GetValue(healthDisplay), Is.False,
                "Health appeared before the health tutorial page.");
            yield return WaitForLine(tutorial, currentLineProperty, 11, 2f);
            float healthDeadline = Time.realtimeSinceStartup + 1f;
            while (!(bool)healthVisibleProperty.GetValue(healthDisplay))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(healthDeadline),
                    "Health did not fade in when the tutorial introduced the system.");
                yield return null;
            }

            yield return WaitForLine(tutorial, currentLineProperty, 13, 2f);
            SubmitText(tutorialType, tutorial, "I'm ready!");
            float handoffDeadline = Time.realtimeSinceStartup + 2f;
            while (!(bool)tutorialFinishedProperty.GetValue(tutorial))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(handoffDeadline),
                    "The completed tutorial did not hand off to main gameplay.");
                yield return null;
            }
            Type gameplayType = Type.GetType("LevelThreeTypingGameplay, Assembly-CSharp");
            Component gameplay = UnityEngine.Object.FindFirstObjectByType(gameplayType) as Component;
            Assert.That((bool)gameplayType.GetProperty("IsRunning").GetValue(gameplay), Is.True,
                "Main Level 3 gameplay was not started by the tutorial completion event.");
        }

        [UnityTest]
        public IEnumerator MainGameplay_CompletesIncidentsImmediatelyAndFailsAfterThreeMisses()
        {
            SceneManager.LoadScene("Scene_Level3", LoadSceneMode.Single);
            yield return null;

            Type sequenceType = Type.GetType("LevelThreeOpeningSequence, Assembly-CSharp");
            Component sequence = UnityEngine.Object.FindFirstObjectByType(sequenceType) as Component;
            if (sequence is Behaviour sequenceBehaviour) sequenceBehaviour.enabled = false;

            Type gameplayType = Type.GetType("LevelThreeTypingGameplay, Assembly-CSharp");
            Assert.That(gameplayType, Is.Not.Null, "LevelThreeTypingGameplay type was not compiled.");
            Component gameplay = UnityEngine.Object.FindFirstObjectByType(gameplayType) as Component;
            Assert.That(gameplay, Is.Not.Null, "Scene_Level3 has no main typing gameplay component.");
            List<float> authoredDurations = GetPrivateField<List<float>>(gameplayType, gameplay, "mainLineDurations");
            Assert.That(authoredDurations.Count, Is.EqualTo(21));
            Assert.That(authoredDurations[0], Is.Not.EqualTo(authoredDurations[7]),
                "The saved scene does not contain varied Inspector timing for short and long speech lines.");
            gameplayType.GetMethod("SetTiming", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(gameplay, new object[] { 0.12f, 2f, 0.02f, 0.03f });
            CanvasGroup restartFade = GetPrivateField<CanvasGroup>(gameplayType, gameplay, "restartFadeOverlay");
            restartFade.alpha = 0f;
            gameplayType.GetMethod("BeginGameplay", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);

            PropertyInfo lineProperty = gameplayType.GetProperty("CurrentMainLineIndex");
            PropertyInfo incidentVisibleProperty = gameplayType.GetProperty("IncidentIsVisible");
            PropertyInfo incidentTypedProperty = gameplayType.GetProperty("IncidentTypedCharacterCount");
            PropertyInfo missCountProperty = gameplayType.GetProperty("MissCount");
            PropertyInfo failedProperty = gameplayType.GetProperty("IsFailed");

            SubmitText(gameplayType, gameplay, "nation");
            yield return WaitForLine(gameplay, lineProperty, 1, 1f);
            Assert.That((int)missCountProperty.GetValue(gameplay), Is.EqualTo(0));
            SubmitText(gameplayType, gameplay, "independent");
            yield return WaitForLine(gameplay, lineProperty, 2, 1f);

            float incidentDeadline = Time.realtimeSinceStartup + 1f;
            while (!(bool)incidentVisibleProperty.GetValue(gameplay))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(incidentDeadline),
                    "The first gameplay incident did not appear after its random delay.");
                yield return null;
            }

            RectTransform incidentRect = GetPrivateField<RectTransform>(gameplayType, gameplay, "incidentPanelRect");
            AssertRectContainedByParent(incidentRect);
            TMP_Text incidentMessage = GetPrivateField<TMP_Text>(gameplayType, gameplay, "incidentMessageText");
            StringAssert.Contains("\n\n", incidentMessage.text,
                "The incident prompt and required words were not separated by an empty line.");

            gameplayType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);
            SubmitText(gameplayType, gameplay, "rai");
            Assert.That((int)incidentTypedProperty.GetValue(gameplay), Is.EqualTo(3));
            gameplayType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);
            Assert.That((int)incidentTypedProperty.GetValue(gameplay), Is.EqualTo(0),
                "Switching away did not reset partial incident typing.");
            gameplayType.GetMethod("ToggleSelectedPanel", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);
            SubmitText(gameplayType, gameplay, "raise volume");
            Assert.That((bool)incidentVisibleProperty.GetValue(gameplay), Is.False,
                "A completed gameplay incident remained visible until its timer expired.");
            Assert.That(incidentRect.gameObject.activeSelf, Is.False,
                "A completed gameplay incident GameObject was not deactivated.");
            Assert.That((int)missCountProperty.GetValue(gameplay), Is.EqualTo(0));

            float failureDeadline = Time.realtimeSinceStartup + 1.5f;
            while (!(bool)failedProperty.GetValue(gameplay))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(failureDeadline),
                    "Gameplay did not fail after three missed main-panel entries.");
                yield return null;
            }
            Assert.That((int)missCountProperty.GetValue(gameplay), Is.EqualTo(3));
            CanvasGroup failureGroup = GetPrivateField<CanvasGroup>(gameplayType, gameplay, "failurePanelGroup");
            Assert.That(failureGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            Component health = GetPrivateField<Component>(gameplayType, gameplay, "healthDisplay");
            Slider healthSlider = GetPrivateField<Slider>(health.GetType(), health, "healthSlider");
            Assert.That(healthSlider.value, Is.EqualTo(0f).Within(0.001f),
                "The health slider did not synchronize with the zero-health failure state.");

            var sceneHandleBeforeRetry = SceneManager.GetActiveScene().handle;
            Component tutorial = GetPrivateField<Component>(gameplayType, gameplay, "tutorialPanel");
            Type tutorialType = tutorial.GetType();
            tutorialType.GetMethod("SetTutorialTiming", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 0f, 0.12f, 0f, 0f });
            gameplayType.GetMethod("SetRestartFadeDuration", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(gameplay, new object[] { 0.05f });
            gameplayType.GetMethod("RetryFromFailure", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);

            PropertyInfo restartProperty = gameplayType.GetProperty("IsRestartTransitioning");
            bool sawOpaqueRetryFade = false;
            float retryDeadline = Time.realtimeSinceStartup + 2f;
            while ((bool)restartProperty.GetValue(gameplay))
            {
                sawOpaqueRetryFade |= restartFade.alpha >= 0.99f;
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(retryDeadline), "Retry fade did not finish.");
                yield return null;
            }

            Assert.That(sawOpaqueRetryFade, Is.True, "Retry never reached a fully black transition frame.");
            Assert.That(restartFade.alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandleBeforeRetry),
                "Retry reloaded the scene instead of returning within the current Level 3 run.");
            Assert.That((int)tutorialType.GetProperty("CurrentLineIndex").GetValue(tutorial), Is.EqualTo(13));
            Assert.That((bool)tutorialType.GetProperty("IsFinished").GetValue(tutorial), Is.False);
            Assert.That((bool)tutorialType.GetProperty("IsInteractionPaused").GetValue(tutorial), Is.False);
            Assert.That(failureGroup.alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(healthSlider.value, Is.EqualTo(3f).Within(0.001f));

            SubmitText(tutorialType, tutorial, "I'm ready!");
            float resumedDeadline = Time.realtimeSinceStartup + 1f;
            while (!(bool)gameplayType.GetProperty("IsRunning").GetValue(gameplay))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(resumedDeadline),
                    "Completing the retry ready-check did not restart gameplay.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Tutorial_HoldSkipJumpsToFinalReadyCheckAndHidesPrompt()
        {
            SceneManager.LoadScene("Scene_Level3", LoadSceneMode.Single);
            yield return null;

            Type sequenceType = Type.GetType("LevelThreeOpeningSequence, Assembly-CSharp");
            Component sequence = UnityEngine.Object.FindFirstObjectByType(sequenceType) as Component;
            if (sequence is Behaviour sequenceBehaviour) sequenceBehaviour.enabled = false;

            Type tutorialType = Type.GetType("LevelThreeTutorialPanel, Assembly-CSharp");
            Component tutorial = UnityEngine.Object.FindFirstObjectByType(tutorialType) as Component;
            Assert.That(tutorial, Is.Not.Null);
            tutorialType.GetMethod("SetTutorialTiming", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 0f, 1f, 0f, 0f });
            tutorialType.GetMethod("SetSkipHoldDuration", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 1f });
            tutorialType.GetMethod("ShowTutorial", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tutorial, null);
            yield return null;

            PropertyInfo lineProperty = tutorialType.GetProperty("CurrentLineIndex");
            Assert.That((int)lineProperty.GetValue(tutorial), Is.EqualTo(0));
            CanvasGroup skipGroup = GetPrivateField<CanvasGroup>(tutorialType, tutorial, "skipPromptGroup");
            Slider skipSlider = GetPrivateField<Slider>(tutorialType, tutorial, "skipHoldSlider");
            Assert.That(skipGroup.gameObject.activeSelf, Is.True);

            tutorialType.GetMethod("SubmitSkipHold", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 0.4f });
            Assert.That(skipSlider.value, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That((int)lineProperty.GetValue(tutorial), Is.EqualTo(0));
            tutorialType.GetMethod("CancelSkipHold", BindingFlags.Instance | BindingFlags.Public)?.Invoke(tutorial, null);
            Assert.That(skipSlider.value, Is.EqualTo(0f).Within(0.001f));

            tutorialType.GetMethod("SubmitSkipHold", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(tutorial, new object[] { 1f });
            Assert.That((int)lineProperty.GetValue(tutorial), Is.EqualTo(13));
            Assert.That(skipGroup.gameObject.activeSelf, Is.False,
                "Skip prompt remained active on the final tutorial line.");
        }

        [UnityTest]
        public IEnumerator MainGameplay_MissedIncidentAlsoRemovesHealth()
        {
            SceneManager.LoadScene("Scene_Level3", LoadSceneMode.Single);
            yield return null;

            Type sequenceType = Type.GetType("LevelThreeOpeningSequence, Assembly-CSharp");
            Component sequence = UnityEngine.Object.FindFirstObjectByType(sequenceType) as Component;
            if (sequence is Behaviour sequenceBehaviour) sequenceBehaviour.enabled = false;

            Type gameplayType = Type.GetType("LevelThreeTypingGameplay, Assembly-CSharp");
            Component gameplay = UnityEngine.Object.FindFirstObjectByType(gameplayType) as Component;
            Assert.That(gameplay, Is.Not.Null);
            gameplayType.GetMethod("SetTiming", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(gameplay, new object[] { 5f, 0.1f, 0f, 0f });
            gameplayType.GetMethod("BeginGameplay", BindingFlags.Instance | BindingFlags.Public)?.Invoke(gameplay, null);
            gameplayType.GetMethod("ShowIncident", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(gameplay, new object[] { 0 });

            PropertyInfo incidentVisibleProperty = gameplayType.GetProperty("IncidentIsVisible");
            PropertyInfo missCountProperty = gameplayType.GetProperty("MissCount");
            float deadline = Time.realtimeSinceStartup + 1f;
            while ((bool)incidentVisibleProperty.GetValue(gameplay))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "A missed incident did not close when its timer expired.");
                yield return null;
            }

            Assert.That((int)missCountProperty.GetValue(gameplay), Is.EqualTo(1),
                "A missed incident did not remove one shared health point.");
            Component health = GetPrivateField<Component>(gameplayType, gameplay, "healthDisplay");
            Slider healthSlider = GetPrivateField<Slider>(health.GetType(), health, "healthSlider");
            Assert.That(healthSlider.value, Is.EqualTo(2f).Within(0.001f));
        }

        private static Camera FindSceneCamera(string name)
        {
            Camera camera = Resources.FindObjectsOfTypeAll<Camera>()
                .FirstOrDefault(item => item.gameObject.scene == SceneManager.GetActiveScene() && item.name == name);
            Assert.That(camera, Is.Not.Null, $"{name} was not found.");
            return camera;
        }

        private static void AssertOnlyCameraActive(Camera expected, params Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                bool shouldBeActive = camera == expected;
                Assert.That(camera.gameObject.activeInHierarchy && camera.enabled, Is.EqualTo(shouldBeActive),
                    $"Unexpected active state on {camera.name}.");
            }
        }

        private static T GetPrivateField<T>(Type type, object target, string fieldName) where T : class
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            T value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field was not assigned: {fieldName}");
            return value;
        }

        private static void SubmitText(Type tutorialType, Component tutorial, string text)
        {
            MethodInfo submit = tutorialType.GetMethod("SubmitCharacter", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(submit, Is.Not.Null);
            foreach (char character in text) submit.Invoke(tutorial, new object[] { character });
        }

        private static void AssertRectContainedByParent(RectTransform rect)
        {
            Assert.That(rect.parent, Is.TypeOf<RectTransform>());
            RectTransform parent = (RectTransform)rect.parent;
            Vector3[] panelCorners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            rect.GetWorldCorners(panelCorners);
            parent.GetWorldCorners(parentCorners);
            const float tolerance = 0.1f;
            Assert.That(panelCorners[0].x, Is.GreaterThanOrEqualTo(parentCorners[0].x - tolerance));
            Assert.That(panelCorners[0].y, Is.GreaterThanOrEqualTo(parentCorners[0].y - tolerance));
            Assert.That(panelCorners[2].x, Is.LessThanOrEqualTo(parentCorners[2].x + tolerance));
            Assert.That(panelCorners[2].y, Is.LessThanOrEqualTo(parentCorners[2].y + tolerance));
        }

        private static IEnumerator WaitForLine(Component tutorial, PropertyInfo currentLineProperty, int expectedLine, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while ((int)currentLineProperty.GetValue(tutorial) < expectedLine)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), $"Tutorial did not reach line {expectedLine}.");
                yield return null;
            }
            Assert.That((int)currentLineProperty.GetValue(tutorial), Is.EqualTo(expectedLine));
        }
    }
}
