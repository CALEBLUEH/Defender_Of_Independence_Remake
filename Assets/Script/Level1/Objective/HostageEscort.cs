using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class HostageEscort : MonoBehaviour, IPlayerInteractable
    {
        [Header("Follow")]
        [SerializeField, Min(0.5f)] private float followDistance = 2.2f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 360f;
        [SerializeField, Min(2f)] private float maximumCatchUpDistance = 12f;

        private LevelOneObjectiveController _objective;
        private Transform _player;
        private Rigidbody _body;

        public string InteractionPrompt => IsFollowing ? "HOSTAGE IS FOLLOWING" : "ASK HOSTAGE TO FOLLOW";
        public bool CanInteract => !IsSaved;
        public bool IsFollowing { get; private set; }
        public bool IsSaved { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
        }

        public void Initialize(LevelOneObjectiveController objective, Transform player)
        {
            _objective = objective;
            _player = player;
        }

        public void Interact()
        {
            _objective?.RequestEscort(this);
        }

        public void BeginFollowing()
        {
            if (!IsSaved)
            {
                IsFollowing = true;
            }
        }

        public void MarkSaved()
        {
            if (IsSaved)
            {
                return;
            }

            IsFollowing = false;
            IsSaved = true;
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (!IsFollowing || IsSaved || _player == null || _body == null)
            {
                return;
            }

            Vector3 target = _player.position - _player.forward * followDistance;
            target.y = _body.position.y;
            float distance = Vector3.Distance(_body.position, target);
            Vector3 nextPosition = distance > maximumCatchUpDistance
                ? target
                : Vector3.MoveTowards(_body.position, target, moveSpeed * Time.fixedDeltaTime);
            _body.MovePosition(nextPosition);

            Vector3 direction = _player.position - nextPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                _body.MoveRotation(Quaternion.RotateTowards(_body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }

        private void OnValidate()
        {
            followDistance = Mathf.Max(0.5f, followDistance);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            turnSpeed = Mathf.Max(0.1f, turnSpeed);
            maximumCatchUpDistance = Mathf.Max(2f, maximumCatchUpDistance);
        }
    }
}
