using System;
using UnityEngine;
using Script.Interfaces;
namespace Script.Data_Model.Status
{
    // Chỉ chứa data và state của Wall, không có logic behavior
    public class WallStatus : MonoBehaviour, IHealth, IHealthReactive
    {
        [Header("Basic Info")]
        public int tier;
        public WallState state;
        
        [Header("Health System")]
        public float maxHp;
        public float currentHp;
        //2 biến dưới dùng để làm cột mộc khi reset maxhp
        public float normalMaxHp;//máu tối đa chưa ban phước
        public float blessedMaxHp;//máu tối đa đã ban phước

        // Events để Behavior lắng nghe
        public Action<float> OnDamaged;
        public Action<float> OnHealed;
        public Action OnDeath;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnTakeDamageEvent;

        // --- IHealth Implementation ---
        public float CurrentHp
        {
            get => currentHp;
            set
            {
                float next = Mathf.Clamp(value, 0, MaxHp);
                if (Mathf.Approximately(currentHp, next)) return;
                float prev = currentHp;
                currentHp = next;
                OnHealthChanged?.Invoke(currentHp, maxHp);
                if (next < prev)
                    OnTakeDamageEvent?.Invoke(prev - next);
            }
        }

        public float MaxHp
        {
            get => maxHp;
            set
            {
                if (Mathf.Approximately(maxHp, value)) return;
                maxHp = value;
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);
                OnHealthChanged?.Invoke(currentHp, maxHp);
            }
        }

        public void TakeDamage(float amount)
        {
            if (currentHp <= 0) return;
            currentHp -= amount;
            OnDamaged?.Invoke(amount);
            OnTakeDamageEvent?.Invoke(amount);
            if (currentHp <= 0)
            {
                currentHp = 0;
                Die();
            }

            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void Heal(float amount)
        {
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            OnHealed?.Invoke(amount);
            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void Die()
        {
            OnDeath?.Invoke();
        }
        
        [Header("OnUpgradeComplete System")]
        public int upgradeCost;//số tiền cần cho level tiếp theo
        public bool readyToUpgrade;//wallmanager sẽ yêu cầu villagermanager đem thợ xây đến để dập khi đủ tiền
        public bool isMaxTier;//cấp tối đa,không hiện coin slot
        
        public int upgradeProgress;//tiến độ xây dựng hiện tại
        public int upgradeProgressMax;// cần đạt đến đây để nâng cấp
        
        [Header("Building Settings")]
        public float minimumDistance = 20f;//khoảng cách xây tối thiểu
        public int rebuildCost;//giá xây lại nếu bị phá
        
    }
    public enum WallState
    {
        Wall,
        Stump
    }

}