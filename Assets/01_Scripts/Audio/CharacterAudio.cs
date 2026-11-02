using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterGun), typeof(CharacterHealth), typeof(CharacterNoiseEmitter))]
public sealed class CharacterAudio : MonoBehaviour
{
    [SerializeField] private CharacterAudioProfile characterProfile;
    [SerializeField] private SoundCuePlayer weaponPlayer;
    [SerializeField] private SoundCuePlayer footstepPlayer;
    [SerializeField] private SoundCuePlayer reloadPlayer;
    [SerializeField] private SoundCuePlayer voicePlayer;
    [SerializeField, Min(0f)] private float hurtInterval = 0.12f;

    private CharacterGun gun;
    private CharacterHealth health;
    private CharacterNoiseEmitter noiseEmitter;
    private float nextHurtTime;

    private bool CanPlayWorldEffect => health != null && health.IsAlive && Time.timeScale > 0f;

    private void Awake()
    {
        gun = GetComponent<CharacterGun>();
        health = GetComponent<CharacterHealth>();
        noiseEmitter = GetComponent<CharacterNoiseEmitter>();
    }

    private void OnEnable()
    {
        nextHurtTime = 0f;
        gun.ShotFired += HandleShotFired;
        gun.ReloadStarted += HandleReloadStarted;
        gun.ReloadCompleted += HandleReloadCompleted;
        gun.ReloadCancelled += HandleReloadCancelled;
        noiseEmitter.Stepped += HandleStepped;
        health.Damaged += HandleDamaged;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (gun != null)
        {
            gun.ShotFired -= HandleShotFired;
            gun.ReloadStarted -= HandleReloadStarted;
            gun.ReloadCompleted -= HandleReloadCompleted;
            gun.ReloadCancelled -= HandleReloadCancelled;
        }
        if (noiseEmitter != null) noiseEmitter.Stepped -= HandleStepped;
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }
        StopAllPlayers();
    }

    private void LateUpdate()
    {
        if (gun != null) UpdateWeaponPositions(gun.MuzzlePosition);
    }

    private void UpdateWeaponPositions(Vector3 position)
    {
        if (weaponPlayer != null) weaponPlayer.transform.position = position;
        if (reloadPlayer != null) reloadPlayer.transform.position = position;
    }

    private void HandleShotFired(FirearmData data, Vector3 position)
    {
        if (!CanPlayWorldEffect || weaponPlayer == null || data == null || data.AudioProfile == null) return;
        UpdateWeaponPositions(position);
        weaponPlayer.Play(data.AudioProfile.Fire);
    }

    private void HandleStepped(Vector3 position, bool isSprinting)
    {
        if (!CanPlayWorldEffect || footstepPlayer == null || characterProfile == null) return;
        // Keep the source's authored local offset near the feet.
        footstepPlayer.Play(isSprinting ? characterProfile.SprintFootstep : characterProfile.WalkFootstep);
    }

    private void HandleReloadStarted(FirearmData data)
    {
        if (!CanPlayWorldEffect || !gun.IsReloading || reloadPlayer == null) return;
        reloadPlayer.Stop();
        if (data != null && data.AudioProfile != null)
        {
            UpdateWeaponPositions(gun.MuzzlePosition);
            reloadPlayer.Play(data.AudioProfile.ReloadStart, interrupt: true);
        }
    }

    private void HandleReloadCompleted(FirearmData data)
    {
        if (reloadPlayer == null) return;
        reloadPlayer.Stop();
        if (CanPlayWorldEffect && data != null && data.AudioProfile != null)
            reloadPlayer.Play(data.AudioProfile.ReloadComplete, interrupt: true);
    }

    private void HandleReloadCancelled()
    {
        if (reloadPlayer != null) reloadPlayer.Stop();
    }

    private void HandleDamaged(Vector3 hitPoint, Vector3 shotDirection)
    {
        // Health has already changed: lethal hits use only Death.
        if (!CanPlayWorldEffect || voicePlayer == null || characterProfile == null ||
            characterProfile.Hurt == null || Time.unscaledTime < nextHurtTime) return;
        nextHurtTime = Time.unscaledTime + hurtInterval;
        voicePlayer.Play(characterProfile.Hurt, interrupt: true);
    }

    private void HandleDied()
    {
        if (footstepPlayer != null) footstepPlayer.Stop();
        if (reloadPlayer != null) reloadPlayer.Stop();
        if (voicePlayer == null) return;
        voicePlayer.Stop();
        if (characterProfile != null) voicePlayer.Play(characterProfile.Death, interrupt: true);
    }

    private void StopAllPlayers()
    {
        if (weaponPlayer != null) weaponPlayer.Stop();
        if (footstepPlayer != null) footstepPlayer.Stop();
        if (reloadPlayer != null) reloadPlayer.Stop();
        if (voicePlayer != null) voicePlayer.Stop();
    }
}
