using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.MobsManager;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherReturnToBaseState : DenizenState, IArcherAttackState
    {
        private readonly ArcherJob _job;

        public ArcherReturnToBaseState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerMove");
        }

        public override void Update(DenizenContext ctx)
        {
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
                // Tiếp tục di chuyển về base
                Vector3 targetPos = GetBasePosition(ctx);
                ctx.Movement.FaceTarget(targetPos);
                ctx.Movement.MoveTo(targetPos, ctx.Status.RunSpeed);

                if (ctx.Movement.HasReachedTarget)
                {
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

        private Vector3 GetBasePosition(DenizenContext ctx)
        {
            return GuardManager.Instance != null 
                ? GuardManager.Instance.Squad.GetDefensePositionForArcher(ctx.GameObject) 
                : ctx.Transform.position;
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            Vector3 basePos = GetBasePosition(ctx);
            return $"BasePos: {basePos} | DistToBase: {Vector3.Distance(ctx.Transform.position, basePos):F1}m";
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
