using UnityEngine;

public sealed class HangarShop : MonoBehaviour, IInteractable
{
    [SerializeField] private ShopData data;
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string promptText;

    public string PromptText => promptText;
    public ShopData Data => data;
    public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;

    private void Start() { if (data != null) InventorySession.Instance.EnsureShop(data); }

    public bool CanInteract() => 
        data != null && 
        isActiveAndEnabled &&
        gameObject.scene.name == SceneNames.GetSceneName(SceneType.Hangar) &&
        InventorySession.Instance.IsInitialized && 
        InventorySession.Instance.State == GameState.Hangar;

    public bool CanAccess(InventorySessionBinding binding) => 
        CanInteract() &&
        binding != null && 
        binding == InventorySessionBinding.Current && 
        binding.CanUseInventory &&
        binding.gameObject.scene == gameObject.scene && 
        binding.Interactor != null &&
        binding.Interactor.IsInRange(this);

    public void OnInteract() { }
}
