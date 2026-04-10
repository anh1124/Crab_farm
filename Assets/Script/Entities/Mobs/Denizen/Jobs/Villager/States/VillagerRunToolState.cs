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
            var villagerJob = ctx.VillagerJob;
            if (villagerJob == null) return;

            // Nếu gặp nguy hiểm thì đổi ngay sang hoảng loạn
            if (ctx.Status.isFearPending)
            {
                villagerJob.ChangeState(villagerJob.FearState);
                return;
            }

            // Nếu công cụ mục tiêu đã mất hoặc bị người khác lấy
            if (ctx.Status.currentTool == null || !ctx.Status.currentTool.gameObject.activeInHierarchy || ctx.Status.currentTool.pickedBy != null)
            {
                ctx.Status.currentTool = null; // Cần clear biến này đi để không bị loop nhảy vào lại
                villagerJob.ChangeState(villagerJob.IdleState);
                return;
            }

            // Di chuyển đến Tool
            float distance = Vector3.Distance(ctx.Transform.position, ctx.Status.currentTool.transform.position);
            if (distance > 0.5f)
            {
                villagerJob.SetMoveTarget(ctx.Status.currentTool.transform.position, ctx.Status.runSpeed);
            }
            else
            {
                // Đã tới vị trí tool nhưng có thể chưa nhặt được (cooldown/điều kiện pickup),
                // nên dừng movement để tránh loop anim chạy tại chỗ.
                ctx.Movement?.Stop();
            }
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
