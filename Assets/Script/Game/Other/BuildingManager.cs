using System;
using System.Collections.Generic;
using System.IO; // Import thêm thư viện để đọc file JSON
using Newtonsoft.Json; // Import Newtonsoft.Json để parse JSON
using Script.Data_Model;
using Script.Game.Structures.TownCenter;
using UnityEngine;

namespace Script.Game.Other
{
    //script này sẽ đọc data từ json chỉ định,load số lượng và cấp độ tối đa mà 1 công trình có thể xây
    // Enum khai báo các loại công trình trong game
    public enum BuildingType
    {
        TownCenter,
        Wall,
        HammerVendor,
        BowVendor,
        // Mở rộng thêm các công trình nếu cần
    }

    // Lớp lưu thông tin số lượng công trình tối đa có thể xây ở một cấp
    [Serializable]
    public class StructureInfo
    {
        public BuildingType buildingType;
        public int maxQuantity;
        public int maxLevel;
    }
    // Lớp lưu danh sách cấu hình công trình cho một cấp của TownCenter
    [Serializable]
    public class TownCenterConfig
    {
        [Tooltip("Cấp của TownCenter")]
        public int level;
        [Tooltip("Danh sách công trình có thể xây ở cấp này")] 
        public List<StructureInfo> structures;
    }

    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager instance;

        [Header("Town Center Settings")]
        [Tooltip("Danh sách prefab TownCenter theo cấp (index tương ứng với cấp)")]
        public GameObject[] townCenterPrefabs;

        [Header("Town Center Build Configs")]
        public List<TownCenterConfig> townCenterConfigs;

        [Header("Available Structures (Debug)")]
        [Tooltip("Danh sách công trình có thể xây (hiển thị trong Inspector)")]
        public List<string> availableStructuresForInspector = new List<string>();

        public int currentTownCenterLevel;
        [Tooltip("Prefab TownCenter hiện tại (nếu có)")]
        public GameObject currentTownCenter;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            // Đọc dữ liệu từ JSON
            LoadTownCenterConfigsFromJson();
            
            // Add null check to handle cases where configs might not be loaded
            if (townCenterConfigs == null || townCenterConfigs.Count == 0)
            {
                Debug.LogWarning("TownCenter configs not loaded or empty. Building rules will not be applied correctly.");
                townCenterConfigs = new List<TownCenterConfig>();
            }
            
