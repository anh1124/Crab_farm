using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantGetCoinState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Status == null || ctx.Status.coinTarget == null || !ctx.Status.coinTarget.activeInHierarchy)
            {
                ctx.VagrantJob?.ChangeState(ctx.VagrantJob.IdleState);
                return;
            }

            if (ctx.Animator != null) 
                ctx.Animator.Play("run");

            if (ctx.Movement != null)
                ctx.Movement.SetTarget(ctx.Status.coinTarget.transform.position, ctx.Status.runSpeed);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var vaSoul = ctx.VagrantJob;
            if (vaSoul == null) return;

            // Nếu đồng xu đã bị người khác lượm hoặc biến mất
            if (ctx.Status.coinTarget == null || !ctx.Status.coinTarget.activeInHierarchy)
            {
                vaSoul.ChangeState(vaSoul.IdleState);
                return;
            }

            // Liên tục cập nhật mục tiêu theo vị trí đồng xu (để lượm chuẩn xác)
            if (ctx.Movement != null)
                ctx.Movement.SetTarget(ctx.Status.coinTarget.transform.position, ctx.Status.runSpeed);
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;

            // Đảm bảo luôn chạy animation "run" trong lúc dí theo coin
            if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("run"))
                ctx.Animator.Play("run");
        }
    }
}
