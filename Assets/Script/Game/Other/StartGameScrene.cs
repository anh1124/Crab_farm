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
            if (PlayerController.Instance.player != null)
            {
                // Nếu player đã được load, chỉ cần set lại camera
                CameraFollowPlayer.Instance.setCameraTarget();
                CameraFollowPlayer.Instance.TeleportCameraTo(PlayerController.Instance.player.transform.position);
                return;
            }

            // Nếu player chưa tồn tại thì tạo mới
            GameObject player = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
            if (player == null)
            {
                Debug.LogError("Failed to instantiate Player from prefab.");
                return;
            }
            PlayerController.Instance.player = player;
            PlayerController.Instance.SetUpControllerPlayer();
            CameraFollowPlayer.Instance.setCameraTarget();
            CameraFollowPlayer.Instance.TeleportCameraTo(player.transform.position);
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
