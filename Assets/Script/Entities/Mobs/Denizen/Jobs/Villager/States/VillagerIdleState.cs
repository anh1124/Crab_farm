using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerIdleState : DenizenState
    {
        private readonly VillagerJob _job;
        private float _nextRandomWalkTime;
        private Vector3 _currentWalkTarget;
        private float _lastToolCheckTime = -999f;
        private const float TOOL_CHECK_INTERVAL = 3f;

        public VillagerIdleState(VillagerJob job)
        {
            _job = job;
        }

        public override void Enter(DenizenContext ctx)
        {
            _nextRandomWalkTime = Time.time + Random.Range(2f, 5f);
            
            // Ép quét Tool ngay khi vừa vào rảnh rỗi (giống legacy start)
            _lastToolCheckTime = -999f;
            
            Animator anim = _job.GetAnimator();
            if (anim != null) anim.Play("idle");
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Status.isInDanger)
            {
                _job.ChangeState(_job.FearState);
                return;
            }

            if (ctx.Status.currentTool != null)
            {
                _job.ChangeState(_job.RunToolState);
                return;
            }

            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                _job.ChangeState(_job.ToTownState);
                return;
            }

            // [FIX] Liên tục hỏi ToolManager mỗi 3 giây nếu rảnh rỗi
            if (Time.time - _lastToolCheckTime >= TOOL_CHECK_INTERVAL)
            {
                _lastToolCheckTime = Time.time;
                if (Script.Manager.MobsManager.ToolManager.Instance != null && 
                    Script.Manager.MobsManager.ToolManager.Instance.GetTotalUnservedTools() > 0)
                {
                    Script.Manager.MobsManager.ToolManager.Instance.OnVillagerBecameIdle(_job);
                }
            }

            if (!ctx.Movement.IsMoving && Time.time >= _nextRandomWalkTime)
            {
                // Sử dụng Plaza Position thay vì wander tại chỗ
                float randomX = Utility.GetRandomPlazaPos();
                _currentWalkTarget = new Vector3(randomX, ctx.Transform.position.y, 0f);
                
                ctx.Movement.FaceTarget(_currentWalkTarget);
                ctx.Movement.MoveTo(_currentWalkTarget, ctx.Status.WalkSpeed);
                _nextRandomWalkTime = Time.time + Random.Range(5f, 12f);
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

        public override string GetDebugInfo(DenizenContext ctx)
        {
            return $"NextWalkIn: {(_nextRandomWalkTime - Time.time):F1}s | " +
                   $"IsMoving: {ctx.Movement.IsMoving} | " +
                   $"Target: {_currentWalkTarget} | " +
                   $"Dist: {Vector3.Distance(ctx.Transform.position, _currentWalkTarget):F1}m";
        }
    }
}
