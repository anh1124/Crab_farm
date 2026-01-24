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
        public float moveSpeed;
        public float positionX;
        public float positionY;
        public int attackDame;
        public float attackSpeed;
        public bool isGround;
        
        [Header("Resources")]
        public float currentStamina;
        public float maxStamina;
        public float staminaRegenRate;
        
        public float currentMana;
        public float maxMana;
        public float manaRegenRate;
        
        // Khởi tạo mặc định khi component được tạo runtime (AddComponent)
        private void Awake()
        {
            // Chỉ khởi tạo nếu chưa có giá trị (tránh ghi đè data đã load)
            if (maxStamina == 0f)
            {
                maxStamina = 100f;
                currentStamina = 100f;
                staminaRegenRate = 10f;
                
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
            }
        }
        
        // Thiết lập mặc định khi thêm component trong Editor (không ghi đè runtime)
        private void Reset()
        {
            maxHp = 100;
            currentHp = 100;
            coin = 0;
            moveSpeed = 7f;
            positionX = 0f;
            positionY = 0f;
            attackDame = 1;
            attackSpeed = 1f;
            isGround = false;
            
            // NEW: Initialize resources
            maxStamina = 100f;
            currentStamina = 100f;
            staminaRegenRate = 10f;
            
            maxMana = 50f;
            currentMana = 50f;
            manaRegenRate = 5f;
        }
    }
}
