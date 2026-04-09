using UnityEngine;
using Script.Data_Model;

namespace Script.Game.Enemy
{
    public interface IEnemyPool
    {
        void Setup(IEnemyDataProvider dataProvider, PrefabData prefabData, EnemyTargetManager targetManager);
        void InitializePools();
        GameObject GetEnemy(string enemyName);
        void ReturnEnemy(GameObject enemy);
        int GetPoolCount();
    }
}
