using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;

namespace Script.Game.Enemy
{
    public interface IEnemyDataProvider
    {
        List<GameObject> Enemies { get; }
        List<TotalEnemyList.EnemyDataBase> ReadyToUseEnemyDataList { get; }
        void AddEnemy(GameObject enemy);
        void RemoveEnemy(GameObject enemy);
        void InitializeEnemyData();
        List<(TotalEnemyList.EnemyDataBase Data, int Quantity)> SelectMonstersByPower(int maxPower);
    }
}
