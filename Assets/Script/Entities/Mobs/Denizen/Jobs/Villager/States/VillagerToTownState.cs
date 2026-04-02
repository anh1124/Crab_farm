using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerToTownState : DenizenState
    {
        private Vector3 _target;

        public override void Enter(DenizenContext ctx)
        {
            _target = new Vector3(0, ctx.Transform.position.y, 0f); // Tọa độ trung tâm (Plaza)
            if (ctx.Animator != null) 
                ctx.Animator.Play("walk");

            if (ctx.Movement != null)
                ctx.Movement.SetTarget(_target);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var vSoul = ctx.VillagerJob;
            if (vSoul == null) return;

            // Nếu đã đến trung tâm thành phố thì quay về Idle
            if (vSoul.HasReachedPosition(_target))
            {
                if (ctx.Movement != null) ctx.Movement.Stop();
                vSoul.ChangeState(vSoul.IdleState);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;
            
            // ToTownState nghĩa là đang di chuyển. Trừ khi bị cản thì đứng im chờ StateMachine
            if (ctx.Status.isMoving)
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                    ctx.Animator.Play("walk");
            }
            else
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
