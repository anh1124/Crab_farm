using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager.Building;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherHoldWallState : DenizenState, IArcherAttackState
    {
        private readonly ArcherJob _job;
        private Vector3 _defensePosition;

        public ArcherHoldWallState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            _defensePosition = CalculateDefensePosition(ctx);

            ctx.Movement.StopMovement();
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerIdle");
        }

        public override void Update(DenizenContext ctx)
        {
            // UpdateBehavior (5-10Hz): Chỉ lo việc di chuyển/định vị
            if (ctx.Status.currentTarget != null)
            {
                ctx.Movement.StopMovement();
                ctx.Movement.FaceTarget(ctx.Status.currentTarget.transform.position);
            }
            else
            {
                // Chỉ di chuyển nếu xa vị trí thủ ít nhất REACHED_THRESHOLD
                if (Vector3.Distance(ctx.Transform.position, _defensePosition) > DenizenMovement.REACHED_THRESHOLD)
                {
                    if (!ctx.Movement.IsMoving)
                    {
                        ctx.Movement.FaceTarget(_defensePosition);
                        ctx.Movement.MoveTo(_defensePosition, ctx.Status.WalkSpeed);
                    }
                }
                else
                {
                    if (ctx.Movement.IsMoving) ctx.Movement.StopMovement();
                    ctx.Movement.FaceOutward(ctx.Status.isLeftFlank);
                }
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            // FrameUpdate (60Hz): Xử lý Tấn công để đảm bảo bắn ngay lập tức khi sẵn sàng
            Animator anim = _job.GetAnimator();
            if (anim == null) return;

            if (ctx.Status.currentTarget != null)
            {
                if (ctx.ArcherShooting != null && ctx.ArcherShooting.CanShoot(ctx.Status.AttackCooldown))
                {
                    ctx.ArcherShooting.StartShooting();
                    anim.Play("archerDraw");
                }
            }

            // Tránh đè animation khi đang bắn (Draw/Release)
            if ((ctx.ArcherShooting != null && ctx.ArcherShooting.IsAttacking) || 
                anim.GetCurrentAnimatorStateInfo(0).IsName("archerRelease") || 
                anim.GetCurrentAnimatorStateInfo(0).IsName("archerDraw"))
                return;

            // Visual check
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

        private Vector3 CalculateDefensePosition(DenizenContext ctx)
        {
            if (ctx.GuardManager != null)
            {
                return ctx.GuardManager.Squad.GetDefensePositionForArcher(ctx.GameObject);
            }
            return ctx.Transform.position;
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

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"Target: {(ctx.Status.currentTarget != null ? ctx.Status.currentTarget.name : "None")}";
        }
    }
}
