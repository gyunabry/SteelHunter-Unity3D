using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public sealed class StashInventory
{
    private readonly List<ItemStack> items = new();
    private readonly ReadOnlyCollection<ItemStack> readOnlyItems;

    public event Action Changed;
    public IReadOnlyList<ItemStack> Items => readOnlyItems;

    public StashInventory()
    {
        readOnlyItems = items.AsReadOnly();
    }

    internal bool TryDeposit(PlayerInventory inventory, int index,
        out string message)
    {
        message = null;
        if (inventory == null ||
            !inventory.TryPrepareRemove(index, out ItemStack item,
                out ItemStack[] nextInventory))
        {
            message = "보관할 아이템이 없습니다.";
            return false;
        }

        if (item.Data is EquipmentData && ContainsId(item.InstanceId))
        {
            message = "이미 창고에 있는 장비입니다.";
            return false;
        }

        List<ItemStack> nextStash = new(items);
        AddTo(nextStash, item);

        // 양쪽 소유 상태를 먼저 확정하고 그 다음 이벤트를 보낸다.
        inventory.ReplaceSlotsSilently(nextInventory);
        items.Clear();
        items.AddRange(nextStash);
        inventory.PublishChanged();
        Changed?.Invoke();
        return true;
    }

    internal bool TryWithdraw(int index, PlayerInventory inventory,
        out string message)
    {
        message = null;
        if (inventory == null || index < 0 || index >= items.Count ||
            items[index] == null)
        {
            message = "꺼낼 아이템이 없습니다.";
            return false;
        }

        if (!inventory.TryPrepareAdd(items[index], out ItemStack[] nextInventory))
        {
            message = "가방 공간 또는 허용 무게가 부족하거나 중복 장비입니다.";
            return false;
        }

        items[index] = null;
        inventory.ReplaceSlotsSilently(nextInventory);
        inventory.PublishChanged();
        Changed?.Invoke();
        return true;
    }

    private bool ContainsId(string id)
    {
        foreach (ItemStack item in items)
        {
            if (item != null && item.InstanceId == id)
                return true;
        }
        return false;
    }

    private static void AddTo(List<ItemStack> target, ItemStack incoming)
    {
        if (incoming.Data is EquipmentData)
        {
            int empty = target.IndexOf(null);
            if (empty >= 0) target[empty] = incoming;
            else target.Add(incoming);
            return;
        }

        int remaining = incoming.Quantity;
        int maxStack = incoming.Data.MaxStack;

        for (int i = 0; i < target.Count && remaining > 0; i++)
        {
            ItemStack existing = target[i];
            if (existing == null || existing.Data != incoming.Data) continue;
            int amount = Mathf.Min(remaining,
                Mathf.Max(0, maxStack - existing.Quantity));
            if (amount == 0) continue;
            target[i] = new ItemStack(existing.Data, existing.Quantity + amount);
            remaining -= amount;
        }

        for (int i = 0; i < target.Count && remaining > 0; i++)
        {
            if (target[i] != null) continue;
            int amount = Mathf.Min(remaining, maxStack);
            target[i] = new ItemStack(incoming.Data, amount);
            remaining -= amount;
        }

        while (remaining > 0)
        {
            int amount = Mathf.Min(remaining, maxStack);
            target.Add(new ItemStack(incoming.Data, amount));
            remaining -= amount;
        }
    }
}
