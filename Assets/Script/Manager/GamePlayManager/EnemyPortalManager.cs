using UnityEngine;
using Script.Manager.WorldManager;
using Script.Data_Model;
using System.Collections.Generic;
using Script.Entities.Structures.SummondPortal;

namespace Script.Manager.GamePlayManager
{
    public class EnemyPortalManager : MonoBehaviour
    {
        public static EnemyPortalManager Instance { get; private set; }

        public GameObject portalL;
        public GameObject portalR;

        // Settings for spawning
        [Header("Spawn Settings")]
        public float minSpawnDistance = 40f;
        public float maxSpawnDistance = 80f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Đăng ký kiểm tra quá trình sinh cổng vào mỗi buổi sáng (6:00 AM)
            GameTimeManager.Instance.SubscribeToHourlyEvent(GameTimeManager.MORNING_HOUR, CheckAndSpawnPortals);
        }

        private void CheckAndSpawnPortals()
        {
            // Nếu chưa có TownCenter thì không sinh cổng
            if (Utility.GetTownCenter() == null)
            {
                Debug.Log("[EnemyPortalManager] Chưa có TownCenter, hủy sinh cổng hôm nay.");
                return;
            }

            // Sinh cổng ngẫu nhiên ở 1 trong 2 bên
            bool spawnRight = UnityEngine.Random.value > 0.5f;

            if (spawnRight)
            {
                if (!portalR) SpawnPortal(true);
            }
            else
            {
                if (!portalL) SpawnPortal(false);
            }
        }

        [ContextMenu("Test Spawn Random Portal")]
        public void TestSpawnRandomPortal()
        {
            bool spawnRight = UnityEngine.Random.value > 0.5f;
            
            if (spawnRight)
            {
                if (portalR)
                {
                    Debug.LogWarning("[EnemyPortalManager] Chọn trúng Cổng Phải (Right) nhưng đã tồn tại!");
                    return;
                }
                SpawnPortal(true);
            }
            else
            {
                if (portalL)
                {
                    Debug.LogWarning("[EnemyPortalManager] Chọn trúng Cổng Trái (Left) nhưng đã tồn tại!");
                    return;
                }
                SpawnPortal(false);
            }
        }

        private void SpawnPortal(bool isRightSide)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.summoningPortal == null)
            {
                Debug.LogError("[EnemyPortalManager] Không tìm thấy PrefabData hoặc summoningPortal prefab!");
                return;
            }

            // Lấy vị trí an toàn ngoài bức tường
            Vector3 spawnPos = Utility.GetRandomOutsideSafePos(isRightSide, minSpawnDistance, maxSpawnDistance);
            
            // Vector3.zero được trả về nếu lỗi (không có TownCenter)
            if (spawnPos == Vector3.zero) return;

            // Instantiate prefab
            GameObject newPortal = Instantiate(PrefabData.Instance.summoningPortal, spawnPos, Quaternion.identity);
            newPortal.name = isRightSide ? "EnemyPortal_Right" : "EnemyPortal_Left";

            // BẮT BUỘC: Đưa Portal vào EnemyContainer để EntitySaveManager quét được và lưu vào JSON
            if (EntitySaveManager.Instance != null && EntitySaveManager.Instance.enemyContainer != null)
            {
                newPortal.transform.SetParent(EntitySaveManager.Instance.enemyContainer.transform, true);
            }

            // Lưu reference lại
            if (isRightSide)
            {
                portalR = newPortal;
            }
            else
            {
                portalL = newPortal;
            }

            // Khởi tạo Setup cho portal behavior
            var behavior = newPortal.GetComponent<EnemyPortalBehavior>();
            if (behavior) 
            {
                // Gọi tới hàm setup của behavior
                behavior.Setup();
            }

            Debug.Log($"[EnemyPortalManager] Đã tạo thành công cổng {(isRightSide ? "PHẢI" : "TRÁI")} tại: {spawnPos}");
        }

        /// <summary>
        /// Được gọi từ EnemyPortalBehavior khi cổng bị phá
        /// </summary>
        public void OnPortalDestroyed(EnemyPortalBehavior portal)
        {
            if (portalL == portal.gameObject) portalL = null;
            if (portalR == portal.gameObject) portalR = null;
        }

        /// <summary>
        /// Hàm dành riêng cho WaveManager để lấy list cổng đã sẵn sàng đẻ quái
        /// </summary>
        public List<EnemyPortalBehavior> GetActivePortals()
        {
            List<EnemyPortalBehavior> list = new List<EnemyPortalBehavior>();
            
            // Validate bằng unity null-check
            if (portalL)
            {
                var behaviorL = portalL.GetComponent<EnemyPortalBehavior>();
                if (behaviorL && behaviorL.IsReadyToSpawn) list.Add(behaviorL);
            }

            if (portalR)
            {
                var behaviorR = portalR.GetComponent<EnemyPortalBehavior>();
                if (behaviorR && behaviorR.IsReadyToSpawn) list.Add(behaviorR);
            }

            return list;
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.gameObject)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(GameTimeManager.MORNING_HOUR, CheckAndSpawnPortals);
            }
        }
    }
}
