using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Script.Data_Model.Status;

using Script.Data_Model.Interfaces;
using Script.Data_Model;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class WallAndStumpManager : MonoBehaviour, IDataListBootstrap
    {
        //TODO: kiểm tra xem script này có trong GM chưa
        public static WallAndStumpManager Instance { get; private set; }
        public GameObject townCenter;
        public List<GameObject> leftWalls = new List<GameObject>();
        public List<GameObject> rightWalls = new List<GameObject>();
        public List<GameObject> stumps = new List<GameObject>();

        [Header("Critical HP Settings")]
        [Tooltip("HP threshold để coi như wall nguy hiểm, cần rút lui")]
        public int criticalHPThreshold = 10;

        [Header("Defendable Wall Tracking")]
        [Tooltip("List các wall bên trái còn có thể phòng thủ (HP >= threshold)")]
        public List<GameObject> defendableLeftWalls = new List<GameObject>();
        
        [Tooltip("List các wall bên phải còn có thể phòng thủ (HP >= threshold)")]
        public List<GameObject> defendableRightWalls = new List<GameObject>();

        // Cached references to avoid null checks every frame
        private Script.Game.Mobs.MobsManager.GuardManager guardManager;

        private void Start()
        {
            // Initialization is now handled via Bootstrap (InitializeData)
            // However, we can keep a safety check here or perform non-critical startup tasks
        }

        private void Awake()
        {
            // Singleton init handled in InitializeSingleton which is called by Bootstrap
            // But if Bootstrap doesn't run (e.g. testing scene without Bootstrap), we need this:
            if (Instance == null)
            {
                InitializeSingleton();
                InitializeReferences();
                UpdateDefendableWalls();
            }
        }

        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeReferences()
        {
            // Cache GuardManager reference
            if (guardManager == null)
            {
                guardManager = Script.Game.Mobs.MobsManager.GuardManager.Instance;
            }
        }

        #region IDataListBootstrap Implementation
        // Highest priority (0) - must initialize before GuardManager (Order 1)
        public int Order => 0;

        public void InitializeData(PrefabData context)
        {
            InitializeSingleton();
            InitializeReferences();
            UpdateDefendableWalls();
            // Debug.Log("[WallAndStumpManager] Initialized via Bootstrap");
        }
        #endregion

        #region Wall HP Monitoring

        /// <summary>
        /// Kiểm tra HP của wall và thông báo nếu critical
        /// GỌI HÀM NÀY từ WallBehavior.TakeDamage()
        /// </summary>
        /// <param name="wall">Wall cần kiểm tra</param>
        public void CheckWallCriticalHP(GameObject wall)
        {
            if (wall == null)
            {
                Debug.LogWarning("[WallAndStumpManager] CheckWallCriticalHP: wall is null");
                return;
            }

            var wallStatus = wall.GetComponent<WallStatus>();
            if (wallStatus == null)
            {
                Debug.LogWarning($"[WallAndStumpManager] Wall {wall.name} missing WallStatus component");
                return;
            }

            // Check if HP is below critical threshold
            if (wallStatus.currentHp < criticalHPThreshold)
            {
               // Debug.Log($"[WallAndStumpManager] ⚠️ WALL CRITICAL: {wall.name} HP={wallStatus.currentHp}/{criticalHPThreshold}");
               // Logic notify GuardManager đã được chuyển sang Event-based trong WallBehavior -> GuardManager
               // Hàm này chỉ giữ lại để log hoặc debug nếu cần.
            }
        }

        /// <summary>
        /// Kiểm tra wall có nằm bên trái town center không
        /// </summary>
        private bool IsWallOnLeftSide(GameObject wall)
        {
            if (wall == null) return false;
            
            if (townCenter == null)
            {
                townCenter = Utility.GetTownCenter();
            }

            if (townCenter == null)
            {
                Debug.LogError("[WallAndStumpManager] Cannot determine wall side - Town Center not found!");
                return false;
            }

            float wallX = wall.transform.position.x;
            float townCenterX = townCenter.transform.position.x;
            
            return wallX < townCenterX;
        }

        /// <summary>
        /// Thông báo GuardManager về wall critical
        /// </summary>
        private void NotifyGuardManagerWallCritical(GameObject wall, bool isLeftSide)
        {
            if (guardManager == null)
            {
                // Try to get it again
                guardManager = Script.Game.Mobs.MobsManager.GuardManager.Instance;
            }

            if (guardManager != null)
            {
                guardManager.NotifyWallCritical(wall, isLeftSide);
                Debug.Log($"[WallAndStumpManager] Notified GuardManager about critical wall: {wall.name}");
            }
            else
            {
                Debug.LogWarning("[WallAndStumpManager] GuardManager not found, cannot notify!");
            }
        }

        #endregion

        #region Defendable Wall Management

        /// <summary>
        /// Cập nhật danh sách các wall còn có thể phòng thủ (HP >= threshold)
        /// Gọi hàm này mỗi khi wall HP thay đổi hoặc wall bị destroy
        /// </summary>
        public void UpdateDefendableWalls()
        {
            defendableLeftWalls.Clear();
            defendableRightWalls.Clear();

            // Check left walls
            foreach (var wall in leftWalls)
            {
                if (wall == null) continue;

                var wallStatus = wall.GetComponent<WallStatus>();
                if (wallStatus != null && wallStatus.currentHp >= criticalHPThreshold)
                {
                    defendableLeftWalls.Add(wall);
                }
            }

            // Check right walls
            foreach (var wall in rightWalls)
            {
                if (wall == null) continue;

                var wallStatus = wall.GetComponent<WallStatus>();
                if (wallStatus != null && wallStatus.currentHp >= criticalHPThreshold)
                {
                    defendableRightWalls.Add(wall);
                }
            }

            // Debug.Log($"[WallAndStumpManager] Updated defendable walls - Left: {defendableLeftWalls.Count}, Right: {defendableRightWalls.Count}");
        }

        /// <summary>
        /// Lấy wall có thể phòng thủ xa nhất (cho archer mới spawn)
        /// </summary>
        /// <param name="isLeft">Bên trái hay phải</param>
        /// <returns>Wall xa nhất có thể phòng thủ, hoặc null nếu không có</returns>
        public GameObject GetFarthestDefendableWall(bool isLeft)
        {
            List<GameObject> walls = isLeft ? defendableLeftWalls : defendableRightWalls;

            if (walls.Count > 0)
            {
                // Defendable walls already sorted because source lists (leftWalls/rightWalls) are sorted
                // leftWalls: sorted descending by X (farthest left is index 0)
                // rightWalls: sorted ascending by X (closest to town is index 0... WAIT)
                
                // Checking sort order in RegisterWall:
                // Left: OrderByDescending(x) -> Largest X first? No.
                // Left side: X < TownX. Farthest is Smallest X.
                // OrderByDescending: 10, 5, 2... No wait. -2, -5, -10. Descending means -2 first (closest).
                // Let's re-read RegisterWall logic carefully.
                
                // LOGIC CHECK: 
                // leftWalls.OrderByDescending(w => w.transform.position.x)
                // If Town is 0. Walls at -5, -10, -20.
                // Descending: -5, -10, -20. 
                // Index 0 is -5 (Closest to town). Index Last is -20 (Farthest).
                
                // rightWalls.OrderBy(w => w.transform.position.x)
                // Walls at 5, 10, 20.
                // Ascending: 5, 10, 20.
                // Index 0 is 5 (Closest to town). Index Last is 20 (Farthest).
                
                // SO: For BOTH lists, Index 0 is CLOSEST to town. Index Last is FARTHEST.
                
                // GetFarthestDefendableWall needs the one FARTHEST from town.
                // That means the LAST element in the list.
                
                GameObject farthest = walls[walls.Count - 1];
                Debug.Log($"[WallAndStumpManager] GetFarthestDefendableWall({(isLeft ? "LEFT" : "RIGHT")}): {farthest.name}");
                return farthest;
            }

            Debug.LogWarning($"[WallAndStumpManager] No defendable wall on {(isLeft ? "LEFT" : "RIGHT")} side!");
            return null;
        }

        /// <summary>
        /// Kiểm tra xem có wall nào có thể phòng thủ phía sau wall hiện tại không
        /// </summary>
        /// <param name="currentWall">Wall hiện tại</param>
        /// <returns>Wall phía sau có thể phòng thủ, hoặc null nếu không có</returns>
        public GameObject GetDefendableWallBehind(GameObject currentWall)
        {
            if (currentWall == null) return null;

            // First get the normal wall behind
            GameObject wallBehind = GetWallBehind(currentWall);

            // If no wall behind at all
            if (wallBehind == null)
            {
                // Debug.Log($"[WallAndStumpManager] No wall behind {currentWall.name} at all");
                return null;
            }

            // Check if wall behind is defendable
            var wallStatus = wallBehind.GetComponent<WallStatus>();
            if (wallStatus != null && wallStatus.currentHp >= criticalHPThreshold)
            {
                // Debug.Log($"[WallAndStumpManager] Defendable wall behind {currentWall.name}: {wallBehind.name}");
                return wallBehind;
            }

            // Wall behind is not defendable, try to find next one
            // Debug.Log($"[WallAndStumpManager] Wall behind {currentWall.name} ({wallBehind.name}) is NOT defendable, searching further...");
            
            // Recursive call to find the next wall behind that IS defendable.
            // If this returns null, it means there are NO more defendable walls behind up to the town center.
            // Archers receiving null should enter "Last Stand" mode (fight to the death).
            return GetDefendableWallBehind(wallBehind); 
        }

        /// <summary>
        /// Đếm số lượng wall có thể phòng thủ ở một bên
        /// </summary>
        public int GetDefendableWallCount(bool isLeft)
        {
            return isLeft ? defendableLeftWalls.Count : defendableRightWalls.Count;
        }

        /// <summary>
        /// Kiểm tra xem một wall có thể phòng thủ không
        /// </summary>
        public bool IsWallDefendable(GameObject wall)
        {
            if (wall == null) return false;

            var wallStatus = wall.GetComponent<WallStatus>();
            if (wallStatus == null) return false;

            return wallStatus.currentHp >= criticalHPThreshold;
        }

        #endregion

        #region Wall Recovery

        /// <summary>
        /// Gọi hàm này khi wall được repair/heal
        /// Sẽ cập nhật lại defendable walls list
        /// </summary>
        public void OnWallRepaired(GameObject wall)
        {
            if (wall == null) return;

            var wallStatus = wall.GetComponent<WallStatus>();
            if (wallStatus == null) return;

            Debug.Log($"[WallAndStumpManager] Wall {wall.name} repaired to HP={wallStatus.currentHp}");
            
            // Update defendable walls
            UpdateDefendableWalls();

            // Chỉ notify GuardManager nếu KHÔNG đang thiết quân luật
            // OnWallRepaired chỉ được gọi khi wall ĐẦY MÁU (theo logic WallBehavior.Heal)
            // Nên không cần check HP threshold nữa
            if (guardManager != null && guardManager.detectedEnemies.Count == 0)
            {
                guardManager.UpdateDefensiveWalls();
            }
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// In ra thông tin tất cả walls và trạng thái HP
        /// </summary>
        [ContextMenu("Debug: Print All Walls HP")]
        public void DebugPrintAllWallsHP()
        {
            Debug.Log("=== LEFT WALLS HP ===");
            foreach (var wall in leftWalls)
            {
                if (wall == null) continue;
                var status = wall.GetComponent<WallStatus>();
                string defendable = IsWallDefendable(wall) ? "✅ DEFENDABLE" : "❌ CRITICAL";
                Debug.Log($"{wall.name}: HP={status?.currentHp ?? 0}/{status?.maxHp ?? 0} {defendable}");
            }

            Debug.Log("=== RIGHT WALLS HP ===");
            foreach (var wall in rightWalls)
            {
                if (wall == null) continue;
                var status = wall.GetComponent<WallStatus>();
                string defendable = IsWallDefendable(wall) ? "✅ DEFENDABLE" : "❌ CRITICAL";
                Debug.Log($"{wall.name}: HP={status?.currentHp ?? 0}/{status?.maxHp ?? 0} {defendable}");
            }

            Debug.Log($"Defendable: Left={defendableLeftWalls.Count}, Right={defendableRightWalls.Count}");
        }

        /// <summary>
        /// Test hàm GetDefendableWallBehind
        /// </summary>
        [ContextMenu("Debug: Test GetDefendableWallBehind")]
        public void DebugTestGetDefendableWallBehind()
        {
            if (leftWalls.Count > 0)
            {
                // Assuming leftWalls[0] is closest to town (Wait, let's verify sort logic again).
                // Based on RegisterWall:
                // Left: -5, -10, -20. Descending. -5 is index 0. -20 is index Last.
                // Closest to town is index 0. Farthest is index Last.
                
                // So if we are at Farthest (Last), behind is Closer (Last - 1).
                
                GameObject farthestWall = GetFarthestDefendableWall(true);
                if (farthestWall != null)
                {
                   GameObject wallBehind = GetDefendableWallBehind(farthestWall);
                   Debug.Log($"Test wall: {farthestWall.name}, Defendable wall behind: {wallBehind?.name ?? "NULL"}");
                }
            }
        }

        #endregion

        #region Wall Management
        public void RegisterWall(GameObject wall)
        {
            if (wall == null) return;

            float wallX = wall.transform.position.x;
            townCenter = Utility.GetTownCenter();

            if (townCenter == null)
            {
                Debug.LogError("[WallAndStumpManager] Town Center not found, cannot register wall.");
                return;
            }

            float townCenterX = townCenter.transform.position.x;

            if (wallX < townCenterX)
            {
                // Add to left walls and keep sorted by distance to town center (closest first)
                leftWalls.Add(wall);
                leftWalls = leftWalls.OrderByDescending(w => w.transform.position.x).ToList();
            }
            else
            {
                // Add to right walls and keep sorted by distance to town center (closest first)
                rightWalls.Add(wall);
                rightWalls = rightWalls.OrderBy(w => w.transform.position.x).ToList();
            }
            //Debug.Log($"[WallAndStumpManager] Registered wall {wall.name}. Left Walls: {leftWalls.Count}, Right Walls: {rightWalls.Count}");
            
            // Update defendable walls list too (ensure new wall is counted if healthy)
            UpdateDefendableWalls();
            
            // Chỉ notify GuardManager nếu KHÔNG đang trong thiết quân luật
            if (guardManager != null && guardManager.detectedEnemies.Count == 0)
            {
                guardManager.UpdateDefensiveWalls();
            }
        }

        public void UnregisterWall(GameObject wall)
        {
            if (wall == null) return;

            if (leftWalls.Contains(wall))
            {
                leftWalls.Remove(wall);
            }
            else if (rightWalls.Contains(wall))
            {
                rightWalls.Remove(wall);
            }
            //Debug.Log($"[WallAndStumpManager] Unregistered wall {wall.name}. Left Walls: {leftWalls.Count}, Right Walls: {rightWalls.Count}");
            
            // Update defendable walls list too
            UpdateDefendableWalls();
        }
        #endregion

        #region Stump Management
        public void RegisterStump(GameObject stump)
        {
            if (stump == null) return;
            if (!stumps.Contains(stump))
            {
                stumps.Add(stump);
                Debug.Log($"[WallAndStumpManager] Registered stump {stump.name}. Total stumps: {stumps.Count}");
            }
        }

        public void UnregisterStump(GameObject stump)
        {
            if (stump == null) return;
            if (stumps.Contains(stump))
            {
                stumps.Remove(stump);
                Debug.Log($"[WallAndStumpManager] Unregistered stump {stump.name}. Total stumps: {stumps.Count}");
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Lấy bức tường xa trung tâm thị trấn nhất (bên trái hoặc bên phải).
        /// </summary>
        /// <param name="isLeft">True để lấy tường bên trái, False để lấy tường bên phải.</param>
        /// <returns>GameObject của bức tường xa nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetFarthestWall(bool isLeft)
        {
            if (isLeft)
            {
                return leftWalls.Count > 0 ? leftWalls[0] : null; // Left walls are sorted descending by x, so 0 is farthest left
            }
            else
            {
                return rightWalls.Count > 0 ? rightWalls[rightWalls.Count - 1] : null; // Right walls are sorted ascending by x, so last is farthest right
            }
        }

        /// <summary>
        /// Lấy bức tường gần nhất với một vị trí cụ thể.
        /// </summary>
        /// <param name="position">Vị trí tham chiếu.</param>
        /// <returns>GameObject của bức tường gần nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetNearestWall(Vector3 position)
        {
            GameObject nearestWall = null;
            float minDistance = float.MaxValue;

            foreach (var wall in leftWalls)
            {
                if (wall == null) continue;
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestWall = wall;
                }
            }

            foreach (var wall in rightWalls)
            {
                if (wall == null) continue;
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestWall = wall;
                }
            }

            return nearestWall;
        }

        /// <summary>
        /// Lấy bức tường gần trung tâm thị trấn nhất (bên trái hoặc bên phải).
        /// </summary>
        /// <param name="isLeft">True để lấy tường bên trái, False để lấy tường bên phải.</param>
        /// <returns>GameObject của bức tường gần nhất, hoặc null nếu không có tường.</returns>
        public GameObject GetNearestWallToTownCenter(bool isLeft)
        {
            if (isLeft)
            {
                return leftWalls.Count > 0 ? leftWalls[leftWalls.Count - 1] : null; // Left walls are sorted descending by x, so last is closest to town center
            }
            else
            {
                return rightWalls.Count > 0 ? rightWalls[0] : null; // Right walls are sorted ascending by x, so 0 is closest to town center
            }
        }

        /// <summary>
        /// Lấy bức tường ngay phía sau bức tường hiện tại trong danh sách (gần trung tâm thị trấn hơn).
        /// </summary>
        /// <param name="currentWall">Bức tường hiện tại.</param>
        /// <returns>GameObject của bức tường phía sau, hoặc null nếu không tìm thấy hoặc là bức tường gần nhất.</returns>
        public GameObject GetWallBehind(GameObject currentWall)
        {
            if (currentWall == null) return null;

            float wallX = currentWall.transform.position.x;
            if (townCenter == null) townCenter = Utility.GetTownCenter();
            if (townCenter == null)
            {
                Debug.LogError("[WallAndStumpManager] Town Center not found, cannot get wall behind.");
                return null;
            }

            float townCenterX = townCenter.transform.position.x;

            if (wallX < townCenterX)
            {
                // Bức tường hiện tại nằm ở phía bên trái
                int index = leftWalls.IndexOf(currentWall);
                // Nếu tìm thấy bức tường hiện tại và nó không phải là bức tường gần trung tâm thị trấn nhất (index > 0)
                if (index != -1 && index > 0)
                {
                    return leftWalls[index - 1]; // Bức tường trước đó trong danh sách giảm dần (gần trung tâm thị trấn hơn)
                }
            }
            else
            {
                // Bức tường hiện tại nằm ở phía bên phải
                int index = rightWalls.IndexOf(currentWall);
                // Nếu tìm thấy bức tường hiện tại và nó không phải là bức tường gần trung tâm thị trấn nhất (index > 0)
                if (index != -1 && index > 0)
                {
                    return rightWalls[index - 1]; // Bức tường trước đó trong danh sách tăng dần (gần trung tâm thị trấn hơn)
                }
            }

            return null; // Không tìm thấy bức tường phía sau hoặc nó là bức tường gần nhất với trung tâm thị trấn
        }
        #endregion
    }
} 