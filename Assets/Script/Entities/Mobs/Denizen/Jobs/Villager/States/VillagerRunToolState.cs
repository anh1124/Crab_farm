using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerRunToolState : DenizenState
    {
        private readonly VillagerJob _job;

        public VillagerRunToolState(VillagerJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Status.currentTool == null)
            {
                _job.TransitionToIdle();
                return;
            }
            
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("run");
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Status.isInDanger)
            {
                _job.ChangeState(_job.FearState);
                return;
            }

            if (ctx.Status.currentTool == null || !ctx.Status.currentTool.gameObject.activeInHierarchy)
            {
                ctx.Status.currentTool = null;
                _job.TransitionToIdle();
                return;
            }

            ctx.Movement.FaceTarget(ctx.Status.currentTool.transform.position);
            ctx.Movement.MoveTo(ctx.Status.currentTool.transform.position, ctx.Status.RunSpeed);
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("run"))
                anim.Play("run");
        }
    }
}
