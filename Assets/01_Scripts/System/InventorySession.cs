using System;
using UnityEngine;

public sealed class InventorySession : Singleton<InventorySession>
{
    private PlayerInventorySnapshot playerSnapshot;

    public StashInventory Stash { get; private set; } = new StashInventory();
    public GameState State { get; private set; } = GameState.Loading;
    public SortieResult? LastResult { get; private set; }
    public bool IsInitialized => playerSnapshot != null;
    public ShopState Shop { get; private set; }
    public ShopWallet Wallet { get; private set; }

    public void EnsureShop(ShopData data)
    {
        if (Shop != null) return;
        Shop = new ShopState(data);
        Wallet = new ShopWallet(data.initialBalance);
    }

    private void Update()
    {
        Shop?.Advance(Time.deltaTime, State);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeginNewPlaySession()
    {
        // Domain Reload를 꺼도 이전 실행의 메모리 상태를 다시 사용하지 않는다.
        InventorySession session = Instance;
        session.playerSnapshot = null;
        session.Stash = new StashInventory();
        session.State = GameState.Loading;
        session.LastResult = null;
        session.Shop = null;
        session.Wallet = null;
        Time.timeScale = 1f;
    }

    public void StorePlayer(PlayerInventorySnapshot snapshot)
    {
        playerSnapshot = snapshot ??
            throw new ArgumentNullException(nameof(snapshot));
    }

    public void RestorePlayer(PlayerInventory inventory,
        CharacterEquipment equipment, QuickSlots quickSlots)
    {
        if (playerSnapshot == null)
            throw new InvalidOperationException("복원할 소지 상태가 없습니다.");
        playerSnapshot.Apply(inventory, equipment, quickSlots);
    }

    internal void MarkSceneReady(SceneType type)
    {
        State = type switch
        {
            SceneType.Hangar => GameState.Hangar,
            SceneType.Game => GameState.Battle,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                "플레이 가능한 씬만 준비 완료를 처리할 수 있습니다.")
        };
        LastResult = null;
    }

    internal void BeginLoading()
    {
        State = GameState.Loading;
    }

    internal void CancelLoading(GameState previousState)
    {
        State = previousState;
    }

    public bool TryResolveSortie(SortieResult result,
        InventorySessionBinding binding)
    {
        if (State != GameState.Battle || 
            binding == null ||
            !binding.IsReady ||
            binding != InventorySessionBinding.Current ||
            binding.SceneType != SceneType.Game)
        {
            return false;
        }

        if (result != SortieResult.Success 
            && result != SortieResult.Destroyed &&
            result != SortieResult.Victory &&
            result != SortieResult.Abandoned)
        {
            return false;
        }

        // 현재 플레이어의 인벤토리와 장비 상태를 복사
        PlayerInventorySnapshot next = binding.CaptureSnapshot();

        // 탈출 성공, 보스 처치일 경우 소지품 유지
        bool keepCarriedItems = result == SortieResult.Success || result == SortieResult.Victory;

        if (!keepCarriedItems)
        {
            next = next.WithoutCarriedItems(
                binding.Inventory.BaseSlotCount);
        }

        // 플레이어 상태를 저장
        playerSnapshot = next;
        LastResult = result;
        State = GameState.Result;

        return true;
    }
}
