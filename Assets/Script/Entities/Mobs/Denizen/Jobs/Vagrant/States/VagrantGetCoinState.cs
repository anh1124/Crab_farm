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

            // Safety gate: nếu có đe dọa thì ưu tiên Fear ngay cả khi đang đứng trên coin chờ nhặt.
            if (ctx.Status != null && ctx.Status.isFearPending)
            {
                vaSoul.ChangeState(vaSoul.FearState);
                return;
            }

            // Nếu đồng xu đã bị người khác lượm hoặc biến mất
            if (ctx.Status.coinTarget == null || !ctx.Status.coinTarget.activeInHierarchy)
            {
                vaSoul.ChangeState(vaSoul.IdleState);
                return;
            }

            // Liên tục cập nhật mục tiêu theo vị trí đồng xu (để lượm chuẩn xác)
            bool reachedCoin = vaSoul.HasReachedPosition(ctx.Status.coinTarget.transform.position, 0.5f);
            if (reachedCoin)
            {
                ctx.Movement?.Stop();
                return;
            }

            if (ctx.Movement != null)
                ctx.Movement.SetTarget(ctx.Status.coinTarget.transform.position, ctx.Status.runSpeed);
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;
            if (ctx.Status != null && ctx.Status.isFearPending) return;

            if (ctx.Status != null && ctx.Status.isMoving)
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("run"))
                    ctx.Animator.Play("run");
            }
            else
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
