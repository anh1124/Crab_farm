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
            if (ctx.Status != null)
                ctx.Status.CanMove = true;

            if (ctx.Animator != null) 
                ctx.Animator.Play("run");

            // Dùng runSpeed để chạy về safe zone nhanh hơn khi đêm
            if (ctx.Movement != null)
                ctx.Movement.SetTarget(_target, ctx.Status != null ? ctx.Status.runSpeed : -1f);
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var villagerJob = ctx.VillagerJob;
            if (villagerJob == null) return;

            // Nếu đã đến trung tâm thành phố thì quay về Idle
            if (villagerJob.HasReachedPosition(_target))
            {
                if (ctx.Movement != null) ctx.Movement.Stop();
                villagerJob.ChangeState(villagerJob.IdleState);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;
            
            // ToTownState khi đêm: chạy về safe zone
            if (ctx.Status.isMoving)
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("run"))
                    ctx.Animator.Play("run");
            }
            else
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
