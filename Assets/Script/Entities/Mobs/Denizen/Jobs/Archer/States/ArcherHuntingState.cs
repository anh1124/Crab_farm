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
                
            if (ctx.Status != null)
                ctx.Status.CanMove = true;

            ctx.Movement?.Stop();
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var archerJob = ctx.ArcherJob;
            if (archerJob == null) return;

            // 1. Kiểm tra thời gian (Đêm xuống -> Về căn cứ)
            if (HandleTimeTransition(ctx, archerJob)) return;

            // 2. Kiểm tra mục tiêu chiến đấu
            if (HandleCombatTransitions(ctx, archerJob)) return;

            // 3. Quét tiền thưởng (Loot)
            if (HandleLootCollection(ctx, archerJob)) return;

            // 4. Di chuyển săn bắn ngẫu nhiên
            HandleRandomHunting(ctx, archerJob);
        }

        private bool HandleTimeTransition(DenizenContext ctx, ArcherJob archerJob)
        {
            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                archerJob.ChangeState(archerJob.ReturnToBaseState);
                return true;
            }
            return false;
        }

        private bool HandleCombatTransitions(DenizenContext ctx, ArcherJob archerJob)
        {
            if (ctx.Status.currentTarget != null)
            {
                if (ctx.SkillManager != null && ctx.SkillManager.IsReady(0))
                {
                    bool isHuntingTarget = ctx.Status.currentTarget.CompareTag(GameTags.Animal) || 
                                         ctx.Status.currentTarget.CompareTag(GameTags.EnemyPortal);
                    
                    if (isHuntingTarget)
                    {
                        archerJob.ChangeState(archerJob.AttackState);
                    }
                    else
                    {
                        if (archerJob.CanAttackFromHere())
                            archerJob.ChangeState(archerJob.AttackState);
                        else
                            archerJob.ChangeState(archerJob.ReturnToBaseState);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool HandleLootCollection(DenizenContext ctx, ArcherJob archerJob)
        {
            if (ctx.Status != null && !ctx.Status.IsFull)
            {
                var detectedObjects = ctx.CircleDetect?.GetDetectedObjects();
                if (detectedObjects != null)
                {
                    GameObject nearestCoin = null;
                    float minDist = float.MaxValue;

                    foreach (var obj in detectedObjects)
                    {
                        if (obj == null || !obj.CompareTag(GameTags.Coin)) continue;

                        var coinScript = obj.GetComponent<Script.Game.Item.Coin.PhysicalItem>();
                        if (coinScript != null && coinScript.droppedByAnimal && 
                            coinScript.denizensPickupAble && coinScript.IsAvailable())
                        {
                            float dist = Vector3.Distance(ctx.Transform.position, obj.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearestCoin = obj;
                            }
                        }
                    }

                    if (nearestCoin != null)
                    {
                        _huntingTarget = nearestCoin.transform.position;
                        _hasTarget = true;
                        archerJob.SetMoveTarget(_huntingTarget);
                        
                        if (ctx.Animator != null && ctx.Animator.runtimeAnimatorController != null)
                            ctx.Animator.Play("archerMove");
                        return true;
                    }
                }
            }
            return false;
        }

        private void HandleRandomHunting(DenizenContext ctx, ArcherJob archerJob)
        {
            if (!_hasTarget && Time.time >= _nextReevaluate)
            {
                if (ctx.GuardManager != null)
                {
                    var bounds = WorldUtility.GetHuntingBounds(ctx.GuardManager, ctx.Status.isLeftFlank);
                    float randomX = Random.Range(bounds.start, bounds.end);
                    _huntingTarget = new Vector3(randomX, ctx.Transform.position.y, 0f);
                }
                else
                {
                    float randomX = Script.Utility.GetRandomHuntingPos(); 
                    _huntingTarget = new Vector3(randomX, ctx.Transform.position.y, 0f);
                }
                
                _hasTarget = true;
                archerJob.SetMoveTarget(_huntingTarget);
                
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
