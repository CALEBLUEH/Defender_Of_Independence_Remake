using Fungus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefenderOfIndependence.Cutscenes
{
    [CommandInfo("Flow", "Load Scene Direct", "Loads a Build Settings scene immediately without Fungus's legacy unused-asset cleanup pass.")]
    [AddComponentMenu("")]
    public sealed class LoadSceneDirect : Command
    {
        [SerializeField] private string targetScene = string.Empty;

        public override void OnEnter()
        {
            if (string.IsNullOrWhiteSpace(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError($"Cannot load scene '{targetScene}'. Make sure it is enabled in Build Settings.", this);
                Continue();
                return;
            }

            SceneManager.LoadScene(targetScene);
        }

        public override string GetSummary()
        {
            return string.IsNullOrWhiteSpace(targetScene) ? "Error: No scene selected" : targetScene;
        }
    }
}
