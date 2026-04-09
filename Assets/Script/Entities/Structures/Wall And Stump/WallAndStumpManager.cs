using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Script.Data_Model.Status;

using Script.Data_Model.Interfaces;
using Script.Data_Model;

namespace Script.Game.Structures.Wall_And_Stump
{
    /// <summary>
    /// [SUMMARY] Manager quản lý toàn bộ hệ thống Wall (Tường) và Stump (Gốc tường) trong game.
    /// Script này đóng vai trò là "Source of Truth" (nguồn dữ liệu gốc) cho các hệ thống khác như GuardManager.
    /// 
    /// CÁC HÀM CHÍNH ĐỂ TƯƠNG TÁC:
    /// 1. RegisterEntity(GameObject): Đăng ký 1 object. Tự động nhận diện là Wall hay Stump dựa trên Tag để nhét vào đúng List.
    /// 2. UnregisterEntity(GameObject): Hủy đăng ký 1 object khỏi mọi danh sách. Gọi khi object bị destroy.
    /// 3. SortAndNotify(): Sắp xếp lại toàn bộ danh sách theo khoảng cách nhà chính và thông báo cho lính (GuardManager) cập nhật vị trí.
    /// 4. GetDefendableWallBehind(GameObject): Tìm bức tường hợp lệ phía sau để lính rút lui khi tường hiện tại sắp vỡ.
    /// </summary>
    public class WallAndStumpManager : MonoBehaviour, IDataListBootstrap
    {
        #region Singleton & Fields
        public static WallAndStumpManager Instance { get; private set; }
        
        [Header("References")]
        public GameObject townCenter;
        private Script.Game.Mobs.MobsManager.GuardManager guardManager;

        [Header("Walls & Stumps Lists")]
        public List<GameObject> leftWalls = new List<GameObject>();
        public List<GameObject> rightWalls = new List<GameObject>();
        public List<GameObject> stumps = new List<GameObject>();

        [Header("Critical HP Settings")]
        [Tooltip("HP threshold để coi như wall nguy hiểm, cần rút lui")]
        public int criticalHPThreshold = 10;
        #endregion

        #region Lifecycle & Bootstrap
        private void Awake()
        {
            if (Instance == null)
            {
                InitializeSingleton();
                InitializeReferences();
            }
        }

        private void Start() { }

        private void InitializeSingleton()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void InitializeReferences()
        {
            if (guardManager == null)
                guardManager = FindObjectOfType<Script.Game.Mobs.MobsManager.GuardManager>();
        }

        public int Order => 0; // WallAndStumpManager should initialize before GuardManager (Order 1)

        public void InitializeData(PrefabData context)
        {
            InitializeSingleton();
            InitializeReferences();
            // Debug.Log("[WallAndStumpManager] Initialized via Bootstrap");
        }
        #endregion

        #region Unified Registration API
        /// <summary>
        /// Hàm đăng ký chính: Tự động xác minh Tag của Object để đưa vào danh sách Wall hoặc Stump tương ứng.
        /// </summary>
        public void RegisterEntity(GameObject entity)
        {
            if (entity == null) return;

            if (entity.CompareTag(Script.Utils.GameTags.Wall)) RegisterWall(entity);
            else if (entity.CompareTag(Script.Utils.GameTags.Stump)) RegisterStump(entity);
        }

        /// <summary>
        /// Hàm hủy đăng ký chính: Xóa Object khỏi mọi danh sách.
        /// </summary>
        public void UnregisterEntity(GameObject entity)
        {
            if (entity == null) return;

            UnregisterWall(entity);
            UnregisterStump(entity);
        }
        #endregion

        #region Wall Management (Internal)
        private void RegisterWall(GameObject wall)
        {
            if (wall == null) return;

            if (!leftWalls.Contains(wall) && !rightWalls.Contains(wall))
            {
                if (townCenter == null) townCenter = Utility.GetTownCenter();
                float wallX = wall.transform.position.x;
                float townCenterX = townCenter != null ? townCenter.transform.position.x : 0;

                if (wallX < townCenterX) leftWalls.Add(wall);
                else rightWalls.Add(wall);
            }

            SortAndNotify();
        }

