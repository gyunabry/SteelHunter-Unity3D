using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuickSlots : MonoBehaviour
{
    [Serializable]
    private class Slot
    {
        public ItemData item;
        public Image icon;
        public TMP_Text quantityText;
    }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Slot[] slots = new Slot[4];

    private static readonly Key[] Keys =
    {
        Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6
    };

    private bool started;
    private bool uiBlocked;
    private int uiClosedFrame = -1;

    public event Action Changed;

    public ItemData GetAssignedItem(int slotIndex)
    {
        if (slots == null || slotIndex < 0 ||
            slotIndex >= slots.Length || slotIndex >= Keys.Length)
            return null;

        return slots[slotIndex]?.item;
    }

    public int GetAssignedQuantity(int slotIndex)
    {
        ItemData item = GetAssignedItem(slotIndex);
        return item == null || inventory == null || inventory.SlotCount == 0
            ? 0 : inventory.GetQuantity(item);
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.Changed += Refresh;

        if (started)
            Refresh();
    }

    private void Start()
    {
        started = true;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;
    }

    private void LateUpdate()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || inventory == null ||
            uiBlocked || Time.frameCount == uiClosedFrame ||
            Time.timeScale <= 0f)
        {
            return;
        }

        int count = Mathf.Min(slots.Length, Keys.Length);

        for (int i = 0; i < count; i++)
        {
            if (!keyboard[Keys[i]].wasPressedThisFrame)
                continue;

            Slot slot = slots[i];

            if (slot != null)
                inventory.TryUseItem(slot.item);

            // Process only the lowest pressed slot number per frame.
            break;
        }
    }

    public void SetUIBlocked(bool blocked)
    {
        if (uiBlocked == blocked)
            return;

        uiBlocked = blocked;

        if (!blocked)
            uiClosedFrame = Time.frameCount;
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null)
                continue;

            ItemData item = slot.item;

            if (slot.icon != null)
            {
                slot.icon.sprite = item != null ? item.Icon : null;
                slot.icon.enabled = item != null && item.Icon != null;
            }

            if (slot.quantityText != null)
            {
                slot.quantityText.text = item == null || inventory == null
                    ? string.Empty
                    : GetAssignedQuantity(i).ToString();
            }
        }

        Changed?.Invoke();
    }

    public bool TryAssign(int slotIndex, PlayerInventory sourceInventory, ItemData item)
    {
        if (inventory == null || sourceInventory != inventory || item == null)
            return false;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length || slotIndex >= Keys.Length)
            return false;
        if (inventory.SlotCount == 0 || inventory.GetQuantity(item) <= 0)
            return false;

        if (slots[slotIndex] == null) slots[slotIndex] = new Slot();
        slots[slotIndex].item = item;
        Refresh();
        return true;
    }

    internal ItemData[] CopyBindings()
    {
        ItemData[] copy = new ItemData[4];
        for (int i = 0; i < copy.Length; i++)
            copy[i] = slots != null && i < slots.Length ? slots[i]?.item : null;
        return copy;
    }

    internal void RestoreBindingsSilently(ItemData[] items)
    {
        if (slots == null || slots.Length != 4)
            Array.Resize(ref slots, 4);

        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == null) slots[i] = new Slot();
            slots[i].item = items[i];
        }
    }

    internal void RefreshAfterRestore()
    {
        Refresh();
    }
}
