using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPointRoot;
    [SerializeField, Min(0)] private int spawnCount = 1;

    [Header("NavMesh 설정")]
    [SerializeField] private int agentTypeId = 0;
    [SerializeField] private int navigationAreaMask = NavMesh.AllAreas;

    [Header("캐릭터 배치")]
    [SerializeField, Min(0f)] private float capsuleCenterHeight = 1f;

    [Header("시드")]
    [SerializeField] private int fixedSeed = -1;

    private bool hasSpawned;

    // 보스 스폰 이벤트
    public event Action<CharacterHealth> EnemySpawned;

    public CharacterHealth LastSpawnedHealth { get; private set; }

    private void Start()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        if (hasSpawned)
            return;

        if (enemyPrefab == null || spawnPointRoot == null)
        {
            Debug.LogError(
                "EnemyPrefab 또는 SpawnPointRoot가 없습니다.", this);
            return;
        }

        CharacterRoot prefabRoot = enemyPrefab.GetComponent<CharacterRoot>();

        EnemyCharacterBrain prefabBrain = enemyPrefab.GetComponent<EnemyCharacterBrain>();

        if (prefabRoot == null ||
            prefabRoot.Team != CharacterTeam.Enemy ||
            prefabBrain == null ||
            !prefabBrain.enabled ||
            !enemyPrefab.activeSelf)
        {
            Debug.LogError("활성 상태인 캐릭터 적 프리팹이 필요합니다.", this);
            return;
        }

        hasSpawned = true;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agentTypeId,
            areaMask = navigationAreaMask
        };

        var candidates = new List<EnemySpawnPoint>(
            spawnPointRoot.GetComponentsInChildren<EnemySpawnPoint>(
                false));

        var random = fixedSeed >= 0
            ? new System.Random(fixedSeed)
            : new System.Random();

        Shuffle(candidates, random);

        int spawnedCount = 0;

        foreach (EnemySpawnPoint spawnPoint in candidates)
        {
            if (spawnedCount >= spawnCount)
                break;

            if (!spawnPoint.TryGetSpawnPose(
                    filter,
                    out Vector3 groundPosition,
                    out Quaternion rotation))
            {
                Debug.LogWarning(
                    $"{spawnPoint.name} 주변에 캐릭터용 NavMesh가 없습니다.",
                    spawnPoint);
                continue;
            }

            Vector3 characterPosition =
                groundPosition + Vector3.up * capsuleCenterHeight;

            GameObject enemy = Instantiate(enemyPrefab, characterPosition, rotation);

            CharacterRoot root = enemy.GetComponent<CharacterRoot>();
            EnemyCharacterBrain brain = enemy.GetComponent<EnemyCharacterBrain>();

            if (root == null || 
                !root.IsInitialized ||
                root.Team != CharacterTeam.Enemy ||
                brain == null || 
                !brain.isActiveAndEnabled)
            {
                Debug.LogError($"{spawnPoint.name}에서 캐릭터 적 초기화에 실패했습니다.", enemy);

                Destroy(enemy);
                continue;
            }

            brain.Configure(spawnPoint.GetPatrolPoints(), characterPosition);

            spawnedCount++;

            LastSpawnedHealth = root.Health;

            EnemySpawned?.Invoke(LastSpawnedHealth);
        }

        if (spawnedCount < spawnCount)
        {
            Debug.LogWarning($"요청한 적 {spawnCount}명 중 {spawnedCount}명만 생성했습니다.", this);
        }
    }

    private static void Shuffle<T>(
        IList<T> items,
        System.Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (items[i], items[swapIndex]) =
                (items[swapIndex], items[i]);
        }
    }
}