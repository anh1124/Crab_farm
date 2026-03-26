using Script.Game.Enemy.Core;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    public class DeadState : EnemyState
    {
        protected override bool AllowMovementInState => false;
        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Die");
            }
            // Khóa di chuyển khi ngã xuống
            _moveTarget = null;
            if (ctx.Status != null) ctx.Status.IsMoving = false;
        }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            // Sống chết có số, giờ đứng im chờ hồi kiếp (đợi event bay màu)
            _moveTarget = null;
            if (ctx.CurrentTarget != null) ctx.CurrentTarget = null;
        }

        public override void Update(EnemyContext ctx)
        {
             // Đã khóa chân ở Enter
        }

        /// <summary>
        /// SỰ KIỆN: Frame cuối của hoạt ảnh chết sẽ gọi vào đây
        /// </summary>
        public override void OnDieAnimationComplete(EnemyContext ctx)
        {
            // Cút ngay lập tức!
            ctx.ReturnToPoolAction?.Invoke(ctx.Self);
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
