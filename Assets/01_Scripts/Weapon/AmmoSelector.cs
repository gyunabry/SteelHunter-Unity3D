using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerCharacterInput))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(CharacterEquipment))]
[RequireComponent(typeof(CharacterGun))]
public class AmmoSelector : MonoBehaviour
{
    private PlayerCharacterInput playerInput;
    private PlayerInventory inventory;
    private CharacterEquipment equipment;
    private CharacterGun gun;
    private readonly List<AmmoData> candidates = new();

    private void Awake()
    {
        playerInput = GetComponent<PlayerCharacterInput>();
        inventory = GetComponent<PlayerInventory>();
        gun = GetComponent<CharacterGun>();
        equipment = GetComponent<CharacterEquipment>();
    }

    private void OnEnable()
    {
        playerInput.SelectAmmoRequested += CycleAmmo;
    }

    private void OnDisable()
    {
        if (playerInput != null) playerInput.SelectAmmoRequested -= CycleAmmo;
    }
    
    /// <summary>
    /// 플레이어가 소지한 탄약 중 다음 탄약을 선택하는 메서드
    /// </summary>
    private void CycleAmmo()
    {
        WeaponInstance weapon = equipment.EquippedWeapon;
        if (weapon == null || weapon.Data == null || !gun.IsInitialized)
        {
            return;
        }

        candidates.Clear();

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            AmmoData ammo = inventory.GetItem(i)?.Data as AmmoData;
            if (ammo == null || 
                !weapon.Data.IsCompatible(ammo) || 
                inventory.GetQuantity(ammo) <= 0 || 
                candidates.Contains(ammo))
            {
                continue;
            }

            candidates.Add(ammo);
        }

        if (candidates.Count == 0) return;

        // 현재 무기에 선택된 탄약을 currentIndex로 저장
        int currentIndex = candidates.IndexOf(weapon.SelectedAmmo);
        int nextIndex = (currentIndex + 1) % candidates.Count;

        gun.TrySelectAmmo(candidates[nextIndex]);
    }
}
