using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Script.Game.Structures.TownCenter;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine.Serialization;

namespace Script.Data_Model
{
    public class PrefabData : MonoBehaviour
    {
     
        public static PrefabData Instance { get; private set; }

        [Header("Player / Camera")]
        public GameObject player;
        public Camera mainCamera;
        
        // Biến kiểm soát quá trình xây dựng (bỏ qua quá trình xây dựng hoặc yêu cầu thợ xây)
        public bool skipBuildingProcess = true; 
        
        public List<string> toolTag = new List<string> { "Hammer", "Bow" };
        
        [Header("Player Character")] [Space(5)]
        public List<GameObject> playerCharacterList = new List<GameObject>();

        // New class for simplified player prefabWall paths
        [Serializable]
        private class PlayerPrefabConfig
        {
            public List<string> playerPrefabPaths;
        }
       
        [Space(15)] [Header("Map Generation")] [Space(5)]
        #region Map Generation
        public int currentCreatChunk = 21;
        #endregion
        
        [Header("Wall ")] [Space(5)]
        
        public GameObject prefabWall;//prefab wall bình thường ,khi chạy sẽ tự lắp data vào

        [Space(15)] [Header("Arrow ")] [Space(5)]
        public GameObject arrow;
        
        [Space(15)] 
        [Header("Coin ")]
        [Space(5)] 
        #region Coin
        public GameObject coinPrefab;
        
        public float coinMoveSpeed = 0.5f;
        public int fallSpeed = 15;
        public float timeMaxFloatCoin = 3f;
        #endregion
        
        
        [Space(15)] [Header("TownCenter ")] [Space(5)]  
        #region TownCenter
        public List<GameObject> townCenterList = new List<GameObject>();
        public float plazaDistance = 30f;//khoảng cách quảng trường
        public GameObject tent;// for towncenter tier 2
        #endregion

        [Space(15)] [Header("Farm ")] [Space(5)]  
        #region Farm

        public GameObject farmWaterStream0;
        public GameObject farmWell1;
        public GameObject farmHouse2;
        public GameObject farmPlot;

        #endregion

        [Space(15)] [Header("Tower ")] [Space(5)]  
        #region Tower

        
        
        public List<GameObject> towerList = new List<GameObject>();
        
        #endregion

        [Space(15)] [Header("OnUpgradeComplete Tech ")] [Space(5)]  
        #region UpgradeTech
    
        // Tech variables have been moved to WorldInfo
        // Access them through WorldFileManager.Instance.worldInfo instead
        
        public GameObject upgradeHouse;
        
        // Method to update tech status and save to worldInfo
        public void UpdateTechStatus(bool fireArrow, bool penetrating, bool forge, bool adhesive)
        {
            // Use WorldFileManager.Instance to update and save tech status
            if (Save_Controller.WorldFileManager.Instance != null)
            {
                Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    fireArrow, penetrating, forge, adhesive);
            }
        }

