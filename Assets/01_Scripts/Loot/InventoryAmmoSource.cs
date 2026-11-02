using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryAmmoSource : MonoBehaviour, IAmmoSource
{
    [SerializeField] private PlayerInventory inventory;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
    }

    public int GetQuantity(AmmoData ammo)
    {
        return inventory.GetQuantity(ammo);
    }

    public int ConsumeUpTo(AmmoData ammo, int requestedRounds)
    {
        if (requestedRounds <= 0) return 0;
        int amount = Mathf.Min(requestedRounds, GetQuantity(ammo));
        return amount > 0 && inventory.TryConsume(ammo, amount) ? amount : 0;
    }
}
