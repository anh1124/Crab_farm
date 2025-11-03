using System;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;

namespace Script.Game.Player
{
    // Chứa logic gameplay của Player, sử dụng PlayerStatus để lưu trữ trạng thái
    
    //[TODO] thêm script kiểm tra script player đã đủ chưa
    
    public class PlayerBehavior : MonoBehaviour, IAttacker, ICoinable, ICoinShield, IDeathDrop, IHealth, ISaveable
    {
        [Header("Player Status Data")]
        public PlayerStatus status;

        [Serializable]
        public class PlayerStatusData
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
        }

        private void Awake()
        {
            if (status == null)
            {
                status = GetComponent<PlayerStatus>();
                if (status == null)
                {
                    status = gameObject.AddComponent<PlayerStatus>();
                }
            }
        }

        public float MoveSpeed { get => status.moveSpeed; set => status.moveSpeed = value; }
        public int AttackDame { get => status.attackDame; set => status.attackDame = value; }
        public float AttackSpeed { get => status.attackSpeed; set => status.attackSpeed = value; }
        public int MaxHp { get => status.maxHp; set => status.maxHp = value; }

     

        public void TakeDamage(int amount)
        {
            throw new System.NotImplementedException();
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public void Heal(int amount)
        {
            throw new System.NotImplementedException();
        }

        public bool IsDead { get; }
        public int CurrentHp { get => status.currentHp; set => status.currentHp = value; }
        public int Coin { get => status.coin; set => status.coin = value; }
        public void SubtractCoin(int amount)
        {
            if (amount <= 0) return;
            status.coin = Mathf.Max(0, status.coin - amount);
        }

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            status.coin += amount;
        }

        public void PickUpCoin(GameObject coinGameObject)
        {
            if (coinGameObject == null) return;
            AddCoin(1);
            Destroy(coinGameObject);
        }

        // Implement các interface khác nếu cần

        public void Attack()
        {
            // Logic tấn công
        }

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && Coin > 0)
            {
                Coin -= 1;
                if (Coin < 0) Coin = 0;
                DropCoin(whoDoDame);
                return true;
            }
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(whoDoDame);
                    return false;
                }
                return true;
            }
            return true;
        }

        public void Die(GameObject whoDoDame)
        {
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            gameObject.SetActive(false);
        }

        public int CurrentCoin { get; set; }
        public bool HasCoinShield => Coin > 0;

        public bool ConsumeCoinShield()
        {
            if (!HasCoinShield) return false;
            Coin -= 1;
            if (Coin < 0) Coin = 0;
            return true;
        }
        public bool HasHp => CurrentHp > 0;

        public void DropCoin(GameObject whoDoDame)
        {
            // Logic rơi coin khi bị tấn công
        }

        // ISaveable
        public string GetPrefabID() => "player";

        public object CaptureData()
        {
            return new PlayerStatusData
            {
                maxHp = status.maxHp,
                currentHp = status.currentHp,
                coin = status.coin,
                moveSpeed = status.moveSpeed,
                positionX = transform.position.x,
                positionY = transform.position.y,
                attackDame = status.attackDame,
                attackSpeed = status.attackSpeed,
                isGround = status.isGround
            };
        }

        public void RestoreData(object data)
        {
            PlayerStatusData loaded = data as PlayerStatusData;
            if (loaded != null)
            {
                status.maxHp = loaded.maxHp;
                status.currentHp = loaded.currentHp;
                status.coin = loaded.coin;
                status.moveSpeed = loaded.moveSpeed;
                status.attackDame = loaded.attackDame;
                status.attackSpeed = loaded.attackSpeed;
                status.isGround = loaded.isGround;
                transform.position = new Vector3(loaded.positionX, loaded.positionY, 0);
            }
        }

        public void DropOnDeath()
        {
            throw new System.NotImplementedException();
        }
    }
}