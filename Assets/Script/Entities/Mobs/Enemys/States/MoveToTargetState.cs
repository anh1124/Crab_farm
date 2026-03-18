using System.Linq;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using Script.Utils;
using Script.Data_Model; // for PrefabData

namespace Script.Game.Enemy.States
{
    public class MoveToTargetState : EnemyState
    {
        protected float _attackTimer;
        protected bool _isWaitingForCooldown = false;
        protected bool _isMoving = false;
        protected bool _isAttacking = false;
        protected GameObject _kingdomTarget;
        
        // Cache để giảm Physics Query
        protected Vector2 _lastScanPosition;
        protected GameObject _cachedObstacle;
        protected const float RESCAN_DISTANCE = 0.15f;
        protected float _lastTargetCheckTime;
        // Thêm cache thời gian để force rescan ngay cả khi enemy đứng yên (fix bug tấn công target đã đi mất)
        protected float _lastScanTime;
        protected const float MAX_SCAN_STALE_TIME = 0.3f; // Force rescan mỗi 0.3s dù không di chuyển

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Walk");

            // Có thể tấn công ngay ở frame đầu chạm obstacle
            _attackTimer = ctx.Status.AttackCooldown;

            // QUAN TRỌNG: Reset cache khi Enter để tránh dùng dữ liệu cũ
            // (vì state này được pre-allocated và tái sử dụng cho nhiều enemy)
            _kingdomTarget = null;
            _cachedObstacle = null;
            _lastScanPosition = Vector2.zero;
            _lastScanTime = -999f;     // Force rescan ngay lập tức
            _lastTargetCheckTime = -999f;
            _isMoving = false;
            _isAttacking = false;
            _isWaitingForCooldown = false;
        }

        public override void Update(EnemyContext ctx)
        {
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

            // 2. Greed mechanic
            if (ctx.GreedIsFull)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(ctx.Controller.ReturnStateInstance);
                return;
            }

            // 3. Scan các vật cản (Obstacles)
            GameObject obstacle = ScanForObstacle(ctx);
            if (obstacle != null)
            {
                if (_isAttacking)
                {
                    _isMoving = false;
                    ctx.Controller.DebugAttackTimer = _attackTimer;
                    return;
                }

                // Attack logic
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _isAttacking = true;
                    _isWaitingForCooldown = false;
                    PerformAttack(ctx, obstacle);
                    
                    if (ctx.Animator != null)
                    {
                        // Giả sử có animation Attack chung lúc này (ShieldBot sẽ override sau)
                        ctx.Animator.SetTrigger("Attack"); 
                    }
                }
                else
                {
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle"); // trigger Idle để quái đứng im chờ
                    }
                }
                
                // Khi có obstacle, enemy dừng lại để chờ cooldown đánh
                _isMoving = false;
                return; 
            }

            // 4. Nếu không có obstacle, tiếp tục di chuyển về phía TownCenter
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }
            
            _isMoving = true;

            // Cập nhật ui debug cooldown
            ctx.Controller.DebugAttackTimer = _attackTimer;
        }

        public override void FrameUpdate(EnemyContext ctx)
        {
            if (_isMoving && _kingdomTarget != null)
            {
                MovementUtility.MoveTowardsTarget(ctx.Self, _kingdomTarget.transform.position, ctx.Status.MoveSpeed);
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
            
            // Ép về Idle ngay sau khi đánh xong để chờ (nếu ở Update phát hiện hết quái nó sẽ tự chuyển thành Walk sau)
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Idle");
            }
        }

        protected GameObject ScanForObstacle(EnemyContext ctx)
        {
            // TỐI ƯU: Chỉ scan lại nếu đã di chuyển một khoảng nhất định
            // HOẶC nếu cache đã cũ quá (fix bug: enemy đứng yên không bao giờ rescan)
            bool hasMoved = Vector2.Distance(ctx.Self.transform.position, _lastScanPosition) >= RESCAN_DISTANCE;
            bool isCacheStale = (Time.time - _lastScanTime) >= MAX_SCAN_STALE_TIME;
            if (!hasMoved && !isCacheStale)
            {
                return _cachedObstacle;
            }
            _lastScanPosition = ctx.Self.transform.position;
            _lastScanTime = Time.time;

            // 1. Nếu Enemy có gắn RayCastHelper, ưu tiên sử dụng (DÙNG CACHE TRONG CONTEXT)
            var raycastHelper = ctx.RaycastHelper;
            if (raycastHelper != null)
            {
                var hits = raycastHelper.GetListHitObjects(ctx.Status.AttackRange);
                
                if (hits.Count > 0)
                {
                    GameObject validObstacle = null;
                    foreach (var hit in hits)
                    {
                        if (hit == null) continue;
                        if (!Script.Utils.CombatUtility.IsTargetValidAndAlive(hit)) continue;

                        // KIỂM TRA TAG: Chỉ coi là obstacle hợp lệ nếu có tag khớp với advanceTags
                        bool isValidTag = ctx.TargetManager != null
                            ? ctx.TargetManager.IsValidAdvanceTarget(hit)
                            : true; // Fallback: cho phép tất cả nếu không có manager

                        if (!isValidTag) continue;

                        // Ưu tiên TownCenter (primary kingdom target)
                        if (hit == _kingdomTarget)
                        {
                            _cachedObstacle = hit;
                            return hit;
                        }
                        if (validObstacle == null) validObstacle = hit;
                    }
                    _cachedObstacle = validObstacle;
                    if (validObstacle != null) return validObstacle;
                }
                _cachedObstacle = null;
                return null;
            }

            // 2. Fallback cổ điển
            if (_kingdomTarget == null) { _cachedObstacle = null; return null; }
            Vector2 direction = (_kingdomTarget.transform.position.x > ctx.Self.transform.position.x) ? Vector2.right : Vector2.left;

            RaycastHit2D phit = Physics2D.Raycast(ctx.Self.transform.position, direction, ctx.Status.AttackRange, ctx.RaycastHelper != null ? (int)ctx.RaycastHelper.GetComponent<RayCastHelper>().tagsToDetect.Count : ~0); // Dummy layer check
            // Sửa lại logic fallback để dùng wallLayer hoặc tag check
            
            _cachedObstacle = null; // Tạm thời fallback trả về null để tập trung vào RCH path
            return null;
        }

        // Tấn công obstacle
        protected virtual void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            if (obstacle == null) return;

            // [REFACTOR] Sử dụng DamageHandler trung tâm để giải quyết sát thương
            // Điều này đảm bảo các logic như ICoinShield và IToolShield hoạt động đồng bộ
            float damage = ctx.Status.AttackDamage;
            bool stillAlive = Script.Utils.DamageHandler.ApplyDamage(obstacle, damage, ctx.Self);

            if (ctx.Status.debugEnemy)
            {
                Debug.Log($"[{ctx.Self.name}] Tấn công {obstacle.name} gây {damage} sát thương. Target Valid: {stillAlive}");
            }
        }
    }
}
