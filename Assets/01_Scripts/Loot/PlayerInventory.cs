using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int baseSlotCount = 20;
    [SerializeField, Min(0f)] private float baseMaxWeight = 40f;
    [SerializeField] private CharacterEquipment equipment;

    // 순서 정보가 필요하기 때문에 배열 사용
    private ItemStack[] slots;

    public event Action Changed;
    public long Version { get; private set; }

    public int BaseSlotCount => Mathf.Max(1, baseSlotCount);
    public int SlotCount => slots?.Length ?? 0;

    // 가방 안의 내용물 무게
    public float ContentsWeight
    {
        get
        {
            float total = 0f;

            foreach (ItemStack stack in slots)
            {
                if (stack != null)
                {
                    total += stack.TotalWeight;
                }
            }

            return total;
        }
    }

    // 장비를 포함한 가방 전체 무게
    public float CurrentWeight
    {
        get
        {
            float equippedWeight = equipment != null ? equipment.EquippedWeight : 0f;

            return ContentsWeight + equippedWeight;
        }
    }

    public float MaxWeight
    {
        get
        {
            BackpackData bag = equipment != null ? equipment.Backpack : null;

            return GetMaxWeight(bag);
        }
    }

    public int OccupiedSlotCount
    {
        get
        {
            int count = 0;

            foreach (ItemStack stack in slots)
            {
                if (stack != null) count++;
            }

            return count;
        }
    }

    private void Awake()
    {
        if (equipment == null)
            equipment = GetComponent<CharacterEquipment>();
        slots = new ItemStack[BaseSlotCount];
    }

    public bool TryAddItem(ItemStack item)
    {
        if (!TryPrepareAdd(item, out ItemStack[] next))
            return false;
    
        ReplaceSlotsSilently(next);
        PublishChanged();
        return true;
    }

    internal ItemStack[] CopySlots()
    {
        ItemStack[] copy = new ItemStack[SlotCount];
        for (int i = 0; i < copy.Length; i++)
            copy[i] = slots[i]?.DeepCopy();
        return copy;
    }
    
    internal void ReplaceSlotsSilently(ItemStack[] next)
    {
        slots = next;
    }
    
    internal bool ContainsInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return false;
    
        if (slots != null)
        {
            foreach (ItemStack stack in slots)
            {
                if (stack != null && stack.InstanceId == instanceId)
                    return true;
            }
        }
    
        return equipment != null && equipment.ContainsInstanceId(instanceId);
    }
    
    internal bool TryPrepareRemove(int index,
        out ItemStack item, out ItemStack[] next)
    {
        item = null;
        next = null;
        if (slots == null || index < 0 || index >= slots.Length)
            return false;
    
        item = slots[index];
        if (item == null) return false;
    
        next = (ItemStack[])slots.Clone();
        next[index] = null;
        return true;
    }
    
    internal bool TryPrepareAdd(ItemStack item, out ItemStack[] next)
    {
        next = null;
        if (slots == null || item == null || item.Data == null)
            return false;
    
        if (item.Data is EquipmentData && ContainsInstanceId(item.InstanceId))
            return false;

        if (CurrentWeight + item.TotalWeight > MaxWeight)
            return false;
    
        ItemStack[] candidate = (ItemStack[])slots.Clone();
    
        if (item.Data is EquipmentData)
        {
            int emptyIndex = Array.IndexOf(candidate, null);
            if (emptyIndex < 0) return false;
            candidate[emptyIndex] = item;
            next = candidate;
            return true;
        }
    
        int remaining = item.Quantity;
        int maxStack = item.Data.MaxStack;
    
        for (int i = 0; i < candidate.Length && remaining > 0; i++)
        {
            ItemStack existing = candidate[i];
            if (existing == null || existing.Data != item.Data)
                continue;
    
            int amount = Mathf.Min(remaining,
                Mathf.Max(0, maxStack - existing.Quantity));
            if (amount == 0) continue;
            candidate[i] = new ItemStack(existing.Data, existing.Quantity + amount);
            remaining -= amount;
        }
    
        for (int i = 0; i < candidate.Length && remaining > 0; i++)
        {
            if (candidate[i] != null) continue;
            int amount = Mathf.Min(remaining, maxStack);
            candidate[i] = new ItemStack(item.Data, amount);
            remaining -= amount;
        }
    
        if (remaining > 0) return false;
        next = candidate;
        return true;
    }

    private static bool IsEquipment(ItemStack item)
    {
        return item != null
            && item.Data is EquipmentData
            && item.Quantity == 1;
    }

    private static bool TryBuildResized(
        ItemStack[] source,
        int capacity,
        out ItemStack[] resized)
    {
        resized = null;

        if (capacity < 1)
            return false;

        int occupied = 0;

        foreach (ItemStack stack in source)
        {
            if (stack != null)
                occupied++;
        }

        if (occupied > capacity)
            return false;

        resized = new ItemStack[capacity];

        Array.Copy(
            source,
            resized,
            Math.Min(source.Length, capacity));

        // 작은 가방으로 변경할 때 뒤쪽 물품을 앞의 빈 칸으로 이동.
        int emptyIndex = 0;

        for (int i = capacity; i < source.Length; i++)
        {
            if (source[i] == null)
                continue;

            while (resized[emptyIndex] != null)
                emptyIndex++;

            resized[emptyIndex] = source[i];
        }

        return true;
    }

    internal void PublishChanged()
    {
        Version++;
        Changed?.Invoke();
    }

    internal bool TryPrepareRemoveQuantity(int index, int quantity, out ItemStack[] next)
    {
        next = null;
        if (slots == null || index < 0 || index >= slots.Length || quantity <= 0)
            return false;
        ItemStack item = slots[index];
        if (item == null || quantity > item.Quantity) return false;
        next = (ItemStack[])slots.Clone();
        next[index] = quantity == item.Quantity ? null
            : new ItemStack(item.Data, item.Quantity - quantity);
        return true;
    }

    internal bool TryPreparePurchase(ItemData data, int quantity, out ItemStack[] next)
    {
        next = null;
        if (data == null || quantity <= 0) return false;
        if (!(data is EquipmentData))
        {
            return TryPrepareAdd(new ItemStack(data, quantity), out next);
        }
            
        if (slots == null || CurrentWeight + data.UnitWeight * quantity >= MaxWeight)
        {
            return false;
        }
            
        if (quantity > SlotCount - OccupiedSlotCount) return false;

        next = (ItemStack[])slots.Clone();

        for (int i = 0; i < next.Length && quantity > 0; i++)
        {
            if (next[i] != null) continue;
            next[i] = new ItemStack(data, 1);
            quantity--;
        }

        return true;
    }

    public ItemStack GetItem(int index)
    {
        return slots[index];
    }

    // 인벤토리에 있는 특정 아이템의 총 개수를 반환
    public int GetQuantity(ItemData data)
    {
        int total = 0;

        foreach (ItemStack stack in slots)
        {
            if (stack != null && stack.Data == data)
            {
                total += stack.Quantity;
            }
        }

        return total;
    }

    public bool TryUseItem(ItemData data)
    {
        if (slots == null || data == null || GetQuantity(data) <= 0)
            return false;

        if (!data.TryUse(gameObject))
            return false;

        return TryConsume(data, 1);
    }

    public bool TryConsume(ItemData data, int amount)
    {
        if (slots == null || data == null || amount <= 0)
            return false;

        if (data is EquipmentData)
        {
            return false;
        }

        if (GetQuantity(data) < amount)
        {
            return false;
        }

        int remaining = amount;

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            ItemStack stack = slots[i];

            if (stack == null || stack.Data != data)
                continue;

            int consumed = Mathf.Min(stack.Quantity, remaining);
            int newQuantity = stack.Quantity - consumed;

            slots[i] = newQuantity > 0
                ? new ItemStack(stack.Data, newQuantity)
                : null;

            remaining -= consumed;
        }

        PublishChanged();
        return true;
    }

    public bool TryEquipItem(int index, out ItemStack item)
    {
        item = null;
        ItemStack candidate = GetItem(index);
        if (candidate == null || !(candidate.Data is EquipmentData))
        {
            return false;
        }

        item = candidate;
        slots[index] = null;
        PublishChanged();
        return true;
    }

    public bool TrySwapEquippedWeapon(int index, ref ItemStack equippedItem)
    {
        ItemStack incoming = GetItem(index);
        if (incoming == null || !(incoming.Data is FirearmData) ||
            incoming.Weapon == null || incoming.Quantity != 1)
            return false;

        if (equippedItem != null &&
            (!(equippedItem.Data is FirearmData) ||
             equippedItem.Weapon == null || equippedItem.Quantity != 1))
            return false;

        if (ReferenceEquals(incoming, equippedItem)) return false;

        // Commit both owners before publishing the inventory change.
        slots[index] = equippedItem;
        equippedItem = incoming;
        PublishChanged();
        return true;
    }

    public bool TryStoreEquippedWeapon(ref ItemStack equippedItem)
    {
        if (slots == null || equippedItem == null ||
       !(equippedItem.Data is FirearmData) ||
       equippedItem.Weapon == null || equippedItem.Quantity != 1)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) continue;

            slots[i] = equippedItem;
            equippedItem = null;
            PublishChanged();
            return true;
        }

        return false;
    }

    internal bool TryExchangeEquipment(
        int inventoryIndex,
        ref ItemStack equippedItem,
        int targetCapacity,
        float targetMaxWeight)
    {
        ItemStack incoming = GetItem(inventoryIndex);

        if (!IsEquipment(incoming))
            return false;

        if (equippedItem != null && !IsEquipment(equippedItem))
            return false;

        if (ReferenceEquals(incoming, equippedItem))
            return false;

        // 장착 위치만 바뀌므로 총 소지 무게는 그대로다.
        // 가방 교체 후의 허용 무게로 검사한다.
        if (CurrentWeight >= targetMaxWeight)
            return false;

        ItemStack[] candidate = (ItemStack[])slots.Clone();

        // 새 장비가 있던 가방 칸에 이전 장비를 넣는다.
        candidate[inventoryIndex] = equippedItem;

        if (!TryBuildResized(candidate, targetCapacity, out ItemStack[] resized))
            return false;

        // 모든 검사에 성공한 후 실제 상태를 변경한다.
        slots = resized;
        equippedItem = incoming;

        return true;
    }

    internal bool TryReturnEquipment(
        ref ItemStack equippedItem,
        int targetCapacity,
        float targetMaxWeight)
    {
        if (!IsEquipment(equippedItem))
            return false;

        if (CurrentWeight >= targetMaxWeight)
            return false;

        int emptyIndex = Array.IndexOf(slots, null);

        if (emptyIndex < 0)
            return false;

        ItemStack[] candidate = (ItemStack[])slots.Clone();
        candidate[emptyIndex] = equippedItem;

        if (!TryBuildResized(candidate, targetCapacity, out ItemStack[] resized))
            return false;

        slots = resized;
        equippedItem = null;

        return true;
    }

    private static bool CanMerge(ItemStack existing, ItemStack incoming)
    {
        return existing != null &&
            existing.Data == incoming.Data;
    }

    /// <summary>
    /// 가방 데이터를 통해 해당 가방의 추가되는 무게를 반환
    /// </summary>
    public float GetMaxWeight(BackpackData bag)
    {
        return Mathf.Max(0f, baseMaxWeight) + (bag != null ? bag.AddedWeightCapacity : 0f);
    }
}
