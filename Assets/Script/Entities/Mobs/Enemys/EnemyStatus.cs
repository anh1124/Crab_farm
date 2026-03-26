using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;
using Script.Data_Model.Interfaces;
using static Script.Game.Enemy.TotalEnemyList;

namespace Script.Game.Enemy
{
    public class EnemyStatus : MonoBehaviour, IHealth, IDie, ICoinable, IDamageable, ISaveable
    {
        [Header("Debug Stats")]
        [SerializeField] private float _currentHp;
        [field: SerializeField] public float MaxHp { get; set; }
        [SerializeField] private string _enemyID; // Lưu định danh loại enemy (shieldBot, circleHead, v.v.)

        // ICoinable implementation
        [field: SerializeField] public int Coin { get; set; }
        [field: SerializeField] public int MaxCoins { get; set; } = 999;

        public float CurrentHp 
        { 
            get => _currentHp; 
            set => _currentHp = value; 
        }

        public bool IsDead { get; private set; }

        // Stats từ data (Sẽ BỊ GHI ĐÈ bởi file JSON khi Enemy được sinh ra!)
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public float AttackRange { get; private set; } = 0.8f; // Mặc định cận chiến
        [field: SerializeField] public float AttackCooldown { get; private set; }
        [field: SerializeField] public float AttackDamage { get; private set; }
        public bool debugEnemy; // Biến kiểm soát debug cho Enemy

        // Flags quản lý khả năng hành động (Tận dụng để khóa khi đang dùng Skill hoặc bị choáng)
        [field: SerializeField] public bool CanMove { get; set; } = true;
        [field: SerializeField] public bool CanAction { get; set; } = true;
        public bool IsMoving { get; set; } = false;
        // --- COMBAT FLAGS (Hiện lên Inspector để soi lỗi) ---
        [field: SerializeField] public bool IsSkillCasting { get; set; } = false;
        [field: SerializeField] public bool SkillAllowsMovement { get; set; } = true;

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
            _enemyID = data.enemyName; // Lưu ID loại enemy từ database
            
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



        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHp -= amount;
            if (CurrentHp <= 0 && !IsDead)
            {
                Die();
            }
        }

        // --- ISaveable Implementation ---
        public string GetPrefabID() => _enemyID; 

        public object CaptureData()
        {
            return new EnemySaveData
            {
                enemyID = _enemyID,
                currentHp = _currentHp,
                posX = transform.position.x,
                posY = transform.position.y
            };
        }

        public void RestoreData(object data)
        {
            if (data is Newtonsoft.Json.Linq.JObject jObj)
            {
                data = jObj.ToObject<EnemySaveData>();
            }

            if (data is EnemySaveData save)
            {
                _enemyID = save.enemyID;

                // QUAN TRỌNG: Re-setup stats cơ bản từ TotalEnemyList dựa trên ID trước khi áp dụng máu hiện tại
                if (TotalEnemyList.Instance != null && TotalEnemyList.Instance.EnemyDataList != null)
                {
                    var baseData = TotalEnemyList.Instance.EnemyDataList.Find(d => d.enemyName == _enemyID);
                    if (baseData != null)
                    {
                        Setup(baseData);
                        if (debugEnemy) Debug.Log($"[EnemyStatus] Restored base stats for {_enemyID} from TotalEnemyList");
                    }
                }

                _currentHp = save.currentHp;
                transform.position = new Vector3(save.posX, save.posY, 0);
                
                // Cập nhật lại flag chết dựa trên hp
                IsDead = (_currentHp <= 0);
            }
        }

        [System.Serializable]
        public class EnemySaveData
        {
            public string enemyID;
            public float currentHp;
            public float posX;
            public float posY;
        }

        // --- IDamageable Implementation ---
        public void ReceiveDamage(float damage, UnityEngine.GameObject instigator)
        {
            TakeDamage(damage);
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDieEvent?.Invoke(gameObject);
        }

        public void Heal(float amount)
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
