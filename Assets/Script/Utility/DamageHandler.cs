using UnityEngine;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;

namespace Script.Utils
{
    // Centralized damage resolver supporting optional coin shield
    public static class DamageHandler
    {
        // Returns true if damage was applied (or absorbed), false if target died
        public static bool ApplyDamage(GameObject target, int damage, GameObject instigator)
        {
            if (target == null) return true;

            // Use IHealth interface for damage handling
            var health = target.GetComponent<IHealth>();
            if (health != null)
            {
                // If target supports coin shield, try to consume first
                var coinShield = target.GetComponent<ICoinShield>();
                if (coinShield != null && coinShield.HasCoinShield)
                {
                    if (coinShield.ConsumeCoinShield())//nếu có coinshield thì chặn
                    {
                        return true; // absorbed
                    }
                }
                
                // Apply damage using IHealth
                if (damage > 0)
                {
                    health.TakeDamage(damage);
                }
                return health.CurrentHp > 0;
            }

            // If no health interface present, treat as no-op
            Debug.LogWarning($"[DamageHandler] Target {target.name} has no IHealth interface.");
            return true;
        }
    }
}


