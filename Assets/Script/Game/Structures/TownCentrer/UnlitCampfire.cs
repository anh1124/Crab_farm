using UnityEngine;

namespace Script.Game.Structures.TownCentrer
{
   public class UnlitCampfire : MonoBehaviour
   {
      private CoinUser _coinUser;

      private void Start()
      {
         _coinUser = GetComponent<CoinUser>();
         if (_coinUser != null)
         {
            _coinUser.actionHaveEnoughCoin += ActionHaveEnoughCoin;
         }
         else
         {
            Debug.LogWarning(gameObject.name +"cần có Script CoinUser") ;
         }
      }
      private void OnDestroy()
      {
         if (_coinUser != null)
         {
            _coinUser.actionHaveEnoughCoin -= ActionHaveEnoughCoin;
         }
      }

      private void ActionHaveEnoughCoin()
      {
         Debug.Log(gameObject.name + " đủ coin");
      }
   }
}
