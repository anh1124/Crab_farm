using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.Denizen.Jobs.Archer;
using Script.Audio;
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
                // [DEBUG] Log chi tiết để xác định lý do không tấn công
                if (ctx.Status?.currentTarget == null)
                {
                    Debug.LogWarning($"[ArcherAttackState] {ctx.GameObject?.name}: Không tấn công vì currentTarget = NULL! (Có thể enemy chưa được nhận diện hoặc IsAlive = false)");
                }
                if (ctx.SkillManager == null)
                {
                    Debug.LogError($"[ArcherAttackState] {ctx.GameObject?.name}: SkillManager = NULL!");
                }
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
                
                // [FIX] Nếu không an toàn do lọt vào tường -> Quay lại trạng thái trước đó để tiếp tục di chuyển/hành động.
                // Tránh việc đang đi săn mà lọt vào trigger tường cái là dừng khựng lại (Idle) hoặc chạy về nhà (ReturnToBase).
                if (archerJob.IsInsideWall(false))
                {
                    archerJob.ChangeState(archerJob.PreviousState ?? archerJob.IdleState);
                }
                else
                {
                    archerJob.ChangeState(archerJob.ReturnToBaseState);
                }
                return;
            }

            // Kiểm tra SkillManager đã sẵn sàng chưa (Slot 0 là bắn cung)
            if (ctx.SkillManager == null)
            {
                Debug.LogError($"[ArcherAttackState] {ctx.GameObject.name}: SkillManager là NULL!");
                archerJob.ChangeState(archerJob.IdleState);
                return;
            }

            if (!ctx.SkillManager.IsReady(0))
            {
                // Skill còn hồi chiêu -> Về Idle
                if (ctx.Status.debugDenizen)
                    Debug.Log($"[AttackState] {ctx.GameObject.name}: Slot 0 chưa sẵn sàng (cooldown) -> Idle");
                archerJob.ChangeState(archerJob.IdleState);
                return;
            }

            bool skillExecuted = ctx.SkillManager.ExecuteSkill(0, ctx.GameObject);
            if (skillExecuted)
            {
                ctx.Status.isSkillCasting = true;
                ctx.Animator?.Play("archerDraw");

                // Phát âm thanh khi giương cung
                var emitter = ctx.GameObject.GetComponent<AudioEmitter>();
                if (emitter != null)
                    emitter.PlaySound("Bow_Draw", PlaySoundConfig.Default);

                if (ctx.Status.debugDenizen)
                    Debug.Log($"<color=green>[AttackState]</color> {ctx.GameObject.name} giương cung nhắm {ctx.Status.currentTarget.name}");
            }
            else
            {
                // Thực thi thất bại (thiếu mana/tài nguyên hoặc lỗi khác)
                Debug.LogWarning($"[ArcherAttackState] {ctx.GameObject.name}: ExecuteSkill(0) trả về FALSE! Có thể do thiếu tài nguyên hoặc lỗi hệ thống.");
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

            // [SAFETY] Nếu bị ép đổi state khi đang bắn (VD: GuardManager bắt rút lui), 
            // phải báo cho SkillManager kết thúc để giải phóng slot và bắt đầu cooldown.
            if (ctx.SkillManager != null && ctx.SkillManager.GetState(0) == Script.Game.PlayerCharacter.Skills.SkillManager.SkillState.Executing)
            {
                ctx.SkillManager.OnSkillAnimationEnd(0, ctx.GameObject);
            }
        }

        public void OnDrawComplete(DenizenContext ctx) 
        { 
            ctx.Animator?.Play("archerRelease"); 
        }

        public void OnArrowRelease(DenizenContext ctx)
        {
            // Phát âm thanh khi thả tên
            var emitter = ctx.GameObject.GetComponent<AudioEmitter>();
            if (emitter != null)
                emitter.PlaySound("Bow_Release", PlaySoundConfig.Default);

            // Kiểm tra ArcherShooting trước khi bắn
            if (ctx.ArcherShooting == null)
            {
                Debug.LogError($"[ArcherAttackState] {ctx.GameObject.name}: ArcherShooting là NULL! Kiểm tra DenizenContext.Setup() có được gọi với arrowPool không.");
                ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
                return;
            }

            if (ctx.Status?.currentTarget != null)
                ctx.ArcherShooting.ShootAt(ctx.Status.currentTarget.transform.position);
            else
                Debug.LogWarning($"[ArcherAttackState] {ctx.GameObject.name}: Không có target để bắn!");

            // Báo cho SkillManager biết animation đã kết thúc để bắt đầu cooldown
            ctx.SkillManager?.OnSkillAnimationEnd(0, ctx.GameObject);

            if (ctx.Status != null) ctx.Status.isSkillCasting = false;

            ctx.Animator?.Play("archerIdle");
            // Bắn xong → về Idle để tái đánh giá
            ctx.ArcherJob?.ChangeState(ctx.ArcherJob.IdleState);
        }
    }
}
