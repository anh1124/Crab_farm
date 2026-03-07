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

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Walk");

            // Có thể tấn công ngay ở frame đầu chạm obstacle
            _attackTimer = ctx.Status.AttackCooldown;
        }

        public override void Update(EnemyContext ctx)
        {
            // 1. Phân giải mục tiêu gốc của quốc gia (TownCenter / Vương Quốc)
            GameObject kingdomTarget = ctx.TargetManager.GetTarget(ctx.Self);
            
            if (kingdomTarget == null || !kingdomTarget.activeSelf)
            {
                ctx.Controller.ChangeState(new IdleState());
                return;
            }

            // 2. Greed mechanic (nếu có - với shieldbot GreedThreshold = 0)
            if (ctx.GreedIsFull)
            {
                ctx.Controller.ChangeState(new ReturnToPortalState());
                return;
            }

            // 3. Scan các vật cản (Obstacles)
            GameObject obstacle = ScanForObstacle(ctx);
            if (obstacle != null)
            {
                // Attack logic
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _attackTimer = 0;
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
                    _attackTimer += Time.deltaTime;
                    
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle"); // trigger Idle để quái đứng im chờ
                    }
                }
                
                // Khi có obstacle, enemy dừng lại để chờ cooldown đánh
                return; 
            }

            // 4. Nếu không có obstacle, tiếp tục di chuyển về phía TownCenter
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }
            MovementUtility.MoveTowardsTarget(ctx.Self, kingdomTarget.transform.position, ctx.Status.MoveSpeed);

            // Cập nhật ui debug cooldown
            ctx.Controller.DebugAttackTimer = _attackTimer;
        }

        public override void Exit(EnemyContext ctx)
        {
            _attackTimer = 0;
        }

        protected GameObject ScanForObstacle(EnemyContext ctx)
        {
            // 1. Nếu Enemy có gắn RayCastHelper, ưu tiên sử dụng
            var raycastHelper = ctx.Self.GetComponent<RayCastHelper>();
            if (raycastHelper != null)
            {
                // Thay vì vẽ tia, lấy trực tiếp danh sách rà quét tự động từ helper
                var hits = raycastHelper.GetListHitObjects(ctx.Status.AttackRange);
                
                if (hits.Count > 0)
                {
                    // Chọn mục tiêu đầu tiên hợp lệ (còn máu/khiên)
                    GameObject validObstacle = null;
                    foreach (var hit in hits)
                    {
                        if (Script.Utils.CombatUtility.IsTargetValidAndAlive(hit))
                        {
                            // Ưu tiên TownCenter nếu nó nằm trong danh sách quét
                            GameObject kingdomTarget = ctx.TargetManager.GetTarget(ctx.Self);
                            if (hit == kingdomTarget) 
                            {
                                return hit; 
                            }
                            // Bỏ túi vật cản đầu tiên hợp lệ nếu chưa lưu ai
                            if (validObstacle == null) 
                            {
                                validObstacle = hit;
                            }
                        }
                    }
                    
                    if (validObstacle != null) return validObstacle;
                }
                
                return null; // Quét xong không thấy ai
            }

            // 2. Fallback cổ điển (Nếu cấu hình quên gắn RayCastHelper)
            GameObject fallbackKingdomTarget = ctx.TargetManager.GetTarget(ctx.Self);
            if (fallbackKingdomTarget == null) return null;

            Vector2 direction = (fallbackKingdomTarget.transform.position.x > ctx.Self.transform.position.x) ? Vector2.right : Vector2.left;

            RaycastHit2D phit = Physics2D.Raycast(
                ctx.Self.transform.position, 
                direction, 
                ctx.Status.AttackRange
            );

            if (phit.collider != null)
            {
                if (PrefabData.Instance != null && PrefabData.Instance.enemyObstacleTags != null)
                {
                    if (PrefabData.Instance.enemyObstacleTags.Contains(phit.collider.tag))
                    {
                        return phit.collider.gameObject;
                    }
                }
                if (phit.collider.gameObject == fallbackKingdomTarget)
                {
                    return fallbackKingdomTarget;
                }
            }

            return null;
        }

        // Tấn công obstacle
        protected virtual void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            // Ưu tiên phá Shield coin trước
            ICoinShield coinShield = obstacle.GetComponent<ICoinShield>();
            if (coinShield != null && coinShield.HasCoinShield)
            {
                bool consumed = coinShield.ConsumeCoinShield();
                if (consumed)
                {
                    Debug.Log($"[{ctx.Self.name}] Consumed CoinShield of {obstacle.name}");
                    return; // Một phát đánh rơi 1 coin, không trừ HP
                }
            }

            // Trừ máu IHealth
            IHealth health = obstacle.GetComponent<IHealth>();
            if (health != null)
            {
                health.TakeDamage(ctx.Status.AttackDamage);
                Debug.Log($"[{ctx.Self.name}] Dealt {ctx.Status.AttackDamage} damage to {obstacle.name}");
            }
        }
    }
}
