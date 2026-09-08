using UnityEngine;

namespace DefenderOfIndependence.Level2
{
    public sealed class LevelTwoScheduledCharacter : MonoBehaviour
    {
        [SerializeField, Range(1, 6)] private int activeDay = 1;
        [SerializeField] private GameObject characterVisual;

        public int ActiveDay => activeDay;
        public GameObject CharacterVisual => characterVisual;

        public void ApplyDay(int day)
        {
            if (characterVisual != null)
            {
                characterVisual.SetActive(day == activeDay);
            }
        }
    }
}
