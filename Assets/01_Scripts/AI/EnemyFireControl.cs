using UnityEngine;

public sealed class EnemyFireControl
{
    private readonly CharacterRoot self;
    private readonly CharacterGun gun;
    private readonly CharacterAim aim;
    private readonly CharacterMover mover;
    private readonly ICharacterCommands commands;
    private readonly EnemyPerception perception;
    private readonly EnemyCharacterData data;

    public EnemyFireControl(CharacterRoot self, CharacterGun gun, CharacterAim aim,
        CharacterMover mover, ICharacterCommands commands, EnemyPerception perception,
        EnemyCharacterData data)
    {
        this.self = self;
        this.gun = gun;
        this.aim = aim;
        this.mover = mover;
        this.commands = commands;
        this.perception = perception;
        this.data = data;
    }

    public bool HasFireLine(EnemyBlackboard memory)
    {
        if (!memory.HasSight || memory.Target == null) return false;
        Vector3 muzzle = gun.MuzzlePosition;
        Vector3 target = BallisticAimPoint(memory);
        return perception.HasLineToTarget(muzzle, target, memory.Target);
    }

    public bool TryFire(EnemyBlackboard memory)
    {
        if (!memory.HasSight || !memory.HasConfirmedTarget || memory.Target == null ||
            !gun.CanFireNow) return false;

        if (!mover.IsGrounded || mover.PlaneVelocity.sqrMagnitude > 0.2f * 0.2f)
        {
            return false;
        }

        Vector3 muzzle = gun.MuzzlePosition;
        Vector3 target = BallisticAimPoint(memory);
        Vector3 distance = target - muzzle;
        distance.y = 0f;
        Vector3 velocity = mover.PlaneVelocity;
        commands.SetAim(target);
        if (distance.magnitude > gun.EffectiveRange || velocity.magnitude > 0.2f ||
            aim.HorizontalAimError(target) > data.aimTolerance ||
            !perception.HasLineToTarget(muzzle, target, memory.Target))
        {
            return false;
        }

        return commands.TryFire();
    }

    public Vector3 BallisticAimPoint(EnemyBlackboard memory)
    {
        Vector3 point = memory.LastAimPoint;
        point.y = gun.MuzzlePosition.y;
        return point;
    }
}
