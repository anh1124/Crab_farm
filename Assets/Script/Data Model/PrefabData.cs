using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection; // Import thư viện I/O để ghi file
using System.Text;
using Newtonsoft.Json;
using Script.Game.Structures.TownCenter;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine.Serialization;

namespace Script.Data_Model
{
    public class PrefabData : MonoBehaviour
    {
     
        public static PrefabData Instance { get; private set; }

        public GameObject player;
        public float playerSpeed = 7;
        public Camera mainCamera;
        
        // Biến kiểm soát quá trình xây dựng (bỏ qua quá trình xây dựng hoặc yêu cầu thợ xây)
        public bool skipBuildingProcess = true; 
        
        public List<string> ToolTag = new List<string> { "Hammer", "Bow" };
        
        [Header("Player Character")] [Space(5)]
        public List<GameObject> playerCharacterList = new List<GameObject>();

        // New class for simplified player prefab paths
        [System.Serializable]
        private class PlayerPrefabConfig
        {
            public List<string> playerPrefabPaths;
        }
       
        [Space(15)] [Header("Map Generation")] [Space(5)]
        #region Map Generation
        public int currentCreatChunk = 21;
        #endregion
        
        [Header("Wall and Stump ")] [Space(5)]
        #region Wall and stump
        public GameObject wallDirtMound0;//this not a wall,just a stumb  
        
        public List<GameObject> wallList = new List<GameObject>();
        public List<GameObject> stumpList = new List<GameObject>();
        
        #endregion
        
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
        public float plazaDistance = 10f;//khoảng cách quảng trường
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

        [Space(15)] [Header("Upgrade Tech ")] [Space(5)]  
        #region UpgradeTech
    
        // Tech variables have been moved to WorldInfo
        // Access them through WorldFileManager.Instance.worldInfo instead
        
        public GameObject upgradeHouse;
        
