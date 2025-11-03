using System;
using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine.Serialization;

namespace Script.Game.Structures.TownCenter
{
    public class TownCenterDataList : MonoBehaviour, IDataListBootstrap
    {
        [Serializable] 
        public class TownCenterData 
        {
            public int tier;
            public int upgradeCost;
            public GameObject curentPrefab;
        }
        public static TownCenterDataList Instance { get; private set; }
        public List<TownCenterData> townCentrerList;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }//singleton
        
        //load data vào list
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
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 1,
                upgradeCost = 7,
                curentPrefab = PrefabData.Instance.townCenterList[1],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 2,
                upgradeCost = 10,
                curentPrefab = PrefabData.Instance.townCenterList[2],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 3,
                upgradeCost = 15,
                curentPrefab = PrefabData.Instance.townCenterList[3],
            });
            townCentrerList.Add(new TownCenterData
            {
                tier = 4,
                curentPrefab = PrefabData.Instance.townCenterList[4],
            });
        }

        // Bootstrap interface implementation
        public int Order => 110; // chạy sau WallDataList/kiểm tra ,tại sao lại phải chạy sau wall data
        public void InitializeData(PrefabData context)
        {
            InitializeWallData();
        }
        
        // lấy dữ liệu TownCenter theo tier
        public TownCenterData GetByTier(int tier)
        {
            if (townCentrerList == null)
            {
                Debug.LogError("[TownCenterDataList] townCentrerList is null");
                return null;
            }
            
            var data = townCentrerList.Find(d => d.tier == tier);
            if (data == null)
            {
                Debug.LogError($"[TownCenterDataList] Không tìm thấy data cho tier {tier}");
            }
            return data;
        }
        
        
    }
}
