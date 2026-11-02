using UnityEngine;

[RequireComponent(typeof(EnemyCharacterBrain))]
public sealed class EnemyCharacterDebugView : MonoBehaviour
{
    [SerializeField] private bool showOverlay = true;

    [Header("Perception Gizmos")]
    [SerializeField] private bool showPerceptionGizmos = true;
    [SerializeField, Range(12, 128)]
    private int perceptionArcSegments = 48;

    private EnemyCharacterBrain brain;
    private CharacterGun gun;

    private void Awake()
    {
        brain = GetComponent<EnemyCharacterBrain>();
        gun = GetComponent<CharacterGun>();
    }

    private void OnGUI()
    {
        if (!showOverlay || Camera.main == null || brain == null || brain.Memory == null) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screen.z <= 0f) return;
        EnemyBlackboard memory = brain.Memory;
        float age = Time.time - memory.LastSeenAt;
        string label = $"{brain.ActiveAction}  Seen:{memory.HasSight}  " +
            $"Aware:{memory.Awareness:0.0}  Memory:{(float.IsInfinity(age) ? -1f : age):0.0}s\n" +
            $"Ammo:{gun.LoadedRounds}/{gun.ReserveRounds}  Reload:{gun.IsReloading}\n" +
            $"PathFail:{brain.PathFailures}  Cover:{(brain.ReservedCover != null ? brain.ReservedCover.name : "-")}";
        GUI.Label(new Rect(screen.x - 140f, Screen.height - screen.y - 66f, 400f, 66f), label);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showPerceptionGizmos) return;

        // 편집 모드에서는 Awake()가 실행되지 않았을 수 있으므로
        // 기존 brain 필드 대신 직접 컴포넌트를 가져옵니다.
        EnemyCharacterBrain selectedBrain = GetComponent<EnemyCharacterBrain>();
        if (selectedBrain == null || selectedBrain.Profile == null) return;

        EnemyCharacterData data = selectedBrain.Profile;

        // EnemyPerception.Sample()에서 사용하는 눈 위치와 동일합니다.
        Vector3 eye = transform.position + Vector3.up * 0.45f;

        // 수평면에서 시야 범위를 표시합니다.
        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Color previousColor = Gizmos.color;

        try
        {
            // 이미 발견한 대상을 계속 감지할 수 있는 범위
            Gizmos.color = new Color(1f, 0.55f, 0f);
            DrawSightSector(
                eye,
                forward,
                data.loseRange,
                data.fieldOfView
            );

            // 새로운 대상을 발견하는 범위
            Gizmos.color = Color.green;
            DrawSightSector(
                eye,
                forward,
                data.detectionRange,
                data.fieldOfView
            );

            // 눈 위치 및 정면 방향
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(eye, 0.1f);
            Gizmos.DrawLine(
                eye,
                eye + forward * Mathf.Min(2f, data.detectionRange)
            );
        }
        finally
        {
            Gizmos.color = previousColor;
        }
    }

    private void DrawSightSector(
        Vector3 origin,
        Vector3 forward,
        float radius,
        float fieldOfView)
    {
        if (radius <= 0f) return;

        float angle = Mathf.Clamp(fieldOfView, 0f, 360f);
        float halfAngle = angle * 0.5f;
        int segments = Mathf.Clamp(perceptionArcSegments, 12, 128);

        Vector3 firstDirection =
            Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;

        Vector3 firstPoint = origin + firstDirection * radius;
        Vector3 previousPoint = firstPoint;

        // 여러 선분으로 부채꼴의 원호를 그립니다.
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);

            Vector3 direction =
                Quaternion.AngleAxis(currentAngle, Vector3.up) * forward;

            Vector3 currentPoint = origin + direction * radius;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // 360도 시야는 원으로만 표시합니다.
        if (angle < 359.99f)
        {
            Gizmos.DrawLine(origin, firstPoint);
            Gizmos.DrawLine(origin, previousPoint);
        }
    }
}
