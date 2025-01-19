using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageStatus : BaseStatus
{
    public GameObject toolEquip;//công cụ mà villager đang cầm
    public override void Die(GameObject whoDoDame)
    {
        base.Die(whoDoDame);  // Gọi phương thức base nếu cần thiết
    }
}
