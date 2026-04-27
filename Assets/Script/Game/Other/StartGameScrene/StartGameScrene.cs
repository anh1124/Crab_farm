using Script.Data_Model;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using Script.Controller.PlayerController;
using Script.Controller.GUI_Controller.PlayerGui;
using Script.Manager.GamePlayManager;
using Script.Audio;
using Script.Utils;

namespace Script.Game.Other
{
    [RequireComponent(typeof(CheatManager))]
    public partial class StartGameScrene : MonoBehaviour
    {
        [Header("Save Settings")]
        [Tooltip("Có load save từ pref hay không, false dành cho tutorial")]
        public bool loadSaveFromPref = true;
        
        [Header("Tutorial Mode")]
        [Tooltip("Bật chế độ Tutorial. Khi true, loadSaveFromPref sẽ bị ghi đè thành false và các phase không cần thiết sẽ bị bỏ qua.")]
        public bool isTutorialScene = false;
        
        // Property để đảm bảo isTutorialScene ưu tiên cao hơn
        public bool ShouldLoadSave => loadSaveFromPref && !isTutorialScene;
        
        /// <summary>
        /// Static flag để các script khác kiểm tra có đang ở tutorial scene không.
        /// </summary>
        public static bool IsTutorial => Instance?.isTutorialScene ?? false;
        
        public static StartGameScrene Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            
            // Ưu tiên cao: Nếu là tutorial scene, ghi đè loadSaveFromPref thành false
            if (isTutorialScene)
            {
                loadSaveFromPref = false;
                Debug.Log("[StartGameScrene] isTutorialScene = true, đã ghi đè loadSaveFromPref = false");
            }
        }

        public void Start()
        {
            try
            {
                if (isTutorialScene)
                {
                    InitializeTutorialScene();
                }
                else
                {
                    InitializeNormalGame();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($" Game loading failed: {ex.Message}\n{ex.StackTrace}");
                SceneManager.LoadScene("MainMenuScene");
            }
        }

        private void InitializeTutorialScene()
        {
            Debug.Log("[StartGameScrene] ===== TUTORIAL SCENE LOADING =====");

            // ===== PHASE 1: CORE DATA =====
            InitializeCoreData(); // Init PrefabData and bootstrap systems

            // ===== PHASE 2: DYNAMIC POOLS =====
            InitializeCoinBase();

            // NOTE: Không setup container - Player và TownCenter đã được thêm thủ công vào scene
            // Chỉ hook player có sẵn (không spawn mới nếu không tìm thấy)
            HookPlayerFromScene();

            // ===== PHASE 3: BACKGROUND MUSIC =====
            PlayBackgroundMusic();

            Debug.Log("[StartGameScrene] Tutorial scene loading completed");
        }

        private void InitializeNormalGame()
        {
            Debug.Log("[StartGameScrene] ===== NORMAL GAME LOADING =====");

            // ===== PHASE 1: CORE DATA =====
            InitializeCoreData(); // Init PrefabData and bootstrap systems

            // ===== PHASE 2: WORLD INFO =====
            if (!InitializeActiveWorld()) return;
            ValidateWorldData();

            // ===== PHASE 4: DYNAMIC POOLS =====
            InitializeCoinBase();

            // Nếu không phải tutorial, mới cần EnemyBase
            if (!isTutorialScene)
            {
                InitializeEnemyBase();
            }
            else
            {
                Debug.Log("[StartGameScrene] TutorialScene: Bỏ qua InitializeEnemyBase()");
            }

            // ===== PHASE 5: SAVE LOAD ENTITIES =====
            // Cần khởi tạo pool trước khi load để enemy load-from-save đi đúng đường pool.
            LoadSavedEntitiesPhased();

            // Re-bootstrap denizens after save loading phase, so they have their managers
            if (denizenManager != null) denizenManager.InitializeAllDenizensInScene(guardManager);

            // ===== PHASE 6: PLAYER INSTANCE =====
            InstancePlayerAndSetCamera();
            InitializePlayer();

            // ===== PHASE 7: BACKGROUND MUSIC =====
            PlayBackgroundMusic();

            // Nếu đây là thế giới mới (vừa được Generate), thực hiện thiết lập các object phụ qua NewWorldInitializer
            if (!isTutorialScene && LoadFromSave.Instance != null && LoadFromSave.Instance.IsNewWorld)
            {
                if (NewWorldInitializer.Instance != null)
                {
                    NewWorldInitializer.Instance.ExecuteSetup();
                }
                else
                {
                    Debug.LogWarning("[StartGameScrene] NewWorldInitializer instance not found!");
                }
            }

            #if UNITY_EDITOR
            if (!isTutorialScene) DevelopTest();
            #endif
        }

        private void HookPlayerFromScene()
        {
            Debug.Log("[StartGameScrene] Hooking player from scene");

            if (PlayerController.Instance == null)
            {
                Debug.LogError("[StartGameScrene] PlayerController.Instance is null!");
                return;
            }

            // Tìm player trong scene bằng tag
            GameObject player = GameObject.FindGameObjectWithTag(Script.Utils.GameTags.Player);
            if (player == null)
            {
                Debug.LogError("[StartGameScrene] No player found in scene with tag 'Player'!");
                return;
            }

            // Gán player vào PlayerController
            PlayerController.Instance.player = player;
            Debug.Log($"[StartGameScrene] Hooked player: {player.name}");

            // Setup controller
            PlayerController.Instance.SetUpControllerPlayer();

            // Setup camera
            if (CameraFollowPlayer.Instance != null)
            {
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
                Debug.Log("[StartGameScrene] Camera setup completed");
            }

            // Setup PlayerGui
            if (PlayerGui.Instance != null)
            {
                PlayerGui.Instance.RefreshSkillManagerCache();
                Debug.Log("[StartGameScrene] PlayerGui setup completed");
            }
        }


        public static void DevelopTest()
        {
            if (PlayerController.Instance == null || PlayerController.Instance.player == null)
            {
                Debug.LogError("Player không tồn tại, không thể chạy DevelopTest");
                return;
            }

            if (CheatManager.Instance != null)
            {
                CheatManager.Instance.ApplyDevCheats();
            }

            if (WorldFileManager.Instance != null && WorldFileManager.Instance.worldInfo != null)
            {
                var info = WorldFileManager.Instance.worldInfo;
                Debug.Log($"WorldInfo hiện tại: {info.Name}, Tech: FireArrow={info.TechFireArrow}, Forge={info.TechForge}");
            }

            Debug.LogWarning("DevelopTest đang bật ở StartGameScrene");
        }
    }
}