        // Method to update tech status and save to worldInfo
        public void UpdateTechStatus(bool fireArrow, bool penetrating, bool forge, bool adhesive)
        {
            // Use WorldFileManager.Instance to update and save tech status
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    fireArrow, penetrating, forge, adhesive);
            }
        }

        // Individual methods for updating each tech
        public void SetFireArrowTech(bool value) 
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow = value;
                Script.Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    value, 
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetPenetratingTech(bool value) 
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating = value;
                Script.Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    value,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetForgeTech(bool value) 
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechForge = value;
                Script.Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    value,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive);
            }
        }
        
        public void SetAdhesiveTech(bool value) 
        {
            if (Script.Save_Controller.WorldFileManager.Instance != null)
            {
                Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechAdhesive = value;
                Script.Save_Controller.WorldFileManager.Instance.SaveTechStatus(
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechFireArrow,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechPenetrating,
                    Script.Save_Controller.WorldFileManager.Instance.worldInfo.TechForge,
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
        public GameObject vagrant;
        public GameObject village;
        #endregion
        
        [Space(15)] [Header("Subjects ")] [Space(5)]  
        #region Nature
        public GameObject tree;//đây là tree nguyên bản,nhưng khi lấy tree thì phải dùng hàm get random tree
        
        // Thêm danh sách prefab Tree
        [FormerlySerializedAs("treePrefabs")] [Header("Tree Prefabs")]
        public List<GameObject> treePrefabsList = new();
        
        private Dictionary<string, GameObject> _prefabDict = new();
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
        public int TreeMaxWorkProgress = 5;//số lần cần để chặt cây

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
        
        #endregion

        [Space(15)] [Header("GUI ")] 
        public GameObject gameObjectHaveCanvas;
        public GameObject confirmationPanel;
        
        [Space(5)]  
        [Header("other ")]
        //danh sách những thứ có coinUser và tag để hiện coin slot
        public List<string> playerCoinUserInteractable ; 
      
        public readonly Dictionary<string, int> productPrices = new Dictionary<string, int>
        {
            { "BowVendor", 2 },
            { "HammerVendor", 3 },
            { "ScytheVendor", 3 },
            { "SwordVendor", 3 },
            { "SpikeVendor", 3 },
        };

        
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
            
            // Đăng ký các prefab
            RegisterPrefabs();
            
            //danh sách chứa tag mà player có thể tương tác bằng trigger helper
            playerCoinUserInteractable = new List<string> {"Wall","Structure","Tree","TownCenter",
                "BowVendor","HammerVendor","ScytheVendor","SwordVendor","SpikeVendor"}; 
            CallOtherListToGetData();
            
            // Loại bỏ tất cả các phần tử null khỏi danh sách charactor 
            playerCharacterList.RemoveAll(item => item == null);
            RequirePrefabCheck();
        }
        
        /// <summary>
        /// Đăng ký tất cả các prefab vào từ điển _prefabDict
        /// </summary>
        private void RegisterPrefabs()
        {
            // Đăng ký các prefab TownCenter
            for (int i = 0; i < townCenterList.Count; i++)
            {
                _prefabDict.Add($"town_center_{i}", townCenterList[i]);
            }
            // Đăng ký các prefab tree
            for(int i = 0; i < treePrefabsList.Count; i++)
            {
                _prefabDict.Add($"tree_{i}", treePrefabsList[i]);
            }

            // Đăng ký các prefab wall với định dạng "wall_{tier}"
            _prefabDict.Add("wall_0", wallDirtMound0);
            for (int i = 0; i < wallList.Count; i++)
            {
                _prefabDict.Add($"wall_{i + 1}", wallList[i]);
            }

            // Đăng ký các prefab stump
            for (int i = 0; i < stumpList.Count; i++)
            {
                _prefabDict.Add($"stump_{i + 1}", stumpList[i]);
            }

            // Đăng ký các prefab tower
            for (int i = 0; i < towerList.Count; i++)
            {
                _prefabDict.Add($"tower_{i}", towerList[i]);
            }
           
            // Register vendor prefabs
            _prefabDict.Add("bow_vendor", bowVendor);
            _prefabDict.Add("hammer_vendor", hammerVendor);
            _prefabDict.Add("scythe_vendor", oilBarrelVendor);
            _prefabDict.Add("sword_vendor", siegeWorkshop);
            _prefabDict.Add("spike_vendor", upgradeHouse);
            
            // Register coin prefab
            if (coinPrefab != null)
            {
                _prefabDict.Add("coin", coinPrefab);
            }
            else
            {
                Debug.LogError("Coin prefab is missing! Make sure to assign it in the inspector.");
            }
            
            // Register vagrant prefab
            if (vagrant != null)
            {
                _prefabDict.Add("vagrant", vagrant);
            }

            // Register villager prefab
            if (village != null)
            {
                _prefabDict.Add("villager", village);
            }
            else
            {
                Debug.LogError("Villager prefab is missing! Make sure to assign it in the inspector.");
            }
        }
        
        public GameObject GetPrefabByID(string id)
        {
            return _prefabDict.TryGetValue(id, out GameObject prefab) ? prefab : null;
        }
        private void RequirePrefabCheck()
        {
            AddCurrentPlayerCharactor();
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
                    Debug.Log("Main camera found and assigned.");
                }
            }
            else
            {
                Debug.Log("Main camera is already assigned.");
            }
        }

        public void AddCurrentPlayerCharactor()
        {
            if (player == null)
            {
                
                // Kiểm tra nếu danh sách rỗng
                if (playerCharacterList != null && playerCharacterList.Count > 0)
                {
                    player = playerCharacterList[0]; // Lấy phần tử đầu tiên trong danh sách
                }
                else
                {
                    Debug.LogWarning("PlayerCharactorList is empty or null. Cannot assign player.");
                }
            }
        }
        private void CallOtherListToGetData()
        {
            WallDataList.Instance.InitializeWallData();
            TownCenterDataList.Instance.InitializeWallData();
        }
        [System.Serializable]
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
        
        private void ValidateVendorPrefabs()
        {
            // Check if vendor prefabs are assigned
            if (bowVendor == null) Debug.LogError("bowVendor prefab is missing!");
            if (hammerVendor == null) Debug.LogError("hammerVendor prefab is missing!");
            if (oilBarrelVendor == null) Debug.LogError("oilBarrelVendor prefab is missing!");
            if (siegeWorkshop == null) Debug.LogError("siegeWorkshop prefab is missing!");
            if (upgradeHouse == null) Debug.LogError("upgradeHouse prefab is missing!");
            if (coinPrefab == null) Debug.LogError("coinPrefab is missing!");
            
            // Check if prefabs are properly registered in dictionary
            Debug.Log("Checking vendor prefab registrations...");
            if (!_prefabDict.ContainsKey("bow_vendor")) 
                Debug.LogError("bow_vendor not registered in prefab dictionary!");
            if (!_prefabDict.ContainsKey("hammer_vendor")) 
                Debug.LogError("hammer_vendor not registered in prefab dictionary!");
            if (!_prefabDict.ContainsKey("scythe_vendor")) 
                Debug.LogError("scythe_vendor not registered in prefab dictionary!");
            if (!_prefabDict.ContainsKey("sword_vendor")) 
                Debug.LogError("sword_vendor not registered in prefab dictionary!");
            if (!_prefabDict.ContainsKey("spike_vendor")) 
                Debug.LogError("spike_vendor not registered in prefab dictionary!");
            if (!_prefabDict.ContainsKey("coin")) 
                Debug.LogError("coin not registered in prefab dictionary!");
        }
        
        // New method to load player character data from JSON
        public void LoadPlayerCharactersFromJson()
        {
            Debug.Log("Starting to load player characters from JSON...");
            try
            {
                // Clear existing player character list
                playerCharacterList.Clear();
                
                // Try to load the JSON file from Resources
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/Player/PlayerCharacterConfig");
                if (jsonTextAsset != null)
                {
                    Debug.Log($"Successfully loaded PlayerCharacterConfig.json: {jsonTextAsset.text}");
                    
                    // Deserialize the JSON
                    var config = JsonConvert.DeserializeObject<PlayerPrefabConfig>(jsonTextAsset.text);
                    if (config?.playerPrefabPaths != null && config.playerPrefabPaths.Count > 0)
                    {
                       // Debug.Log($"Found {config.playerPrefabPaths.Count} prefab paths in the JSON file.");
                        
                        foreach (var prefabPath in config.playerPrefabPaths)
                        {
                            Debug.Log($"Attempting to load prefab from path: {prefabPath}");
                            
                            // Try to load the prefab directly
                            GameObject prefab = Resources.Load<GameObject>(prefabPath);
                            
                            if (prefab != null)
                            {
                                playerCharacterList.Add(prefab);
                                Debug.Log($"SUCCESS: Added player prefab from path: {prefabPath}");
                            }
                            else
                            {
                                Debug.LogError($"FAILED: Could not load player prefab from path: {prefabPath}");
                                // Try to list available resources in the directory
                                string directory = System.IO.Path.GetDirectoryName(prefabPath).Replace("\\", "/");
                                Debug.Log($"Checking directory: {directory}");
                                UnityEngine.Object[] resources = Resources.LoadAll(directory);
                                Debug.Log($"Found {resources.Length} resources in directory {directory}:");
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
                        Debug.LogWarning("No player prefab paths found in PlayerCharacterConfig.json");
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
        
        
        public bool IsTool(GameObject item)
        {
            return ToolTag.Contains(item.tag);
        }
    }
}
