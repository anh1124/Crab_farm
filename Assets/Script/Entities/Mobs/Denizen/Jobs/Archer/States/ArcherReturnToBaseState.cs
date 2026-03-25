using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.MobsManager;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherReturnToBaseState : DenizenState
    {
        private readonly ArcherJob _job;

        public ArcherReturnToBaseState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerMove");
        }

        public override void Update(DenizenContext ctx)
        {
            Vector3 targetPos = GetBasePosition(ctx);
            ctx.Movement.FaceTarget(targetPos);
            ctx.Movement.MoveTo(targetPos, ctx.Status.RunSpeed);

            if (ctx.Movement.HasReachedTarget)
            {
                _job.ChangeState(_job.HoldWallState);
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                anim.Play("archerMove");
        }

        private Vector3 GetBasePosition(DenizenContext ctx)
        {
            return GuardManager.Instance != null 
                ? GuardManager.Instance.Squad.GetDefensePositionForArcher(ctx.GameObject) 
                : ctx.Transform.position;
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            Vector3 basePos = GetBasePosition(ctx);
            return $"BasePos: {basePos} | DistToBase: {Vector3.Distance(ctx.Transform.position, basePos):F1}m";
        }
    }
}
