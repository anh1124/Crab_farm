using UnityEngine;
using Script.Manager.WorldManager;
using Script.Data_Model;
using System.Collections.Generic;
using Script.Entities.Structures.SummondPortal;
using Script.Game.Enemy;

namespace Script.Manager.GamePlayManager
{
    public class EnemyPortalManager : MonoBehaviour
    {
        public static EnemyPortalManager Instance { get; private set; }

        private IEnemyPool _enemyPool;
        private IEnemyDataProvider _enemyDataProvider;
        private IGameTimeProvider _timeProvider;
        private bool _isSubscribedToTime = false;

        public void Setup(IEnemyPool pool, IEnemyDataProvider dataProvider, IGameTimeProvider timeProvider)
        {
            _enemyPool = pool;
            _enemyDataProvider = dataProvider;
            _timeProvider = timeProvider;

            // Đăng ký ngay khi Setup() được gọi để tránh race condition với Start()
            if (_timeProvider != null && !_isSubscribedToTime)
            {
                _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                _isSubscribedToTime = true;
                Debug.Log("[EnemyPortalManager] Đã đăng ký SpawnAvailablePortal vào giờ 6:00 AM.");
            }
            else if (_timeProvider == null)
            {
                Debug.LogWarning("[EnemyPortalManager] Setup: timeProvider là null! Portal sẽ không tự sinh theo ngày.");
            }
        }

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
            // Nếu Setup() chưa được gọi (ví dụ: scene test đơn lẻ), fallback tự tìm
            if (!_isSubscribedToTime)
            {
                var tp = FindObjectOfType<GameTimeManager>();
                if (tp != null)
                {
                    _timeProvider = tp;
                    _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                    _isSubscribedToTime = true;
                    Debug.LogWarning("[EnemyPortalManager] Start() fallback: auto-found timeProvider và đăng ký ngưỡng 6:00 AM.");
                }
                else
                {
                    Debug.LogWarning("[EnemyPortalManager] Start(): Không tìm thấy GameTimeManager. Portal sẽ không tự sinh!");
                }
            }
        }

        private void RescuePlayerIfStuck(Vector3 portalPos, bool isRightSide, Vector3 portalScale)
        {
            if (Script.Controller.PlayerController.PlayerController.Instance == null || 
                Script.Controller.PlayerController.PlayerController.Instance.player == null) return;
            
            GameObject playerObj = Script.Controller.PlayerController.PlayerController.Instance.player;
            float playerX = playerObj.transform.position.x;

            if (isRightSide)
            {
                // Nếu player ở bên phải của portal phía bên phải (vùng bị kẹt)
                if (playerX > portalPos.x)
                {
                    Vector3 newPos = playerObj.transform.position;
                    newPos.x = portalPos.x - (portalScale.x * 2.5f); // Đẩy vào trong vương quốc
                    playerObj.transform.position = newPos;
                    Debug.LogWarning("[EnemyPortalManager] Giải cứu Player bị kẹt bên ngoài cổng PHẢI vừa sinh!");
                }
            }
            else
            {
                // Nếu player ở bên trái của portal phía bên trái (vùng bị kẹt)
                if (playerX < portalPos.x)
                {
                    Vector3 newPos = playerObj.transform.position;
                    newPos.x = portalPos.x + (portalScale.x * 2.5f); // Đẩy vào trong vương quốc
                    playerObj.transform.position = newPos;
                    Debug.LogWarning("[EnemyPortalManager] Giải cứu Player bị kẹt bên ngoài cổng TRÁI vừa sinh!");
                }
            }
        }

      

        [ContextMenu("Test Spawn Random Portal")]
        public void TestSpawnRandomPortal()
        {
            Debug.Log("[EnemyPortalManager] ---> Nhận lệnh TestSpawnRandomPortal từ ContextMenu...");
            SpawnAvailablePortal();
        }

