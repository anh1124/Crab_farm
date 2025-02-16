using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Game.Structures.TownCenter;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Structures.TownCenter
{
    public class TownCenter : MonoBehaviour,IUpgradeable
    { /*
       * tag Structure
       * layer Defautl
       */
        #region SerializeFields & Private Variables
        // Sử dụng [SerializeField] để có thể chỉnh sửa trên Inspector
        [SerializeField] public int level;
        [SerializeField] public int upgradeCost;
        [SerializeField] public GameObject currentPrefab;
        [SerializeField] public GameObject nextUpgradePrefab;
        [SerializeField] public bool readyToUpgrade;
        [SerializeField] public bool maxLevel;
        
        // Các biến không thuộc interface
        public float plazaDistance;
        public CoinUser coinUser;
        
        //2 biến đặc biệt của công trình 
        /*public int centralWallLowerTier;
        public int centralTowerLowerTier;*/
        #endregion
        
        #region Interface Properties Implementation
        public int Level
        {
            get => level;
            set => level = value;
        }

        public int UpgradeCost
        {
            get => upgradeCost;
            set => upgradeCost = value;
        }

        public GameObject CurrentPrefab
        {
            get => currentPrefab;
            set => currentPrefab = value;
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

            GetPlazaDistance();
            InitializeData();
            UpdateCoinUserData();
            BuildingManager.instance.CheckForExistingTownCenter();
        }
        
        private void InitializeData()
        {
            // Lấy dữ liệu từ TownCenterDataList theo level hiện tại
            TownCenterDataList.TownCentrerData townCenterData = TownCenterDataList.Instance.GetWallDataByTier(Level);
            
            if (townCenterData != null)
            {
                UpgradeCost = townCenterData.upgradeCost;
                CurrentPrefab = townCenterData.curentPrefab; // Lưu ý: đảm bảo tên biến trong data trùng với đây
                NextUpgradePrefab = townCenterData.nextUpgradePrefab;
                MaxLevel = townCenterData.maxLevel;

                // Nếu cần sử dụng các dữ liệu khác (ví dụ: centralWallLowerTier, centralTowerLowerTier)
                // centralWallLowerTier = townCenterData.centralWallLowerTier;
                // centralTowerLowerTier = townCenterData.centralTowerLowerTier;
            }
            else
            {
                Debug.LogWarning($"No townCentrerData found for tier: {Level}");
            }
        }
        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = UpgradeCost;
            coinUser.onUpgradeComplete = Upgrade;
        }
        // Triển khai phương thức Upgrade theo interface
        public void Upgrade()
        {
            if (NextUpgradePrefab != null)
            {
                // Instantiate đối tượng mới với parent là transform cha của đối tượng hiện tại
                GameObject upgraded = Instantiate(NextUpgradePrefab, transform.position, Quaternion.identity, transform.parent);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning(NextUpgradePrefab + " is null in " + CurrentPrefab.name);
            }

            
         
        }


        // Triển khai phương thức GetUpgradeCost theo interface
        public int GetUpgradeCost()
        {
            return UpgradeCost;
        }

        // Phương thức lấy thông tin khoảng cách Plaza từ PrefabData
        public void GetPlazaDistance()
        {
            plazaDistance = PrefabData.Instance.plazaDistance;
        }
    }
}
