using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    public sealed class PlayerTargetHealthDisplay : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private FirstPersonWeaponHud hud;
        [SerializeField, Min(1f)] private float maximumDistance = 150f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private CombatHealth _lastTarget;
        private int _lastPercent = -1;

        private void Update()
        {
            CombatHealth target = FindEnemyUnderCrosshair();
            int percent = target == null ? -1 : Mathf.CeilToInt(target.HealthPercent * 100f);
            if (target == _lastTarget && percent == _lastPercent)
            {
                return;
            }

            _lastTarget = target;
            _lastPercent = percent;
            hud?.ShowTargetHealth(target);
        }

        private CombatHealth FindEnemyUnderCrosshair()
        {
            if (aimCamera == null || !Physics.Raycast(
                    aimCamera.transform.position,
                    aimCamera.transform.forward,
                    out RaycastHit hit,
                    maximumDistance,
                    targetLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            CombatHealth health = hit.collider.GetComponentInParent<CombatHealth>();
            return health != null && health.Team == CombatTeam.Enemy && !health.IsDead ? health : null;
        }
    }
}
