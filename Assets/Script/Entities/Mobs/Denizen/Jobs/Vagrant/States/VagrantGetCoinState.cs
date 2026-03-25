using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantGetCoinState : DenizenState
    {
        private readonly VagrantJob _job;

        public VagrantGetCoinState(VagrantJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Status.coinTarget == null)
            {
                _job.ChangeState(_job.IdleState);
                return;
            }
            
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("run");
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Status.coinTarget == null || !ctx.Status.coinTarget.gameObject.activeInHierarchy)
            {
                ctx.Status.coinTarget = null;
                _job.ChangeState(_job.IdleState);
                return;
            }

            ctx.Movement.FaceTarget(ctx.Status.coinTarget.transform.position);
            ctx.Movement.MoveTo(ctx.Status.coinTarget.transform.position, ctx.Status.RunSpeed);
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("run"))
            {
                anim.Play("run");
            }
        }
    }
}
