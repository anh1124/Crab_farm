using Script.Game.Enemy.Core;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    public class DeadState : EnemyState
    {
        private bool _requestedRecycle;
        private bool _loggedAnimState;

        protected override bool AllowMovementInState => false;
        public override void Enter(EnemyContext ctx)
        {
            _requestedRecycle = false;
            _loggedAnimState = false;

            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Die");
            }
            if (ctx.Controller != null && ctx.Controller.isDebug)
                Debug.Log($"[DeadState] {ctx.Self.name} ENTER -> Trigger Die, waiting animation event.");

            // Khóa di chuyển khi ngã xuống
            _moveTarget = null;
            if (ctx.Status != null) ctx.Status.IsMoving = false;
        }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            // Sống chết có số, giờ đứng im chờ hồi kiếp (đợi event bay màu)
            _moveTarget = null;
            if (ctx.CurrentTarget != null) ctx.CurrentTarget = null;

            if (!_loggedAnimState && ctx.Animator != null)
            {
                _loggedAnimState = true;
                var info = ctx.Animator.GetCurrentAnimatorStateInfo(0);
                if (ctx.Controller != null && ctx.Controller.isDebug)
                    Debug.Log($"[DeadState] {ctx.Self.name} animator current state after death trigger: hash={info.shortNameHash}, normalizedTime={info.normalizedTime:F2}");
            }
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
            if (_requestedRecycle) return;
            _requestedRecycle = true;
            if (ctx.Controller != null && ctx.Controller.isDebug)
                Debug.Log($"[DeadState] {ctx.Self.name} OnDieAnimationComplete received -> ReturnToPool.");
            ctx.ReturnToPoolAction?.Invoke(ctx.Self);
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
