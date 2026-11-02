using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInventorySnapshot
{
    private readonly ItemStack[] inventorySlots;
    private readonly ItemStack[] equippedSlots;
    private readonly ItemData[] quickSlotItems;
    private readonly EquipmentSlot activeWeaponSlot;

    private PlayerInventorySnapshot(
        ItemStack[] inventorySlots,
        ItemStack[] equippedSlots, 
        EquipmentSlot activeWeaponSlot,
        ItemData[] quickSlotItems)
    {
        this.inventorySlots = inventorySlots;
        this.equippedSlots = equippedSlots;
        this.activeWeaponSlot = activeWeaponSlot;
        this.quickSlotItems = quickSlotItems;
    }

    public static PlayerInventorySnapshot Capture(
        PlayerInventory inventory,
        CharacterEquipment equipment, 
        QuickSlots quickSlots)
    {
        var snapshot = new PlayerInventorySnapshot(
            inventory.CopySlots(), equipment.CopyEquipped(),
            equipment.ActiveWeaponSlot, quickSlots.CopyBindings());
        snapshot.CheckState(inventory);
        return snapshot;
    }

    public PlayerInventorySnapshot WithoutCarriedItems(int baseSlotCount)
    {
        return new PlayerInventorySnapshot(
            new ItemStack[Mathf.Max(1, baseSlotCount)],
            new ItemStack[5], EquipmentSlot.PrimaryWeapon,
            (ItemData[])quickSlotItems.Clone());
    }

    public void Apply(PlayerInventory inventory,
        CharacterEquipment equipment, QuickSlots quickSlots)
    {
        CheckState(inventory);

        // 실제 상태를 바꾸기 전에 사본 생성을 모두 끝낸다.
        ItemStack[] nextInventory = Clone(inventorySlots);
        ItemStack[] nextEquipment = Clone(equippedSlots);
        ItemData[] nextQuickSlots = (ItemData[])quickSlotItems.Clone();

        equipment.RestoreSilently(nextEquipment, activeWeaponSlot);
        inventory.ReplaceSlotsSilently(nextInventory);
        quickSlots.RestoreBindingsSilently(nextQuickSlots);

        equipment.PublishRestored();
        inventory.PublishChanged();
        quickSlots.RefreshAfterRestore();
    }

    private void CheckState(PlayerInventory inventory)
    {
        if (!CharacterEquipment.CanRestore(equippedSlots, activeWeaponSlot))
            throw new InvalidOperationException("장착 슬롯 상태가 올바르지 않습니다.");
        if (quickSlotItems == null || quickSlotItems.Length != 4)
            throw new InvalidOperationException("퀵슬롯은 4개여야 합니다.");

        BackpackData bag = equippedSlots[(int)EquipmentSlot.Backpack]?.Data
            as BackpackData;
        int expectedCapacity = inventory.BaseSlotCount +
            (bag != null ? bag.AddedSlotCount : 0);
        if (inventorySlots == null || inventorySlots.Length != expectedCapacity)
        {
            throw new InvalidOperationException(
                "가방 슬롯 수가 현재 기본 용량/장착 가방과 일치하지 않습니다.");
        }

        var ids = new HashSet<string>();
        CheckItems(inventorySlots, ids);
        CheckItems(equippedSlots, ids);
    }

    private static void CheckItems(ItemStack[] items, HashSet<string> ids)
    {
        foreach (ItemStack item in items)
        {
            if (item == null) continue;
            if (item.Data == null || item.Quantity <= 0)
                throw new InvalidOperationException("아이템 상태가 올바르지 않습니다.");
            if (item.Data is EquipmentData &&
                (item.Quantity != 1 || string.IsNullOrEmpty(item.InstanceId) ||
                 !ids.Add(item.InstanceId)))
            {
                throw new InvalidOperationException("장비 ID가 누락되거나 중복됐습니다.");
            }
        }
    }

    private static ItemStack[] Clone(ItemStack[] source)
    {
        ItemStack[] copy = new ItemStack[source.Length];
        for (int i = 0; i < source.Length; i++)
            copy[i] = source[i]?.DeepCopy();
        return copy;
    }
}
