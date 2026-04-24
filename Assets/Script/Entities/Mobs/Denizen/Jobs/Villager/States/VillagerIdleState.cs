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
        private const float TOOL_CHECK_INTERVAL = 1.5f;

        public override void Enter(DenizenContext ctx)
        {
            _nextRandomWalkTime = Time.time + Random.Range(2f, 5f);
            _lastToolCheck = -999f; // Force check ngay lập tức ở lần UpdateBehavior đầu tiên
            _hasTarget = false;
            
            if (ctx.Animator != null) 
                ctx.Animator.Play("idle");

            if (ctx.Movement != null)
                ctx.Movement.Stop();
                
            // [FIX] Khi vừa chuyển từ Vagrant -> Villager, check tool ngay lập tức
            // Không đợi interval để tránh trường hợp idle không làm gì
            TryCheckToolsImmediate(ctx);
        }
        
        /// <summary>
        /// Thử check và assign tool ngay lập tức khi vào IdleState
        /// </summary>
        private void TryCheckToolsImmediate(DenizenContext ctx)
        {
            var toolManager = Script.Manager.MobsManager.ToolManager.Instance;
            var villagerJob = ctx.VillagerJob;
            
            if (toolManager != null && villagerJob != null && toolManager.GetTotalUnservedTools() > 0)
            {
                toolManager.OnVillagerBecameIdle(villagerJob);
                
                // Nếu đã chuyển sang RunToolState, cập nhật lại _lastToolCheck
                if (villagerJob.CurrentState is VillagerRunToolState)
                {
                    _lastToolCheck = Time.time; // Reset để tránh check lại ngay
                    
                    if (ctx.Status?.debugDenizen == true)
                        Debug.Log($"[VillagerIdleState] {ctx.GameObject.name}: Ngay khi vào Idle đã được assign tool!");
                }
            }
        }

        public override void UpdateBehavior(DenizenContext ctx)
        {
            if (ctx.IsSkillCasting) return;
            var villagerJob = ctx.VillagerJob;
            if (villagerJob == null) return;

            // Kiểm tra các điều kiện chuyển trạng thái khẩn cấp
            if (ctx.Status.isFearPending)
            {
                villagerJob.ChangeState(villagerJob.FearState);
                return;
            }

            if (ctx.Status.currentTool != null)
            {
                villagerJob.ChangeState(villagerJob.RunToolState);
                return;
            }
            
            // 3. Đang đêm → về thành (ToTownState) — CHỈ khi chưa ở gần Plaza
            if (ctx.TimeManager != null && !ctx.TimeManager.IsDaytime)
            {
                // Nếu chưa ở gần tâm thành phố (Plaza), mới phải đi về
                if (!villagerJob.HasReachedPosition(Vector3.zero, 4.0f))
                {
                    villagerJob.ChangeState(villagerJob.ToTownState);
                    return;
                }
            }

            // Kiểm tra xem có công cụ nào mới không (định kỳ)
            if (Time.time - _lastToolCheck >= TOOL_CHECK_INTERVAL)
            {
                _lastToolCheck = Time.time;
                
                var toolManager = Script.Manager.MobsManager.ToolManager.Instance;
                if (toolManager != null)
                {
                    int toolCount = toolManager.GetTotalUnservedTools();
                    
                    if (toolCount > 0)
                    {
                        // Có tool available, thử assign
                        toolManager.OnVillagerBecameIdle(villagerJob);
                        
                        // Nếu đã đổi state (sang RunToolState), thoát UpdateBehavior ngay
                        if (villagerJob.CurrentState != this) return;
                        
                        // RACE CONDITION: Nếu vẫn còn ở IdleState tức là tool bị villager khác lấy mất
                        // Thử lại ngay lần nữa (không đợi interval)
                        Debug.Log($"[VillagerIdleState] {ctx.GameObject.name}: Tool bị mất bởi villager khác, thử lại ngay...");
                        
                        // Kiểm tra lại có tool không
                        if (toolManager.GetTotalUnservedTools() > 0)
                        {
                            toolManager.OnVillagerBecameIdle(villagerJob);
                            if (villagerJob.CurrentState != this) return;
                        }
                    }
                    else if (ctx.Status.debugDenizen)
                    {
                        Debug.Log($"[VillagerIdleState] {ctx.GameObject.name}: Không có tool nào available");
                    }
                }
            }

            // Logic đi dạo ngẫu nhiên
            if (!_hasTarget && Time.time >= _nextRandomWalkTime)
            {
                _walkTarget = new Vector3(Script.Utility.GetRandomPlazaPos(), ctx.Transform.position.y, 0f);
                _hasTarget = true;
                villagerJob.SetMoveTarget(_walkTarget);
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
