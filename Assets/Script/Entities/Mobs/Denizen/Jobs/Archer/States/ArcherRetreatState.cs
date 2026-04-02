using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherRetreatState : DenizenState
    {
        private GameObject _wallTarget;

        public void SetRetreatTarget(GameObject wall) => _wallTarget = wall;

        public override void Enter(DenizenContext ctx)
        {
            if (ctx.Animator != null) 
                ctx.Animator.Play("archerMove");
            
            ctx.Status.isMovementAllowed = true;

            if (ctx.Movement != null)
                ctx.Movement.SetTarget(_wallTarget.transform.position, ctx.Status.runSpeed);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerSoul = ctx.ArcherJob;
            if (archerSoul == null) return;

            if (_wallTarget == null)
            {
                archerSoul.ChangeState(archerSoul.IdleState);
                return;
            }

            // Nếu đã đến bức tường được chỉ định
            if (archerSoul.HasReachedPosition(_wallTarget.transform.position))
            {
                ctx.Status.assignedWall = _wallTarget;
                if (ctx.Movement != null) ctx.Movement.Stop();
                archerSoul.ChangeState(archerSoul.HoldWallState);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null || ctx.Animator.runtimeAnimatorController == null) return;

            if (ctx.Status.isMoving)
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                    ctx.Animator.Play("archerMove");
            }
            else
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                    ctx.Animator.Play("archerIdle");
            }
        }
    }
}
