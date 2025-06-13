using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Game.Structures.Wall_And_Stump;
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
        public CoinUser coinUser;
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int tier;
        public int normalMaxHp;
        public int blessedMaxHp;

        //prefab
        public GameObject curentPrefab;
        public GameObject stumpPrefab;
        public GameObject nextUpgradePrefab;

        //upgrade 
        public int rebuildCost;//không dùng vì ,khi hỏng sẽ thay luôn gameobejct;
        public int upgradeCost;
        public bool readyToUpgrade;
        public bool maxLevel;

        //build menusystem
        public float minimumDistance = 20f;//khoảng cách tối thiều giữa các tường,tree
        public int buildProgress;
        public int buildProgressMax;
    
        public float MinimumDistance => minimumDistance;
        
        public bool HasCoinShield => coinUser != null && coinUser.collectedCoins > 0;
        public bool HasHp => true;
        public int CurrentCoin { get => coinUser != null ? coinUser.collectedCoins : 0; set { if (coinUser != null) coinUser.collectedCoins = value; } }
        public int MaxCoin { get => coinUser != null ? coinUser.requiredCoins : 0; set { if (coinUser != null) coinUser.requiredCoins = value; } }
        
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
            BuildingManager.instance.RotateTowardsTownCenter(gameObject);

            // Đăng ký GameObject vào WallAndStumpManager dựa trên tag
            if (Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance != null)
            {
                if (gameObject.CompareTag("Wall"))
                {
                    Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance.RegisterWall(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[Wall.cs] GameObject {gameObject.name} has unexpected tag: {gameObject.tag}. Not registering with WallAndStumpManager as a Wall.");
                }
            }
            else
            {
                Debug.LogError("[Wall] WallAndStumpManager instance not found!");
            }
        }

        private void OnDestroy()
        {
            // Hủy đăng ký GameObject khỏi WallAndStumpManager khi bị hủy
            if (Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance != null)
            {
                if (gameObject.CompareTag("Wall"))
                {
                    Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance.UnregisterWall(gameObject);
                }
            }
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
                
                // Set buildProgressMax from configuration
                buildProgressMax = wallData.buildProgress;
                
                UpdateMaxHp();
                CurrentHp = MaxHp;
            }
            else
            {
                Debug.LogWarning($"No WallData found for tier: {tier}");
            }
        }

        
        /// <summary>
        /// Cập nhật MaxHp của tường dựa trên công nghệ Adhesive
        /// - Nếu techAdhesive = true: Sử dụng blessedMaxHp (HP tối đa đã được tăng cường)
        /// - Nếu techAdhesive = false: Sử dụng normalMaxHp (HP tối đa bình thường)
        /// Hàm này sẽ được gọi mỗi khi thông tin thế giới thay đổi hoặc khi khởi tạo
        /// </summary>
        public void UpdateMaxHp()
        {
            bool techAdhesive = false;
            // Kiểm tra WorldFileManager tồn tại và có worldInfo không null trước khi truy cập
            if (Script.Save_Controller.WorldFileManager.Instance != null && 
                Script.Save_Controller.WorldFileManager.Instance.worldInfo != null)
            {
                techAdhesive = Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive;
            }
            MaxHp = techAdhesive ? blessedMaxHp : normalMaxHp;
            CurrentHp = CurrentHp > MaxHp ? MaxHp : CurrentHp;
        }

        /// <summary>
        /// Đăng ký lắng nghe sự kiện OnWorldInfoChanged khi đối tượng được kích hoạt
        /// Sự kiện này sẽ được gọi mỗi khi dữ liệu worldInfo thay đổi, ví dụ như khi nghiên cứu techAdhesive
        /// Giúp tường cập nhật HP tối đa một cách tự động khi công nghệ được mở khóa mà không cần khởi động lại
        /// </summary>
        private void OnEnable() 
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.OnWorldInfoChanged += OnWorldInfoChanged;
            }
        }

        /// <summary>
        /// Hủy đăng ký lắng nghe sự kiện OnWorldInfoChanged khi đối tượng bị vô hiệu hóa
        /// Điều này để ngăn memory leak và lỗi null reference khi đối tượng không còn hoạt động
        /// </summary>
        private void OnDisable()
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.OnWorldInfoChanged -= OnWorldInfoChanged;
            }
        }

        /// <summary>
        /// Được gọi mỗi khi worldInfo thay đổi thông qua sự kiện OnWorldInfoChanged
        /// Cập nhật lại MaxHp dựa trên trạng thái hiện tại của techAdhesive
        /// Điều này đảm bảo tất cả tường trong game sẽ được tăng HP ngay lập tức khi công nghệ được mở khóa
        /// </summary>
        /// <param name="worldInfo">Thông tin thế giới hiện tại chứa trạng thái của techAdhesive</param>
        private void OnWorldInfoChanged(Script.Save_Controller.WorldInfo worldInfo)
        {
            UpdateMaxHp();
            if (BuildingManager.isDebug)
            {
                Debug.Log($"Wall {gameObject.name} updated HP due to WorldInfo change. TechAdhesive: {worldInfo.TechAdhesive}, MaxHP: {MaxHp}");
            }
        }

        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = upgradeCost;
            coinUser.onUpgradeComplete = OnCoinCollectionComplete;
            
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
        
        // Phương thức mới được gọi khi thu thập đủ coin
        private void OnCoinCollectionComplete()
        {
            // Đánh dấu đã sẵn sàng nâng cấp
            readyToUpgrade = true;
            
            // Kiểm tra xem có bỏ qua quá trình xây dựng không
            if (PrefabData.Instance.skipBuildingProcess)
            {
                // Nếu ở chế độ bỏ qua quá trình xây dựng, nâng cấp ngay lập tức
                Upgrade();
            }
            else
            {
                // Ghi log yêu cầu thợ xây đến
                Debug.LogWarning($"[Hệ thống xây dựng] Tường cấp {tier} tại vị trí {transform.position} đã sẵn sàng để nâng cấp! Cần thợ xây đến làm việc.");
                //thêm script điều thiển thợ xây
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

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && CurrentCoin > 0)
            {
                CurrentCoin -= 1;
                if (CurrentCoin < 0) CurrentCoin = 0;
                // Có thể thêm hiệu ứng rơi xu ở đây
                return true;
            }
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(whoDoDame);
                    return false;
                }
                return true;
            }
            return true;
        }

        public void Repair()
        {
            UpdateMaxHp();
            CurrentHp = MaxHp;
        }

        public void Die(GameObject whoDoDame)
        {
            Debug.Log($"{gameObject.name} đã bị phá hủy bởi {whoDoDame?.name ?? "unknown"}");
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
            public bool readyToUpgrade;
            public int buildProgress;
            public int buildProgressMax;
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
                maxHp = this.MaxHp,
                readyToUpgrade = this.readyToUpgrade,
                buildProgress = this.buildProgress,
                buildProgressMax = this.buildProgressMax
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
            
            // Lưu lại các giá trị khác từ dữ liệu đã load
            bool savedReadyToUpgrade = wallData.readyToUpgrade;
            int savedBuildProgress = wallData.buildProgress;
            int savedBuildProgressMax = wallData.buildProgressMax;
            
            // Khởi tạo lại dữ liệu từ DataList
            InitializeWallData();
            
            // Khôi phục giá trị HP từ save data
            CurrentHp = savedCurrentHp;
            MaxHp = savedMaxHp;
            
            // Khôi phục các giá trị khác từ save data
            readyToUpgrade = savedReadyToUpgrade;
            buildProgress = savedBuildProgress;
            buildProgressMax = savedBuildProgressMax;
            
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
        

        public void AddWorkingProcess(int amount)
        {
            buildProgress += amount;
            if (buildProgress > buildProgressMax) buildProgress = buildProgressMax;
            Debug.LogWarning($"[Wall] AddWorkingProcess: buildProgress = {buildProgress}/{buildProgressMax}");
            if (buildProgress >= buildProgressMax)
            {
                Debug.LogWarning("[Wall] buildProgress reached max, upgrading!");
                Upgrade();
            }
        }
    }
}
