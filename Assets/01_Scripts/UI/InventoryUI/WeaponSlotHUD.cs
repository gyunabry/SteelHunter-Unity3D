using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotHUD : MonoBehaviour
{
    [Header("표시할 무기 슬롯")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.PrimaryWeapon;

    [Header("플레이어 참조")]
    [SerializeField] private CharacterEquipment equipment;
    [SerializeField] private CharacterGun gun;
    [SerializeField] private PlayerInventory inventory;

    [Header("UI 참조")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TMP_Text selectedAmmoText;
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private GameObject ammoInfoRoot;
    [SerializeField] private GameObject equippedFrame;
    [SerializeField] private GameObject emptySlotVisual;
    [SerializeField] private TMP_Text loadedAmmoText;
    [SerializeField] private GameObject reloadIndicator;
    [SerializeField] private GameObject selectedAmmoKeyHint;

    [Header("색상")]
    [SerializeField] private Color normalCountColor = Color.white;
    [SerializeField] private Color emptyCountColor = new Color32(235, 90, 90, 255);

    private bool dirty;

    private void Awake()
    {
        if (equipment == null || gun == null || inventory == null ||
            weaponIcon == null || selectedAmmoText == null || ammoCountText == null)
        {
            Debug.LogError("WeaponSlotHUD의 참조가 연결되어 있지 않습니다.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (equipment != null) equipment.Changed += MarkDirty;
        if (gun != null) gun.StateChanged += MarkDirty;
        if (inventory != null) inventory.Changed += MarkDirty;
        MarkDirty();
    }

    private void Start()
    {
        MarkDirty();
    }

    private void OnDisable()
    {
        if (equipment != null) equipment.Changed -= MarkDirty;
        if (gun != null) gun.StateChanged -= MarkDirty;
        if (inventory != null) inventory.Changed -= MarkDirty;
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        Refresh();
    }

    private void Refresh()
    {
        // 현재 HUD 무기 슬롯에 지정된 무기를 조회
        WeaponInstance weapon = equipment.GetEquipped(slot)?.Weapon;
        bool hasWeapon = weapon != null && weapon.Data != null;
        // 현재 슬롯이 선택된 무기와 같은 종류인지 검사
        bool isActive = hasWeapon && equipment.ActiveWeaponSlot == slot;

        weaponIcon.sprite = hasWeapon ? weapon.Data.Icon : null;
        weaponIcon.enabled = weaponIcon.sprite != null;

        if (ammoInfoRoot != null) ammoInfoRoot.SetActive(isActive);
        if (equippedFrame != null) equippedFrame.SetActive(isActive);
        selectedAmmoText.gameObject.SetActive(isActive);
        if (selectedAmmoKeyHint != null) selectedAmmoKeyHint.SetActive(isActive);
        if (emptySlotVisual != null) emptySlotVisual.SetActive(!hasWeapon);
        if (reloadIndicator != null) reloadIndicator.SetActive(isActive && gun.IsReloading);

        if (!hasWeapon)
        {
            selectedAmmoText.text = string.Empty;
            ammoCountText.text = string.Empty;
            ammoCountText.color = normalCountColor;

            if (loadedAmmoText != null)
            {
                loadedAmmoText.text = string.Empty;
                loadedAmmoText.gameObject.SetActive(false);
            }
            return;
        }

        AmmoData selected = weapon.SelectedAmmo;
        int loaded = weapon.LoadedRounds;

        // 각 슬롯의 선택 탄종에 맞는 예비 탄약 수
        int reserve = selected != null ? inventory.GetQuantity(selected) : 0;

        selectedAmmoText.text = selected != null ? GetAmmoName(selected) : "탄약 미선택";

        ammoCountText.text = selected != null
            ? $"{loaded:00}/{reserve:00}"
            : $"{loaded:00}/--";

        ammoCountText.color = loaded > 0 ? normalCountColor : emptyCountColor;

        bool differentAmmo = loaded > 0 && weapon.LoadedAmmo != null &&
            weapon.LoadedAmmo != selected;
        if (loadedAmmoText != null)
        {
            loadedAmmoText.text = differentAmmo ? $"장전 : {GetAmmoName(weapon.LoadedAmmo)}" : string.Empty;
            loadedAmmoText.gameObject.SetActive(differentAmmo);
        }
    }

    private static string GetAmmoName(AmmoData ammo)
    {
        return string.IsNullOrWhiteSpace(ammo.DisplayName)
            ? ammo.name : ammo.DisplayName;
    }
}