            UpdateAvailableBuildings();
        }

        private void LoadTownCenterConfigsFromJson()
        {
            try 
            {
                // Load from the new path: Resources/DataConfig/TownCenterConfig
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/TownCenterConfig");
                if (jsonTextAsset != null)
                {
                    var configWrapper = JsonConvert.DeserializeObject<TownCenterConfigWrapper>(jsonTextAsset.text);
                    townCenterConfigs = configWrapper?.townCenterConfigs ?? new List<TownCenterConfig>();
                    Debug.Log($"Đã load {townCenterConfigs.Count} cấp cấu hình từ Resources/DataConfig/TownCenterConfig.");
                }
                else
                {
                    // Fallback to direct file path if not found in Resources
                    string filePath = Path.Combine(Application.dataPath, "Resources/DataConfig/TownCenterConfig.json");
                    if (File.Exists(filePath))
                    {
                        string jsonContent = File.ReadAllText(filePath);
                        var configWrapper = JsonConvert.DeserializeObject<TownCenterConfigWrapper>(jsonContent);
                        townCenterConfigs = configWrapper?.townCenterConfigs ?? new List<TownCenterConfig>();
                        Debug.Log($"Đã load {townCenterConfigs.Count} cấp cấu hình từ file path {filePath}.");
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy file TownCenterConfig.json trong Resources/DataConfig hoặc ở đường dẫn trực tiếp.");
                        townCenterConfigs = new List<TownCenterConfig>();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading TownCenter configs: {e.Message}");
                townCenterConfigs = new List<TownCenterConfig>();
            }
        }

        // Wrapper class để ánh xạ JSON
        [Serializable]
        private class TownCenterConfigWrapper
        {
            public List<TownCenterConfig> townCenterConfigs;
        }

        public void UpdateAvailableBuildings()
        {
            availableStructuresForInspector.Clear(); // Xóa danh sách cũ

            if (townCenterConfigs == null || townCenterConfigs.Count == 0)
            {
                Debug.LogWarning("Không có cấu hình TownCenter để cập nhật.");
                return;
            }

            if (currentTownCenterLevel < 0 || currentTownCenterLevel >= townCenterConfigs.Count)
            {
                Debug.LogWarning("Cấp TownCenter (" + currentTownCenterLevel + ") không có cấu hình phù hợp!");
                return;
            }

            List<StructureInfo> availableStructures = townCenterConfigs[currentTownCenterLevel].structures;
            if (availableStructures == null || availableStructures.Count == 0)
            {
                Debug.LogWarning($"Không có cấu hình công trình cho cấp TownCenter {currentTownCenterLevel}");
                return;
            }

            Debug.Log($"[UpdateAvailableBuildings] Cập nhật build rule cho cấp TownCenter {currentTownCenterLevel}:");
            foreach (var structureInfo in availableStructures)
            {
                int builtCount = CountBuiltBuildings(structureInfo.buildingType);
                string structureInfoText = $"{structureInfo.buildingType}: {builtCount}/{structureInfo.maxQuantity}";
                availableStructuresForInspector.Add(structureInfoText);

                Debug.Log($"- {structureInfo.buildingType}: {builtCount}/{structureInfo.maxQuantity}");
            }
            
            // Notify BuildMenu to update its buildable structures list
            if (BuildMenu.instance != null)
            {
                // We don't directly call UpdateBuildableStructuresList here to avoid a circular reference
                // as it already calls UpdateAvailableBuildings itself
                Debug.Log("BuildingManager notified BuildMenu to update available structures");
            }
        }

        public int CountBuiltBuildings(BuildingType type)
        {
            try
            {
                string tag = type.ToString();
                GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(tag);
                return foundObjects != null ? foundObjects.Length : 0;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error counting buildings of type {type}: {e.Message}");
                return 0;
            }
        }
        public void RotateTowardsTownCenter(GameObject obj)
        {
            if (currentTownCenter == null)
            {
                Debug.LogWarning("TownCenter chưa được gán.");
                return;
            }

            // Adjust the scale based on position
            Vector3 directionToTownCenter = obj.transform.position - currentTownCenter.transform.position;
            Vector3 scale = obj.transform.localScale;
            
            // Match the logic from Utility.RotateObjectTowardsTownCenter that works correctly
            if (directionToTownCenter.x < 0)
            {
                // Object is to the LEFT of town center
                scale.x = -1f * Mathf.Abs(scale.x); // Flip scale to face left
            }
            else
            {
                // Object is to the RIGHT of town center
                scale.x = Mathf.Abs(scale.x); // Normal scale to face right
            }
            
            // Apply the scale
            obj.transform.localScale = scale;
            
            // Ensure rotation is identity (no rotation)
            obj.transform.rotation = Quaternion.identity;
            
            Debug.Log($"BuildingManager.RotateTowardsTownCenter: Object {obj.name} at position {obj.transform.position}, town center at {currentTownCenter.transform.position}, direction {directionToTownCenter}, scale {scale}");
        }
        
        public void SetTownCenterAndUpdateBuildRule(GameObject townCenterObj)
        {
            if (townCenterObj == null)
            {
                Debug.LogError("Attempted to set a null TownCenter object!");
                return;
            }
            
            // Set the current TownCenter object
            currentTownCenter = townCenterObj;
            
            // Get the TownCenter component to retrieve its level
            TownCenter townCenterComponent = townCenterObj.GetComponent<TownCenter>();
            if (townCenterComponent != null)
            {
                // Update the current TownCenter level
                currentTownCenterLevel = townCenterComponent.Level;
                
                // Update available buildings based on the new level
                UpdateAvailableBuildings();
                
                Debug.Log($"TownCenter set to {townCenterObj.name} at level {currentTownCenterLevel}");
            }
            else
            {
                Debug.LogError($"TownCenter component not found on {townCenterObj.name}");
                // Set a default level to avoid issues
                currentTownCenterLevel = 0;
            }
            
            // Notify other systems that town center has changed if needed
            // Add any required system notifications here
        }
    }
}
