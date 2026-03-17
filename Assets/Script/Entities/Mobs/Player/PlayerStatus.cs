using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;

namespace Script.Data_Model.Status
{
    // Data component cho Player (MonoBehaviour, khởi tạo qua AddComponent)
    public class PlayerStatus: MonoBehaviour
    {
        public int maxHp;
        public int currentHp;
        public int coin;
        public int maxCoins;
        public float moveSpeed;
        public float positionX;
        public float positionY;
        public int attackDame;
        public float attackSpeed;
        public bool isGround;
        
        [Header("Resources")]
        
        public float currentMana;
        public float maxMana;
        public float manaRegenRate;
        
        // Khởi tạo mặc định khi component được tạo runtime (AddComponent)
        private void Awake()
        {
            // Chỉ khởi tạo nếu chưa có giá trị (tránh ghi đè data đã load)
            if (maxMana == 0f)
            {
                maxMana = 50f;
                currentMana = 50f;
                manaRegenRate = 5f;
            }
            
            if (maxHp == 0)
            {
                maxHp = 100;
                currentHp = 100;
                moveSpeed = 10f;
                attackDame = 1;
                attackSpeed = 1f;
                maxCoins = 100;
            }
        }
        
        // Thiết lập mặc định khi thêm component trong Editor (không ghi đè runtime)
        private void Reset()
        {
            maxHp = 100;
            currentHp = 100;
            coin = 0;
            maxCoins = 100;
            moveSpeed = 12f;
            positionX = 0f;
            positionY = 0f;
            attackDame = 1;
            attackSpeed = 1f;
            isGround = false;
            
            // NEW: Initialize resources
            
            maxMana = 50f;
            currentMana = 50f;
            manaRegenRate = 5f;
        }

        [ContextMenu("Trừ 2 HP")]
        public void Deduct2HP()
        {
            currentHp -= 2;
            if (currentHp < 0) currentHp = 0;
            Debug.Log($"[PlayerStatus] Đã trừ 2  HP. HP hiện tại: {currentHp}/{maxHp}");
        }

        [ContextMenu("Tăng 10 Max HP")]
        public void Add10MaxHP()
        {
            maxHp += 10;
            Debug.Log($"[PlayerStatus] Đã tăng 10 Max HP. Max HP mới: {maxHp}");
        }

        [ContextMenu("Hồi đầy máu")]
        public void HealFull()
        {
            currentHp = maxHp;
            Debug.Log($"[PlayerStatus] Đã hồi đầy máu. HP hiện tại: {currentHp}/{maxHp}");
        }
    }
}
