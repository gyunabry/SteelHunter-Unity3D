using UnityEngine;

public sealed class EnemyPerception
{
    private readonly CharacterRoot self;
    private readonly CharacterHealth health;
    private readonly EnemyCharacterData data;
    private readonly EnemyBlackboard blackboard;
    private readonly LayerMask targetMask;
    private readonly LayerMask sightMask;
    private readonly Collider[] candidates = new Collider[64];
    private readonly RaycastHit[] hits = new RaycastHit[64];
    private float lastSampleAt;
    private bool active;

    public EnemyPerception(CharacterRoot self, EnemyCharacterData data, EnemyBlackboard blackboard,
        LayerMask targetMask, LayerMask sightMask)
    {
        this.self = self;
        health = self.Health;
        this.data = data;
        this.blackboard = blackboard;
        this.targetMask = targetMask;
        this.sightMask = sightMask;
    }

    public void Activate()
    {
        if (active) return;
        active = true;
        lastSampleAt = 0f;
        CharacterStimulusBus.SoundReported += OnSound;
        health.Damaged += OnDamaged;
    }

    public void Deactivate()
    {
        if (!active) return;
        active = false;
        CharacterStimulusBus.SoundReported -= OnSound;
        health.Damaged -= OnDamaged;
    }

    public void Dispose() => Deactivate();

    public void Sample()
    {
        float now = Time.time;
        float dt = lastSampleAt <= 0f ? data.perceptionInterval : now - lastSampleAt;
        lastSampleAt = now;

        CharacterRoot selected = null;
        Vector3 selectedAim = default;
        float bestScore = float.NegativeInfinity;
        Vector3 eye = self.transform.position + Vector3.up * 0.45f;
        int count = Physics.OverlapSphereNonAlloc(eye, data.loseRange, candidates,
            targetMask, QueryTriggerInteraction.Ignore);
        Collider[] results = candidates;
        if (count == candidates.Length)
        {
            results = Physics.OverlapSphere(eye, data.loseRange, targetMask,
                QueryTriggerInteraction.Ignore);
            count = results.Length;
        }

        for (int i = 0; i < count; i++)
        {
            CharacterRoot target = results[i].GetComponentInParent<CharacterRoot>();
            if (!IsHostile(target)) continue;

            Vector3 aim = results[i].bounds.center;
            Vector3 toAim = aim - eye;
            float distance = FlatDistance(self.transform.position, target.transform.position);
            float allowedRange = target == blackboard.Target ? data.loseRange : data.detectionRange;
            if (distance > allowedRange || toAim.sqrMagnitude < 0.001f ||
                Vector3.Angle(self.transform.forward, toAim) > data.fieldOfView * 0.5f ||
                !HasLineToTarget(eye, aim, target)) continue;

            float score = -distance + (target == blackboard.Target ? 3f : 0f);
            if (score <= bestScore) continue;
            bestScore = score;
            selected = target;
            selectedAim = aim;
        }

        blackboard.HasSight = selected != null;
        if (selected == null)
        {
            blackboard.Awareness = Mathf.Max(0f, blackboard.Awareness - dt);
            if (blackboard.Target != null &&
                (blackboard.Target.Health == null || !blackboard.Target.Health.IsAlive))
                blackboard.ForgetTarget();
            return;
        }

        if (blackboard.Target != selected)
        {
            blackboard.Target = selected;
            blackboard.Awareness = 0f;
            blackboard.HasConfirmedTarget = false;
        }

        blackboard.LastSeenPosition = selected.transform.position;
        blackboard.LastAimPoint = selectedAim;
        blackboard.LastSeenAt = now;
        blackboard.Awareness += Mathf.Max(0f, dt);
        if (blackboard.Awareness >= data.confirmationTime)
            blackboard.HasConfirmedTarget = true;
    }

    public bool HasLineToTarget(Vector3 origin, Vector3 destination, CharacterRoot target)
    {
        if (!IsHostile(target)) return false;
        Vector3 offset = destination - origin;
        float distance = offset.magnitude;
        if (distance < 0.001f) return false;
        int count = Physics.RaycastNonAlloc(origin, offset / distance, hits, distance + 0.1f,
            sightMask, QueryTriggerInteraction.Ignore);
        RaycastHit[] results = hits;
        if (count == hits.Length)
        {
            results = Physics.RaycastAll(origin, offset / distance, distance + 0.1f,
                sightMask, QueryTriggerInteraction.Ignore);
            count = results.Length;
        }

        Collider nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider collider = results[i].collider;
            if (collider == null || collider.transform.IsChildOf(self.transform) ||
                results[i].distance >= nearestDistance) continue;
            nearest = collider;
            nearestDistance = results[i].distance;
        }
        return nearest != null && nearest.GetComponentInParent<CharacterRoot>() == target;
    }

    private bool IsHostile(CharacterRoot target)
    {
        return target != null && target != self && target.Health != null &&
            target.Health.IsAlive && self.Team != CharacterTeam.Neutral &&
            target.Team != CharacterTeam.Neutral && !self.IsFriendlyTo(target);
    }

    private void OnSound(CharacterSound sound)
    {
        if (!IsHostile(sound.Source)) return;
        float distance = FlatDistance(self.transform.position, sound.Position);
        if (distance > sound.Radius) return;

        Vector3 origin = self.transform.position + Vector3.up * 0.45f;
        Vector3 destination = sound.Position + Vector3.up * 0.45f;
        Vector3 delta = destination - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude,
            sightMask, QueryTriggerInteraction.Ignore);
        bool blocked = false;
        for (int i = 0; i < count; i++)
        {
            Collider hit = hits[i].collider;
            if (hit == null || hit.transform.IsChildOf(self.transform) ||
                hit.transform.IsChildOf(sound.Source.transform)) continue;
            blocked = true;
            break;
        }
        if (blocked && distance > sound.Radius * data.soundOcclusionFactor) return;

        blackboard.LastHeardPosition = sound.Position;
        blackboard.LastHeardAt = Time.time;
    }

    private void OnDamaged(Vector3 hitPoint, Vector3 shotDirection)
    {
        if (shotDirection.sqrMagnitude < 0.01f) return;
        blackboard.DamageThreatPosition = hitPoint - shotDirection.normalized * 5f;
        blackboard.LastDamagedAt = Time.time;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 difference = b - a;
        difference.y = 0f;
        return difference.magnitude;
    }
}
