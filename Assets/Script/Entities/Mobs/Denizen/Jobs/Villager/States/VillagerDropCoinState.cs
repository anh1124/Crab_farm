using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerDropCoinState : DenizenState
    {
        private readonly VillagerJob _job;

        public VillagerDropCoinState(VillagerJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            ctx.Movement.StopMovement();
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("idle");
        }

        public override void Update(DenizenContext ctx)
        {
            // Nếu bị tấn công thoát ngay
            if (ctx.Status.isInDanger)
            {
                _job.ChangeState(_job.FearState);
                return;
            }

            // Kiểm tra Player còn trong range không ở 5Hz (UpdateBehavior)
            // Giữ cho hàm Scan Physics chạy nhẹ nhất có thể
            bool playerInRange = IsPlayerInRange(ctx);
            bool canDrop = (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin());
            
            // Nếu không thể share coin nữa hoặc player ra khỏi range
            if (!playerInRange || !canDrop)
            {
                _job.TransitionToIdle();
                return;
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                anim.Play("idle");

            // Chạy Logic thả coin ở 60Hz (FrameUpdate) để mượt mà nhất với cooldown
            // Vì Update của NPC chạy 5Hz (0.2s) nên lỡ nhịp cooldown rất dễ bị cộng dồn thành giật/chậm
            if (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin())
            {
                ctx.CoinSharing.TryDropCoin();
            }
        }

        private bool IsPlayerInRange(DenizenContext ctx)
        {
            // Dân làng dùng Raycaster để kiểm tra phạm vi
            if (ctx.RayCaster != null)
            {
                foreach (var go in ctx.RayCaster.GetListHitObjects())
                {
                    if (go == null) continue;
                    if (go.CompareTag("Player")) return true;
                }
            }
            return false;
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            bool inRange = IsPlayerInRange(ctx);
            bool canDrop = (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin());
            return $"PlayerInRange: {inRange} | CanDropCode: {canDrop} | Coins: {ctx.Status.CurrentCoins}";
        }
    }
}
