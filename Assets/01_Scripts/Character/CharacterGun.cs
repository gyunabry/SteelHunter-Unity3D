using System;
using UnityEngine;

public sealed class CharacterGun : MonoBehaviour
{
    private const float CriticalChance = 0.1f;

    [SerializeField] private WeaponVisuals weaponVisuals;
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private MonoBehaviour weaponProviderComponent;
    [SerializeField] private MonoBehaviour reserveSource;

    private CharacterRoot owner;
    private IWeaponProvider weaponProvider;
    private IAmmoSource ammoSource;

    private bool initialized;
    private bool listening;
    private bool reloading;
    private float nextFireTime;
    private float reloadEndTime;
    private float reloadDuration;
    private float aimStartedTime;

    public bool IsAiming { get; private set; }

    public float AimProgress => !IsAiming || Weapon == null ? 0f
        : Weapon.Stats.AimTime <= 0f ? 1f
        : Mathf.Clamp01((Time.time - aimStartedTime) / Weapon.Stats.AimTime);

    // Spread is a horizontal half-angle in degrees; preserve the resolved target height.
    public float CurrentSpreadAngle => Weapon == null ? 0f
        : Mathf.Lerp(Weapon.Stats.HipFireSpread, Weapon.Stats.AimSpread, AimProgress);

    public float MovementSpeedCoefficient => Weapon == null ? 1f
        : Weapon.Stats.MoveSpeedCoefficient * (IsAiming ? Weapon.Stats.AimMoveCoefficient : 1f);

    private WeaponInstance reloadWeapon;
    private AmmoData reloadAmmo;

    public event Action StateChanged;
    public event Action Fired;
    public event Action<FirearmData, Vector3> ShotFired;
    public event Action<FirearmData> ReloadStarted;
    public event Action<FirearmData> ReloadCompleted;
    public event Action ReloadCancelled;

    private WeaponInstance Weapon => weaponProvider?.EquippedWeapon;

    public WeaponType WeaponType => Weapon?.Data.WeaponType ?? WeaponType.None;

    public bool IsInitialized => initialized;
    public bool HasWeapon => Weapon != null;
    public bool IsReloading => reloading;
    public int LoadedRounds => Weapon?.LoadedRounds ?? 0;
    public int MagazineSize => Weapon?.Stats.MagazineCapacity ?? 0;
    public int ReserveRounds => ammoSource != null && Weapon?.SelectedAmmo != null
        ? ammoSource.GetQuantity(Weapon.SelectedAmmo) : 0;
    public float EffectiveRange => Weapon?.Stats.Range ?? 0f;

    public Vector3 MuzzlePosition => TryGetMuzzlePoint(out Transform muzzle)
        ? muzzle.position
        : transform.position;

    public float ReloadProgress => reloading
        ? Mathf.Clamp01(1f - (reloadEndTime - Time.time) / reloadDuration) : 0f;

    public float ProjectileHitRadius => bulletPrefab.HitRadius;

    public bool CanFireNow => 
        initialized && 
        isActiveAndEnabled &&
        owner != null && 
        owner.Health != null && 
        owner.Health.IsAlive &&
        Time.timeScale > 0f && 
        HasWeapon && 
        !reloading &&
        TryGetMuzzlePoint(out _) &&
        Weapon.LoadedRounds > 0 &&
        Weapon.LoadedAmmo != null &&
        Weapon.Data.IsCompatible(Weapon.LoadedAmmo) &&
        Time.time >= nextFireTime;

    public void Initialize(CharacterRoot characterRoot)
    {
        Unsubscribe();
        initialized = false;

        owner = characterRoot;

        weaponProvider = weaponProviderComponent != null
            ? weaponProviderComponent as IWeaponProvider
            : GetComponent<IWeaponProvider>();

        ammoSource = reserveSource != null
            ? reserveSource as IAmmoSource
            : GetComponent<IAmmoSource>();

        if (weaponVisuals == null)
        {
            weaponVisuals = GetComponent<WeaponVisuals>();
        }

        CancelReload();
        SetAiming(false);
        nextFireTime = 0f;

        if (owner == null || owner.Health == null || weaponProvider == null || bulletPrefab == null || ammoSource == null)
        {
            Debug.LogError("CharacterGun needs owner, weapon provider, muzzle, bullet and ammo source.", this);
            return;
        }

        weaponVisuals.Initialize(weaponProvider);

        initialized = true;
        if (isActiveAndEnabled) Subscribe();
        StateChanged?.Invoke();
    }

    public bool TrySelectAmmo(AmmoData ammo)
    {
        WeaponInstance weapon = Weapon;
        if (!initialized || !isActiveAndEnabled || weapon == null ||
            owner == null || owner.Health == null || !owner.Health.IsAlive ||
            Time.timeScale <= 0f || !weapon.Data.IsCompatible(ammo) ||
            ammoSource == null || ammoSource.GetQuantity(ammo) <= 0)
            return false;

        weapon.SelectedAmmo = ammo;
        StateChanged?.Invoke();
        return true;
    }

