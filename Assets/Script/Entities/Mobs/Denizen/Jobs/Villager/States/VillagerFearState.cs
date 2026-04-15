using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerFearState : DenizenState
    {
        private Vector3 _townCenterPos;

        public override void Enter(DenizenContext ctx)
        {
            // Thay vì chạy về (0,0,0), hãy chạy về Plaza ngẫu nhiên
            float randomX = Script.Utility.GetRandomPlazaPos();
            _townCenterPos = new Vector3(randomX, ctx.Transform.position.y, 0f);

            // Bắt đầu đếm ngược từ status
            if (ctx.Status != null)
            {
                ctx.Status.fearTimer = ctx.Status.maxFearDuration;
                ctx.Status.isFearPending = false;
                ctx.Status.isInDanger = true;
                ctx.Status.CanMove = true;
            }

            if (ctx.Animator != null) 
                ctx.Animator.Play("run");

            // Chỉ định Cơ Thể chạy về plaza (bằng tốc độ chạy)
            if (ctx.Movement != null)
                ctx.Movement.SetTarget(_townCenterPos, ctx.Status != null ? ctx.Status.runSpeed : -1f);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            
            // Nếu đã đến điểm chạy trốn cũ nhưng vẫn còn sợ, thì chọn điểm Plaza khác để chạy tiếp
            if (ctx.VillagerJob != null && ctx.VillagerJob.HasReachedPosition(_townCenterPos, 1.5f) && ctx.Status.fearTimer > 1f)
            {
                float newX = Script.Utility.GetRandomPlazaPos();
                _townCenterPos = new Vector3(newX, ctx.Transform.position.y, 0f);
                ctx.Movement?.SetTarget(_townCenterPos, ctx.Status != null ? ctx.Status.runSpeed : -1f);
            }

            // Chuyển về Idle khi hết sợ hãi
            if (ctx.Status != null && ctx.Status.fearTimer <= 0)
            {
                ctx.Status.isInDanger = false;
                ctx.Status.isFearPending = false;

                VillagerJob job = ctx.VillagerJob;
                if (job != null)
                {
                    // Dừng chạy khi đã hết sợ
                    if (ctx.Movement != null) ctx.Movement.Stop();

                    job.ChangeState(job.IdleState);
                }
            }
        }

        public override void Update(DenizenContext ctx)
        {
            // Trừ timer tại status hàng frame
            if (ctx.Status != null && ctx.Status.fearTimer > 0)
            {
                ctx.Status.fearTimer -= Time.deltaTime;
            }

            if (ctx.Animator == null) return;
            
            if (ctx.Status.isMoving)
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
