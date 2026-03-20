using System.Linq;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using Script.Utils;

namespace Script.Game.Enemy.States
{
    /// <summary>
    /// Trạng thái di chuyển về phía TownCenter/Mục tiêu nhiệm vụ
    /// </summary>
    public class MoveToTargetState : CombatState
    {
        protected GameObject _kingdomTarget;
        protected float _lastTargetCheckTime;

        public override void Enter(EnemyContext ctx)
        {
            base.Enter(ctx);
            _kingdomTarget = null;
            _lastTargetCheckTime = -999f;
        }

        public override void Update(EnemyContext ctx)
        {
            if (ctx.TargetManager == null) return;

            // 1. Phân giải mục tiêu gốc (Cache kết quả trả về từ TargetManager)
            if (_kingdomTarget == null || Time.time - _lastTargetCheckTime > 0.5f)
            {
                _kingdomTarget = ctx.TargetManager.GetTarget(ctx.Self);
                _lastTargetCheckTime = Time.time;
            }
            
            if (_kingdomTarget == null || !_kingdomTarget.activeSelf)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(ctx.Controller.IdleStateInstance); 
                return;
            }

            // 2. Greed mechanic (Khi đầy Greed thì bỏ chạy)
            if (ctx.GreedIsFull)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(ctx.Controller.ReturnStateInstance);
                return;
            }

            // 3. Scan các vật cản (Obstacles) – Dùng cơ chế Cache của base CombatState
            GameObject obstacle = ScanForObstacleInternal(ctx);
            
            if (obstacle != null)
            {
                // Nếu đang bận vung đòn (ShieldBot sẽ override sau)
                if (_isAttacking)
                {
                    _isMoving = false;
                    return;
                }

                // Attack logic - Cooldown được đếm tự động ở CombatState.FrameUpdate
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _isAttacking = true;
                    _isWaitingForCooldown = false;
                    PerformAttack(ctx, obstacle); // Gọi hàm ảo (MoveToTarget sẽ chạy Animation "Attack" ở đây)
                }
                else
                {
                    // Đứng chờ hồi chiêu (Xử lý Animation Idle)
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle");
                    }
                }
                
                _isMoving = false;
                return; 
            }

            // 4. Nếu không có vật cản, tiếp tục di chuyển
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }
            
            _isMoving = true;
        }

        protected override void HandleMovement(EnemyContext ctx)
        {
            if (_kingdomTarget != null)
            {
                MovementUtility.MoveTowardsTarget(ctx.Self, _kingdomTarget.transform.position, ctx.Status.MoveSpeed);
            }
        }

        protected override Vector3 GetMoveTargetPosition(EnemyContext ctx)
        {
            return _kingdomTarget != null ? _kingdomTarget.transform.position : ctx.Self.transform.position;
        }

        protected override bool IsTargetValidForState(EnemyContext ctx, GameObject target)
        {
            // MoveToTargetState cho quái đánh bất cứ thứ gì cản đường nằm trong advanceTags
            if (target == _kingdomTarget) return true;
            return ctx.TargetManager.IsValidAdvanceTarget(target);
        }

        protected override GameObject PerformFallbackScan(EnemyContext ctx, int layerMask)
        {
            // Thiết lập LayerMask cho di chuyển tiến: Đánh tất cả (Default + Walls)
            int combinedMask = (1 << LayerMask.NameToLayer(GameTags.Wall)) | (1 << LayerMask.NameToLayer("Default"));
            return base.PerformFallbackScan(ctx, combinedMask);
        }

        protected override void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            // Logic tấn công cơ bản (Gây dame qua Code)
            base.PerformAttack(ctx, obstacle);
            
            // Kích hoạt animation Attack
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Attack"); 
            }
        }
    }
}
