using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDropable
{
    void Drop(GameObject whoDrop,GameObject dropWhat,int amount,Vector2 pos);
}
