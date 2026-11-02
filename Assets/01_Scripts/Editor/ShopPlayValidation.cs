using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Opt-in integration test: real scene transitions with the scene's UI and binding.
[InitializeOnLoad]
public static class ShopPlayValidation
{
    private const string PhaseKey = "SteelHunter.ShopValidation.Phase";
    private const string Result = "Temp/ShopPlayValidation.result";
    static ShopPlayValidation() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        int phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0)
        {
            if (!File.Exists("Temp/ShopPlayValidation.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Temp/ShopPlayValidation.request");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ShopSetup.ScenePath ||
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            { File.WriteAllText(Result, "FAIL: open and save HangarScene first."); return; }
            SessionState.SetString("SteelHunter.ShopValidation.Start", DateTime.UtcNow.ToString("O"));
            SessionState.SetInt(PhaseKey, 1);
            EditorApplication.isPlaying = true;
            return;
        }
        if (phase == 9)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) SessionState.SetInt(PhaseKey, 0);
            return;
        }
        try
        {
            if ((DateTime.UtcNow - DateTime.Parse(SessionState.GetString("SteelHunter.ShopValidation.Start", ""))).TotalSeconds > 120)
                throw new Exception("Timed out waiting for scene initialization.");
            if (!EditorApplication.isPlaying) return;
            InventorySessionBinding binding = InventorySessionBinding.Current;
            if (binding == null || !binding.IsReady) return;
            InventorySession session = InventorySession.Instance;
            if (phase == 1 && session.State == GameState.Hangar)
            {
                Check(session.Wallet.Balance == 1000, "Initial wallet");
                HangarShop terminal = UnityEngine.Object.FindFirstObjectByType<HangarShop>();
                ItemData item = session.Shop.Offers[0].Item;
                SessionState.SetInt("SteelHunter.ShopValidation.Quantity", binding.Inventory.GetQuantity(item));
                Open(binding, terminal);
                ShopPresenter presenter = UnityEngine.Object.FindFirstObjectByType<ShopPresenter>();
                ShopView view = presenter.GetComponent<ShopView>();
                Click(view, "ShopWindow/Stock/Content/Item");
                view.transform.Find("ShopWindow/Quantity").GetComponent<InputField>().text = "10";
                Click(view, "ShopWindow/Trade");
                Check(session.Wallet.Balance == 980 && session.Shop.Offers[0].Stock == 110, "UI purchase");
                Check(binding.Inventory.GetQuantity(item) == SessionState.GetInt("SteelHunter.ShopValidation.Quantity", 0) + 10, "Bag purchase");
                // Force a refresh while selected; the old button must not purchase a new offer.
                Click(view, "ShopWindow/Stock/Content/Item");
                session.Shop.Advance(600, GameState.Hangar);
                Click(view, "ShopWindow/Trade");
                Check(session.Wallet.Balance == 980 && session.Shop.Offers[0].Stock == 120, "UI stale quote rejected");
                presenter.Close();
                Check(!binding.IsShopOpen, "Close releases UI ownership");
                SessionState.SetInt(PhaseKey, 2);
                SceneLoader.Instance.EnterBattleField();
            }
            else if (phase == 2 && session.State == GameState.Battle)
            {
                Check(session.Wallet.Balance == 980 && session.Shop.Offers[0].Stock == 120, "Battle retains economy");
                Check(session.TryResolveSortie(SortieResult.Success, binding), "Resolve successful sortie");
                SessionState.SetInt(PhaseKey, 3);
                Check(SceneLoader.Instance.TryExitBattleField(), "Return to hangar");
            }
            else if (phase == 3 && session.State == GameState.Hangar)
            {
                ItemData item = session.Shop.Offers[0].Item;
                Check(binding.Inventory.GetQuantity(item) == SessionState.GetInt("SteelHunter.ShopValidation.Quantity", 0) + 10, "Success retains purchased items");
                HangarShop terminal = UnityEngine.Object.FindFirstObjectByType<HangarShop>();
                Open(binding, terminal);
                int index = -1;
                for (int i = 0; i < binding.Inventory.SlotCount; i++)
                    if (binding.Inventory.GetItem(i)?.Data == item) { index = i; break; }
                ShopPresenter presenter = UnityEngine.Object.FindFirstObjectByType<ShopPresenter>();
                typeof(ShopPresenter).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, new object[] { false, index });
                ShopView view = presenter.GetComponent<ShopView>();
                view.transform.Find("ShopWindow/Quantity").GetComponent<InputField>().text = "4";
                Click(view, "ShopWindow/Trade");
                Check(session.Wallet.Balance == 984, "Sale after return");
                // Loading closes an open shop and preserves the input lock.
                SessionState.SetInt(PhaseKey, 4);
                SceneLoader.Instance.EnterBattleField();
            }
            else if (phase == 4 && session.State == GameState.Battle)
            {
                Check(session.TryResolveSortie(SortieResult.Destroyed, binding), "Resolve failed sortie");
                SessionState.SetInt(PhaseKey, 5);
                Check(SceneLoader.Instance.TryExitBattleField(), "Return after failure");
            }
            else if (phase == 5 && session.State == GameState.Hangar)
            {
                Check(session.Wallet.Balance == 984 && session.Shop.Offers[0].Stock == 120, "Failure preserves economy");
                Check(binding.Inventory.OccupiedSlotCount == 0, "Failure clears carried inventory");
                File.WriteAllText(Result, "PASS: UI purchase, stale quote, success return, sale, failure return, wallet/stock preservation.");
                SessionState.SetInt(PhaseKey, 9);
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception error)
        {
            File.WriteAllText(Result, error.ToString());
            Debug.LogException(error);
            SessionState.SetInt(PhaseKey, 9);
            EditorApplication.isPlaying = false;
        }
    }
    private static void Open(InventorySessionBinding binding, HangarShop terminal)
    {
        binding.transform.position = terminal.transform.position + new Vector3(0, 0, -2);
        Physics.SyncTransforms();
        Check(terminal.CanAccess(binding), "Terminal access");
        var callback = (Action<IInteractable>)typeof(Interactor)
            .GetField("Interacted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(binding.Interactor);
        callback.Invoke(terminal);
        Check(binding.IsShopOpen, "UI opens");
    }
    private static void Click(ShopView view, string path) => view.transform.Find(path).GetComponent<Button>().onClick.Invoke();
    private static void Check(bool value, string name) { if (!value) throw new Exception("Shop Play validation: " + name); }
}
