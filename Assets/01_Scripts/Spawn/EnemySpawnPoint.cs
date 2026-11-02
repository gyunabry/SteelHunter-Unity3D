using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("순찰 경로")]
    [SerializeField] private Transform patrolRoot;

    [Header("NavMesh 보정")]
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 3f;

    /// <summary>
    /// PatrolRoot 아래의 포인트를 가져와 순찰 지점 배열로 반환
    /// </summary>
    public Transform[] GetPatrolPoints()
    {
        if (patrolRoot == null)
        {
            return Array.Empty<Transform>();
        }

        int count = patrolRoot.childCount;
        var points = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            points[i] = patrolRoot.GetChild(i);
        }

        return points;
    }

    /// <summary>
    /// 스폰 포인트 주변에서 실제로 전차를 생성할 수 있는 NavMesh를 탐색
    /// </summary>
    public bool TryGetSpawnPose(NavMeshQueryFilter filter, out Vector3 position, out Quaternion rotation)
    {
        rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleDistance, filter))
        {
            position = default;
            return false;
        }

        position = hit.position;
        return true;
    }

    /// <summary>
    /// 빨간원 : 전차 생성 위치
    /// 노란점 : 순찰 포인트
    /// 노란선 : 순찰 순서
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (patrolRoot == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < patrolRoot.childCount; i++)
        {
            Transform current = patrolRoot.GetChild(i);
            Transform next = patrolRoot.GetChild(
                (i + 1) % patrolRoot.childCount);

            Gizmos.DrawSphere(current.position, 0.4f);
            Gizmos.DrawLine(current.position, next.position);
        }
    }
}
