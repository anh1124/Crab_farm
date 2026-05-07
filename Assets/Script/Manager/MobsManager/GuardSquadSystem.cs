using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;
using Script.Manager.Building;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.Denizen.Jobs.Archer;

namespace Script.Game.Mobs.MobsManager
{
    public class GuardSquadSystem : MonoBehaviour
    {
        [Header("Archer Management")]
        public List<GameObject> leftFlankArchers = new List<GameObject>();
        public List<GameObject> rightFlankArchers = new List<GameObject>();

        [Header("Defense Slot Settings")]
        public int archerSlotCount = 7;
        public float archerSlotSpacing = 1.2f;
        public float defaultSlotOffset = 7.0f; 
        public float rowDepthOffset = 0.3f;

        private GuardManager guardManager;
        private BuildingManager _bm;
        private BuildingManager BM => _bm != null ? _bm : (_bm = FindObjectOfType<BuildingManager>());
        
        // [NEW] Flag bảo vệ chống gọi chồng RebalanceSquads
        private bool _isRebalancing = false;

        private void Start()
        {
            guardManager = FindObjectOfType<GuardManager>();
        }

        public void RegisterArcher(GameObject archer)
        {
            if (archer == null) return;
            if (leftFlankArchers.Contains(archer) || rightFlankArchers.Contains(archer)) return;

            var archerStatus = archer.GetComponent<DenizenStatus>();
            if (archerStatus == null) return;

            if (leftFlankArchers.Count <= rightFlankArchers.Count)
            {
                leftFlankArchers.Add(archer);
                archerStatus.isLeftFlank = true;
                if (guardManager) guardManager.DebugLog($"Registered {archer.name} to LEFT flank");
            }
            else
            {
                rightFlankArchers.Add(archer);
                archerStatus.isLeftFlank = false;
                if (guardManager) guardManager.DebugLog($"Registered {archer.name} to RIGHT flank");
            }

            if (guardManager && guardManager.Alarm.isEnemyAttack)
            {
                 archerStatus.archerState = DenizenStatus.ArcherState.ReturningToBase;
            }
        }

        public void UnregisterArcher(GameObject archer)
        {
            if (archer == null) return;

            bool wasRemoved = false;
            if (leftFlankArchers.Contains(archer))
            {
                leftFlankArchers.Remove(archer);
                wasRemoved = true;
            }
            else if (rightFlankArchers.Contains(archer))
            {
                rightFlankArchers.Remove(archer);
                wasRemoved = true;
            }

            if (wasRemoved)
            {
                RebalanceSquads();
            }
        }

        /// <summary>
        /// [NEW] Cân bằng đội hình archer dựa trên vị trí thực tế.
        /// Luôn thực hiện cân bằng, sắp xếp lại toàn bộ đội hình theo tọa độ X.
        /// Có bảo vệ chống gọi chồng (re-entry protection).
        /// </summary>
        public void RebalanceSquads()
        {
            if (_isRebalancing) return;
            _isRebalancing = true;
            
            try
            {
                RebalanceSquadsInternal();
            }
            finally
            {
                _isRebalancing = false;
            }
        }
        
        /// <summary>
        /// [NEW] Logic cân bằng triệt để - gom tất cả archer, sắp xếp theo X, phân bổ lại flank.
        /// </summary>
        private void RebalanceSquadsInternal()
        {
            leftFlankArchers.RemoveAll(x => x == null);
            rightFlankArchers.RemoveAll(x => x == null);

            int diff = leftFlankArchers.Count - rightFlankArchers.Count;
            if (Mathf.Abs(diff) <= 1) return; // Đã cân bằng, không cần làm gì

            if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] Rebalancing squads... (Diff: {diff})");

            // [STEP 1] Gom tất cả archer từ cả 2 flank
            var allArchers = new List<GameObject>();
            allArchers.AddRange(leftFlankArchers);
            allArchers.AddRange(rightFlankArchers);
            
            // [STEP 2] Sắp xếp theo tọa độ X tăng dần (từ trái sang phải)
            allArchers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            
            // [STEP 3] Tính toán phân bổ - ưu tiên trái khi số lượng lẻ
            int total = allArchers.Count;
            int targetLeft = (total + 1) / 2; // Ví dụ: 5 archer → 3 left, 2 right
            
            if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] Total: {total}, Target Left: {targetLeft}, Right: {total - targetLeft}");

            // [STEP 4] Xóa sạch list cũ
            leftFlankArchers.Clear();
            rightFlankArchers.Clear();
            
