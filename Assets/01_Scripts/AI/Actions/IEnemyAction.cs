namespace SteelHunter.AI.Character
{
    public interface IEnemyAction
    {
        EnemyAction Kind { get; }
        void Enter(float now);
        void Tick(float now);
        bool IsComplete(float now);
        void Exit(EnemyActionExitReason reason, float now);
    }

    public enum EnemyActionExitReason { Completed, Interrupted, Died, Disabled }
}
