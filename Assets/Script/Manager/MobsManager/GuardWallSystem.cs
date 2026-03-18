using System.Collections.Generic;
using UnityEngine;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager.WorldManager;
using Script.Manager.Building;

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
            guardManager = GuardManager.Instance;
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
                guardManager.Squad.NotifyArchersDefensivePositionChanged(currentLeftWall, currentRightWall);
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
                guardManager.Squad.RetreatArchersOnFlank(isLeftSide, safeWall);
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
