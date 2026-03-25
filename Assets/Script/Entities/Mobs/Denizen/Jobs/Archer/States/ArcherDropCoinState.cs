using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherDropCoinState : DenizenState
    {
        private readonly ArcherJob _job;

        public ArcherDropCoinState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            ctx.Movement.StopMovement();
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerIdle");
        }

        public override void Update(DenizenContext ctx)
        {
            // Kiểm tra Player còn trong range không
            bool playerInRange = IsPlayerInRange(ctx);
            bool canDrop = ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin();
            
            // Nếu không thể share coin nữa hoặc player ra khỏi range
            if (!playerInRange || !canDrop)
            {
                ReturnToNormalBehavior(ctx);
                return;
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                anim.Play("archerIdle");

            // Chạy Logic thả coin ở 60Hz (FrameUpdate) để mượt mà nhất
            if (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin())
            {
                ctx.CoinSharing.TryDropCoin();
            }
        }

        private void ReturnToNormalBehavior(DenizenContext ctx)
        {
             if (ctx.GuardManager != null && ctx.GuardManager.Alarm.isEnemyAttack)
             {
                 _job.ChangeState(_job.ReturnToBaseState);
             }
             else if (ctx.TimeManager != null && ctx.TimeManager.IsDaytime)
             {
                 _job.ChangeState(_job.HuntingState);
             }
             else
             {
                 _job.ChangeState(_job.HoldWallState);
             }
        }

        private bool IsPlayerInRange(DenizenContext ctx)
        {
            if (ctx.CircleDetect != null)
            {
                // GetListDetectedObjects() scan mới nhất (không dùng GetDetectedObjects() = cache cũ)
                foreach (var go in ctx.CircleDetect.GetListDetectedObjects())
                {
                    if (go == null) continue;
                    if (go.CompareTag("Player")) return true;
                }
            }
            return false;
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"CanDrop: {(ctx.CoinSharing != null ? ctx.CoinSharing.CanDropCoin().ToString() : "No Sharing")}";
        }
    }
}
