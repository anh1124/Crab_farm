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
                ctx.Status.CanMove = false;
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
            var archerJob = ctx.ArcherJob;
            if (archerJob == null) return;

            bool isSafe = archerJob.CanAttackFromHere();
            if (ctx.Status.debugDenizen) 
            {
                archerJob.DebugArcherCombat();
            }

            if (!isSafe)
            {
                if (ctx.Status.debugDenizen) 
                    Debug.Log($"<color=red>[AttackState]</color> {ctx.GameObject.name} HỦY PHÁT BẮN vì vị trí không an toàn!");
                archerJob.ChangeState(archerJob.ReturnToBaseState);
                return;
            }

            // Kiểm tra SkillManager đã sẵn sàng chưa (Slot 0 là bắn cung)
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
                    // Thực thi thất bại (thiếu mana/tài nguyên nếu có) -> Về Idle
                    archerJob.ChangeState(archerJob.IdleState);
                }
            }
            else
            {
                // Skill còn hồi chiêu -> Về Idle
                if (ctx.Status.debugDenizen) 
                    Debug.Log($"[AttackState] {ctx.GameObject.name} skill đang hồi chiêu -> Idle");
                archerJob.ChangeState(archerJob.IdleState);
            }
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerJob = ctx.ArcherJob;
            if (ctx.Status == null || archerJob == null) return;

            // Chỉ theo dõi mục tiêu trong lúc chờ animation hoàn tất.
            // Nếu mục tiêu biến mất giữa chừng → hủy về Idle.
            GameObject target = ctx.Status.currentTarget;
            if (target == null || !target.activeInHierarchy)
            {
                archerJob.ChangeState(archerJob.IdleState);
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
                ctx.Status.CanMove = true;
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

            // Báo cho SkillManager biết animation đã kết thúc để bắt đầu cooldown
            ctx.SkillManager?.OnSkillAnimationEnd(0, ctx.GameObject);
            
            if (ctx.Status != null) ctx.Status.isSkillCasting = false;
            
            ctx.Animator?.Play("archerIdle");
            // Bắn xong → về Idle để tái đánh giá
            ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
        }
    }
}
