using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class WallDataList : MonoBehaviour
    {
        public static WallDataList Instance { get; private set; }
        
        public static bool isDebug = false;
        
        [System.Serializable]
        public class WallData
        {
            public int tier;
            public int upgradeCost;
            public int rebuildCost;
            public int normalMaxHp;
            public int blessedMaxHp;
            
            // String IDs for JSON serialization
            public string prefabId;
            public string stumpPrefabId;
            public string nextUpgradePrefabId;
            
            // GameObject references (not serialized to JSON)
            [JsonIgnore]
            public GameObject curentPrefab;   
            [JsonIgnore]
            public GameObject stumpPrefab;
            [JsonIgnore]
            public GameObject nextUpgradePrefab;
            
            public bool maxLevel;
            
            // Helper method to resolve prefab IDs to GameObjects after deserialization
            public void ResolvePrefabReferences()
            {
                if (!string.IsNullOrEmpty(prefabId))
                    curentPrefab = GetWallPrefabById(prefabId);
                    
                if (!string.IsNullOrEmpty(stumpPrefabId))
                    stumpPrefab = GetStumpPrefabById(stumpPrefabId);
                    
                if (!string.IsNullOrEmpty(nextUpgradePrefabId))
                    nextUpgradePrefab = GetWallPrefabById(nextUpgradePrefabId);
            }
            
            private GameObject GetWallPrefabById(string id)
            {
                if (id == "wall_0") return PrefabData.Instance.wallDirtMound0;
                
                if (id.StartsWith("wall_"))
                {
                    int index = int.Parse(id.Substring(5)) - 1;
                    if (index >= 0 && index < PrefabData.Instance.wallList.Count)
                    {
                        return PrefabData.Instance.wallList[index];
                    }
                }
                return null;
            }
            
            private GameObject GetStumpPrefabById(string id)
            {
                if (id.StartsWith("stump_"))
                {
                    int index = int.Parse(id.Substring(6)) - 1;
                    if (index >= 0 && index < PrefabData.Instance.stumpList.Count)
                    {
                        return PrefabData.Instance.stumpList[index];
                    }
                }
                return null;
            }
        }
        
        [System.Serializable]
        private class WallStructureConfigWrapper
        {
            public List<WallData> wallStructureConfigs;
        }
        
        public List<WallData> wallList = new List<WallData>(); 
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void InitializeWallData()
        {
            LoadWallDataFromJson();
        }
        
        private void LoadWallDataFromJson()
        {
            try 
            {
                // Clear existing data
                wallList.Clear();
                
                // Load from Resources/DataConfig/Structure/WallStructureConfig.json
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/Structure/WallStructureConfig");
                if (jsonTextAsset != null)
                {
                    var configWrapper = JsonConvert.DeserializeObject<WallStructureConfigWrapper>(jsonTextAsset.text);
                    
                    if (configWrapper?.wallStructureConfigs != null)
                    {
                        foreach (var wallData in configWrapper.wallStructureConfigs)
                        {
                            // Resolve GameObject references from string IDs
                            wallData.ResolvePrefabReferences();
                            wallList.Add(wallData);
                        }
                        
                        if (isDebug) Debug.Log($"Loaded {wallList.Count} wall configurations from JSON");
                    }
                }
                else
                {
                    Debug.LogError("WallStructureConfig.json không tìm thấy trong Resources/DataConfig");
                    LoadHardcodedWallData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading wall configs: {e.Message}");
                LoadHardcodedWallData();
            }
        }
        
        private void LoadHardcodedWallData()
        {
            if (isDebug) Debug.LogWarning("Falling back to hardcoded wall data");
            wallList.Clear();
            
            wallList.Add(new WallData
            {
                tier = 0,
                upgradeCost = 1,
                prefabId = "wall_0",
                nextUpgradePrefabId = "wall_1",
                curentPrefab = PrefabData.Instance.wallDirtMound0,
                nextUpgradePrefab = PrefabData.Instance.wallList[0]
            });
            wallList.Add(new WallData
            {
                tier = 1,
                upgradeCost = 3,
                rebuildCost = 1,
                normalMaxHp = 25,
                blessedMaxHp = 50,
                prefabId = "wall_1",
                stumpPrefabId = "stump_1",
                nextUpgradePrefabId = "wall_2",
                curentPrefab = PrefabData.Instance.wallList[0],
                stumpPrefab = PrefabData.Instance.stumpList[0],
                nextUpgradePrefab = PrefabData.Instance.wallList[1]
            });
            wallList.Add(new WallData
            {
                tier = 2,
                upgradeCost = 5,
                rebuildCost = 1,
                normalMaxHp = 50,
                blessedMaxHp = 90,
                prefabId = "wall_2",
                stumpPrefabId = "stump_2",
                nextUpgradePrefabId = "wall_3",
                curentPrefab = PrefabData.Instance.wallList[1],
                stumpPrefab = PrefabData.Instance.stumpList[1],
                nextUpgradePrefab = PrefabData.Instance.wallList[2]
            });
            wallList.Add(new WallData
            {
                tier = 3,
                upgradeCost = 7,
                rebuildCost = 2,
                normalMaxHp = 75,
                blessedMaxHp = 130,
                prefabId = "wall_3",
                stumpPrefabId = "stump_3",
                nextUpgradePrefabId = "wall_4",
                curentPrefab = PrefabData.Instance.wallList[2],
                stumpPrefab = PrefabData.Instance.stumpList[2],
                nextUpgradePrefab = PrefabData.Instance.wallList[3]
            });
            wallList.Add(new WallData
            {
                tier = 4,
                upgradeCost = 9,
                rebuildCost = 2,
                normalMaxHp = 100,
                blessedMaxHp = 170,
                prefabId = "wall_4",
                stumpPrefabId = "stump_4",
                nextUpgradePrefabId = "wall_5",
                curentPrefab = PrefabData.Instance.wallList[3],
                stumpPrefab = PrefabData.Instance.stumpList[3],
                nextUpgradePrefab = PrefabData.Instance.wallList[4]
            });
            wallList.Add(new WallData
            {
                tier = 5,
                upgradeCost = 11,
                rebuildCost = 3,
                normalMaxHp = 125,
                blessedMaxHp = 210,
                prefabId = "wall_5",
                stumpPrefabId = "stump_5",
                curentPrefab = PrefabData.Instance.wallList[4],
                stumpPrefab = PrefabData.Instance.stumpList[4],
                maxLevel = true
            });
        }
        
        public WallData GetWallDataByTier(int tier)
        {
            foreach (WallData wallData in wallList)
            {
                if (wallData.tier == tier)
                {
                    return wallData;
                }
            }
    
            if (isDebug) Debug.LogWarning("WallData with tier " + tier + " not found.");
            return null;
        }
    }
}