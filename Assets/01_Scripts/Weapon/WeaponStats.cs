using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 스탯값을 저장하는 클래스
/// </summary>
public class WeaponStats
{
    private readonly Dictionary<GunStat, float> values = new();

    internal WeaponStats(Dictionary<GunStat, float> source)
    {
        values = new Dictionary<GunStat, float>(source);
    }

    public float GetValue(GunStat stat) 
    {
        if (!values.TryGetValue(stat, out float value))
        {
            throw new ArgumentOutOfRangeException(nameof(stat));
        }

        return value;
    }

    #region 스탯
    public float Damage => GetValue(GunStat.Damage);
    public float FireRate => GetValue(GunStat.FireRate);
    public float FireInterval => 1f / FireRate;
    public float ProjectileLifetime => Range / BulletSpeed;

    public int MagazineCapacity => (int)GetValue(GunStat.MagazineCapacity);

    public float ReloadTime => GetValue(GunStat.ReloadTime);
    public float BulletSpeed => GetValue(GunStat.BulletSpeed);
    public float Range => GetValue(GunStat.Range);

    public float CriticalDamageMultiplier => GetValue(GunStat.CriticalDamageMultiplier);

    public float SoundRange => GetValue(GunStat.SoundRange);
    public float AimTime => Mathf.Max(0f, GetValue(GunStat.AimTime));

    public float MoveSpeedCoefficient => Mathf.Max(0f, GetValue(GunStat.MoveSpeedCoefficient));

    public float AimMoveCoefficient => Mathf.Clamp01(GetValue(GunStat.AimMoveCoefficient));

    public float HipFireSpread => Mathf.Clamp(GetValue(GunStat.HipFireSpread), 0f, 90f);
    public float AimSpread => Mathf.Clamp(GetValue(GunStat.AimSpread), 0f, HipFireSpread);
    #endregion
}
