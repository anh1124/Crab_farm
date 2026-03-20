using Script.Game.Enemy.Core;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    /// <summary>
    /// Trạng thái rút lui về Portal (thường kích hoạt khi đã đầy "Greed")
    /// </summary>
    public class ReturnToPortalState : CombatState
    {
        public override void Enter(EnemyContext ctx)
        {
            // Rút lui có thể quét thưa hơn một chút
            _rescanDistance = 0.2f;
            base.Enter(ctx);
        }

        public override void Update(EnemyContext ctx)
        {
            // 1. Kiểm tra Portal 
            if (ctx.HomePortal == null || !ctx.HomePortal.activeSelf)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(ctx.Controller.IdleStateInstance);
                return;
            }

            // 2. Scan các vật cản (Khi rút lui chỉ ưu tiên phá Wall chắn đường)
            GameObject obstacle = ScanForObstacleInternal(ctx);
            if (obstacle != null)
            {
                if (_isAttacking)
                {
                    _isMoving = false;
                    return;
                }

                // Chặn đường -> Phải phá hủy vật cản
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _isAttacking = true;
                    _isWaitingForCooldown = false;
                    PerformAttack(ctx, obstacle);
                }
                else
                {
                    // Chờ hồi chiêu (Anim Idle)
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle");
                    }
                }
                _isMoving = false;
                return; 
            }

            // 3. Không có vật cản -> Di chuyển về phía Portal
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }

            _isMoving = true;
        }

        protected override void HandleMovement(EnemyContext ctx)
        {
            if (ctx.HomePortal != null)
            {
                Script.Utils.MovementUtility.MoveTowardsTarget(ctx.Self, ctx.HomePortal.transform.position, ctx.Status.MoveSpeed);
            }
        }

        protected override Vector3 GetMoveTargetPosition(EnemyContext ctx)
        {
            return ctx.HomePortal != null ? ctx.HomePortal.transform.position : ctx.Self.transform.position;
        }

        protected override void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            if (obstacle == null) return;
            // Sử dụng DamageHandler trung tâm để giải quyết sát thương đồng nhất
            base.PerformAttack(ctx, obstacle);
            
            if (ctx.Animator != null) ctx.Animator.SetTrigger("Attack");
        }

        protected override bool IsTargetValidForState(EnemyContext ctx, GameObject target)
        {
            // Chỉ đánh phá Tường (Wall) khi đang trên đường rút lui
            return target.CompareTag(Script.Utils.GameTags.Wall) && 
                   (ctx.TargetManager != null ? ctx.TargetManager.IsValidRetreatTarget(target) : true);
        }

        protected override GameObject PerformFallbackScan(EnemyContext ctx, int layerMask)
        {
            // Ép quét vào Wall Layer khi rút lui để phá rào chắn
            int wallLayer = 1 << LayerMask.NameToLayer(Script.Utils.GameTags.Wall);
            return base.PerformFallbackScan(ctx, wallLayer);
        }
    }
}
