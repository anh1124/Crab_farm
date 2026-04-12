using System;
using UnityEngine;
using UnityEngine.Serialization;
using Script.Interfaces;

namespace Script.Data_Model.Status
{
    [Serializable]
    public class TownCenterStatus : MonoBehaviour, IHealth, IHealthReactive
    {
        public int tier;
        public int upgradeCost;
        public bool readyToUpgrade;
        public int buildProgress;
        public int buildProgressMax;

        [Header("Health")]
        public float maxHp = 0f; // Will be set by tier-based data (tier * 25)
        [SerializeField] private float _currentHp = 0f;

        /// <summary>Giữ cho TownCenterBehavior và code cũ.</summary>
        public event Action<float, float> OnHpChanged;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnTakeDamageEvent;

        public float currentHp
        {
            get => _currentHp;
            set
            {
                float prev = _currentHp;
                _currentHp = Mathf.Clamp(value, 0, maxHp);
                if (Mathf.Approximately(prev, _currentHp)) return;
                float damageDealt = prev > _currentHp ? prev - _currentHp : -1f;
                RaiseHpChanged(damageDealt);
            }
        }

        float IHealth.CurrentHp
        {
            get => _currentHp;
            set => currentHp = value;
        }

        float IHealth.MaxHp
        {
            get => maxHp;
            set
            {
                float next = Mathf.Max(0f, value);
                if (Mathf.Approximately(maxHp, next)) return;
                maxHp = next;
                _currentHp = Mathf.Clamp(_currentHp, 0f, maxHp);
                RaiseHpChanged(-1f);
            }
        }

        void IHealth.TakeDamage(float amount)
        {
            if (amount <= 0f || _currentHp <= 0f) return;
            currentHp -= amount;
        }

        void IHealth.Heal(float amount)
        {
            if (amount <= 0f) return;
            currentHp += amount;
        }

        void IHealth.Die()
        {
            currentHp = 0f;
        }

        private void RaiseHpChanged(float damageAmountForUi)
        {
            OnHpChanged?.Invoke(_currentHp, maxHp);
            OnHealthChanged?.Invoke(_currentHp, maxHp);
            if (damageAmountForUi >= 0f)
                OnTakeDamageEvent?.Invoke(damageAmountForUi);
        }

        [ContextMenu("Trừ 100 HP")]
        public void Deduct100HP()
        {
            currentHp -= 100f;
            Debug.Log($"[TownCenterStatus] Đã trừ 100 HP. HP hiện tại: {currentHp}/{maxHp}");
        }
    }
}
