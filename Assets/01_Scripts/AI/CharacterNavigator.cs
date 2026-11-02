using UnityEngine;
using UnityEngine.AI;

public enum CharacterPathStatus { Moving, Arrived, Failed }

public sealed class CharacterNavigator
{
    private readonly Transform self;
    private readonly CharacterMover mover;
    private readonly ICharacterCommands commands;
    private readonly NavMeshQueryFilter filter;
    private readonly NavMeshPath path = new NavMeshPath();
    private Vector3[] corners;
    private int cornerIndex;
    private float lastProgressAt;
    private Vector3 lastProgressPosition;
    private float stuckTimeout;
    private readonly float pathRetryInterval;
    private float nextPathAttemptAt;
    private Vector3 pathGoal;
    private bool hasPathGoal;

    public int PathFailures { get; private set; }

    public bool HasPath => corners != null && cornerIndex < corners.Length;
    public Vector3 Destination { get; private set; }

    public CharacterNavigator(Transform self, CharacterMover mover, ICharacterCommands commands,
        int agentTypeId, int areaMask, float stuckTimeout, float pathRetryInterval = 0.5f)
    {
        this.self = self;
        this.mover = mover;
        this.commands = commands;
        this.stuckTimeout = stuckTimeout;
        this.pathRetryInterval = pathRetryInterval;
        filter = new NavMeshQueryFilter { agentTypeID = agentTypeId, areaMask = areaMask };
    }

    public bool CanReach(Vector3 destination, out Vector3 onMesh, out float length)
    {
        onMesh = default;
        length = 0f;
        if (!NavMesh.SamplePosition(self.position, out NavMeshHit start, 2f, filter) ||
            !NavMesh.SamplePosition(destination, out NavMeshHit end, 2f, filter) ||
            !NavMesh.CalculatePath(start.position, end.position, filter, path) ||
            path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2) return false;

        Vector3[] points = path.corners;
        for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
        onMesh = end.position;
        return true;
    }

    public bool TrySetDestination(Vector3 destination)
        => TrySetDestination(destination, Time.time);

    private bool TrySetDestination(Vector3 destination, float now)
    {
        Stop();
        if (!CanReach(destination, out Vector3 onMesh, out _)) return false;
        corners = (Vector3[])path.corners.Clone();
        cornerIndex = 1;
        Destination = onMesh;
        lastProgressAt = now;
        lastProgressPosition = self.position;
        return true;
    }

    public CharacterPathStatus Tick(bool sprint = false)
        => Tick(sprint, Time.time);

    private CharacterPathStatus Tick(bool sprint, float now)
    {
        if (!HasPath) return CharacterPathStatus.Failed;
        while (cornerIndex < corners.Length - 1 &&
            FlatDistance(self.position, corners[cornerIndex]) < 0.7f) cornerIndex++;

        Vector3 delta = corners[cornerIndex] - self.position;
        delta.y = 0f;
        if (cornerIndex == corners.Length - 1 && delta.magnitude < 0.7f)
        {
            commands.SetMove(Vector3.zero, false);
            Vector3 velocity = mover.PlaneVelocity;
            if (velocity.magnitude < 0.2f)
            {
                Stop();
                return CharacterPathStatus.Arrived;
            }
            return CharacterPathStatus.Moving;
        }

        if (FlatDistance(self.position, lastProgressPosition) >= 0.25f)
        {
            lastProgressPosition = self.position;
            lastProgressAt = now;
        }
        if (now - lastProgressAt >= stuckTimeout)
        {
            Stop();
            return CharacterPathStatus.Failed;
        }

        commands.SetMove(delta.normalized, sprint);
        return CharacterPathStatus.Moving;
    }

    public void Stop()
    {
        corners = null;
        cornerIndex = 0;
        hasPathGoal = false;
        commands.SetMove(Vector3.zero, false);
    }

    public void BeginAction()
    {
        Stop();
        PathFailures = 0;
        nextPathAttemptAt = 0f;
    }

    public void ResetFailures() { PathFailures = 0; }

    public CharacterPathStatus MoveTo(Vector3 destination, bool sprint, float now)
    {
        if (FlatDistance(self.position, destination) < 0.7f)
        {
            commands.SetMove(Vector3.zero, false);

            // 목적지 반경에 들어와도 감속이 끝날 때까지 이동 중으로 취급한다.
            if (mover.PlaneVelocity.sqrMagnitude > 0.2f * 0.2f)
            {
                return CharacterPathStatus.Moving;
            }

            Stop();
            return CharacterPathStatus.Arrived;
        }

        if ((!HasPath || !hasPathGoal || FlatDistance(pathGoal, destination) > 1.5f) &&
            now >= nextPathAttemptAt)
        {
            nextPathAttemptAt = now + pathRetryInterval;
            bool found = TrySetDestination(destination, now);
            pathGoal = destination;
            hasPathGoal = true;
            if (!found) { PathFailures++; return CharacterPathStatus.Failed; }
        }
        if (!HasPath) return CharacterPathStatus.Failed;
        CharacterPathStatus status = Tick(sprint, now);
        if (status == CharacterPathStatus.Failed) PathFailures++;
        return status;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        delta.y = 0f;
        return delta.magnitude;
    }
}
