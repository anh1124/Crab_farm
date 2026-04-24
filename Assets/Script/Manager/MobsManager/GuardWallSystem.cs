using System.Collections.Generic;
using UnityEngine;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager.WorldManager;
using Script.Manager.Building;
using Script.Manager.NatureManager;

namespace Script.Game.Mobs.MobsManager
{
    public class GuardWallSystem : MonoBehaviour
    {
        [Header("Defensive Structures")]
        public GameObject currentLeftWall;
        public GameObject currentRightWall;

        private WallAndStumpManager wallManager;
        private GuardManager guardManager;

        private void Start()
        {
            guardManager = FindObjectOfType<GuardManager>();
            wallManager = WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                UpdateDefensiveWalls();
            }
        }

        public void UpdateDefensiveWalls()
        {
            if (wallManager == null) wallManager = WallAndStumpManager.Instance;
            if (wallManager == null) return;
            
            // Nếu đang trong combat, không tự động đổi wall
            if (guardManager != null && guardManager.Alarm.isEnemyAttack) return;

            GameObject newLeftWall = FindBestDefensiveWall(wallManager.leftWalls);
            GameObject newRightWall = FindBestDefensiveWall(wallManager.rightWalls);
            
            UpdateSpecificWall(ref currentLeftWall, newLeftWall, true);
            UpdateSpecificWall(ref currentRightWall, newRightWall, false);
            
            if (guardManager != null && guardManager.Squad != null)
            {
                // Cập nhật vị trí bình thường, không force return
                guardManager.Squad.NotifyArchersDefensivePositionChanged(currentLeftWall, currentRightWall, forceReturn: false);
            }
        }

        private GameObject FindBestDefensiveWall(List<GameObject> walls)
        {
            if (walls == null || walls.Count == 0) return null;

            int threshold = (wallManager != null) ? wallManager.criticalHPThreshold : 10;

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                GameObject wall = walls[i];
                if (wall == null) continue;
                if (!wall.CompareTag(Script.Utils.GameTags.Wall)) continue;

                var behavior = wall.GetComponent<WallBehavior>();
                if (behavior != null && behavior.Status.CurrentHp >= threshold)
                {
                    return wall;
                }
            }
            return null;
        }
        
        private void UpdateSpecificWall(ref GameObject currentWall, GameObject newWall, bool isLeft)
        {
            if (currentWall != newWall)
            {
                if (currentWall != null)
                {
                    var oldBehavior = currentWall.GetComponent<WallBehavior>();
                    if (oldBehavior != null) oldBehavior.OnCriticalHP -= HandleWallCritical;
                }

                currentWall = newWall;

                if (currentWall != null)
                {
                    var newBehavior = currentWall.GetComponent<WallBehavior>();
                    if (newBehavior != null) newBehavior.OnCriticalHP += HandleWallCritical;
                }
                
                if (guardManager != null) guardManager.DebugLog($"[GuardWallSystem] Updated {(isLeft ? "LEFT" : "RIGHT")} defensive wall to: {currentWall?.name ?? "NULL"}");
                
                // Notify RabbitBushManager that kingdom zone changed
                if (RabbitBushManager.Instance != null)
                {
                    RabbitBushManager.Instance.NotifyKingdomZoneChanged();
                }
            }
        }
        
        private void HandleWallCritical(GameObject wall)
        {
            if (wall == null) return;
            bool isLeftSide = (wall == currentLeftWall);
            NotifyWallCritical(wall, isLeftSide);
        }

        public void NotifyWallCritical(GameObject wall, bool isLeftSide)
        {
            if (wall == null || wallManager == null) return;
            if (wall != currentLeftWall && wall != currentRightWall) return;

            Debug.Log($"[GuardWallSystem] ⚠️ WALL CRITICAL: {wall.name} trên {(isLeftSide ? "LEFT" : "RIGHT")}. Tìm điểm lùi quân...");

            GameObject safeWall = FindSafeWallRecursively(wall);
            
            if (isLeftSide)
                UpdateSpecificWall(ref currentLeftWall, safeWall, true);
            else
                UpdateSpecificWall(ref currentRightWall, safeWall, false);

            if (guardManager != null && guardManager.Squad != null)
            {
                // Retreat archers ở side bị phá
                guardManager.Squad.RetreatArchersOnFlank(isLeftSide, safeWall);
                
                // [FIX] Thông báo cho TẤT CẢ archer (cả 2 bên) cập nhật vị trí phòng thủ với forceReturn=true
                // Archer đang chạy từ side khác sang cũng cần biết wall mới để cập nhật target
                GameObject otherSideWall = isLeftSide ? 
                    guardManager.Wall?.currentRightWall : 
                    guardManager.Wall?.currentLeftWall;
                    
                // [NEW] Force tất cả archer quay về base ngay lập tức để nhận wall mới
                guardManager.Squad.NotifyArchersDefensivePositionChanged(
                    isLeftSide ? safeWall : otherSideWall,
                    isLeftSide ? otherSideWall : safeWall,
                    forceReturn: true  // ← Force archer quay về base ngay
                );
                
                Debug.Log($"[GuardWallSystem] Đã force tất cả archer quay về vị trí phòng thủ mới. Left: {(isLeftSide ? safeWall?.name : otherSideWall?.name)}, Right: {(isLeftSide ? otherSideWall?.name : safeWall?.name)}");
            }
        }

        private GameObject FindSafeWallRecursively(GameObject currentWall)
        {
            if (currentWall == null || wallManager == null) return null;
            GameObject wallBehind = wallManager.GetWallBehind(currentWall);
            if (wallBehind == null) return null;
            if (wallManager.IsWallDefendable(wallBehind)) return wallBehind;
            return FindSafeWallRecursively(wallBehind); // Đệ quy tìm wall an toàn
        }

        public GameObject GetCurrentDefensiveWall(bool isLeft)
        {
            if (isLeft && currentLeftWall == null) UpdateDefensiveWalls();
            if (!isLeft && currentRightWall == null) UpdateDefensiveWalls();
            return isLeft ? currentLeftWall : currentRightWall;
        }

        public void OnWallListUpdated()
        {
            if (guardManager != null && !guardManager.Alarm.isEnemyAttack)
            {
                UpdateDefensiveWalls();
            }
        }

        /// <summary>
        /// TownCenter bị tấn công: chuyển tiền tuyến về Plaza để tập hợp Archer.
        /// Không phá hoặc đổi trạng thái bất kỳ wall nào.
        /// </summary>
        public void ForceFrontlineToPlaza()
        {
            if (guardManager == null) guardManager = FindObjectOfType<GuardManager>();

            UpdateSpecificWall(ref currentLeftWall, null, true);
            UpdateSpecificWall(ref currentRightWall, null, false);

            if (guardManager != null && guardManager.Squad != null)
                // TownCenter bị tấn công - force archer về Plaza
                guardManager.Squad.NotifyArchersDefensivePositionChanged(currentLeftWall, currentRightWall, forceReturn: true);

            if (guardManager != null)
                guardManager.DebugLog("[GuardWallSystem] TownCenter under attack -> frontline forced to Plaza.");
        }

        private void OnDestroy()
        {
            if (currentLeftWall != null)
            {
                var leftBehavior = currentLeftWall.GetComponent<WallBehavior>();
                if (leftBehavior != null) leftBehavior.OnCriticalHP -= HandleWallCritical;
            }
            if (currentRightWall != null)
            {
                var rightBehavior = currentRightWall.GetComponent<WallBehavior>();
                if (rightBehavior != null) rightBehavior.OnCriticalHP -= HandleWallCritical;
            }
        }
    }
}
