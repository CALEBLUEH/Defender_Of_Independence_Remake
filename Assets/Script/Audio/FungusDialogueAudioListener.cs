using Fungus;
using UnityEngine;

namespace DefenderOfIndependence.Audio
{
    public sealed class FungusDialogueAudioListener : MonoBehaviour, IWriterListener
    {
        public void OnStart(AudioClip _) => GameAudioService.Instance?.PlayNextDialogue();
        public void OnInput() { }
        public void OnPause() { }
        public void OnResume() { }
        public void OnEnd(bool _) { }
        public void OnAllWordsWritten() { }
        public void OnGlyph() { }
        public void OnVoiceover(AudioClip _) { }
    }
}
