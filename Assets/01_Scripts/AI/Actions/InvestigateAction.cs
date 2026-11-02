using UnityEngine;

namespace SteelHunter.AI.Character
{
    public sealed class InvestigateAction : EnemyCharacterAction
    {
        private float deadline;
        private float handledThrough;
        public override EnemyAction Kind => EnemyAction.Investigate;
        public InvestigateAction(EnemyActionContext context) : base(context) { }

        public override void Enter(float now)
        {
            deadline = now + Context.Profile.investigationDuration;
            handledThrough = float.NegativeInfinity;
            Context.Memory.ReturnRequested = false;
        }

        public override bool IsComplete(float now) => now >= deadline;

        public override void Tick(float now)
        {
            if (IsComplete(now)) { Context.Commands.SetMove(Vector3.zero, false); return; }
            Vector3 destination = Context.Memory.SuspicionPosition;
            handledThrough = Mathf.Max(handledThrough, Context.Memory.SuspicionAt);
            Scan(destination - Context.Self.transform.position, 60f, now);
            Context.Navigator.MoveTo(destination, false, now);
        }

        public override void Exit(EnemyActionExitReason reason, float now)
        {
            if (reason == EnemyActionExitReason.Completed || reason == EnemyActionExitReason.Interrupted)
                Context.Memory.AcknowledgeClue(handledThrough);
            if (reason == EnemyActionExitReason.Completed) Context.Memory.ReturnRequested = true;
        }
    }
}
