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
            float randomX = WorldUtility.GetRandomPlazaPos();
            _target = new Vector3(randomX, ctx.Transform.position.y, 0f);
            if (ctx.Status != null)
                ctx.Status.CanMove = true;

            // Tốc độ ban đầu sẽ được quyết định trong UpdateBehavior để tránh lặp logic
            UpdateSpeed(ctx);
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
                return;
            }

            UpdateSpeed(ctx);
        }

        private void UpdateSpeed(DenizenContext ctx)
        {
            if (ctx.Movement == null || ctx.Status == null) return;

            // Logic thay đổi tốc độ dựa trên vị trí (Plaza vs Outside)
            bool isInPlaza = WorldUtility.IsInPlazaZone(ctx.Transform.position.x);
            bool safe = !ctx.Status.isInDanger && ctx.Status.fearTimer <= 0;

            // Nếu ở trong plaza và an toàn -> Walk
            // Nếu ở ngoài plaza HOẶC đang gặp nguy hiểm -> Run
            float targetSpeed = (isInPlaza && safe) ? ctx.Status.walkSpeed : ctx.Status.runSpeed;
            
            ctx.Movement.SetTarget(_target, targetSpeed);
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null || ctx.Status == null) return;
            
            if (ctx.Status.isMoving)
            {
                // Chọn animation dựa trên speed thực tế
                string animName = ctx.Status.currentSpeed > ctx.Status.walkSpeed + 0.1f ? "run" : "walk";
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName(animName))
                    ctx.Animator.Play(animName);
            }
            else
            {
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
