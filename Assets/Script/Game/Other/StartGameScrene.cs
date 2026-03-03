using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Script.Manager;
using System.Linq;
using Script.Controller.PlayerController;
using Script.Data_Model.Interfaces;

namespace Script.Game.Other
{
    public class StartGameScrene : MonoBehaviour
    {
        public GameObject pauseMenuPanel;
        
        public void Start()
        {
            try
            {
                //Debug.Log("🚀 Bắt đầu load game với thứ tự phase tối ưu...");

                // ===== PHASE 1: CORE DATA =====
                // Load tất cả data lists cần thiết cho game (WallDataList, TownCenterDataList, etc.)
                // Đây là data cơ bản phải có trước khi spawn bất kỳ entity nào
                //Debug.Log("📦 PHASE 1: CORE DATA - Loading essential data lists...");
                //Debug.Log("📦 PHASE 1: CORE DATA - Loading essential data lists...");
                InitializeCoreData(); // Init PrefabData và bootstrap system (tự động load các datalist-towncenterlistdata theo thứ tự đã đăng ký trong bootstrap)



                // ===== PHASE 2: WORLD INFO =====
                // Load world information (tech, time, weather) - thông tin context cho save/load
                // Phải load trước save để có data context khi quyết định spawn entities
                //Debug.Log("🌍 PHASE 2: WORLD INFO - Loading world context data...");
                // Kiểm tra và load data thế giới đang hoạt động (hoặc hủy load nếu thất bại)
                if (!WorldFileManager.Instance.ValidateAndLoadActiveWorld())
                {
                    return; // Load thất bại, đã tự động quay về menu
                }
                ValidateWorldData();

                // ===== PHASE 3: UI SETUP =====
                // Setup UI components trước khi player spawn
                // Đảm bảo UI ready khi player cần interact
                //Debug.Log("🖥️ PHASE 3: UI SETUP - Initializing UI components...");
                SetupGUI(); // Setup pause menu và UI elements

                // ===== PHASE 4: SAVE LOAD ENTITIES =====
                // Load entities từ save file (walls, towncenters, enemies, etc.)
                // Load TRƯỚC player để tránh dependency issues
                //Debug.Log("💾 PHASE 4: SAVE LOAD ENTITIES - Loading saved entities...");
                if (WorldFileManager.Instance.IsActiveWorldPathValid())
                {
                    // NEW: Use refactored LoadFromSave methods
                    LoadFromSave.Instance.LoadTechAndTimeFromSave();
                    LoadFromSave.Instance.LoadEntitiesFromSave();
                }

                // ===== PHASE 5: PLAYER INSTANCE =====
                // Instance/setup player object và camera
                // Player spawn SAU khi entities đã load để tránh conflicts
              //  Debug.Log("👤 PHASE 5: PLAYER INSTANCE - Creating/setup player object...");
                InstancePlayerAndSetCamera(); // Check if loaded from save, else spawn new
                InitializePlayer(); // Setup BuildMenu với player
             

                // Development features
                #if UNITY_EDITOR
                DevelopTest();
                #endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Game loading failed: {ex.Message}\n{ex.StackTrace}");
                // Fallback to main menu on critical error
                SceneManager.LoadScene("MainMenuScene");
            }
        }


        private void SetupGUI()
        {
            //tự động disable khi laod game
            if (pauseMenuPanel == null)
            {
                Debug.LogError("pauseMenuPanel là null");
            }
            else
            {
                pauseMenuPanel.SetActive(true);
            }
        }

        private void InitializePlayer()
        {
            if (BuildMenu.instance != null)
            {
                BuildMenu.instance.InitializePlayer();
            }
            else
            {
                Debug.LogWarning("BuildMenu.instance không tồn tại, không thể khởi tạo player cho BuildMenu");
            }
        }

        // ==================== PHASED LOADING METHODS ====================


        private void InitializeCoreData()
        {
            // 1) Check PrefabData sẵn sàng
            if (PrefabData.Instance == null)
            {
                var prefabData = FindObjectOfType<PrefabData>(true);
                if (prefabData == null)
                {
                    throw new System.Exception("[Bootstrap] PrefabData missing. Put a PrefabData in the scene before loading.");
                }
            }
            // 2) Check & Initialize Singletons (GuardManager)
            if (Script.Game.Mobs.MobsManager.GuardManager.Instance == null)
            {
                Debug.LogWarning("⚠️ GuardManager missing in scene! Creating new instance automatically.");
                GameObject gmObj = new GameObject("GuardManager");
                gmObj.AddComponent<Script.Game.Mobs.MobsManager.GuardManager>();
            }
            // 3) Trigger bootstrap system để init IDataListBootstrap components
            var dataLists = FindObjectsOfType<MonoBehaviour>(true)
                .OfType<IDataListBootstrap>()
                .OrderBy(x => x.Order)
                .ToList();

            Debug.Log($"📦 Initializing {dataLists.Count} data managers via bootstrap...");
            foreach (var list in dataLists)
            {
                try
                {
                    list.InitializeData(PrefabData.Instance);
                    Debug.Log($"  ✅ {list.GetType().Name} initialized");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to initialize {list.GetType().Name}: {ex.Message}");
                    throw; // Re-throw to fail loading
                }
            }

            // 4) Initialize Arrow Pool
            InitializeArrowPool();
        }

