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

        [Header("Resources")]
        public ResourceStat HP = new ResourceStat();
        public ResourceStat Mana = new ResourceStat();

        public int maxCoins;
        public float moveSpeed;
        public float hpRegenRate;
        public float manaRegenRate;
        
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

        // Không có Awake() — PlayerBehavior.ApplyConfig() là nơi khởi tạo duy nhất
        // Không có Reset() — giá trị mặc định sống trong PlayerConfig ScriptableObject

        [ContextMenu("Trừ 2 HP")]
        public void Deduct2HP()
        {
            HP.Reduce(2f);
            Debug.Log($"[PlayerStatus] Đã trừ 2 HP. HP hiện tại: {HP.Current}/{HP.Max}");
        }

        [ContextMenu("Tăng 10 Max HP")]
        public void Add10MaxHP()
        {
            HP.SetMax(HP.Max + 10f);
            Debug.Log($"[PlayerStatus] Đã tăng 10 Max HP. Max HP mới: {HP.Max}");
        }

        [ContextMenu("Hồi đầy máu")]
        public void HealFull()
        {
            HP.Add(HP.Max);
            Debug.Log($"[PlayerStatus] Đã hồi đầy máu. HP hiện tại: {HP.Current}/{HP.Max}");
        }
    }
}
