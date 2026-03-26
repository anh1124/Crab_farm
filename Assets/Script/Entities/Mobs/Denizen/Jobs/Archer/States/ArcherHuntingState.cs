using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherHuntingState : DenizenState, IArcherAttackState
    {
        private readonly ArcherJob _job;
        private float _lastPatrolMoveTime;
        private Vector3 _currentPatrolTarget;
        private bool _hasPatrolTarget;

        // Interval tuần tra (port từ legacy HUNTING_PATROL_INTERVAL = 5s)
        private const float PATROL_INTERVAL = 5f;

        public ArcherHuntingState(ArcherJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            _hasPatrolTarget = false;
            _lastPatrolMoveTime = -PATROL_INTERVAL; // Trigger chọn điểm ngay lập tức
        }

        public override void Update(DenizenContext ctx)
        {
            // --- Combat: Khi có target dừng lại ---
            if (ctx.Status.currentTarget != null)
            {
                if (ctx.Movement.IsMoving)
                    ctx.Movement.StopMovement();
                ctx.Movement.FaceTarget(ctx.Status.currentTarget.transform.position);
                return;
            }

            // --- Patrol: Tuần tra khi không có target ---
            // Chọn điểm patrol mới khi:
            //   1. Chưa có điểm nào HOẶC
            //   2. Đã đến nơi HOẶC
            //   3. Hết interval (giống legacy HUNTING_PATROL_INTERVAL)
            bool needNewTarget = !_hasPatrolTarget
                || ctx.Movement.HasReachedTarget
                || (Time.time - _lastPatrolMoveTime > PATROL_INTERVAL);

            if (needNewTarget)
            {
                _currentPatrolTarget = CalculateHuntingPatrolPosition(ctx);
                _hasPatrolTarget = true;
                _lastPatrolMoveTime = Time.time;

                if (Vector3.Distance(ctx.Transform.position, _currentPatrolTarget) > DenizenMovement.REACHED_THRESHOLD)
                {
                    ctx.Movement.FaceTarget(_currentPatrolTarget);
                    ctx.Movement.MoveTo(_currentPatrolTarget, ctx.Status.WalkSpeed);
                }
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim == null) return;

            // Nếu có target và cooldown xong → bắn
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

            // Không có target: cập nhật animation di chuyển
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

        // Animation event: Draw xong → Release
        public void OnDrawComplete(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("archerRelease");
        }

        // Animation event: Tên bay → Bắn
        public void OnArrowRelease(DenizenContext ctx)
        {
            if (ctx.Status.currentTarget != null && ctx.ArcherShooting != null)
            {
                ctx.ArcherShooting.ShootAt(ctx.Status.currentTarget.transform.position);
            }
        }

        private Vector3 CalculateHuntingPatrolPosition(DenizenContext ctx)
        {
            // Dùng Utility.GetRandomOutsideSafePos để tuần tra ngoài tường xa nhất
            return Utility.GetRandomOutsideSafePos(
                !ctx.Status.isLeftFlank, // isRight
                5f,                      // min distance from wall
                15f,                     // max distance from wall
                30f                      // fallback if no wall
            );
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"Flank: {(ctx.Status.isLeftFlank ? "Left" : "Right")} | " +
                   $"NextPatrolIn: {(PATROL_INTERVAL - (Time.time - _lastPatrolMoveTime)):F1}s | " +
                   $"HasTarget: {ctx.Status.currentTarget != null} | " +
                   $"IsMoving: {ctx.Movement.IsMoving} | PatrolTarget: {_currentPatrolTarget}";
        }
    }
}
