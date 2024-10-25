using System;
using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using UnityEngine.Serialization;

namespace Script.Game.Structures.TownCentrer
{
    public class TownCenterDataList : MonoBehaviour
    {
        public static TownCenterDataList Instance { get; private set; }
        //số tiền mà 1 towncentrer cấp 3 trở lên có coinStock
        //sẽ được lưu ở từng mapdata hoặc biến riêng
        [System.Serializable] 
        public class TownCentrerData 
        {
            public int tier;
            public int upgradeCost;
            public GameObject curentPrefab;
            public GameObject nextUpgradePrefab;
            public bool maxLevel;
            //-attribute
            
            //lvl-0 unlit campfire
            //no other
            
            //lvl-1 campfire
            public bool bowVendor;
            public bool hammerVendor;
            
            //lvl-2 Village tent
            public bool hoeVendor;
            
            //lvl-3 Townhall
            public bool coinStock;
            public bool pykeVendor;
            
            //lvl-4 Big TownHall
            public bool shieldVendor;
            public bool siegeWorkshop;
            
            //lvl-5>???
           
            
            public int centralWallLowerTier;
            public int centralTowerLowerTier;
            
            
            //khi thực hiện nâng cấp townCentrel
            /*
             * kiểm tra thêm xem nếu tier của 2 centrelWall hoặc CentrelTower
             * thì sẽ được nâng cấp miễn phí đến tier thấp nhất này,
             * nếu nâng cấp towerCoentrer thành công ở trong tình trạng báo động thì việc nâng cấp này sẽ được hoãn đến khi
             * các 2 bức tường sẽ được hoãn đến sang mai
             */

        }
        [SerializeField] private List<TownCentrerData> townCentrerList;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeWallData()
        {
            townCentrerList.Add(new TownCentrerData
            {
                tier = 0,
                upgradeCost=3,
                curentPrefab = PrefabData.Instance.townCenterUnlitCampfire0,
                nextUpgradePrefab=PrefabData.Instance.townCenterCampfire1,
            });
            townCentrerList.Add(new TownCentrerData
            {
                tier = 1,
                upgradeCost=7,
                curentPrefab = PrefabData.Instance.townCenterCampfire1,
                nextUpgradePrefab=PrefabData.Instance.townCenterVillageTents2,
                
                bowVendor = true,
                hammerVendor = true
            });
            townCentrerList.Add(new TownCentrerData
            {
                tier = 2,
                upgradeCost=10,
                curentPrefab = PrefabData.Instance.townCenterVillageTents2,
                nextUpgradePrefab=PrefabData.Instance.townCenterTownHall3,
                
                bowVendor = true,
                hammerVendor = true,
                hoeVendor = true
                
            });
            townCentrerList.Add(new TownCentrerData
            {
                tier = 3,
                upgradeCost=15,
                curentPrefab = PrefabData.Instance.townCenterTownHall3,
                nextUpgradePrefab=PrefabData.Instance.townCenterBigTownHall4,
                
                bowVendor = true,
                hammerVendor = true,
                hoeVendor = true,
                coinStock = true,
                pykeVendor = true,
                
            });
            townCentrerList.Add(new TownCentrerData
            {
                tier = 4,
                upgradeCost=0,//để là 0 ,để cho không hiển thị coinslot vì đây là cáp cuối
                curentPrefab = PrefabData.Instance.townCenterBigTownHall4,
                nextUpgradePrefab=null,
                maxLevel = true,
                
                bowVendor = true,
                hammerVendor = true,
                hoeVendor = true,
                coinStock = true,
                pykeVendor = true,
                shieldVendor = true,
                siegeWorkshop = true,
            });
            
            //Debug.Log("townCentrerList data initialized with count: " + townCentrerList.Count);
            
        }
        public TownCentrerData GetWallDataByTier(int tier)
        {
            foreach (TownCentrerData townCentre in townCentrerList)
            {
                if (townCentre.tier == tier)
                {
                    return townCentre;
                }
            }
    
            Debug.LogWarning("townCentre with tier " + tier + " not found.");
            return null;
        }

    }
}
