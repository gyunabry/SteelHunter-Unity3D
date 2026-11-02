using System;
using SteelHunter.AI.Character;

public enum EnemyAction 
{   
    None, 
    Patrol, 
    Investigate, 
    Search, 
    Combat, 
    Return, 
    Dead 
}

public sealed class EnemyActionRunner
{
    private readonly IEnemyAction[] actions = new IEnemyAction[(int)EnemyAction.Dead + 1];
    private readonly EnemyBlackboard memory;
    private readonly Action stopNavigation;
    private readonly Action beginNavigation;
    private readonly Action releaseCover;
    private IEnemyAction current;
    private bool died;
    public EnemyAction Current => current?.Kind ?? EnemyAction.None;

    public EnemyActionRunner(IEnemyAction[] registered, EnemyBlackboard memory,
        Action stopNavigation, Action beginNavigation, Action releaseCover)
    {
        this.memory = memory ?? throw new ArgumentNullException(nameof(memory));
        this.stopNavigation = stopNavigation ?? throw new ArgumentNullException(nameof(stopNavigation));
        this.beginNavigation = beginNavigation ?? throw new ArgumentNullException(nameof(beginNavigation));
        this.releaseCover = releaseCover ?? throw new ArgumentNullException(nameof(releaseCover));
        if (registered == null) throw new ArgumentNullException(nameof(registered));
        foreach (IEnemyAction action in registered)
        {
            if (action == null || action.Kind <= EnemyAction.None || action.Kind > EnemyAction.Dead ||
                actions[(int)action.Kind] != null)
                throw new ArgumentException("Enemy action registration is invalid or duplicated.");
            actions[(int)action.Kind] = action;
        }
        for (int i = 1; i < actions.Length; i++)
            if (actions[i] == null) throw new ArgumentException($"Enemy action {(EnemyAction)i} is missing.");
        memory.ActiveAction = EnemyAction.None.ToString();
    }

    public void Select(EnemyAction next, float now)
    {
        if (next < EnemyAction.None || next > EnemyAction.Dead) throw new ArgumentOutOfRangeException(nameof(next));
        if (died && next != EnemyAction.Dead) return;
        if (next == Current) return;
        EndCurrent(EnemyActionExitReason.Interrupted, now);
        if (next == EnemyAction.None) return;
        beginNavigation();
        current = actions[(int)next];
        memory.ActiveAction = next.ToString();
        current.Enter(now);
    }

    public void ResolveCompletion(float now)
    {
        if (current != null && current.IsComplete(now)) EndCurrent(EnemyActionExitReason.Completed, now);
    }

    public void Tick(float now) { current?.Tick(now); }

    public void Die(float now)
    {
        if (died) return;
        died = true;
        EndCurrent(EnemyActionExitReason.Died, now);
        releaseCover();
        beginNavigation();
        current = actions[(int)EnemyAction.Dead];
        memory.ActiveAction = EnemyAction.Dead.ToString();
        current.Enter(now);
    }

    public void Stop(EnemyActionExitReason reason, float now)
    {
        EndCurrent(reason, now);
        releaseCover();
    }

    private void EndCurrent(EnemyActionExitReason reason, float now)
    {
        IEnemyAction previous = current;
        current = null;
        memory.ActiveAction = EnemyAction.None.ToString();
        try { previous?.Exit(reason, now); }
        finally { stopNavigation(); }
    }
}
