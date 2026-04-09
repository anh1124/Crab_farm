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
            var archerJob = ctx.ArcherJob;
            if (archerJob == null) return;

            // Đêm xuống → phòng thủ
            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                archerJob.ChangeState(archerJob.ReturnToBaseState);
                return;
            }

            // Phát hiện mục tiêu
            if (ctx.Status.currentTarget != null)
            {
                // Chỉ chuẩn bị tấn công nếu Skill đã hồi chiêu xong
                if (ctx.SkillManager != null && ctx.SkillManager.IsReady(0))
                {
                    // Animal và EnemyPortal nằm cùng cấp -> săn nã tự do
                    bool isHuntingTarget = ctx.Status.currentTarget.CompareTag(GameTags.Animal) || ctx.Status.currentTarget.CompareTag(GameTags.EnemyPortal);
                    
                    if (isHuntingTarget)
                    {
                        archerJob.ChangeState(archerJob.AttackState);
                    }
                    else
                    {
                        // Đích thị là Enemy đang tiến quân -> phải kiểm tra kỷ luật vị trí
                        if (archerJob.CanAttackFromHere())
                            archerJob.ChangeState(archerJob.AttackState);
                        else
                            archerJob.ChangeState(archerJob.ReturnToBaseState); // Về nơi an toàn trước
                    }
                    return;
                }
            }

            // Đi săn lang lang trong vùng bãi săn được định nghĩa trong GuardManager
            if (!_hasTarget && Time.time >= _nextReevaluate)
            {
                bool isLeft = !ctx.Status.isLeftFlank; // Hướng bãi săn ngược với cánh đứng 
                if (ctx.GuardManager != null)
                {
                    var bounds = WorldUtility.GetHuntingBounds(ctx.GuardManager, ctx.Status.isLeftFlank);
                    float randomX = Random.Range(bounds.start, bounds.end);
                    _huntingTarget = new Vector3(randomX, ctx.Transform.position.y, 0f);
                    _hasTarget = true;
                    archerJob.SetMoveTarget(_huntingTarget);
                }
                else
                {
                    // Fallback nếu không có GuardManager
                    float randomX = Script.Utility.GetRandomHuntingPos(); 
                    _huntingTarget = new Vector3(randomX, ctx.Transform.position.y, 0f);
                    _hasTarget = true;
                    archerJob.SetMoveTarget(_huntingTarget);
                }
                
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
