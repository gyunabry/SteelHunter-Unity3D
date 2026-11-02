using System.Collections.Generic;
using UnityEngine;

public sealed class CoverPoint : MonoBehaviour
{
    private static readonly List<CoverPoint> active = new List<CoverPoint>();
    [SerializeField] private Transform hidePosition;
    [SerializeField] private Transform peekPosition;
    [SerializeField] private Collider blocker;
    private CharacterRoot reserver;

    public static IReadOnlyList<CoverPoint> Active => active;
    public Vector3 HidePosition => hidePosition != null ? hidePosition.position : transform.position;
    public Vector3 PeekPosition => peekPosition != null ? peekPosition.position : transform.position;
    public bool IsAvailableFor(CharacterRoot actor) => reserver == null || reserver == actor;

    private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
    private void OnDisable() { active.Remove(this); reserver = null; }

    public bool TryReserve(CharacterRoot actor)
    {
        if (!isActiveAndEnabled || actor == null || !IsAvailableFor(actor)) return false;
        reserver = actor;
        return true;
    }

    public void Release(CharacterRoot actor)
    {
        if (reserver == actor) reserver = null;
    }

    public bool BlocksThreat(Vector3 threat, float characterHeight)
    {
        if (blocker == null) return false;
        Vector3 origin = threat + Vector3.up * characterHeight;
        Vector3 destination = HidePosition + Vector3.up * characterHeight;
        Vector3 offset = destination - origin;
        return offset.sqrMagnitude > 0.01f &&
            blocker.Raycast(new Ray(origin, offset.normalized), out _, offset.magnitude);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(HidePosition, 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(PeekPosition, 0.25f);
        Gizmos.DrawLine(HidePosition, PeekPosition);
    }
}