        // Individual methods for updating each tech
        public void SetFireArrowTech(bool value) 
        {
            if (Save_Controller.WorldFileManager.Instance != null)
            {
                Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow = value;
                Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    value, 
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetPenetratingTech(bool value) 
        {
            if (Save_Controller.WorldFileManager.Instance != null)
            {
                Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating = value;
                Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    value,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetForgeTech(bool value) 
        {
            if (Save_Controller.WorldFileManager.Instance != null)
            {
                Save_Controller.WorldFileManager.Instance.worldInfo.TechForge = value;
                Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    value,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetAdhesiveTech(bool value) 
        {
            if (Save_Controller.WorldFileManager.Instance != null)
            {
                Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive = value;
                Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
                    value);
            }
        }

        #endregion

        [Space(15)] [Header("OtherStruction ")] [Space(5)]  
        #region OtherKingdomStruction

        public GameObject siegeWorkshop;
        public GameObject bowVendor;
        public GameObject hammerVendor;
        public GameObject oilBarrelVendor;

        #endregion

        [Space(15)] [Header("Subjects ")] [Space(5)]  
        #region Subjects
        
        public GameObject catapult;
        
        
        public GameObject denizen;//base prefab for village,archer,vagrant,... 
        #endregion
        [FormerlySerializedAs("Bow")] [Space(15)] [Header("ItemItem ")] [Space(5)]  
        #region ItemItem
        
        public GameObject bow;
        public GameObject hammer;
       
        #endregion
        [Space(15)] [Header("Subjects ")] [Space(5)]  
        #region Nature
        public GameObject tree;//đây là tree nguyên bản,nhưng khi lấy tree thì phải dùng hàm get random tree
        
        // Thêm danh sách prefabWall Tree
        [FormerlySerializedAs("treePrefabs")] [Header("Tree Prefabs")]
        public List<GameObject> treePrefabsList = new();
        
        private Dictionary<string, GameObject> prefabDict = new();
        
        public GameObject GetRandomTree()
        {
            if (treePrefabsList.Count == 0)
            {
                Debug.LogWarning("Danh sách treePrefabsList đang rỗng.");
                return null;
            }
            
            int randomIndex = UnityEngine.Random.Range(0, treePrefabsList.Count);
            return treePrefabsList[randomIndex];
        }
        public int treeChopCost =1;//số xu cần dể chặt cây
        public int treeMaxWorkProgress = 5;//số lần cần để chặt cây

        [Space(15)] 
        public int maxDistanceFromBush = 5;
        public GameObject rabbitBush;
        public int maxRabbitLimit;
        
        [Space(15)] 
        public GameObject rabbit;
        public int rabbitHp =1;
        public int rabbitSpeed = 1;
        public int rabbitDieReward = 1;
        #endregion

        #region EnemyStruction

        [Space(15)] [Header("EnemyStruction ")] [Space(5)]

        public GameObject summoningPortal;
        
        [Space(5)]
        [Header("Enemy Interaction")]
        [Tooltip("Danh sách các Tag vật phẩm mà Enemy có thể nhặt, ví dụ Coin hoặc Tool")]
        public List<string> enemyLootTags = new List<string> { "Coin", "Tool" };
        
        #endregion

        [Space(15)] [Header("GUI ")] 
        public GameObject gameObjectHaveCanvas;
        public GameObject confirmationPanel;
        
        [Space(5)]  
        [Header("other ")]
        //danh sách những thứ có coinUser và tag để hiện coin slot
        public List<string> playerCoinUserInteractable ;
        
        // Danh sách các tag của các đối tượng được coi là đồng minh (Ally Tags)
        public List<string> allyTags = new List<string> { "Player", "Archer", "Wall","Villager" };
        
        // Danh sách tag mục tiêu săn bắn cho Archer
        public List<string> huntingTargetTags = new List<string> { "Animal" };

        [Header("Debug / Settings")]
        public bool allowAllyDamage = false; // Toggle friendly fire logic
      
        public readonly Dictionary<string, int> productPrices = new Dictionary<string, int>();


        
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
            
            // Đăng ký các prefabWall
            RegisterPrefabs();
            
			//danh sách chứa tag mà player có thể tương tác bằng trigger helper
			playerCoinUserInteractable = new List<string> {"Wall","Structure","Tree","TownCenter",
				"BowVendor","HammerVendor","ScytheVendor","SwordVendor","SpikeVendor"}; 

            // Load player characters from JSON first
            LoadPlayerCharactersFromJson();

            // Loại bỏ tất cả các phần tử null khỏi danh sách charactor
            playerCharacterList.RemoveAll(item => item == null);
            RequirePrefabCheck();

            // Load dynamic config JSON for vendor prices and ally tags
            LoadDynamicConfig();
            
            if (Save_Controller.WorldFileManager.Instance == null)
            {
                Debug.LogError("[PrefabData] WorldFileManager không tìm thấy trong scene! Hãy thêm nó vào GameplayScene.");
            }
        }
        
        /// <summary>
        /// Đăng ký tất cả các prefabWall vào từ điển _prefabDict
        /// </summary>
        private void RegisterPrefabs()
        {
            // Đăng ký các prefabWall TownCenter
            for (int i = 0; i < townCenterList.Count; i++)
            {
                prefabDict.Add($"town_center_{i}", townCenterList[i]);
            }
            // Đăng ký các prefabWall tree
            for(int i = 0; i < treePrefabsList.Count; i++)
            {
                prefabDict.Add($"tree_{i}", treePrefabsList[i]);
            }
            // Đăng ký các prefabWall tower
            for (int i = 0; i < towerList.Count; i++)
            {
                prefabDict.Add($"tower_{i}", towerList[i]);
            }
            // Register vendor prefabs
            prefabDict.Add("bow_vendor", bowVendor);
            prefabDict.Add("hammer_vendor", hammerVendor);
            prefabDict.Add("scythe_vendor", oilBarrelVendor);
            prefabDict.Add("sword_vendor", siegeWorkshop);
            prefabDict.Add("spike_vendor", upgradeHouse);
            prefabDict.Add("wall", prefabWall);
            prefabDict.Add("coin", coinPrefab);
            prefabDict.Add("denizen", denizen);
            prefabDict.Add("EnemyPortalPrefab_ID", summoningPortal);
            
        }
        
        public GameObject GetPrefabByID(string id)
        {
            return prefabDict.TryGetValue(id, out GameObject prefab) ? prefab : null;
        }

        // Typed getters for safety/clarity
        public GameObject GetTownCenterByLevel(int level)
        {
            if (level < 0 || level >= townCenterList.Count) return null;
            return townCenterList[level];
        }

        public GameObject GetRandomTreePrefab()
        {
            return GetRandomTree();
        }
        private void RequirePrefabCheck()
        {
            AddCurrentPlayerCharacter();
            FindMainCamera();
        }
        public void FindMainCamera()
        {
            if (mainCamera == null)
            {
                // Tìm camera dựa trên tag "MainCamera"
                mainCamera = Camera.main;

                // Nếu không tìm thấy camera có tag "MainCamera"
                if (mainCamera == null)
                {
                    // Tìm tất cả các Camera trong Scene
                    Camera[] cameras = FindObjectsOfType<Camera>();

                    if (cameras.Length > 0)
                    {
                        mainCamera = cameras[0]; // Gán camera đầu tiên tìm thấy
                        Debug.LogWarning("No camera with 'MainCamera' tag found. Using the first available camera.");
                    }
                    else
                    {
                        Debug.LogError("No camera found in the scene!");
                    }
                }
                else
                {
                  //  Debug.Log("Main camera found and assigned.");
                }
            }
            else
            {
                Debug.Log("Main camera is already assigned.");
            }
        }

        public void AddCurrentPlayerCharacter()
        {
            if (player == null)
            {
                
                // Kiểm tra nếu danh sách rỗng
                if (playerCharacterList is { Count: > 0 })
                {
                    player = playerCharacterList[0]; // Lấy phần tử đầu tiên trong danh sách
                }
                else
                {
                    Debug.LogWarning("PlayerCharactorList is empty or null. Cannot assign player.");
                }
            }
        }
        

        [Serializable]
        public class BuildingData
        {
            public GameObject prefab;
            public float requiredDistance;
        }

        //list này chứ thông số về khoảng cách các gameobejtc với nhau
        public List<BuildingData> buildingDataList = new List<BuildingData>();

        public BuildingData GetBuildingData(GameObject prefab)
        {
            return buildingDataList.Find(data => data.prefab == prefab);
        }

		private void Start()
        {
            if (townCenterList.Count <= 0)
            {
                Debug.LogError("thiếu dữ liệu về towncenter,hãy thêm nó vào prefabdata.towncenterlist ");
            }
            
            // Validate vendor prefabs
            ValidateVendorPrefabs();
        }

        // Dynamic config structures
        [Serializable]
        private class VendorPriceEntry
        {
            public string productId;
            public int price;
        }

        [Serializable]
        private class AllyTagsWrapper
        {
            public List<string> allyTags;
        }

        [Serializable]
        private class DynamicConfigRoot
        {
            public List<VendorPriceEntry> vendorPrices;
            public AllyTagsWrapper allyTags;
        }

        private void LoadDynamicConfig()
        {
            try
            {
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/DynamicGameConfig");
                if (jsonTextAsset == null)
                {
                    Debug.LogWarning("DynamicGameConfig.json not found in Resources/DataConfig; using defaults if any.");
                    return;
                }

                var root = JsonConvert.DeserializeObject<DynamicConfigRoot>(jsonTextAsset.text);
                if (root != null)
                {
                    // Load vendor prices
                    productPrices.Clear();
                    if (root.vendorPrices != null)
                    {
                        foreach (var vp in root.vendorPrices)
                        {
                            if (vp != null && !string.IsNullOrEmpty(vp.productId))
                            {
                                productPrices[vp.productId] = vp.price;
                            }
                        }
                    }

                    // Load ally tags
                    if (root.allyTags != null && root.allyTags.allyTags != null && root.allyTags.allyTags.Count > 0)
                    {
                        allyTags = new List<string>(root.allyTags.allyTags);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load DynamicGameConfig: {e.Message}");
            }
        }

  
        
        private void ValidateVendorPrefabs()
        {
            // Check if vendor prefabs are assigned
            if (bowVendor == null) Debug.LogError("bowVendor prefabWall is missing!");
            if (hammerVendor == null) Debug.LogError("hammerVendor prefabWall is missing!");
            if (oilBarrelVendor == null) Debug.LogError("oilBarrelVendor prefabWall is missing!");
            if (siegeWorkshop == null) Debug.LogError("siegeWorkshop prefabWall is missing!");
            if (upgradeHouse == null) Debug.LogError("upgradeHouse prefabWall is missing!");
            if (coinPrefab == null) Debug.LogError("coinPrefab is missing!");
            
            // Check if prefabs are properly registered in dictionary
            //Debug.Log("Checking vendor prefabWall registrations...");
            if (!prefabDict.ContainsKey("bow_vendor")) 
                Debug.LogError("bow_vendor not registered in prefabWall dictionary!");
            if (!prefabDict.ContainsKey("hammer_vendor")) 
                Debug.LogError("hammer_vendor not registered in prefabWall dictionary!");
            if (!prefabDict.ContainsKey("scythe_vendor")) 
                Debug.LogError("scythe_vendor not registered in prefabWall dictionary!");
            if (!prefabDict.ContainsKey("sword_vendor")) 
                Debug.LogError("sword_vendor not registered in prefabWall dictionary!");
            if (!prefabDict.ContainsKey("spike_vendor")) 
                Debug.LogError("spike_vendor not registered in prefabWall dictionary!");
            if (!prefabDict.ContainsKey("coin")) 
                Debug.LogError("coin not registered in prefabWall dictionary!");
        }
        
        // New method to load player character data from JSON
        public void LoadPlayerCharactersFromJson()
        {
            //Debug.Log("Starting to load player characters from JSON...");
            try
            {
                // Clear existing player character list
                playerCharacterList.Clear();
                
                // Try to load the JSON file from Resources
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/Player/PlayerCharacterConfig");
                if (jsonTextAsset != null)
                {
                    //Debug.Log($"Successfully loaded PlayerCharacterConfig.json: {jsonTextAsset.text}");
                    
                    // Deserialize the JSON
                    var config = JsonConvert.DeserializeObject<PlayerPrefabConfig>(jsonTextAsset.text);
                    if (config?.playerPrefabPaths is { Count: > 0 })
                    {
                       // Debug.Log($"Found {config.playerPrefabPaths.Count} prefabWall paths in the JSON file.");
                        
                        foreach (var prefabPath in config.playerPrefabPaths)
                        {
                           // Debug.Log($"Attempting to load prefabWall from path: {prefabPath}");
                            
                            // Try to load the prefabWall directly
                            GameObject prefab = Resources.Load<GameObject>(prefabPath);
                            
                            if (prefab != null)
                            {
                                playerCharacterList.Add(prefab);
                               // Debug.Log($"SUCCESS: Added player prefabWall from path: {prefabPath}");
                            }
                            else
                            {
                                Debug.LogError($"FAILED: Could not load player prefabWall from path: {prefabPath}");
                                // Try to list available resources in the directory
                                string directory = System.IO.Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
                              //  Debug.Log($"Checking directory: {directory}");
                                UnityEngine.Object[] resources = Resources.LoadAll(directory);
                              //  Debug.Log($"Found {resources.Length} resources in directory {directory}:");
                                foreach (var resource in resources)
                                {
                                    Debug.Log($"- {resource.name} (Type: {resource.GetType()})");
                                }
                            }
                        }
                        
                        // Set the default player if we loaded any prefabs
                        if (playerCharacterList.Count > 0)
                        {
                            player = playerCharacterList[0];
                            Debug.Log($"Set default player to: {player.name}");
                        }
                        else
                        {
                            Debug.LogError("No prefabs were successfully loaded into playerCharacterList!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No player prefabWall paths found in PlayerCharacterConfig.json");
                    }
                }
                else
                {
                    Debug.LogError("PlayerCharacterConfig.json not found in Resources/DataConfig");
                    
                    // List available files in the DataConfig directory to help debugging
                    UnityEngine.Object[] dataConfigFiles = Resources.LoadAll("DataConfig");
                    Debug.Log($"Found {dataConfigFiles.Length} files in Resources/DataConfig:");
                    foreach (var file in dataConfigFiles)
                    {
                        Debug.Log($"- {file.name} (Type: {file.GetType()})");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading player prefabs: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        private void OnValidate()
        {
            // Basic validation to prevent duplicates/nulls in prefabWall dictionary and lists
            if (townCenterList != null)
            {
                townCenterList.RemoveAll(item => item == null);
            }
            if (treePrefabsList != null)
            {
                treePrefabsList.RemoveAll(item => item == null);
            }
          

            // Ensure product prices has expected keys (can be moved to separate config later)
            productPrices.TryAdd("BowVendor", 2);
            productPrices.TryAdd("HammerVendor", 3);
        }
    }
}
