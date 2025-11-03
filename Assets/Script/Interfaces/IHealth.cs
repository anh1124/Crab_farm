using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy.Interfaces
{
  
    // Quản lý máu
    public interface IHealth
    {
        int CurrentHp { get; set; }
        int MaxHp { get; set; }

        void TakeDamage(int amount);
        void Die();
        void Heal(int amount);
        
    }
}