using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShopPresenter : MonoBehaviour
{
    [SerializeField] private InventorySessionBinding binding;
    [SerializeField] private ShopView view;

    private Interactor subscribedInteractor;
    private CharacterHealth health;
    private HangarShop terminal;
    private ShopService service;
    private ShopQuote quote;
    private ShopState shop;
    private ShopWallet wallet;
    private StashInventory stash;
    private bool stashDirty;
    private long shownShopVersion = -1;
    private long shownInventoryVersion = -1;

    public bool IsOpen { get; private set; }

    public int LastClosedFrame { get; private set; } = -1;

    private void OnEnable()
    {
        if (binding == null || view == null || binding.Interactor == null)
        {
            Debug.LogError("ShopPresenter의 Binding, View, Interactor 연결을 확인하세요.", this);
            return;
        }

        subscribedInteractor = binding.Interactor;
        subscribedInteractor.Interacted += Interacted;
        health = binding.GetComponent<CharacterHealth>();
        if (health != null) health.Died += Close;
        view.Selected += Select;
        view.TradeRequested += Trade;
        view.CloseRequested += Close;
    }

    private void OnDisable()
    {
        Close();
        if (subscribedInteractor != null)
            subscribedInteractor.Interacted -= Interacted;
        subscribedInteractor = null;
        if (health != null) health.Died -= Close;
        if (view != null)
        {
            view.Selected -= Select;
            view.TradeRequested -= Trade;
            view.CloseRequested -= Close;
        }
    }

    private bool CanAccess() => IsOpen && terminal != null &&
        terminal.CanAccess(binding) && (health == null || health.IsAlive);

    private void Interacted(IInteractable target)
    {
        if (!(target is HangarShop candidate) || !candidate.CanAccess(binding) ||
            (health != null && !health.IsAlive)) return;

        Close();
        // 기존 화면이 소유하던 입력/커서 상태를 먼저 해제한 뒤 상점을 연다.
        binding.InventoryPresenter.Close();

        InventorySession session = InventorySession.Instance;
        session.EnsureShop(candidate.Data);
        shop = session.Shop;
        wallet = session.Wallet;

        // new StashInventory()를 만들지 않는다. 세션이 가진 실제 창고를 공유한다.
        stash = session.Stash;
        stash.Changed += HandleStashChanged;
        stashDirty = true;
        terminal = candidate;
        quote = null;
        IsOpen = true;

        // 같은 플레이어 가방을 기존 거래 서비스에 전달한다.
        service = new ShopService(shop, wallet, binding.Inventory, CanAccess);
        binding.GetComponent<CharacterGun>()?.CancelReload();
        SetBlocked(true);
        GameCursor.Current.ShowPointer();

        // 먼저 화면을 켜고, 그다음 슬롯을 생성하고 데이터를 넣는다.
        view.Show();
        Refresh();
    }

    private void Update()
    {
        if (!IsOpen) return;

        Keyboard keyboard = Keyboard.current;
        bool closePressed = keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);

        if (!CanAccess() || closePressed)
        {
            Close();
            return;
        }

        if (shownShopVersion != shop.Version ||
            shownInventoryVersion != binding.Inventory.Version)
        {
            // 재입고되거나 가방 칸 내용이 바뀌면 이전 선택으로 거래하지 않는다.
            bool hadSelection = quote != null;
            quote = null;
            Refresh();
        }

        // 가방 Version이 같아도 총기의 장전 상태가 바뀔 수 있다.
        // ShopService가 확인하는 원본 아이템/가격/탄약 상태도 검사한다.
        if (quote != null && !service.IsCurrent(quote))
        {
            quote = null;
            view.SetSelection(null);
        }

        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            view.RequestTradeFromShortcut();
        }

        if (stashDirty)
        {
            stashDirty = false;
            view.SetStashRows(stash);
        }

        // 목록은 변경 시 갱신하고 시간·재화·가방 제목은 최신 값으로 표시한다.
        view.SetHeader(wallet.Balance, shop.RemainingSeconds, binding.Inventory);
    }

    private void Refresh()
    {
        shownShopVersion = shop.Version;
        shownInventoryVersion = binding.Inventory.Version;
        view.SetRows(shop, binding.Inventory);
        view.SetStashRows(stash);
        stashDirty = false;
        view.SetSelection(quote);
        view.SetHeader(wallet.Balance, shop.RemainingSeconds, binding.Inventory);
    }

    private void HandleStashChanged() => stashDirty = true;

    private void Select(bool buy, int index)
    {
        if (!CanAccess()) return;
        quote = service.Quote(buy, index);
        view.SetSelection(quote);
    }

    private void Trade(int quantity)
    {
        if (!CanAccess() || quote == null)
            return;

        // 거래 전에 선택한 상품 정보를 보관한다.
        ShopQuote previousQuote = quote;

        bool success = service.TryTrade(previousQuote, quantity, out string message);

        if (previousQuote.IsPurchase &&
            (success || service.IsCurrent(previousQuote)))
        {
            // 구매 성공: 남은 재고와 최신 버전으로 선택 정보를 다시 만든다.
            // 재화/공간 부족으로 실패: 선택이 유효하면 상세창을 유지한다.
            // 재고가 0이어도 Quote()는 선택 정보를 반환한다.
            quote = service.Quote(true, previousQuote.Index);
        }
        else
        {
            // 판매 후에는 기존처럼 선택을 해제한다.
            // 상품 목록이 바뀌어 무효화된 선택도 해제한다.
            quote = null;
        }

        // 상품 목록, 가방, 재화, 상세창을 최신 상태로 갱신한다.
        Refresh();

        if (!success)
        {
            // 현재는 Console에 표시한다.
            // 실패 안내 UI가 있다면 이 메시지를 전달하면 된다.
            Debug.LogWarning(message, this);
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        LastClosedFrame = Time.frameCount;
        IsOpen = false;

        // 세션 창고는 씬보다 오래 살아 있다. 구독을 해제해야
        // 닫히거나 파괴된 Presenter를 창고 이벤트가 계속 참조하지 않는다.
        if (stash != null) stash.Changed -= HandleStashChanged;
        stash = null;
        stashDirty = false;
        terminal = null;
        quote = null;
        service = null;
        shop = null;
        wallet = null;
        if (view != null) view.Hide();

        // 씬 이동이나 사망으로 닫히는 경우에는 입력 잠금을 유지한다.
        bool keepBlocked = binding == null || !binding.CanUseInventory ||
            (health != null && !health.IsAlive);
        SetBlocked(keepBlocked);
        if (!keepBlocked) GameCursor.Current.ShowCrosshair();
    }

    private void SetBlocked(bool blocked)
    {
        if (binding == null) return;
        binding.GetComponent<PlayerCharacterInput>()?.SetUIBlocked(blocked);
        binding.QuickSlots?.SetUIBlocked(blocked);
        binding.Interactor?.SetBlock(blocked);
    }
}