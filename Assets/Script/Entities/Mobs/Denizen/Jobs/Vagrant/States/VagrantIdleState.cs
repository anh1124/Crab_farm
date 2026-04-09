using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantIdleState : DenizenState
    {
        private float _nextWalkTime;
        private Vector3 _target;
        private bool _hasTarget;

        public override void Enter(DenizenContext ctx)
        {
            _nextWalkTime = Time.time + Random.Range(2f, 5f);
            _hasTarget = false;
            
            if (ctx.Animator != null) 
                ctx.Animator.Play("idle");

            if (ctx.Movement != null)
                ctx.Movement.Stop();
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            var vaSoul = ctx.VagrantJob;
            if (vaSoul == null) return;

            // Nếu gặp nguy hiểm hoặc kích hoạt sợ hãi
            if (ctx.Status.isFearPending)
            {
                vaSoul.ChangeState(vaSoul.FearState);
                return;
            }

            // Nếu thấy đồng xu xung quanh (và nó còn đang hoạt động)
            if (ctx.Status.coinTarget != null && ctx.Status.coinTarget.activeInHierarchy)
            {
                vaSoul.ChangeState(vaSoul.GetCoinState);
                return;
            }

            // Logic đi dạo thẩn thờ
            if (!_hasTarget && Time.time >= _nextWalkTime)
            {
                // Xác định phía hiện tại (Trái hay Phải) so với TownCenter để tránh đi xuyên vương quốc
                var townCenter = Utility.GetTownCenter();
                float townCenterX = (townCenter != null) ? townCenter.transform.position.x : 0f;
                bool isRight = ctx.Transform.position.x > townCenterX;

                // Lấy vị trí random ở phía hiện tại, bên ngoài tường thành (hoặc fallback nếu không có tường)
                _target = new Vector3(Utility.GetRandomOutsideSafePos(isRight, 5f, 15f, 30f).x, ctx.Transform.position.y, 0f);
                _hasTarget = true;
                vaSoul.SetMoveTarget(_target);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;

            if (_hasTarget)
            {
                if (ctx.VagrantJob.HasReachedPosition(_target))
                {
                    _hasTarget = false;
                    _nextWalkTime = Time.time + Random.Range(5f, 15f);
                    ctx.Movement?.Stop();
                    ctx.Animator.Play("idle");
                }
                else
                {
                    // Đảm bảo luôn play walk khi đang di chuyển
                    if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                        ctx.Animator.Play("walk");
                }
            }
            else
            {
                // Đảm bảo luôn play idle khi đứng yên
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
