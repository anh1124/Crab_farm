using Script.Game.Enemy.Core;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    public class ReturnToPortalState : EnemyState
    {
        protected float _attackTimer;
        protected bool _isWaitingForCooldown = false;

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
                ctx.Controller.ChangeState(new IdleState());
                return;
            }

            // 2. Scan các vật cản (Chỉ tấn công Tường khi đang rút lui)
            GameObject obstacle = ScanForWallObstacle(ctx);
            if (obstacle != null)
            {
                // Hành vi Tấn công tương tự MoveToTarget
                if (_attackTimer >= ctx.Status.AttackCooldown)
                {
                    _attackTimer = 0;
                    _isWaitingForCooldown = false;
                    PerformAttack(ctx, obstacle);
                    
                    if (ctx.Animator != null) ctx.Animator.SetTrigger("Attack"); 
                }
                else
                {
                    _attackTimer += Time.deltaTime;
                    if (!_isWaitingForCooldown)
                    {
                        _isWaitingForCooldown = true;
                        if (ctx.Animator != null) ctx.Animator.SetTrigger("Idle");
                    }
                }
                return; 
            }

            // 3. Di chuyển về Portal
            if (_isWaitingForCooldown)
            {
                _isWaitingForCooldown = false;
                if (ctx.Animator != null) ctx.Animator.SetTrigger("Walk");
            }
            Script.Utils.MovementUtility.MoveTowardsTarget(ctx.Self, ctx.HomePortal.transform.position, ctx.Status.MoveSpeed);

            ctx.Controller.DebugAttackTimer = _attackTimer;
        }

        public override void Exit(EnemyContext ctx)
        {
            _attackTimer = 0;
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
