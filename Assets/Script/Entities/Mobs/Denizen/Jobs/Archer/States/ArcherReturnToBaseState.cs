using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Manager.WorldManager;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherReturnToBaseState : DenizenState
    {
        private Vector3 _basePos;

        public override void Enter(DenizenContext ctx)
        {
            // [SMART POSITIONING] — Tìm vị trí an toàn nhất để điều chỉnh
            // Ưu tiên 1: Vị trí Slot phòng thủ được giao
            if (ctx.GuardManager != null)
            {
                _basePos = ctx.GuardManager.GetDefensePositionForArcher(ctx.GameObject);
            }
            else
            {
                _basePos = GetFallbackPlazaPos(ctx);
            }

            if (ctx.Animator != null) ctx.Animator.Play("archerMove");
            if (ctx.Status != null)
                ctx.Status.CanMove = true;

            if (ctx.Movement != null) ctx.Movement.SetTarget(_basePos, ctx.Status.runSpeed);
        }

        /// <summary>Lấy vị trí fallback về TownCenter Plaza (Safety Fallback khi khởi tạo).</summary>
        private Vector3 GetFallbackPlazaPos(DenizenContext ctx)
        {
            float randomX = WorldUtility.GetRandomPlazaPos();
            return new Vector3(randomX, ctx.Transform.position.y, 0f);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerJob = ctx.ArcherJob;
            if (archerJob == null) return;

            // Nếu đang có mục tiêu, skill sẵn sàng và vị trí hiện tại đã hợp lệ để bắn
            // thì vào Attack ngay, không ép chạy về slot nữa.
            if (ctx.Status.currentTarget != null && ctx.SkillManager != null && ctx.SkillManager.IsReady(0))
            {
                if (archerJob.CanAttackFromHere())
                {
                    archerJob.StopMovement();
                    archerJob.ChangeState(archerJob.AttackState);
                    return;
                }
            }

            // CHỈ kiểm tra vật lý — đã đến đúng điểm slot chưa?
            // _basePos = GuardManager.GetDefensePositionForArcher() đã tính sẵn offset đúng.
            // KHÔNG dùng CanAttackFromHere() ở đây: nó kiểm tra ranh giới mềm
            // và bị floating-point lừa khi đứng chính xác tại boundary → gây loop vô tận.
            if (archerJob.HasReachedPosition(_basePos, 0.4f))
            {
                // Đã tới vị trí thì dừng cờ di chuyển ngay để tránh spam move animation
                // trong frame chuyển state.
                archerJob.StopMovement();
                archerJob.ExecuteDefensiveOrder();
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null || ctx.Animator.runtimeAnimatorController == null) return;

            if (ctx.Status.isMoving)
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                    ctx.Animator.Play("archerMove");
            }
            else
            {
                // Chỉ ép hướng khi đã dừng để không gây chạy lùi.
                float faceDir = ctx.Status.isLeftFlank ? -1f : 1f;
                MovementUtility.FaceTarget(ctx.GameObject, ctx.Transform.position + new Vector3(faceDir, 0f, 0f));

                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                    ctx.Animator.Play("archerIdle");
            }
        }
    }
}
