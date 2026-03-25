using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherRetreatState : DenizenState
    {
        private readonly ArcherJob _job;
        private GameObject _retreatTarget;

        public ArcherRetreatState(ArcherJob job)
        {
            _job = job;
        }

        public void SetRetreatTarget(GameObject wall)
        {
            _retreatTarget = wall;
        }

        public override void Enter(DenizenContext ctx)
        {
            if (_retreatTarget == null)
            {
                 _job.ChangeState(_job.ReturnToBaseState);
                 return;
            }
            
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerMove");
        }

        public override void Update(DenizenContext ctx)
        {
            if (_retreatTarget == null || !_retreatTarget.activeInHierarchy)
            {
                 _job.ChangeState(_job.ReturnToBaseState);
                 return;
            }

            Vector3 targetPos = _retreatTarget.transform.position;
            ctx.Movement.FaceTarget(targetPos);
            ctx.Movement.MoveTo(targetPos, ctx.Status.RunSpeed);

            // Kiểm tra đã đến nơi an toàn chưa
            if (ctx.Movement.HasReachedTarget)
            {
                ctx.Status.assignedWall = _retreatTarget;
                _job.ChangeState(_job.HoldWallState);
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                anim.Play("archerMove");
        }

        public override void Exit(DenizenContext ctx)
        {
            _retreatTarget = null;
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"RetreatTarget: {(_retreatTarget != null ? _retreatTarget.name : "None")} | " +
                   $"DistToTarget: {( _retreatTarget != null ? Vector3.Distance(ctx.Transform.position, _retreatTarget.transform.position) : 0):F1}m";
        }
    }
}