        private void InitializeArrowPool()
        {
            if (PrefabData.Instance == null || PrefabData.Instance.arrow == null)
            {
                Debug.LogError("[System] Missing Arrow Prefab in PrefabData!");
                return;
            }

            // Check if pool already exists (singleton check is inside ArrowPool but good to check here too)
            if (Script.Game.Combat.ArrowPool.Instance == null)
            {
                GameObject poolObj = new GameObject("ArrowPool");
                var pool = poolObj.AddComponent<Script.Game.Combat.ArrowPool>();
                pool.Initialize(PrefabData.Instance.arrow);
                Debug.Log("✅ ArrowPool system initialized.");
            }
            else
            {
                // Ensure it's initialized if it existed but wasn't ready
                Script.Game.Combat.ArrowPool.Instance.Initialize(PrefabData.Instance.arrow);
            }
        }

        // ==================== VALIDATION METHODS ====================


        private void ValidateWorldData()
        {
            if (WorldFileManager.Instance == null)
                throw new System.Exception("WorldFileManager.Instance is null");

            if (WorldFileManager.Instance.worldInfo == null)
                Debug.LogWarning("⚠️ WorldInfo is null - using default world");

          //  Debug.Log("✅ World data validation passed");
        }

     
        private void LoadWorldInfoIfNeeded()
        {
            if (WorldFileManager.Instance == null)
            {
                Debug.LogError("WorldFileManager.Instance là null, không thể load WorldInfo");
                return;
            }

            // Lấy tên thế giới từ đường dẫn save hiện tại
            string savePath = PlayerPrefs.GetString("ActiveWorldPath", "");
            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("Đường dẫn save không hợp lệ");
                return;
            }

            //Debug.Log($"SaveFilePath: {savePath}");

            try
            {
                // Cách 1: Lấy thế giới từ đường dẫn save
                string worldName = ExtractWorldNameFromSavePath(savePath);
                
                if (string.IsNullOrEmpty(worldName))
                {
                    Debug.LogError("Không thể xác định tên thế giới từ đường dẫn save");
                    
                    // Sử dụng phương án dự phòng: Khởi tạo mới worldInfo
                    CreateDefaultWorldInfo();
                    return;
                }
                
                // Load WorldInfo
                WorldInfo loadedInfo = WorldFileManager.Instance.LoadWorldInfo(worldName);
                if (loadedInfo != null)
                {
                    Debug.Log($"Đã load WorldInfo - thế giới: {worldName}");
                    
                    // Đồng bộ tech từ WorldInfo sang PrefabData
                    WorldFileManager.Instance.SyncTechToPrefabData();
                    //Debug.Log("Đã đồng bộ tech từ WorldInfo sang PrefabData");
                }
                else
                {
                    Debug.LogWarning($"Không thể load WorldInfo cho thế giới: {worldName}");
                    CreateDefaultWorldInfo();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Lỗi khi tải WorldInfo: {ex.Message}");
                CreateDefaultWorldInfo();
            }
        }
        
        /// <summary>
        /// Trích xuất tên thế giới từ đường dẫn save
        /// </summary>
        private string ExtractWorldNameFromSavePath(string savePath)
        {
            if (string.IsNullOrEmpty(savePath)) return string.Empty;

            // Chuẩn hóa đường dẫn (thay thế cả dấu \ và / thành /)
            string normalizedPath = savePath.Replace('\\', '/');
           // Debug.Log($"Đường dẫn chuẩn hóa: {normalizedPath}");
            
            // Tách đường dẫn
            string[] parts = normalizedPath.Split('/');
            
            // Tìm thư mục 'saves'
            int savesIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "saves") 
                {
                    savesIndex = i;
                    break;
                }
            }
            
            // Nếu tìm thấy 'saves' và có thư mục con sau nó
            if (savesIndex >= 0 && savesIndex + 1 < parts.Length)
            {
                return parts[savesIndex + 1];
            }
            
