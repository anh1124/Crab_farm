using System;
using System.Collections.Generic;
using Script.Game.Enemy;
using Script.Game.Enemy.Core;
using UnityEngine;
using Script.Manager.WorldManager;
using Script.Entities.Structures.SummondPortal;

namespace Script.Manager.GamePlayManager
{
    [System.Serializable]
    public class EnemyWaveData
    {
        public TotalEnemyList.EnemyDataBase enemyData;
        public int count;
    }

    public class EnemyWaveManager : MonoBehaviour
    {
        public static EnemyWaveManager Instance { get; private set; }

        private IEnemyDataProvider _enemyDataProvider;
        private IEnemyPool _enemyPool;
        private IGameTimeProvider _timeProvider;

        public void Setup(IEnemyDataProvider dataProvider, IEnemyPool pool, IGameTimeProvider time)
        {
            _enemyDataProvider = dataProvider;
            _enemyPool = pool;
            _timeProvider = time;

            if (Utility.IsTutorialScene())
            {
                Debug.Log("[EnemyWaveManager] Tutorial scene detected. Wave triggering disabled.");
                return;
            }

            _timeProvider = time;

            if (_timeProvider != null)
            {
                _timeProvider.SubscribeToHourlyEvent(GameTimeManager.NIGHT_HOUR, TriggerNightWave);
            }
        }

        private int currentWavePower;
        
        [Header("Wave Power Balancing")]
        [SerializeField] private int initialPower = 5;
        [SerializeField] private int powerIncreaseInterval = 2; // Cứ mỗi X ngày tăng thêm power
        [SerializeField] private int powerIncreaseAmount = 5;
        [SerializeField] private int destroyedPortalPowerPenalty = 5;

        [Header("Storm Invasion - Extra Power")]
        [Tooltip("Power cộng thêm khi portal bị phá (cộng dồn mỗi khi có portal bị phá)")]
        [SerializeField] private int extraPower = 0;

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
            // Initialization handled via Setup in DI flow
        }

        private void TriggerNightWave()
        {
            if (Utility.IsTutorialScene()) return;
            if (_enemyPool == null || _enemyDataProvider == null || _timeProvider == null)
            {
                Debug.LogError("[EnemyWaveManager] Dependencies not set! Call Setup first.");
                return;
            }

            // Nếu chưa có TownCenter hoặc TownCenter tier < 1 thì không sinh wave
            if (Script.Utility.GetTownCenterTier() < 1)
            {
                Debug.Log("[EnemyWaveManager] Chưa có TownCenter hoặc Tier < 1, hủy sinh wave đêm nay.");
                return;
            }

            _enemyPool.InitializePools();

            // log kiểm tra trạng thái toàn bộ cổng hiện có trên Map trước khi đẻ quân
            var portalManager = FindObjectOfType<EnemyPortalManager>();
            if (portalManager == null) return;

            // Log tất cả portal hiện có
            var activePortalsForLog = portalManager.GetActivePortals();
            Debug.Log($"[EnemyWaveManager] Kích hoạt Wave Đêm. Số cổng sẵn sàng: {activePortalsForLog.Count}");

            // Hỏi EnemyPortalManager để lấy các cổng đã kích hoạt xong delay
            var activePortals = portalManager.GetActivePortals();
            if (activePortals.Count == 0)
            {
                Debug.LogWarning("[EnemyWaveManager] Không có portal nào SẴN SÀNG đêm nay. Bỏ qua spawn wave.");
                return;
            }

            // Tính toán power cho wave dựa theo ngày cấu hình
            int daysElapsed = _timeProvider.GetElapsedDays();
            int displayDay = daysElapsed + 1; // GameDay hiển thị (1-based)
            currentWavePower = CalculateWavePower(daysElapsed);
            int basePowerForLog = initialPower + (daysElapsed / powerIncreaseInterval) * powerIncreaseAmount;
            StartWaveTracking(currentWavePower); // [DEBUG] Khởi tạo track
            Debug.Log($"[EnemyWaveManager] [DEBUG] Đêm GameDay={displayDay} (daysElapsed={daysElapsed}) với tổng Power: {currentWavePower} | ExtraPower: {extraPower} | Base: {basePowerForLog}");

            // Tạo danh sách các enemy cho trận chiến
            var selectedMonsters = _enemyDataProvider.SelectMonstersByPower(currentWavePower);
            Debug.Log($"[EnemyWaveManager] [DEBUG] SelectMonstersByPower({currentWavePower}) trả về {selectedMonsters.Count} loại enemy");
            
            // Map danh sách tuples vừa bốc thành list Serialize chuyên dụng
            List<EnemyWaveData> finalWaveEnemies = new List<EnemyWaveData>();
            int totalEnemyCount = 0;
            foreach (var monster in selectedMonsters)
            {
                finalWaveEnemies.Add(new EnemyWaveData
                {
                    enemyData = monster.Data,
                    count = monster.Quantity
                });
                totalEnemyCount += monster.Quantity;
                Debug.Log($"[EnemyWaveManager] [DEBUG] - {monster.Data.enemyName}: {monster.Quantity} con (power: {monster.Data.power})");
            }
            
            if (totalEnemyCount == 0)
            {
                Debug.LogWarning($"[EnemyWaveManager] [DEBUG] Không có enemy nào được chọn! Power={currentWavePower}, EnemyList.Count={selectedMonsters.Count}");
            }

            // Với mỗi môn đệ cổng, giao danh sách con và mồi nút Spawn cho nó chạy riêng rẽ
            foreach (var behavior in activePortals)
            {
                if (behavior != null)
                {
                    // Gọi hàm StartSpawning trực tiếp từ Entity Behavior
                    behavior.StartSpawning(finalWaveEnemies);
                    
                    Debug.Log($"[EnemyWaveManager] Cổng tại {behavior.transform.position} đã kích hoạt bắt đầu xuất quân.");
                }
            }
        }

