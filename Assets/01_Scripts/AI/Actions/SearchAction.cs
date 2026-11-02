using UnityEngine;

namespace SteelHunter.AI.Character
{
    public sealed class SearchAction : EnemyCharacterAction
    {
        private int searchIndex;
        private float deadline;
        private float observedAt;
        private CharacterRoot observedTarget;
        public override EnemyAction Kind => EnemyAction.Search;
        public SearchAction(EnemyActionContext context) : base(context) { }

        public override void Enter(float now)
        {
            searchIndex = 0;
            observedAt = Context.Memory.LastSeenAt;
            observedTarget = Context.Memory.Target;
            deadline = Mathf.Min(now + Context.Profile.searchDuration,
                observedAt + Context.Profile.pursuitLimit);
            Context.Memory.ReturnRequested = false;
        }

        public override bool IsComplete(float now) => now >= deadline;

        public override void Tick(float now)
        {
            if (IsComplete(now)) { Context.Commands.SetMove(Vector3.zero, false); return; }
            Vector3 destination = searchIndex == 0 ? Context.Memory.LastSeenPosition :
                Context.Memory.LastSeenPosition + Quaternion.AngleAxis(120f * searchIndex, Vector3.up) *
                (Context.Memory.HomeForward * 2f);
            Scan(destination - Context.Self.transform.position, 90f, now);
            CharacterPathStatus status = Context.Navigator.MoveTo(destination, false, now);
            if (status != CharacterPathStatus.Arrived && Context.Navigator.PathFailures <= 2) return;
            searchIndex = Mathf.Min(3, searchIndex + 1);
            Context.Navigator.ResetFailures();
        }

        public override void Exit(EnemyActionExitReason reason, float now)
        {
            if (reason != EnemyActionExitReason.Completed) return;
            Context.Memory.ForgetTargetIfUnchanged(observedTarget, observedAt);
            Context.Memory.AcknowledgeClue(observedAt);
            Context.Memory.ReturnRequested = true;
        }
    }
}
