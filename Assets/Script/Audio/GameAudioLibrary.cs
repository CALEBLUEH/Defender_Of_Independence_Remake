using UnityEngine;

namespace DefenderOfIndependence.Audio
{
    [CreateAssetMenu(menuName = "Defender of Independence/Audio Library", fileName = "GameAudioLibrary")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Header("Interface and Gameplay Effects")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip nextDialogue;
        [SerializeField] private AudioClip walking;
        [SerializeField] private AudioClip loseMusic;
        [SerializeField] private AudioClip keyboardTap;
        [SerializeField] private AudioClip cheering;
        [SerializeField] private AudioClip merdeka;
        [SerializeField] private AudioClip gunshot;

        [Header("Looping Scene Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip levelOneMusic;
        [SerializeField] private AudioClip levelTwoMusic;
        [SerializeField] private AudioClip levelThreeMusic;
        [SerializeField] private AudioClip galleryMusic;

        public AudioClip ButtonClick => buttonClick;
        public AudioClip NextDialogue => nextDialogue;
        public AudioClip Walking => walking;
        public AudioClip LoseMusic => loseMusic;
        public AudioClip KeyboardTap => keyboardTap;
        public AudioClip Cheering => cheering;
        public AudioClip Merdeka => merdeka;
        public AudioClip Gunshot => gunshot;
        public AudioClip LevelThreeMusic => levelThreeMusic;

        public AudioClip GetAutomaticSceneMusic(string sceneName)
        {
            return sceneName switch
            {
                "Scene_MainMenu" => mainMenuMusic,
                "Scene_Level1" => levelOneMusic,
                "Scene_Level2" => levelTwoMusic,
                "Scene_Gallery" => galleryMusic,
                _ => null
            };
        }
    }
}