        private int CalculateWavePower(int daysElapsed)
        {
            // Cấu hình mới: dùng tham số thay vì magic number
            int basePower = initialPower + (daysElapsed / powerIncreaseInterval) * powerIncreaseAmount;
            
            // Cộng thêm extraPower từ việc phá portal
            int totalPower = basePower + extraPower;
            Debug.Log($"[EnemyWaveManager] CalculateWavePower: daysElapsed={daysElapsed}, Base={basePower}, Extra={extraPower}, Total={totalPower}");
            return totalPower;
        }

        public List<EnemyWaveData> currentWaveEnemies = new List<EnemyWaveData>();

        #region Storm Invasion - Extra Power Management
        /// <summary>
        /// Được gọi khi một portal bị phá. Cộng thêm số ngày hiện tại vào extraPower.
        /// </summary>
        public void AddExtraPowerFromDestroyedPortal()
        {
            // 1 portal bị phá tăng cố định power penalty
            extraPower += destroyedPortalPowerPenalty;
            
            Debug.Log($"[EnemyWaveManager] Portal bị phá! Cộng thêm {destroyedPortalPowerPenalty} power. ExtraPower hiện tại: {extraPower}");
        }
        
        /// <summary>
        /// Lấy giá trị extraPower hiện tại (cho debug)
        /// </summary>
        public int GetExtraPower() => extraPower;
        
        /// <summary>
        /// Set extraPower (dùng khi load từ save)
        /// </summary>
        public void SetExtraPower(int value) => extraPower = value;
        #endregion
        [SerializeField] private int debugCurrentWavePower; // Hiển thị power thực tế của wave vừa tạo
        [SerializeField] private int debugRemainingPower;   // [DEBUG] Sức mạnh còn lại khi đang spawn
        [SerializeField] private int debugTotalSpawnedEnemies; // [DEBUG] Tổng số enemy đã spawn trong wave

        /// <summary>
        /// [DEBUG] Được gọi mỗi khi một enemy được spawn để cập nhật debug context.
        /// </summary>
        public void OnEnemySpawned(int enemyPower)
        {
            debugRemainingPower = Mathf.Max(0, debugRemainingPower - enemyPower);
            debugTotalSpawnedEnemies++;
        }

        /// <summary>
        /// [DEBUG] Bắt đầu track wave mới.
        /// </summary>
        public void StartWaveTracking(int totalPower)
        {
            debugCurrentWavePower = totalPower;
            debugRemainingPower = totalPower;
            debugTotalSpawnedEnemies = 0;
        }

        [Header("Debug Settings")]
        [SerializeField] private int customWavePower = 10;

