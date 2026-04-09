using UnityEngine;

namespace Script.Game.Item.Coin
{
    public interface ICoinPool
    {
        void Setup(Data_Model.PrefabData prefabData);
        void Init();
        GameObject GetCoin();
        void ReturnCoin(GameObject coin);
    }
}
