namespace SteelHunter.AI.Character
{
    public sealed class DeadAction : EnemyCharacterAction
    {
        public override EnemyAction Kind => EnemyAction.Dead;
        public DeadAction(EnemyActionContext context) : base(context) { }
        public override void Enter(float now)
        {
            Context.Commands.Stop();
            Context.Mover.StopImmediately();
        }
        public override void Tick(float now) { Context.Commands.Stop(); }
    }
}
