using Script.Game.Enemy.States;
using UnityEngine;

namespace Script.Game.Enemy.Variants.ShieldBot
{
    /// <summary>
    /// ShieldBot Attack: Đặc thù là dùng Skill AOE "shock attack"
    /// Kế thừa từ class AttackState (Lớp đã tích hợp SkillManager)
    /// </summary>
    public class ShieldBotAttackState : AttackState
    {
        public override void Enter(EnemyContext ctx)
        {
            base.Enter(ctx); // Để Base StandStill (_isMoving=false)
        }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            // Tận dụng lớp cha: Nếu hết địch -> Quay về MoveState, nếu còn địch -> Gọi SkillManager.ExecuteSkill
            base.UpdateBehavior(ctx);
        }

        public override void Exit(EnemyContext ctx)
        {
            base.Exit(ctx);
            // Tính năng an toàn: Đảm bảo tắt collider khi rời khỏi state
            if (ctx.Controller is ShieldBotAIController shieldBot)
            {
                shieldBot.ForceDisableShockCollider();
            }
        }
    }
}
