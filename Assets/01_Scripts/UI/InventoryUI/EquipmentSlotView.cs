using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class EquipmentSlotView : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private EquipmentSlot slot;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject activeFrame;

    private PlayerInventory inventory;
    private Action<EquipmentSlot> selectRequested;
    private Action<EquipmentSlot> unequipRequested;
    private Action<int, ItemStack, EquipmentSlot> equipRequested;
    private bool hasEquipment;
    private ItemStack displayedStack;
    private ItemStack firstClickedStack;
    private int firstPointerId;

    public EquipmentSlot Slot => slot;

    public void Bind(
        PlayerInventory owner,
        ItemStack equipped,
        bool isActiveWeapon,
        Action<EquipmentSlot> onSelect,
        Action<EquipmentSlot> onUnequip,
        Action<int, ItemStack, EquipmentSlot> onEquip)
    {
        if (inventory != owner || !ReferenceEquals(displayedStack, equipped))
            firstClickedStack = null;
        displayedStack = equipped;
        inventory = owner;
        selectRequested = onSelect;
        unequipRequested = onUnequip;
        equipRequested = onEquip;

        ItemData data = equipped?.Data;
        hasEquipment = data != null;
        if (itemIcon != null)
        {
            itemIcon.sprite = data != null ? data.Icon : null;
            itemIcon.enabled = itemIcon.sprite != null;
        }

        if (itemName != null)
            itemName.text = data != null ? data.DisplayName : string.Empty;

        if (emptyVisual != null) emptyVisual.SetActive(data == null);
        if (activeFrame != null) activeFrame.SetActive(isActiveWeapon && data != null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasEquipment || eventData.button != PointerEventData.InputButton.Left)
        {
            firstClickedStack = null;
            return;
        }

        if (eventData.clickCount == 1)
        {
            firstClickedStack = displayedStack;
            firstPointerId = eventData.pointerId;
            // Preserve immediate weapon selection on the first click.
            if (slot == EquipmentSlot.PrimaryWeapon || slot == EquipmentSlot.SecondaryWeapon)
                selectRequested?.Invoke(slot);
            return;
        }

        bool unequip = eventData.clickCount == 2 && firstClickedStack != null &&
            ReferenceEquals(firstClickedStack, displayedStack) &&
            firstPointerId == eventData.pointerId;
        firstClickedStack = null;
        if (unequip) unequipRequested?.Invoke(slot);
    }

    private void OnDisable() => firstClickedStack = null;

    public void OnDrop(PointerEventData eventData)
    {
        firstClickedStack = null;
        if (inventory == null || eventData.pointerDrag == null ||
            !eventData.pointerDrag.TryGetComponent(out InventoryItemDrag source) ||
            source.PlayerInventory != inventory)
            return;

        int index = source.SourceIndex;
        ItemStack stack = source.DraggedStack;
        if (stack == null || index < 0 || index >= inventory.SlotCount ||
            !ReferenceEquals(inventory.GetItem(index), stack))
            return;

        equipRequested?.Invoke(index, stack, slot);
    }
}
