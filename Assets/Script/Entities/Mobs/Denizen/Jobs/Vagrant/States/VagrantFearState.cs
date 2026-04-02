using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantFearState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            // Bắt đầu đếm ngược từ status
            if (ctx.Status != null)
            {
                ctx.Status.fearTimer = ctx.Status.maxFearDuration;
                ctx.Status.isFearPending = false; // Xóa cờ kích hoạt sau khi đã vào trạng thái
                ctx.Status.isInDanger = true;
            }

            if (ctx.Animator != null) 
                ctx.Animator.Play("fear");

            // Xóa mục tiêu di chuyển cũ nếu có
            if (ctx.Movement != null)
                ctx.Movement.Stop();
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;

            // Logic chuyển state khi hết thời gian sợ hãi (Dùng trực tiếp dữ liệu từ Status)
            if (ctx.Status != null && ctx.Status.fearTimer <= 0)
            {
                ctx.Status.isInDanger = false;
                ctx.Status.isFearPending = false;

                VagrantJob job = ctx.VagrantJob;
                if (job != null)
                {
                    job.ChangeState(job.IdleState);
                }
            }
        }

        public override void Update(DenizenContext ctx)
        {
            // Trừ timer tại status mỗi frame
            if (ctx.Status != null && ctx.Status.fearTimer > 0)
            {
                ctx.Status.fearTimer -= Time.deltaTime;
            }

            if (ctx.Animator == null) return;
            if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("fear"))
                ctx.Animator.Play("fear");
        }
    }
}
