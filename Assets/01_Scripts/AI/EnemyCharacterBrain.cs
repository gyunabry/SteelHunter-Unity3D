using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using SteelHunter.AI.Character;

[RequireComponent(typeof(CharacterRoot))]
[DisallowMultipleComponent]
public sealed class EnemyCharacterBrain : MonoBehaviour
{
    [SerializeField] private EnemyCharacterData profile;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private LayerMask sightMask = ~0;
    [SerializeField] private int agentTypeId = 0;
    [SerializeField] private int areaMask = NavMesh.AllAreas;

    private CharacterRoot self;
    private CharacterMover mover;
    private CharacterNavigator navigation;
    private EnemyPerception perception;
    private CoverSelector cover;
    private EnemyBlackboard memory;
    private EnemyActionRunner runner;
    private BTNode decisionTree;
    private EnemyAction proposedAction;
    private float nextPerceptionAt;
    private float nextThinkAt;
    private float decisionTime;
    private Vector3 configuredHome;
    private bool hasConfiguredHome;
    private bool initialized;
    private bool active;

    public EnemyCharacterData Profile => profile;
    public EnemyAction ActiveAction => runner?.Current ?? EnemyAction.None;
    public EnemyBlackboard Memory => memory;
    public int PathFailures => navigation?.PathFailures ?? 0;
    public CoverPoint ReservedCover => cover?.Current;

    public void Configure(Transform[] points, Vector3 home)
    {
        patrolPoints = points;
        configuredHome = home;
        hasConfiguredHome = true;
        if (memory == null) return;
        memory.PatrolPoints = points ?? System.Array.Empty<Transform>();
        memory.HomePosition = home;
    }

    private void OnEnable()
    {
        if (initialized && self.Health.IsAlive) ActivateRuntime();
    }

    private void Start()
    {
        self = GetComponent<CharacterRoot>();
        CharacterGun gun = GetComponent<CharacterGun>();
        CharacterAim aim = GetComponent<CharacterAim>();
        mover = GetComponent<CharacterMover>();
        CharacterCommandReceiver commands = GetComponent<CharacterCommandReceiver>();

        if (profile == null || self == null || !self.IsInitialized ||
            self.Team != CharacterTeam.Enemy || self.Health == null ||
            gun == null || !gun.IsInitialized || aim == null || mover == null ||
            commands == null || !commands.IsInitialized ||
            GetComponent<PlayerCharacterInput>() != null || GetComponent<PlayerInput>() != null)
        {
            Debug.LogError("Enemy character prefab has missing references or competing player input.", this);
            enabled = false;
            return;
        }

        memory = new EnemyBlackboard
        {
            HomePosition = hasConfiguredHome ? configuredHome : transform.position,
            HomeForward = transform.forward,
            PatrolPoints = patrolPoints ?? System.Array.Empty<Transform>()
        };

        navigation = new CharacterNavigator(
            transform, 
            mover, 
            commands, 
            agentTypeId,
            areaMask, 
            profile.stuckTimeout, 
            profile.pathRetryInterval);

        perception = new EnemyPerception(self, profile, memory, targetMask, sightMask);

        var fireControl = new EnemyFireControl(self, gun, aim, mover, commands, perception, profile);

        cover = new CoverSelector(self, navigation, perception, profile);

        var context = new EnemyActionContext(
            self, 
            profile, 
            memory, 
            navigation, 
            fireControl,
            cover, 
            gun, 
            mover, 
            commands);

        runner = new EnemyActionRunner(new IEnemyAction[]
        {
            new SteelHunter.AI.Character.PatrolAction(context),
            new SteelHunter.AI.Character.InvestigateAction(context),
            new SteelHunter.AI.Character.SearchAction(context),
            new SteelHunter.AI.Character.CombatAction(context),
            new SteelHunter.AI.Character.ReturnAction(context),
            new SteelHunter.AI.Character.DeadAction(context)
        }, memory, navigation.Stop, navigation.BeginAction, cover.Release);

        decisionTree = BuildDecisionTree();
        initialized = true;
        ActivateRuntime();
    }

