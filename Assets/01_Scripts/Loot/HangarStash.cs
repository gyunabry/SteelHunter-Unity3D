using UnityEngine;

public sealed class HangarStash : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string promptText;

    public string PromptText => promptText;

    public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;
    public StashInventory Storage => InventorySession.Instance.Stash;

    public bool CanInteract()
    {
        return isActiveAndEnabled &&
            InventorySession.Instance.IsInitialized &&
            InventorySession.Instance.State == GameState.Hangar;
    }

    public void OnInteract()
    {
        // 화면 열기는 Interactor.Interacted를 받은 Presenter가 담당한다.
    }

    private bool CanAccess(InventorySessionBinding binding)
    {
        return CanInteract() && binding != null &&
            binding == InventorySessionBinding.Current &&
            binding.CanUseInventory && binding.SceneType == SceneType.Hangar &&
            binding.gameObject.scene == gameObject.scene &&
            binding.Interactor != null && binding.Interactor.IsInRange(this);
    }

    public bool TryDeposit(InventorySessionBinding binding, int inventoryIndex,
        out string message)
    {
        if (!CanAccess(binding))
        {
            message = "격납고 창고 가까이에서 이용할 수 있습니다.";
            return false;
        }
        return Storage.TryDeposit(binding.Inventory, inventoryIndex, out message);
    }

    public bool TryWithdraw(InventorySessionBinding binding, int stashIndex,
        out string message)
    {
        if (!CanAccess(binding))
        {
            message = "격납고 창고 가까이에서 이용할 수 있습니다.";
            return false;
        }
        return Storage.TryWithdraw(stashIndex, binding.Inventory, out message);
    }
}
