using UnityEngine;
using Script.Game.Enemy.Enemy_Type;
using Unity.VisualScripting;
using UnityEngine.Serialization;

namespace Script.Game.Enemy.Enemy_Type
{
    public class GreedAI : Base.Enemy
    {
        public int dame;
        public int hp;
        public int speed;
        
        [FormerlySerializedAs("CurrentState")] public AllState currentState;
    
        public enum AllState
        {
            Idle, // Di chuyển ngẫu nhiên
            Attack, // Tấn công theo hướng bên trái
            Run, // Chạy về cổng gần nhất
            Die // Enemy chết
        }

        private void Start()
        {
            InitializeData();
        }
        public void InitializeData()
        {
            name = "Greed";
            TotalEnemyList.EnemyDataBase data =TotalEnemyList.Instance.GetEnemyDataByName(name);
            if (data != null)
            {
                dame = data.dame;
                hp = data.hp;
                speed = data.speed;
            }
        }
        private void Update()
        {
            // Logic state machine cho Greed
            switch (currentState)
            {
                case AllState.Idle:
                    // Di chuyển ngẫu nhiên
                    break;
                
                case AllState.Attack:

                    // Tấn công tường hoặc công trình
                    break;

                case AllState.Run:
                    // Chạy về cổng gần nhất
                    break;

                case AllState.Die:
                    HandleDeath();
                    break;
            }
        }

        // Phương thức xử lý khi enemy chết
        private void HandleDeath()
        {
            // Chạy animation chết
     
            // Vô hiệu hóa collider hoặc các hành vi khác
            GetComponent<Collider2D>().enabled = false;
            // Tùy chọn: Destroy gameObject sau một khoảng thời gian
            Destroy(gameObject, 2f); // Xóa sau 2 giây
        }

    }
}