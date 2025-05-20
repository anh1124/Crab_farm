using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace Script.Game.Other
{
    public class StartGameScrene : MonoBehaviour
    {
        public GameObject pauseMenuPanel;
        public void Start()
        {
            PrefabData.Instance.LoadPlayerCharactersFromJson();
            //thực hiện load thế giới,tạo map,load container,etc...
            if (!LoadFromSave.Instance.DoLoadFromSaveFilePath(PlayerPrefs.GetString("SaveFilePath", "")))
            {
                Debug.LogError("Không thể load từ file save, chuyển về màn hình chính");
                SceneManager.LoadScene("MainMenuScene");//về màn hình chính nếu hàm trên false
                return;
            }
            
            // Kiểm tra và load WorldInfo sau khi load save
            LoadWorldInfoIfNeeded();
            //instancePlayer
            InstancePlayerAndSetCamera();
            
            //gán player cho các script cần thiết player khác
            if (BuildMenu.instance != null)
            {
                BuildMenu.instance.InitializePlayer();
            }
            else
            {
                Debug.LogWarning("BuildMenu.instance không tồn tại, không thể khởi tạo player cho BuildMenu");
            }
            
            //thêm setup lại mấy cái gui ở dưới
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("pauseMenuPanel là null, không thể hiển thị menu tạm dừng");
            }
            
            // Chỉ chạy DevelopTest trong môi trường phát triển
            #if UNITY_EDITOR
            DevelopTest();
            #endif
            
            if (EntitySaveManager.Instance == null)
            {
                Debug.LogError("EntitySaveManager.Instance is null.");
            }
        }

        /// <summary>
        /// Kiểm tra và load WorldInfo từ file save hiện tại
        /// </summary>
        private void LoadWorldInfoIfNeeded()
        {
            if (WorldFileManager.Instance == null)
            {
                Debug.LogError("WorldFileManager.Instance là null, không thể load WorldInfo");
                return;
            }

            // Lấy tên thế giới từ đường dẫn save hiện tại
            string savePath = PlayerPrefs.GetString("SaveFilePath", "");
            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("Đường dẫn save không hợp lệ");
                return;
            }

            Debug.Log($"SaveFilePath: {savePath}");

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
                
                Debug.Log($"Tên thế giới được xác định: {worldName}");
                
                // Load WorldInfo
                WorldInfo loadedInfo = WorldFileManager.Instance.LoadWorldInfo(worldName);
                if (loadedInfo != null)
                {
                    Debug.Log($"Đã load WorldInfo cho thế giới: {worldName}");
                    
                    // Đồng bộ tech từ WorldInfo sang PrefabData
                    WorldFileManager.Instance.SyncTechToPrefabData();
                    Debug.Log("Đã đồng bộ tech từ WorldInfo sang PrefabData");
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
            Debug.Log($"Đường dẫn chuẩn hóa: {normalizedPath}");
            
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
            Debug.Log("Tạo WorldInfo mặc định");
            
            // Tạo WorldInfo với thông tin cơ bản
            WorldInfo defaultInfo = new WorldInfo(
                name: "DefaultWorld",
                creationDate: System.DateTime.Now,
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
            
            Debug.Log("Đã tạo và thiết lập WorldInfo mặc định");
        }

        public void InstancePlayerAndSetCamera()
        {
            // Kiểm tra PlayerController.Instance tồn tại
            if (PlayerController.PlayerController.Instance == null)
            {
                Debug.LogError("PlayerController.Instance là null, không thể tạo player");
                return;
            }
            
            if (PlayerController.PlayerController.Instance.player != null)
            {
                // Kiểm tra CameraFollowPlayer.Instance tồn tại
                if (CameraFollowPlayer.Instance == null)
                {
                    Debug.LogError("CameraFollowPlayer.Instance là null, không thể thiết lập camera");
                    return;
                }
                
                // Nếu player đã được load, chỉ cần set lại camera
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(PlayerController.PlayerController.Instance.player.transform.position);
                return;
            }

            // Kiểm tra EntitySaveManager.Instance tồn tại
            if (EntitySaveManager.Instance == null)
            {
                Debug.LogError("EntitySaveManager.Instance là null, không thể tạo player");
                return;
            }
            
            // Kiểm tra PrefabData.Instance tồn tại
            if (PrefabData.Instance == null || PrefabData.Instance.player == null)
            {
                Debug.LogError("PrefabData.Instance hoặc PrefabData.Instance.player là null, không thể tạo player");
                return;
            }

            // Nếu player chưa tồn tại thì tạo mới
            GameObject player = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
            if (player == null)
            {
                Debug.LogError("Failed to instantiate Player from prefab. Make sure player characters are loaded correctly.");
                return;
            }
            PlayerController.PlayerController.Instance.player = player;
            PlayerController.PlayerController.Instance.SetUpControllerPlayer();
            
            // Kiểm tra CameraFollowPlayer.Instance tồn tại
            if (CameraFollowPlayer.Instance == null)
            {
                Debug.LogError("CameraFollowPlayer.Instance là null, không thể thiết lập camera");
                return;
            }
            
            CameraFollowPlayer.Instance.setCameraTarget();
            CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
        }

        public static void DevelopTest()
        {
            // Kiểm tra PlayerController.Instance tồn tại
            if (PlayerController.PlayerController.Instance == null || 
                PlayerController.PlayerController.Instance.player == null)
            {
                Debug.LogError("Player không tồn tại, không thể chạy DevelopTest");
                return;
            }
            
            // Kiểm tra component PlayerStatus tồn tại
            PlayerStatus playerStatus = PlayerController.PlayerController.Instance.player.GetComponent<PlayerStatus>();
            if (playerStatus == null)
            {
                Debug.LogError("Component PlayerStatus không tồn tại trên player");
                return;
            }
            
            //add some coin for player test
            playerStatus.Coin += 100;
            
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
