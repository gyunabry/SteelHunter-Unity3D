using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    [Header("전체 창")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button closeButton;

    [Header("플레이어 인벤토리")]
    [SerializeField] private TMP_Text inventoryTitleText;
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private EquipmentSlotView[] equipmentSlots;

    [Header("무게/퀵슬롯")]
    [SerializeField] private Image weightFill;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private QuickSlotBarView quickSlotBar;
    [SerializeField] private InventoryHudVisibility hudVisibility;

    [Header("루팅 / 창고 공용 패널")]
    [SerializeField] private GameObject lootPanel;
    [SerializeField] private TMP_Text lootTitleText;
    [SerializeField] private Transform lootContent;

    [Header("슬롯")]
    [SerializeField] private ItemSlotView slotPrefab;

    private readonly List<ItemSlotView> inventorySlots = new();
    private readonly List<ItemSlotView> lootSlots = new();
    private PlayerInventory displayedInventory;
    private LootContainer displayedLoot;
    private StashInventory displayedStash;

    public event Action CloseRequested;
    public event Action<int> InventoryEquipRequested;
    public event Action<int> LootTakeRequested;
    public event Action<int> DepositRequested;
    public event Action<int> WithdrawRequested;
    public event Action<EquipmentSlot> WeaponSelectRequested;
    public event Action<EquipmentSlot> UnequipRequested;
    public event Action<int, ItemStack, EquipmentSlot> EquipRequested;

    private void Awake()
    {
        if (hudVisibility == null)
            hudVisibility = GetComponentInParent<InventoryHudVisibility>(true);

        closeButton.onClick.AddListener(HandleCloseClicked);
        Hide();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    public void BindQuickSlots(QuickSlots source)
    {
        if (quickSlotBar != null) quickSlotBar.Bind(source);
    }

    public void Show(PlayerInventory inventory, LootContainer loot,
        CharacterEquipment equipment, StashInventory stash = null)
    {
        windowRoot.SetActive(true);
        if (hudVisibility != null) hudVisibility.SetInventoryOpen(true);
        Refresh(inventory, loot, equipment, stash);
    }

    public void Refresh(
        PlayerInventory inventory, 
        LootContainer loot,
        CharacterEquipment equipment, 
        StashInventory stash = null)
    {
        if (displayedInventory != inventory || displayedLoot != loot ||
            !ReferenceEquals(displayedStash, stash))
        {
            foreach (ItemSlotView slot in inventorySlots) slot.ResetClickSequence();
            foreach (ItemSlotView slot in lootSlots) slot.ResetClickSequence();
        }

        displayedInventory = inventory;
        displayedLoot = loot;
        displayedStash = stash;

        inventoryTitleText.text = $"가방 ({inventory.OccupiedSlotCount}/{inventory.SlotCount})";

        RefreshWeight(inventory.CurrentWeight, inventory.MaxWeight);

        if (equipmentSlots != null && equipment != null)
        {
            foreach (EquipmentSlotView slot in equipmentSlots)
            {
                if (slot == null) continue;
                EquipmentSlot target = slot.Slot;
                bool active = (target == EquipmentSlot.PrimaryWeapon ||
                    target == EquipmentSlot.SecondaryWeapon) &&
                    equipment.ActiveWeaponSlot == target;
                slot.Bind(inventory, equipment.GetEquipped(target), active,
                    HandleWeaponSelect, HandleUnequip, HandleEquip);
            }
        }

        PrepareSlots(inventorySlots, inventoryContent, inventory.SlotCount);
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            int index = i;

            ItemStack stack = inventory.GetItem(i);
            ItemSlotView slot = inventorySlots[i];

            Action doubleClick = stash != null
                ? () => DepositRequested?.Invoke(index)
                : () => InventoryEquipRequested?.Invoke(index);

            slot.ShowSlotView(stack, onDoubleClick: doubleClick);

            InventoryItemDrag drag = slot.GetComponent<InventoryItemDrag>();

            if (drag != null) drag.Bind(inventory, i, stack);
        }

        bool showingStash = stash != null;
        bool hasContainer = showingStash || loot != null;
        lootPanel.SetActive(hasContainer);
        if (!hasContainer) return;

        IReadOnlyList<ItemStack> items = showingStash ? stash.Items : loot.Items;
        lootTitleText.text = showingStash ? "창고" : loot.ContainerName;

        int count = showingStash ? Mathf.Max(5, items.Count) : items.Count;
        PrepareSlots(lootSlots, lootContent, count);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            ItemStack item = i < items.Count ? items[i] : null;
            ItemSlotView slot = lootSlots[i];
            Action doubleClick = showingStash
                ? () => WithdrawRequested?.Invoke(index)
                : () => LootTakeRequested?.Invoke(index);
            slot.ShowSlotView(item, onDoubleClick: doubleClick);

            // 창고/루팅 칸을 가방 칸인 것처럼 드래그하지 않게 한다.
            InventoryItemDrag drag = slot.GetComponent<InventoryItemDrag>();
            if (drag != null) drag.Bind(null, -1, null);
        }
    }

    public void Hide()
    {
        windowRoot.SetActive(false);
        if (hudVisibility != null) hudVisibility.SetInventoryOpen(false);
    }

    private void RefreshWeight(float current, float max)
    {
        if (weightFill != null)
        {
            weightFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : current > 0f ? 1f : 0f;
        }

        if (weightText != null)
        {
            weightText.text = $"{current:0.0} / {max:0.0}";
        }
    }

    private void PrepareSlots(List<ItemSlotView> slots,
        Transform parent, int requiredCount)
    {
        while (slots.Count < requiredCount)
            slots.Add(Instantiate(slotPrefab, parent));
        for (int i = 0; i < slots.Count; i++)
            slots[i].gameObject.SetActive(i < requiredCount);
    }

    private void HandleCloseClicked() => CloseRequested?.Invoke();
    private void HandleWeaponSelect(EquipmentSlot slot) => WeaponSelectRequested?.Invoke(slot);
    private void HandleUnequip(EquipmentSlot slot) => UnequipRequested?.Invoke(slot);
    private void HandleEquip(int index, ItemStack stack, EquipmentSlot slot)
        => EquipRequested?.Invoke(index, stack, slot);
}
