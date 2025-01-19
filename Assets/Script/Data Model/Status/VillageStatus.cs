using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : BaseStatus
{
    public int atk;              // Attack power
    public float attackSpeed;      // Attack speed

    public override void Die(GameObject whoDoDame)
    {
        Debug.Log("Player has died, killed by: " + whoDoDame);
        // Các hành động khác khi nhân vật chết, ví dụ như game over, respawn...
        base.Die(whoDoDame);  // Gọi phương thức base nếu cần thiết
    }
}
