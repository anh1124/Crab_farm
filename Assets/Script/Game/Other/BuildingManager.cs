using System;
using System.Collections.Generic;
using System.IO; // Import thêm thư viện để đọc file JSON
using Newtonsoft.Json; // Import Newtonsoft.Json để parse JSON
using Script.Data_Model;
using Script.Game.Structures.TownCenter;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

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
        [Tooltip("Loại công trình")]
        public BuildingType buildingType;
        [Tooltip("Số lượng tối đa có thể xây")]
        public int maxQuantity;
        [Tooltip("Cấp tối đa của công trình (nếu công trình có thể nâng cấp)")]
        public int maxLevel;
    }
    // Lớp lưu danh sách cấu hình công trình cho một cấp của TownCenter
    [Serializable]
    public class TownCenterConfig
    {
        [FormerlySerializedAs("level")]
        [Tooltip("Cấp của TownCenter")]
        public int townCenterLevel;
        [FormerlySerializedAs("availableStructures")]
        [Tooltip("Danh sách công trình có thể xây ở cấp này")] 
        public List<StructureInfo> availableStructures;
    }

    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager instance;
        
        [Header("Debug Settings")]
        [Tooltip("Enable to show debug logs for building and upgrading")]
        public bool enableDebugLogs = false;
        
        // Static property for checking debug state globally
        public static bool isDebug => instance != null && instance.enableDebugLogs;

        [Header("Town Center Settings")]
        [Tooltip("Danh sách prefab TownCenter theo cấp (index tương ứng với cấp)")]
        public GameObject[] townCenterPrefabs;

        [Header("Config File Settings")]
        [Tooltip("Tên file config trong thư mục Resources/DataConfig")]
        public string configFileName = "BuildingLimits";

        [Header("Town Center Build Configs")]
        [SerializeField] 
        private List<TownCenterConfig> townCenterConfigs = new List<TownCenterConfig>();
        
        // Public property to access townCenterConfigs from other classes
        public List<TownCenterConfig> TownCenterConfigs => townCenterConfigs;

        [Header("Available Structures (Debug)")]
        [SerializeField]
        private List<StructureInfo> availableStructures = new List<StructureInfo>();

        [HideInInspector]
        public int currentTownCenterLevel;
        [Tooltip("Prefab TownCenter hiện tại (nếu có)")]
        [SerializeField] 
        private GameObject currentTownCenter;
        
        // Public property to access currentTownCenter from other classes
        public GameObject CurrentTownCenter => currentTownCenter;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
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

            // Tìm TownCenter trong scene
            if (currentTownCenter == null)
            {
                GameObject townCenterObject = GameObject.FindGameObjectWithTag("TownCenter");
                if (townCenterObject != null)
                {
                    SetTownCenterAndUpdateBuildRule(townCenterObject);
                }
                else if (isDebug)
                {
                    Debug.LogWarning("No TownCenter found in scene. Building rules will use default level 0.");
                }
            }
        }

        private void LoadTownCenterConfigsFromJson()
        {
            try 
            {
                // Load from the new path: Resources/DataConfig/[configFileName]
                string fullPath = $"DataConfig/{configFileName}";
                TextAsset jsonTextAsset = Resources.Load<TextAsset>(fullPath);
                
                if (jsonTextAsset != null)
                {
                    // Parse the JSON directly to handle property name mismatches
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonTextAsset.text);
                    if (jsonObject != null && jsonObject.ContainsKey("townCenterConfigs"))
                    {
                        var configsArray = JsonConvert.DeserializeObject<List<object>>(jsonObject["townCenterConfigs"].ToString());
                        townCenterConfigs = new List<TownCenterConfig>();
                        
                        foreach (var configObj in configsArray)
                        {
                            var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configObj.ToString());
                            int level = Convert.ToInt32(configDict["townCenterLevel"]);
                            
                            var config = new TownCenterConfig
                            {
                                townCenterLevel = level,
                                availableStructures = new List<StructureInfo>()
                            };
                            
                            string structureKey = configDict.ContainsKey("structures") ? "structures" : 
                                                  configDict.ContainsKey("availableStructures") ? "availableStructures" : null;
                            
                            if (structureKey != null && configDict.ContainsKey(structureKey))
                            {
                                var structures = JsonConvert.DeserializeObject<List<object>>(configDict[structureKey].ToString());
                                foreach (var structObj in structures)
                                {
                                    var structDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(structObj.ToString());
                                    var structInfo = new StructureInfo
                                    {
                                        buildingType = Enum.Parse<BuildingType>(structDict["buildingType"].ToString()),
                                        maxQuantity = Convert.ToInt32(structDict["maxQuantity"])
                                    };
                                    
                                    if (structDict.ContainsKey("maxLevel"))
                                    {
                                        structInfo.maxLevel = Convert.ToInt32(structDict["maxLevel"]);
                                    }
                                    
                                    config.availableStructures.Add(structInfo);
                                }
                            }
                            
                            townCenterConfigs.Add(config);
                        }
                        
                        if (isDebug) Debug.Log($"Đã load {townCenterConfigs.Count} cấp cấu hình từ {fullPath}");
                    }
                    else
                    {
                        Debug.LogError($"JSON format is invalid in {fullPath} - missing 'townCenterConfigs' property");
                        TownCenterConfigFallback();
                    }
                }
                else
                {
                    // Fallback to direct file path if not found in Resources
                    Debug.LogError($"Configuration file {configFileName} not found in Resources/DataConfig. Using fallback configuration.");
                    TownCenterConfigFallback();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading TownCenter configs: {e.Message}\n{e.StackTrace}");
                TownCenterConfigFallback();
            }
        }
        
        private void TownCenterConfigFallback()
        {
            Debug.LogWarning("Using fallback TownCenter configurations");
            townCenterConfigs = new List<TownCenterConfig>();
            
            // Level 0 - Only TownCenter
            var config0 = new TownCenterConfig
            {
                townCenterLevel = 0,
                availableStructures = new List<StructureInfo>
                {
                    new StructureInfo { buildingType = BuildingType.TownCenter, maxQuantity = 1 }
                }
            };
            townCenterConfigs.Add(config0);
            
            // Level 1 - Basic buildings
            var config1 = new TownCenterConfig
            {
                townCenterLevel = 1,
                availableStructures = new List<StructureInfo>
                {
                    new StructureInfo { buildingType = BuildingType.Wall, maxQuantity = 2, maxLevel = 1 },
                    new StructureInfo { buildingType = BuildingType.HammerVendor, maxQuantity = 1 },
                    new StructureInfo { buildingType = BuildingType.BowVendor, maxQuantity = 1 }
                }
            };
            townCenterConfigs.Add(config1);
            
            // Level 2 - More capacity
            var config2 = new TownCenterConfig
            {
                townCenterLevel = 2,
                availableStructures = new List<StructureInfo>
                {
                    new StructureInfo { buildingType = BuildingType.Wall, maxQuantity = 4, maxLevel = 2 },
                    new StructureInfo { buildingType = BuildingType.HammerVendor, maxQuantity = 1 },
                    new StructureInfo { buildingType = BuildingType.BowVendor, maxQuantity = 1 }
                }
            };
            townCenterConfigs.Add(config2);
            
            // Level 3
            var config3 = new TownCenterConfig
            {
                townCenterLevel = 3,
                availableStructures = new List<StructureInfo>
                {
                    new StructureInfo { buildingType = BuildingType.Wall, maxQuantity = 6, maxLevel = 3 },
                    new StructureInfo { buildingType = BuildingType.HammerVendor, maxQuantity = 1 },
                    new StructureInfo { buildingType = BuildingType.BowVendor, maxQuantity = 1 }
                }
            };
            townCenterConfigs.Add(config3);
            
            // Level 4
            var config4 = new TownCenterConfig
            {
                townCenterLevel = 4,
                availableStructures = new List<StructureInfo>
                {
                    new StructureInfo { buildingType = BuildingType.Wall, maxQuantity = 8, maxLevel = 4 },
                    new StructureInfo { buildingType = BuildingType.HammerVendor, maxQuantity = 1 },
                    new StructureInfo { buildingType = BuildingType.BowVendor, maxQuantity = 1 }
                }
            };
            townCenterConfigs.Add(config4);
        }

        public void UpdateAvailableBuildings()
        {
            // Clear current list
            availableStructures.Clear();
            
            if (townCenterConfigs == null || townCenterConfigs.Count == 0)
            {
                Debug.LogWarning("Không có cấu hình TownCenter để cập nhật.");
                return;
            }

            // Find the config for the current TownCenter level
            TownCenterConfig currentConfig = null;
            
            foreach (var config in townCenterConfigs)
            {
                if (config.townCenterLevel == currentTownCenterLevel)
                {
                    currentConfig = config;
                    break;
                }
            }
            
            // If not found, try to find the closest lower level
            if (currentConfig == null && townCenterConfigs.Count > 0)
            {
                int closestLevel = -1;
                foreach (var config in townCenterConfigs)
                {
                    if (config.townCenterLevel < currentTownCenterLevel && 
                        config.townCenterLevel > closestLevel)
                    {
                        closestLevel = config.townCenterLevel;
                        currentConfig = config;
                    }
                }
                
                if (currentConfig != null)
                {
                    Debug.LogWarning($"No configuration found for level {currentTownCenterLevel}, using level {closestLevel} instead");
                }
                else
                {
                    // If still not found, use the first available config
                    currentConfig = townCenterConfigs[0];
                    Debug.LogWarning($"No suitable configuration found for level {currentTownCenterLevel}, using level {currentConfig.townCenterLevel} instead");
                }
            }
            
            if (currentConfig != null)
            {
                // Add all available structures for this level
                if (currentConfig.availableStructures != null)
                {
                    availableStructures.AddRange(currentConfig.availableStructures);
                    
                    if (isDebug)
                    {
                        Debug.Log($"[UpdateAvailableBuildings] Cập nhật build rule cho cấp TownCenter {currentTownCenterLevel}:");
                        foreach (var structureInfo in availableStructures)
                        {
                            int builtCount = CountBuiltBuildings(structureInfo.buildingType);
                            Debug.Log($"- {structureInfo.buildingType}: {builtCount}/{structureInfo.maxQuantity}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No available structures defined for town center level {currentTownCenterLevel}");
                }
                
                // Notify BuildMenu to update its buildable structures list
                if (BuildMenu.instance != null)
                {
                    if (isDebug) Debug.Log("BuildingManager notified BuildMenu to update available structures");
                }
            }
            else
            {
                Debug.LogWarning($"Không có cấu hình công trình cho cấp TownCenter {currentTownCenterLevel}");
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
                if (isDebug) Debug.LogWarning("TownCenter chưa được gán.");
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
            
            if (isDebug) Debug.Log($"BuildingManager.RotateTowardsTownCenter: Object {obj.name} at position {obj.transform.position}, town center at {currentTownCenter.transform.position}, direction {directionToTownCenter}, scale {scale}");
        }
        
        // Check if a specific building type can be upgraded to the next tier based on current town center level
        public bool CanBuildingBeUpgraded(BuildingType buildingType, int currentTier)
        {
            // Check if we have configurations
            if (townCenterConfigs == null || availableStructures == null)
            {
                Debug.LogWarning("No town center configurations available for upgrade check");
                return false;
            }

            // Find the structure info for this building type
            StructureInfo structureInfo = availableStructures.Find(s => s.buildingType == buildingType);
            if (structureInfo == null)
            {
                if (isDebug) Debug.Log($"Building type {buildingType} is not available at town center level {currentTownCenterLevel}");
                return false;
            }

            // Check if we have data for the max level allowed
            if (structureInfo.maxLevel <= 0)
            {
                if (isDebug) Debug.Log($"Building type {buildingType} doesn't have a valid maxLevel ({structureInfo.maxLevel})");
                return false;
            }

            // Remove the special case for tier 4 wall to allow for upgrades beyond tier 4
            // This will rely on configuration and the existence of a next upgrade prefab

            // Check if the next tier would exceed the max level allowed
            int nextTier = currentTier + 1;
            if (nextTier > structureInfo.maxLevel)
            {
                if (isDebug) Debug.Log($"Building type {buildingType} at tier {currentTier} cannot be upgraded. Max level is {structureInfo.maxLevel}, town center level is {currentTownCenterLevel}");
                return false;
            }

            if (isDebug) Debug.Log($"Building type {buildingType} at tier {currentTier} can be upgraded to tier {nextTier}, max allowed level is {structureInfo.maxLevel}");
            return true;
        }
        
        // Gets the maximum tier allowed for a specific building type at current town center level
        public int GetMaxAllowedTier(BuildingType buildingType)
        {
            if (townCenterConfigs == null || availableStructures == null)
            {
                return 0;
            }
            
            StructureInfo structureInfo = availableStructures.Find(s => s.buildingType == buildingType);
            if (structureInfo == null)
            {
                Debug.LogWarning($"Building type {buildingType} not found in available structures for town center level {currentTownCenterLevel}");
                return 0;
            }
            
            // Debug log to help trace issues
            if (isDebug) Debug.Log($"Town center level {currentTownCenterLevel} allows {buildingType} up to tier {structureInfo.maxLevel}");
            
            return structureInfo.maxLevel;
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
                int previousLevel = currentTownCenterLevel;
                currentTownCenterLevel = townCenterComponent.Level;
                
                // Update available buildings based on the new level
                UpdateAvailableBuildings();
                
                if (isDebug) Debug.Log($"TownCenter updated from level {previousLevel} to {currentTownCenterLevel}");
                
                // Force update of all wall upgrade limits in the scene when town center upgrades
                if (previousLevel != currentTownCenterLevel)
                {
                    UpdateAllStructureLimits();
                }
            }
            else
            {
                Debug.LogError($"TownCenter component not found on {townCenterObj.name}");
                // Set a default level to avoid issues
                currentTownCenterLevel = 0;
            }
        }
        
        // Update all structures in the scene with new upgrade limits
        private void UpdateAllStructureLimits()
        {
            // Update walls
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            if (walls != null)
            {
                foreach (GameObject wall in walls)
                {
                    Wall wallComponent = wall.GetComponent<Wall>();
                    if (wallComponent != null)
                    {
                        // Force update of coin user data
                        wallComponent.RefreshUpgradeStatus();
                    }
                }
                Debug.Log($"Updated upgrade limits for {walls.Length} walls based on new town center level {currentTownCenterLevel}");
            }
            
            // Update other structures that need to check upgrade limits
            // (add more as needed)
        }
    }
}
