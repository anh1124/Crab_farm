using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantFearState : DenizenState
    {
        private readonly VagrantJob _job;

        // Fear duration giờ đã lấy theo ctx.Status.maxFearDuration thay vì hardcode.
        private float _fearEndTime;

        public VagrantFearState(VagrantJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            _fearEndTime = Time.time + ctx.Status.maxFearDuration;
            ctx.Movement.StopMovement();
            // Reset coin target để không quay lại GetCoin ngay sau khi hết sợ
            ctx.Status.coinTarget = null;

            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("fear");
        }

        // Mỗi lần OnObjectsDetected gặp enemy, gia hạn thêm thời gian sợ
        public void RefreshFear(DenizenContext ctx)
        {
            _fearEndTime = Time.time + ctx.Status.maxFearDuration;
        }

        public override void Update(DenizenContext ctx)
        {
            if (Time.time >= _fearEndTime)
            {
                _job.ChangeState(_job.IdleState);
            }
        }
    }
}
