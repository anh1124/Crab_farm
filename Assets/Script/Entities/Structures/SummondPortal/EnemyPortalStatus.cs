using System;
using UnityEngine;
using Script.Interfaces;
using Script.Data_Model.Interfaces;

namespace Script.Entities.Structures.SummondPortal
{
    /// <summary>
    /// Data container cho EnemyPortal - chỉ lưu trữ và hiển thị data.
    /// Tất cả thông số ban đầu được quyết định bởi EnemyPortalManager (Factory pattern).
    /// </summary>
    public class EnemyPortalStatus : MonoBehaviour, IHealth, IHealthReactive
    {
        [Header("Portal States")]
        public bool isReadyToSpawn = false;

        [Tooltip("Số ngày cần để cổng được kích hoạt (mặc định = 1)")]
        public int activeDelay = 1;
        
        [Tooltip("Ngày mà cổng được sinh ra")]
        public int spawnDay;

        [Header("Health")]
        [Tooltip("[DATA] Max HP - được set bởi EnemyPortalManager khi spawn")]
        [SerializeField] private float maxHp = 100f;
        [Tooltip("[DATA] Current HP - được set bởi EnemyPortalManager khi spawn")]
        [SerializeField] private float currentHp;

        // Awake KHÔNG tự set HP nữa - để Manager (Factory) quyết định
        // private void Awake() { }

        public float CurrentHp 
        { 
            get => currentHp; 
            set => currentHp = Mathf.Clamp(value, 0, MaxHp); 
        }

        public float MaxHp 
        { 
            get => maxHp; 
            set => maxHp = Mathf.Max(1f, value); 
        }

        /// <summary>
        /// [BUILD INTERFACE] Reset HP về max khi portal được spawn mới.
        /// Được gọi bởi EnemyPortalManager sau khi Instantiate.
        /// </summary>
        public void ResetHealthToFull()
        {
            currentHp = maxHp;
            Debug.Log($"[EnemyPortalStatus] {gameObject.name} HP reset to full: {currentHp}/{maxHp}");
        }

        // Events for Behavior to subscribe to
        public Action<float> OnDamaged;
        public Action<float> OnHealed;
        public Action OnDeath;

        // IHealthReactive events
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float> OnTakeDamageEvent;

        // --- IHealth Implementation ---
        public void TakeDamage(float amount)
        {
            if (currentHp <= 0) return;
            currentHp -= amount;

            // Hiện số sát thương
            Script.Utils.CombatUtility.ShowFloatDamage(transform.position, amount, Script.FloatingText.FloatingTextType.Damage);

            OnDamaged?.Invoke(amount);
            OnTakeDamageEvent?.Invoke(amount);
            OnHealthChanged?.Invoke(currentHp, maxHp);
            if (currentHp <= 0)
            {
                currentHp = 0;
                Die();
            }
        }

        public void Heal(float amount)
        {
            float prevHp = currentHp;
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            
            float actualHeal = currentHp - prevHp;
            if (actualHeal > 0)
            {
                Script.Utils.CombatUtility.ShowFloatDamage(transform.position, actualHeal, Script.FloatingText.FloatingTextType.Heal);
            }

            OnHealed?.Invoke(amount);
            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void Die()
        {
            OnDeath?.Invoke();
        }

    }
}
