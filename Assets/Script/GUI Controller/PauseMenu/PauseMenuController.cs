using System;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI_Controller.PauseMenu
{
    public class PauseMenuController : MonoBehaviour
    {
        

        public GameObject guiCanvas;

        #region Pause menu component

        

        
        public GameObject pauseMenuPanel;
        
        public GameObject openPauseMenu;
        public Button openPauseMenuBtn;
        
        public Button continueBtn;
        public Button optionBtn;
        public Button saveExitBtn;
        
        #endregion
        
        private void Start()
        {
            
            ExitMenu();
            openPauseMenuBtn.onClick.AddListener(OpenMenu);
            continueBtn.onClick.AddListener(ExitMenu);
            optionBtn.onClick.AddListener(OpenOption);
            saveExitBtn.onClick.AddListener(SaveAndExit);
            gameObject.SetActive(false);//ẩn đi
        }

        public void ExitMenu()
        {
            Time.timeScale = 1f;
            pauseMenuPanel.SetActive(false);
            openPauseMenu.SetActive(true);
        }
        public void OpenMenu()
        {
            Time.timeScale = 0f;
            pauseMenuPanel.SetActive(true);
            openPauseMenu.SetActive(false);
        }

        public void OpenOption()
        {
            
        }

        public void SaveAndExit()
        {
            
        }
    }
}
