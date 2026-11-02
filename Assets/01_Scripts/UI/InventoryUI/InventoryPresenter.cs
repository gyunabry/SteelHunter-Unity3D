using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryPresenter : MonoBehaviour
{
    [Header("화면")]
    [SerializeField] private InventoryView view;

    [Header("플레이어")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Interactor interactor;
    [SerializeField] private PlayerCharacterInput playerInput;
    [SerializeField] private QuickSlots quickSlots;
    [SerializeField] private CharacterHealth playerHealth;
    [SerializeField] private CharacterEquipment equipment;

    private InventorySessionBinding binding;
    private LootContainer currentLoot;
    private HangarStash currentStash;
    private StashInventory subscribedStash;
    private bool isOpen;
    private bool showingLoot;
    private bool showingStash;

    private bool CanOpen => binding != null && binding.CanUseInventory && !binding.IsShopOpen &&
        (playerHealth == null || playerHealth.IsAlive);

    private void Awake()
    {
        if (inventory != null)
            binding = inventory.GetComponent<InventorySessionBinding>();
        if (view != null) view.BindQuickSlots(quickSlots);
    }

    private void OnEnable()
    {
        if (interactor != null) interactor.Interacted += HandleInteracted;
        if (view != null)
        {
            view.CloseRequested += Close;
            view.LootTakeRequested += HandleLootTakeRequested;
            view.InventoryEquipRequested += HandleInventoryEquipRequested;
            view.DepositRequested += HandleDepositRequested;
            view.WithdrawRequested += HandleWithdrawRequested;
            view.UnequipRequested += HandleUnequipRequested;
            view.WeaponSelectRequested += HandleWeaponSelectRequested;
            view.EquipRequested += HandleEquipRequested;
        }
        if (inventory != null) inventory.Changed += RefreshOpenView;
        if (equipment != null) equipment.Changed += RefreshOpenView;
        if (playerHealth != null) playerHealth.Died += Close;
    }

    private void OnDisable()
    {
        if (interactor != null) interactor.Interacted -= HandleInteracted;
        if (view != null)
        {
            view.CloseRequested -= Close;
            view.LootTakeRequested -= HandleLootTakeRequested;
            view.InventoryEquipRequested -= HandleInventoryEquipRequested;
            view.DepositRequested -= HandleDepositRequested;
            view.WithdrawRequested -= HandleWithdrawRequested;
            view.UnequipRequested -= HandleUnequipRequested;
            view.WeaponSelectRequested -= HandleWeaponSelectRequested;
            view.EquipRequested -= HandleEquipRequested;
        }
        if (inventory != null) inventory.Changed -= RefreshOpenView;
        if (equipment != null) equipment.Changed -= RefreshOpenView;
        if (playerHealth != null) playerHealth.Died -= Close;
        Close();
    }

    private void Update()
    {
        if (!CanOpen)
        {
            Close();
            return;
        }

        if (isOpen &&
            ((showingLoot && (currentLoot == null || !interactor.IsInRange(currentLoot))) ||
             (showingStash && (currentStash == null || !interactor.IsInRange(currentStash)))))
        {
            Close();
            return;
        }

        if (binding.WasShopClosedThisFrame) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (isOpen && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (isOpen) Close();
            else Open(null, null);
        }
    }

    private void HandleInteracted(IInteractable target)
    {
        if (target is HangarStash stash) Open(null, stash);
        else if (target is LootContainer loot) Open(loot, null);
    }

    private void Open(LootContainer loot, HangarStash stash)
    {
        if (!CanOpen || view == null) return;
        if (loot != null && !interactor.IsInRange(loot)) return;
        if (stash != null && !interactor.IsInRange(stash)) return;
        if (isOpen) Close();

        currentLoot = loot;
        currentStash = stash;
        showingLoot = loot != null;
        showingStash = stash != null;
        isOpen = true;

        subscribedStash = stash != null ? stash.Storage : null;
        if (subscribedStash != null) subscribedStash.Changed += RefreshOpenView;

        if (playerInput != null) playerInput.SetUIBlocked(true);
        if (quickSlots != null) quickSlots.SetUIBlocked(true);
        if (interactor != null) interactor.SetBlock(true);
        GameCursor.Current.ShowPointer();
        view.Show(inventory, currentLoot, equipment, subscribedStash);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        showingLoot = false;
        showingStash = false;
        currentLoot = null;
        currentStash = null;

        if (subscribedStash != null)
            subscribedStash.Changed -= RefreshOpenView;
        subscribedStash = null;
        if (view != null) view.Hide();

        // 창 닫기가 결과 처리/씬 이동 중의 입력 잠금을 풀지 않게 한다.
        bool keepBlocked = !CanOpen;
        if (playerInput != null) playerInput.SetUIBlocked(keepBlocked);
        if (quickSlots != null) quickSlots.SetUIBlocked(keepBlocked);
        if (interactor != null) interactor.SetBlock(keepBlocked);
        if (!keepBlocked) GameCursor.Current.ShowCrosshair();
    }

    private void HandleLootTakeRequested(int index)
    {
        if (!CanOpen || !isOpen || !showingLoot || currentLoot == null ||
            !interactor.IsInRange(currentLoot)) return;

        if (!currentLoot.TryTransferTo(index, inventory))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("아이템을 가져오지 못했습니다.", this);
#endif
        RefreshOpenView();
    }

    private void HandleDepositRequested(int index)
    {
        if (!CanOpen || !isOpen || !showingStash || currentStash == null) return;
        if (!currentStash.TryDeposit(binding, index, out string message))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message, this);
#endif
    }

    private void HandleWithdrawRequested(int index)
    {
        if (!CanOpen || !isOpen || !showingStash || currentStash == null) return;
        if (!currentStash.TryWithdraw(binding, index, out string message))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message, this);
#endif
    }

    private void HandleInventoryEquipRequested(int index)
    {
        if (!CanOpen || !isOpen || inventory == null || equipment == null ||
            index < 0 || index >= inventory.SlotCount) return;
        ItemStack item = inventory.GetItem(index);
        if (item == null || !(item.Data is EquipmentData)) return;
        if (!equipment.TryEquip(index))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("장비 종류와 가방 용량을 확인하세요.", this);
#endif
    }

    private void HandleEquipRequested(int index, ItemStack stack, EquipmentSlot slot)
    {
        if (!CanOpen || !isOpen || inventory == null || equipment == null ||
            index < 0 || index >= inventory.SlotCount ||
            !ReferenceEquals(inventory.GetItem(index), stack)) return;
        if (!equipment.TryEquip(index, slot))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("해당 장비 칸에 장착할 수 없습니다.", this);
#endif
    }

    private void HandleWeaponSelectRequested(EquipmentSlot slot)
    {
        if (CanOpen && isOpen && equipment != null)
            equipment.TrySelectWeapon(slot);
    }

    private void HandleUnequipRequested(EquipmentSlot slot)
    {
        if (!CanOpen || !isOpen || equipment == null) return;
        if (!equipment.TryUnequip(slot))
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("장비를 해제할 공간 또는 허용 무게가 부족합니다.", this);
#endif
    }

    private void RefreshOpenView()
    {
        if (isOpen && view != null)
            view.Refresh(inventory, currentLoot, equipment, subscribedStash);
    }
}
