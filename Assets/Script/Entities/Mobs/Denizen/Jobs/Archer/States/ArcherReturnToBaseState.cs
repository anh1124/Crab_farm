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
                // Ưu tiên 2: Plaza trung tâm
                GameObject townCenter = WorldUtility.GetTownCenter();
                float tx = townCenter != null ? townCenter.transform.position.x : 0f;
                _basePos = new Vector3(tx, ctx.Transform.position.y, 0f);
            }
            
            if (ctx.Animator != null) ctx.Animator.Play("archerMove");
            if (ctx.Movement != null) ctx.Movement.SetTarget(_basePos, ctx.Status.runSpeed);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerJob = ctx.ArcherJob;
            if (archerJob == null) return;

            // CHỈ kiểm tra vật lý — đã đến đúng điểm slot chưa?
            // _basePos = GuardManager.GetDefensePositionForArcher() đã tính sẵn offset đúng.
            // KHÔNG dùng CanAttackFromHere() ở đây: nó kiểm tra ranh giới mềm
            // và bị floating-point lừa khi đứng chính xác tại boundary → gây loop vô tận.
            if (archerJob.HasReachedPosition(_basePos, 0.4f))
            {
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
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                    ctx.Animator.Play("archerIdle");
            }
        }
    }
}
