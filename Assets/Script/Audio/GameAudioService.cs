using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderOfIndependence.Audio
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameAudioService : MonoBehaviour
    {
        private const string MusicVolumeKey = "DefenderOfIndependence.Audio.MusicVolume";
        private const string EffectsVolumeKey = "DefenderOfIndependence.Audio.EffectsVolume";

        [SerializeField] private GameAudioLibrary library;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource effectsSource;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultEffectsVolume = 0.9f;

        private readonly HashSet<Button> wiredButtons = new();
        private bool musicIsPaused;

        public static GameAudioService Instance { get; private set; }
        public static bool HasInstance => Instance != null;
        public static event Action<float, float> VolumesChanged;

        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public GameAudioLibrary Library => library;
        public AudioClip CurrentMusic => musicSource != null ? musicSource.clip : null;
        public bool IsMusicPaused => musicIsPaused;
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
        public bool IsEffectPlaying => effectsSource != null && effectsSource.isPlaying;
        public float MusicPlaybackTime => musicSource != null ? musicSource.time : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
            EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, defaultEffectsVolume));
            ConfigureSources();
        }

        private void OnEnable()
        {
            if (Instance == this) SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (Instance == this) HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetEffectsVolume(float volume)
        {
            EffectsVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(EffectsVolumeKey, EffectsVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void PauseMusic()
        {
            if (musicSource == null || musicIsPaused) return;
            musicIsPaused = true;
            if (musicSource.isPlaying) musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource == null || !musicIsPaused) return;
            musicIsPaused = false;
            if (musicSource.clip != null) musicSource.UnPause();
        }

        public void StartLevelThreeMusic()
        {
            if (library != null) PlayMusic(library.LevelThreeMusic, false);
        }

        public void RestoreCurrentSceneMusic()
        {
            AudioClip clip = SceneManager.GetActiveScene().name == "Scene_Level3"
                ? library != null ? library.LevelThreeMusic : null
                : library != null ? library.GetAutomaticSceneMusic(SceneManager.GetActiveScene().name) : null;
            PlayMusic(clip, true);
        }

        public void PlayLossMusic()
        {
            if (library != null) PlayMusic(library.LoseMusic, true);
        }

        public void StopMusic()
        {
            musicIsPaused = false;
            if (musicSource == null) return;
            musicSource.Stop();
            musicSource.clip = null;
        }

        public void PlayButtonClick() => PlayEffect(library != null ? library.ButtonClick : null);
        public void PlayNextDialogue() => PlayEffect(library != null ? library.NextDialogue : null);
        public void PlayKeyboardTap() => PlayEffect(library != null ? library.KeyboardTap : null);
        public void PlayCheering() => PlayEffect(library != null ? library.Cheering : null);
        public void PlayMerdeka() => PlayEffect(library != null ? library.Merdeka : null);
        public void PlayGunshot() => PlayEffect(library != null ? library.Gunshot : null);

        public void PlayEffect(AudioClip clip, float volumeScale = 1f)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            musicIsPaused = false;
            // One-shots belong to the scene that triggered them. In particular,
            // the Level 3 opening cheer must never carry into the Credits scene.
            if (effectsSource != null) effectsSource.Stop();
            PlayMusic(library != null ? library.GetAutomaticSceneMusic(scene.name) : null, false);
            WireSceneButtons(scene);
        }

        private void PlayMusic(AudioClip clip, bool restartEvenIfSame)
        {
            if (musicSource == null) return;
            if (!restartEvenIfSame && musicSource.clip == clip)
            {
                if (clip != null && !musicSource.isPlaying && !musicIsPaused) musicSource.Play();
                return;
            }
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.time = 0f;
            if (clip != null && !musicIsPaused) musicSource.Play();
        }

        private void WireSceneButtons(Scene scene)
        {
            wiredButtons.RemoveWhere(button => button == null);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveListener(PlayButtonClick);
                button.onClick.AddListener(PlayButtonClick);
                wiredButtons.Add(button);
            }
        }

        private void ConfigureSources()
        {
            if (musicSource != null)
            {
                musicSource.playOnAwake = false; musicSource.loop = true; musicSource.spatialBlend = 0f;
            }
            if (effectsSource != null)
            {
                effectsSource.playOnAwake = false; effectsSource.loop = false; effectsSource.spatialBlend = 0f;
            }
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = MusicVolume;
            if (effectsSource != null) effectsSource.volume = EffectsVolume;
            VolumesChanged?.Invoke(MusicVolume, EffectsVolume);
        }
    }
}
