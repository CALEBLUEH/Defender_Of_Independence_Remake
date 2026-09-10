using UnityEngine;

namespace DefenderOfIndependence.SceneFlow
{
    public static class GameProgressionState
    {
        private const string PostGameKey = "DefenderOfIndependence.Progression.PostGame";

        public static bool IsPostGame => PlayerPrefs.GetInt(PostGameKey, 0) == 1;

        public static void UnlockPostGame()
        {
            PlayerPrefs.SetInt(PostGameKey, 1);
            PlayerPrefs.Save();
        }

        public static void ResetToInitialState()
        {
            PlayerPrefs.DeleteKey(PostGameKey);
            PlayerPrefs.Save();
        }
    }
}
