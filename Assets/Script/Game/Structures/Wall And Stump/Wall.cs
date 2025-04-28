using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Game.Item.Coin;
using Script.Game.Other;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class Wall : MonoBehaviour, IDamageable, IMinimumDistance, ISaveable, IUpgradeable
    { 
        /*
         * chú ý, stump ở layer defautl
         *         wall ở layer wall
         *          layer wall
         */
        // Properties từ IDamageable
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        
        
        [SerializeField] private int tier;
        
        [SerializeField] private int normalMaxHp;
        [SerializeField] private int blessedMaxHp;
        
        [SerializeField] private GameObject curentPrefab;
        [SerializeField] private GameObject stumpPrefab;
        [SerializeField] private GameObject nextUpgradePrefab;
        
        [SerializeField] private int rebuildCost;
        [SerializeField] private int upgradeCost;
        
        [SerializeField] private bool readyToUpgrade;
        [SerializeField] private bool maxLevel;
  
        [SerializeField] private CoinUser coinUser;
        [SerializeField] private float minimumDistance = 20f;
    
        public float MinimumDistance => minimumDistance;
        
        #region IUpgradeable Properties
        public int Level
        {
            get => tier;
            set => tier = value;
        }

        public int UpgradeCost
        {
            get => upgradeCost;
            set => upgradeCost = value;
        }

        public GameObject CurrentPrefab
        {
            get => curentPrefab;
            set => curentPrefab = value;
        }

        public GameObject NextUpgradePrefab
        {
            get => nextUpgradePrefab;
            set => nextUpgradePrefab = value;
        }

        public bool ReadyToUpgrade
        {
            get => readyToUpgrade;
            set => readyToUpgrade = value;
        }

        public bool MaxLevel
        {
            get => maxLevel;
            set => maxLevel = value;
        }
        #endregion
        
        private void Start()
        {
            coinUser = GetComponent<CoinUser>();
            if (coinUser == null)
            {
                Debug.LogError($"CoinUser component missing on {gameObject.name}");
                return;
            }
    
            InitializeWallData();
            UpdateCoinUserData();
    
            // Thay vì gọi BuildingManager.instance.RotateTowardsTownCenter(gameObject),
            // ta gọi trực tiếp hàm tiện ích của bạn:
            BuildingManager.instance.RotateTowardsTownCenter(this.gameObject);
        }


        private void InitializeWallData()
        {
            //Debug.LogWarning("InitializeWallData called for tier: " + tier);
            WallDataList.WallData wallData = WallDataList.Instance.GetWallDataByTier(tier);
            
            if (wallData != null)
            {
                upgradeCost = wallData.upgradeCost;
                rebuildCost = wallData.rebuildCost;
                normalMaxHp = wallData.normalMaxHp;
                blessedMaxHp = wallData.blessedMaxHp;
                
                curentPrefab = wallData.curentPrefab;
                stumpPrefab = wallData.stumpPrefab;
                nextUpgradePrefab = wallData.nextUpgradePrefab;
                
                // Only max level if no next upgrade prefab available
                // Don't hardcode tier 4 as max level - use config file instead
                maxLevel = (nextUpgradePrefab == null);
                
                UpdateMaxHp();
                CurrentHp = MaxHp;
            }
            else
            {
                Debug.LogWarning($"No WallData found for tier: {tier}");
            }
        }

        private void UpdateMaxHp()
        {
            MaxHp = PrefabData.Instance.techAdhesive ? blessedMaxHp : normalMaxHp;
        }

        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = upgradeCost;
            coinUser.onUpgradeComplete = Upgrade;
            
            // Check if the wall can be upgraded based on town center level
            bool canBeUpgraded = CanUpgradeByTownCenterLevel();
            
            // Only allow accepting coins if the wall can be upgraded and is not at max level
            // Check if this is the final level available in the game or by town center level
            bool isMaxLevelForTownCenter = !canBeUpgraded;
            bool isMaxLevelForWall = maxLevel;

            coinUser.canAcceptCoins = !isMaxLevelForTownCenter && !isMaxLevelForWall;
            
            // Set building info for CoinUser
            coinUser.SetBuildingInfo(BuildingType.Wall, tier);
            
            // Debug information - only if debug is enabled
            if (BuildingManager.isDebug)
            {
                Debug.Log($"Wall tier={tier}, canBeUpgraded={canBeUpgraded}, isMaxLevelForTownCenter={isMaxLevelForTownCenter}, " +
                         $"isMaxLevelForWall={isMaxLevelForWall}, canAcceptCoins={coinUser.canAcceptCoins}");
            }
        }
        
        // Check if this wall can be upgraded based on town center level
        private bool CanUpgradeByTownCenterLevel()
        {
            if (BuildingManager.instance == null) return false;
            
            // Debug to see the current town center level and max wall tier allowed
            int townCenterLevel = BuildingManager.instance.currentTownCenterLevel;
            int maxAllowedTier = BuildingManager.instance.GetMaxAllowedTier(BuildingType.Wall);
            
            // Only log if debug is enabled
            if (BuildingManager.isDebug)
            {
                Debug.Log($"Town Center Level: {townCenterLevel}, Max Wall Tier Allowed: {maxAllowedTier}, Current Wall Tier: {tier}");
            }
            
            return BuildingManager.instance.CanBuildingBeUpgraded(BuildingType.Wall, tier);
        }

        public void TakeDamage(int damageAmount, GameObject whoDoDame)
        {
            CurrentHp -= damageAmount;
            if (CurrentHp <= 0)
            {
                Die(whoDoDame);
            }
        }

        public void Repair()
        {
            UpdateMaxHp();
            CurrentHp = MaxHp;
        }

        public void Die(GameObject WhoDoDame)
        {
            GameObject stump = Instantiate(stumpPrefab, transform.position, Quaternion.identity);
            Stump stumpScriptInstance = stump.GetComponent<Stump>();
            stumpScriptInstance.InitializeStumpData(tier);
            Destroy(gameObject);
        }

        // Implementing IUpgradeable.Upgrade() method
        public void Upgrade()
        {
            // Check if upgrade is allowed by town center level
            if (!CanUpgradeByTownCenterLevel())
            {
                if (BuildingManager.isDebug)
                {
                    Debug.LogWarning($"Wall at tier {tier} cannot be upgraded: Town Center level is too low.");
                }
                return;
            }
            
            if (nextUpgradePrefab != null)
            {
                if (BuildingManager.isDebug)
                {
                    Debug.Log($"Upgrading wall from tier {tier} to tier {tier + 1}");
                }
                
                // Get the current parent of this object
                Transform parentTransform = transform.parent;
        
                // Instantiate the new upgrade prefab as a child of the same parent
                GameObject newUpgrade = Instantiate(nextUpgradePrefab, transform.position, Quaternion.identity, parentTransform);
        
                // Destroy the current object
                Destroy(gameObject);
            }
            else
            {
                if (BuildingManager.isDebug)
                {
                    Debug.LogWarning($"Cannot upgrade wall at tier {tier}: No next upgrade prefab is available");
            }
            }
        }
        
        // Implementing IUpgradeable.GetUpgradeCost() method
        public int GetUpgradeCost()
        {
            return upgradeCost;
        }

        #region Save System Implementation
        [System.Serializable]
        public class WallData
        {
            public float positionX;
            public float positionY;
            public int tier;
            public int currentHp;
            public int maxHp;
        }
        
        public string GetPrefabID()
        {
            return $"wall_{tier}";
        }
        
        public object CaptureData()
        {
            return new WallData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                tier = this.tier,
                currentHp = this.CurrentHp,
                maxHp = this.MaxHp
            };
        }

        public void RestoreData(object data)
        {
            WallData wallData = (WallData)data;
            transform.position = new Vector3(wallData.positionX, wallData.positionY, 0);
            tier = wallData.tier;
            CurrentHp = wallData.currentHp;
            // Lưu lại giá trị HP từ dữ liệu đã load
            int savedCurrentHp = CurrentHp;
            int savedMaxHp = MaxHp;
            
            // Khởi tạo lại dữ liệu từ DataList
            InitializeWallData();
            
            // Khôi phục giá trị HP từ save data
            CurrentHp = savedCurrentHp;
            MaxHp = savedMaxHp;
            
            // Đảm bảo HP không âm
            if (CurrentHp <= 0) 
            {
                CurrentHp = MaxHp;
            }
        }
        #endregion

        // Adding a public method to refresh upgrade status when town center level changes
        public void RefreshUpgradeStatus()
        {
            // Re-evaluate the maxLevel based on current BuildingManager configuration
            // and on whether there's a next upgrade prefab
            if (nextUpgradePrefab != null)
            {
                maxLevel = false; // There is a next upgrade prefab
                
                if (BuildingManager.isDebug)
                {
                    Debug.Log($"Wall tier {tier} can be upgraded (has upgrade prefab)");
                }
            }
            else
            {
                maxLevel = true; // No next upgrade prefab
                
                if (BuildingManager.isDebug)
                {
                    Debug.Log($"Wall tier {tier} is at max level (no upgrade prefab)");
                }
            }
            
            // Update CoinUser configuration
            if (coinUser != null)
            {
                UpdateCoinUserData();
            }
        }
    }
}
