using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerIdleState : DenizenState
    {
        private float _nextRandomWalkTime;
        private Vector3 _walkTarget;
        private bool _hasTarget;
        private float _lastToolCheck;
        private const float TOOL_CHECK_INTERVAL = 3f;

        public override void Enter(DenizenContext ctx)
        {
            _nextRandomWalkTime = Time.time + Random.Range(2f, 5f);
            _lastToolCheck = -999f;
            _hasTarget = false;
            
            if (ctx.Animator != null) 
                ctx.Animator.Play("idle");

            if (ctx.Movement != null)
                ctx.Movement.Stop();
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            var vSoul = ctx.VillagerJob;
            if (vSoul == null) return;

            // Kiểm tra các điều kiện chuyển trạng thái khẩn cấp
            if (ctx.Status.isFearPending)
            {
                vSoul.ChangeState(vSoul.FearState);
                return;
            }

            if (ctx.Status.currentTool != null)
            {
                vSoul.ChangeState(vSoul.RunToolState);
                return;
            }
            
            // 3. Đang đêm → về thành (ToTownState) — CHỈ khi chưa ở gần Plaza
            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                // Nếu chưa ở gần tâm thành phố (Plaza), mới phải đi về
                if (!vSoul.HasReachedPosition(Vector3.zero, 4.0f))
                {
                    vSoul.ChangeState(vSoul.ToTownState);
                    return;
                }
            }

            // Kiểm tra xem có công cụ nào mới không (định kỳ)
            if (Time.time - _lastToolCheck >= TOOL_CHECK_INTERVAL)
            {
                _lastToolCheck = Time.time;
                if (Script.Manager.MobsManager.ToolManager.Instance?.GetTotalUnservedTools() > 0)
                {
                    Script.Manager.MobsManager.ToolManager.Instance.OnVillagerBecameIdle(vSoul);
                }
            }

            // Logic đi dạo ngẫu nhiên
            if (!_hasTarget && Time.time >= _nextRandomWalkTime)
            {
                _walkTarget = new Vector3(Script.Utility.GetRandomPlazaPos(), ctx.Transform.position.y, 0f);
                _hasTarget = true;
                vSoul.SetMoveTarget(_walkTarget);
            }
        }

        public override void Update(DenizenContext ctx)
        {
            if (ctx.Animator == null) return;

            if (_hasTarget)
            {
                // Nếu đã đến đích thì dừng lại và chờ lần đi dạo tiếp theo
                if (ctx.VillagerJob.HasReachedPosition(_walkTarget))
                {
                    _hasTarget = false;
                    _nextRandomWalkTime = Time.time + Random.Range(5f, 12f);
                    ctx.Movement?.Stop();
                    ctx.Animator.Play("idle");
                }
                else
                {
                    // Luôn đảm bảo chơi walk khi đang di chuyển
                    if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                        ctx.Animator.Play("walk");
                }
            }
            else
            {
                // Luôn đảm bảo chơi idle khi đứng yên
                if (!ctx.Animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
                    ctx.Animator.Play("idle");
            }
        }
    }
}
