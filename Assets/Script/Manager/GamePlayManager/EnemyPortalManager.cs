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
            Debug.Log("[EnemyPortalManager] Setup() được gọi");
            _enemyPool = pool;
            _enemyDataProvider = dataProvider;
            _timeProvider = timeProvider;

            // Đăng ký ngay khi Setup() được gọi để tránh race condition với Start()
            Debug.Log($"[EnemyPortalManager] Setup: timeProvider={(_timeProvider != null)}, _isSubscribedToTime={_isSubscribedToTime}");
            
            if (_timeProvider != null && !_isSubscribedToTime)
            {
                int currentDay = _timeProvider.GetElapsedDays();
                int currentHour = _timeProvider.GameHour;
                
                Debug.Log("[EnemyPortalManager] Bắt đầu đăng ký hourly events...");
                
                _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                Debug.Log("[EnemyPortalManager] Đã đăng ký SpawnAvailablePortal(6)");
                
                _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                Debug.Log("[EnemyPortalManager] Đã đăng ký ProcessFrontlineDelays(6)");
                
                _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                Debug.Log("[EnemyPortalManager] Đã đăng ký MovePortalsTowardsTownCenter(19)");
                
                _isSubscribedToTime = true;
                
                Debug.Log($"[EnemyPortalManager] === SETUP OK | Day {currentDay} Hour {currentHour}:00 ===");
                Debug.Log($"[EnemyPortalManager] Tất cả hourly events đã đăng ký thành công!");
            }
            else if (_timeProvider == null)
            {
                Debug.LogError("[EnemyPortalManager] KHÔNG THỂ ĐĂNG KÝ! timeProvider là null!");
            }
            else
            {
                Debug.LogWarning($"[EnemyPortalManager] Bỏ qua đăng ký vì _isSubscribedToTime={_isSubscribedToTime}");
            }
        }

        // Legacy: Giữ lại để tương thích với code cũ, sẽ được cập nhật từ danh sách mới
        public GameObject portalL;
        public GameObject portalR;

        // Storm Invasion: Danh sách quản lý tất cả portal theo bên
        [Header("Storm Invasion - Portal Lists")]
        [SerializeField] private List<EnemyPortalBehavior> leftPortals = new List<EnemyPortalBehavior>();
        [SerializeField] private List<EnemyPortalBehavior> rightPortals = new List<EnemyPortalBehavior>();

        // Settings for spawning
        [Header("Spawn Settings")]
        public float minSpawnDistance = 40f;
        public float maxSpawnDistance = 80f;

        [Header("Storm Invasion - Movement")]
        [Tooltip("Tốc độ di chuyển của portal về phía TownCenter mỗi đêm")]
        public float portalMoveSpeed = 5f;
        [Tooltip("Thời gian di chuyển mỗi đêm (giây)")]
        public float portalMoveDuration = 3f;
        [Tooltip("Khoảng cách spawn portal phía sau (so với portal gần nhất)")]
        public float backPortalSpawnOffset = 100f;
        [Tooltip("Số portal tối đa mỗi bên")]
        public int maxPortalsPerSide = 2;

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
                    _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                    _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                    _isSubscribedToTime = true;
                    Debug.LogWarning("[EnemyPortalManager] Start() fallback: auto-found timeProvider và đăng ký đủ 3 events (6h spawn, 6h delay, 19h move).");
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
            // FIX: Đảm bảo đã được setup trước khi spawn (tránh lỗi khi ChunkManager gọi trước InitializeEnemyBase)
            if (!_isSubscribedToTime)
            {
                Debug.LogWarning("[EnemyPortalManager] SpawnStartingPortals() gọi trước khi Setup()! Tự động fallback setup...");
                var tp = FindObjectOfType<GameTimeManager>();
                if (tp != null)
                {
                    _timeProvider = tp;
                    if (_timeProvider != null && !_isSubscribedToTime)
                    {
                        _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                        _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                        _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                        _isSubscribedToTime = true;
                        Debug.Log("[EnemyPortalManager] Lazy Setup: Đã đăng ký time events!");
                    }
                }
                else
                {
                    Debug.LogError("[EnemyPortalManager] Không tìm thấy GameTimeManager! Portal sẽ không nhận time events.");
                }
            }

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

            // Storm Invasion: Spawn thêm 2 portal phía sau sau khi có portal ban đầu
            // Chỉ thực hiện nếu đang tạo world mới (không có map.json)
            if (!EntitySaveManager.IsLoading)
            {
                // Đợi 1 frame để portal ban đầu hoàn tất Start() và đăng ký vào danh sách
                StartCoroutine(SpawnBackPortalsDelayed());
            }
        }

        private System.Collections.IEnumerator SpawnBackPortalsDelayed()
        {
            // Đợi 2 frame:
            // Frame 1: Instantiate xong
            // Frame 2: Start() của portal chạy → RegisterPortal() hoàn tất
            yield return null;
            yield return null;
            SpawnBackPortals();
        }

        private void SpawnAvailablePortal()
        {
            // Storm Invasion: Check danh sách mới thay vì chỉ check portalL/R
            bool needLeft = leftPortals.Count < maxPortalsPerSide;
            bool needRight = rightPortals.Count < maxPortalsPerSide;

            if (!needLeft && !needRight)
            {
                Debug.Log($"[EnemyPortalManager] Đã đủ {maxPortalsPerSide} portal mỗi bên. Không thể sinh thêm.");
                return;
            }

            // Ưu tiên spawn bên nào thiếu, nếu cả 2 đều thiếu thì random
            if (needLeft && needRight)
            {
                bool spawnRight = UnityEngine.Random.value > 0.5f;
                Debug.Log($"[EnemyPortalManager] Cả 2 bên đều thiếu -> Random chọn sinh cổng {(spawnRight ? "PHẢI" : "TRÁI")}");
                SpawnPortal(spawnRight);
            }
            else if (needLeft)
            {
                Debug.Log($"[EnemyPortalManager] Bên trái thiếu ({leftPortals.Count}/{maxPortalsPerSide}) -> Sinh cổng Trái.");
                SpawnPortal(false);
            }
            else
            {
                Debug.Log($"[EnemyPortalManager] Bên phải thiếu ({rightPortals.Count}/{maxPortalsPerSide}) -> Sinh cổng Phải.");
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

            // FIX: Auto-find dependencies nếu chưa được inject (khi spawn trước khi Setup() hoàn tất)
            if (_enemyPool == null)
            {
                _enemyPool = FindObjectOfType<Script.Game.Enemy.EnemyPool>(true);
                Debug.LogWarning($"[EnemyPortalManager] _enemyPool chưa được inject, auto-find: {_enemyPool != null}");
            }
            if (_enemyDataProvider == null)
            {
                _enemyDataProvider = FindObjectOfType<Script.Game.Enemy.TotalEnemyList>(true);
                Debug.LogWarning($"[EnemyPortalManager] _enemyDataProvider chưa được inject, auto-find: {_enemyDataProvider != null}");
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
                
                // FIX: Đăng ký ngay lập tức thay vì chờ Start() frame sau
                // Tránh race condition với ProcessFrontlineDelays (6:00)
                RegisterPortal(behavior);
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
            if (portal == null) return;

            // Xóa khỏi danh sách mới
            leftPortals.Remove(portal);
            rightPortals.Remove(portal);

            // Cập nhật legacy references
            if (portalL == portal.gameObject) portalL = null;
            if (portalR == portal.gameObject) portalR = null;

            // Sắp xếp lại và cập nhật tuyến đầu
            SortPortals();
            UpdateFrontlineStatus();

            Debug.Log($"[EnemyPortalManager] Portal bị phá tại x={portal.transform.position.x:F1}. Đã cập nhật danh sách.");
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

        #region Storm Invasion System

        /// <summary>
        /// Được gọi từ EnemyPortalBehavior khi portal spawn hoặc load từ save.
        /// Đăng ký portal vào danh sách quản lý theo bên và sắp xếp lại.
        /// </summary>
        public void RegisterPortal(EnemyPortalBehavior portal)
        {
            if (portal == null) return;

            GameObject townCenter = Utility.GetTownCenter();
            float centerX = townCenter != null ? townCenter.transform.position.x : 0f;
            float portalX = portal.transform.position.x;

            // Xác định bên trái/phải
            bool isRight = portalX > centerX;

            Debug.Log($"[EnemyPortalManager] RegisterPortal: portal tại x={portalX:F1}, centerX={centerX}, isRight={isRight}");

            if (isRight)
            {
                if (!rightPortals.Contains(portal))
                {
                    rightPortals.Add(portal);
                    // Cập nhật legacy reference
                    portalR = portal.gameObject;
                    Debug.Log($"[EnemyPortalManager] Thêm portal vào rightPortals. Count={rightPortals.Count}");
                }
            }
            else
            {
                if (!leftPortals.Contains(portal))
                {
                    leftPortals.Add(portal);
                    // Cập nhật legacy reference
                    portalL = portal.gameObject;
                    Debug.Log($"[EnemyPortalManager] Thêm portal vào leftPortals. Count={leftPortals.Count}");
                }
            }

            SortPortals();
            UpdateFrontlineStatus();
        }

        /// <summary>
        /// Sắp xếp danh sách portal theo khoảng cách đến TownCenter (gần nhất đầu tiên).
        /// </summary>
        private void SortPortals()
        {
            GameObject townCenter = Utility.GetTownCenter();
            Vector3 centerPos = townCenter != null ? townCenter.transform.position : Vector3.zero;

            // Sắp xếp left portals: gần TownCenter nhất (x lớn nhất trong số âm) đầu tiên
            leftPortals.Sort((a, b) =>
            {
                float distA = Mathf.Abs(a.transform.position.x - centerPos.x);
                float distB = Mathf.Abs(b.transform.position.x - centerPos.x);
                return distA.CompareTo(distB);
            });

            // Sắp xếp right portals: gần TownCenter nhất (x nhỏ nhất trong số dương) đầu tiên
            rightPortals.Sort((a, b) =>
            {
                float distA = Mathf.Abs(a.transform.position.x - centerPos.x);
                float distB = Mathf.Abs(b.transform.position.x - centerPos.x);
                return distA.CompareTo(distB);
            });

            // Cập nhật legacy references
            if (leftPortals.Count > 0) portalL = leftPortals[0].gameObject;
            else portalL = null;

            if (rightPortals.Count > 0) portalR = rightPortals[0].gameObject;
            else portalR = null;
        }

        /// <summary>
        /// Cập nhật trạng thái tuyến đầu cho tất cả portal.
        /// Chỉ portal đầu tiên mỗi bên là tuyến đầu.
        /// Khi portal mới trở thành tuyến đầu (vừa lên index 0), reset status.activeDelay.
        /// </summary>
        private void UpdateFrontlineStatus()
        {
            // Left side
            for (int i = 0; i < leftPortals.Count; i++)
            {
                bool isFront = (i == 0); // Chỉ portal đầu tiên là tuyến đầu
                bool wasFront = leftPortals[i].IsFrontline(); // Kiểm tra trạng thái cũ
                leftPortals[i].SetFrontline(isFront);
                
                // Nếu vừa trở thành tuyến đầu, reset status.activeDelay
                if (isFront && !wasFront)
                {
                    leftPortals[i].ResetToDefaultDelay();
                    Debug.Log($"[EnemyPortalManager] Left portal [{i}] tại x={leftPortals[i].transform.position.x:F1} VỪA TRỞ THÀNH TUYẾN ĐẦU - Reset delay");
                }
                
                if (isFront)
                    Debug.Log($"[EnemyPortalManager] Left portal [{i}] tại x={leftPortals[i].transform.position.x:F1} là TUYẾN ĐẦU");
            }

            // Right side
            for (int i = 0; i < rightPortals.Count; i++)
            {
                bool isFront = (i == 0);
                bool wasFront = rightPortals[i].IsFrontline();
                rightPortals[i].SetFrontline(isFront);
                
                if (isFront && !wasFront)
                {
                    rightPortals[i].ResetToDefaultDelay();
                    Debug.Log($"[EnemyPortalManager] Right portal [{i}] tại x={rightPortals[i].transform.position.x:F1} VỪA TRỞ THÀNH TUYẾN ĐẦU - Reset delay");
                }
                
                if (isFront)
                    Debug.Log($"[EnemyPortalManager] Right portal [{i}] tại x={rightPortals[i].transform.position.x:F1} là TUYẾN ĐẦU");
            }
        }

        /// <summary>
        /// Gọi portal tuyến đầu (index 0) tự trừ delay.
        /// Portal tự quyết định có active hay không dựa trên status.activeDelay.
        /// </summary>
        private void ProcessFrontlineDelays()
        {
            Debug.Log("[EnemyPortalManager] ProcessFrontlineDelays() ĐƯỢC GỌI!");
            
            int currentDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
            int currentHour = _timeProvider != null ? _timeProvider.GameHour : 0;
            
            Debug.Log($"[EnemyPortalManager] === ProcessFrontlineDelays | Day {currentDay} Hour {currentHour}:00 === Left={leftPortals.Count}, Right={rightPortals.Count}");
            
            // Gọi left portal index 0 tự xử lý delay
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                var portal = leftPortals[0];
                Debug.Log($"[EnemyPortalManager] Gọi TickDelay cho Left[0] x={portal.transform.position.x:F1} | isFrontline={portal.IsFrontline()} activeDelay={portal.GetStatus()?.activeDelay} isReady={portal.IsReadyToSpawn}");
                leftPortals[0].TickDelay();
            }
            else
            {
                Debug.Log($"[EnemyPortalManager] [WARNING] Không có Left[0] để TickDelay! List count={leftPortals.Count}");
            }
            
            // Gọi right portal index 0 tự xử lý delay
            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                var portal = rightPortals[0];
                Debug.Log($"[EnemyPortalManager] Gọi TickDelay cho Right[0] x={portal.transform.position.x:F1} | isFrontline={portal.IsFrontline()} activeDelay={portal.GetStatus()?.activeDelay} isReady={portal.IsReadyToSpawn}");
                rightPortals[0].TickDelay();
            }
            else
            {
                Debug.Log($"[EnemyPortalManager] [WARNING] Không có Right[0] để TickDelay! List count={rightPortals.Count}");
            }
        }

        /// <summary>
        /// Lấy danh sách portal tuyến đầu (gần TownCenter nhất mỗi bên, đang active).
        /// </summary>
        public List<EnemyPortalBehavior> GetFrontlineActivePortals()
        {
            List<EnemyPortalBehavior> frontline = new List<EnemyPortalBehavior>();

            if (leftPortals.Count > 0 && leftPortals[0] != null && leftPortals[0].IsReadyToSpawn)
                frontline.Add(leftPortals[0]);

            if (rightPortals.Count > 0 && rightPortals[0] != null && rightPortals[0].IsReadyToSpawn)
                frontline.Add(rightPortals[0]);

            return frontline;
        }

        /// <summary>
        /// Yêu cầu portal tuyến đầu di chuyển về phía TownCenter.
        /// Portal tự quyết định có di chuyển hay không (nếu chưa active thì không di chuyển).
        /// </summary>
        private void MovePortalsTowardsTownCenter()
        {
            GameObject townCenter = Utility.GetTownCenter();
            if (townCenter == null) return;

            float targetX = townCenter.transform.position.x;

            // Yêu cầu left portal index 0 di chuyển (portal tự quyết định)
            if (leftPortals.Count > 0 && leftPortals[0] != null)
            {
                leftPortals[0].TryMoveTowards(targetX, portalMoveSpeed, portalMoveDuration);
            }

            // Yêu cầu right portal index 0 di chuyển (portal tự quyết định)
            if (rightPortals.Count > 0 && rightPortals[0] != null)
            {
                rightPortals[0].TryMoveTowards(targetX, portalMoveSpeed, portalMoveDuration);
            }

            // Sắp xếp lại sau khi di chuyển (phòng khi portal vượt qua nhau)
            SortPortals();
        }

        /// <summary>
        /// Spawn thêm 2 portal phía sau (1 trái, 1 phải) với offset cố định.
        /// Chỉ gọi khi tạo world mới (không có map.json).
        /// </summary>
        public void SpawnBackPortals()
        {
            GameObject townCenter = Utility.GetTownCenter();
            if (townCenter == null) return;

            float centerX = townCenter.transform.position.x;

            // Spawn portal trái phía sau (further left)
            if (leftPortals.Count > 0 && leftPortals.Count < maxPortalsPerSide)
            {
                float nearestLeftX = leftPortals[0].transform.position.x;
                float backLeftX = nearestLeftX - backPortalSpawnOffset;
                SpawnPortalAtPosition(backLeftX, isRightSide: false);
            }

            // Spawn portal phải phía sau (further right)
            if (rightPortals.Count > 0 && rightPortals.Count < maxPortalsPerSide)
            {
                float nearestRightX = rightPortals[0].transform.position.x;
                float backRightX = nearestRightX + backPortalSpawnOffset;
                SpawnPortalAtPosition(backRightX, isRightSide: true);
            }
        }

        /// <summary>
        /// Spawn portal tại vị trí X cụ thể.
        /// </summary>
        private void SpawnPortalAtPosition(float xPos, bool isRightSide)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.summoningPortal == null)
            {
                Debug.LogError("[EnemyPortalManager] Không tìm thấy summoningPortal prefab!");
                return;
            }

            GameObject townCenter = Utility.GetTownCenter();
            float yPos = townCenter != null ? townCenter.transform.position.y : 0f;
            Vector3 spawnPos = new Vector3(xPos, yPos, 0f);

            GameObject newPortal = Instantiate(PrefabData.Instance.summoningPortal, spawnPos, Quaternion.identity);
            newPortal.name = isRightSide ? "EnemyPortal_Right_Back" : "EnemyPortal_Left_Back";

            if (EntitySaveManager.Instance != null && EntitySaveManager.Instance.enemyContainer != null)
            {
                newPortal.transform.SetParent(EntitySaveManager.Instance.enemyContainer.transform, true);
            }

            var behavior = newPortal.GetComponent<EnemyPortalBehavior>();
            if (behavior)
            {
                behavior.Setup(_enemyPool, _enemyDataProvider, _timeProvider);
                // Back portal không phải tuyến đầu nên không kích hoạt ngay
                // Sẽ tự động được đăng ký vào Start()
            }

            Debug.Log($"[EnemyPortalManager] Đã tạo portal phía sau {(isRightSide ? "PHẢI" : "TRÁI")} tại x={xPos:F1}");
        }

        #endregion

        /// <summary>
        /// Hàm dành riêng cho WaveManager để lấy list cổng đã sẵn sàng đẻ quái.
        /// Chỉ trả về portal tuyến đầu đang active.
        /// </summary>
        public List<EnemyPortalBehavior> GetActivePortals()
        {
            return GetFrontlineActivePortals();
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
                _timeProvider.UnsubscribeFromHourlyEvent(6, ProcessFrontlineDelays);
                _timeProvider.UnsubscribeFromHourlyEvent(19, MovePortalsTowardsTownCenter);
            }
        }
    }
}
