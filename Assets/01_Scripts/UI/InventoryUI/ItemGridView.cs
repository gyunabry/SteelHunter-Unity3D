using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemGridView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private ItemSlotView slotPrefab;
    [SerializeField] private int minimumStorageCount = 50;

    private readonly List<ItemSlotView> slots = new();

    public void ShowInventory(PlayerInventory inventory, Action<int> onClick = null, Action<int> onRightClick = null, bool allowDrag = false)
    {
        PrepareSlots(inventory.SlotCount);
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            ItemStack item = inventory.GetItem(i);
            BindSlot(i, item, onClick, onRightClick);

            InventoryItemDrag drag = slots[i].GetComponent<InventoryItemDrag>();
            if (drag != null)
            {
                // 드래그 기능은 명시적으로 켠 경우에만 허용
                // 상점에서는 드래그 X
                if (allowDrag) drag.Bind(inventory, i, item);
                else drag.Bind(null, -1, null);
            }
        }
    }
    
    public void ShowStash(StashInventory stash, Action<int> onClick = null, Action<int> onRightClick = null)
    {
        int slotCount = Mathf.Max(minimumStorageCount, stash.Items.Count);
        PrepareSlots(slotCount);
        for (int i = 0; i < slotCount; i++)
        {
            ItemStack item = i < stash.Items.Count ? stash.Items[i] : null;
            BindSlot(i, item, onClick, onRightClick);
            slots[i].GetComponent<InventoryItemDrag>()?.Bind(null, -1, null);
        }
    }

    private void BindSlot(int index, ItemStack item, Action<int> onClick = null, Action<int> onRightClick = null)
    {
        Action left = onClick == null ? null : () => onClick(index);
        Action right = onRightClick == null ? null : () => onRightClick(index);
        slots[index].ShowSlotView(item, left, right);
    }

    private void PrepareSlots(int count)
    {
        // 한 번 만든 슬롯은 재사용
        while (slots.Count < count)
        {
            slots.Add(Instantiate(slotPrefab, content));
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (i >= count)
            {
                slots[i].ShowSlotView(null);
                slots[i].GetComponent<InventoryItemDrag>()?.Bind(null, -1, null);
            }

            slots[i].gameObject.SetActive(i < count);
        }
    }
}
