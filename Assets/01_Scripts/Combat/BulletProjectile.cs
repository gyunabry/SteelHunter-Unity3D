using UnityEngine;

public sealed class BulletProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.15f;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    private CharacterRoot owner;
    private Vector3 direction;
    private float damage;
    private int armorPenetrationLevel;
    private bool isCritical;
    private float criticalDamageMultiplier;
    private float speed;
    private float remainingLifetime;
    private bool initialized;

    public float HitRadius => hitRadius;

    public void Initialize(
        CharacterRoot characterOwner,
        Vector3 worldDirection,
        float bulletDamage,
        int bulletArmorPenetrationLevel,
        bool bulletIsCritical,
        float bulletCriticalDamageMultiplier,
        float bulletSpeed,
        float lifetime)
    {
        owner = characterOwner;
        direction = worldDirection.normalized;
        damage = Mathf.Max(0f, bulletDamage);
        armorPenetrationLevel = bulletArmorPenetrationLevel;
        isCritical = bulletIsCritical;
        criticalDamageMultiplier = bulletCriticalDamageMultiplier;
        speed = Mathf.Max(0.01f, bulletSpeed);
        remainingLifetime = Mathf.Max(0.01f, lifetime);
        initialized = direction.sqrMagnitude > 0f;

        if (!initialized) Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!initialized) return;

        float stepTime = Mathf.Min(Time.fixedDeltaTime, remainingLifetime);
        float distance = speed * stepTime;
        Vector3 origin = transform.position;

        if (TryFindHit(origin, distance, out RaycastHit hit))
        {
            // SphereCast의 distance는 구 중심이 이동한 거리다.
            transform.position = origin + direction * hit.distance;

            // 시작부터 겹쳐 있으면 hit.point가 Vector3.zero일 수 있다.
            // 원점으로 순간이동시키지 않고 충돌체의 가까운 점을 사용한다.
            Vector3 impactPoint = hit.distance <= 0.0001f
                ? hit.collider.ClosestPoint(origin)
                : hit.point;

            // 이벤트에서 다른 코드가 호출되기 전에 탄을 처리 완료로 표시한다.
            initialized = false;

            CharacterHealth victim = hit.collider.GetComponentInParent<CharacterHealth>();

            if (victim != null)
            {
                CharacterStats stats = victim.GetComponentInParent<CharacterStats>();
                float finalDamage = BulletDamage.Calculate(
                    damage,
                    stats != null ? stats.ArmorLevel : -1,
                    armorPenetrationLevel,
                    isCritical,
                    criticalDamageMultiplier);

                bool applied = victim.ApplyDamage(finalDamage, impactPoint, direction);

                if (applied)
                {
                    CharacterRoot target = victim.GetComponentInParent<CharacterRoot>();

                    // 피격당한 캐릭터가 적군일때만 피격 피드백 커서 표시
                    if (owner != null && owner.Team == CharacterTeam.Player &&
                        target != null && target.Team == CharacterTeam.Enemy)
                    {
                        GameCursor.Current?.PlayHitFeedback();
                    }

                    HitText text = PoolManager.Instance.GetPool<HitText>();

                    text.Play(finalDamage, impactPoint, direction, Camera.main, isCritical);
                }
            }

            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;
        remainingLifetime -= stepTime;

        if (remainingLifetime <= 0f)
        {
            initialized = false;
            Destroy(gameObject);
        }
    }

    private bool TryFindHit(Vector3 origin, float distance, out RaycastHit nearestHit)
    {
        nearestHit = default;

        int count = Physics.SphereCastNonAlloc(
            origin, HitRadius, direction, hitBuffer, distance,
            hitMask, QueryTriggerInteraction.Ignore);

        RaycastHit[] hits = hitBuffer;

        // 버퍼가 꽉 찬 경우 NonAlloc 대신 All로 다시 검사한다.
        if (count == hitBuffer.Length)
        {
            hits = Physics.SphereCastAll(
                origin, HitRadius, direction, distance,
                hitMask, QueryTriggerInteraction.Ignore);

            count = hits.Length;
        }

        bool found = false;
        bool nearestIsWorld = false;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;

            if (collider == null) continue;
            if (collider.transform.IsChildOf(transform)) continue;
            if (owner != null && collider.transform.IsChildOf(owner.transform)) continue;

            CharacterRoot target = collider.GetComponentInParent<CharacterRoot>();
            if (owner != null && owner.IsFriendlyTo(target)) continue;

            bool isWorld = target == null;
            if (hit.distance > nearestDistance) continue;

            // 시작 겹침 등 거리가 같은 경우 환경 충돌을 우선한다.
            if (hit.distance == nearestDistance &&
                (!isWorld || nearestIsWorld)) continue;

            nearestHit = hit;
            nearestDistance = hit.distance;
            nearestIsWorld = isWorld;
            found = true;
        }

        return found;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, HitRadius);
    }
}
