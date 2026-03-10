using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

public class EnemyStatus : IAttacker
{
    public int AttackDame { get; set; }              // Attack power
    public float AttackSpeed { get; set; }      // Attack speed

    public void Attack()
    {
    }
  
}
