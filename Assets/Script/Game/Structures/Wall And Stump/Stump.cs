using Script.Data_Model;
using UnityEngine;
using Script.Game.Item.Coin;

namespace Script.Game.Structures.Wall_And_Stump
{
    /// <summary>
    /// stump hiện tại có 1 hành động
    /// -Rebuild
    /// </summary>
    public class Stump : MonoBehaviour
    {
        [SerializeField] private int tier;
        [SerializeField] private int rebuildCost;
        [SerializeField] private GameObject wallPrefab;
        
        private CoinUser coinUser;
        
        private void Start()
        {
            coinUser = GetComponent<CoinUser>();
            if (coinUser == null)
            {
                Debug.LogError($"CoinUser component missing on {gameObject.name}");
                return;
            }
            InitializeStumpData(tier);
            UpdateCoinUserData();
        }
        
        public void InitializeStumpData(int tier)
        {
            WallDataList.WallData wallData = WallDataList.Instance.GetWallDataByTier(tier);
            if (wallData != null)
            {
                rebuildCost = wallData.rebuildCost;
                wallPrefab  = wallData.curentPrefab;
            }
            else
            {
                Debug.LogWarning($"No WallData found for tier: {tier}");
            }
        }

        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = rebuildCost;
            coinUser.onUpgradeComplete = Rebuild;
        }
        

        private void Rebuild()
        {
            if (wallPrefab != null)
            {
                Instantiate(wallPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}