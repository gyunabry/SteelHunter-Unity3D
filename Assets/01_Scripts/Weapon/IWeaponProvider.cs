using System;

/// <summary>
/// 현재 사용하는 총 인스턴스와 총이 변경되었다는 알림을 제공하는 인터페이스
/// </summary>
public interface IWeaponProvider
{
    WeaponInstance EquippedWeapon { get; }

    event Action WeaponChanged;
}
