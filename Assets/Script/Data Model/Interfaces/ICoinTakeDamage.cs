using UnityEngine;

namespace Script.Data_Model.Interfaces
{
    //interface này sử dụng cho những thực thể khi bị thương sẽ rơi ra tiền thay vì mất hp
    public interface ICoinTakeDamage
    {
        void OnCoinDamage();
        int CurrentCoins { get; }
        int MaxCoins { get; }
        void DropCoins(int amount);
    }
} 