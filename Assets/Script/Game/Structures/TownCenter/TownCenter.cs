using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Game.Other.Nature;
using UnityEngine;

namespace Script.Game.Structures.TownCenter
{
    public class TownCenter : MonoBehaviour,IUpgradeable, ISaveable
    { 
        #region SerializeFields & Private Variables
        // Sử dụng [SerializeField] để có thể chỉnh sửa trên Inspector
        [SerializeField] public int level;
        [SerializeField] public int upgradeCost;
        [SerializeField] public GameObject currentPrefab;
        [SerializeField] public GameObject nextUpgradePrefab;
        [SerializeField] public bool readyToUpgrade;
        [SerializeField] public bool maxLevel;

        private readonly int extraDistance = 5;
        
        // Các biến không thuộc interface
        public float plazaDistance;
        public CoinUser coinUser;
        
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
            
            //assign it self to become unique towncenter object in game
            BuildingManager.instance.SetTownCenterAndUpdateBuildRule(gameObject);
        }
        
        private void InitializeData()
        {
            // Lấy dữ liệu từ TownCenterDataList theo level hiện tại
            TownCenterDataList.TownCenterData townCenterData = TownCenterDataList.Instance.GetWallDataByTier(Level);
            
            if (townCenterData != null)
            {
                UpgradeCost = townCenterData.upgradeCost;
                CurrentPrefab = townCenterData.curentPrefab;
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

        public void RuleAddWall()
        {
            if (level == 0)
            {
                
                // Vẽ hình tròn kiểm tra cây trong vùng plazaDistance
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, plazaDistance + extraDistance);
                foreach (Collider2D collider in colliders)
                {
                    NatureTree tree = collider.GetComponent<NatureTree>();
                    if (tree != null)
                    {
                        tree.TakeDame(int.MaxValue); // Chặt cây ngay lập tức
                    }
                }

                // Tạo tường ở hai bên TownCenter
                Vector2 leftPos = new Vector2(transform.position.x - plazaDistance, transform.position.y);
                Vector2 rightPos = new Vector2(transform.position.x + plazaDistance, transform.position.y);

                GameObject leftWall = Instantiate(PrefabData.Instance.wallDirtMound0, leftPos, Quaternion.identity);
                GameObject rightWall = Instantiate(PrefabData.Instance.wallDirtMound0, rightPos, Quaternion.identity);

                // Xoay tường về phía TownCenter
                BuildingManager.instance.RotateTowardsTownCenter(leftWall);
                BuildingManager.instance.RotateTowardsTownCenter(rightWall);

                // Đưa tường vào container để quản lý
                SetUpContainer.Instance.MoveObjectToContainer(leftWall);
                SetUpContainer.Instance.MoveObjectToContainer(rightWall);
            }
        }


        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = UpgradeCost;
            coinUser.onUpgradeComplete = Upgrade;
            
            // Set building type and tier information for the CoinUser component
            coinUser.SetBuildingInfo(BuildingType.TownCenter, Level);
        }
        // Triển khai phương thức Upgrade theo interface
        public void Upgrade()
        {
            if (NextUpgradePrefab != null)
            {
                // Instantiate đối tượng mới với parent là transform cha của đối tượng hiện tại
                GameObject upgraded = Instantiate(NextUpgradePrefab, transform.position, Quaternion.identity,
                    transform.parent);
                
                // Update BuildingManager with the new TownCenter
                if (BuildingManager.instance != null)
                {
                    BuildingManager.instance.SetTownCenterAndUpdateBuildRule(upgraded);
                    
                    // Update the buildable structures list in BuildMenu
                    if (BuildMenu.instance != null)
                    {
                        BuildMenu.instance.UpdateBuildableStructuresList();
                        Debug.Log("Updated buildable structures list after TownCenter upgrade");
                    }
                }
                
                Destroy(gameObject);
                
                // Add wall when upgraded to level 1
                RuleAddWall();
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

        #region Save System Implementation
        [System.Serializable]
        public class TownCenterData
        {
            public float positionX;
            public float positionY;
            public bool readyToUpgrade;
        }
        public string GetPrefabID()
        {
            int index = level;
            return $"town_center_{index}";
        }
        public object CaptureData()
        {
            return new TownCenterData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                readyToUpgrade = this.readyToUpgrade
            };
        }

    public void RestoreData(object data)
    {
        TownCenterData townData = (TownCenterData)data;
        transform.position = new Vector3(townData.positionX, townData.positionY, 0);
        readyToUpgrade = townData.readyToUpgrade;
        
        // Đảm bảo CoinUser đã được khởi tạo
        if (coinUser == null)
        {
            coinUser = GetComponent<CoinUser>();
        }
        
        // Cập nhật dữ liệu từ DataList và UI
        InitializeData();
        UpdateCoinUserData();
        
    }
        #endregion
    }
}