        private void UnregisterWall(GameObject wall)
        {
            if (wall == null) return;
            bool removed = leftWalls.Remove(wall) || rightWalls.Remove(wall);
            if (removed) SortAndNotify();
        }

        private void RegisterStump(GameObject stump)
        {
            if (stump == null) return;
            if (!stumps.Contains(stump)) stumps.Add(stump);
        }

        private void UnregisterStump(GameObject stump)
        {
            if (stump == null) return;
            stumps.Remove(stump);
        }

        /// <summary>
        /// Sắp xếp dánh sách wall theo khoảng cách nhà chính (Index 0 là gần nhất).
        /// Sau khi sắp xếp, thông báo cho GuardManager để bầu chọn điểm phòng thủ.
        /// </summary>
        public void SortAndNotify()
        {
            if (townCenter == null)
            {
                var bm = FindObjectOfType<Script.Manager.Building.BuildingManager>();
                // Mute error log if BuildingManager hasn't loaded TownCenter yet
                if (bm != null && bm.currentTownCenter != null)
                {
                    townCenter = Script.Utils.WorldUtility.GetTownCenter();
                }
            }
            if (townCenter == null) return;

            if (leftWalls.Count > 0)
                leftWalls = leftWalls.OrderByDescending(w => w.transform.position.x).ToList();
            
            if (rightWalls.Count > 0)
                rightWalls = rightWalls.OrderBy(w => w.transform.position.x).ToList();

            if (guardManager == null) InitializeReferences();
            if (guardManager != null) guardManager.OnWallListUpdated();
        }
        #endregion

        #region Query Methods & Helpers
        /// <summary>
        /// Trả về true nếu wall hiện tại có HP trên mức báo động và có Tag là Wall.
        /// </summary>
        public bool IsWallDefendable(GameObject wall)
        {
            if (wall == null || !wall.CompareTag(Script.Utils.GameTags.Wall)) return false;
            var wallStatus = wall.GetComponent<WallStatus>();
            return wallStatus != null && wallStatus.currentHp >= criticalHPThreshold;
        }

        /// <summary>
        /// Lấy bức tường nằm phía sau (gần nhà chính hơn) của bức tường hiện tại.
        /// </summary>
        public GameObject GetWallBehind(GameObject currentWall)
        {
            if (currentWall == null) return null;

            if (townCenter == null) townCenter = Utility.GetTownCenter();
            if (townCenter == null) return null;

            float wallX = currentWall.transform.position.x;
            float townCenterX = townCenter.transform.position.x;

            List<GameObject> flank = (wallX < townCenterX) ? leftWalls : rightWalls;
            int index = flank.IndexOf(currentWall);

            // Index 0 là gần nhà chính nhất. Muốn lấy "phía sau" tức là gần nhà chính hơn -> index - 1.
            if (index > 0) return flank[index - 1];

            return null;
        }

        public GameObject GetFarthestWall(bool isLeft)
        {
            List<GameObject> list = isLeft ? leftWalls : rightWalls;
            return list.Count > 0 ? list[list.Count - 1] : null;
        }

        public GameObject GetNearestWallToTownCenter(bool isLeft)
        {
            List<GameObject> list = isLeft ? leftWalls : rightWalls;
            return list.Count > 0 ? list[0] : null;
        }

        public GameObject GetNearestWall(Vector3 position)
        {
            GameObject nearestWall = null;
            float minDistance = float.MaxValue;

            foreach (var wall in leftWalls.Concat(rightWalls))
            {
                if (wall == null) continue;
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance) { minDistance = distance; nearestWall = wall; }
            }
            return nearestWall;
        }
        #endregion

        #region Debug Helpers
        [ContextMenu("Debug: Print All Walls HP")]
        public void DebugPrintAllWallsHP()
        {
            Debug.Log("=== WALLS HP STATUS ===");
            foreach (var wall in leftWalls.Concat(rightWalls))
            {
                if (wall == null) continue;
                var status = wall.GetComponent<WallStatus>();
                string state = IsWallDefendable(wall) ? "✅ DEFENDABLE" : "❌ CRITICAL/STUMP";
                Debug.Log($"{wall.name}: HP={status?.currentHp}/{status?.maxHp} [{state}]");
            }
        }
        #endregion
    }
}