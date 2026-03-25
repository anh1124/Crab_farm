using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerFearState : DenizenState
    {
        private readonly VillagerJob _job;
        private float _fearEndTime;
        private Vector3 _fleeTarget;

        // Bỏ biến hardcode FEAR_DURATION

        public VillagerFearState(VillagerJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            _fearEndTime = Time.time + ctx.Status.maxFearDuration;
            ctx.Status.isInDanger = true;

            // Tìm điểm an toàn ngẫu nhiên trong Plaza thay vì chạy vào giữa bản đồ
            float randomPlazaX = Utility.GetRandomPlazaPos();
            _fleeTarget = new Vector3(randomPlazaX, ctx.Transform.position.y, 0f);

            ctx.Movement.FaceTarget(_fleeTarget);
            ctx.Movement.MoveTo(_fleeTarget, ctx.Status.RunSpeed);

            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("run");
        }

        // Gia hạn thời gian sợ khi vẫn thấy enemy
        public void RefreshFear(DenizenContext ctx)
        {
            _fearEndTime = Time.time + ctx.Status.maxFearDuration;
        }

        public override void Update(DenizenContext ctx)
        {
            // Hết hoảng sợ
            if (Time.time >= _fearEndTime)
            {
                ctx.Status.isInDanger = false;
                _job.TransitionToIdle();
                return;
            }

            // Nếu đến điểm flee rồi mà vẫn sợ -> chọn điểm Plaza khác
            if (ctx.Movement.HasReachedTarget)
            {
                float randomPlazaX = Utility.GetRandomPlazaPos();
                _fleeTarget = new Vector3(randomPlazaX, ctx.Transform.position.y, 0f);
                ctx.Movement.FaceTarget(_fleeTarget);
                ctx.Movement.MoveTo(_fleeTarget, ctx.Status.RunSpeed);
            }
        }

        public override void FrameUpdate(DenizenContext ctx)
        {
            Animator anim = _job.GetAnimator();
            if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("run"))
                anim.Play("run");
        }

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"FearTime: {(_fearEndTime - Time.time):F1}s | FleeTarget: {_fleeTarget}";
        }
    }
}
