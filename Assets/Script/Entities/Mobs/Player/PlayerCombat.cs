using Script.Data_Model.Status;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Game.Player
{
    /// <summary>
    /// PlayerCombat quản lý logic sinh tồn và chiến đấu của Player.
    /// Bao gồm: Health, Nhận sát thương, Hồi máu, Chết, Khiên bằng Coin và tấn công cơ bản.
    /// Tách rời khỏi PlayerBehavior.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    [RequireComponent(typeof(PlayerBehavior))]
    public class PlayerCombat : MonoBehaviour, IHealth, IDie, IDamageable, ICoinShield, IDeathDrop
    {
        private PlayerStatus status;
        private PlayerBehavior playerBehavior;

        private void Awake()
        {
            status = GetComponent<PlayerStatus>();
            playerBehavior = GetComponent<PlayerBehavior>();
        }

        public int CurrentHp 
        { 
            get => status.currentHp; 
            set => status.currentHp = value; 
        }

        public int MaxHp 
        { 
            get => status.maxHp; 
            set => status.maxHp = value; 
        }

        public bool IsDead { get; private set; }
        
        // IHealth
        public bool HasHp => CurrentHp > 0;

        // ICoinShield
        public bool HasCoinShield => playerBehavior.Coin > 0;

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, null);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHp += amount;
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
        }

        public void Die()
        {
            Die(null);
        }

        // IDamageable
        public void ReceiveDamage(int damage, GameObject instigator)
        {
            if (damage <= 0 || IsDead) return;

            // Ưu tiên dùng Coin đỡ đòn
            if (HasCoinShield && playerBehavior.Coin > 0)
            {
                playerBehavior.SubtractCoin(1);
                DropCoin(instigator);
                return;
            }

            // Hết Coin thì trừ máu
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(instigator);
                }
            }
        }

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            ReceiveDamage(damage, whoDoDame);
            return !IsDead;
        }

        // IDie
        public void Die(GameObject whoDoDame)
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            DropOnDeath();
            gameObject.SetActive(false);
        }

        public int CurrentCoin 
        { 
            get => playerBehavior.Coin; 
            set => playerBehavior.Coin = value; 
        }

        public bool ConsumeCoinShield()
        {
            if (!HasCoinShield) return false;
            playerBehavior.SubtractCoin(1);
            return true;
        }

        public void DropCoin(GameObject whoDoDame)
        {
            // Logic rơi coin khi bị mất shield
        }

        // IDeathDrop
        public void DropOnDeath()
        {
            // Logic rơi đồ khi chết
        }
    }
}
