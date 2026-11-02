using UnityEngine;

public sealed class CharacterCommandReceiver : MonoBehaviour, ICharacterCommands
{
    private CharacterMover mover;
    private CharacterAim aim;
    private CharacterGun gun;
    private CharacterHealth health;

    public bool IsInitialized => mover != null && aim != null && gun != null && health != null;

    public void Initialize(
        CharacterMover characterMover,
        CharacterAim characterAim,
        CharacterGun characterGun,
        CharacterHealth characterHealth)
    {
        mover = characterMover;
        aim = characterAim;
        gun = characterGun;
        health = characterHealth;
    }

    public void SetMove(Vector3 worldDirection, bool sprint)
    {
        if (!IsInitialized || !isActiveAndEnabled) return;
        mover.SetMove(health.IsAlive ? worldDirection : Vector3.zero, health.IsAlive && sprint);
    }

    public void SetAim(Vector3 worldPoint)
    {
        if (IsInitialized && isActiveAndEnabled && health.IsAlive) aim.SetAim(worldPoint);
    }

    public bool TryFire()
    {
        if (!IsInitialized || !isActiveAndEnabled || !health.IsAlive ||
            !aim.TryGetFireDirection(gun.MuzzlePosition, out Vector3 direction))
        {
            return false;
        }

        return gun.TryFire(direction);
    }

    public bool TryReload()
    {
        return IsInitialized && isActiveAndEnabled && health.IsAlive && gun.TryReload();
    }

    public void SetAiming(bool aiming)
    {
        if (!IsInitialized) return;
        gun.SetAiming(aiming && isActiveAndEnabled && health.IsAlive);
    }

    public void Stop()
    {
        mover?.StopImmediately();
        aim?.ClearAim();
        gun?.SetAiming(false);
    }

    private void OnDisable()
    {
        Stop();
    }
}
