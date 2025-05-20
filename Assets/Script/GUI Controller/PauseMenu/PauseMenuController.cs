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
        
        
         public Button pauseButton;
        
        public Button resumeButton;
        public Button optionBtn;
        public Button saveExitBtn;
        
        #endregion
        
        private void Start()
        {
            guiCanvasManager = gameObject.transform.parent.GetComponent<GuiCanvasManager>();
           
            pauseButton.onClick.AddListener(OpenMenu);
            resumeButton.onClick.AddListener(Resume);
            optionBtn.onClick.AddListener(OpenOption);
            saveExitBtn.onClick.AddListener(SaveAndExit);
            Resume();
            gameObject.SetActive(false);//ẩn đi
            
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
            pauseButton.gameObject.SetActive(false);
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
