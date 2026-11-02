using UnityEngine;

public class PoolPreLoader : MonoBehaviour
{
    [Header("오브젝트 풀에 미리 생성할 프리팹")]
    [SerializeField] private HitText hitTextPrefab;

    [Header("미리 생성할 개수")]
    [SerializeField] private int hitTextCount = 20;

    private void Start()
    {
        PoolManager.Instance.PreLoadPool(hitTextPrefab, hitTextCount);
    }
}
