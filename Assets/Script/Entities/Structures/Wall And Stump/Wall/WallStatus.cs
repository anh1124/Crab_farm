using System;
using UnityEngine;
using Script.Game.Enemy.Interfaces;

namespace Script.Data_Model.Status
{
    // Chỉ chứa data và state của Wall, không có logic behavior
    
    public class WallStatus : MonoBehaviour, IHealth
    {
        [Header("Basic Info")]
        public int tier;
        public WallState state;
        
        [Header("Health System")]
        public int maxHp;
        public int currentHp;
        //2 biến dưới dùng để làm cột mộc khi reset maxhp
        public int normalMaxHp;//máu tối đa chưa ban phước
        public int blessedMaxHp;//máu tối đa đã ban phước

        // Events để Behavior lắng nghe
        public Action<int> OnDamaged;
        public Action<int> OnHealed;
        public Action OnDeath;

        // --- IHealth Implementation ---
        public int CurrentHp { get => currentHp; set => currentHp = Mathf.Clamp(value, 0, MaxHp); }
        public int MaxHp { get => maxHp; set => maxHp = value; }

        public void TakeDamage(int amount)
        {
            if (currentHp <= 0) return;
            currentHp -= amount;
            OnDamaged?.Invoke(amount);
            if (currentHp <= 0)
            {
                currentHp = 0;
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            OnHealed?.Invoke(amount);
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