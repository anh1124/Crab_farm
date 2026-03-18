using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy.Interfaces
{
  
    // Quản lý máu
    public interface IHealth
    {
        float CurrentHp { get; set; }
        float MaxHp { get; set; }

        void TakeDamage(float amount);
        void Die();
        void Heal(float amount);
        
    }
}