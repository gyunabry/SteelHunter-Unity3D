using UnityEngine;

namespace SteelHunter.AI.Character
{
    public abstract class EnemyCharacterAction : IEnemyAction
    {
        protected EnemyActionContext Context { get; }
        public abstract EnemyAction Kind { get; }
        protected EnemyCharacterAction(EnemyActionContext context) { Context = context; }
        public virtual void Enter(float now) { }
        public abstract void Tick(float now);
        public virtual bool IsComplete(float now) => false;
        public virtual void Exit(EnemyActionExitReason reason, float now) { }

        protected void Scan(Vector3 basis, float amplitude, float now)
        {
            basis.y = 0f;
            if (basis.sqrMagnitude < 0.01f) basis = Context.Memory.HomeForward;
            float angle = Mathf.Sin(now * 0.8f) * amplitude;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * basis.normalized;
            Context.Commands.SetAim(Context.Self.transform.position + direction * 8f);
        }

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = b - a;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
