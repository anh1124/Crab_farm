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

            if (_timeProvider != null)
            {
                _timeProvider.SubscribeToHourlyEvent(GameTimeManager.NIGHT_HOUR, TriggerNightWave);
            }
        }

        private int currentWavePower;

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

            GameObject pL = portalManager.portalL;
            GameObject pR = portalManager.portalR;
            
            string logL = pL != null ? $"[Cổng Trái Tồn Tại - Ready: {pL.GetComponent<EnemyPortalBehavior>().IsReadyToSpawn}]" : "[Cổng Trái NULL]";
            string logR = pR != null ? $"[Cổng Phải Tồn Tại - Ready: {pR.GetComponent<EnemyPortalBehavior>().IsReadyToSpawn}]" : "[Cổng Phải NULL]";
            Debug.Log($"[EnemyWaveManager] Kích hoạt Wave Đêm. Report Các Cổng: {logL} | {logR}");

            // Hỏi EnemyPortalManager để lấy các cổng đã kích hoạt xong delay
            var activePortals = portalManager.GetActivePortals();
            if (activePortals.Count == 0)
            {
                Debug.LogWarning("[EnemyWaveManager] Không có portal nào SẴN SÀNG đêm nay. Bỏ qua spawn wave.");
                return;
            }

            // Tính toán power cho wave dựa theo ngày cấu hình
            int daysElapsed = _timeProvider.GetElapsedDays();
            currentWavePower = CalculateWavePower(daysElapsed);
            debugCurrentWavePower = currentWavePower;
            Debug.Log($"[EnemyWaveManager] Đêm ngày {daysElapsed} với tổng Power: {currentWavePower}");

            // Tạo danh sách các enemy cho trận chiến
            var selectedMonsters = _enemyDataProvider.SelectMonstersByPower(currentWavePower);
            
            // Map danh sách tuples vừa bốc thành list Serialize chuyên dụng
            List<EnemyWaveData> finalWaveEnemies = new List<EnemyWaveData>();
            foreach (var monster in selectedMonsters)
            {
                finalWaveEnemies.Add(new EnemyWaveData
                {
                    enemyData = monster.Data,
                    count = monster.Quantity
                });
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
            // Công thức: power = day + day*5 = day*6
            return daysElapsed * 6;
        }

        public List<EnemyWaveData> currentWaveEnemies = new List<EnemyWaveData>();

        [Header("Debug Info")]
        [SerializeField] private int debugCurrentWavePower; // Hiển thị power thực tế của wave vừa tạo

        [Header("Debug Settings")]
        [SerializeField] private int customWavePower = 10;

        [ContextMenu("Test Trigger Wave With Custom Power")]
        public void TestTriggerWaveWithCustomPower()
        {
            Debug.Log($"[EnemyWaveManager - DEV TEST] Bắt đầu gọi Wave với power {customWavePower}...");
            
            // Debug: Set time to night, spawn random portal, and activate it
            SetupDebugEnvironment();

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

        [ContextMenu("Test Trigger Night Wave (SPAWN THẬT)")]
        public void TestTriggerNightWave()
        {
            Debug.Log("[EnemyWaveManager - DEV TEST] Bắt đầu gọi TriggerNightWave bằng tay...");
            
            // Debug: Set time to night, spawn random portal, and activate it
            SetupDebugEnvironment();
            
            TriggerNightWave();
        }

        private void SetupDebugEnvironment()
        {
            // 1. Set time to night
            var timeManager = FindObjectOfType<GameTimeManager>();
            if (timeManager != null)
            {
                timeManager.SetTimeToNight();
                Debug.Log("[EnemyWaveManager - DEBUG] Đã chuyển thời gian về ban đêm (19:00).");
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
