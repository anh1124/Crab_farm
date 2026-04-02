using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerRunToolState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Animator != null) 
                ctx.Animator.Play("run");

            if (ctx.Status != null && ctx.Status.currentTool != null)
                ctx.Movement?.SetTarget(ctx.Status.currentTool.transform.position, ctx.Status.runSpeed);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            var vSoul = ctx.VillagerJob;
            if (vSoul == null) return;

            // Nếu gặp nguy hiểm thì đổi ngay sang hoảng loạn
            if (ctx.Status.isFearPending)
            {
                vSoul.ChangeState(vSoul.FearState);
                return;
            }

            // Nếu công cụ mục tiêu đã mất hoặc bị người khác lấy
            if (ctx.Status.currentTool == null || !ctx.Status.currentTool.gameObject.activeInHierarchy)
            {
                vSoul.ChangeState(vSoul.IdleState);
                return;
            }

            // Cập nhật điểm đến theo vị trí hiện tại của công cụ (nếu nảy đi nơi khác)
            if (ctx.Movement != null)
            {
                vSoul.SetMoveTarget(ctx.Status.currentTool.transform.position, ctx.Status.runSpeed);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;

            // Đảm bảo luôn chạy animation "run" trong suốt quá trình đi lấy tool
            if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("run"))
                ctx.Animator.Play("run");
        }
    }
}
