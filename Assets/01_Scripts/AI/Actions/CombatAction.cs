using UnityEngine;

namespace SteelHunter.AI.Character
{
    public sealed class CombatAction : EnemyCharacterAction
    {
        private int burstRemaining;
        private float reactionEndsAt;
        private float pauseEndsAt;
        private float nextCoverEvaluationAt;
        private float reloadStartedAt;
        private Vector3 retreatDestination;
        private bool hasRetreatDestination;
        public override EnemyAction Kind => EnemyAction.Combat;
        public CombatAction(EnemyActionContext context) : base(context) { }

        public override void Enter(float now)
        {
            reactionEndsAt = now + Random.Range(Context.Profile.reactionDelay.x, Context.Profile.reactionDelay.y);
            burstRemaining = Random.Range(Context.Profile.burstRounds.x, Context.Profile.burstRounds.y + 1);
            pauseEndsAt = reactionEndsAt;
            reloadStartedAt = float.NegativeInfinity;
            hasRetreatDestination = false;
            nextCoverEvaluationAt = 0f;
            Context.Memory.ReturnRequested = false;
        }

        public override void Tick(float now)
        {
            if (!Context.Memory.HasSight || Context.Memory.Target == null)
            {
                Context.Commands.SetMove(Vector3.zero, false);
                return;
            }
            Vector3 threat = Context.Memory.LastSeenPosition;
            Context.Commands.SetAim(Context.FireControl.BallisticAimPoint(Context.Memory));
            if (HandleEmptyAmmo(threat, now)) return;
            reloadStartedAt = float.NegativeInfinity;
            if (Context.Gun.IsReloading) { Context.Commands.SetMove(Vector3.zero, false); return; }
            if (!TryPositionForCombat(threat, now)) return;
            Context.Commands.SetMove(Vector3.zero, false);
            TickBurst(now);
        }

        private bool HandleEmptyAmmo(Vector3 threat, float now)
        {
            if (Context.Gun.LoadedRounds > 0) return false;
            if (Context.Gun.ReserveRounds > 0) { TickReload(threat, now); return true; }
            Context.Cover.Release();
            if (!hasRetreatDestination)
            {
                Vector3 away = Context.Self.transform.position - threat;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Context.Memory.HomeForward;
                retreatDestination = Context.Self.transform.position + away.normalized * 4f;
                hasRetreatDestination = true;
            }
            Context.Navigator.MoveTo(retreatDestination, true, now);
            return true;
        }

        private void TickReload(Vector3 threat, float now)
        {
            if (float.IsNegativeInfinity(reloadStartedAt)) reloadStartedAt = now;
            EvaluateCover(threat, false, now);
            if (Context.Cover.Current != null && now - reloadStartedAt < Context.Profile.reloadCoverTimeout &&
                Context.Navigator.MoveTo(Context.Cover.Current.HidePosition, true, now) !=
                CharacterPathStatus.Arrived) return;
            Context.Commands.SetMove(Vector3.zero, false);
            if (!Context.Gun.IsReloading) Context.Commands.TryReload();
        }

        private bool TryPositionForCombat(Vector3 threat, float now)
        {
            bool wantsCover = Context.Self.Health.CurrentHealth / Context.Self.Health.MaxHealth <=
                Context.Profile.retreatHealthFraction || now - Context.Memory.LastDamagedAt < 2f ||
                now - reactionEndsAt < 0.2f;
            if (wantsCover && Context.Cover.Current == null) EvaluateCover(threat, true, now);
            if (Context.Cover.Current != null) EvaluateCover(threat, true, now);
            if (Context.Cover.Current != null)
                return Context.Navigator.MoveTo(Context.Cover.Current.PeekPosition, false, now) ==
                    CharacterPathStatus.Arrived;

            float distance = FlatDistance(Context.Self.transform.position, threat);
            bool hasFireLine = Context.FireControl.HasFireLine(Context.Memory);
            if (Context.Navigator.PathFailures > 2 ||
                (distance >= Context.Profile.preferredMinRange - Context.Profile.rangeHysteresis &&
                 distance <= Context.Profile.preferredMaxRange + Context.Profile.rangeHysteresis && hasFireLine))
                return true;

            Vector3 direction = Context.Self.transform.position - threat;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Context.Memory.HomeForward;
            direction.Normalize();
            Vector3 destination = distance < Context.Profile.preferredMinRange
                ? Context.Self.transform.position + direction * 3f
                : threat + direction * Mathf.Lerp(Context.Profile.preferredMinRange,
                    Context.Profile.preferredMaxRange, 0.5f);
            if (!hasFireLine) destination += Quaternion.AngleAxis(90f, Vector3.up) * direction * 3f;
            if (FlatDistance(destination, Context.Memory.HomePosition) <= Context.Profile.pursuitRadius)
                Context.Navigator.MoveTo(destination, false, now);
            else Context.Commands.SetMove(Vector3.zero, false);
            return false;
        }

        private void EvaluateCover(Vector3 threat, bool needsPeek, float now)
        {
            if (now < nextCoverEvaluationAt) return;
            Context.Cover.Select(threat, Context.Memory.Target, Context.Memory.LastAimPoint, needsPeek);
            nextCoverEvaluationAt = now + 1f;
        }

        private void TickBurst(float now)
        {
            if (now < reactionEndsAt || now < pauseEndsAt) return;
            if (burstRemaining <= 0)
            {
                burstRemaining = Random.Range(Context.Profile.burstRounds.x, Context.Profile.burstRounds.y + 1);
                pauseEndsAt = now + Random.Range(Context.Profile.burstPause.x, Context.Profile.burstPause.y);
                return;
            }
            if (Context.FireControl.TryFire(Context.Memory)) burstRemaining--;
        }

        public override void Exit(EnemyActionExitReason reason, float now) { Context.Cover.Release(); }
    }
}
