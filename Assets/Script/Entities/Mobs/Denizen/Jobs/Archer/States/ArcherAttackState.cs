using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public interface IArcherAttackState
    {
        void OnDrawComplete(DenizenContext ctx);
        void OnArrowRelease(DenizenContext ctx);
    }

    public class ArcherAttackState : DenizenState, IArcherAttackState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Status != null)
            {
                ctx.Status.isSkillCasting = false;
                ctx.Status.isMovementAllowed = false;
            }
            ctx.Movement?.Stop();
            ctx.Animator?.Play("archerIdle");

            // [ONE-SHOT] Kích hoạt skill ngay khi vào state.
            // Mỗi lần vào AttackState = đúng 1 phát bắn.
            if (ctx.SkillManager == null || ctx.Status?.currentTarget == null)
            {
                ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
                return;
            }

            // [TACTICAL CHECK FIRST] — Kiểm tra kỷ luật trước khi bóp cò
            var archer = ctx.ArcherJob;
            if (archer == null) return;

            bool isSafe = archer.CanAttackFromHere();
            if (ctx.Status.debugDenizen) 
            {
                ctx.ArcherJob?.DebugArcherCombat();
            }

            if (!isSafe)
            {
                if (ctx.Status.debugDenizen) 
                    Debug.Log($"<color=red>[AttackState]</color> {ctx.GameObject.name} HỦY PHÁT BẮN vì vị trí không an toàn!");
                ctx.ArcherJob?.ChangeState(ctx.ArcherJob.ReturnToBaseState);
                return;
            }

            if (ctx.SkillManager.IsReady(0))
            {
                if (ctx.SkillManager.ExecuteSkill(0, ctx.GameObject))
                {
                    ctx.Status.isSkillCasting = true;
                    ctx.Animator?.Play("archerDraw");
                    if (ctx.Status.debugDenizen) 
                        Debug.Log($"<color=green>[AttackState]</color> {ctx.GameObject.name} giương cung nhắm {ctx.Status.currentTarget.name}");
                }
                else
                {
                    ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
                }
            }
            else
            {
                // Skill còn hồi chiêu → về Idle, HoldWall sẽ thử lại sau
                if (ctx.Status.debugDenizen) 
                    Debug.Log($"[AttackState] {ctx.GameObject.name} skill đang hồi chiêu → Idle");
                ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
            }
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.Status == null || ctx.ArcherJob == null) return;

            // Chỉ theo dõi mục tiêu trong lúc chờ animation hoàn tất.
            // Nếu mục tiêu biến mất giữa chừng → hủy về Idle.
            GameObject target = ctx.Status.currentTarget;
            if (target == null || !target.activeInHierarchy)
            {
                ctx.ArcherJob.ChangeState(ctx.ArcherJob.IdleState);
                return;
            }
            // Còn lại: đợi OnDrawComplete → OnArrowRelease → tự thoát.
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Status?.currentTarget != null)
                MovementUtility.FaceTarget(ctx.GameObject, ctx.Status.currentTarget.transform.position);
        }

        public override void Exit(DenizenContext ctx)
        {
            if (ctx.Status != null)
            {
                ctx.Status.isSkillCasting = false;
                ctx.Status.isMovementAllowed = true;
            }
        }

        public void OnDrawComplete(DenizenContext ctx) 
        { 
            ctx.Animator?.Play("archerRelease"); 
        }

        public void OnArrowRelease(DenizenContext ctx)
        {
            if (ctx.Status?.currentTarget != null && ctx.ArcherShooting != null)
                ctx.ArcherShooting.ShootAt(ctx.Status.currentTarget.transform.position);

            ctx.SkillManager?.OnSkillAnimationEnd(0, ctx.GameObject);
            if (ctx.Status != null) ctx.Status.isSkillCasting = false;
            
            ctx.Animator?.Play("archerIdle");
            // Bắn xong → về Idle để tái đánh giá
            ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
        }
    }
}
