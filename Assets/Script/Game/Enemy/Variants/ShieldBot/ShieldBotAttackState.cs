using UnityEngine;
using Script.Game.Enemy.States;
using Script.Utils;

namespace Script.Game.Enemy.Variants.ShieldBot
{
    public class ShieldBotAttackState : MoveToTargetState
    {
        private bool _isAttacking = false;

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("walk");

            _attackTimer = ctx.Status.AttackCooldown;
        }

        public override void Update(EnemyContext ctx)
        {
            if (ctx.GreedIsFull)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(new ReturnToPortalState());
                return;
            }

            // Đứng im tại chỗ để chờ quá trình "shock attack" hoàn tất
            // LƯU Ý: Phải đặt trên cả check Target null để tay đang vung thì bắt buộc vung nốt.
            if (_isAttacking)
            {
                _isMoving = false;
                ctx.Controller.DebugAttackTimer = _attackTimer;
                return;
            }

            _kingdomTarget = ctx.TargetManager.GetTarget(ctx.Self);
            if (_kingdomTarget == null || !_kingdomTarget.activeSelf)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(new IdleState());
                return;
            }


            GameObject obstacle = ScanForObstacle(ctx);
            if (obstacle != null)
            {
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _isAttacking = true;
                    _isWaitingForCooldown = false;
                    
                    if (ctx.Animator != null)
                    {
                        // Gọi anim shock attack
                        ctx.Animator.SetTrigger("shock attack"); 
                    }
                }
                else
                {
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle");
                    }
                }
                
                // Khi chạm mục tiêu, dừng lại đứng yên để chờ tung đòn
                _isMoving = false;
                return; 
            }

            // Di chuyển nếu không có vật cản
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("walk");
            }
            _isMoving = true;

            // Update debug timer
            ctx.Controller.DebugAttackTimer = _attackTimer;
        }

        public override void Exit(EnemyContext ctx)
        {
            base.Exit(ctx);
            _isAttacking = false;
            
            // QUAN TRỌNG: Tắt collider đi nếu bị ép chết (DeadState) hoặc ép Cancel giữa chừng.
            if (ctx.Controller is ShieldBotAIController shieldBot)
            {
                shieldBot.ForceDisableShockCollider();
            }
        }

        // --- ĐƯỢC GỌI TỪ EVENT CỦA CONTROLLER ---
        
        public void DealDamageFromEvent(EnemyContext ctx)
        {
            if (ctx.Controller is ShieldBotAIController shieldBot && shieldBot.ShockAttackCollider != null)
            {
                Collider2D damageCollider = shieldBot.ShockAttackCollider.GetComponent<Collider2D>();
                if (damageCollider != null && damageCollider.gameObject.activeInHierarchy)
                {
                    System.Collections.Generic.List<Collider2D> hits = new System.Collections.Generic.List<Collider2D>();
                    ContactFilter2D filter = new ContactFilter2D();
                    filter.useTriggers = true; // Cho phép đánh trúng cả Player hitbox Trigger
                    
                    int hitCount = Physics2D.OverlapCollider(damageCollider, filter, hits);
                    
                    if (hitCount > 0)
                    {
                        System.Collections.Generic.HashSet<GameObject> hitTargets = new System.Collections.Generic.HashSet<GameObject>();
                        var rch = ctx.Self.GetComponent<RayCastHelper>();
                        
                        foreach (var hitCol in hits)
                        {
                            GameObject hitObj = hitCol.gameObject;
                            
                            // 1. Loại trừ bản thân
                            if (hitObj.transform.IsChildOf(ctx.Self.transform) || hitObj.transform == ctx.Self.transform)
                                continue;
                                
                            // 2. Lọc bằng Tag y hệt RaycastHelper
                            GameObject validTarget = null;
                            if (rch != null && rch.tagsToDetect.Count > 0)
                            {
                                if (rch.tagsToDetect.Contains(hitObj.tag))
                                {
                                    validTarget = hitObj;
                                }
                                else if (hitObj.transform.parent != null && rch.tagsToDetect.Contains(hitObj.transform.parent.tag))
                                {
                                    validTarget = hitObj.transform.parent.gameObject;
                                }
                            }
                            else
                            {
                                // Fallback nếu quên gắn RaycastHelper
                                validTarget = hitObj;
                            }
                            
                            Debug.Log($"[ShieldBot AOE] Hit Object: {hitObj.name} (Tag: {hitObj.tag}) -> Valid Target Detected: {validTarget?.name}");
                            
                            // 3. Gay dame (chống đúp damage lên cùng 1 target nếu target có nhiều collider)
                            if (validTarget != null && validTarget.activeInHierarchy && !hitTargets.Contains(validTarget))
                            {
                                // 4. Kiểm tra mục tiêu CÒN SỐNG (HP > 0 hoặc Có Khiên)
                                bool isAlive = Script.Utils.CombatUtility.IsTargetValidAndAlive(validTarget);
                                Debug.Log($"[ShieldBot AOE] Check target: {validTarget.name} IsAlive = {isAlive}");

                                if (isAlive)
                                {
                                    hitTargets.Add(validTarget);
                                    Debug.Log($"[ShieldBot AOE] PerformAttack on {validTarget.name}!");
                                    PerformAttack(ctx, validTarget);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[ShieldBot] Shock attack hụt. Không có vật thể nào lọt vào vùng Collider Sát thương.");
                    }
                }
            }
        }

        public void EndAttackEvent(EnemyContext ctx)
        {
            _attackTimer = 0;
            _isAttacking = false;
            
            // Ép về Idle sau khi đánh xong chờ cooldown
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("Idle"); 
            }
        }
    }
}
