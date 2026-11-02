using UnityEngine;

namespace SteelHunter.AI.Character
{
    public sealed class PatrolAction : EnemyCharacterAction
    {
        private int patrolIndex;
        private float waitUntil;
        public override EnemyAction Kind => EnemyAction.Patrol;
        public PatrolAction(EnemyActionContext context) : base(context) { }

        public override void Tick(float now)
        {
            Scan(Context.Memory.HomeForward, 50f, now);
            Transform[] points = Context.Memory.PatrolPoints;
            if (points == null || points.Length == 0 || now < waitUntil)
            {
                Context.Commands.SetMove(Vector3.zero, false);
                return;
            }
            Transform point = points[patrolIndex % points.Length];
            if (point == null) { patrolIndex++; return; }
            CharacterPathStatus status = Context.Navigator.MoveTo(point.position, false, now);
            if (status != CharacterPathStatus.Arrived && Context.Navigator.PathFailures <= 2) return;
            patrolIndex++;
            Context.Navigator.ResetFailures();
            waitUntil = now + Context.Profile.waypointWait;
        }
    }
}
