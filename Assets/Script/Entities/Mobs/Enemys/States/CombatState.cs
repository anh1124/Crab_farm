using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    /// <summary>
    /// Base class cho các trạng thái có hành vi Di chuyển + Tấn công vật cản (Combat)
    /// Trích xuất logic Cooldown, Scan Cache và Animation Event dùng chung.
    /// </summary>
    public abstract class CombatState : EnemyState
    {
        protected float _attackTimer;
        protected bool _isWaitingForCooldown = false;
        protected bool _isMoving = false;
        protected bool _isAttacking = false;
        
        // Cache để giảm tải Physics Query
        protected Vector2 _lastScanPosition;
        protected GameObject _cachedObstacle;
        protected float _rescanDistance = 0.15f;
        protected float _lastScanTime;
        protected const float MAX_SCAN_STALE_TIME = 0.3f; 

        public override void Enter(EnemyContext ctx)
        {
            ResetCombatFlags();
            // Mặc định cho phép tấn công ngay khi vừa chạm mục tiêu
            _attackTimer = ctx.Status.AttackCooldown;
            
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Walk");
        }

        protected virtual void ResetCombatFlags()
        {
            _isMoving = false;
            _isAttacking = false;
            _isWaitingForCooldown = false;
            _cachedObstacle = null;
            _lastScanPosition = Vector2.zero;
            _lastScanTime = -999f;
        }

        public override void FrameUpdate(EnemyContext ctx)
        {
            // ✅ Tối ưu: Timer luôn chạy trừ khi đang bị khóa bởi Animation tấn công
            if (!_isAttacking)
            {
                if (_attackTimer < ctx.Status.AttackCooldown)
                {
                    _attackTimer += Time.deltaTime;
                }
            }
            
            // Cập nhật giá trị hiển thị Debug lên Controller
            if (ctx.Controller != null)
                ctx.Controller.DebugAttackTimer = _attackTimer;

            // Thực hiện di chuyển vật lý nếu cờ _isMoving đang bật
            if (_isMoving)
            {
                HandleMovement(ctx);
            }
        }

        /// <summary>
        /// Logic di chuyển cụ thể (về phía TownCenter hoặc Portal)
        /// </summary>
        protected abstract void HandleMovement(EnemyContext ctx);

        public override void Exit(EnemyContext ctx)
        {
            _isAttacking = false;
            _isMoving = false;
            _isWaitingForCooldown = false;
        }

        /// <summary>
        /// Được gọi từ Animation Event khi kết thúc đòn đánh
        /// </summary>
        public virtual void OnAttackAnimationComplete(EnemyContext ctx)
        {
            _attackTimer = 0;
            _isAttacking = false;
            _isWaitingForCooldown = false; // Giải phóng trạng thái chờ để Update() có thể quyết định bước tiếp theo
            
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Idle");
        }

        /// <summary>
        /// Thực hiện gây sát thương lên mục tiêu
        /// </summary>
        protected virtual void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            if (obstacle == null) return;
            
            float damage = ctx.Status.AttackDamage;
            Script.Utils.DamageHandler.ApplyDamage(obstacle, damage, ctx.Self);

            if (ctx.Status.debugEnemy)
            {
                Debug.Log($"[{ctx.Self.name}] CombatState: Tấn công {obstacle.name} gây {damage} sát thương.");
            }
        }

        /// <summary>
        /// Logic quét vật cản chung (có cache)
        /// </summary>
        protected virtual GameObject ScanForObstacleInternal(EnemyContext ctx, int layerMask = ~0)
        {
            // Chỉ scan lại nếu đã di chuyển đủ xa hoặc cache đã quá cũ (vượt quá STALE_TIME)
            bool hasMoved = Vector2.Distance(ctx.Self.transform.position, _lastScanPosition) >= _rescanDistance;
            bool isCacheStale = (Time.time - _lastScanTime) >= MAX_SCAN_STALE_TIME;
            
            if (!hasMoved && !isCacheStale)
            {
                return _cachedObstacle;
            }
            
            _lastScanPosition = ctx.Self.transform.position;
            _lastScanTime = Time.time;

            // 1. Ưu tiên dùng RayCastHelper nếu có (Tận dụng cơ chế quét đa mục tiêu)
            if (ctx.RaycastHelper != null)
            {
                var hits = ctx.RaycastHelper.GetListHitObjects(ctx.Status.AttackRange);
                if (hits != null && hits.Count > 0)
                {
                    foreach (var hit in hits)
                    {
                        if (hit == null) continue;
                        if (!Script.Utils.CombatUtility.IsTargetValidAndAlive(hit)) continue;
                        
                        // Kiểm tra Tag hợp lệ qua TargetManager (nếu có)
                        if (ctx.TargetManager != null && !IsTargetValidForState(ctx, hit)) continue;

                        _cachedObstacle = hit;
                        return hit;
                    }
                }
                _cachedObstacle = null;
                return null;
            }

            // 2. Fallback: Raycast đơn giản (Dành cho các Enemy cơ bản không có RayCastHelper)
            return PerformFallbackScan(ctx, layerMask);
        }

        /// <summary>
        /// Kiểm tra xem target có phù hợp với trạng thái hiện tại không (vd: ReturnToPortal chỉ đánh Wall)
        /// </summary>
        protected virtual bool IsTargetValidForState(EnemyContext ctx, GameObject target) => true;

        /// <summary>
        /// Quét mục tiêu thủ công nếu không có Helper
        /// </summary>
        protected virtual GameObject PerformFallbackScan(EnemyContext ctx, int layerMask)
        {
            // Thiết lập hướng quét dựa trên mục tiêu hiện tại (TownCenter hoặc Portal)
            Vector3 targetPos = GetMoveTargetPosition(ctx);
            Vector2 direction = (targetPos.x > ctx.Self.transform.position.x) ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(ctx.Self.transform.position, direction, ctx.Status.AttackRange, layerMask);
            
            if (hit.collider != null && Script.Utils.CombatUtility.IsTargetValidAndAlive(hit.collider.gameObject))
            {
                _cachedObstacle = hit.collider.gameObject;
                return _cachedObstacle;
            }

            _cachedObstacle = null;
            return null;
        }

        protected abstract Vector3 GetMoveTargetPosition(EnemyContext ctx);
    }
}
