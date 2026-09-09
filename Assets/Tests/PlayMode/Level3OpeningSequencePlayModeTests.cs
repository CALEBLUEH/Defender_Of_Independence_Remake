using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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

            sequenceType.GetMethod("SetPlaybackSpeed", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(sequence, new object[] { 20f });

            PropertyInfo activeShotProperty = sequenceType.GetProperty("ActiveShot");
            PropertyInfo transitioningProperty = sequenceType.GetProperty("IsTransitioning");
            PropertyInfo completeProperty = sequenceType.GetProperty("IsComplete");
            PropertyInfo fadeAlphaProperty = sequenceType.GetProperty("FadeAlpha");
            bool sawShot2 = false;
            bool sawOpaqueTransition = false;
            float timeout = Time.realtimeSinceStartup + 15f;

            while (!(bool)completeProperty.GetValue(sequence))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Opening sequence timed out.");
                int activeShot = (int)activeShotProperty.GetValue(sequence);
                bool transitioning = (bool)transitioningProperty.GetValue(sequence);
                float fadeAlpha = (float)fadeAlphaProperty.GetValue(sequence);
                if (transitioning && fadeAlpha > 0.8f) sawOpaqueTransition = true;

                if (activeShot == 2)
                {
                    sawShot2 = true;
                    AssertOnlyCameraActive(camera2, camera1, camera2, camera3);
                    if (!transitioning) Assert.That(tunkuAnimator.speed, Is.GreaterThan(0f));
                }
                yield return null;
            }

            Assert.That(sawShot2, Is.True, "Camera 2 was never active.");
            Assert.That(sawOpaqueTransition, Is.True, "An opaque black transition was not observed.");
            AssertOnlyCameraActive(camera3, camera1, camera2, camera3);
            Assert.That(tunkuAnimator.speed, Is.EqualTo(0f), "Tunku animation continued beyond Camera 2.");
            Assert.That((float)fadeAlphaProperty.GetValue(sequence), Is.LessThanOrEqualTo(0.01f));

            yield return new WaitForSecondsRealtime(0.5f);
            AssertOnlyCameraActive(camera3, camera1, camera2, camera3);
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
    }
}
