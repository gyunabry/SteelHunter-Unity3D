using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShopOffer
{
    public ItemData Item { get; }
    public int Stock { get; internal set; }
    public int InitialStock { get; }
    public int BundleSize { get; }
    // Price per purchase bundle; ItemData.BuyPrice remains the price per item.
    public long UnitPrice { get; }
    internal ShopOffer(ItemData item, int stock, int bundleSize = 1)
    {
        Item = item;
        Stock = stock;
        InitialStock = stock;
        BundleSize = Math.Max(1, bundleSize);
        UnitPrice = (long)item.BuyPrice * BundleSize;
    }
}

// Session-owned state. No scene objects or Unity clock are retained here.
public sealed class ShopState
{
    private readonly List<ShopOffer> offers = new();
    private readonly System.Random random;
    public ShopData Data { get; }
    public IReadOnlyList<ShopOffer> Offers { get; }
    public long Version { get; private set; }
    public double RemainingSeconds { get; private set; }
    public event Action Changed;

    public ShopState(ShopData data, System.Random random = null)
    {
        Data = data != null ? data : throw new ArgumentNullException(nameof(data));
        this.random = random ?? new System.Random();
        Offers = offers.AsReadOnly();
        Refresh();
    }

    public void Advance(double seconds, GameState state)
    {
        if (state != GameState.Hangar && state != GameState.Battle) return;
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
        RemainingSeconds -= seconds;
        if (RemainingSeconds > 0) return;
        double overdue = -RemainingSeconds;
        Refresh();
        RemainingSeconds -= overdue % Interval;
    }

    private double Interval => float.IsNaN(Data.refreshSeconds) ||
        float.IsInfinity(Data.refreshSeconds) ? 600 : Math.Max(1, Data.refreshSeconds);

    private static bool Valid(ShopData.Entry entry) => entry != null &&
        entry.item != null && entry.item.CanBuy && entry.minQuantity > 0 &&
        entry.maxQuantity >= entry.minQuantity;

    private void Refresh()
    {
        offers.Clear();
        var used = new HashSet<ItemData>();
        foreach (ShopData.Entry entry in Data.fixedItems ?? Array.Empty<ShopData.Entry>())
            if (Valid(entry) && used.Add(entry.item)) Add(entry);
        var candidates = new List<ShopData.Entry>();
        foreach (ShopData.Entry entry in Data.randomItems ?? Array.Empty<ShopData.Entry>())
            if (Valid(entry) && entry.weight > 0 && !float.IsInfinity(entry.weight) &&
                used.Add(entry.item)) candidates.Add(entry);
        int count = Math.Min(Math.Max(0, Data.randomCount), candidates.Count);
        for (int i = 0; i < count; i++)
        {
            double total = 0;
            foreach (var entry in candidates) total += entry.weight;
            double roll = random.NextDouble() * total;
            int selected = candidates.Count - 1;
            for (int j = 0; j < candidates.Count; j++)
            {
                roll -= candidates[j].weight;
                if (roll < 0) { selected = j; break; }
            }
            Add(candidates[selected]);
            candidates.RemoveAt(selected);
        }
        RemainingSeconds = Interval;
        Version++;
        Changed?.Invoke();
    }

    private void Add(ShopData.Entry entry)
    {
        int quantity = (int)(entry.minQuantity + Math.Floor(random.NextDouble() *
            ((double)entry.maxQuantity - entry.minQuantity + 1)));
        offers.Add(new ShopOffer(entry.item, quantity, entry.bundleSize));
    }

    internal void ConsumeSilently(int index, int quantity)
    {
        offers[index].Stock -= quantity;
        Version++;
    }
    internal void PublishChanged() => Changed?.Invoke();
}

public sealed class ShopWallet
{
    public int Balance { get; private set; }
    public event Action Changed;
    public ShopWallet(int initialBalance) => Balance = Math.Max(0, initialBalance);
    internal void SetSilently(int balance) => Balance = balance;
    internal void PublishChanged() => Changed?.Invoke();
}
