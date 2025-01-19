using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IDropReward
{
    void DropReward(Vector2 position, int amount, Vector2 direction);
}