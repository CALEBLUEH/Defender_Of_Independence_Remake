using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderOfIndependence.Level1
{
    public sealed class FirstPersonWeaponHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text fireModeText;
        [SerializeField] private CombatHealth playerHealth;
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private TMP_Text playerHealthText;
        [SerializeField] private TMP_Text targetHealthText;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed += RefreshPlayerHealth;
                RefreshPlayerHealth(playerHealth);
            }
        }

        private void Start()
        {
            RefreshPlayerHealth(playerHealth);
            ShowTargetHealth(null);
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed -= RefreshPlayerHealth;
            }
        }

        public void Refresh(int ammunition, int magazineSize, bool automatic, bool reloading)
        {
            if (ammoText != null)
            {
                ammoText.text = reloading ? "RELOADING" : $"{ammunition:00} / {magazineSize:00}";
            }

            if (fireModeText != null)
            {
                fireModeText.text = automatic ? "AUTO" : "MANUAL";
            }
        }

        public void ShowTargetHealth(CombatHealth target)
        {
            if (targetHealthText == null)
            {
                return;
            }

            bool visible = target != null && target.Team == CombatTeam.Enemy && !target.IsDead;
            targetHealthText.gameObject.SetActive(visible);
            targetHealthText.text = visible
                ? $"{target.DisplayName}: {Mathf.CeilToInt(target.HealthPercent * 100f)}%"
                : string.Empty;
        }

        private void RefreshPlayerHealth(CombatHealth sender)
        {
            if (sender == null)
            {
                return;
            }

            if (playerHealthSlider != null)
            {
                playerHealthSlider.SetValueWithoutNotify(sender.HealthPercent);
            }
            else if (playerHealthFill != null)
            {
                playerHealthFill.fillAmount = sender.HealthPercent;
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = $"HP {Mathf.CeilToInt(sender.HealthPercent * 100f)}%";
            }
        }
    }
}
