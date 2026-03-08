using System;
using System.Collections.Generic;
using Script.Game.Enemy;
using UnityEngine;
using Script.Manager.WorldManager;
using Script.Entities.Structures.SummondPortal;

namespace Script.Manager.GamePlayManager
{
    public class EnemyWaveManager : MonoBehaviour
    {
        public static EnemyWaveManager Instance { get; private set; }

        public int growthRate = 2; // Mỗi ngày tăng thêm 2 điểm power
        private int currentWavePower;
    
        private void Awake()
        {
            AddSingleton();
        }

        private void AddSingleton()
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
            // Đăng ký kích hoạt Wave mỗi đêm (19:00 PM)
            GameTimeManager.Instance.SubscribeToHourlyEvent(GameTimeManager.NIGHT_HOUR, TriggerNightWave);
        }

        private void TriggerNightWave()
        {
            // [FIX LỖI POOL]
            // Khi spawn bằng Inspector Test, có thể EnemyPool chưa được Initialize
            if (EnemyPool.Instance != null && TotalEnemyList.Instance != null && EnemyPool.Instance.GetPoolCount() == 0)
            {
                EnemyPool.Instance.InitializePools();
            }

            // log kiểm tra trạng thái toàn bộ cổng hiện có trên Map trước khi đẻ quân
            GameObject pL = EnemyPortalManager.Instance.portalL;
            GameObject pR = EnemyPortalManager.Instance.portalR;
            
            string logL = pL != null ? $"[Cổng Trái Tồn Tại - Ready: {pL.GetComponent<EnemyPortalBehavior>().IsReadyToSpawn}]" : "[Cổng Trái NULL]";
            string logR = pR != null ? $"[Cổng Phải Tồn Tại - Ready: {pR.GetComponent<EnemyPortalBehavior>().IsReadyToSpawn}]" : "[Cổng Phải NULL]";
            Debug.Log($"[EnemyWaveManager] Kích hoạt Wave Đêm. Report Các Cổng: {logL} | {logR}");

            // Hỏi EnemyPortalManager để lấy các cổng đã kích hoạt xong delay
            var activePortals = EnemyPortalManager.Instance.GetActivePortals();
            if (activePortals.Count == 0)
            {
                Debug.LogWarning("[EnemyWaveManager] Không có portal nào SẴN SÀNG đêm nay. Bỏ qua spawn wave.");
                return;
            }

            // Tính toán power cho wave dựa theo ngày cấu hình
            int daysElapsed = GameTimeManager.Instance.GetElapsedDays();
            currentWavePower = CalculateWavePower(daysElapsed);
            Debug.Log($"[EnemyWaveManager] Đêm ngày {daysElapsed} với tổng Power: {currentWavePower}");

            // Tạo danh sách các enemy cho trận chiến
            var selectedMonsters = TotalEnemyList.Instance.SelectMonstersByPower(currentWavePower);
            
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
                    // Copy list quái vào hệ thống global
                    currentWaveEnemies = finalWaveEnemies;
                    
                    // Gọi hàm StartSpawning trực tiếp từ Entity Behavior (SummondPortal đã bị hợp nhất)
                    behavior.StartSpawning();
                    
                    Debug.Log($"[EnemyWaveManager] Cổng tại {behavior.transform.position} đã kích hoạt bắt đầu xuất quân.");
                }
            }
        }

        private int CalculateWavePower(int daysElapsed)
        {
            // Công thức: power = basePower × (1 + growthRate/100)^daysElapsed
            return (int)(100 * Math.Pow(1 + (growthRate / 100f), daysElapsed) - 100);
        }

    
        
        // ------------------ CODE ĐÃ DECRACHED (Nút bấm manual inspector) -------- //
        
        public List<EnemyWaveData> currentWaveEnemies = new List<EnemyWaveData>();

        [Header("Debug Settings")]
        [SerializeField] private int customWavePower = 10;

        [ContextMenu("Test Trigger Wave With Custom Power")]
        public void TestTriggerWaveWithCustomPower()
        {
            Debug.Log($"[EnemyWaveManager - DEV TEST] Bắt đầu gọi Wave với power {customWavePower}...");
            
            if (EnemyPool.Instance != null && TotalEnemyList.Instance != null && EnemyPool.Instance.GetPoolCount() == 0)
            {
                EnemyPool.Instance.InitializePools();
            }

            var activePortals = EnemyPortalManager.Instance.GetActivePortals();
            if (activePortals == null || activePortals.Count == 0)
            {
                Debug.LogWarning("[EnemyWaveManager] Không có portal nào SẴN SÀNG. Bỏ qua spawn wave.");
                return;
            }

            currentWavePower = customWavePower;

            var selectedMonsters = TotalEnemyList.Instance.SelectMonstersByPower(currentWavePower);
            
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
                    behavior.StartSpawning();
                    Debug.Log($"[EnemyWaveManager] Cổng tại {behavior.transform.position} đã kích hoạt bắt đầu xuất quân (Custom Power: {customWavePower}).");
                }
            }
        }

        [ContextMenu("Test Trigger Night Wave (SPAWN THẬT)")]
        public void TestTriggerNightWave()
        {
            Debug.Log("[EnemyWaveManager - DEV TEST] Bắt đầu gọi TriggerNightWave bằng tay...");
            TriggerNightWave();
        }

        [ContextMenu("Test Spawn 1 ShieldBot")]
        public void TestSpawnShieldBot()
        {
            var activePortals = EnemyPortalManager.Instance.GetActivePortals();
            if (activePortals == null || activePortals.Count == 0)
            {
                Debug.LogError("[TestSpawnShieldBot] LỖI: Không tìm thấy Portal nào SẴN SÀNG trên bản đồ!");
                return;
            }

            GameObject portalObj = activePortals[0].gameObject;
            
            // Yêu cầu Pool cấp ShieldBot
            GameObject shieldBot = EnemyPool.Instance.GetEnemy("shieldBot");
            if (shieldBot != null)
            {
                shieldBot.transform.position = portalObj.transform.position;
                shieldBot.transform.rotation = Quaternion.identity;

                // Reset state machine thông qua Controller
                EnemyAIController controller = shieldBot.GetComponent<EnemyAIController>();
                if (controller != null)
                {
                    controller.ResetEnemy(portalObj);
                    Debug.Log($"[TestSpawnShieldBot] THÀNH CÔNG đẻ 1 ShieldBot từ pool tại cổng {portalObj.name}");
                }
                else
                {
                    Debug.LogError("[TestSpawnShieldBot] ShieldBot thiếu component EnemyAIController (hoặc ShieldBotAIController)!");
                }
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.gameObject)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(GameTimeManager.NIGHT_HOUR, TriggerNightWave);
            }
        }
    }

    [System.Serializable]
    public class EnemyWaveData
    {
        public TotalEnemyList.EnemyDataBase enemyData;
        public int count;
    }
}
