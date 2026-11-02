using System;
using UnityEngine;

/// <summary>
/// 적 전용 고정 총기, 탄약을 추가하는 클래스
/// </summary>
public class EnemyFixedLoadout : MonoBehaviour, IWeaponProvider, IAmmoSource
{
    private EnemyCharacterBrain enemyBrain;
    private EnemyCharacterData enemyData;

    private WeaponInstance weapon;
    private bool initialized;

    public event Action WeaponChanged;

    public WeaponInstance EquippedWeapon
    {
        get 
        {
            EnsureInitialize();
            return weapon; 
        }
    }

    private void Awake()
    {
        EnsureInitialize();
    }

    private void EnsureInitialize()
    {
        if (initialized) return;

        initialized = true;

        enemyBrain = GetComponent<EnemyCharacterBrain>();

        enemyData = enemyBrain != null ? enemyBrain.Profile : null;

        FirearmData firearm = enemyData != null ? enemyData.Firearm : null;
        AmmoData ammo = enemyData != null ? enemyData.Ammo : null;

        if (firearm == null || ammo == null)
        {
            Debug.LogError("적 데이터에 총기와 탄약을 지정해야 합니다.", this);
            return;
        }

        if (!firearm.IsCompatible(ammo))
        {
            Debug.LogError("적 데이터의 총기와 탄약 구경이 호환되지 않습니다.",this);
            return;
        }

        weapon = new WeaponInstance(firearm)
        {
            SelectedAmmo = ammo,
            LoadedAmmo = ammo,
            LoadedRounds = firearm.MagazineSize
        };

        WeaponChanged?.Invoke();
    }

    public int ConsumeUpTo(AmmoData requestedAmmo, int requestedRounds)
    {
        if (requestedRounds <= 0 || GetQuantity(requestedAmmo) <= 0)
        {
            return 0;
        }

        return requestedRounds;
    }

    public int GetQuantity(AmmoData requestedAmmo)
    {
        EnsureInitialize();

        if (weapon == null ||
            enemyData == null ||
            requestedAmmo == null ||
            requestedAmmo != enemyData.Ammo)
        {
            return 0;
        }

        // 적은 지정된 탄약을 무한정 사용 가능
        return int.MaxValue;
    }
}
