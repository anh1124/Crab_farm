using UnityEngine;
using Script.Game.Item.Coin;

namespace Script.Game.Mobs.Interfaces
{
    /// <summary>
    /// For behaviors that need to perform specific logic immediately after picking up a coin.
    /// Example: Vagrant transforms into Villager.
    /// </summary>
    public interface ICoinReactor
    {
        void OnCoinPickedUp(Coin coin);
    }
}
