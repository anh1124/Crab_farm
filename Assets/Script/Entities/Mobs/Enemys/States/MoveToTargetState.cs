using System.Linq;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Utils;

namespace Script.Game.Enemy.States
{
    /// <summary>
    /// Trạng thái di chuyển về phía Mục tiêu chính ban ngày.
    /// CHỈ XỬ LÝ: Quét vật cản bằng Radar. Mục tiêu di chuyển sẽ được gán ở đây.
    /// </summary>
    public class MoveToTargetState : EnemyState
    {
        protected override bool AllowMovementInState => true;
        public override void Enter(EnemyContext ctx) { }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            base.UpdateBehavior(ctx);//chạy base

            if (ctx.Status == null || ctx.TargetManager == null) return;

            // 1.kiểm tra taget 
            var target = ctx.TargetManager.PrimaryTarget;
            if (target == null || !target.activeInHierarchy)
            {
                _moveTarget = null;
                ctx.Controller.ChangeState(ctx.Controller.IdleStateInstance); 
                return;
            }

            // Gán đích để hàm Movement lớp cha tự động đẩy đi
            _moveTarget = target;
            ctx.CurrentTarget = target;

            // 2. [OBSTACLE DETECTION] Quét tia Raycast xem có "chướng ngại vật" nào chặn đường không
            var detected = (ctx.RaycastHelper != null) ? ctx.RaycastHelper.GetListHitObjects() : null;
            
            if (detected != null && detected.Any(obj => obj != null && TargetLists.EnemyAdvanceTargets.Any(tag => obj.CompareTag(tag))))
            {
                ctx.Controller.ChangeState(ctx.Controller.AttackStateInstance);
                return;
            }
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
