using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A second view of the existing bindings; this component owns no item state.
public sealed class QuickSlotBarView : MonoBehaviour
{
    [Serializable]
    private sealed class SlotUI
    {
        public Image icon;
        public TMP_Text quantityText;
        public QuickSlotDropTarget dropTarget;
    }

    [SerializeField] private QuickSlots quickSlots;
    [SerializeField] private SlotUI[] slots = new SlotUI[4];

    public void Bind(QuickSlots source)
    {
        if (isActiveAndEnabled && quickSlots != null)
            quickSlots.Changed -= Refresh;
        quickSlots = source;
        if (isActiveAndEnabled && quickSlots != null)
            quickSlots.Changed += Refresh;

        for (int i = 0; i < slots.Length; i++)
            if (slots[i]?.dropTarget != null)
                slots[i].dropTarget.Bind(source, i);

        Refresh();
    }

    private void OnEnable() => Bind(quickSlots);
    private void Start() => Refresh();

    private void OnDisable()
    {
        if (quickSlots != null) quickSlots.Changed -= Refresh;
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SlotUI slot = slots[i];
            if (slot == null) continue;
            ItemData item = quickSlots != null ? quickSlots.GetAssignedItem(i) : null;
            if (slot.icon != null)
            {
                slot.icon.sprite = item != null ? item.Icon : null;
                slot.icon.enabled = slot.icon.sprite != null;
            }
            if (slot.quantityText != null)
                slot.quantityText.text = item == null ? string.Empty
                    : quickSlots.GetAssignedQuantity(i).ToString();
        }
    }
}