    private void ActivateRuntime()
    {
        if (active) return;
        perception.Activate();
        self.Health.Died += OnDied;
        nextPerceptionAt = Time.time + Random.Range(0f, profile.perceptionInterval);
        nextThinkAt = Time.time + Random.Range(0f, profile.thinkInterval);
        active = true;
    }

    private void Update()
    {
        if (!active) return;
        float now = Time.time;
        if (!self.Health.IsAlive) { runner.Die(now); return; }
        if (Time.timeScale <= 0f) { navigation.Stop(); return; }

        bool shouldThink = now >= nextThinkAt;
        if (shouldThink) runner.ResolveCompletion(now);
        if (now >= nextPerceptionAt)
        {
            perception.Sample();
            nextPerceptionAt = now + profile.perceptionInterval;
        }
        if (shouldThink)
        {
            decisionTime = now;
            runner.Select(ChooseAction(), now);
            nextThinkAt = now + profile.thinkInterval;
        }
        runner.Tick(now);
    }

    private EnemyAction ChooseAction()
    {
        proposedAction = EnemyAction.Patrol;
        decisionTree.Evaluate();
        return proposedAction;
    }

    private BTNode BuildDecisionTree()
    {
        BTNode Select(EnemyAction action) => new BTAction(() =>
        {
            proposedAction = action;
            return BTNodeStatus.Running;
        });

        bool WithinLeash()
        {
            Vector3 delta = transform.position - memory.HomePosition;
            delta.y = 0f;
            return delta.magnitude <= profile.pursuitRadius;
        }

        return new BTSelector(
            // 사망 상태 -> dead
            new BTSequence(
                new BTCondition(() => !self.Health.IsAlive), 
                Select(EnemyAction.Dead)),

            // 추격 범위 내 + 시야/표적 확인 -> combat
            new BTSequence(
                new BTCondition(() => WithinLeash() && memory.HasSight && memory.HasConfirmedTarget), 
                Select(EnemyAction.Combat)),

            // 추격 범위 내 + 수색 조건 -> search
            new BTSequence(
                new BTCondition(() => WithinLeash() && 
                    ((memory.HasRecentSight(decisionTime, profile) && 
                    memory.LastSeenAt > memory.InvestigatedThrough) ||
                    runner.Current == EnemyAction.Search)), 
                Select(EnemyAction.Search)),

            // 추격 범위 내 + 조사 조건 -> investigate
            new BTSequence(
                new BTCondition(() => WithinLeash() && 
                    ((memory.HasSuspicion(decisionTime, profile) &&
                    memory.SuspicionAt > memory.InvestigatedThrough) ||
                    runner.Current == EnemyAction.Investigate)), 
                Select(EnemyAction.Investigate)),

            // 복귀 요청 or 추격 범위 밖 -> return
            new BTSequence(
                new BTCondition(() => memory.ReturnRequested || !WithinLeash()),
                Select(EnemyAction.Return)),

            // 기본 행동 -> patrol
            Select(EnemyAction.Patrol));
    }

    private void OnDied() { runner.Die(Time.time); }

    private void OnDisable()
    {
        if (!active) return;
        decisionTree.Abort();
        runner.Stop(EnemyActionExitReason.Disabled, Time.time);
        mover.StopImmediately();
        perception.Deactivate();
        self.Health.Died -= OnDied;
        active = false;
    }

    private void OnDestroy() { perception?.Dispose(); }

    private void OnDrawGizmosSelected()
    {
        if (memory == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(memory.LastSeenPosition, 0.45f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(memory.LastHeardPosition, 0.3f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(memory.HomePosition, profile != null ? profile.pursuitRadius : 1f);
    }
}
