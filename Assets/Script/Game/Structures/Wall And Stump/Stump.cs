using Script.Data_Model;
using UnityEngine;
using Script.Game.Item.Coin;
using Script.Game.Other;

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
            this.tier = tier;
            WallDataList.WallData wallData = WallDataList.Instance.GetWallDataByTier(tier);
            if (wallData != null)
            {
                rebuildCost = wallData.rebuildCost;
                wallPrefab  = wallData.curentPrefab;
            }
            else if (BuildingManager.isDebug)
            {
                Debug.LogWarning($"No WallData found for tier: {tier}");
            }
        }

        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = rebuildCost;
            coinUser.onUpgradeComplete = Rebuild;
            
            // Check if this tier of wall can be built based on town center level
            bool canRebuild = CanRebuildWall();
            coinUser.canAcceptCoins = canRebuild;
            
            // Keep the SetBuildingInfo for compatibility with the CoinUser system
            // But a stump isn't really a wall - it's just the ruins of one
            coinUser.SetBuildingInfo(BuildingType.Wall, tier);
        }
        
        // Check if this wall can be rebuilt based on town center level
        private bool CanRebuildWall()
        {
            if (BuildingManager.instance == null) return false;
            
            // For rebuilding, we need to check if the current tier is allowed
            // (not tier+1 like with upgrades)
            int maxAllowedTier = BuildingManager.instance.GetMaxAllowedTier(BuildingType.Wall);
            
            if (tier > maxAllowedTier)
            {
                if (BuildingManager.isDebug) 
                    Debug.Log($"Cannot rebuild wall tier {tier}: Town Center only allows up to tier {maxAllowedTier}");
                return false;
            }
            
            return true;
        }
        
        private void Rebuild()
        {
            // Double-check rebuild permission before proceeding
            if (!CanRebuildWall())
            {
                if (BuildingManager.isDebug)
                {
                    Debug.LogWarning($"Wall stump at tier {tier} cannot be rebuilt: Town Center level is too low.");
                }
                return;
            }
            
            if (wallPrefab != null)
            {
                Instantiate(wallPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}