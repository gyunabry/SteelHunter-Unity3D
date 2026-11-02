using UnityEngine;

public sealed class EnemyBlackboard
{
    public CharacterRoot Target;
    public Vector3 LastSeenPosition;
    public Vector3 LastAimPoint;
    public float LastSeenAt = float.NegativeInfinity;
    public bool HasSight;
    public bool HasConfirmedTarget;
    public float Awareness;

    public Vector3 LastHeardPosition;
    public float LastHeardAt = float.NegativeInfinity;
    public Vector3 DamageThreatPosition;
    public float LastDamagedAt = float.NegativeInfinity;
    public Vector3 HomePosition;
    public Vector3 HomeForward = Vector3.forward;
    public Transform[] PatrolPoints = System.Array.Empty<Transform>();
    public float InvestigatedThrough = float.NegativeInfinity;
    public bool ReturnRequested;
    public string ActiveAction;

    public bool HasRecentSight(float now, EnemyCharacterData data) =>
        HasConfirmedTarget && now - LastSeenAt <= data.sightMemoryDuration;

    public bool HasSuspicion(float now, EnemyCharacterData data) =>
        now - LastHeardAt <= data.investigationDuration ||
        now - LastDamagedAt <= data.investigationDuration ||
        (Awareness > 0f && now - LastSeenAt <= data.investigationDuration);

    public Vector3 SuspicionPosition =>
        LastDamagedAt > LastHeardAt && LastDamagedAt > LastSeenAt ? DamageThreatPosition :
        LastHeardAt > LastSeenAt ? LastHeardPosition : LastSeenPosition;

    public void ForgetTarget()
    {
        Target = null;
        HasConfirmedTarget = false;
        HasSight = false;
        Awareness = 0f;
    }

    public float SuspicionAt => Mathf.Max(LastSeenAt, LastHeardAt, LastDamagedAt);

    public void AcknowledgeClue(float through) =>
        InvestigatedThrough = Mathf.Max(InvestigatedThrough, through);

    public void ForgetTargetIfUnchanged(CharacterRoot observedTarget, float observedAt)
    {
        if (Target == observedTarget && LastSeenAt <= observedAt) ForgetTarget();
    }
}
