using UnityEngine;

/// <summary>
/// 마우스 위치에 Raycast를 통해 적을 감지하고 적이 있다면 플레이어 캐릭터의 muzzle point에서 해당 부
/// </summary>
public class PlayerAimResolver : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask sightMask;
    [SerializeField, Min(0.01f)] private float cursorAssistRadius = 0.25f;
    [SerializeField, Min(1f)] private float cameraQueryDistance = 200f;
    [SerializeField, Min(0.1f)] private float maxAssistDistance = 30f;
    [SerializeField, Min(0f)] private float maxHeightDifference = 3f;
    [SerializeField, Range(0f, 80f)] private float maxPitch = 25f;

    private CharacterRoot owner;
    private CharacterGun gun;

    private void Awake()
    {
        owner = GetComponent<CharacterRoot>();
        gun = GetComponent<CharacterGun>();
    }

    public bool TryResolve(Camera camera, Vector2 screenPosition, out Vector3 aimPoint)
    {
        aimPoint = default;
        
        Ray cursorRay = camera.ScreenPointToRay(screenPosition);

        if (gun.HasWeapon)
        {
            // 정확히 가리킨 적을 우선적으로 선택
            RaycastHit[] directHits = Physics.RaycastAll(cursorRay, cameraQueryDistance, targetMask, QueryTriggerInteraction.Ignore);
            if (TrySelect(camera, directHits, out aimPoint)) return true;

            RaycastHit[] assistedHits = Physics.SphereCastAll(cursorRay, Mathf.Max(0.01f, cursorAssistRadius), cameraQueryDistance, targetMask, QueryTriggerInteraction.Ignore);
            if (TrySelect(camera, assistedHits, out aimPoint)) return true;
        }

        // 적을 선택하지 못하면 총구 높이로 수평 사격
        Plane plane = new Plane(Vector3.up, gun.MuzzlePosition);
        if (!plane.Raycast(cursorRay, out float distance)) return false;

        aimPoint = cursorRay.GetPoint(distance);
        return true;
    }

    private bool TrySelect(
        Camera camera, RaycastHit[] hits, out Vector3 aimPoint)
    {
        aimPoint = default;
        bool found = false;
        float bestDistance = float.PositiveInfinity;
        Vector3 muzzle = gun.MuzzlePosition;
        float allowedDistance = Mathf.Min(maxAssistDistance, gun.EffectiveRange);

        foreach (RaycastHit hit in hits)
        {
            Collider collider = hit.collider;
            if (collider == null || hit.distance >= bestDistance) continue;

            CharacterRoot target =
                collider.GetComponentInParent<CharacterRoot>();
            if (target == null || target == owner ||
                !target.isActiveAndEnabled || target.Health == null ||
                !target.Health.IsAlive || owner.IsFriendlyTo(target) ||
                target.Team == CharacterTeam.Neutral) continue;

            // 현재 프리팹의 CapsuleCollider/CharacterController 중심을 사용한다.
            Vector3 point = collider.bounds.center;
            Vector3 delta = point - muzzle;
            float flatDistance = new Vector2(delta.x, delta.z).magnitude;
            if (flatDistance < 0.1f || delta.magnitude > allowedDistance ||
                Mathf.Abs(delta.y) > maxHeightDifference) continue;

            float pitch = Mathf.Atan2(Mathf.Abs(delta.y), flatDistance)
                * Mathf.Rad2Deg;
            if (pitch > maxPitch) continue;

            Vector3 screenPoint = camera.WorldToScreenPoint(point);
            if (screenPoint.z <= 0f ||
                !camera.pixelRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                continue;

            // Orthographic 카메라에서도 표적 픽셀의 올바른 Ray 원점을 사용한다.
            Ray viewRay = camera.ScreenPointToRay(
                new Vector2(screenPoint.x, screenPoint.y));
            if (!HasLineToTarget(viewRay.origin, point, target, 0f)) continue;

            // 카메라에 보여도 총구에서 막히면 자동 보정 대상으로 선택하지 않는다.
            if (!HasLineToTarget(
                muzzle, point, target, gun.ProjectileHitRadius)) continue;

            bestDistance = hit.distance;
            aimPoint = point;
            found = true;
        }

        return found;
    }

    private bool HasLineToTarget(
        Vector3 origin, Vector3 destination, CharacterRoot target, float radius)
    {
        Vector3 delta = destination - origin;
        float distance = delta.magnitude;
        if (distance < 0.001f) return false;

        Vector3 direction = delta / distance;
        int mask = sightMask.value | targetMask.value;
        RaycastHit[] hits = radius > 0f
            ? Physics.SphereCastAll(origin, radius, direction,
                distance + 0.01f, mask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(origin, direction,
                distance + 0.01f, mask, QueryTriggerInteraction.Ignore);

        CharacterRoot nearestCharacter = null;
        float nearestDistance = float.PositiveInfinity;
        bool nearestIsWorld = false;

        foreach (RaycastHit hit in hits)
        {
            Collider collider = hit.collider;
            if (collider == null ||
                collider.transform.IsChildOf(owner.transform)) continue;

            CharacterRoot character =
                collider.GetComponentInParent<CharacterRoot>();
            if (owner.IsFriendlyTo(character)) continue;

            bool isWorld = character == null;
            if (hit.distance > nearestDistance) continue;
            if (hit.distance == nearestDistance &&
                (!isWorld || nearestIsWorld)) continue;

            nearestDistance = hit.distance;
            nearestCharacter = character;
            nearestIsWorld = isWorld;
        }

        return nearestCharacter == target;
    }
}
