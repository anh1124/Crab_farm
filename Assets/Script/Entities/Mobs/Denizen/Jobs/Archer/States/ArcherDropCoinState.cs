using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherDropCoinState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Animator != null) ctx.Animator.Play("archerIdle");
            ctx.Status.isMovementAllowed = false;
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;

            // 1. CHECK DANGER
            if (ctx.Status.isInDanger)
            {
                ReturnToNormalBehavior(ctx);
                return;
            }

            // 2. CHECK PLAYER RANGE
            bool playerInRange = IsPlayerInRange(ctx);
            bool canDrop = ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin();
            
            if (!playerInRange || !canDrop)
            {
                ReturnToNormalBehavior(ctx);
                return;
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator != null && !ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                ctx.Animator.Play("archerIdle");

            if (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin())
            {
                ctx.CoinSharing.TryDropCoin();
            }
        }

        private void ReturnToNormalBehavior(DenizenContext ctx)
        {
             var archer = ctx.CurrentJob as ArcherJob;
             if (archer == null) return;

             if (ctx.GuardManager != null && ctx.GuardManager.isEnemyAttack)
             {
                 archer.ChangeState(archer.ReturnToBaseState);
             }
             else if (ctx.TimeManager != null && ctx.TimeManager.IsDaytime)
             {
                 archer.ChangeState(archer.HuntingState);
             }
             else
             {
                 archer.ChangeState(archer.HoldWallState);
             }
        }

        public override void Exit(DenizenContext ctx)
        {
            ctx.Status.isMovementAllowed = true;
        }

        private bool IsPlayerInRange(DenizenContext ctx)
        {
            if (ctx.CircleDetect != null)
            {
                foreach (var go in ctx.CircleDetect.GetListDetectedObjects())
                {
                    if (go == null) continue;
                    if (go.CompareTag("Player")) return true;
                }
            }
            return false;
        }
    }
}
