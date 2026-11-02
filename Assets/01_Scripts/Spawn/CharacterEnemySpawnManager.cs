using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class CharacterEnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPointRoot;
    [SerializeField, Min(0)] private int spawnCount = 1;
    [SerializeField] private int agentTypeId = 0;
    [SerializeField] private int areaMask = NavMesh.AllAreas;
    [SerializeField, Min(0f)] private float capsuleCenterHeight = 1f;
    [SerializeField] private int fixedSeed = -1;
    private bool attempted;

    private void Start() { SpawnEnemies(); }

    public void Configure(GameObject prefab, Transform points, int count, int seed = -1)
    {
        if (attempted) throw new InvalidOperationException("Cannot reconfigure a running enemy spawner.");
        enemyPrefab = prefab;
        spawnPointRoot = points;
        spawnCount = Mathf.Max(0, count);
        fixedSeed = seed;
    }

    public void SpawnEnemies()
    {
        if (attempted) return;
        attempted = true;
        if (enemyPrefab == null || spawnPointRoot == null)
        {
            Debug.LogError("Character enemy prefab or spawn root is missing.", this);
            return;
        }

        var points = new List<EnemySpawnPoint>(
            spawnPointRoot.GetComponentsInChildren<EnemySpawnPoint>(false));
        var random = fixedSeed >= 0 ? new System.Random(fixedSeed) : new System.Random();
        for (int i = points.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (points[i], points[other]) = (points[other], points[i]);
        }

        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeId, areaMask = areaMask };
        int created = 0;
        foreach (EnemySpawnPoint point in points)
        {
            if (created >= spawnCount) break;
            if (!point.TryGetSpawnPose(filter, out Vector3 position, out Quaternion rotation))
            {
                Debug.LogWarning($"No humanoid NavMesh at {point.name}.", point);
                continue;
            }

            Vector3 characterPosition = position + Vector3.up * capsuleCenterHeight;
            GameObject enemy = Instantiate(enemyPrefab, characterPosition, rotation);
            CharacterRoot root = enemy.GetComponent<CharacterRoot>();
            EnemyCharacterBrain brain = enemy.GetComponent<EnemyCharacterBrain>();
            if (root == null || !root.IsInitialized || root.Team != CharacterTeam.Enemy ||
                brain == null)
            {
                Debug.LogError("Invalid character enemy prefab.", enemy);
                Destroy(enemy);
                continue;
            }
            brain.Configure(point.GetPatrolPoints(), characterPosition);
            created++;
        }
    }
}
