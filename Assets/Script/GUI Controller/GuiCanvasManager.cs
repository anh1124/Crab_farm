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
            SetMainGameplayUI();
            pauseButton.onClick.AddListener(ShowPauseMenu);
        }

        public void SetMainGameplayUI()
        {
            pauseMenu.SetActive(false);
            
            playerHud.SetActive(true);
            buildMenu.SetActive(true);
            pauseButton.gameObject.SetActive(true);
        }

        public void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
            
            playerHud.SetActive(false);
            buildMenu.SetActive(false);
            pauseButton.gameObject.SetActive(false);
        }
    }
}
