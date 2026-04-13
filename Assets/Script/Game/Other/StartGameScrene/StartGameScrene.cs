using Script.Data_Model;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using Script.Controller.PlayerController;
using Script.Manager.GamePlayManager;

namespace Script.Game.Other
{
    [RequireComponent(typeof(CheatManager))]
    public partial class StartGameScrene : MonoBehaviour
    {
        public void Start()
        {
            try
            {
                // ===== PHASE 1: CORE DATA =====
                InitializeCoreData(); // Init PrefabData and bootstrap systems

                // ===== PHASE 2: WORLD INFO =====
                if (!InitializeActiveWorld()) return;
                ValidateWorldData();

                // ===== PHASE 4: DYNAMIC POOLS =====
                InitializeCoinBase();
                InitializeEnemyBase();

                // ===== PHASE 5: SAVE LOAD ENTITIES =====
                // Cần khởi tạo pool trước khi load để enemy load-from-save đi đúng đường pool.
                LoadSavedEntitiesPhased();

                // Re-bootstrap denizens after save loading phase, so they have their managers
                if (denizenManager != null) denizenManager.InitializeAllDenizensInScene(guardManager);

                // ===== PHASE 6: PLAYER INSTANCE =====
                InstancePlayerAndSetCamera(); 
                InitializePlayer(); 

                #if UNITY_EDITOR
                DevelopTest();
                #endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($" Game loading failed: {ex.Message}\n{ex.StackTrace}");
                SceneManager.LoadScene("MainMenuScene");
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
