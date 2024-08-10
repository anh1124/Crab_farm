using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.Game.Structures.TownCentrer
{
   public class UnlitCampfire : MonoBehaviour
   {
      
      [SerializeField]
      private bool readyToUpgrade;

      public bool ReadyToUpgrade
      {
         get => readyToUpgrade;
         set => readyToUpgrade = value;
      }
      public void DoUpgrade()
      {
         Debug.Log(gameObject.name + " is upgrading...");
      }

      private void ActionHaveEnoughCoin()
      {
         Debug.Log(gameObject.name + " đủ coin");
      }
   }
}