        [ContextMenu("Test Trigger Wave With Custom Power")]
        public void TestTriggerWaveWithCustomPower()
        {
            Debug.Log($"[EnemyWaveManager - DEV TEST] Bắt đầu gọi Wave với power {customWavePower}...");
            
            // Debug: Set time to night, spawn random portal, and activate it
            SetupDebugEnvironment(1);

            if (_enemyPool != null && _enemyDataProvider != null && _enemyPool.GetPoolCount() == 0)
            {
                _enemyPool.InitializePools();
            }

            var portalManager = FindObjectOfType<EnemyPortalManager>();
            if (portalManager == null) return;

            var activePortals = portalManager.GetActivePortals();
            if (activePortals == null || activePortals.Count == 0)
            {
                Debug.LogWarning("[EnemyWaveManager] Không có portal nào SẴN SÀNG. Bỏ qua spawn wave.");
                return;
            }

            currentWavePower = customWavePower;
            debugCurrentWavePower = currentWavePower;

            var selectedMonsters = _enemyDataProvider.SelectMonstersByPower(currentWavePower);
            
            List<EnemyWaveData> finalWaveEnemies = new List<EnemyWaveData>();
            foreach (var monster in selectedMonsters)
            {
                finalWaveEnemies.Add(new EnemyWaveData
                {
                    enemyData = monster.Data,
                    count = monster.Quantity
                });
            }

            foreach (var behavior in activePortals)
            {
                if (behavior != null)
                {
                    currentWaveEnemies = finalWaveEnemies;
                    behavior.StartSpawning(finalWaveEnemies);
                    Debug.Log($"[EnemyWaveManager] Cổng tại {behavior.transform.position} đã kích hoạt bắt đầu xuất quân (Custom Power: {customWavePower}).");
                }
            }
        }

        [ContextMenu("Test Trigger Night Wave (SPAWN THẬT) - Day 1")]
        public void TestTriggerNightWave()
        {
            Debug.Log("[EnemyWaveManager - DEV TEST] Bắt đầu gọi TriggerNightWave bằng tay (Day 1, Power=0)...");
            
            // Debug: Set time to night, spawn random portal, and activate it
            SetupDebugEnvironment(1);
            
            TriggerNightWave();
        }

        [ContextMenu("Test Trigger Night Wave (SPAWN THẬT) - Day 2 (Power=5)")]
        public void TestTriggerNightWaveDay2()
        {
            Debug.Log("[EnemyWaveManager - DEV TEST] Bắt đầu gọi TriggerNightWave bằng tay (GameDay 2, daysElapsed=1, Power=5)...");
            
            // Debug: Set time to day 2 night (daysElapsed=1 -> power=5), spawn random portal, and activate it
            SetupDebugEnvironment(2);
            
            TriggerNightWave();
        }

        private void SetupDebugEnvironment(int targetDay)
        {
            // 1. Set time to target day and night
            var timeManager = FindObjectOfType<GameTimeManager>();
            if (timeManager != null)
            {
                // Set day trước khi set night
                timeManager.SetDay(targetDay);
                timeManager.SetTimeToNight();
                Debug.Log($"[EnemyWaveManager - DEBUG] Đã chuyển thời gian về Day {targetDay} ban đêm (19:00).");
            }
            else
            {
                Debug.LogWarning("[EnemyWaveManager - DEBUG] Không tìm thấy GameTimeManager để set time về đêm.");
            }

            // 2. Spawn random portal
            var portalManager = FindObjectOfType<EnemyPortalManager>();
            if (portalManager != null)
            {
                portalManager.TestSpawnRandomPortal();
                Debug.Log("[EnemyWaveManager - DEBUG] Đã sinh portal ngẫu nhiên.");
            }
            else
            {
                Debug.LogWarning("[EnemyWaveManager - DEBUG] Không tìm thấy EnemyPortalManager để sinh portal.");
            }

            // 3. Set all portals active
            if (portalManager != null)
            {
                portalManager.SetAllPortalsActive();
                Debug.Log("[EnemyWaveManager - DEBUG] Đã kích hoạt tất cả portal.");
            }
        }

        private void OnDestroy()
        {
            if (_timeProvider != null)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(GameTimeManager.NIGHT_HOUR, TriggerNightWave);
            }
        }
    }
}
