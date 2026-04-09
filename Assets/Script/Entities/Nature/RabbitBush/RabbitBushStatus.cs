using System;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Manager;
using UnityEngine;
using Random = UnityEngine.Random;
using Script.Manager.WorldManager;

namespace Script.Game.Other.Nature
{
    public class RabbitBushStatus: MonoBehaviour
    {
        // Lớp lưu trữ dữ liệu RabbitBushStatus
        [Serializable]
        public class SaveRabbitBush
        {
            public float positionX;
            public float positionY;
            public List<SaveRabbit> rabbits;
        }

        // Lớp lưu trữ dữ liệu RabbitStatus
        [Serializable]
        public class SaveRabbit
        {
            public int hp;
            public float positionX;
            public float positionY;
        }

        //tạo hệ thống thời gian rồi làm tiếp cái này
        
        
        public GameObject rabbit;
        public int maxRabbitLimit;
        public List<GameObject> rabbitList; 
        
        private void Start()
        {
            rabbit = PrefabData.Instance.rabbit;
            maxRabbitLimit = PrefabData.Instance.maxRabbitLimit;
           
            // Khởi tạo danh sách thỏ
            rabbitList = new List<GameObject>();
          
        }
        private void OnEnable()
        {
            // Đăng ký sự kiện của GameTimeManager
            var gtm = FindObjectOfType<GameTimeManager>();
            if (gtm != null) gtm.SubscribeToHourlyEvent(6, SpawnRandomRabbits);
        }
        private void OnDisable()
        {
            var gtm = FindObjectOfType<GameTimeManager>();
            if (gtm != null) gtm.UnsubscribeFromHourlyEvent(6, SpawnRandomRabbits);
        }
        // Hàm sinh thỏ và thêm vào danh sách
        public void SpawnRandomRabbits()
        {
            // Random số lượng thỏ sẽ sinh từ 1 đến 3, nhưng không vượt quá maxRabbitLimit
            int rabbitsToSpawn = Mathf.Min(Random.Range(1, 4), maxRabbitLimit - rabbitList.Count);

            for (int i = 0; i < rabbitsToSpawn; i++)
            {
                Vector2 spawnPosition = new Vector2(transform.position.x, 0); // Vị trí sinh thỏ gần RabbitBushStatus

                // Tạo thỏ mới
                GameObject spawnedRabbit = Instantiate(rabbit, spawnPosition, Quaternion.identity);
                spawnedRabbit.GetComponent<RabbitStatus>().rabbitBush = gameObject; // Gán tham chiếu RabbitBushStatus cho thỏ

                rabbitList.Add(spawnedRabbit); // Thêm thỏ vào danh sách
            }
        }
        public void DestroyBush()
        {
            // Hủy tất cả thỏ trong danh sách
            foreach (GameObject rabbit in rabbitList)
            {
                Destroy(rabbit); // Xóa từng con thỏ
            }
            rabbitList.Clear(); // Dọn danh sách thỏ
            Destroy(gameObject); // Hủy chính RabbitBushStatus
        }
        private void SpawnRabbits(int numberOfRabbits)
        {
            int rabbitsToSpawn = Mathf.Min(numberOfRabbits, maxRabbitLimit - rabbitList.Count); // Đảm bảo không vượt quá giới hạn

            for (int i = 0; i < rabbitsToSpawn; i++)
            {
                // Vị trí sinh thỏ ở vị trí y = 0
                Vector2 spawnPosition = new Vector2(transform.position.x, 0);

                // Dùng Spawner để sinh thỏ
                var esm = FindObjectOfType<EntitySaveManager>();
                if (esm != null) esm.InstanceAndMoveObjectToContainer(rabbit, spawnPosition);

                // Lưu thỏ vào danh sách
                GameObject spawnedRabbit = Instantiate(rabbit, spawnPosition, Quaternion.identity);
                rabbitList.Add(spawnedRabbit);
            }
        }

        // Hàm để kiểm tra và xử lý khi thỏ chết hoặc biến mất
        public void RabbitDied(GameObject deadRabbit)
        {
            // Xóa thỏ khỏi danh sách
            if (rabbitList.Contains(deadRabbit))
            {
                rabbitList.Remove(deadRabbit);
                Destroy(deadRabbit); // Hủy thỏ
            }
        }
     
        
    }
    
}