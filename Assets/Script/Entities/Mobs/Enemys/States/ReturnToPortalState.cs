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

        public override void Enter(EnemyContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("Walk");

            _attackTimer = ctx.Status.AttackCooldown;
        }

        public override void Update(EnemyContext ctx)
        {
            // 1. Kiểm tra Portal 
            if (ctx.HomePortal == null || !ctx.HomePortal.activeSelf)
            {
                _isMoving = false;
                ctx.Controller.ChangeState(new IdleState());
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

        public override void PhysicsUpdate(EnemyContext ctx)
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
            // Quét tia ray hướng về phía cổng
            Vector2 direction = (ctx.HomePortal.transform.position.x > ctx.Self.transform.position.x) ? Vector2.right : Vector2.left;

            RaycastHit2D phit = Physics2D.Raycast(
                ctx.Self.transform.position, 
                direction, 
                ctx.Status.AttackRange
            );

            if (phit.collider != null)
            {
                // Chỉ tấn công nếu vật cản là Wall
                if (phit.collider.CompareTag("Wall"))
                {
                    return phit.collider.gameObject;
                }
            }

            return null;
        }

        private void PerformAttack(EnemyContext ctx, GameObject obstacle)
        {
            Script.Interfaces.ICoinShield coinShield = obstacle.GetComponent<Script.Interfaces.ICoinShield>();
            if (coinShield != null && coinShield.HasCoinShield)
            {
                if (coinShield.ConsumeCoinShield()) return; 
            }

            Script.Game.Enemy.Interfaces.IHealth health = obstacle.GetComponent<Script.Game.Enemy.Interfaces.IHealth>();
            if (health != null)
            {
                health.TakeDamage(ctx.Status.AttackDamage);
            }
        }
    }
}
