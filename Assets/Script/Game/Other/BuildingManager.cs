using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Script.Data_Model;
using UnityEngine;
using Script.Game.Structures.TownCenter;
using UnityEngine.Serialization; // Để dùng TownCenterDataList
using Script; // Import Utility

namespace Script.Game.Other
{
    // Các loại công trình (có thể mở rộng thêm)
    public enum BuildingType
    {
        TownCenter,
        Wall,
        HammerVendor,
        BowVendor,
        // ...
    }

    [Serializable]
    public class BuildingUnlockRule
    {
        public BuildingType buildingType;
        [Tooltip("Số lượng tối đa được phép xây dựng")]
        public int maxQuantity;
        [Tooltip("Cấp tối đa mà công trình đó được nâng cấp")]
        public int maxUpgradeLevel;
    }

    [Serializable]
    public class BuildingUnlockConfig
    {
        public int tier;
        [Tooltip("Danh sách quy tắc mở khóa cho các công trình khi TownCenter đạt cấp này")]
        public List<BuildingUnlockRule> unlockRules;
    }

    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager instance;
        [Header("Town Center Settings")]
        [Tooltip("Mảng prefab của TownCenter (index 0: lvl1, index 1: lvl2, ...)")]
        public GameObject[] townCenterPrefabs;
        
        [Header("Other Structures Prefabs")]
        [Tooltip("Prefab tường cơ bản (phiên bản lvl1)")]
        public GameObject wallPrefabLv1;
        [FormerlySerializedAs("hammerSellerPrefab")] public GameObject hammerVendorPrefab;
        [FormerlySerializedAs("bowSellerPrefab")] public GameObject bowVendorPrefab;
        
        [Header("Building Unlock Configurations")]
        public List<BuildingUnlockConfig> buildingUnlockConfigs = new List<BuildingUnlockConfig>();
        
