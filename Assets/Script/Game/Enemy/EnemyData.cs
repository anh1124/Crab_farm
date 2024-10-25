using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyData : MonoBehaviour
    {
        public GameObject greed;
        public GameObject breeder;
        public GameObject thunderBot;
        public GameObject shieldBot;
        public static EnemyData Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            InitializeEnemyData();
        }

        // Class cơ sở cho dữ liệu của mọi enemy
        public class EnemyDataBase
        {
            public int id;
            public string enemyName;
            public float attackSpeed;
            public int dame;
            public int hp;
            public int speed;
            public GameObject enemyPrefab;
            public int power;//sử dụng để tích chiến lực,dẫn đến số lượng loại này sẽ spawn ra
        }

        public List<EnemyDataBase> EnemyDataList = new List<EnemyDataBase>();

        private void InitializeEnemyData()
        {
        
            // Thêm dữ liệu cho Greed
            EnemyDataList.Add(new EnemyDataBase
            {
                id =1,
                enemyName = "Greed",
                attackSpeed = 2,//2s húc tường 1 lần
                dame = 1,
                hp = 2,
                speed = 4,
                enemyPrefab = greed,
                power=1
            });

            EnemyDataList.Add(new EnemyDataBase
            {
                id =2,
                enemyName = "ThunderBot",
                attackSpeed = 3,//giật 1 lần 5 lần sát thương xuyên mục tiêu mỗi 3s,
                dame = 1,
                hp = 5,
                speed = 2,
                enemyPrefab = thunderBot,
                power=6
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =3,
                enemyName = "ShieldBot",
                attackSpeed = 2,
                dame = 9,//dame nổ khi chết 
                hp = 15,
                speed = 5,
                enemyPrefab = breeder,
                power=15
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =4,
                enemyName = "Breeder",
                attackSpeed = 2,
                dame = 9,
                hp = 30,
                speed = 3,
                enemyPrefab = breeder,
                power=25
            });
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

            DisplaySelectedMonsters(selectedMonsters);
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