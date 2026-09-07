using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOneEnemyBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private CombatHealth health;
        [SerializeField] private Transform eye;
        [SerializeField] private Transform shootOrigin;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float attackSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float defendSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float retreatSpeed = 6f;
        [SerializeField, Min(1f)] private float preferredCombatDistance = 9f;
        [SerializeField, Min(0.05f)] private float navigationRefreshInterval = 0.2f;
        [SerializeField, Min(0.5f)] private float defendWanderRadius = 7f;
        [SerializeField, Min(0.2f)] private float defendWanderInterval = 3f;

        [Header("Perception")]
        [SerializeField, Range(1f, 179f)] private float viewAngle = 110f;
        [SerializeField] private LayerMask sightLayers = ~0;

        [Header("Pistol")]
        [SerializeField, Min(0.05f)] private float fireInterval = 0.65f;
        [SerializeField, Min(0f)] private float minimumWeaponDamage = 5f;
        [FormerlySerializedAs("weaponDamage")]
        [SerializeField, Min(0f)] private float maximumWeaponDamage = 10f;
        [SerializeField, Min(1f)] private float weaponRange = 250f;
        [SerializeField, Min(0f)] private float nearAccuracyDistance = 6f;
        [SerializeField, Min(0.1f)] private float farAccuracyDistance = 35f;
        [SerializeField, Range(0f, 1f)] private float nearAccuracy = 0.85f;
        [SerializeField, Range(0f, 1f)] private float farAccuracy = 0.2f;
        [SerializeField, Min(0f)] private float maximumMissAngle = 12f;

        [Header("Retreat")]
        [SerializeField, Range(0f, 1f)] private float retreatChancePerHit = 0.1f;
        [SerializeField, Min(0.1f)] private float retreatDuration = 3f;
        [SerializeField, Min(1f)] private float retreatDistance = 12f;

        private LevelOneEnemyDirector _director;
        private Transform _player;
        private CombatHealth _playerHealth;
        private Transform _defendSite;
        private EnemyBehaviour _behaviour;
        private float _nextNavigationTime;
        private float _nextWanderTime;
        private float _nextFireTime;
        private float _retreatEndsAt;
        private bool _initialized;
        private bool _deathHandled;

        public EnemyBehaviour Behaviour => _behaviour;
        public bool IsAlive => health != null && !health.IsDead;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (health == null)
            {
                health = GetComponent<CombatHealth>();
            }

            if (agent != null)
            {
                agent.updateRotation = false;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        public void Initialize(
            LevelOneEnemyDirector director,
            Transform player,
            CombatHealth playerHealth,
            Transform defendSite,
            EnemyBehaviour initialBehaviour)
        {
            _director = director;
            _player = player;
            _playerHealth = playerHealth;
            _defendSite = defendSite;
            _initialized = true;
            SetBehaviour(initialBehaviour);
        }

        private void Update()
        {
            if (!_initialized || !IsAlive || _player == null || _playerHealth == null || _playerHealth.IsDead)
            {
                StopAgent();
                return;
            }

            bool seesPlayer = CanSeePlayer(out float playerDistance);
            switch (_behaviour)
            {
                case EnemyBehaviour.Attack:
                    UpdateAttack(seesPlayer, playerDistance);
                    break;
                case EnemyBehaviour.Defend:
                    UpdateDefend(seesPlayer, playerDistance);
                    break;
                case EnemyBehaviour.Retreat:
                    UpdateRetreat();
                    break;
            }

            UpdateFacing(seesPlayer);
        }

        public void ReassessAfterAllyDefeated(float playerAreaRadius)
        {
            if (!IsAlive || _player == null || _behaviour == EnemyBehaviour.Retreat)
            {
                return;
            }

            SetBehaviour(Vector3.Distance(transform.position, _player.position) <= playerAreaRadius
                ? EnemyBehaviour.Defend
                : EnemyBehaviour.Attack);
        }

        private void UpdateAttack(bool seesPlayer, float playerDistance)
        {
            SetAgentSpeed(attackSpeed);
            if (seesPlayer)
            {
                TryShoot(playerDistance);
            }

            if (seesPlayer && playerDistance <= preferredCombatDistance)
            {
                StopAgent();
                return;
            }

            if (Time.time >= _nextNavigationTime)
            {
                SetDestination(_player.position);
                _nextNavigationTime = Time.time + navigationRefreshInterval;
            }
        }

        private void UpdateDefend(bool seesPlayer, float playerDistance)
        {
            SetAgentSpeed(defendSpeed);
            if (seesPlayer)
            {
                StopAgent();
                TryShoot(playerDistance);
                return;
            }

            if (_defendSite == null)
            {
                SetBehaviour(EnemyBehaviour.Attack);
                return;
            }

            if (Time.time >= _nextWanderTime || HasReachedDestination())
            {
                Vector2 offset = Random.insideUnitCircle * defendWanderRadius;
                Vector3 candidate = _defendSite.position + new Vector3(offset.x, 0f, offset.y);
                SetDestination(candidate, defendWanderRadius);
                _nextWanderTime = Time.time + defendWanderInterval;
            }
        }

        private void UpdateRetreat()
        {
            SetAgentSpeed(retreatSpeed);
            if (Time.time >= _retreatEndsAt)
            {
                SetBehaviour(Random.value < 0.5f ? EnemyBehaviour.Attack : EnemyBehaviour.Defend);
                return;
            }

            if (HasReachedDestination() && Time.time >= _nextNavigationTime)
            {
                SetRetreatDestination();
            }
        }

        private bool CanSeePlayer(out float distance)
        {
            Vector3 origin = eye == null ? transform.position + Vector3.up * 1.6f : eye.position;
            Vector3 toPlayer = _playerHealth.AimPointPosition - origin;
            distance = toPlayer.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
            if (flatDirection.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, flatDirection) > viewAngle * 0.5f)
            {
                return false;
            }

            if (!Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance + 0.25f, sightLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.GetComponentInParent<CombatHealth>() == _playerHealth;
        }

        private void TryShoot(float playerDistance)
        {
            if (Time.time < _nextFireTime)
            {
                return;
            }

            _nextFireTime = Time.time + fireInterval;
            Vector3 origin = shootOrigin == null ? (eye == null ? transform.position + Vector3.up * 1.5f : eye.position) : shootOrigin.position;
            Vector3 exactDirection = (_playerHealth.AimPointPosition - origin).normalized;
            float distanceFactor = Mathf.InverseLerp(farAccuracyDistance, nearAccuracyDistance, playerDistance);
            float hitChance = Mathf.Lerp(farAccuracy, nearAccuracy, distanceFactor);
            float missAngle = Mathf.Lerp(maximumMissAngle, 0.5f, distanceFactor);
            Vector3 direction = exactDirection;

            if (Random.value > hitChance)
            {
                Vector2 spread = Random.insideUnitCircle * missAngle;
                direction = Quaternion.AngleAxis(spread.x, transform.up) *
                            Quaternion.AngleAxis(spread.y, transform.right) * exactDirection;
            }

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, weaponRange, sightLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            CombatHealth hitHealth = hit.collider.GetComponentInParent<CombatHealth>();
            if (hitHealth != null && hitHealth.Team == CombatTeam.Player)
            {
                float shotDamage = Random.Range(minimumWeaponDamage, maximumWeaponDamage);
                hitHealth.ApplyDamage(shotDamage, hit.point, direction);
            }
        }

        private void UpdateFacing(bool seesPlayer)
        {
            Vector3 direction = seesPlayer
                ? _player.position - transform.position
                : agent != null && agent.isOnNavMesh ? agent.desiredVelocity : Vector3.zero;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
        }

        private void OnDamaged(CombatHealth sender)
        {
            if (!sender.IsDead && _behaviour != EnemyBehaviour.Retreat && Random.value < retreatChancePerHit)
            {
                SetBehaviour(EnemyBehaviour.Retreat);
            }
        }

        private void OnDied(CombatHealth sender)
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            StopAgent();
            if (agent != null)
            {
                agent.enabled = false;
            }

            foreach (Collider enemyCollider in GetComponentsInChildren<Collider>())
            {
                enemyCollider.enabled = false;
            }

            _director?.NotifyEnemyDied(this);
            Destroy(gameObject, 1.25f);
        }

        private void SetBehaviour(EnemyBehaviour behaviour)
        {
            _behaviour = behaviour;
            _nextNavigationTime = 0f;
            _nextWanderTime = 0f;
            if (behaviour == EnemyBehaviour.Retreat)
            {
                _retreatEndsAt = Time.time + retreatDuration;
                SetRetreatDestination();
            }
        }

        private void SetRetreatDestination()
        {
            Vector3 away = transform.position - _player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -transform.forward;
            }

            Vector3 sideways = Vector3.Cross(Vector3.up, away.normalized) * Random.Range(-retreatDistance * 0.35f, retreatDistance * 0.35f);
            SetDestination(transform.position + away.normalized * retreatDistance + sideways, retreatDistance);
            _nextNavigationTime = Time.time + navigationRefreshInterval;
        }

        private void SetAgentSpeed(float speed)
        {
            if (agent != null && agent.enabled)
            {
                agent.speed = speed;
            }
        }

        private void SetDestination(Vector3 destination, float sampleRadius = 3f)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, sampleRadius, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        private bool HasReachedDestination()
        {
            return agent == null || !agent.enabled || !agent.isOnNavMesh ||
                   (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f);
        }

        private void StopAgent()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        private void OnValidate()
        {
            minimumWeaponDamage = Mathf.Max(0f, minimumWeaponDamage);
            maximumWeaponDamage = Mathf.Max(minimumWeaponDamage, maximumWeaponDamage);
        }
    }
}
