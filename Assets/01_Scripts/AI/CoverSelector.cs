using UnityEngine;

public sealed class CoverSelector
{
    private readonly CharacterRoot self;
    private readonly CharacterNavigator navigator;
    private readonly EnemyPerception perception;
    private readonly EnemyCharacterData data;
    private CoverPoint current;

    public CoverPoint Current => current;

    public CoverSelector(CharacterRoot self, CharacterNavigator navigator,
        EnemyPerception perception, EnemyCharacterData data)
    {
        this.self = self;
        this.navigator = navigator;
        this.perception = perception;
        this.data = data;
    }

    public bool Select(Vector3 threat, CharacterRoot target, Vector3 aimPoint, bool needsPeek)
    {
        if (current != null && current.IsAvailableFor(self) && current.BlocksThreat(threat, 0.4f) &&
            (!needsPeek || (target != null &&
                perception.HasLineToTarget(current.PeekPosition + Vector3.up * 0.25f,
                    aimPoint, target))))
            return true;
        Release();

        CoverPoint best = null;
        float bestScore = float.NegativeInfinity;
        foreach (CoverPoint point in CoverPoint.Active)
        {
            if (point == null || !point.IsAvailableFor(self) ||
                Vector3.Distance(self.transform.position, point.HidePosition) > data.coverSearchRadius ||
                !point.BlocksThreat(threat, 0.4f) ||
                !navigator.CanReach(point.HidePosition, out _, out float length)) continue;

            if (needsPeek)
            {
                if (!navigator.CanReach(point.PeekPosition, out _, out _) || target == null ||
                    !perception.HasLineToTarget(point.PeekPosition + Vector3.up * 0.25f,
                        aimPoint, target)) continue;
            }

            float score = -length - Vector3.Distance(point.PeekPosition, aimPoint) * 0.05f;
            if (score <= bestScore) continue;
            best = point;
            bestScore = score;
        }

        if (best == null || !best.TryReserve(self)) return false;
        current = best;
        return true;
    }

    public void Release()
    {
        if (current != null) current.Release(self);
        current = null;
    }
}
