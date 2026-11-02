using UnityEngine;

namespace SteelHunter.AI.Character
{
    public sealed class ReturnAction : EnemyCharacterAction
    {
        private bool arrived;
        private float observedAt;
        private CharacterRoot observedTarget;
        public override EnemyAction Kind => EnemyAction.Return;
        public ReturnAction(EnemyActionContext context) : base(context) { }

        public override void Enter(float now)
        {
            arrived = false;
            observedAt = Context.Memory.LastSeenAt;
            observedTarget = Context.Memory.Target;
            Context.Memory.ReturnRequested = true;
        }

        public override bool IsComplete(float now) => arrived;

        public override void Tick(float now)
        {
            Scan(Context.Memory.HomePosition - Context.Self.transform.position, 40f, now);
            arrived = Context.Navigator.MoveTo(Context.Memory.HomePosition, false, now) ==
                CharacterPathStatus.Arrived;
        }

        public override void Exit(EnemyActionExitReason reason, float now)
        {
            if (reason != EnemyActionExitReason.Completed) return;
            Context.Memory.ForgetTargetIfUnchanged(observedTarget, observedAt);
            Context.Memory.ReturnRequested = false;
        }
    }
}
