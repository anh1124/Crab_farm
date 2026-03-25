using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantIdleState : DenizenState
    {
        private readonly VagrantJob _job;
        private float _nextRandomWalkTime;

        public VagrantIdleState(VagrantJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            ctx.Movement.StopMovement();
            _nextRandomWalkTime = Time.time + Random.Range(2f, 5f);

            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("idle");
        }

        public override void Update(DenizenContext ctx)
        {
            // 1. Check Fear Pending
            if (ctx.Status.isFearPending)
            {
                ctx.Status.isFearPending = false;
                _job.ChangeState(_job.FearState);
                return;
            }

            // 2. Check Coin Target
            if (ctx.Status.coinTarget != null)
            {
                _job.ChangeState(_job.GetCoinState);
                return;
            }

            // 3. Random Walk / Flip logic (port từ legacy VagrantBehavior)
            if (!ctx.Movement.IsMoving && Time.time >= _nextRandomWalkTime)
            {
                float maxDist = ctx.Status.maxIdleWalkDistance > 0 ? ctx.Status.maxIdleWalkDistance : 3f;

                // 70% đi lang thang quanh startPosition - 30% lật mặt đứng yên (legacy RandomFlipRoutine)
                if (Random.value < 0.7f)
                {
                    float randomOffset = Random.Range(-maxDist, maxDist);
                    float targetX = Mathf.Clamp(
                        ctx.Status.startPosition.x + randomOffset,
                        ctx.Status.startPosition.x - maxDist,
                        ctx.Status.startPosition.x + maxDist
                    );
                    Vector3 target = new Vector3(targetX, ctx.Transform.position.y, ctx.Transform.position.z);
                    ctx.Movement.FaceTarget(target);
                    ctx.Movement.MoveTo(target, ctx.Status.WalkSpeed);
                    _nextRandomWalkTime = Time.time + Random.Range(4f, 8f);
                }
                else
                {
                    // Đứng yên - lật mặt ngẫu nhiên
                    Vector3 scale = ctx.Transform.localScale;
                    scale.x *= -1;
                    ctx.Transform.localScale = scale;
                    _nextRandomWalkTime = Time.time + Random.Range(2f, 5f);
                }
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
