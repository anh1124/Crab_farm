using System;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Structures.Archer_tower
{
    public class ArcherTowerList : MonoBehaviour
    {
        /*add this script to GM GameObject
         * 
         * istrigger true
         * add slot carry acher
         */
        [System.Serializable]
        public class ArcherTowerData
        {
            public int tier;
            public int upgradeCost;//
            public bool covered = false;//protect acher who stan in this tower from about
            public int carryOn;//how many acher this tower can carry
            public GameObject archerTower;
            public GameObject nextUpgrade;
        }
    
        // Singleton instance
        public static ArcherTowerList Instance { get; private set; }

        [SerializeField] private List<ArcherTowerData> archerTowerList;
    
        public bool loadData = false;
        public Action onDataLoaded;  // Declare Action
    
        private void Awake()
        {
            //singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject); // Destroy duplicate instances
                return;
            }
        }

        private void Start()
        {
            InitializeAcherTowerData();
        
            loadData = true;  // Set loadData to true when loading is complete
            onDataLoaded?.Invoke();  // Invoke the action
        }

        public void InitializeAcherTowerData()
        {
            List<ArcherTowerData> newAcherTowers = new List<ArcherTowerData>()
            {
                new ArcherTowerData
                {
                  tier = 0,
                  upgradeCost = 3,
                  covered = false,
                  carryOn = 0,
                  archerTower = PrefabData.Instance.towerList[0],
                  nextUpgrade = PrefabData.Instance.towerList[1]
                },
                new ArcherTowerData
                {
                    tier = 1,//rock platform
                    upgradeCost = 3,
                    covered = false,
                    carryOn = 1,
                    archerTower = PrefabData.Instance.towerList[1],
                    nextUpgrade = PrefabData.Instance.towerList[2]
                },
                new ArcherTowerData
                {
                    tier = 2, // Wooden watchtower	,taller than ,shot skill better
                    upgradeCost = 6,
                    covered = false,
                    carryOn = 1,
                    archerTower = PrefabData.Instance.towerList[2],
                    nextUpgrade = PrefabData.Instance.towerList[3]
                },
                new ArcherTowerData
                {
                    tier = 3, //Stone tower
                    upgradeCost = 9,
                    covered = false,
                    carryOn = 2,
                    archerTower = PrefabData.Instance.towerList[3],
                    nextUpgrade = PrefabData.Instance.towerList[4]
                },
                new ArcherTowerData
                {
                    tier = 4, //Triplet tower
                    upgradeCost = 12,
                    covered = false,
                    carryOn = 3,
                    archerTower = PrefabData.Instance.towerList[4],
                    nextUpgrade = PrefabData.Instance.towerList[5]
                },
                new ArcherTowerData
                {
                    tier = 5, //Roofed Triplet tower
                    upgradeCost = 15,
                    covered = true,
                    carryOn = 3,
                    archerTower = PrefabData.Instance.towerList[5],
                    nextUpgrade = PrefabData.Instance.towerList[6]
                },
                new ArcherTowerData
                {
                    tier = 6, //Quadruplet tower
                    upgradeCost = 18,
                    covered = false,
                    carryOn = 4,
                    archerTower = PrefabData.Instance.towerList[6],
                    nextUpgrade = null
                }
            };
        
            archerTowerList.AddRange(new List<ArcherTowerData>());
            Debug.Log("Added archer tower to archerTower list. Total archerTower:" + archerTowerList.Count);
        }

        public ArcherTowerData GetArcherTowerByTier(int tier)
        {
            return archerTowerList.Find(archerTowerData => archerTowerData.tier == tier);
        }

        public GameObject GetPrefabArcherTowerByTier(int tier)
        {
            foreach (var archerTowerData in archerTowerList)
            {
                if ( archerTowerData.tier ==  tier)
                {
                    return archerTowerData.archerTower;
                }
            }
            Debug.LogWarning("No archerTower found for tier: " + tier);
            return null;  // Return null if no archer tower is found for the given tier
        }

        public bool CheckDidThisTowerCanUpgradeByTier(int tier)
        {
            // Find the tower data for the given tier
            ArcherTowerData towerData = archerTowerList.Find(tower => tower.tier == tier);

            // Check if the tower data was found and if it has a next upgrade available
            if (towerData != null && towerData.nextUpgrade != null)
            {
                return true;  // Tower can be upgraded
            }

            // If no tower data was found or there's no next upgrade, return false
            return false;
        }

    }
}
