using Script.Data_Model.Status;
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
    [RequireComponent(typeof(Script.Utils.DamageFlashEffect))]
    public class PlayerCombat : MonoBehaviour, IHealth, IDie, IDamageable, ICoinShield, IDeathDrop, IHealthReactive
    {
        private PlayerStatus status;
        private PlayerBehavior playerBehavior;

        // IHealthReactive events
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float> OnTakeDamageEvent;

        private void Awake()
        {
            status = GetComponent<PlayerStatus>();
            playerBehavior = GetComponent<PlayerBehavior>();
        }

        private void Start()
        {
            if (status != null && status.HP != null)
            {
                status.HP.OnChanged += HandleHPChanged;
            }
        }

        private void OnDestroy()
        {
            if (status != null && status.HP != null)
            {
                status.HP.OnChanged -= HandleHPChanged;
            }
        }

        private void HandleHPChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
            if (current <= 0 && !IsDead)
            {
                Die(null);
            }
        }

        public float CurrentHp 
        { 
            get => status.HP.Current; 
            set 
            {
                float diff = value - status.HP.Current;
                if (diff > 0) status.HP.Add(diff);
                else if (diff < 0) status.HP.Reduce(-diff);
            }
        }

        public float MaxHp 
        { 
            get => status.HP.Max; 
            set => status.HP.SetMax(value); 
        }

        public bool IsDead { get; private set; }
        
        // IHealth
        public bool HasHp => CurrentHp > 0;

        // ICoinShield
        public bool HasCoinShield => playerBehavior.Coin > 0;

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            status.HP.Add(amount);
        }

        public void Die()
        {
            Die(null);
        }

        // IDamageable
        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0 || IsDead) return;

            // Ưu tiên dùng Coin đỡ đòn
            if (HasCoinShield && playerBehavior.Coin > 0)
            {
                playerBehavior.SubtractCoin(1);
                DropCoin(instigator);
                OnTakeDamageEvent?.Invoke(damage);
                return;
            }

            // Hết Coin thì trừ máu
            if (HasHp)
            {
                status.HP.Reduce(damage);
                OnTakeDamageEvent?.Invoke(damage);
                if (status.HP.Current <= 0)
                {
                    Die(instigator);
                }
            }
        }

        public bool TakeDamage(float damage, GameObject whoDoDame)
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
            
            // Move AudioListener to Camera to keep hearing sounds after player is deactivated
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }

            if (Script.GUI_Controller.GameOverHandler.Instance != null)
            {
                Script.GUI_Controller.GameOverHandler.Instance.TriggerGameOver();
            }
            else
            {
                Debug.LogWarning("[PlayerCombat] GameOverHandler.Instance is null! Please add GameOverHandler to the scene.");
            }
            
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
