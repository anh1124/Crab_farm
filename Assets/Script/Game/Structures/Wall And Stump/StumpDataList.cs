using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json; // Cần thiết cho việc đọc JSON
using System.IO; // Cần thiết cho thao tác tệp

namespace Script.Game.Structures.Wall_And_Stump
{
    public class StumpDataList : MonoBehaviour
    {
        public static StumpDataList Instance { get; private set; }

        [System.Serializable]
        public class StumpData
        {
            public int tier;
            public int rebuildCost;
            public GameObject currentPrefab;

            // Default constructor cho deserialization của JsonUtility
            public StumpData() { }

            public StumpData(int tier, int rebuildCost, GameObject currentPrefab)
            {
                this.tier = tier;
                this.rebuildCost = rebuildCost;
                this.currentPrefab = currentPrefab;
            }
        }

        [System.Serializable]
        private class StumpDataWrapper
        {
            public List<StumpData> stumps;
        }

        public List<StumpData> stumpDataList = new List<StumpData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                LoadStumpData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadStumpData()
        {
            string filePath = Path.Combine(Application.dataPath, "Resources", "DataConfig", "Structure", "StumpData.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                StumpDataWrapper wrapper = JsonConvert.DeserializeObject<StumpDataWrapper>(json);
                if (wrapper != null && wrapper.stumps != null)
                {
                    stumpDataList = wrapper.stumps;
                   // Debug.Log($"[StumpDataList] Loaded {stumpDataList.Count} stump data entries.");
                }
                else
                {
                    Debug.LogError("[StumpDataList] Failed to parse StumpData.json or data is empty.");
                }
            }
            else
            {
                Debug.LogWarning("[StumpDataList] StumpData.json not found at " + filePath + ". Initializing with default data.");
                // Optionally, initialize with some default data if the file doesn't exist
                InitializeDefaultData();
            }
        }

        private void InitializeDefaultData()
        {
            // Example default data. You should replace this with your actual game data.
            stumpDataList.Add(new StumpData(1, 100, null)); // Tier 1 stump, 100 rebuild cost, no prefab for now
            stumpDataList.Add(new StumpData(2, 200, null)); // Tier 2 stump, 200 rebuild cost
            // Add more tiers as needed

            // You might want to save this default data to the JSON file
            SaveStumpData();
        }

        private void SaveStumpData()
        {
            string filePath = Path.Combine(Application.dataPath, "Resources", "DataConfig", "Structure", "StumpData.json");
            StumpDataWrapper wrapper = new StumpDataWrapper { stumps = stumpDataList };
            string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Debug.Log("[StumpDataList] Stump data saved to " + filePath);
        }

        public StumpData GetStumpDataByTier(int tier)
        {
            return stumpDataList.Find(data => data.tier == tier);
        }

        // Helper method to get prefab from Resources
        public GameObject GetStumpPrefab(string prefabName)
        {
            // Assuming your stump prefabs are in Resources/Prefab/Structure/Wall and stump/Stump
            return Resources.Load<GameObject>("Prefab/Structure/Wall and stump/Stump/" + prefabName);
        }
    }
} 