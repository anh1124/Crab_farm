using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class TotalEnemyList : MonoBehaviour
    {

        public GameObject circleHead;
        
        public GameObject shieldBot;
        public GameObject twoHeadBot;

        public GameObject stormHead;
        public GameObject toucher;
        
        public static TotalEnemyList Instance { get; private set; }

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
            // Việc khởi tạo data sẽ được gọi thống nhất từ StartGameScrene
        }

        private bool _isInitialized = false;

        // Class cơ sở cho dữ liệu của mọi enemy
        [System.Serializable]
        public class EnemyDataBase
        {
            public int id;
            public string enemyName;
            public float attackSpeed;
            public float attackRange; // Được tự động parse từ file JSON
            public int dame;
            public int hp;
            public int speed;
            public GameObject enemyPrefab; // Not loaded from JSON
            public string description;
            public int power;//sử dụng để tích chiến lực,dẫn đến số lượng loại này sẽ spawn ra
        }

        [System.Serializable]
        public class EnemyDataConfigWrapper
        {
            public List<EnemyDataBase> enemies;
        }

        public List<EnemyDataBase> EnemyDataList = new List<EnemyDataBase>();

        public void InitializeEnemyData()
        {
            if (_isInitialized) return;

            // Load JSON from Resources
            TextAsset jsonFile = Resources.Load<TextAsset>("DataConfig/EnemyDataConfig");
            if (jsonFile == null)
            {
                Debug.LogError("Cannot find EnemyDataConfig.json in Resources/DataConfig");
                return;
            }

            EnemyDataConfigWrapper config = JsonUtility.FromJson<EnemyDataConfigWrapper>(jsonFile.text);

            if (config != null && config.enemies != null)
            {
                List<string> jsonEnemies = new List<string>();
                List<string> readyEnemies = new List<string>();

                foreach (var data in config.enemies)
                {
                    jsonEnemies.Add(data.enemyName);

                    // Map prefab based on name (chống sai case nhầm hoa/thường)
                    switch (data.enemyName.ToLower())
                    {
                        case "circlehead":
                            data.enemyPrefab = circleHead;
                            break;
                        case "shieldbot":
                            data.enemyPrefab = shieldBot;
                            break;
                        case "twoheadbot":
                            data.enemyPrefab = twoHeadBot;
                            break;
                        case "stormhead":
                            data.enemyPrefab = stormHead;
                            break;
                        case "toucher":
                            data.enemyPrefab = toucher;
                            break;
                        default:
                            break; // LogWarning is handled below if prefab is null
                    }

                    if (data.enemyPrefab != null)
                    {
                        readyEnemies.Add(data.enemyName);
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab not found for enemy: {data.enemyName}");
                    }

                    EnemyDataList.Add(data);
                }

                Debug.Log($"[EnemyData] Đã load {jsonEnemies.Count} enemy data từ JSON: {string.Join(", ", jsonEnemies)}");
                Debug.Log($"[EnemyData] Đã sẵn sàng {readyEnemies.Count} enemy (có cả data + prefab): {string.Join(", ", readyEnemies)}");
            }
            
            _isInitialized = true;
        }

        // Phương thức lấy dữ liệu enemy theo tên
        public EnemyDataBase GetEnemyDataByName(string enemyName)
        {
            return EnemyDataList.Find(data => data.enemyName == enemyName);
        }
        
        
        // Hàm chọn danh sách quái vật dựa trên maxPower
        public List<(EnemyDataBase Data, int Quantity)> SelectMonstersByPower(int maxPower)
        {
            int monterPowerReturn = 0;
            var selectedMonsters = new List<(EnemyDataBase Data, int Quantity)>();

            var sortedEnemies = EnemyDataList.OrderByDescending(e => e.power).ToList();
            int index = 0;

            while (monterPowerReturn < maxPower)
            {
                var enemy = sortedEnemies[index % sortedEnemies.Count];

                if (monterPowerReturn + enemy.power <= maxPower)
                {
                    monterPowerReturn += enemy.power;

                    var existingMonster = selectedMonsters.FirstOrDefault(m => m.Data == enemy);
                    if (existingMonster != default)
                    {
                        int i = selectedMonsters.FindIndex(m => m.Data == enemy);
                        selectedMonsters[i] = (enemy, existingMonster.Quantity + 1);
                    }
                    else
                    {
                        selectedMonsters.Add((enemy, 1));
                    }
                }

                index++;
            }

            // Disable internal logging as the caller handles it now
            // DisplaySelectedMonsters(selectedMonsters);
            return selectedMonsters;
        }
        public void DisplaySelectedMonsters(List<(EnemyDataBase Data, int Quantity)> selectedMonsters)
        {
            foreach (var monster in selectedMonsters)
            {
                Debug.Log($"Enemy: {monster.Data.enemyName}, Quantity: {monster.Quantity}");
            }
        }

        // ==========================================
        // DEBUG CHO INSPECTOR
        // ==========================================
        [ContextMenu("Debug: In Tình Trạng Data & Prefab")]
        public void DebugDataAndPrefabStatus()
        {
            if (EnemyDataList == null || EnemyDataList.Count == 0)
            {
                Debug.LogWarning("[TotalEnemyList] Danh sách Data trống! Hãy Play game để Initialize trước.");
                return;
            }

            List<string> readyEnemies = new List<string>();    // Có Data + Có Prefab
            List<string> missingEnemies = new List<string>();  // Có Data nhưng Thiếu Prefab

            foreach (var data in EnemyDataList)
            {
                if (data.enemyPrefab != null)
                {
                    readyEnemies.Add(data.enemyName);
                }
                else
                {
                    missingEnemies.Add(data.enemyName);
                }
            }

            // In ra Console
            Debug.Log("<color=green>===================================</color>");
            Debug.Log($"<color=green>1. ĐÃ SẴN SÀNG ({readyEnemies.Count} loại - Có Data + Có Prefab):</color>\n" + 
                      (readyEnemies.Count > 0 ? string.Join(", ", readyEnemies) : "Không có quái nào"));
            
            Debug.Log($"<color=orange>2. THIẾU PREFAB ({missingEnemies.Count} loại - Có Data nhưng chửa gán Prefab):</color>\n" + 
                      (missingEnemies.Count > 0 ? string.Join(", ", missingEnemies) : "Rất tốt, không thiếu quái nào!"));
            Debug.Log("<color=green>===================================</color>");
        }

    }
}