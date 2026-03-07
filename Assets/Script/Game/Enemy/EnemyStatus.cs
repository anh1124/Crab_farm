using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;
using Script.Data_Model.Interfaces;
using static Script.Game.Enemy.TotalEnemyList;

namespace Script.Game.Enemy
{
    public class EnemyStatus : MonoBehaviour, IHealth, IDie, ICoinable
    {
        [Header("Debug Stats")]
        [SerializeField] private int _currentHp;
        [field: SerializeField] public int MaxHp { get; set; }

        // ICoinable implementation
        [field: SerializeField] public int Coin { get; set; }
        [field: SerializeField] public int MaxCoins { get; set; } = 999;

        public int CurrentHp 
        { 
            get => _currentHp; 
            set => _currentHp = value; 
        }

        public bool IsDead { get; private set; }

        // Stats từ data (Sẽ BỊ GHI ĐÈ bởi file JSON khi Enemy được sinh ra!)
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public float AttackRange { get; private set; } = 0.8f; // Mặc định cận chiến
        [field: SerializeField] public float AttackCooldown { get; private set; }
        [field: SerializeField] public int AttackDamage { get; private set; }

        public event System.Action<GameObject> OnDieEvent;

        private void Awake()
        {
            // Tạm thời trống, các thiết lập sẽ được gọi qua Setup
        }

        // Gọi từ EnemyPool khi khởi tạo (hoặc tái sử dụng)
        public void Setup(EnemyDataBase data)
        {
            MaxHp = data.hp;
            CurrentHp = MaxHp;
            IsDead = false;
            
            MoveSpeed = data.speed;
            AttackCooldown = data.attackSpeed;
            AttackDamage = data.dame;
            
            // Lấy AttackRange từ Database nếu có config (fallback 0.8f nếu file JSON bị thiếu/ghi zero)
            if (data.attackRange > 0)
            {
                AttackRange = data.attackRange;
            }

            // Đồng bộ tầm nhìn xuống RayCastHelper để tia vẽ ra khớp với logic Combat
            RayCastHelper rch = GetComponent<RayCastHelper>();
            if (rch != null)
            {
                rch.raycastDistance = AttackRange;
            }
        }

       

        public void SubtractCoin(int amount)
        {
            Coin -= amount;
            if (Coin < 0) Coin = 0;
        }

        public void AddCoin(int amount)
        {
            Coin += amount;
            if (Coin > MaxCoins) Coin = MaxCoins;
        }

        public void PickUpCoin(GameObject coinGameObject)
        {
            AddCoin(1);
            if (coinGameObject != null) Destroy(coinGameObject);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            CurrentHp -= amount;
            if (CurrentHp <= 0 && !IsDead)
            {
                Die();
            }
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDieEvent?.Invoke(gameObject);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            
            CurrentHp += amount;
            if (CurrentHp > MaxHp)
            {
                CurrentHp = MaxHp;
            }
        }
    }
}
