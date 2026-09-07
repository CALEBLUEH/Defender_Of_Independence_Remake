using UnityEngine;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        [SerializeField] private CombatHealth health;
        [SerializeField] private Image damageOverlay;
        [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.28f;
        [SerializeField, Min(0f)] private float holdDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.65f;

        private float _remainingHold;
        private float _currentAlpha;

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
            }

            SetAlpha(0f);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            SetAlpha(0f);
        }

        private void Update()
        {
            if (_remainingHold > 0f)
            {
                _remainingHold -= Time.unscaledDeltaTime;
                return;
            }

            if (_currentAlpha <= 0f)
            {
                return;
            }

            SetAlpha(Mathf.MoveTowards(_currentAlpha, 0f, peakAlpha / fadeDuration * Time.unscaledDeltaTime));
        }

        private void OnDamaged(CombatHealth sender)
        {
            _remainingHold = holdDuration;
            SetAlpha(peakAlpha);
        }

        private void SetAlpha(float alpha)
        {
            _currentAlpha = alpha;
            if (damageOverlay == null)
            {
                return;
            }

            Color color = damageOverlay.color;
            color.a = alpha;
            damageOverlay.color = color;
        }

        private void OnValidate()
        {
            peakAlpha = Mathf.Clamp01(peakAlpha);
            holdDuration = Mathf.Max(0f, holdDuration);
            fadeDuration = Mathf.Max(0.01f, fadeDuration);
        }
    }
}
