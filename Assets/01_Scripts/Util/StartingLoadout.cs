using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerInventory), typeof(CharacterEquipment))]
public class StartingLoadout : MonoBehaviour
{
    [Serializable]
    private struct AmmoEntry
    {
        public AmmoData ammo;
        [Min(0)] public int quantity;
    }

    [SerializeField] private FirearmData firearm;
    [SerializeField] private AmmoEntry[] ammoEntries = Array.Empty<AmmoEntry>();
    [SerializeField] private bool equipOnStart;
    [SerializeField] private AmmoData defaultAmmo;
    [SerializeField] private bool reloadOnStart;

    private bool granted;

    private void Start()
    {
        // 세션 연결이 없는 다른 기존 씬은 원래 동작을 유지한다.
        if (GetComponent<InventorySessionBinding>() == null)
            GiveInitialItems();
    }

    public void GiveInitialItems()
    {
        if (granted) return;
        granted = true;

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        CharacterEquipment equipment = GetComponent<CharacterEquipment>();
        CharacterGun gun = GetComponent<CharacterGun>();
        ItemStack firearmItem = null;

        if (firearm != null)
        {
            firearmItem = new ItemStack(firearm, 1);
            if (!inventory.TryAddItem(firearmItem))
            {
                Debug.LogWarning("Could not add the starting firearm.", this);
                firearmItem = null;
            }
        }

        foreach (AmmoEntry entry in ammoEntries)
        {
            if (entry.ammo == null || entry.quantity <= 0) continue;
            if (!inventory.TryAddItem(new ItemStack(entry.ammo, entry.quantity)))
                Debug.LogWarning("Could not add starting ammunition.", this);
        }

        if (!equipOnStart || firearmItem == null) return;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (!ReferenceEquals(inventory.GetItem(i), firearmItem)) continue;
            if (!equipment.TryEquip(i)) return;
            if (gun != null && gun.TrySelectAmmo(defaultAmmo) && reloadOnStart)
                gun.TryReload();
            return;
        }
    }
}
