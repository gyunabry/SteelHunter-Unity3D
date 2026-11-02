using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ray가 도달하는 위치를 표현하는 구조체
public struct ViewCastInfo
{
    public bool hit;
    public Vector3 point;
    public float distance;
    public float angle;

    public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle)
    {
        hit = _hit;
        point = _point;
        distance = _distance;
        angle = _angle;
    }
}

// 레이스캐스트 시 두 정점을 간선으로 저장하기 위한 구조체
public struct Edge
{
    public Vector3 PointA, PointB;
    public Edge(Vector3 _PointA, Vector3 _PointB)
    {
        PointA = _PointA;
        PointB = _PointB;
    }
}

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private float viewRadius;
    [SerializeField, Range(0, 360)] public float viewAngle;
    [Tooltip("View origin height relative to the character pivot, in local units.")]
    [SerializeField] private float eyeHeight;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float delay = 0.2f;

    [SerializeField] private float meshResolution;
    // 얻어낸 정점 리스트로 폴리곤 메쉬를 만들어 저장할 변수
    private Mesh viewMesh;
    [SerializeField] private MeshFilter viewMeshFilter;

    [SerializeField] private int edgeResolveIterations = 50;
    [SerializeField] private float edgeDistanceThreshold;

    private List<Transform> visibleTargets = new();

    private WaitForSeconds delayWait;
    

    public float ViewRadius => viewRadius;
    public Vector3 ViewOrigin => transform.TransformPoint(Vector3.up * eyeHeight);
    public List<Transform> VisibleTargets => visibleTargets;
    public float MeshResolution => meshResolution;
    public MeshFilter ViewMeshFilter => viewMeshFilter;
         
    private void Awake()
    {
        delayWait = new WaitForSeconds(delay);
    }

    private void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        StartCoroutine(FindTargetsWithDelay());
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    private IEnumerator FindTargetsWithDelay()
    {
        while (true)
        {
            yield return delayWait;
            FindVisibleTargets();
        }
    } 

    private void FindVisibleTargets()
    {
        visibleTargets.Clear();

        Vector3 origin = ViewOrigin;
        Collider[] targetsInViewRadius = Physics.OverlapSphere(origin, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 targetPosition = targetsInViewRadius[i].bounds.center;
            Vector3 dirToTarget = (targetPosition - origin).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(origin, targetPosition);

                if (!Physics.Raycast(origin, dirToTarget, distanceToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    /// <summary>
    /// 레이캐스트 결과를 ViewCastInfo로 반환하는 메서드
    /// </summary>
    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        Vector3 origin = ViewOrigin;
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, origin + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    private void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new();
        ViewCastInfo preViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);
            
            // 0번째 정점은 원점이기 때문에 검사하지 않음
            if (i != 0)
            {
                bool edgeDistanceThresholdExceed = Mathf.Abs(preViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;

                // 둘 중 한 raycast가 장애물을 만나지 않았거나,
                // 두 raycast가 서로 다른 장애물에 hit된 것이라면 (edgeDistanceThreshold로 검사)
                if (preViewCast.hit != newViewCast.hit || (preViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceed))
                {
                    Edge e = FindEdge(preViewCast, newViewCast);

                    if (e.PointA != Vector3.zero)
                    {
                        viewPoints.Add(e.PointA);
                    }

                    if (e.PointB != Vector3.zero)
                    {
                        viewPoints.Add(e.PointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            preViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        vertices[0] = viewMeshFilter.transform.InverseTransformPoint(ViewOrigin);

        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = viewMeshFilter.transform.InverseTransformPoint(viewPoints[i]);
            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    /// <summary>
    /// 두 ViewCastInfo를 통해 보간 간선을 찾는 메서드
    /// 이진 탐색
    /// </summary>
    private Edge FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = minAngle + (maxAngle - minAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);
            bool edgeDistanceThresholdExceed = Mathf.Abs(minViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDistanceThresholdExceed)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new Edge(minPoint, maxPoint);
    }

    // y축 오일러 각을 3차원 방향 벡터로 변환한다.
    // 원본과 구현이 살짝 다름에 주의. 결과는 같다.
    public Vector3 DirFromAngle(float angleDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Cos((-angleDegrees + 90) * Mathf.Deg2Rad), 0, Mathf.Sin((-angleDegrees + 90) * Mathf.Deg2Rad));
    }
}