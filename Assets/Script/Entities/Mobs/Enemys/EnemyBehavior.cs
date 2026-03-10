using System;
using UnityEngine;
using Script.Interfaces;

namespace Script.Game.Enemy
{
    public class EnemyBehavior : MonoBehaviour, IDamageable
    {
        // Define Despawn event for GuardManager to subscribe to
        public event Action<EnemyBehavior> OnDespawn;

        [Header("Testing")]
        [SerializeField] private int hp = 10;

        private void OnDisable()
        {
            // Trigger event when disabled (returned to pool or destroyed)
            NotifyDespawn();
        }

        private void OnDestroy()
        {
            // Trigger event when destroyed
            NotifyDespawn();
        }

        // Call this when the enemy dies or is removed
        public void Despawn()
        {
            NotifyDespawn();
            
            // For testing/pooling simulation
            gameObject.SetActive(false); 
        }

        private void NotifyDespawn()
        {
            OnDespawn?.Invoke(this);
        }

        // Simple Hit logic
        public void TakeDamage(int amount)
        {
            hp -= amount;
            if (hp <= 0)
            {
                Despawn();
            }
        }

        // --- IDamageable Interface Implementation ---
        public void ReceiveDamage(int damage, UnityEngine.GameObject instigator)
        {
            TakeDamage(damage);
        }
    }
}
