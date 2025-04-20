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
                switch (id)
                {
                    case "wall_0": return PrefabData.Instance.wallDirtMound0;
                    case "wall_1": return PrefabData.Instance.wallSpike1;
                    case "wall_2": return PrefabData.Instance.wallPalisade2;
                    case "wall_3": return PrefabData.Instance.wallStone3;
                    case "wall_4": return PrefabData.Instance.wallTallStone4;
                    case "wall_5": return PrefabData.Instance.wallIron5;
                    default: return null;
                }
            }
            
            private GameObject GetStumpPrefabById(string id)
            {
                switch (id)
                {
                    case "stump_1": return PrefabData.Instance.stumpSpike1;
                    case "stump_2": return PrefabData.Instance.stumpPalisade2;
                    case "stump_3": return PrefabData.Instance.stumpStone3;
                    case "stump_4": return PrefabData.Instance.stumpTallStone4;
                    case "stump_5": return PrefabData.Instance.stumpIron5;
                    default: return null;
                }
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
                
                // Load from Resources/DataConfig/WallStructureConfig.json
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/WallStructureConfig");
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
                    FallbackToHardcodedData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading wall configs: {e.Message}");
                FallbackToHardcodedData();
            }
        }
        
        // This is just a fallback in case the JSON loading fails
        private void FallbackToHardcodedData()
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
                nextUpgradePrefab = PrefabData.Instance.wallSpike1
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
                curentPrefab = PrefabData.Instance.wallSpike1,
                stumpPrefab = PrefabData.Instance.stumpSpike1,
                nextUpgradePrefab = PrefabData.Instance.wallPalisade2
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
                curentPrefab = PrefabData.Instance.wallPalisade2,
                stumpPrefab = PrefabData.Instance.stumpPalisade2,
                nextUpgradePrefab = PrefabData.Instance.wallStone3
            });
            wallList.Add(new WallData
            {
                tier = 3,
                upgradeCost = 8,
                rebuildCost = 3,
                normalMaxHp = 200,
                blessedMaxHp = 360,
                prefabId = "wall_3",
                stumpPrefabId = "stump_3",
                nextUpgradePrefabId = "wall_4",
                curentPrefab = PrefabData.Instance.wallStone3,
                stumpPrefab = PrefabData.Instance.stumpStone3,
                nextUpgradePrefab = PrefabData.Instance.wallTallStone4
            });
            wallList.Add(new WallData
            {
                tier = 4,
                upgradeCost = 12,
                rebuildCost = 5,
                normalMaxHp = 300,
                blessedMaxHp = 500,
                prefabId = "wall_4",
                stumpPrefabId = "stump_4",
                nextUpgradePrefabId = "wall_5",
                curentPrefab = PrefabData.Instance.wallTallStone4,
                stumpPrefab = PrefabData.Instance.stumpTallStone4,
                nextUpgradePrefab = PrefabData.Instance.wallIron5
            });
            wallList.Add(new WallData
            {
                tier = 5,
                upgradeCost = 0,
                rebuildCost = 6,
                normalMaxHp = 400,
                blessedMaxHp = 800,
                prefabId = "wall_5",
                stumpPrefabId = "stump_5",
                curentPrefab = PrefabData.Instance.wallIron5,
                stumpPrefab = PrefabData.Instance.stumpIron5,
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