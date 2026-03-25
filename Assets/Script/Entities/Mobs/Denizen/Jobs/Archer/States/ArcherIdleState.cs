using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherIdleState : DenizenState
    {
        private readonly ArcherJob _job;

        public ArcherIdleState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            ctx.Movement.StopMovement();
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerIdle");
        }

        public override void Update(DenizenContext ctx)
        {
            // Nếu đang có alarm -> về base
            if (ctx.GuardManager != null && ctx.GuardManager.Alarm.isEnemyAttack)
            {
                _job.ChangeState(_job.ReturnToBaseState);
                return;
            }

            // Mặc định ban ngày -> Hunting, ban đêm -> ReturnToBase
            bool isDaytime = ctx.TimeManager == null || ctx.TimeManager.IsDaytime;
            _job.ChangeState(isDaytime ? (DenizenState)_job.HuntingState : _job.ReturnToBaseState);
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                anim.Play("archerIdle");
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"CurrentTarget: {(ctx.Status.currentTarget != null ? ctx.Status.currentTarget.name : "None")}";
        }
    }
}
