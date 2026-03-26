using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherRetreatState : DenizenState, IArcherAttackState
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

            // Kiểm tra có được phép bắn không
            bool canShoot = ctx.Status.currentTarget != null && Utility.IsInKingdomSafeZone(ctx.Transform.position.x);

            if (canShoot)
            {
                // Dừng lại và quay về phía enemy
                ctx.Movement.StopMovement();
                ctx.Movement.FaceTarget(ctx.Status.currentTarget.transform.position);
            }
            else
            {
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
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim == null) return;

            bool canShoot = ctx.Status.currentTarget != null && Utility.IsInKingdomSafeZone(ctx.Transform.position.x);

            if (canShoot)
            {
                if (ctx.ArcherShooting != null && ctx.ArcherShooting.CanShoot(ctx.Status.AttackCooldown))
                {
                    ctx.ArcherShooting.StartShooting();
                    anim.Play("archerDraw");
                }
            }

            // Không ghi đè animation khi đang bắn
            if ((ctx.ArcherShooting != null && ctx.ArcherShooting.IsAttacking) || 
                anim.GetCurrentAnimatorStateInfo(0).IsName("archerRelease") || 
                anim.GetCurrentAnimatorStateInfo(0).IsName("archerDraw"))
                return;

            if (ctx.Movement.IsMoving)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                    anim.Play("archerMove");
            }
            else
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                    anim.Play("archerIdle");
            }
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

        public void OnDrawComplete(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerRelease");
        }

        public void OnArrowRelease(DenizenContext ctx)
        {
            if (ctx.Status.currentTarget != null && ctx.ArcherShooting != null)
            {
                ctx.ArcherShooting.ShootAt(ctx.Status.currentTarget.transform.position);
            }
        }
    }
}
