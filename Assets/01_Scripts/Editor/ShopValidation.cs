using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ShopValidation
{
    private static int checks;
    private static readonly List<UnityEngine.Object> temporary = new();
    [MenuItem("Tools/SteelHunter/Validate Shop Bundles")]
    public static void RunBundles()
    {
        checks = 0;
        try
        {
            ItemData ammo = Item<ItemData>("Bundle ammo", 2, 1);
            Set(ammo, "maxStack", 40);
            ShopData data = ScriptableObject.CreateInstance<ShopData>(); temporary.Add(data);
            ShopData.Entry entry = Entry(ammo, 2);
            entry.bundleSize = 30;
            data.fixedItems = new[] { entry };
            var state = new ShopState(data);
            var wallet = new ShopWallet(1000);
            PlayerInventory inv = Inventory(3, 100);
            var service = new ShopService(state, wallet, inv, () => true);
            ShopQuote quote = service.Quote(true, 0);
            Check(quote.BundleSize == 30 && quote.MaxQuantity == 2 &&
                quote.InitialStock == 2 && quote.UnitPrice == 60, "Bundle quote and price");
            Check(service.TryTrade(quote, 1, out _), "First bundle purchase");
            Check(inv.GetQuantity(ammo) == 30 && wallet.Balance == 940 &&
                state.Offers[0].Stock == 1 && state.Offers[0].InitialStock == 2, "First bundle accounting");
            Reject(service, quote, 1, wallet, inv, state, "Consumed quote rejected");
            Check(service.TryTrade(service.Quote(true, 0), 1, out _), "Second bundle purchase");
            Check(inv.GetQuantity(ammo) == 60 && inv.GetItem(0).Quantity == 40 &&
                inv.GetItem(1).Quantity == 20 && wallet.Balance == 880, "Merge and split stacks");
            quote = service.Quote(true, 0);
            Check(quote != null && quote.MaxQuantity == 0 && quote.InitialStock == 2,
                "Sold-out detail remains inspectable");
            Reject(service, quote, 1, wallet, inv, state, "Sold-out purchase rejected");
            ShopQuote sale = service.Quote(false, 0);
            Check(sale.BundleSize == 1 && sale.UnitPrice == 1 &&
                service.TryTrade(sale, 1, out _) && inv.GetQuantity(ammo) == 59 &&
                wallet.Balance == 881, "Sale remains per item");
            state.Advance(600, GameState.Hangar);
            Check(state.Offers[0].Stock == 2 && state.Offers[0].InitialStock == 2,
                "Refresh restores bundle stock");
            Check(service.TryTrade(service.Quote(true, 0), 2, out _) &&
                inv.GetQuantity(ammo) == 119 && wallet.Balance == 761 && state.Offers[0].Stock == 0,
                "Multiple bundles use bundle stock and item quantity");
            state.Advance(600, GameState.Hangar);
            var poor = new ShopWallet(59);
            var poorService = new ShopService(state, poor, inv, () => true);
            Reject(poorService, poorService.Quote(true, 0), 1, poor, inv, state, "Bundle unaffordable");
            PlayerInventory small = Inventory(1, 100);
            small.TryAddItem(new ItemStack(ammo, 20));
            var smallService = new ShopService(state, wallet, small, () => true);
            Reject(smallService, smallService.Quote(true, 0), 1, wallet, small, state,
                "Partial bundle space rejects entire purchase");
            Check(small.GetQuantity(ammo) == 20, "Failed bundle leaves existing stack unchanged");
            Set(ammo, "unitWeight", 1f);
            PlayerInventory limited = Inventory(3, 29);
            var limitedService = new ShopService(state, wallet, limited, () => true);
            Reject(limitedService, limitedService.Quote(true, 0), 1, wallet, limited, state,
                "Weight checks all thirty items");
            entry.bundleSize = 0;
            var legacy = new ShopState(data);
            Check(legacy.Offers[0].BundleSize == 1 && legacy.Offers[0].UnitPrice == 2,
                "Legacy zero bundle size falls back to single item");
            entry.bundleSize = int.MaxValue;
            var huge = new ShopState(data);
            Check(huge.Offers[0].UnitPrice == (long)int.MaxValue * 2, "Bundle price avoids int overflow");
            var hugeService = new ShopService(huge, wallet, inv, () => true);
            Reject(hugeService, hugeService.Quote(true, 0), 1, wallet, inv, huge, "Oversize bundle rejected");
            ValidateBundleUI(service.Quote(true, 0), quote, sale);
            Debug.Log($"Shop bundle validation passed: {checks} checks.");
        }
        finally
        {
            foreach (UnityEngine.Object obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            temporary.Clear();
        }
    }

    private static void ValidateBundleUI(ShopQuote available, ShopQuote soldOut, ShopQuote sale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/UI/ShopItemSlot.prefab");
        var slot = new SerializedObject(prefab.GetComponent<ShopSlotView>());
        var bundleText = slot.FindProperty("bundleCountText").objectReferenceValue as TMPro.TMP_Text;
        Check(bundleText != null && bundleText.name == "BundleCount_Text", "Prefab bundle text binding");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(ShopSetup.ScenePath);
        try
        {
            ShopView view = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                view = root.GetComponentInChildren<ShopView>(true);
                if (view != null) break;
            }
            Check(view != null, "Scene shop view");
            var serialized = new SerializedObject(view);
            var stock = serialized.FindProperty("detailStockText").objectReferenceValue as TMPro.TMP_Text;
            var button = serialized.FindProperty("tradeButton").objectReferenceValue as UnityEngine.UI.Button;
            Check(stock != null && button != null, "Detail stock and button binding");
            view.SetSelection(available);
            Check(stock.text == "재고: 2 / 2" && stock.gameObject.activeSelf && button.interactable,
                "Available bundle detail");
            view.SetSelection(soldOut);
            Check(stock.text == "재고: 0 / 2" && !button.interactable, "Sold-out detail disables purchase");
            view.SetSelection(sale);
            Check(!stock.gameObject.activeSelf && button.interactable, "Sale hides shop stock");
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }

    [MenuItem("Tools/SteelHunter/Validate Shop Transactions")]
    public static void Run()
    {
        checks = 0;
        try
        {
            ItemData ammo = Item<ItemData>("Ammo", 2, 1);
            ItemData expensive = Item<ItemData>("Expensive", 2000, 1000);
            FirearmData gun = Item<FirearmData>("Gun", 100, 50);
            AmmoData loaded = Item<AmmoData>("Loaded", 4, 2);
            ShopData data = ScriptableObject.CreateInstance<ShopData>(); temporary.Add(data);
            data.fixedItems = new[] { Entry(ammo, 120), Entry(gun, 3), Entry(expensive, 1) };
            data.randomItems = new[] { Entry(ammo, 10), Entry(gun, 1), Entry(loaded, 30), Entry(loaded, 5) };
            var state = new ShopState(data, new System.Random(1));
            Check(state.Offers.Count == 4, "No duplicates and short random pool");
            PlayerInventory inv = Inventory(20, 40);
            var wallet = new ShopWallet(1000);
            bool access = true;
            var service = new ShopService(state, wallet, inv, () => access);
            ShopQuote buy = service.Quote(true, 0);
            Check(service.TryTrade(buy, 10, out _), "Purchase");
            Check(wallet.Balance == 980 && inv.GetQuantity(ammo) == 10 && state.Offers[0].Stock == 110, "Purchase accounting");
            Check(!service.TryTrade(buy, 10, out _) && wallet.Balance == 980, "Duplicate quote rejected");
            ShopQuote sell = service.Quote(false, 0);
            Check(service.TryTrade(sell, 4, out _), "Partial sale");
            Check(wallet.Balance == 984 && inv.GetQuantity(ammo) == 6, "Partial sale accounting");
            Reject(service, service.Quote(true, 2), 1, wallet, inv, state, "Insufficient balance");
            Reject(service, service.Quote(true, 0), 0, wallet, inv, state, "Zero quantity");
            Reject(service, service.Quote(true, 0), -1, wallet, inv, state, "Negative quantity");
            Reject(service, service.Quote(true, 0), 121, wallet, inv, state, "Insufficient stock");
            buy = service.Quote(true, 0);
            access = false; Reject(service, buy, 1, wallet, inv, state, "Access denied"); access = true;
            buy = service.Quote(true, 0);
            state.Advance(600, GameState.Hangar);
            Reject(service, buy, 1, wallet, inv, state, "Expired stock quote");
            Check(state.Offers[0].Stock == 120, "Stock replaced not accumulated");
            state.Advance(10, GameState.Battle);
            double time = state.RemainingSeconds;
            state.Advance(99, GameState.Paused); state.Advance(99, GameState.Result); state.Advance(99, GameState.Loading);
            Check(state.RemainingSeconds == time && time == 590, "Clock state gating");
            state.Advance(1210, GameState.Hangar);
            Check(state.RemainingSeconds == 580, "Large elapsed time preserves remainder");
            Check(service.TryTrade(service.Quote(true, 1), 2, out _), "Multiple equipment purchase");
            ItemStack firstGun = inv.GetItem(1), secondGun = inv.GetItem(2);
            Check(firstGun.InstanceId != secondGun.InstanceId && firstGun.Weapon.LoadedRounds == 0, "Fresh equipment IDs and empty magazines");
            typeof(WeaponInstance).GetProperty("LoadedAmmo").SetValue(firstGun.Weapon, loaded);
            typeof(WeaponInstance).GetProperty("LoadedRounds").SetValue(firstGun.Weapon, 7);
            sell = service.Quote(false, 1);
            Check(sell.UnitPrice == 64, "Loaded ammo included");
            int before = wallet.Balance;
            Check(service.TryTrade(sell, 1, out _) && wallet.Balance == before + 64 && inv.GetItem(1) == null, "Loaded gun removed with ammo");
            sell = service.Quote(false, 2);
            typeof(WeaponInstance).GetProperty("LoadedRounds").SetValue(secondGun.Weapon, 1);
            Reject(service, sell, 1, wallet, inv, state, "Weapon state stale quote");
            ItemData unsellable = Item<ItemData>("Quest", 2, 0);
            inv.TryAddItem(new ItemStack(unsellable, 1));
            Check(service.Quote(false, 1) == null, "Zero price cannot be sold");
            sell = service.Quote(false, 0);
            inv.TryConsume(ammo, 1);
            Reject(service, sell, 1, wallet, inv, state, "Inventory changed quote");
            var rich = new ShopWallet(int.MaxValue);
            var overflowService = new ShopService(state, rich, inv, () => true);
            Reject(overflowService, overflowService.Quote(false, 0), 1, rich, inv, state, "Wallet overflow");
            Set(ammo, "buyPrice", int.MaxValue); Set(ammo, "sellPrice", 1);
            var costly = new ShopState(data);
            var costService = new ShopService(costly, rich, inv, () => true);
            Reject(costService, costService.Quote(true, 0), 2, rich, inv, costly, "Total overflow");
            Set(ammo, "buyPrice", 2);
            PlayerInventory full = Inventory(1, 40);
            full.TryAddItem(new ItemStack(gun, 1));
            var fullService = new ShopService(state, wallet, full, () => true);
            Reject(fullService, fullService.Quote(true, 0), 1, wallet, full, state, "No empty slot");
            Set(ammo, "unitWeight", 1f);
            PlayerInventory limited = Inventory(2, 10);
            var limitedService = new ShopService(state, wallet, limited, () => true);
            Reject(limitedService, limitedService.Quote(true, 0), 10, wallet, limited, state, "Exact weight limit");
            Check(limitedService.TryTrade(limitedService.Quote(true, 0), 9, out _), "Below weight limit");
            bool observerConsistent = false, reentryRejected = false;
            buy = service.Quote(true, 0); before = wallet.Balance;
            int previousQuantity = inv.GetQuantity(ammo), stockBefore = state.Offers[0].Stock;
            inv.Changed += () => {
                observerConsistent = wallet.Balance == before - 2 && inv.GetQuantity(ammo) == previousQuantity + 1 && state.Offers[0].Stock == stockBefore - 1;
                reentryRejected = !service.TryTrade(buy, 1, out _);
            };
            Check(service.TryTrade(buy, 1, out _) && observerConsistent && reentryRejected, "Atomic observers and reentry protection");
            var fresh = new ShopState(data);
            Check(new ShopWallet(data.initialBalance).Balance == 1000 && fresh.Offers[0].Stock == 120 && fresh.RemainingSeconds == 600, "New session defaults");
            Debug.Log($"Shop validation passed: {checks} checks.");
        }
        finally
        {
            foreach (UnityEngine.Object obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            temporary.Clear();
        }
    }
    private static void Reject(ShopService service, ShopQuote quote, int quantity, ShopWallet wallet,
        PlayerInventory inv, ShopState shop, string name)
    {
        int balance = wallet.Balance; long version = inv.Version, shopVersion = shop.Version;
        Check(!service.TryTrade(quote, quantity, out _) && balance == wallet.Balance && version == inv.Version && shopVersion == shop.Version, name);
    }
    private static PlayerInventory Inventory(int count, float maxWeight)
    {
        GameObject obj = new GameObject("ShopValidation"); temporary.Add(obj);
        PlayerInventory inv = obj.AddComponent<PlayerInventory>();
        Set(inv, "baseSlotCount", count); Set(inv, "baseMaxWeight", maxWeight);
        typeof(PlayerInventory).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inv, null);
        return inv;
    }
    private static T Item<T>(string name, int buy, int sell) where T : ItemData
    {
        T item = ScriptableObject.CreateInstance<T>(); temporary.Add(item); item.name = name;
        Set(item, "buyPrice", buy); Set(item, "sellPrice", sell); Set(item, "canSell", sell > 0);
        return item;
    }
    private static void Set(UnityEngine.Object target, string field, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (value is int i) p.intValue = i;
        else if (value is float f) p.floatValue = f;
        else if (value is bool b) p.boolValue = b;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static ShopData.Entry Entry(ItemData item, int quantity) => new ShopData.Entry {
        item = item, minQuantity = quantity, maxQuantity = quantity, weight = 1 };
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("Shop validation failed: " + name);
        checks++;
    }
}
