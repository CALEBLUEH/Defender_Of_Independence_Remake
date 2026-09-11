using UnityEngine;

namespace DefenderOfIndependence.Audio
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementLoopAudio : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip walkingClip;
        [SerializeField, Min(0f)] private float movementThreshold = 0.15f;

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (audioSource != null)
            {
                audioSource.clip = walkingClip; audioSource.loop = true; audioSource.playOnAwake = false; audioSource.spatialBlend = 0f;
            }
        }

        private void OnEnable()
        {
            GameAudioService.VolumesChanged += HandleVolumesChanged; RefreshVolume();
        }

        private void OnDisable()
        {
            GameAudioService.VolumesChanged -= HandleVolumesChanged; audioSource?.Stop();
        }

        private void Update()
        {
            if (audioSource == null || walkingClip == null || characterController == null) return;
            Vector3 velocity = characterController.velocity; velocity.y = 0f;
            bool shouldPlay = Time.timeScale > 0f && characterController.enabled && characterController.isGrounded &&
                              velocity.sqrMagnitude > movementThreshold * movementThreshold;
            if (shouldPlay)
            {
                if (!audioSource.isPlaying) audioSource.Play();
            }
            else if (audioSource.isPlaying) audioSource.Pause();
        }

        private void HandleVolumesChanged(float _, float effects) => SetVolume(effects);
        private void RefreshVolume() => SetVolume(GameAudioService.HasInstance ? GameAudioService.Instance.EffectsVolume : 1f);
        private void SetVolume(float value) { if (audioSource != null) audioSource.volume = Mathf.Clamp01(value); }
    }
}
