using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Manager.WorldManager;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherHuntingState : DenizenState
    {
        private float _nextReevaluate;
        private Vector3 _huntingTarget;
        private bool _hasTarget;

        public override void Enter(DenizenContext ctx)
        {
            _nextReevaluate = Time.time + Random.Range(1f, 3f);
            _hasTarget = false;
            
            // Đảm bảo dùng đúng tên anim cho Archer và kiểm tra controller
            if (ctx.Animator != null && ctx.Animator.runtimeAnimatorController != null)
                ctx.Animator.Play("archerIdle");
                
            ctx.Movement?.Stop();
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var aSoul = ctx.ArcherJob;
            if (aSoul == null) return;

            // Đêm xuống → phòng thủ
            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                aSoul.ChangeState(aSoul.ReturnToBaseState);
                return;
            }

            // Phát hiện địch → kiểm tra vùng an toàn trước khi attack
            if (ctx.Status.currentTarget != null)
            {
                if (aSoul.CanAttackFromHere())
                    aSoul.ChangeState(aSoul.AttackState);
                else
                    aSoul.ChangeState(aSoul.ReturnToBaseState); // Về nơi an toàn trước
                return;
            }

            // Đi săn lang thang trong vùng hợp lệ
            if (!_hasTarget && Time.time >= _nextReevaluate)
            {
                // Thay vì random cả 2 bên (dễ chạy xuyên vương quốc), hãy săn ở hướng hiện tại của Archer
                bool huntOnRight = ctx.Transform.position.x > 0;
                Vector3 huntPos = Script.Utility.GetRandomOutsideSafePos(huntOnRight, 5f, 35f);
                
                _huntingTarget = new Vector3(huntPos.x, ctx.Transform.position.y, 0f);
                _hasTarget = true;
                aSoul.SetMoveTarget(_huntingTarget);
                
                if (ctx.Animator != null && ctx.Animator.runtimeAnimatorController != null)
                    ctx.Animator.Play("archerMove");
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null || ctx.Animator.runtimeAnimatorController == null) return;

            if (_hasTarget)
            {
                if (ctx.ArcherJob.HasReachedPosition(_huntingTarget))
                {
                    _hasTarget = false;
                    _nextReevaluate = Time.time + Random.Range(3f, 8f);
                    ctx.Movement?.Stop();
                    ctx.Animator.Play("archerIdle");
                }
                else
                {
                    // Luôn đảm bảo chơi archerMove khi đang di chuyển săn
                    if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerMove"))
                        ctx.Animator.Play("archerMove");
                }
            }
            else
            {
                // Luôn đảm bảo chơi archerIdle khi đứng yên
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("archerIdle"))
                    ctx.Animator.Play("archerIdle");
            }
        }
    }
}
