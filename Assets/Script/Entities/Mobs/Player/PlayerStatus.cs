using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using System;
using UnityEngine;

namespace Script.Data_Model.Status
{
    // Data component cho Player (MonoBehaviour, khởi tạo qua AddComponent)
    public class PlayerStatus: MonoBehaviour
    {
        // Event bắn ra mỗi khi số coin thay đổi — UI lắng nghe event này
        public event Action<int> OnCoinChanged;

        // Event bắn ra mỗi khi HP thay đổi (currentHp, maxHp)
        public event Action<int, int> OnHPChanged;

        // currentHp và maxHp dùng property để tự động bắn event OnHPChanged
        private int _currentHp;
        public int currentHp
        {
            get => _currentHp;
            set
            {
                if (_currentHp == value) return;
                _currentHp = value;
                OnHPChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        private int _maxHp;
        public int maxHp
        {
            get => _maxHp;
            set
            {
                if (_maxHp == value) return;
                _maxHp = value;
                OnHPChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        public int maxCoins;
        public float moveSpeed;
        public float positionX;
        public float positionY;
        public int attackDame;
        public float attackSpeed;
        public bool isGround;

        // Coin sử dụng property để tự động bắn event khi thay đổi
        private int _coin;
        public int coin
        {
            get => _coin;
            set
            {
                if (_coin == value) return;
                _coin = value;
                OnCoinChanged?.Invoke(_coin);
            }
        }
        
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
