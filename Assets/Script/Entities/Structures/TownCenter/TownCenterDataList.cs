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
        public void InitializeWallData(PrefabData context)
        {
            if(townCentrerList == null)
                townCentrerList = new List<TownCenterData>();
            else
                townCentrerList.Clear();
            
            var prefabData = context != null ? context : PrefabData.Instance;
            if (prefabData == null || prefabData.townCenterList == null || prefabData.townCenterList.Count == 0)
            {
                Debug.LogError("[TownCenterDataList] PrefabData.townCenterList is null/empty");
                return;
            }

            // Upgrade costs by tier (tier i uses upgradeCost when upgrading from i -> i+1).
            // If you add more prefabs, costs will auto-extend.
            int[] defaultCosts = { 3, 7, 10, 15, 20, 25 };
            int lastCost = defaultCosts[defaultCosts.Length - 1];

            for (int i = 0; i < prefabData.townCenterList.Count; i++)
            {
                int cost = i < defaultCosts.Length ? defaultCosts[i] : (lastCost + 5 * (i - defaultCosts.Length + 1));
                townCentrerList.Add(new TownCenterData
                {
                    tier = i,
                    upgradeCost = cost,
                    curentPrefab = prefabData.townCenterList[i],
                });
            }
        }

        // Bootstrap interface implementation
        public int Order => 110; // chạy sau WallDataList/kiểm tra ,tại sao lại phải chạy sau wall data
        public void InitializeData(PrefabData context)
        {
            InitializeWallData(context);
        }
        
        // lấy dữ liệu TownCenter theo tier
        public TownCenterData GetByTier(int tier)
        {
            TryGetByTier(tier, out var data, logIfMissing: true);
            return data;
        }

        // For "check next tier" use-cases: can be silent (no spam log)
        public bool TryGetByTier(int tier, out TownCenterData data, bool logIfMissing)
        {
            data = null;
            if (townCentrerList == null)
            {
                if (logIfMissing)
                    Debug.LogError("[TownCenterDataList] townCentrerList is null");
                return false;
            }

            data = townCentrerList.Find(d => d.tier == tier);
            if (data == null && logIfMissing)
                Debug.LogError($"[TownCenterDataList] Không tìm thấy data cho tier {tier}");

            return data != null;
        }
        
        
    }
}
