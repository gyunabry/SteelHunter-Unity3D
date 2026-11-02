using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부착물에 따른 스탯 계산을 담당하는 클래스
/// </summary>
public class WeaponStatCalculator
{
    // GunStat 열거형에 정의된 모든 값을 배열로 생성
    private static readonly GunStat[] AllStats = (GunStat[])Enum.GetValues(typeof(GunStat));

    public static WeaponStats Calculate(FirearmData firearm, IEnumerable<GunAttachmentData> attachments)
    {
        if (firearm == null)
        {
            throw new ArgumentNullException(nameof(firearm));
        }

        var addTotals = new Dictionary<GunStat, float>();
        var percentTotals = new Dictionary<GunStat, float>();

        foreach (GunStat stat in AllStats)
        {
            addTotals.Add(stat, 0f);
            percentTotals.Add(stat, 0f);
        }

        if (attachments != null)
        {
            // 부착물 별로 순회
            foreach (GunAttachmentData attachment in attachments)
            {
                if (attachment == null) continue;

                var modifiers = attachment.Modifiers;

                if (modifiers == null) continue;

                // 부착물 적용 스탯 순회
                foreach (GunStatModifier modifier in modifiers)
                {
                    if (!addTotals.ContainsKey(modifier.stat))
                    {
                        Debug.LogWarning($"{modifier.stat} : 정의되지 않은 스탯입니다.");
                        continue;
                    }

                    addTotals[modifier.stat] += modifier.addValue;
                    percentTotals[modifier.stat] += modifier.percentValue;
                }
            }
        }

        var results = new Dictionary<GunStat, float>();

        foreach (GunStat stat in AllStats)
        {
            float baseValue = GetBaseValue(firearm, stat);

            float value = (baseValue + addTotals[stat]) * (1f + percentTotals[stat]);

            results.Add(stat, value);
        }

        return new WeaponStats(results);
    }

    private static float GetBaseValue(FirearmData firearm, GunStat stat)
    {
        switch (stat)
        {
            case GunStat.Damage:
                return firearm.Damage;

            case GunStat.FireRate:
                return firearm.FireRate;

            case GunStat.MagazineCapacity:
                return firearm.MagazineCapacity;

            case GunStat.ReloadTime:
                return firearm.ReloadTime;

            case GunStat.BulletSpeed:
                return firearm.BulletSpeed;

            case GunStat.Range:
                return firearm.Range;

            case GunStat.CriticalDamageMultiplier:
                return firearm.CriticalDamageMultiplier;

            case GunStat.SoundRange:
                return firearm.SoundRange;

            case GunStat.AimTime:
                return firearm.AimTime;

            case GunStat.MoveSpeedCoefficient:
                return firearm.MoveSpeedCoefficient;

            case GunStat.AimMoveCoefficient:
                return firearm.AimMoveCoefficient;

            case GunStat.HipFireSpread:
                return firearm.HipFireSpread;

            case GunStat.AimSpread:
                return firearm.AimSpread;

            default:
                throw new ArgumentOutOfRangeException(nameof(stat));
        }
    }
}
