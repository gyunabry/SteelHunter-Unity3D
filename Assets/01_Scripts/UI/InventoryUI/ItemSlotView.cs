using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;

    public Action onLeftClick;
    public Action onRightClick;
    public Action onDoubleClick;

    private ItemStack displayedStack;
    private ItemStack firstClickedStack;
    private int firstPointerId;

    private int firstClickCount;

    private void Awake()
    {
        Hide();
    }

    public void ShowSlotView(ItemStack stack,
        Action onLeftClick = null, Action onRightClick = null, Action onDoubleClick = null)
    {
        if (stack == null || stack.Data == null)
        {
            Hide();
            return;
        }

        if (!ReferenceEquals(displayedStack, stack) ||
            (this.onDoubleClick == null) != (onDoubleClick == null))
            ResetClickSequence();

        displayedStack = stack;
        this.onDoubleClick = onDoubleClick;
        this.onLeftClick = onLeftClick;
        this.onRightClick = onRightClick;

        iconImage.sprite = stack.Data.Icon;
        iconImage.enabled = stack.Data.Icon != null;
        nameText.text = stack.Data.DisplayName;
        quantityText.text = stack.Quantity > 1 ? stack.Quantity.ToString() : string.Empty;
    }

    private void Hide()
    {
        ResetClickSequence();

        displayedStack = null;
        onDoubleClick = null;
        onLeftClick = null;
        onRightClick = null;

        iconImage.sprite = null;
        iconImage.enabled = false;
        nameText.text = string.Empty;
        quantityText.text = string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (onDoubleClick != null)
            {
                // 정상적인 클릭 이벤트만 처리
                if (eventData.clickCount <= 0 || displayedStack == null)
                {
                    ResetClickSequence();
                    return;
                }

                // 이전에 기록한 클릭과 현재 클릭이 동일한 아이템에 대한 클릭인지 검사
                bool activate =
                    firstClickedStack != null &&
                    ReferenceEquals(firstClickedStack, displayedStack) &&
                    firstPointerId == eventData.pointerId &&
                    eventData.clickCount > 1 &&
                    firstClickCount == eventData.clickCount - 1;

                if (activate)
                {
                    // 실행하기 전에 더블클릭 소비
                    ResetClickSequence();
                    onDoubleClick?.Invoke();
                }
                else
                {
                    firstClickedStack = displayedStack;
                    firstPointerId = eventData.pointerId;
                    firstClickCount = eventData.clickCount;
                }
                
            }
            onLeftClick?.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            ResetClickSequence();
            onRightClick?.Invoke();
        }
    }

    public void ResetClickSequence()
    {
        firstClickCount = 0;
        firstClickedStack = null;
        firstPointerId = default;
    }

    private void OnDisable() => ResetClickSequence();
}
