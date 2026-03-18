using Script.Game.Enemy.Core;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    public class ReturnToPortalState : EnemyState
    {
        protected float _attackTimer;
        protected bool _isWaitingForCooldown = false;
        protected bool _isMoving = false;
        protected bool _isAttacking = false;

        // Cache để giảm Physics Query
        protected Vector2 _lastScanPosition;
        protected GameObject _cachedObstacle;
        protected const float RESCAN_DISTANCE = 0.2f;
        protected float _lastScanTime;
        protected const float MAX_SCAN_STALE_TIME = 0.3f;

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Walk");

            _attackTimer = ctx.Status.AttackCooldown;

            // QUAN TRỌNG: Reset cache khi Enter để tránh dùng dữ liệu cũ
            _cachedObstacle = null;
            _lastScanPosition = Vector2.zero;
            _lastScanTime = -999f;
            _isMoving = false;
            _isAttacking = false;
            _isWaitingForCooldown = false;
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

            // 2. Scan các vật cản (Chỉ tấn công Tường khi đang rút lui)
            GameObject obstacle = ScanForWallObstacle(ctx);
            if (obstacle != null)
            {
                if (_isAttacking)
                {
                    _isMoving = false;
                    ctx.Controller.DebugAttackTimer = _attackTimer;
                    return;
                }

                // Hành vi Tấn công tương tự MoveToTarget
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _isAttacking = true;
                    _isWaitingForCooldown = false;
                    PerformAttack(ctx, obstacle);
                    
                    if (ctx.Animator != null) ctx.Animator.SetTrigger("Attack"); 
                }
                else
                {
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle");
                    }
                }
                _isMoving = false;
                return; 
            }

            // 3. Di chuyển về Portal
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }

            _isMoving = true;
            ctx.Controller.DebugAttackTimer = _attackTimer;
        }

        public override void FrameUpdate(EnemyContext ctx)
        {
            if (_isMoving && ctx.HomePortal != null)
            {
                Script.Utils.MovementUtility.MoveTowardsTarget(ctx.Self, ctx.HomePortal.transform.position, ctx.Status.MoveSpeed);
            }

            if (_isWaitingForCooldown)
            {
                _attackTimer += Time.deltaTime;
                ctx.Controller.DebugAttackTimer = _attackTimer;
            }
        }

        public override void Exit(EnemyContext ctx)
        {
            _attackTimer = 0;
            _isAttacking = false;
        }

        public virtual void OnAttackAnimationComplete(EnemyContext ctx)
        {
            _attackTimer = 0;
            _isAttacking = false;
            
            // Ép về Idle sau khi đánh xong để chờ (nếu ở Update phát hiện hết quái nó sẽ tự chuyển thành Walk sau)
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Idle");
            }
        }

        private GameObject ScanForWallObstacle(EnemyContext ctx)
        {
            bool hasMoved = Vector2.Distance(ctx.Self.transform.position, _lastScanPosition) >= RESCAN_DISTANCE;
            bool isCacheStale = (Time.time - _lastScanTime) >= MAX_SCAN_STALE_TIME;
            if (!hasMoved && !isCacheStale)
            {
                return _cachedObstacle;
            }
            _lastScanPosition = ctx.Self.transform.position;
            _lastScanTime = Time.time;

            Vector2 direction = (ctx.HomePortal.transform.position.x > ctx.Self.transform.position.x) ? Vector2.right : Vector2.left;
            RaycastHit2D phit = Physics2D.Raycast(ctx.Self.transform.position, direction, ctx.Status.AttackRange, 1 << LayerMask.NameToLayer(Script.Utils.GameTags.Wall));

            if (phit.collider != null && phit.collider.CompareTag(Script.Utils.GameTags.Wall))
            {
                // Kiểm tra tag hợp lệ theo retreatTags
                bool isValid = ctx.TargetManager != null
                    ? ctx.TargetManager.IsValidRetreatTarget(phit.collider.gameObject)
                    : true;
                _cachedObstacle = isValid ? phit.collider.gameObject : null;
            }
            else
            {
                _cachedObstacle = null;
            }
            return _cachedObstacle;
        }

        private void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            if (obstacle == null) return;
            // Sử dụng DamageHandler trung tâm để giải quyết sát thương đồng nhất
            Script.Utils.DamageHandler.ApplyDamage(obstacle, ctx.Status.AttackDamage, ctx.Self);
        }
    }
}
