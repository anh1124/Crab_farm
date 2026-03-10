using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class IdleState : EnemyState
    {
        private float _lastCheckTime = -999f; // -999 để đảm bảo check ngay lập tức khi mới Enter
        private const float CHECK_INTERVAL = 0.5f;

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Idle");

            // Reset để enemy mới spawn được check target ngay lập tức
            _lastCheckTime = -999f;
        }

        public override void Update(EnemyContext ctx)
        {
            // Dùng Time.time thay vì cộng dồn deltaTime
            // Vì UpdateBehavior chỉ được gọi mỗi tickInterval (0.2s), dùng deltaTime sẽ tính sai
            if (Time.time - _lastCheckTime < CHECK_INTERVAL) return;
            _lastCheckTime = Time.time;

            // Hỏi EnemyTargetManager để xin reference tới TownCenter
            var target = ctx.TargetManager?.GetTarget(ctx.Self);

            if (target != null)
            {
                ctx.CurrentTarget = target;
                ctx.Controller.ChangeState(ctx.Controller.MoveStateInstance);
            }
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
