using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InventoryDoubleClickValidation
{
    [MenuItem("Tools/SteelHunter/Validate Inventory Double Click")]
    public static void Run()
    {
        var root = new GameObject("Double Click Validation");
        root.SetActive(false);
        var data = ScriptableObject.CreateInstance<TestUsableItemData>();
        try
        {
            ItemStack first = new ItemStack(data, 1);
            ItemStack second = new ItemStack(data, 1);
            ItemSlotView slot = MakeSlot(root.transform);
            int activated = 0;
            Action activate = () => activated++;
            slot.ShowSlotView(first, onDoubleClick: activate);
            Click(slot, 1);
            Check(activated == 0, "Single click must not activate");
            slot.ShowSlotView(first, onDoubleClick: activate);
            Click(slot, 2);
            Click(slot, 3);
            Click(slot, 4);
            Check(activated == 1, "Same-item refresh preserves double click; extra clicks do not repeat");
            Click(slot, 1);
            slot.ShowSlotView(second, onDoubleClick: activate);
            Click(slot, 2);
            Check(activated == 1, "Replacement item must not inherit first click");
            Click(slot, 1);
            Click(slot, 2, pointer: 42);
            Check(activated == 1, "Different pointer must not complete double click");
            Click(slot, 1);
            slot.SendMessage("OnDisable");
            Click(slot, 2);
            Check(activated == 1, "Closing the slot cancels first click");
            Click(slot, 1);
            slot.gameObject.AddComponent<InventoryItemDrag>().OnBeginDrag(
                new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            Click(slot, 2);
            Check(activated == 1, "Drag cancels first click");
            slot.ShowSlotView(first, onLeftClick: activate);
            Click(slot, 1);
            Check(activated == 2, "Shop single click remains available");

            var inventory = root.AddComponent<PlayerInventory>();
            Set(inventory, "slots", new ItemStack[] { first });
            var stash = new StashInventory();
            var view = root.AddComponent<InventoryView>();
            Set(view, "inventoryTitleText", Child<TextMeshProUGUI>(root.transform));
            Set(view, "lootTitleText", Child<TextMeshProUGUI>(root.transform));
            Set(view, "inventoryContent", Child<RectTransform>(root.transform));
            Set(view, "lootContent", Child<RectTransform>(root.transform));
            Set(view, "lootPanel", Child<RectTransform>(root.transform).gameObject);
            Set(view, "slotPrefab", slot);
            int equips = 0;
            view.InventoryEquipRequested += _ => equips++;
            view.DepositRequested += index => Check((bool)typeof(StashInventory)
                .GetMethod("TryDeposit", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stash, new object[] { inventory, index, null }), "Deposit succeeds");
            view.WithdrawRequested += index => Check((bool)typeof(StashInventory)
                .GetMethod("TryWithdraw", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stash, new object[] { index, inventory, null }), "Withdraw succeeds");
            view.Refresh(inventory, null, null, stash);
            ItemSlotView bagSlot = Get<List<ItemSlotView>>(view, "inventorySlots")[0];
            Click(bagSlot, 1);
            Check(inventory.GetItem(0) == first, "First bag click does not move item");
            Click(bagSlot, 2);
            Check(inventory.GetItem(0) == null && stash.Items[0]?.Data == data && equips == 0,
                "Stash UI double click deposits instead of equipping");
            view.Refresh(inventory, null, null, stash);
            ItemSlotView stashSlot = Get<List<ItemSlotView>>(view, "lootSlots")[0];
            Click(stashSlot, 1);
            Click(stashSlot, 2);
            Check(inventory.GetItem(0) != null && stash.Items[0] == null,
                "Stash double click withdraws to inventory");
            view.Refresh(inventory, null, null, stash);
            Click(bagSlot, 1);
            view.Refresh(inventory, null, null);
            Click(bagSlot, 2);
            Check(equips == 0, "Changing screen context cancels first click");
            Click(bagSlot, 1);
            Click(bagSlot, 2);
            Check(equips == 1, "Normal inventory double click requests equip");

            var equipmentSlot = Child<EquipmentSlotView>(root.transform);
            int unequips = 0;
            equipmentSlot.Bind(inventory, first, false, null, _ => unequips++, null);
            equipmentSlot.OnPointerClick(new PointerEventData(null)
                { button = PointerEventData.InputButton.Right, clickCount = 1 });
            Click(equipmentSlot, 1);
            Check(unequips == 0, "Right/single click do not unequip");
            Click(equipmentSlot, 2);
            Click(equipmentSlot, 3);
            Check(unequips == 1, "Double click unequips exactly once");
            Debug.Log("Inventory double-click validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static ItemSlotView MakeSlot(Transform parent)
    {
        ItemSlotView slot = Child<ItemSlotView>(parent);
        Set(slot, "iconImage", Child<Image>(slot.transform));
        Set(slot, "nameText", Child<TextMeshProUGUI>(slot.transform));
        Set(slot, "quantityText", Child<TextMeshProUGUI>(slot.transform));
        return slot;
    }

    private static T Child<T>(Transform parent) where T : Component
    {
        var child = new GameObject(typeof(T).Name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>() ?? child.AddComponent<T>();
    }

    private static void Click(IPointerClickHandler slot, int count, int pointer = 1)
        => slot.OnPointerClick(new PointerEventData(null)
        { button = PointerEventData.InputButton.Left, clickCount = count, pointerId = pointer });

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);

    private static T Get<T>(object target, string field)
        => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(target);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
