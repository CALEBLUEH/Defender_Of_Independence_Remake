using UnityEngine;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private CombatHealth health;
        [SerializeField] private Slider slider;
        [SerializeField] private Image fill;

        private Camera _camera;

        private void OnEnable()
        {
            if (health != null)
            {
                health.Changed += Refresh;
                Refresh(health);
            }
        }

        private void Start()
        {
            _camera = Camera.main;
            Refresh(health);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= Refresh;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position, _camera.transform.up);
            }
        }

        private void Refresh(CombatHealth sender)
        {
            if (sender == null)
            {
                return;
            }

            if (slider != null)
            {
                slider.SetValueWithoutNotify(sender.HealthPercent);
            }
            else if (fill != null)
            {
                fill.fillAmount = sender.HealthPercent;
            }
        }
    }
}