        [ContextMenu("Spawn Starting Portals")]
        public void SpawnStartingPortals()
        {
            // Thử tìm lại trong scene nếu reference đang null (phòng trường hợp vừa load xong)
            if (portalL == null) portalL = GameObject.Find("EnemyPortal_Left");
            if (portalR == null) portalR = GameObject.Find("EnemyPortal_Right");

            if (portalL == null)
            {
                Debug.Log("[EnemyPortalManager] Sinh cổng KHỞI ĐẦU bên TRÁI.");
                SpawnPortal(false);
            }
            if (portalR == null)
            {
                Debug.Log("[EnemyPortalManager] Sinh cổng KHỞI ĐẦU bên PHẢI.");
                SpawnPortal(true);
            }
        }

        private void SpawnAvailablePortal()
        {
            if (portalL != null && portalR != null)
            {
                Debug.Log("[EnemyPortalManager] Cả 2 cổng Trái và Phải đều đã tồn tại. Không thể sinh thêm.");
                return;
            }

            if (portalL == null && portalR == null)
            {
                // Chưa có cổng nào -> Random
                bool spawnRight = UnityEngine.Random.value > 0.5f;
                Debug.Log($"[EnemyPortalManager] Random chọn sinh cổng {(spawnRight ? "PHẢI" : "TRÁI")}");
                SpawnPortal(spawnRight);
            }
            else if (portalL == null)
            {
                // Đã có cổng phải, thiếu cổng trái
                Debug.Log("[EnemyPortalManager] Cổng Phải đã tồn tại -> Bắt buộc chuyển sang sinh Cổng Trái.");
                SpawnPortal(false);
            }
            else 
            {
                // Đã có cổng trái, thiếu cổng phải
                Debug.Log("[EnemyPortalManager] Cổng Trái đã tồn tại -> Bắt buộc chuyển sang sinh Cổng Phải.");
                SpawnPortal(true);
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
                portalR = newPortal;
            else
                portalL = newPortal;

            // Khởi tạo Setup cho portal behavior
            var behavior = newPortal.GetComponent<EnemyPortalBehavior>();
            if (behavior) 
            {
                // Gọi tới hàm setup của behavior với các dependency được tiêm vào
                behavior.Setup(_enemyPool, _enemyDataProvider, _timeProvider);
            }

            // Giải cứu player nếu lỡ đứng ở vị trí bị kẹt khi portal vừa xuất hiện
            RescuePlayerIfStuck(spawnPos, isRightSide, newPortal.transform.localScale);

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

        [ContextMenu("Set All Portals Active")]
        public void SetAllPortalsActive()
        {
            if (portalL != null)
            {
                portalL.SetActive(true);
                var behaviorL = portalL.GetComponent<EnemyPortalBehavior>();
                if (behaviorL) behaviorL.ForceActivate();
                Debug.Log("[EnemyPortalManager] Đã bật và Kích Hoạt Sẵn Sàng (Active) cổng Trái.");
            }
            if (portalR != null)
            {
                portalR.SetActive(true);
                var behaviorR = portalR.GetComponent<EnemyPortalBehavior>();
                if (behaviorR) behaviorR.ForceActivate();
                Debug.Log("[EnemyPortalManager] Đã bật và Kích Hoạt Sẵn Sàng (Active) cổng Phải.");
            }
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

        /// <summary>
        /// Inject dependencies do portal tự gọi khi load từ save.
        /// </summary>
        public void InjectDependenciesIntoPortal(EnemyPortalBehavior portal)
        {
            if (portal != null)
            {
                portal.Setup(_enemyPool, _enemyDataProvider, _timeProvider);
                Debug.Log($"[EnemyPortalManager] Đã tiêm lại dependencies cho portal tại {portal.transform.position} (Load từ save).");
            }
        }

        private void OnDestroy()
        {
            if (_timeProvider != null && _isSubscribedToTime)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(6, SpawnAvailablePortal);
            }
        }
    }
}
