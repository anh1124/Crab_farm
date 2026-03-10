using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class IdleState : EnemyState
    {
        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Idle");
        }

        public override void Update(EnemyContext ctx)
        {
            // Hỏi EnemyTargetManager để xin reference tới TownCenter
            var target = ctx.TargetManager?.GetTarget(ctx.Self);

            if (target != null)
            {
                ctx.CurrentTarget = target;
                ctx.Controller.ChangeState(ctx.Controller.GetMoveState());
            }
            // Nếu null -> tiếp tục đứng ở Idle chờ đến frame sau.
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
