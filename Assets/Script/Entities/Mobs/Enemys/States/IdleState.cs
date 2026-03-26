using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class IdleState : EnemyState
    {
       protected override bool AllowMovementInState => false;

        //không có gì ở đây cả ,chỉ chạy anim thôi
        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Controller != null)
                ctx.Controller.SetTriggerSafely("Idle");
        }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            base.UpdateBehavior(ctx);
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
