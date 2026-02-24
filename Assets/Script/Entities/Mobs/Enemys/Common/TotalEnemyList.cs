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

            InitializeEnemyData();
        }

        // Class cơ sở cho dữ liệu của mọi enemy
        [System.Serializable]
        public class EnemyDataBase
        {
            public int id;
            public string enemyName;
            public float attackSpeed;
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

        private void InitializeEnemyData()
        {
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
                foreach (var data in config.enemies)
                {
                    // Map prefab based on name
                    switch (data.enemyName)
                    {
                        case "circleHead":
                            data.enemyPrefab = circleHead;
                            break;
                        case "shieldBot":
                            data.enemyPrefab = shieldBot;
                            break;
                        case "twoHeadBot":
                            data.enemyPrefab = twoHeadBot;
                            break;
                        case "stormHead":
                            data.enemyPrefab = stormHead;
                            break;
                        case "toucher":
                            data.enemyPrefab = toucher;
                            break;
                        default:
                            Debug.LogWarning($"Prefab not found for enemy: {data.enemyName}");
                            break;
                    }
                    EnemyDataList.Add(data);
                }
            }
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

    }
}