using Script.Data_Model;
using Script.Data_Model.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.Game.Other
{
    public class StartGameScrene : MonoBehaviour
    {
        public GameObject pauseMenuPanel;
        public void Start()
        {
            //thay filepath thành bên dưới để test nhanh
            //LoadFromSave.Instance.saveFilePath = @"C:/Users/Acer/AppData/LocalLow/DefaultCompany/Crab_farm\saves\holyCrab";
      
            //nếu không muốn test nữa thì hủy comment dòng bên dưới đi
            //ở đoạn mã dưới ,nếu file lỗi hoặc thiếu thì sẽ trở về màn hình chính và debug ra
       
            
            //thực hiện kiểm tra thế giới,tạo map,load container,etc...
            if (!LoadFromSave.Instance.DoLoadFromSaveFilePath(PlayerPrefs.GetString("SaveFilePath", "")))
            {
                SceneManager.LoadScene("MainMenuScene");//về màn hình chính nếu hàm trên false
            }
            
            //instancePlayer
            InstancePlayerAndSetCamera();
            //set status player
            SetUpContainer.Instance.LoadPlayer();
            //gán player cho các script cần thiết player khác
            BuildMenu.Instance.InitializePlayer();
            
            //thêm setup lại mấy cái gui ở dưới
            pauseMenuPanel.SetActive(true);
            
            DevelopTest();
            if (SetUpContainer.Instance == null)
            {
                Debug.LogError("SetUpContainer.Instance is null.");
            }

          
        }

        public void InstancePlayerAndSetCamera()
        {
            
            // Kiểm tra prefab player
            if (PrefabData.Instance.player == null)
            {
                Debug.LogError("Player character prefab is not assigned in PrefabData.");
                return;
            }
            
            // Tạo instance của player từ prefab
            GameObject player = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
    
            // Kiểm tra xem instance có được tạo thành công không
            if (player == null)
            {
                Debug.LogError("Failed to create Player instance from Prefab.");
                return;
            }

            // Gán instance player vào PlayerController
            PlayerController.Instance.player = player;

            // Thiết lập controller cho player
            PlayerController.Instance.SetUpControllerPlayer();

            // Thiết lập camera theo dõi player
            CameraFollowPlayer.Instance.setCameraTarget();
            CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
            
            // Khởi tạo BuildMenu với player mới
            if (BuildMenu.Instance != null)
            {
                BuildMenu.Instance.InitializePlayer();
            }
            else
            {
                Debug.LogWarning("BuildMenu.Instance is null, unable to initialize player in BuildMenu.");
            }

            // Thông báo hoàn tất khởi tạo player
            Debug.Log("Player successfully instantiated and initialized.");
        }
        public static void DevelopTest()
        {
            //add some coin for player test

            PlayerStatus playerStatus = PlayerController.Instance.player.GetComponent<PlayerStatus>();
            playerStatus.Coin += 100;
            
            Debug.LogWarning("DevelopTest đang bật ở StartGameScrene");
        }
    }
}
