using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;
using Script.Game.Enemy.Variants.ShieldBot;
using Script.Audio;
using UnityEngine;

namespace Script.Game.Enemy
{
    /// <summary>
    /// ShieldBot AI: Sử dụng kiến trúc Executor-Decision chung.
    /// Đặc thù: Tấn công diện rộng (Shock Attack) và có lá chắn.
    /// </summary>
    ///   // anim:
    /*
    -death
    -hit
    -shield finished
    -shield prep
    -shield puulse
    -shock attack
    -walk
    */
    public class ShieldBotAIController : EnemyAIController
    {
        [Header("ShieldBot Settings")]
        [Tooltip("Kéo thả GameObject con chứa Collider sát thương của đòn Shock Attack vào đây")]
        [SerializeField] private GameObject shockAttackCollider;

        public GameObject ShockAttackCollider => shockAttackCollider;

        // [MỚI] Không cần ghi đè GetMoveState nữa vì MoveToTargetState cơ bản đã cực kỳ chuẩn theo Base.

        public override void ResetEnemy(GameObject portal)
        {
            base.ResetEnemy(portal);

            // Phát âm thanh khi ShieldBot được kích hoạt từ pool
            if (_audioEmitter != null)
                _audioEmitter.PlaySound("ShieldBot_Activate", PlaySoundConfig.Default);
        }
        
        // [MỚI] Ghi đè chỉ để chọn đúng State Tấn công đặc thù
        public override AttackState GetAttackState()
        {
            return new ShieldBotAttackState();
        }

        // [MỚI] Ghi đè để chọn đúng DeadState với âm thanh Power_Down
        public override DeadState GetDeadState()
        {
            return new ShieldBotDeadState();
        }

        // ==========================================
        // ANIMATION EVENTS - Được điều hướng từ Skill hoặc State quan tâm
        // ==========================================

        public void AnimEvent_EnableShockCollider()
        {
            if (shockAttackCollider != null)
                shockAttackCollider.SetActive(true);

            // Phát âm thanh shock attack
            if (_audioEmitter != null)
                _audioEmitter.PlaySound("ShieldBot_Shock", PlaySoundConfig.Default);

            // [MỚI]: Thông báo cho SkillManager rằng đã tới frame gây sát thương
            // Điều này sẽ kích hoạt hàm OnHit trong EnemyAttackSkill
            if (_ctx.SkillManager != null)
            {
                // Slot 0 thường là ShockAttack của ShieldBot
                _ctx.SkillManager.OnSkillAnimationDamageFrame(0, _ctx.Self);
            }
        }

        public void AnimEvent_EndShockAttack()
        {
            // Trả quyền xử lý về cho State hiện tại (AttackState sẽ nhận được callback OnAttackAnimationComplete)
            if (_currentState != null)
            {
                _currentState.OnAttackAnimationComplete(_ctx);
            }
            
            ForceDisableShockCollider();
        }

        public void ForceDisableShockCollider()
        {
            if (shockAttackCollider != null)
                shockAttackCollider.SetActive(false);
        }

        [ContextMenu("Print Detailed Debug (ShieldBot)")]
        public override void PrintDetailedDebug()
        {
            base.PrintDetailedDebug();
        }
    }
}
