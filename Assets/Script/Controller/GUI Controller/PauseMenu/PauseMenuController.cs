using System;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace Script.GUI_Controller.PauseMenu
{
    public class PauseMenuController : MonoBehaviour
    {


        private GuiCanvasManager guiCanvasManager;
        #region Pause menu component

        

        
        public GameObject pauseMenuPanel;
        
        public Button resumeButton;
        public Button optionBtn;
        public Button saveExitBtn;
        
        #endregion
        
        private void Start()
        {
            guiCanvasManager = GuiCanvasManager.Instance;
           
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (optionBtn != null) optionBtn.onClick.AddListener(OpenOption);
            if (saveExitBtn != null) saveExitBtn.onClick.AddListener(SaveAndExit);
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            pauseMenuPanel.SetActive(false);
            guiCanvasManager.ResumeGame();
            
        }
        public void OpenMenu()
        {
            Time.timeScale = 0f;
            pauseMenuPanel.SetActive(true);
        }

        public void OpenOption()
        {
            
        }

        public void SaveAndExit()
        {
            WorldFileManager.Instance.SaveCurrentMap();
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
