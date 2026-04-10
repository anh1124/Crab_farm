using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherIdleState : DenizenState
    {
        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Animator != null) 
                ctx.Animator.Play("archerIdle");
                
            ctx.Status.CanMove = false;
            
            if (ctx.Movement != null)
                ctx.Movement.Stop();
        }

        public override void Exit(DenizenContext ctx)
        {
            ctx.Status.CanMove = true;
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator != null && !ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                ctx.Animator.Play("archerIdle");
        }
    }
}
