using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using DefenderOfIndependence.Audio;

namespace DefenderOfIndependence.Level1
{
    public sealed class FirstPersonWeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelOnePlayerInput input;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform weaponView;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private FirstPersonWeaponHud hud;

        [Header("Pistol")]
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(0.02f)] private float automaticShotInterval = 0.12f;
        [SerializeField, Min(0f)] private float reloadDuration = 0.5f;
        [SerializeField, Min(0f)] private float minimumDamage = 5f;
        [FormerlySerializedAs("damage")]
        [SerializeField, Min(0f)] private float maximumDamage = 10f;
        [SerializeField, Min(1f)] private float range = 100f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("View Pushback")]
        [SerializeField] private float weaponPushback = 0.09f;
        [SerializeField] private float weaponPitchKick = 7f;
        [SerializeField] private float viewPitchKick = 1.5f;
        [SerializeField] private float recoilRecovery = 14f;
        [SerializeField] private float movementBobAmount = 0.012f;
        [SerializeField] private float movementBobSpeed = 10f;

        private int _ammunition;
        private bool _automatic;
        private bool _reloading;
        private float _nextShotTime;
        private float _recoilPosition;
        private float _recoilRotation;
        private float _viewKick;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;

        public int Ammunition => _ammunition;
        public bool Automatic => _automatic;
        public bool IsReloading => _reloading;

        private void Awake()
        {
            _ammunition = magazineSize;
            if (weaponView != null)
            {
                _baseLocalPosition = weaponView.localPosition;
                _baseLocalRotation = weaponView.localRotation;
            }
        }

        private void Start()
        {
            RefreshHud();
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            bool firePressed = input.ConsumeFirePressed();
            if (input.ConsumeToggleFireModePressed())
            {
                _automatic = !_automatic;
                RefreshHud();
            }

            if (input.ConsumeReloadPressed())
            {
                BeginReload();
            }

            bool wantsToFire = _automatic ? input.FireHeld : firePressed;
            if (wantsToFire && !_reloading && Time.time >= _nextShotTime)
            {
                Fire();
            }
        }

        private void LateUpdate()
        {
            _recoilPosition = Mathf.Lerp(_recoilPosition, 0f, recoilRecovery * Time.deltaTime);
            _recoilRotation = Mathf.Lerp(_recoilRotation, 0f, recoilRecovery * Time.deltaTime);
            _viewKick = Mathf.Lerp(_viewKick, 0f, recoilRecovery * Time.deltaTime);

            if (weaponView != null)
            {
                float horizontalSpeed = characterController == null
                    ? 0f
                    : new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
                float bob = horizontalSpeed > 0.1f ? Mathf.Sin(Time.time * movementBobSpeed) * movementBobAmount : 0f;
                weaponView.localPosition = _baseLocalPosition + new Vector3(bob * 0.55f, Mathf.Abs(bob), -_recoilPosition);
                weaponView.localRotation = _baseLocalRotation * Quaternion.Euler(-_recoilRotation, 0f, bob * 40f);
            }

            if (aimCamera != null)
            {
                aimCamera.transform.localRotation = Quaternion.Euler(-_viewKick, 0f, 0f);
            }
        }

        private void Fire()
        {
            if (_ammunition <= 0)
            {
                BeginReload();
                return;
            }

            _ammunition--;
            _nextShotTime = Time.time + (_automatic ? automaticShotInterval : 0.05f);
            _recoilPosition = Mathf.Min(_recoilPosition + weaponPushback, weaponPushback * 2f);
            _recoilRotation = Mathf.Min(_recoilRotation + weaponPitchKick, weaponPitchKick * 2f);
            _viewKick = Mathf.Min(_viewKick + viewPitchKick, viewPitchKick * 3f);
            GameAudioService.Instance?.PlayGunshot();

            if (aimCamera != null && Physics.Raycast(
                    aimCamera.transform.position,
                    aimCamera.transform.forward,
                    out RaycastHit hit,
                    range,
                    hitLayers,
                    QueryTriggerInteraction.Ignore))
            {
                IDamageable damageable = FindDamageable(hit.collider);
                float shotDamage = Random.Range(minimumDamage, maximumDamage);
                damageable?.ApplyDamage(shotDamage, hit.point, aimCamera.transform.forward);
            }

            RefreshHud();
        }

        private void BeginReload()
        {
            if (_reloading || _ammunition == magazineSize)
            {
                return;
            }

            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            _reloading = true;
            RefreshHud();
            yield return new WaitForSeconds(reloadDuration);
            _ammunition = magazineSize;
            _reloading = false;
            RefreshHud();
        }

        private void RefreshHud()
        {
            hud?.Refresh(_ammunition, magazineSize, _automatic, _reloading);
        }

        private static IDamageable FindDamageable(Collider hitCollider)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            minimumDamage = Mathf.Max(0f, minimumDamage);
            maximumDamage = Mathf.Max(minimumDamage, maximumDamage);
        }
    }
}
