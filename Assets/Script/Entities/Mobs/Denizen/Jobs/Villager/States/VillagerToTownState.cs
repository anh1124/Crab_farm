using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerToTownState : DenizenState
    {
        private readonly VillagerJob _job;
        private Vector3 _townTarget;

        public VillagerToTownState(VillagerJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            // Chọn một điểm ngẫu nhiên trong Plaza (giống legacy HandleMoveToTownCenterState)
            float randomPlazaX = Utility.GetRandomPlazaPos();
            _townTarget = new Vector3(randomPlazaX, ctx.Transform.position.y, 0);
            ctx.Movement.FaceTarget(_townTarget);
            ctx.Movement.MoveTo(_townTarget, ctx.Status.WalkSpeed);
            
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("walk");
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Status.isInDanger)
            {
                _job.ChangeState(_job.FearState);
                return;
            }

            if (ctx.TimeManager != null && ctx.TimeManager.IsDaytime)
            {
                _job.ChangeState(_job.IdleState);
                return;
            }

            // Nếu đến điểm rồi mà vẫn ban đêm -> chọn điểm plaza mới để đứng/idle
            if (!ctx.Movement.IsMoving)
            {
                float randomPlazaX = Utility.GetRandomPlazaPos();
                _townTarget = new Vector3(randomPlazaX, ctx.Transform.position.y, 0);
                ctx.Movement.FaceTarget(_townTarget);
                ctx.Movement.MoveTo(_townTarget, ctx.Status.WalkSpeed);
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim == null) return;

            if (ctx.Movement.IsMoving)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                    anim.Play("walk");
            }
            else
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    anim.Play("idle");
            }
        }
    }
}
