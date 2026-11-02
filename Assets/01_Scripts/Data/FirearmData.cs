using UnityEngine;

public enum WeaponType
{
    None,
    Pistol,
    Rifle,
    Shotgun,
    SniperRifle
}

[CreateAssetMenu(fileName = "FirearmData", menuName = "SteelHunter/Items/Firearm Data")]
public sealed class FirearmData : EquipmentData
{
    [Header("Audio")]
    [SerializeField] private WeaponAudioProfile audioProfile;
    public WeaponAudioProfile AudioProfile => audioProfile;

    [Header("무기 종류")]
    [SerializeField] private WeaponType weaponType;
    [Header("외형")]
    [SerializeField] private Weapon visualPrefab;

    [Header("탄약")]
    [SerializeField] private CaliberData caliber;

    [Header("기본 성능")]
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0.01f)] private float fireRate = 5f;
    [SerializeField, Min(1)] private int magazineSize = 12;
    [SerializeField, Min(0.01f)] private float reloadDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 35f;
    [SerializeField, Min(0.01f)] private float range = 70f;
    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float soundRange = 30f;

    [Header("조준과 이동")]
    [Tooltip("Seconds to reach aimed accuracy while holding Aim. Zero is instant.")]
    [SerializeField, Min(0f)] private float aimTime = 0.3f;
    [Tooltip("Multiplier for walk and sprint speed while this weapon is equipped.")]
    [SerializeField, Min(0f)] private float moveSpeedCoefficient = 1f;
    [Tooltip("Additional walk speed multiplier while aiming. Sprint is suppressed.")]
    [SerializeField, Range(0f, 1f)] private float aimMoveCoefficient = 0.6f;
    [Tooltip("Horizontal spread half-angle in degrees. 5 means -5 to +5 degrees.")]
    [SerializeField, Range(0f, 90f)] private float hipFireSpread = 5f;
    [Tooltip("Fully aimed horizontal half-angle. Clamped to hip spread at runtime.")]
    [SerializeField, Range(0f, 90f)] private float aimSpread = 1f;

    [Header("부착물")]
    [SerializeField]
    private GunAttachmentSlotMask allowedAttachmentSlots = GunAttachmentSlotMask.All;

    public override ItemKind Kind => ItemKind.Firearm;
    public WeaponType WeaponType => weaponType;
    public Weapon VisualPrefab => visualPrefab;

    public CaliberData Caliber => caliber;
    public float Damage => Mathf.Max(0f, damage);
    public float FireRate => Mathf.Max(0.01f, fireRate);
    public int MagazineCapacity => Mathf.Max(1, magazineSize);
    public float ReloadTime => Mathf.Max(0.01f, reloadDuration);
    public float BulletSpeed => Mathf.Max(0.01f, projectileSpeed);
    public float Range => Mathf.Max(0.01f, range);

    public float CriticalDamageMultiplier => criticalDamageMultiplier;

    public float SoundRange => soundRange;
    public float AimTime => aimTime;

    public float MoveSpeedCoefficient => moveSpeedCoefficient;

    public float AimMoveCoefficient => aimMoveCoefficient;

    public float HipFireSpread => hipFireSpread;
    public float AimSpread => aimSpread;

    // 현재 CharacterGun이 사용하는 API와 연결
    public float FireInterval => 1f / FireRate;
    public int MagazineSize => MagazineCapacity;
    public float ReloadDuration => ReloadTime;
    public float ProjectileSpeed => BulletSpeed;
    public float ProjectileLifetime => Range / BulletSpeed;

    // 총과 탄약의 호환성 여부 검사
    public bool IsCompatible(AmmoData ammo)
    {
        return caliber != null &&
               ammo != null &&
               ammo.Caliber == caliber;
    }

    // 이 무기 데이터가 해당 부착물을 지원하는지 검사
    public bool SupportsAttachmentSlot(GunAttachmentSlot slot)
    {
        GunAttachmentSlotMask mask =
            (GunAttachmentSlotMask)(1 << (int)slot);

        return (allowedAttachmentSlots & mask) != 0;
    }
}
