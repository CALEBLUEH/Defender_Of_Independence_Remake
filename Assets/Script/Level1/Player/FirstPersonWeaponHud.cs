using TMPro;
using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    public sealed class FirstPersonWeaponHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text fireModeText;

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
    }
}
