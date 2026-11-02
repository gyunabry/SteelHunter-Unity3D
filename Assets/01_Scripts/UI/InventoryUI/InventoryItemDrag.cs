using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField, Min(1f)] private float iconSize = 48f;

    private PlayerInventory playerInventory;
    private ItemData item;
    private ItemStack sourceStack;
    private int sourceIndex = -1;
    private Canvas canvas;
    private Image draggingIcon;

    public PlayerInventory PlayerInventory => playerInventory;
    public ItemData DraggedItem => draggingIcon != null ? item : null;
    public ItemStack DraggedStack => draggingIcon != null ? sourceStack : null;
    public int SourceIndex => draggingIcon != null ? sourceIndex : -1;

    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvas = parentCanvas.rootCanvas;
    }

    public void Bind(PlayerInventory inventory, int index, ItemStack stack)
    {
        ClearDrag();
        playerInventory = inventory;
        sourceIndex = index;
        sourceStack = stack;
        item = stack?.Data;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        GetComponent<ItemSlotView>()?.ResetClickSequence();

        if (playerInventory == null || item == null || canvas == null)
            return;

        if (sourceIndex < 0 || sourceIndex >= playerInventory.SlotCount ||
            !ReferenceEquals(playerInventory.GetItem(sourceIndex), sourceStack))
            return;

        ClearDrag();
        eventData.eligibleForClick = false;

        GameObject iconObject = new GameObject(
            "Dragging Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(canvas.transform, false);
        iconObject.transform.SetAsLastSibling();

        draggingIcon = iconObject.GetComponent<Image>();
        draggingIcon.sprite = item.Icon;
        draggingIcon.enabled = item.Icon != null;
        draggingIcon.preserveAspect = true;
        draggingIcon.raycastTarget = false;
        draggingIcon.rectTransform.sizeDelta = Vector2.one * iconSize;

        iconObject.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingIcon != null)
            draggingIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ClearDrag();
    }

    private void OnDisable()
    {
        ClearDrag();
    }

    private void ClearDrag()
    {
        if (draggingIcon != null)
            Destroy(draggingIcon.gameObject);

        draggingIcon = null;
    }
}
