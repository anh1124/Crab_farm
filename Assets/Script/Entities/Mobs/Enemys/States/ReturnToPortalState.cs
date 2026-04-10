using Script.Game.Enemy.Core;
using UnityEngine;
using System.Linq;
using Script.Utils;

namespace Script.Game.Enemy.States
{
    /// <summary>
    /// Trạng thái rút lui về Portal ban Đêm.
    /// CHỈ XỬ LÝ: Radar cản đường và logic Despawn khi sát đích.
    /// </summary>
    public class ReturnToPortalState : EnemyState
    {
        protected override bool AllowMovementInState => true;
        public override void Enter(EnemyContext ctx) { }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            base.UpdateBehavior(ctx); // Chạy base để đồng bộ _moveTarget

            if (ctx.Status == null) return;

            // 1. Kiểm tra Portal mục tiêu (Đích đến ban đêm)
            var portal = ctx.HomePortal;
            if (portal == null || !portal.activeInHierarchy)
            {
                // Tự tìm lại portal theo vị trí hiện tại để tránh kẹt state khi portal reference bị null.
                var portalMgr = Script.Manager.GamePlayManager.EnemyPortalManager.Instance;
                var townCenter = Script.Utility.GetTownCenter();
                if (portalMgr != null && townCenter != null)
                {
                    portal = ctx.Transform.position.x > townCenter.transform.position.x ? portalMgr.portalR : portalMgr.portalL;
                    ctx.HomePortal = portal;
                }
            }

            if (portal == null || !portal.activeInHierarchy)
            {
                _moveTarget = null;
                ctx.Controller.ChangeState(ctx.Controller.IdleStateInstance); 
                return;
            }

            // Gán đích lùi về cho Base và Context Inspector
            _moveTarget = portal;
            ctx.CurrentTarget = portal;

            // kiểm tra kết thúc hành trình
            if (CheckDespawnCondition(ctx)) return;

            // 2. [OBSTACLE DETECTION] Quét Radar cản đường rút
            var detected = (ctx.RaycastHelper != null) ? ctx.RaycastHelper.GetListHitObjects() : null;
            
            if (detected != null && detected.Any(obj => obj != null && TargetLists.EnemyRetreatTargets.Any(tag => obj.CompareTag(tag))))
            {
                ctx.Controller.ChangeState(ctx.Controller.AttackStateInstance);
                return;
            }
        }

        /// <summary>
        /// Logic chuyên biệt để kiểm tra xem quái vật đã bước chân vào portal chưa.
        /// </summary>
        private bool CheckDespawnCondition(EnemyContext ctx)
        {
            if (_moveTarget == null) return false;

            float xDistance = Mathf.Abs(ctx.Transform.position.x - _moveTarget.transform.position.x);
            
            // Nới điều kiện một chút để tránh kẹt do va chạm/dao động quanh portal.
            if (xDistance < 1.0f)
            {
                ctx.Controller.Despawn(); 
                ctx.ReturnToPoolAction?.Invoke(ctx.Self);
                return true; 
            }

            return false;
        }

        public override void Exit(EnemyContext ctx)
        {
        }
    }
}
