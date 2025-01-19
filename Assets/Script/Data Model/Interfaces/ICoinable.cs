using UnityEngine;

namespace Script.Data_Model.Interfaces
{
    public interface ICoinable 
    {
        int Coin { get; set; }
        void DropCoin(GameObject whoDrop);
        void AddCoin(GameObject coinGameObject);
    }
}
