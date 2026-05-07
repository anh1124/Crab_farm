using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    /// <summary>
    /// ArcherRetreatState: Trạng thái rút lui THUẦN TÚY khi phòng tuyến bị vỡ.
    /// [QUY TẮC]: Không tấn công, không dừng lại giữa chừng, chỉ ưu tiên chạy về đích.
    /// [CÂY QUYẾT ĐỊNH]:
    /// 1. Enter:
    ///    - Khôi phục Target: Ưu tiên _wallTarget -> Fallback assignedWall.
    ///    - Nếu không có bất kỳ Wall nào -> Chạy thẳng về Plaza (ReturnToBase).
    ///    - Thiết lập di chuyển về Slot phòng thủ.
    /// 
    /// 2. UpdateBehavior:
    ///    - Chỉ kiểm tra REACH CHECK (Đã đến đích chưa?):
    ///         - Nếu Khoảng cách < 0.4m:
    ///             - CÒN TƯỜNG (Defendable): Dừng lại -> Chuyển sang HoldWallState (Vào vị trí gác).
    ///             - HẾT TƯỜNG (Null/Fallback): Chuyển sang ReturnToBaseState (Về Plaza).
    ///         - Nếu chưa đến đích: Không làm gì cả, để hệ thống di chuyển tiếp tục chạy.
    /// </summary>
    public class ArcherRetreatState : DenizenState
    {
        private GameObject _wallTarget;

        public void SetRetreatTarget(GameObject wall) => _wallTarget = wall;

        public override void Enter(DenizenContext ctx)
        {
            // [FALLBACK] — Nếu bị mất target (do Load hoặc wall bị hủy), lấy assignedWall hiện tại
            if (_wallTarget == null && ctx.Status.assignedWall != null)
                _wallTarget = ctx.Status.assignedWall;

            if (_wallTarget == null)
            {
                if (ctx.Status.debugDenizen) Debug.LogWarning($"[ArcherRetreat] {ctx.GameObject.name} không có target! Rút về Plaza.");
                ctx.ArcherJob?.ChangeState(ctx.ArcherJob.ReturnToBaseState);
                return;
            }

            ctx.Status.CanMove = true;
            
            // [POSITIONING] — Chạy về slot phòng thủ (vị trí an toàn nhất được chỉ định)
            Vector3 targetPos = ctx.GuardManager != null 
                ? ctx.GuardManager.GetDefensePositionForArcher(ctx.GameObject) 
                : _wallTarget.transform.position;

            if (ctx.Movement != null)
                ctx.Movement.SetTarget(targetPos, ctx.Status.runSpeed);

            if (ctx.Animator != null) 
                ctx.Animator.Play("archerMove");
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerSoul = ctx.ArcherJob;
            if (archerSoul == null) return;

            // [DYNAMIC TARGETING] — Luôn hỏi GuardManager vị trí mới nhất (phòng trường hợp wall trước mặt sập)
            // Archer không tự tính toán, chỉ bám theo vị trí mà GuardManager chỉ định.
            Vector3 latestTargetPos = ctx.GuardManager != null 
                ? ctx.GuardManager.GetDefensePositionForArcher(ctx.GameObject) 
                : (ctx.Status.assignedWall != null ? ctx.Status.assignedWall.transform.position : ctx.Transform.position);

            // Cập nhật đích đến cho hệ thống di chuyển để đảm bảo luôn chạy về đúng điểm slot mới nhất
            if (ctx.Movement != null)
                ctx.Movement.SetTarget(latestTargetPos, ctx.Status.runSpeed);

            // [PURE RETREAT LOGIC] — So sánh vị trí hiện tại với đích đến mới nhất từ GuardManager
            float dist = Mathf.Abs(ctx.Transform.position.x - latestTargetPos.x);
            if (dist < 0.4f)
            {
                // [TRANSITION] — Chỉ khi đã đến đích thực tế mới được phép thoát khỏi trạng thái rút lui
                // Kiểm tra xem tại vị trí này có Wall nào để gác không (Frontline hiện tại)
                bool hasDefensiveWall = ctx.GuardManager != null && 
                                      (ctx.GuardManager.currentLeftWall != null || ctx.GuardManager.currentRightWall != null);

                if (hasDefensiveWall)
                {
                    if (ctx.Movement != null) ctx.Movement.Stop();
                    archerSoul.ChangeState(archerSoul.HoldWallState);
                }
                else
                {
                    // Nếu đích đến cuối cùng là Plaza (không còn wall nào)
                    archerSoul.ChangeState(archerSoul.ReturnToBaseState);
                }
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
