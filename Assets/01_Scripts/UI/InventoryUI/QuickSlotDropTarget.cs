using UnityEngine;
using UnityEngine.EventSystems;

public class QuickSlotDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private QuickSlots quickSlots;
    [SerializeField, Range(0, 3)] private int slotIndex;

    public void Bind(QuickSlots source, int index)
    {
        quickSlots = source;
        slotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (quickSlots == null || eventData == null) return;

        GameObject draggedObject = eventData.pointerDrag;

        if (draggedObject == null) return;

        if (!draggedObject.TryGetComponent(
                out InventoryItemDrag source))
        {
            return;
        }

        ItemData item = source.DraggedItem;

        if (item == null)
            return;

        quickSlots.TryAssign(
            slotIndex,
            source.PlayerInventory,
            item);
    }
}
