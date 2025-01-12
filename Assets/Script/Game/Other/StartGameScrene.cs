using System;
using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Data_Model;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameScrene : MonoBehaviour
{
    public GameObject pauseMenuPanel;
    public void Start()
    {
        //thay filepath thành bên dưới để test nhanh
        //LoadFromSave.Instance.saveFilePath = @"C:/Users/Acer/AppData/LocalLow/DefaultCompany/Crab_farm\saves\holyCrab";
      
        //nếu không muốn test nữa thì hủy comment dòng bên dưới đi
        //ở đoạn mã dưới ,nếu file lỗi hoặc thiếu thì sẽ trở về màn hình chính và debug ra
       
        if (!LoadFromSave.Instance.DoLoadFromSaveFilePath(PlayerPrefs.GetString("SaveFilePath", "")))
        {
           // SceneManager.LoadScene("MainMenuScene");//về màn hình chính
        }
        //instancePlayer
        InstancePlayer();
        
        //thêm setup lại mấy cái gui ở dưới
        pauseMenuPanel.SetActive(true);

    }

    public void InstancePlayer()
    {
        if (PrefabData.Instance.player != null)
        {
            // Tạo instance và lưu reference của instance được tạo ra
            GameObject playerInstance = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(PrefabData.Instance.player, Vector2.zero);
        
            // Gán instance đã tạo (không phải prefab) cho PlayerController
            PlayerController.Instance.player = playerInstance;
            PlayerController.Instance.SetUpControllerPlayer();
        }
        else
        {
            Debug.LogError("Player charactor is not assigned in PrefabData.");
        }
    }
}
