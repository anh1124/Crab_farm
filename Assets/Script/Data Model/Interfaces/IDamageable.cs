using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy.Interfaces
{
    public interface ITakeDamageable
    {
        int MaxHp { get; set; }
        int CurrentHp { get; set; }
       
        void TakeDamage(int damageAmount, GameObject whoDoDame);
        void Die(GameObject WhoDoDame);
    }
}