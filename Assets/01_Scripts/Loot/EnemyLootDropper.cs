using UnityEngine;

public class EnemyLootDropper : MonoBehaviour
{
    [Header("Àü¸®Ç°")]
    [SerializeField] private LootContainer lootPrefab;
    [SerializeField] private LootTableData lootTable;
    [SerializeField] private Transform dropAnchor;
    [SerializeField] private LayerMask groundMask;
    
    private CharacterHealth health;
    private bool hasDropped;

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();
    }

    private void OnEnable()
    {
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        if (hasDropped) return;

        hasDropped = true;

        Vector3 origin = dropAnchor != null ? dropAnchor.position : transform.position;
        Vector3 position = origin;
        position.y = 0f;
        RaycastHit[] hits = Physics.RaycastAll(origin + Vector3.up * 1.5f, Vector3.down,
            100f, groundMask, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            position = hit.point;
        }
        if (float.IsPositiveInfinity(nearest))
        {
            Debug.LogWarning("Loot ground was not found. Falling back to y = 0. Check Ground Mask and ground colliders.", this);
        }

        LootContainer container = Instantiate(lootPrefab, position, Quaternion.identity);
        container.Initialize(lootTable);
    }
}
