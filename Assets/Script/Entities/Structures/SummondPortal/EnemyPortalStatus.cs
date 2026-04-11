using System;
using UnityEngine;
using Script.Interfaces;
using Script.Data_Model.Interfaces;

namespace Script.Entities.Structures.SummondPortal
{
    public class EnemyPortalStatus : MonoBehaviour, IHealth
    {
        [Header("Portal States")]
        public bool isReadyToSpawn = false;

        [Tooltip("Số ngày cần để cổng được kích hoạt (mặc định = 1)")]
        public int activeDelay = 1;
        
        [Tooltip("Ngày mà cổng được sinh ra")]
        public int spawnDay;

        [Header("Health")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp;

        private void Awake()
        {
            if (currentHp <= 0f)
            {
                currentHp = maxHp;
            }
        }

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

        // Events for Behavior to subscribe to
        public Action<float> OnDamaged;
        public Action<float> OnHealed;
        public Action OnDeath;

        // --- IHealth Implementation ---
        public void TakeDamage(float amount)
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

        public void Heal(float amount)
        {
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            OnHealed?.Invoke(amount);
        }

        public void Die()
        {
            OnDeath?.Invoke();
        }

    }
}
