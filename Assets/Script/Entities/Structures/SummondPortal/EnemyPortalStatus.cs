using System;
using UnityEngine;
using Script.Game.Enemy.Interfaces;
using Script.Data_Model.Interfaces;
using Script.Interfaces;

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
        [SerializeField] private int maxHp = 1000;
        [SerializeField] private int currentHp;

        public int CurrentHp 
        { 
            get => currentHp; 
            set => currentHp = Mathf.Clamp(value, 0, MaxHp); 
        }

        public int MaxHp 
        { 
            get => maxHp; 
            set => maxHp = Mathf.Max(1, value); 
        }

        // Events for Behavior to subscribe to
        public Action<int> OnDamaged;
        public Action<int> OnHealed;
        public Action OnDeath;

        // --- IHealth Implementation ---
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

    }
}
