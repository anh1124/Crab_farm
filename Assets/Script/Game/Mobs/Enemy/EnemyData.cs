using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyData : MonoBehaviour
    {
        public GameObject greed;
        public GameObject circleHead;
        
        public GameObject shieldBot;
        public GameObject twoHeadBot;

        public GameObject stormHead;
        public GameObject toucher;
        
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
            public string description;
            public int power;//sử dụng để tích chiến lực,dẫn đến số lượng loại này sẽ spawn ra
        }

        public List<EnemyDataBase> EnemyDataList = new List<EnemyDataBase>();

        private void InitializeEnemyData()
        {
        
            // Thêm dữ liệu cho Greed
            EnemyDataList.Add(new EnemyDataBase
            {
                id =1,
                enemyName = "greed",
                attackSpeed = 2,//2s húc tường 1 lần
                dame = 1,
                hp = 2,
                speed = 4,
                enemyPrefab = greed,
                description = "kẻ ăn trộm phổ biến,tấn công player để lấy vàng và rời đi",
                power=1
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =2,
                enemyName = "circleHead",
                attackSpeed = 4,//4s phóng sét 1 lần ,mỗi lần 5 lần giật và hết sạch điện,phải đợi để hồi
                dame = 1,
                hp = 4,
                speed = 2,
                enemyPrefab = circleHead,
                description = "Sạc không dây di động,giúp tăng tốc độ tích năng lượng cho shieldBot gần đó ",
                power=5
            });
            /*
            EnemyDataList.Add(new EnemyDataBase
            {
                id =3,
                enemyName = "shieldBot",
                attackSpeed = 0,
                //chúng không tấn công chỉ di chuyển đến sát tường
                //và phát nổ sau khi chết ,và tạo khiên bằng chính hp của nó
                dame = 9,//dame nổ khi chết ,gây dame khu vực, x2 dame cho tường 
                hp = 15,
                speed = 1,
                enemyPrefab = shieldBot,
                description = "tạo khiên",
                power=15
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =4,
                enemyName = "stormHead",
                attackSpeed = 0.5f,//điều khiển cái đầu bay lên giật sét
                dame = 3,
                hp = 10,
                speed = 1,//khi đến đủ gần bức tường sẽ không tiến lên nữa 
                enemyPrefab = stormHead,
                description = "điều khiển đầu bay,đầu có hp bằng 20%hp ,tạo mới đầu mỗi 5s",
                power=40
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =5,
                enemyName = "twoHeadBot",
                attackSpeed = 1,//quật về phía trước,nhanh nhưng yếu
                dame = 1,
                hp = 30,//sau khi chết sẽ dịch chuyển lại phía sau với nửa hp
                speed = 3,
                enemyPrefab = twoHeadBot,
                description = "khi tử vong sẽ nổ 1 cái đầu để lùi lại,hồi 50%hp tối đa",
                power=25
            });
            EnemyDataList.Add(new EnemyDataBase
            {
                id =6,
                enemyName = "toucher",
                attackSpeed = 2,
                dame = 1,//sát thương lửa
                hp = 20,//mỗi lần tấn công sẽ tự trừ đi 10%hp của bản thân nếu curentHP>5
                speed = 3,
                enemyPrefab = toucher,
                description = "khi tử vong sẽ tạo 1 bãi lửa dưới xác,gây sát thương toàn bộ",
                power=25
            });
            */
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