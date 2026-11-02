using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopView : MonoBehaviour
{
    [Header("상점 화면")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private GameObject inventoryRoot;

    [Header("가방 / 창고")]
    [SerializeField] private ItemGridView bagGrid;
    [SerializeField] private ItemGridView stashGrid;
    [SerializeField] private TMP_Text bagTitleText;

    [Header("상점 상품 목록")]
    [SerializeField] private Transform stockContent;
    [SerializeField] private ShopSlotView offerPrefab;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text refreshTimeText;
    [SerializeField] private Button closeButton;

    [Header("중앙 상품 상세")]
    [SerializeField] private GameObject detailRoot;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailStockText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private Button tradeButton;
    [SerializeField] private TMP_Text tradeButtonText;

    private readonly List<ShopSlotView> offerSlots = new();
    private ShopQuote selected;

    // true: 상점 상품 구매, false: 가방 아이템 판매
    // int: ShopState 또는 PlayerInventory의 실제 인덱스
    public event Action<bool, int> Selected;
    public event Action<int> TradeRequested;
    public event Action CloseRequested;

    private void Awake()
    {
        // Inspector Button.OnClick에는 별도 함수를 넣지 않는다.
        // 버튼과 F키가 모두 RequestTrade를 거쳐 같은 이벤트를 보낸다.
        tradeButton.onClick.AddListener(HandleTradeButton);

        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseButton);

        SetSelection(null);
        Hide();
    }

    private void OnDestroy()
    {
        if (tradeButton != null)
            tradeButton.onClick.RemoveListener(HandleTradeButton);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseButton);
    }

    public void Show()
    {
        shopRoot.SetActive(true);
        inventoryRoot.SetActive(true);

        // 상점을 다시 열었을 때 이전에 선택한 상품이 남지 않게 한다.
        SetSelection(null);
    }

    public void Hide()
    {
        SetSelection(null);
        shopRoot.SetActive(false);
        inventoryRoot.SetActive(false);
    }

    public void SetRows(ShopState shop, PlayerInventory inventory)
    {
        // 만들어 둔 슬롯을 재사용한다.
        while (offerSlots.Count < shop.Offers.Count)
            offerSlots.Add(Instantiate(offerPrefab, stockContent));

        for (int i = 0; i < offerSlots.Count; i++)
        {
            bool visible = i < shop.Offers.Count;
            offerSlots[i].gameObject.SetActive(visible);

            if (!visible)
                continue;

            int index = i;

            // 상품 클릭은 여기서 구매하지 않는다.
            // Presenter에 상품 인덱스를 보내 상세 정보를 먼저 표시한다.
            offerSlots[i].Bind(
                shop.Offers[i],
                () => Selected?.Invoke(true, index)
            );
        }

        // 일반 InventoryUI와 같은 PlayerInventory를 읽는다.
        // 이 화면에서 가방 좌클릭은 판매할 아이템 선택이다.
        bagGrid.ShowInventory(inventory, HandleBagClicked);
    }

    public void SetStashRows(StashInventory stash)
    {
        stashGrid.ShowStash(stash);

        int occupied = 0;
        foreach (ItemStack item in stash.Items)
        {
            if (item != null)
                occupied++;
        }
    }

    public void SetHeader(
        int balance,
        double seconds,
        PlayerInventory inventory)
    {
        balanceText.text = $"$ {balance:N0}";

        double remaining = Math.Ceiling(Math.Max(0, seconds));
        refreshTimeText.text =
            $"다음 새로고침 {Math.Floor(remaining / 60):00}:{remaining % 60:00}";

        bagTitleText.text =
            $"가방 ({inventory.OccupiedSlotCount}/{inventory.SlotCount})  " +
            $"{inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0} kg";
    }

    public void SetSelection(ShopQuote quote)
    {
        selected = quote;

        if (quote == null)
        {
            // 선택이 없거나 거래 후에는 중앙 상세 패널을 숨긴다.
            detailRoot.SetActive(false);
            tradeButton.interactable = false;
            return;
        }

        ItemData item = quote.Item;
        string itemName = string.IsNullOrWhiteSpace(item.DisplayName)
            ? item.name
            : item.DisplayName;

        detailIconImage.sprite = item.Icon;
        detailIconImage.enabled = item.Icon != null;
        detailNameText.text = itemName;
        detailStockText.gameObject.SetActive(quote.IsPurchase);
        detailStockText.text = quote.IsPurchase
            ? $"재고: {quote.MaxQuantity} / {quote.InitialStock}"
            : string.Empty;
        detailsText.text = item.Description;

        // 구매는 한 묶음, 판매는 실제 아이템 한 개 단위다.
        tradeButtonText.text = quote.IsPurchase
            ? (quote.MaxQuantity > 0
                ? $"구매 ×{quote.BundleSize}  ${quote.UnitPrice:N0}"
                : "품절")
            : $"판매 ${quote.UnitPrice:N0}";

        tradeButton.interactable = quote.MaxQuantity > 0;

        // 모든 내용이 준비된 다음 패널을 켠다.
        detailRoot.SetActive(true);
    }

    public void RequestTradeFromShortcut()
    {
        // F키는 상점 상품을 선택했을 때만 구매한다.
        RequestTrade(purchaseOnly: false);
    }

    private void HandleTradeButton()
    {
        // 버튼은 선택 종류에 따라 한 묶음 구매 또는 한 개 판매한다.
        RequestTrade(purchaseOnly: false);
    }

    private void RequestTrade(bool purchaseOnly)
    {
        if (selected == null ||
            !detailRoot.activeInHierarchy ||
            !tradeButton.interactable)
            return;

        if (purchaseOnly && !selected.IsPurchase)
            return;

        // 버튼과 F키가 이 이벤트 하나로 합쳐진다.
        // 실제 거래 검사는 ShopPresenter → ShopService가 수행한다.
        TradeRequested?.Invoke(1);
    }

    private void HandleBagClicked(int index)
    {
        Selected?.Invoke(false, index);
    }

    private void HandleCloseButton()
    {
        CloseRequested?.Invoke();
    }
}