    public bool TryFire(Vector3 direction)
    {
        if (!CanFireNow) return false;
        if (!TryGetMuzzlePoint(out Transform muzzle)) return false;
        if (direction.sqrMagnitude < 0.01f) return false;

        direction.Normalize();

        WeaponInstance weapon = Weapon;
        WeaponStats stats = weapon.Stats;
        AmmoData ammo = weapon.LoadedAmmo;

        float spread = CurrentSpreadAngle;
        if (spread > 0f)
        {
            direction = Quaternion.AngleAxis(
                UnityEngine.Random.Range(-spread, spread), Vector3.up) * direction;
        }

        Vector3 position = MuzzlePosition;

        BulletProjectile bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction, Vector3.up));

        bool isCritical = UnityEngine.Random.value < CriticalChance;

        bullet.Initialize(
            owner,
            direction,
            stats.Damage * ammo.DamageCoefficient,
            ammo.ArmorPenetrationLevel,
            isCritical,
            stats.CriticalDamageMultiplier,
            stats.BulletSpeed,
            stats.ProjectileLifetime);

        weapon.LoadedRounds--;

        if (weapon.LoadedRounds == 0) weapon.LoadedAmmo = null;

        nextFireTime = Time.time + stats.FireInterval;

        CharacterStimulusBus.ReportSound(
            owner,
            position,
            CharacterSoundKind.Gunshot,
            stats.SoundRange);

        // Snapshot the actual shot before state listeners can change equipment.
        ShotFired?.Invoke(weapon.Data, position);
        StateChanged?.Invoke();
        Fired?.Invoke();
        return true;
    }

    public bool TryReload()
    {
        WeaponInstance weapon = Weapon;
        AmmoData ammo = weapon?.SelectedAmmo;
        if (!initialized || !isActiveAndEnabled || owner == null ||
            owner.Health == null || !owner.Health.IsAlive ||
            Time.timeScale <= 0f || reloading || weapon == null ||
            ammoSource == null || !CanLoad(weapon, ammo) ||
            ammoSource.GetQuantity(ammo) <= 0)
            return false;

        reloadWeapon = weapon;
        SetAiming(false);
        reloadAmmo = ammo;
        reloadDuration = weapon.Stats.ReloadTime;
        reloadEndTime = Time.time + reloadDuration;
        reloading = true;
        ReloadStarted?.Invoke(weapon.Data);
        StateChanged?.Invoke();
        return true;
    }

    private static bool CanLoad(WeaponInstance weapon, AmmoData ammo)
    {
        return weapon != null && weapon.Data.IsCompatible(ammo) &&
            weapon.LoadedRounds < weapon.Stats.MagazineCapacity &&
            (weapon.LoadedRounds == 0 || weapon.LoadedAmmo == ammo);
    }

    private void Update()
    {
        if (!initialized || !reloading) return;
        if (owner == null || owner.Health == null || !owner.Health.IsAlive ||
            !ReferenceEquals(Weapon, reloadWeapon))
        {
            CancelReload();
            return;
        }

        if (Time.timeScale <= 0f || Time.time < reloadEndTime) return;

        WeaponInstance weapon = reloadWeapon;
        AmmoData ammo = reloadAmmo;
        FirearmData completedData = weapon.Data;
        bool loadedAny = false;
        if (CanLoad(weapon, ammo))
        {
            int amount = Mathf.Min(
                weapon.Stats.MagazineCapacity - weapon.LoadedRounds,
                ammoSource.GetQuantity(ammo));
            int consumed = ammoSource.ConsumeUpTo(ammo, amount);
            if (consumed > 0)
            {
                weapon.LoadedAmmo = ammo;
                weapon.LoadedRounds += consumed;
                loadedAny = true;
            }
        }

        ClearReloadState();
        if (loadedAny) ReloadCompleted?.Invoke(completedData);
        else ReloadCancelled?.Invoke();
        StateChanged?.Invoke();
    }

    private bool TryGetMuzzlePoint(out Transform muzzle)
    {
        muzzle = null;

        WeaponInstance equippedWeapon = Weapon;

        if (equippedWeapon == null) return false;

        // 모델과 실제 장착 중인 무기 데이터가 일치할 때만 사용
        if (weaponVisuals.CurrentData != equippedWeapon.Data) return false;

        muzzle = weaponVisuals.MuzzlePoint;
        return muzzle != null;
    }

    public void CancelReload()
    {
        bool wasReloading = reloading;
        ClearReloadState();
        if (wasReloading)
        {
            ReloadCancelled?.Invoke();
            StateChanged?.Invoke();
        }
    }

    private void ClearReloadState()
    {
        reloading = false;
        reloadEndTime = 0f;
        reloadDuration = 0f;
        reloadWeapon = null;
        reloadAmmo = null;
    }

    private void HandleWeaponChanged()
    {
        SetAiming(false);
        if (reloading) CancelReload();
        else StateChanged?.Invoke();
    }

    public void SetAiming(bool aiming)
    {
        aiming = aiming && initialized && isActiveAndEnabled &&
            owner != null && owner.Health != null && owner.Health.IsAlive &&
            Time.timeScale > 0f && HasWeapon && !reloading;
        if (IsAiming == aiming) return;

        IsAiming = aiming;
        aimStartedTime = aiming ? Time.time : 0f;
    }

    private void HandleOwnerDied()
    {
        SetAiming(false);
        CancelReload();
    }

    private void Subscribe()
    {
        if (listening) return;
        weaponProvider.WeaponChanged += HandleWeaponChanged;
        owner.Health.Died += HandleOwnerDied;
        listening = true;
    }

    private void Unsubscribe()
    {
        if (!listening) return;
        if (weaponProvider != null) weaponProvider.WeaponChanged -= HandleWeaponChanged;
        if (owner != null && owner.Health != null) owner.Health.Died -= HandleOwnerDied;
        listening = false;
    }

    private void OnEnable()
    {
        if (initialized) Subscribe();
    }

    private void OnDisable()
    {
        SetAiming(false);
        CancelReload();
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
