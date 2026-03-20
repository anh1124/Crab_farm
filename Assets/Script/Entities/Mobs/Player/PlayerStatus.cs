using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using System;
using UnityEngine;

namespace Script.Data_Model.Status
{
    /// <summary>
    /// Data container thuần túy cho Player runtime state.
    /// KHÔNG chứa logic khởi tạo — giá trị mặc định được quản lý bởi PlayerConfig.
    /// PlayerBehavior chịu trách nhiệm điều phối: load từ save → dùng Config mặc định.
    /// </summary>
    public class PlayerStatus : MonoBehaviour
    {
        // Event bắn ra mỗi khi số coin thay đổi — UI lắng nghe event này
        public event Action<int> OnCoinChanged;

        // Event bắn ra mỗi khi HP thay đổi (currentHp, maxHp)
        public event Action<float, float> OnHPChanged;

        // currentHp và maxHp dùng property để tự động bắn event OnHPChanged
        private float _currentHp;
        public float currentHp
        {
            get => _currentHp;
            set
            {
                if (Mathf.Approximately(_currentHp, value)) return;
                _currentHp = value;
                OnHPChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        private float _maxHp;
        public float maxHp
        {
            get => _maxHp;
            set
            {
                if (Mathf.Approximately(_maxHp, value)) return;
                _maxHp = value;
                OnHPChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        public int maxCoins;
        public float moveSpeed;
        public float hpRegenRate;
        
        // TODO: Chuyển isGround, positionX, positionY về PlayerMovement.
        // Các field này là physics state, không phải player resource.
        public float positionX;
        public float positionY;
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
        
        public event Action<float, float> OnManaChanged;

        [Header("Resources")]
        private float _currentMana;
        public float currentMana
        {
            get => _currentMana;
            set
            {
                if (Mathf.Approximately(_currentMana, value)) return;
                _currentMana = value;
                OnManaChanged?.Invoke(_currentMana, _maxMana);
            }
        }

        private float _maxMana;
        public float maxMana
        {
            get => _maxMana;
            set
            {
                if (Mathf.Approximately(_maxMana, value)) return;
                _maxMana = value;
                OnManaChanged?.Invoke(_currentMana, _maxMana);
            }
        }
        
        public float manaRegenRate;

        // Không có Awake() — PlayerBehavior.ApplyConfig() là nơi khởi tạo duy nhất
        // Không có Reset() — giá trị mặc định sống trong PlayerConfig ScriptableObject

        [ContextMenu("Trừ 2 HP")]
        public void Deduct2HP()
        {
            currentHp -= 2f;
            if (currentHp < 0) currentHp = 0;
            Debug.Log($"[PlayerStatus] Đã trừ 2 HP. HP hiện tại: {currentHp}/{maxHp}");
        }

        [ContextMenu("Tăng 10 Max HP")]
        public void Add10MaxHP()
        {
            maxHp += 10f;
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
