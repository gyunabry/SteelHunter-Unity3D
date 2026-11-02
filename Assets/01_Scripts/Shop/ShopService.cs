using System;

// Quotes are issued by one service and consumed only while both source versions match.
public sealed class ShopQuote
{
    internal ShopService Owner { get; }
    internal ItemStack Source { get; }
    internal AmmoData LoadedAmmo { get; }
    internal int LoadedRounds { get; }
    public bool IsPurchase { get; }
    public int Index { get; }
    public ItemData Item { get; }
    public long UnitPrice { get; }
    public int MaxQuantity { get; }
    public int BundleSize { get; }
    public int InitialStock { get; }
    public long ShopVersion { get; }
    public long InventoryVersion { get; }

    internal ShopQuote(ShopService owner, bool buy, int index, ItemData item,
        long price, int max, long shopVersion, long inventoryVersion, ItemStack source,
        int bundleSize = 1, int initialStock = 0)
    {
        Owner = owner; IsPurchase = buy; Index = index; Item = item;
        UnitPrice = price; MaxQuantity = max; ShopVersion = shopVersion;
        InventoryVersion = inventoryVersion; Source = source;
        BundleSize = buy ? Math.Max(1, bundleSize) : 1;
        InitialStock = buy ? initialStock : 0;
        LoadedAmmo = source?.Weapon?.LoadedAmmo;
        LoadedRounds = source?.Weapon?.LoadedRounds ?? 0;
    }
}

public sealed class ShopService
{
    private readonly ShopState shop;
    private readonly ShopWallet wallet;
    private readonly PlayerInventory inventory;
    private readonly Func<bool> canAccess;
    private bool committing;

    public ShopService(ShopState shop, ShopWallet wallet, PlayerInventory inventory,
        Func<bool> canAccess)
    {
        this.shop = shop ?? throw new ArgumentNullException(nameof(shop));
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        this.inventory = inventory != null ? inventory : throw new ArgumentNullException(nameof(inventory));
        this.canAccess = canAccess ?? throw new ArgumentNullException(nameof(canAccess));
    }

    public static long SaleUnitPrice(ItemStack item)
    {
        if (item == null || !item.Data.CanSell) return 0;
        long price = item.Data.SellPrice;
        if (item.Weapon?.LoadedAmmo != null && item.Weapon.LoadedRounds > 0)
            price += (long)Math.Max(0, item.Weapon.LoadedAmmo.SellPrice) * item.Weapon.LoadedRounds;
        return price;
    }

    public ShopQuote Quote(bool purchase, int index)
    {
        if (!canAccess() || committing) return null;
        if (purchase)
        {
            if (index < 0 || index >= shop.Offers.Count) return null;
            ShopOffer offer = shop.Offers[index];
            // Sold-out offers can still be inspected in the detail panel.
            if (!offer.Item.CanBuy) return null;
            return new ShopQuote(this, true, index, offer.Item, offer.UnitPrice,
                offer.Stock, shop.Version, inventory.Version, null,
                offer.BundleSize, offer.InitialStock);
        }
        if (index < 0 || index >= inventory.SlotCount) return null;
        ItemStack source = inventory.GetItem(index);
        long price = SaleUnitPrice(source);
        if (price <= 0) return null;
        return new ShopQuote(this, false, index, source.Data, price,
            source.Quantity, shop.Version, inventory.Version, source);
    }

    public bool IsCurrent(ShopQuote quote)
    {
        if (quote == null || quote.Owner != this || !canAccess() ||
            quote.ShopVersion != shop.Version || quote.InventoryVersion != inventory.Version)
            return false;
        if (quote.IsPurchase)
            return quote.Index >= 0 && quote.Index < shop.Offers.Count &&
                shop.Offers[quote.Index].Item == quote.Item && quote.Item.CanBuy &&
                shop.Offers[quote.Index].UnitPrice == quote.UnitPrice &&
                shop.Offers[quote.Index].BundleSize == quote.BundleSize;
        if (quote.Index < 0 || quote.Index >= inventory.SlotCount) return false;
        ItemStack source = inventory.GetItem(quote.Index);
        return ReferenceEquals(source, quote.Source) && SaleUnitPrice(source) == quote.UnitPrice &&
            source?.Weapon?.LoadedAmmo == quote.LoadedAmmo &&
            (source?.Weapon?.LoadedRounds ?? 0) == quote.LoadedRounds;
    }

    public bool TryTrade(ShopQuote quote, int quantity, out string message)
    {
        message = null;
        if (committing || !IsCurrent(quote))
            return Fail("상품 또는 가방이 변경되었습니다. 아이템을 다시 선택해 주세요.", out message);
        if (quantity <= 0 || quantity > quote.MaxQuantity)
            return Fail("거래 수량을 확인해 주세요.", out message);
        long total;
        try { total = checked(quote.UnitPrice * quantity); }
        catch (OverflowException) { return Fail("거래 금액이 너무 큽니다.", out message); }
        if (total <= 0 || total > int.MaxValue)
            return Fail("거래 금액이 너무 큽니다.", out message);
        ItemStack[] next;
        int nextBalance;
        if (quote.IsPurchase)
        {
            if (shop.Offers[quote.Index].Stock < quantity)
                return Fail("재고가 부족합니다.", out message);
            if (wallet.Balance < total) return Fail("재화가 부족합니다.", out message);
            // quantity counts bundles; only the inventory receives individual items.
            int grantedQuantity;
            try { grantedQuantity = checked(quantity * quote.BundleSize); }
            catch (OverflowException) { return Fail("구매 수량이 너무 큽니다.", out message); }
            if (!inventory.TryPreparePurchase(quote.Item, grantedQuantity, out next))
                return Fail("가방 공간 또는 허용 무게가 부족합니다.", out message);
            nextBalance = wallet.Balance - (int)total;
        }
        else
        {
            if ((long)wallet.Balance + total > int.MaxValue)
                return Fail("보유 가능한 재화 한도를 초과합니다.", out message);
            if (!inventory.TryPrepareRemoveQuantity(quote.Index, quantity, out next))
                return Fail("판매할 아이템이 부족합니다.", out message);
            nextBalance = wallet.Balance + (int)total;
        }
        committing = true;
        try
        {
            inventory.ReplaceSlotsSilently(next);
            wallet.SetSilently(nextBalance);
            if (quote.IsPurchase) shop.ConsumeSilently(quote.Index, quantity);
            // Observers see the complete transaction; reentrant trades are rejected.
            inventory.PublishChanged();
            wallet.PublishChanged();
            shop.PublishChanged();
        }
        finally { committing = false; }
        message = quote.IsPurchase ? "구매했습니다." : "판매했습니다.";
        return true;
    }

    private static bool Fail(string reason, out string message)
    { message = reason; return false; }
}
