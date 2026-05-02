using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager.WorldManager;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherTacticalHandler : DenizenModule
    {
        private ArcherJob _job;

        public ArcherTacticalHandler(DenizenContext context, ArcherJob job) : base(context)
        {
            _job = job;
        }

        public void OnGuardManagerStatusChanged(bool isAttack)
        {
            if (!isAttack)
            {
                // Báo động kết thúc → reset ignoreGuard, về chế độ ban ngày
                _job.SetIgnoreGuard(false);
                _job.HandleDayBreaks();
                return;
            }

            // Báo động mới (wall thay đổi)
            if (_job.IgnoreGuardManagerCommand && (_job.CurrentState == _job.AttackState || _job.CurrentState == _job.HoldWallState))
            {
                bool stillValid = _job.CanAttackFromHere();
                if (stillValid)
                {
                    if (Context.Status.debugDenizen) Debug.Log($"<color=cyan>[ArcherJob]</color> {Context.gameObject.name} IgnoreGuard: Vị trí vẫn hợp lệ sau khi tường thay đổi. Tiếp tục chiến đấu.");
                    return; // Tiếp tục, không rút lui
                }
                else
                {
                    if (Context.Status.debugDenizen) Debug.Log($"<color=orange>[ArcherJob]</color> {Context.gameObject.name} IgnoreGuard: Vị trí không còn hợp lệ. Rút lui!");
                    _job.SetIgnoreGuard(false);
                }
            }

            ExecuteDefensiveOrder();
        }

        public void ForceRetreat(GameObject safeWall)
        {
            if (_job.IgnoreGuardManagerCommand && Context.Status.currentTarget != null)
            {
                bool stillValid = _job.CanAttackFromHere();
                if (stillValid)
                {
                    if (Context.Status.debugDenizen) Debug.Log($"<color=cyan>[ArcherJob]</color> {Context.gameObject.name} ForceRetreat bị từ chối: Đang giao chiến tại vị trí hợp lệ.");
                    return; 
                }
                _job.SetIgnoreGuard(false);
            }

            // [LAST STAND] — Không còn wall nào → tử thủ Plaza
            if (safeWall == null)
            {
                if (Context.Status.debugDenizen) Debug.Log($"<color=red>[ArcherJob]</color> {Context.gameObject.name} KHÔNG CÒN TƯỜNG. Tử thủ tại Plaza!");
                _job.ChangeState(_job.ReturnToBaseState);
                return;
            }

            // [RETREAT] — Gán wall mới và rút lui
            if (Context.Status != null)
                Context.Status.assignedWall = safeWall;
            
            _job.RetreatState.SetRetreatTarget(safeWall);
            _job.ChangeState(_job.RetreatState);
        }

        public void ExecuteDefensiveOrder()
        {
            // Kiểm tra an toàn dựa trên vị trí hiện tại
            bool isSafe = WorldUtility.IsInKingdomSafeZone(Context.transform.position.x) || WorldUtility.IsInPlazaZone(Context.transform.position.x);
            
            // Nếu không an toàn -> Bắt buộc phải tìm đường về base
            if (!isSafe)
            {
                if (_job.CurrentState != _job.ReturnToBaseState && _job.CurrentState != _job.RetreatState)
                {
                    _job.ChangeState(_job.ReturnToBaseState);
                }
                return;
            }

            // Nếu đã an toàn, kiểm tra xem có nên chuyển sang HoldWall không
            // Ưu tiên 1: Đang Attack thì cứ để nó bắn tiếp
            if (_job.CurrentState == _job.AttackState) return;

            // Ưu tiên 2: Nếu đã ở điểm Slot (ReturnToBase xong) hoặc đang Retreat dở thì chuyển sang HoldWall
            if (_job.CurrentState == _job.ReturnToBaseState || (_job.CurrentState != _job.RetreatState && _job.CurrentState != _job.HoldWallState))
            {
                _job.ChangeState(_job.HoldWallState);
            }
        }
    }
}
