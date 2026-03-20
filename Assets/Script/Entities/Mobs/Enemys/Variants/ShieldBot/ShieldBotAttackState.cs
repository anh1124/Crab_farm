using UnityEngine;
using Script.Game.Enemy.States;
using Script.Utils;

namespace Script.Game.Enemy.Variants.ShieldBot
{
    /// <summary>
    /// AI riêng cho ShieldBot: Tấn công bằng Shock Attack (AOE) thay vì sát thương đơn mục tiêu.
    /// Kế thừa từ MoveToTargetState để tận dụng logic di chuyển và dò mục tiêu.
    /// </summary>
    public class ShieldBotAttackState : MoveToTargetState
    {
        public override void Enter(EnemyContext ctx)
        {
            ResetCombatFlags();
            _attackTimer = ctx.Status.AttackCooldown;
            
            // Lưu ý: ShieldBot dùng tên Trigger là "walk" (chữ thường)
            if (ctx.Animator != null) ctx.Animator.SetTrigger("walk");
        }

        public override void Update(EnemyContext ctx)
        {
            // Quá trình vung đòn Shock Attack là AOE, bắt buộc đứng im cho đến khi kết thúc Anim Event
            if (_isAttacking)
            {
                _isMoving = false;
                return;
            }

            // Sử dụng logic di chuyển và scan mục tiêu của base
            base.Update(ctx);
        }

        protected override void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            // Ghi đè: ShieldBot không gây sát thương bằng code ngay lập tức.
            // Chúng ta chỉ kích hoạt Animation, sát thương thực tế sẽ do Anim Event xử lý.
            if (ctx.Animator != null)
            {
                ctx.Animator.SetTrigger("shock attack");
            }
        }

        public override void Exit(EnemyContext ctx)
        {
            base.Exit(ctx);
            _isAttacking = false;
            
            // Tính năng an toàn: Tắt collider nếu AI bị đổi trạng thái hoặc chết giữa lúc vung đòn
            if (ctx.Controller is ShieldBotAIController shieldBot)
            {
                shieldBot.ForceDisableShockCollider();
            }
        }

        // ==========================================
        // AOE LOGIC - Được gọi từ ShieldBotAIController (Animation Events)
        // ==========================================
        
        public void DealDamageFromEvent(EnemyContext ctx)
        {
            if (ctx.Controller is ShieldBotAIController shieldBot && shieldBot.ShockAttackCollider != null)
            {
                Collider2D damageCollider = shieldBot.ShockAttackCollider.GetComponent<Collider2D>();
                if (damageCollider != null && damageCollider.gameObject.activeInHierarchy)
                {
                    System.Collections.Generic.List<Collider2D> hits = new System.Collections.Generic.List<Collider2D>();
                    ContactFilter2D filter = new ContactFilter2D();
                    filter.useTriggers = true; 
                    
                    int hitCount = Physics2D.OverlapCollider(damageCollider, filter, hits);
                    
                    if (hitCount > 0)
                    {
                        System.Collections.Generic.HashSet<GameObject> hitTargets = new System.Collections.Generic.HashSet<GameObject>();
                        var rch = ctx.RaycastHelper; // Dùng cache từ context
                        
                        foreach (var hitCol in hits)
                        {
                            GameObject hitObj = hitCol.gameObject;
                            
                            if (hitObj.transform.IsChildOf(ctx.Self.transform) || hitObj.transform == ctx.Self.transform)
                                continue;
                                
                            GameObject validTarget = null;
                            if (rch != null && rch.tagsToDetect.Count > 0)
                            {
                                if (rch.tagsToDetect.Contains(hitObj.tag))
                                    validTarget = hitObj;
                                else if (hitObj.transform.parent != null && rch.tagsToDetect.Contains(hitObj.transform.parent.tag))
                                    validTarget = hitObj.transform.parent.gameObject;
                            }
                            else
                            {
                                validTarget = hitObj;
                            }
                            
                            if (validTarget != null && validTarget.activeInHierarchy && !hitTargets.Contains(validTarget))
                            {
                                if (Script.Utils.CombatUtility.IsTargetValidAndAlive(validTarget))
                                {
                                    hitTargets.Add(validTarget);
                                    // Gây sát thương thực tế (base.PerformAttack chỉ gọi Code Damage)
                                    Script.Utils.DamageHandler.ApplyDamage(validTarget, ctx.Status.AttackDamage, ctx.Self);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Đồng bộ hàm cũ với logic Reset mới của CombatState
        /// </summary>
        public void EndAttackEvent(EnemyContext ctx)
        {
            OnAttackAnimationComplete(ctx);
        }
    }
}