            // [STEP 5] Gán lại flank theo thứ tự từ trái sang phải
            for (int i = 0; i < total; i++)
            {
                var archer = allArchers[i];
                if (archer == null) continue;
                
                bool toLeft = i < targetLeft;
                var status = archer.GetComponent<DenizenStatus>();
                if (status == null) continue;
                
                // Gán vào list
                if (toLeft)
                    leftFlankArchers.Add(archer);
                else
                    rightFlankArchers.Add(archer);
                
                // Cập nhật status
                bool changed = (status.isLeftFlank != toLeft);
                status.isLeftFlank = toLeft;
                if (guardManager && guardManager.Wall)
                {
                    status.assignedWall = toLeft ? 
                        guardManager.Wall.currentLeftWall : 
                        guardManager.Wall.currentRightWall;
                }
                
                // Force archer quay về base để nhận slot mới dựa trên index
                ForceArcherReturnToBase(archer);
                
                if (guardManager && changed)
                    guardManager.DebugLog($"[GuardSquadSystem] {archer.name} chuyển từ {(toLeft ? "RIGHT" : "LEFT")} sang {(toLeft ? "LEFT" : "RIGHT")}");
            }
            
            if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] Hoàn tất cân bằng. Left: {leftFlankArchers.Count}, Right: {rightFlankArchers.Count}");
        }
        
        /// <summary>
        /// [NEW] Force archer quay về base ngay lập tức.
        /// Hủy di chuyển cũ, set state ReturningToBase để archer tự tính slot mới.
        /// </summary>
        private void ForceArcherReturnToBase(GameObject archer)
        {
            if (archer == null) return;
            
            var status = archer.GetComponent<DenizenStatus>();
            if (status == null) return;
            
            // Set state để archer chuyển sang ReturningToBaseState
            status.archerState = DenizenStatus.ArcherState.ReturningToBase;
            
            // Hủy di chuyển hiện tại (nếu có) để nhận target mới
            var movement = archer.GetComponent<DenizenMovement>();
            movement?.Stop();
            
            // Force state machine chuyển state (nếu cần)
            var roleCtrl = archer.GetComponent<DenizenRoleController>();
            var archerJob = roleCtrl?.JobBrain as ArcherJob;
            if (archerJob != null)
            {
                // Chỉ chuyển nếu đang ở state có thể di chuyển
                if (status.currentStateName == "ArcherIdleState" ||
                    status.currentStateName == "ArcherHoldWallState" ||
                    status.currentStateName == "ArcherReturnToBaseState" ||
                    status.currentStateName == "ArcherMoveState")
                {
                    archerJob.ChangeState(archerJob.ReturnToBaseState);
                }
            }
        }
        
        /// <summary>
        /// Tính center X dựa trên vị trí 2 wall hiện tại hoặc town center
        /// </summary>
        private float GetCenterX()
        {
            // Ưu tiên dùng vị trí giữa 2 wall
            if (guardManager != null && guardManager.Wall != null)
            {
                GameObject leftWall = guardManager.Wall.currentLeftWall;
                GameObject rightWall = guardManager.Wall.currentRightWall;
                
                if (leftWall != null && rightWall != null)
                {
                    return (leftWall.transform.position.x + rightWall.transform.position.x) / 2f;
                }
                else if (leftWall != null)
                {
                    return leftWall.transform.position.x;
                }
                else if (rightWall != null)
                {
                    return rightWall.transform.position.x;
                }
            }
            
            // Fallback: dùng town center hoặc 0
            if (BM != null && BM.currentTownCenter != null)
            {
                return BM.currentTownCenter.transform.position.x;
            }
            
            return 0f;
        }

        public void NotifyAllArchersValues(bool isAttack)
        {
            List<GameObject> allArchers = new List<GameObject>();
            allArchers.AddRange(leftFlankArchers);
            allArchers.AddRange(rightFlankArchers);
            
            foreach(var archer in allArchers)
            {
                if(archer == null) continue;
                var roleCtrl = archer.GetComponent<DenizenRoleController>();
                if(roleCtrl != null && roleCtrl.JobBrain != null)
                {
                    roleCtrl.JobBrain.OnGuardManagerStatusChanged(isAttack);
                }
            }
        }

        /// <summary>
        /// [NEW] Thông báo archer cập nhật vị trí phòng thủ.
        /// </summary>
        /// <param name="forceReturn">Nếu true, force tất cả archer quay về base ngay lập tức</param>
        public void NotifyArchersDefensivePositionChanged(GameObject leftWall, GameObject rightWall, bool forceReturn = false)
        {
            NotifyFlankArchers(leftFlankArchers, leftWall, forceReturn);
            NotifyFlankArchers(rightFlankArchers, rightWall, forceReturn);
        }

        private void NotifyFlankArchers(List<GameObject> archers, GameObject wall, bool forceReturn)
        {
            if (wall == null) return;
            foreach (var archer in archers)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<DenizenStatus>();
                if (status == null) continue;
                
                // Cập nhật wall cho tất cả archer trong flank này
                bool shouldUpdate = status.isLeftFlank == (archers == leftFlankArchers) ||
                                    status.assignedWall == null;
                
                if (shouldUpdate)
                {
                    status.assignedWall = wall;
                    
                    // Force quay về base nếu cần
                    if (forceReturn)
                    {
                        ForceArcherReturnToBase(archer);
                        if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] {archer.name} force quay về {wall.name}");
                    }
                    else if (guardManager)
                    {
                        guardManager.DebugLog($"[GuardSquadSystem] {archer.name} cập nhật wall sang {wall.name}");
                    }
                }
            }
        }

        public void RetreatArchersOnFlank(bool isLeftSide, GameObject safeWall)
        {
            List<GameObject> flank = isLeftSide ? leftFlankArchers : rightFlankArchers;
            foreach (var archer in flank)
            {
                if (archer == null) continue;
                var roleCtrl = archer.GetComponent<DenizenRoleController>();
                if (roleCtrl != null && roleCtrl.JobBrain != null)
                {
                    // Luôn gọi ForceRetreat, kể cả khi safeWall == null (Last Stand)
                    roleCtrl.JobBrain.ForceRetreat(safeWall);
                    if (safeWall == null && guardManager)
                        guardManager.DebugLog($"[GuardSquadSystem] {archer.name} không còn nơi để lùi → Tử thủ tại chỗ!");
                }
            }
        }

        public Vector3 GetDefensePositionForArcher(GameObject archer)
        {
            if (archer == null) return Vector3.zero;

            var status = archer.GetComponent<DenizenStatus>();
            if (status == null) return archer.transform.position;

            bool isLeft = status.isLeftFlank;
            List<GameObject> flankList = isLeft ? leftFlankArchers : rightFlankArchers;
            GameObject wall = (guardManager && guardManager.Wall) ? guardManager.Wall.GetCurrentDefensiveWall(isLeft) : null;
            
            int index = flankList.IndexOf(archer);
            if (index == -1)
            {
                // [SAFETY] — Archer không nằm trong danh sách Flank? 
                // Cho nó chạy về Plaza thay vì đứng im tại chỗ.
                var tc = (BM != null) ? BM.currentTownCenter : null;
                return tc != null ? tc.transform.position : Vector3.zero;
            }

            int slotInRow = index % archerSlotCount;
            int rowIndex = index / archerSlotCount;

            if (wall == null)
            {
                // [CRITICAL FIX] — Luôn ưu tiên rút về Plaza nếu không có tường.
                // Tránh trả về archer.transform.position vì sẽ làm AI đứng im tại chỗ.
                var townCenter = (BM != null) ? BM.currentTownCenter : null;
                
                // Cố gắng tìm TownCenter nếu biến cache bị null
                if (townCenter == null) townCenter = GameObject.FindWithTag(Script.Utils.GameTags.TownCenter);

                if (townCenter != null)
                {
                    Vector3 tcPos = townCenter.transform.position;
                    float plazaRadius = 5f;
                    var tcBehavior = townCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                    if (tcBehavior != null) plazaRadius = tcBehavior.plazaDistance;

                    float plazaHorizontalOffsetInRow = slotInRow * archerSlotSpacing;
                    float plazaTotalRowDepth = rowIndex * rowDepthOffset;
                    float requestedOffset = defaultSlotOffset + plazaHorizontalOffsetInRow + plazaTotalRowDepth;

                    float maxSafeOffset = Mathf.Max(0.5f, plazaRadius - 0.5f);
                    float clampedOffset = Mathf.Min(requestedOffset, maxSafeOffset);
                    float boundaryX = tcPos.x + (isLeft ? -plazaRadius : plazaRadius);
                    float offsetTowardCenter = isLeft ? clampedOffset : -clampedOffset;
                    
                    return new Vector3(boundaryX + offsetTowardCenter, tcPos.y, archer.transform.position.z);
                }

                // Hard fallback: Về gốc tọa độ (thường là TownCenter) nếu mất dấu hoàn toàn, 
                // TUYỆT ĐỐI không trả về vị trí hiện tại của Archer.
                return Vector3.zero; 
            }

            float direction = isLeft ? 1f : -1f;
            
            float horizontalOffsetInRow = slotInRow * archerSlotSpacing;
            float totalRowDepth = rowIndex * rowDepthOffset;
            float totalOffset = defaultSlotOffset + horizontalOffsetInRow + totalRowDepth;

            return new Vector3(
                wall.transform.position.x + direction * totalOffset,
                wall.transform.position.y,
                archer.transform.position.z
            );
        }

        private void OnDestroy()
        {
            leftFlankArchers.Clear();
            rightFlankArchers.Clear();
        }
    }
}
