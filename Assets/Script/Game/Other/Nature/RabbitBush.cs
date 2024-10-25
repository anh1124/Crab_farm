using System;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Other.Nature
{
    public class RabbitBush: MonoBehaviour
    {
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

            // Bắt đầu sinh thỏ
            //SpawnRabbits();
        }
        // Hàm sinh thỏ và thêm vào danh sách
        private void SpawnRabbits()
        {
            for (int i = 0; i < maxRabbitLimit; i++)
            {
                // Luôn sinh thỏ ở vị trí y = 0
                Vector2 spawnPosition = new Vector2(transform.position.x, 0);

                // Dùng Spawner để sinh thỏ
                Spawner.Instance.SummonMods(rabbit, spawnPosition);

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