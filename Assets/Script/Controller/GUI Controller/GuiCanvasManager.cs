using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Script.GUI_Controller
{
    public class GuiCanvasManager : MonoBehaviour
    {
        //button
        [Header("Button")]
        public Button pauseButton;
        //Menu
        [Header("MENU")]
        public GameObject pauseMenu;
        public GameObject playerHud;
        public GameObject buildMenu;
        
        private void Start()
        {
            ResumeGame();
            pauseButton.onClick.AddListener(ShowPauseMenu);
            ShowPauseMenu();//lỗi ,phải cho nó bật menu 1 lần 
        }

      

        public void ShowPauseMenu()
        {
           
            pauseMenu.SetActive(true);
    
            // Thay đổi: Sử dụng SetActive(false) cho Canvas/panel thay vì cả GameObject
            playerHud.gameObject.SetActive(false);
            buildMenu.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(false);
        }
        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
    
            // Kích hoạt lại UI elements
            playerHud.gameObject.SetActive(true);
            buildMenu.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(true);
    
            Time.timeScale = 1; // Bỏ pause game
        }
    }
}
