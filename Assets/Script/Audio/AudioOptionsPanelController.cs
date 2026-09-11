using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderOfIndependence.Audio
{
    public sealed class AudioOptionsPanelController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Slider soundEffectsSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text soundEffectsValue;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            ConfigureSlider(soundEffectsSlider); ConfigureSlider(musicSlider); SetVisible(false);
        }

        private void OnEnable()
        {
            soundEffectsSlider?.onValueChanged.AddListener(SetEffectsVolume);
            musicSlider?.onValueChanged.AddListener(SetMusicVolume);
            closeButton?.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            soundEffectsSlider?.onValueChanged.RemoveListener(SetEffectsVolume);
            musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            GameAudioService audio = GameAudioService.Instance;
            if (audio != null)
            {
                soundEffectsSlider?.SetValueWithoutNotify(audio.EffectsVolume);
                musicSlider?.SetValueWithoutNotify(audio.MusicVolume);
                RefreshLabels(audio.EffectsVolume, audio.MusicVolume);
            }
            IsOpen = true; SetVisible(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void Close() { IsOpen = false; SetVisible(false); }

        private void SetEffectsVolume(float value)
        {
            GameAudioService.Instance?.SetEffectsVolume(value);
            RefreshLabels(value, musicSlider != null ? musicSlider.value : 0f);
        }

        private void SetMusicVolume(float value)
        {
            GameAudioService.Instance?.SetMusicVolume(value);
            RefreshLabels(soundEffectsSlider != null ? soundEffectsSlider.value : 0f, value);
        }

        private void RefreshLabels(float effects, float music)
        {
            if (soundEffectsValue != null) soundEffectsValue.text = Mathf.RoundToInt(effects * 100f) + "%";
            if (musicValue != null) musicValue.text = Mathf.RoundToInt(music * 100f) + "%";
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null) return;
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.gameObject.SetActive(true); panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible; panelGroup.blocksRaycasts = visible;
        }
    }
}