        // TownCenter hiện có (chỉ có 1 TownCenter)
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
            InitStructure();
            InitializeBuildingUnlockConfigs();
            CheckForExistingTownCenter();
            UpdateAvailableBuildings();
        }
        
        private void InitStructure()
        {
            // Giả sử PrefabData.Instance.townCenterList là List<GameObject> đã được thiết lập
            townCenterPrefabs = PrefabData.Instance.townCenterList.ToArray();
            hammerVendorPrefab = PrefabData.Instance.hammerVendor;
            bowVendorPrefab = PrefabData.Instance.bowVendor;
        }
        
        /// <summary>
        /// Khởi tạo cấu hình mở khóa cho các cấp của TownCenter (chỉ dùng cho việc mở khóa các công trình, không dùng để nâng cấp TownCenter).
        /// </summary>
        private void InitializeBuildingUnlockConfigs()
        {
            // Lưu ý, phải khai báo lại toàn bộ những công trình ở tier trước
            if (buildingUnlockConfigs == null || buildingUnlockConfigs.Count == 0)
            {
                buildingUnlockConfigs = new List<BuildingUnlockConfig>();
                
                // Tier 1: Khi TownCenter đạt cấp 1
                BuildingUnlockConfig config1 = new BuildingUnlockConfig();
                config1.tier = 1;
                config1.unlockRules = new List<BuildingUnlockRule>
                {
                    new BuildingUnlockRule { buildingType = BuildingType.Wall, maxQuantity = 2, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.HammerVendor, maxQuantity = 1, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.BowVendor, maxQuantity = 1, maxUpgradeLevel = 1 }
                };
                buildingUnlockConfigs.Add(config1);
                
                // Tier 2: Khi TownCenter đạt cấp 2
                BuildingUnlockConfig config2 = new BuildingUnlockConfig();
                config2.tier = 2;
                config2.unlockRules = new List<BuildingUnlockRule>
                {
                    new BuildingUnlockRule { buildingType = BuildingType.Wall, maxQuantity = 4, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.HammerVendor, maxQuantity = 1, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.BowVendor, maxQuantity = 1, maxUpgradeLevel = 1 }
                };
                buildingUnlockConfigs.Add(config2);
                
                // Tier 3: Khi TownCenter đạt cấp 3
                BuildingUnlockConfig config3 = new BuildingUnlockConfig();
                config3.tier = 3;
                config3.unlockRules = new List<BuildingUnlockRule>
                {
                    new BuildingUnlockRule { buildingType = BuildingType.Wall, maxQuantity = 4, maxUpgradeLevel = 2 },
                    new BuildingUnlockRule { buildingType = BuildingType.HammerVendor, maxQuantity = 1, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.BowVendor, maxQuantity = 1, maxUpgradeLevel = 1 }
                };
                buildingUnlockConfigs.Add(config3);
                
                // Tier 4: Khi TownCenter đạt cấp 4 (cấp cuối)
                BuildingUnlockConfig config4 = new BuildingUnlockConfig();
                config4.tier = 4;
                config4.unlockRules = new List<BuildingUnlockRule>
                {
                    new BuildingUnlockRule { buildingType = BuildingType.Wall, maxQuantity = 4, maxUpgradeLevel = 2 },
                    new BuildingUnlockRule { buildingType = BuildingType.HammerVendor, maxQuantity = 1, maxUpgradeLevel = 1 },
                    new BuildingUnlockRule { buildingType = BuildingType.BowVendor, maxQuantity = 1, maxUpgradeLevel = 1 }
                };
                buildingUnlockConfigs.Add(config4);
            }
        }
        
        /// <summary>
        /// Kiểm tra xem đã có TownCenter trong scene chưa (dựa theo tag "TownCenter")
        /// </summary>
        public void CheckForExistingTownCenter()
        {
            // Kiểm tra nếu SetUpContainer hoặc structureContainer chưa được thiết lập
            if (SetUpContainer.Instance == null || SetUpContainer.Instance.structureContainer == null)
            {
                Debug.LogWarning("Container chưa được thiết lập!");
                return;
            }
    
            // Duyệt qua tất cả các đối tượng con trong structureContainer
            foreach (GameObject structure in SetUpContainer.Instance.GetObjectsFromContainerList(SetUpContainer.Instance.structureContainer))
            {
                if (structure.CompareTag("TownCenter"))
                {
                    currentTownCenter = structure;
                    Debug.Log("Tìm thấy TownCenter trong container: " + currentTownCenter.name);
                    return;
                }
            }
    
            Debug.LogWarning("Không tìm thấy TownCenter trong container!");
        }

        /// <summary>
        /// Cập nhật danh sách prefab có thể xây dựa trên cấp của TownCenter và cấu hình mở khóa trong BuildingUnlockConfigs.
        /// Nếu chưa có TownCenter, chỉ cho phép xây dựng TownCenter (prefab tier 1).
        /// Nếu đã có, dựa theo cấp hiện tại của TownCenter, duyệt qua cấu hình mở khóa của cấp đó và thêm các prefab tương ứng nếu số lượng đã xây chưa đạt giới hạn.
        /// </summary>
        public void UpdateAvailableBuildings()
        {
            // Nếu mảng townCenterPrefabs chưa được khởi tạo hoặc rỗng, kiểm tra xem đã có TownCenter trong container chưa
            if (townCenterPrefabs == null || townCenterPrefabs.Length == 0)
            {
                CheckForExistingTownCenter();
            }
            
            // Danh sách prefab có thể xây dựng
            List<GameObject> availablePrefabs = new List<GameObject>();

            // Nếu chưa có TownCenter nào được xây dựng, chỉ cho phép xây TownCenter (prefab tier 1)
            if (currentTownCenter == null)
            {
                if (townCenterPrefabs != null && townCenterPrefabs.Length > 0 && townCenterPrefabs[0] != null)
                {
                    availablePrefabs.Add(townCenterPrefabs[0]);
                }
            }
            else
            {
                // Nếu đã có TownCenter, lấy component TownCenter để truy xuất cấp (level/tier)
                TownCenter tcScript = currentTownCenter.GetComponent<TownCenter>();
                if (tcScript == null)
                {
                    Debug.LogWarning("Không tìm thấy component TownCenter trên currentTownCenter.");
                    return;
                }

                int currentTier = tcScript.level;
                Debug.Log("Current TownCenter Level: " + currentTier);

                // Quy tắc mới: Nếu TownCenter tồn tại nhưng level bằng 0 thì xóa danh sách buildableStructures
                if (currentTier == 0)
                {
                    if (BuildMenu.instance != null)
                    {
                        BuildMenu.instance.buildableStructures.Clear();
                        Debug.Log("TownCenter level is 0, cleared buildable structures.");
                    }
                    else
                    {
                        Debug.LogWarning("BuildMenu instance chưa được thiết lập.");
                    }
                    return;
                }

                // Lấy cấu hình mở khóa tương ứng với cấp hiện tại của TownCenter
                BuildingUnlockConfig config = buildingUnlockConfigs.Find(c => c.tier == currentTier);
                if (config == null)
                {
                    Debug.LogWarning("Không tìm thấy cấu hình mở khóa cho TownCenter cấp " + currentTier);
                }
                else
                {
                    // Duyệt qua từng quy tắc mở khóa để thêm prefab nếu số lượng đã xây chưa vượt quá giới hạn
                    foreach (var rule in config.unlockRules)
                    {
                        int builtCount = CountBuiltBuildings(rule.buildingType);
                        if (builtCount < rule.maxQuantity)
                        {
                            GameObject prefab = GetPrefabForBuildingType(rule.buildingType);
                            if (prefab != null)
                            {
                                availablePrefabs.Add(prefab);
                            }
                            else
                            {
                                Debug.LogWarning("Không tìm thấy prefab cho loại công trình: " + rule.buildingType);
                            }
                        }
                    }
                }
            }
            
            // Cập nhật danh sách các công trình có thể xây dựng trong BuildMenu
            if (BuildMenu.instance != null)
            {
                BuildMenu.instance.buildableStructures = availablePrefabs;
            }
            else
            {
                Debug.LogWarning("BuildMenu instance chưa được thiết lập trong BuildingManager.");
            }
        }

        /// <summary>
        /// Ánh xạ từ BuildingType sang prefab tương ứng.
        /// Mở rộng thêm khi có loại công trình mới.
        /// </summary>
        private GameObject GetPrefabForBuildingType(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.TownCenter:
                    return (townCenterPrefabs != null && townCenterPrefabs.Length > 0) ? townCenterPrefabs[0] : null;
                case BuildingType.Wall:
                    return wallPrefabLv1;
                case BuildingType.HammerVendor:
                    return hammerVendorPrefab;
                case BuildingType.BowVendor:
                    return bowVendorPrefab;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Đếm số lượng công trình đã xây dựa trên tag (hãy đảm bảo các prefab được gán tag đúng)
        /// </summary>
        private int CountBuiltBuildings(BuildingType type)
        {
            string tag = "";
            switch (type)
            {
                case BuildingType.TownCenter:
                    tag = "TownCenter";
                    break;
                case BuildingType.Wall:
                    tag = "Wall";
                    break;
                case BuildingType.HammerVendor:
                    tag = "HammerVendor";
                    break;
                case BuildingType.BowVendor:
                    tag = "BowVendor";
                    break;
            }
            return string.IsNullOrEmpty(tag) ? 0 : GameObject.FindGameObjectsWithTag(tag).Length;
        }
        
        private void OnDisable()
        {
            Debug.LogError($"{gameObject.name} was disabled.\nCall Stack:\n{Environment.StackTrace}");
        }
        
        // Sửa hàm RotateTowardsTownCenter để sử dụng chung hàm xoay từ Utility
        public void RotateTowardsTownCenter(GameObject obj)
        {
            if (currentTownCenter == null || obj == null)
            {
                return;
            }
    
            // Dùng vị trí của chính đối tượng (wall) làm điểm tham chiếu
            Vector3 objPos = obj.transform.position;
            Vector3 townCenterPos = currentTownCenter.transform.position;
            Utility.RotateObjectTowardsTownCenter(obj, objPos, townCenterPos);
        }



    }
}
