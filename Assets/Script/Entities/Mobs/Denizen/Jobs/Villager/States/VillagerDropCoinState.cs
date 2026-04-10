using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerDropCoinState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Animator != null) ctx.Animator.Play("idle");
            ctx.Status.CanMove = false;
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            var villager = ctx.CurrentJob as VillagerJob;
            if (villager == null) return;

            if (ctx.Status.isInDanger || ctx.Status.CurrentCoins <= 0 || (ctx.CoinSharing != null && !ctx.CoinSharing.CanDropCoin()))
            {
                villager.ChangeState(villager.IdleState);
                return;
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.CoinSharing != null && ctx.CoinSharing.CanDropCoin())
            {
                ctx.CoinSharing.TryDropCoin();
            }
        }

        public override void Exit(DenizenContext ctx)
        {
            ctx.Status.CanMove = true;
        }
    }
}
