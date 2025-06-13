using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy.Interfaces
{
    public interface IDamageable
    {
        // Loại thanh máu mà đối tượng sử dụng
        bool HasCoinShield { get; }
        bool HasHp { get; }

        // Coin Shield
        int CurrentCoin { get; set; }
        int MaxCoin { get; set; }

        // HP
        int CurrentHp { get; set; }
        int MaxHp { get; set; }

        // Nhận dame, trả về true nếu còn sống sau khi nhận dame
        bool TakeDamage(int damage, GameObject whoDoDame);

        // Chết
        void Die(GameObject whoDoDame);
    }
}