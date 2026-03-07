using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class DeadState : EnemyState
    {
        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Die");
            }
        }

        public override void Update(EnemyContext ctx)
        {
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
