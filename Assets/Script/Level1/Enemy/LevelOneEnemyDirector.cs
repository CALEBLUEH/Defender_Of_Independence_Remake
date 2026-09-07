using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DefenderOfIndependence.Level1
{
    public sealed class LevelOneEnemyDirector : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private LevelOneEnemyBrain enemyPrefab;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform hostageSiteA;
        [SerializeField] private Transform hostageSiteB;
        [SerializeField] private Transform player;
        [SerializeField] private CombatHealth playerHealth;

        [Header("Spawning")]
        [SerializeField, Min(0)] private int initialEnemyCount = 5;
        [SerializeField, Min(0)] private int reinforcementCount = 2;
        [SerializeField, Min(1f)] private float reinforcementInterval = 60f;
        [SerializeField, Min(0f)] private float spawnRadius;
        [SerializeField, Min(1)] private int maximumAliveEnemies = 25;

        [Header("Team Decisions")]
        [SerializeField, Min(1f)] private float playerAreaRadius = 18f;

        private readonly List<LevelOneEnemyBrain> _livingEnemies = new List<LevelOneEnemyBrain>();
        private bool _reportedMissingNavMesh;
        private bool _gameplayStarted;
        private Coroutine _reinforcementRoutine;

        public int LivingEnemyCount => _livingEnemies.Count;
        public bool GameplayStarted => _gameplayStarted;

        public void BeginGameplay()
        {
            if (_gameplayStarted)
            {
                return;
            }

            _gameplayStarted = true;
            SpawnBatch(initialEnemyCount);
            _reinforcementRoutine = StartCoroutine(SpawnReinforcements());
        }

        private void OnDisable()
        {
            if (_reinforcementRoutine != null)
            {
                StopCoroutine(_reinforcementRoutine);
                _reinforcementRoutine = null;
            }
        }

        private IEnumerator SpawnReinforcements()
        {
            WaitForSeconds wait = new WaitForSeconds(reinforcementInterval);
            while (true)
            {
                yield return wait;
                SpawnBatch(reinforcementCount);
            }
        }

        public void NotifyEnemyDied(LevelOneEnemyBrain defeated)
        {
            _livingEnemies.Remove(defeated);
            for (int index = _livingEnemies.Count - 1; index >= 0; index--)
            {
                LevelOneEnemyBrain enemy = _livingEnemies[index];
                if (enemy == null)
                {
                    _livingEnemies.RemoveAt(index);
                }
                else
                {
                    enemy.ReassessAfterAllyDefeated(playerAreaRadius);
                }
            }
        }

        private void SpawnBatch(int requestedCount)
        {
            if (enemyPrefab == null || enemySpawnPoint == null || player == null || playerHealth == null)
            {
                Debug.LogError("Level 1 Enemy Director is missing required scene references.", this);
                return;
            }

            int availableSlots = Mathf.Max(0, maximumAliveEnemies - _livingEnemies.Count);
            int count = Mathf.Min(requestedCount, availableSlots);
            for (int index = 0; index < count; index++)
            {
                Vector3 position = FindSpawnPosition(index, count);
                LevelOneEnemyBrain enemy = Instantiate(enemyPrefab, position, enemySpawnPoint.rotation);
                enemy.name = $"Enemy {_livingEnemies.Count + 1:00}";
                EnemyBehaviour initial = Random.value < 0.5f ? EnemyBehaviour.Attack : EnemyBehaviour.Defend;
                Transform defendSite = Random.value < 0.5f ? hostageSiteA : hostageSiteB;
                enemy.Initialize(this, player, playerHealth, defendSite, initial);
                _livingEnemies.Add(enemy);
            }
        }

        private Vector3 FindSpawnPosition(int index, int count)
        {
            float angle = count <= 0 ? 0f : index * Mathf.PI * 2f / count;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
            Vector3 candidate = enemySpawnPoint.position + offset;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(3f, spawnRadius), NavMesh.AllAreas))
            {
                return hit.position;
            }

            if (!_reportedMissingNavMesh)
            {
                _reportedMissingNavMesh = true;
                Debug.LogWarning("Enemy spawn could not find the Level 1 NavMesh. Enemies will spawn at the marker but cannot navigate until the NavMesh is rebuilt.", this);
            }

            return candidate;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.8f);
            if (enemySpawnPoint != null)
            {
                Gizmos.DrawWireSphere(enemySpawnPoint.position, spawnRadius);
            }

            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.8f);
            if (hostageSiteA != null)
            {
                Gizmos.DrawWireSphere(hostageSiteA.position, 7f);
            }

            if (hostageSiteB != null)
            {
                Gizmos.DrawWireSphere(hostageSiteB.position, 7f);
            }

            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.65f);
            if (player != null)
            {
                Gizmos.DrawWireSphere(player.position, playerAreaRadius);
            }
        }
    }
}
