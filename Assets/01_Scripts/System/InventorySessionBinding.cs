using System;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(PlayerInventory), typeof(CharacterEquipment))]
public sealed class InventorySessionBinding : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private CharacterEquipment equipment;
    [SerializeField] private StartingLoadout startingLoadout;
    [SerializeField] private PlayerCharacterInput playerInput;
    [SerializeField] private Interactor interactor;
    [SerializeField] private CharacterGun gun;

    [Header("씬 UI")]
    [SerializeField] private QuickSlots quickSlots;
    [SerializeField] private InventoryPresenter inventoryPresenter;
    [SerializeField] private ShopPresenter shopPresenter;

    private bool controlsLocked;

    public static InventorySessionBinding Current { get; private set; }
    public bool IsReady { get; private set; }
    public SceneType SceneType { get; private set; }
    public PlayerInventory Inventory => inventory;
    public Interactor Interactor => interactor;
    public QuickSlots QuickSlots => quickSlots;
    public InventoryPresenter InventoryPresenter => inventoryPresenter;
    public bool IsShopOpen => shopPresenter != null && shopPresenter.IsOpen;

    public bool WasShopClosedThisFrame =>
    shopPresenter != null &&
    shopPresenter.LastClosedFrame == Time.frameCount;

    public bool CanUseInventory => IsReady && !controlsLocked &&
        (InventorySession.Instance.State == GameState.Hangar ||
         InventorySession.Instance.State == GameState.Battle);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticReference()
    {
        Current = null;
    }

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (equipment == null) equipment = GetComponent<CharacterEquipment>();
        if (startingLoadout == null) startingLoadout = GetComponent<StartingLoadout>();
        if (playerInput == null) playerInput = GetComponent<PlayerCharacterInput>();
        if (interactor == null) interactor = GetComponent<Interactor>();
        if (gun == null) gun = GetComponent<CharacterGun>();

        string sceneName = gameObject.scene.name;
        SceneType = sceneName == SceneNames.GetSceneName(global::SceneType.Hangar)
            ? global::SceneType.Hangar
            : sceneName == SceneNames.GetSceneName(global::SceneType.Game)
                ? global::SceneType.Game : global::SceneType.None;

        if (Current != null && Current != this &&
            Current.gameObject.scene == gameObject.scene)
        {
            Debug.LogError("한 씬에는 InventorySessionBinding을 하나만 둘 수 있습니다.", this);
            enabled = false;
            return;
        }

        Current = this;
        LockControls();
    }

    private void Start()
    {
        try
        {
            if (SceneType == global::SceneType.None || inventory == null ||
                equipment == null || quickSlots == null || playerInput == null ||
                interactor == null || gun == null || inventoryPresenter == null)
            {
                throw new InvalidOperationException("InventorySessionBinding의 씬/플레이어/UI 참조를 연결해야 합니다.");
            }

            InventorySession session = InventorySession.Instance;
            if (session.IsInitialized)
            {
                session.RestorePlayer(inventory, equipment, quickSlots);
            }
            else
            {
                if (startingLoadout != null) startingLoadout.GiveInitialItems();
                session.StorePlayer(CaptureSnapshot());
            }

            if (GameCursor.Current == null || !GameCursor.Current.isActiveAndEnabled)
            {
                throw new InvalidOperationException("GameCursor 프리팹과 UI 참조를 확인해야 합니다.");
            }

            GameCursor.Current.Bind(gun, playerInput);

            IsReady = true;
            SceneLoader.Instance.NotifySceneReady(SceneType);
            UnlockControls();
        }
        catch (Exception exception)
        {
            IsReady = false;
            LockControls();
            Debug.LogException(exception, this);
        }
    }

    public PlayerInventorySnapshot CaptureSnapshot()
    {
        return PlayerInventorySnapshot.Capture(inventory, equipment, quickSlots);
    }

    public void LockControls()
    {
        controlsLocked = true;
        if (shopPresenter != null) shopPresenter.Close();
        if (inventoryPresenter != null)
        {
            inventoryPresenter.Close();
            inventoryPresenter.enabled = false;
        }
        if (playerInput != null) playerInput.SetUIBlocked(true);
        if (quickSlots != null) quickSlots.SetUIBlocked(true);
        if (interactor != null) interactor.SetBlock(true);
        if (gun != null) gun.CancelReload();
    }

    public void UnlockControls()
    {
        if (!IsReady) return;
        GameState state = InventorySession.Instance.State;
        if (state != GameState.Hangar && state != GameState.Battle) return;

        controlsLocked = false;
        if (inventoryPresenter != null) inventoryPresenter.enabled = true;
        if (playerInput != null) playerInput.SetUIBlocked(false);
        if (quickSlots != null) quickSlots.SetUIBlocked(false);
        if (interactor != null) interactor.SetBlock(false);

        GameCursor.Current.ShowCrosshair();
    }

    private void RequestSortie()
    {
        SceneLoader.Instance.EnterBattleField();
    }

    private void OnDestroy()
    {
        if (GameCursor.Current != null)
        {
            GameCursor.Current.Unbind(gun);
        }
            
        if (Current == this) Current = null;
    }
}
