using System;
using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using UnityEngine.Serialization;

namespace Script.Game.Structures.TownCenter
{
    public class TownCenterDataList : MonoBehaviour
    {
        public static TownCenterDataList Instance { get; private set; }
        
        [Serializable] 
        public class TownCenterData 
        {
            public int tier;
            public int upgradeCost;
            public GameObject curentPrefab;
            public GameObject nextUpgradePrefab;
            public bool maxLevel;
        }
        
        [SerializeField] private List<TownCenterData> townCentrerList;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        public void InitializeWallData()
        {
            if(townCentrerList == null)
                townCentrerList = new List<TownCenterData>();
            else
                townCentrerList.Clear();
            
            townCentrerList.Add(new TownCenterData
            {
                tier = 0,
                upgradeCost = 3,
                curentPrefab = PrefabData.Instance.townCenterList[0],
                nextUpgradePrefab = PrefabData.Instance.townCenterList[1],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 1,
                upgradeCost = 7,
                curentPrefab = PrefabData.Instance.townCenterList[1],
                nextUpgradePrefab = PrefabData.Instance.townCenterList[2],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 2,
                upgradeCost = 10,
                curentPrefab = PrefabData.Instance.townCenterList[2],
                nextUpgradePrefab = PrefabData.Instance.townCenterList[3],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 3,
                upgradeCost = 15,
                curentPrefab = PrefabData.Instance.townCenterList[3],
                nextUpgradePrefab = PrefabData.Instance.townCenterList[4],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 4,
                curentPrefab = PrefabData.Instance.townCenterList[4], 
                nextUpgradePrefab = null,
                maxLevel = true,
            });
            //Debug.Log("TownCentrerList data initialized. Count: " + townCentrerList.Count);
        }
        
        public TownCenterData GetWallDataByTier(int tier)
        {
            foreach (TownCenterData data in townCentrerList)
            {
                if (data.tier == tier)
                    return data;
            }
            Debug.LogWarning("TownCenter data with tier " + tier + " not found.");
            return null;
        }
    }
}
