using System;

public enum GunAttachmentSlot
{
    Optic = 0,      // 조준경
    Muzzle,         // 총구
    Magazine,       // 탄창
    Grip,           // 손잡이
    Stock           // 개머리판
}

// 해당 총기가 어떤 슬롯을 사용할지 정하는 열거형
[Flags]
public enum GunAttachmentSlotMask
{
    None = 0,
    Optic = 1 << 0,
    Muzzle = 1 << 1,
    Magazine = 1 << 2,
    Grip = 1 << 3,
    Stock = 1 << 4,
    All = Optic | Muzzle | Magazine | Grip | Stock
}

public enum GunStat
{
    Damage,                     // 기본 데미지
    FireRate,                   // 발사 속도
    MagazineCapacity,           // 기본 탄창 용량
    ReloadTime,                 // 재장전 시간
    BulletSpeed,                // 총알 속도
    Range,                      // 최대 사거리
    CriticalDamageMultiplier,   // 크리티컬 데미지 배율
    SoundRange,                 // 소음 전파 거리
    AimTime,                    // 조준 속도
    MoveSpeedCoefficient,       // 이동속도 계수
    AimMoveCoefficient,         // 조준 이동속도 계수
    HipFireSpread,              // 힙파이어 총알 분산
    AimSpread                   // 조준 총알 분산
}

[Serializable]
public struct GunStatModifier
{
    public GunStat stat;

    // 예) 탄창 용량 +5
    public float addValue;

    // 예) 재장전 시간 +20%라면 0.2
    public float percentValue;
}