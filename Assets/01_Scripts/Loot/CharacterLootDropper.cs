using UnityEngine;

[RequireComponent(typeof(CharacterHealth))]
public sealed class CharacterLootDropper : MonoBehaviour
{
    [SerializeField] private LootContainer lootPrefab;
    [SerializeField] private LootTableData lootTable;
    [SerializeField] private Transform dropAnchor;
    [SerializeField] private LayerMask groundMask = ~0;

    private CharacterHealth health;
    private bool dropped;

    private void Awake() 
    { 
        health = GetComponent<CharacterHealth>(); 
    }

    private void OnEnable() 
    { 
        if (health != null) health.Died += Drop; 
    }

    private void OnDisable() 
    { 
        if (health != null) health.Died -= Drop; 
    }

    private void Drop()
    {
        if (dropped) return;
        dropped = true;

        if (lootPrefab == null || lootTable == null)
        {
            Debug.LogWarning("Character loot prefab or table is missing.", this);
            return;
        }

        Vector3 origin = dropAnchor != null ? dropAnchor.position : transform.position;
        Vector3 position = origin;
        position.y = 0f;

        RaycastHit[] groundHits = 
            Physics.RaycastAll(origin + Vector3.up * 1.5f, Vector3.down, 100f, groundMask, QueryTriggerInteraction.Ignore);

        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in groundHits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest)
            {
                continue;
            }

            nearest = hit.distance;
            position = hit.point;
        }

        LootContainer container = Instantiate(lootPrefab, position, Quaternion.identity);
        container.Initialize(lootTable);
    }
}
