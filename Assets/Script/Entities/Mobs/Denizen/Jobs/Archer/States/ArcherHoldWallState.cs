using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherHoldWallState : DenizenState
    {
        private Vector3 _defensePosition;
        private const float REACH_THRESHOLD = 0.35f;

        public override void Enter(DenizenContext ctx)
        {
            _defensePosition = ctx.GuardManager != null 
                ? ctx.GuardManager.GetDefensePositionForArcher(ctx.GameObject) 
                : ctx.Transform.position;
            ctx.Animator?.Play("archerIdle");
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            var archer = ctx.ArcherJob;
            if (archer == null) return;

            // Slot phòng thủ có thể thay đổi runtime (rebalance/squad update),
            // nên luôn đồng bộ theo GuardManager thay vì giữ giá trị stale từ Enter().
            if (ctx.GuardManager != null)
                _defensePosition = ctx.GuardManager.GetDefensePositionForArcher(ctx.GameObject);

            // [STANCE] — Đứng yên, khóa vận tốc ngay khi vào state này
            archer.StopMovement();

            // Đếm số tường còn defendable ở cánh hiện tại để xử lý plaza fallback đúng logic chiến thuật.
            int defendableCount = 0;
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                var flankWalls = ctx.Status.isLeftFlank ? wallManager.leftWalls : wallManager.rightWalls;
                foreach (var wall in flankWalls)
                {
                    if (wallManager.IsWallDefendable(wall)) defendableCount++;
                }
            }

            // Nếu không còn tường defendable, cho phép đứng plaza để bắn,
            // không ép quay về slot phòng thủ nữa.
            bool allowPlazaLastStand = defendableCount == 0 && ctx.Status.currentTarget != null && archer.CanAttackFromHere();

            // 1. [PHYSICAL DRIFT CHECK] — ƯU TIÊN VỊ TRÍ SỐ 1
            // Chỉ gác thành/bắn khi đang đứng đúng slot được giao (cho phép sai số 0.8m)
            float drift = Mathf.Abs(ctx.Transform.position.x - _defensePosition.x);
            if (drift > 0.8f && !allowPlazaLastStand)
            {
                if (ctx.Status.debugDenizen)
                    Debug.Log($"<color=orange>[HoldWall]</color> {ctx.GameObject.name} bị lệch khỏi slot {drift:F2}m → ReturnToBase");
                archer.ChangeState(archer.ReturnToBaseState);
                return;
            }

            // 2. [ATTACK GATE] — ƯU TIÊN SỐ 2: Chỉ bắn khi đã đứng đúng vị trí và Skill sẵn sàng
            if (ctx.Status.currentTarget != null)
            {
                if (ctx.SkillManager != null && ctx.SkillManager.IsReady(0))
                {
                    archer.ChangeState(archer.AttackState);
                    return;
                }
            }

            // Ưu tiên quay mặt theo mục tiêu hiện tại để tránh vừa bắn xong quay lưng về slot.
            if (ctx.Status.currentTarget != null)
            {
                MovementUtility.FaceTarget(ctx.GameObject, ctx.Status.currentTarget.transform.position);
            }
            else
            {
                // Fallback khi chưa có mục tiêu: quay ra chiến tuyến theo flank.
                float faceDir = ctx.Status.isLeftFlank ? -1f : 1f;
                MovementUtility.FaceTarget(ctx.GameObject, ctx.Transform.position + new Vector3(faceDir, 0, 0));
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;
            // Trạng thái gác thành luôn là Idle, không di chuyển nhấp nháy
            if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                ctx.Animator.Play("archerIdle");
        }

        // Đã bỏ CalculateDefensePosition cũ vì dùng GuardManager thay thế Chuẩn hơn
    }
}