            // Phương án dự phòng: thử lấy thư mục cha của đường dẫn hiện tại
            if (parts.Length >= 2)
            {
                return parts[parts.Length - 2];
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Tạo WorldInfo mặc định khi không thể load từ file
        /// </summary>
        private void CreateDefaultWorldInfo()
        {
            //Debug.Log("Tạo WorldInfo mặc định");
            
            // Tạo WorldInfo với thông tin cơ bản
            WorldInfo defaultInfo = new WorldInfo(
                name: "DefaultWorld",
                creationDate: System.DateTime.Now.ToString("O"),
                seed: 0,
                gameHour: 9,
                gameMinute: 0,
                gameDay: 1,
                gameMonth: 1,
                gameYear: 1
            );
            
            // Gán vào WorldFileManager
            WorldFileManager.Instance.worldInfo = defaultInfo;
            
            // Đồng bộ tech từ WorldInfo sang PrefabData
            WorldFileManager.Instance.SyncTechToPrefabData();
            
            //Debug.Log("Đã tạo và thiết lập WorldInfo mặc định");
        }

        public void InstancePlayerAndSetCamera()
        {
            Debug.Log("👤 PHASE 5: Creating/setup player object...");

            // Kiểm tra các instance cần thiết
            if (PlayerController.Instance == null)
            {
                Debug.LogError("PlayerController.Instance là null, không thể tạo player");
                return;
            }

            if (EntitySaveManager.Instance == null)
            {
                Debug.LogError("EntitySaveManager.Instance là null, không thể tạo player");
                return;
            }

            if (PrefabData.Instance == null || PrefabData.Instance.player == null)
            {
                Debug.LogError("PrefabData.Instance.player là null, không thể tạo player");
                return;
            }

            // Kiểm tra xem có save data của player không để quyết định vị trí spawn
            Vector2 spawnPosition = Vector2.zero; // Vị trí mặc định
            string playerPath = System.IO.Path.Combine(LoadFromSave.Instance.saveFilePath, "PlayerContainer.json");

            if (!string.IsNullOrEmpty(LoadFromSave.Instance.saveFilePath) && System.IO.File.Exists(playerPath))
            {
                try
                {
                    // Đọc vị trí từ save file để spawn player tại đúng vị trí đã save
                    string json = System.IO.File.ReadAllText(playerPath);
                    var savedObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<EntitySaveManager.SavedObject>>(json,
                        new Newtonsoft.Json.JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });

                    if (savedObjects != null && savedObjects.Count > 0)
                    {
                        // Đọc trực tiếp vị trí từ PlayerStatusData mà không cần tạo temporary player
                        var playerData = savedObjects[0].data as Script.Game.Player.PlayerBehavior.PlayerStatusData;
                        if (playerData != null)
                        {
                            spawnPosition = new Vector2(playerData.positionX, playerData.positionY);
                            Debug.Log($"Player sẽ được spawn tại vị trí đã save: {spawnPosition}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Không thể đọc vị trí player từ save, dùng vị trí mặc định: {e.Message}");
                }
            }

            // Tạo player instance tại vị trí đã xác định
            GameObject player = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, spawnPosition);
            if (player == null)
            {
                Debug.LogError("Failed to instantiate Player from prefab. Make sure player characters are loaded correctly.");
                return;
            }

            // Đảm bảo player ở đúng vị trí (nếu save data đã set vị trí khác)
            player.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);

            // Setup PlayerController
            PlayerController.Instance.player = player;
            PlayerController.Instance.SetUpControllerPlayer();

            // Setup Camera
            if (CameraFollowPlayer.Instance != null)
            {
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
            }

            Debug.Log($"✅ Player instance created at {spawnPosition} and systems setup completed");
        }



        public static void DevelopTest()
        {
            // Kiểm tra PlayerController.Instance tồn tại
            if (PlayerController.Instance == null ||
                PlayerController.Instance.player == null)
            {
                Debug.LogError("Player không tồn tại, không thể chạy DevelopTest");
                return;
            }

            //add some coin for player test
            PlayerController.Instance.playerBehavior.Coin += 100;

            // Cho phép gây damage lên đồng minh khi test
            PlayerController.Instance.damageAlly = true;

            // Kiểm tra và log thông tin WorldInfo hiện tại
            if (WorldFileManager.Instance != null && WorldFileManager.Instance.worldInfo != null)
            {
                var info = WorldFileManager.Instance.worldInfo;
                Debug.Log($"WorldInfo hiện tại: {info.Name}, Tech: FireArrow={info.TechFireArrow}, Penetrating={info.TechPenetrating}, Forge={info.TechForge}, Adhesive={info.TechAdhesive}");
            }

            Debug.LogWarning("DevelopTest đang bật ở StartGameScrene");
        }
    }
}
