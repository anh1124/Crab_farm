using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Script.GUI_Controller
{
    public class GuiCanvasManager : MonoBehaviour
    {
        public static GuiCanvasManager Instance { get; private set; }
        
        //button
        [Header("Button")]
        public Button pauseButton;
        public Button closeSoundSettingButton;
        //Menu
        [Header("MENU")]
        public GameObject pauseMenu;
        public GameObject playerHud;
        public GameObject buildMenu;
        public GameObject soundSettingCanvas;
        public ConfirmationMenu confirmationMenu;
        [Header("Resource Bars")]
        public ResourceBarUI healBarUI;
        public ResourceBarUI manaBarUI;
        
        public CoinUI coinUI;
        
        [Header("Time Display")]
        public TextMeshProUGUI timeDisplay;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        private void Start()
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(ShowPauseMenu);

            if (closeSoundSettingButton != null)
            {
                closeSoundSettingButton.onClick.AddListener(HideSoundSetting);
            }

            ResumeGame();
        }

        public void SetupResourceBars(Script.Data_Model.Status.PlayerStatus status)
        {
            if (healBarUI != null)
                healBarUI.Setup(status.HP);
            else
                Debug.LogError("[GuiCanvasManager] healBarUI chưa được gán trong Inspector!");

            if (manaBarUI != null)
                manaBarUI.Setup(status.Mana);
            else
                Debug.LogError("[GuiCanvasManager] manaBarUI chưa được gán trong Inspector!");
        }

        public void ShowPauseMenu()
        {
            Time.timeScale = 0; // Bật pause
            pauseMenu.SetActive(true);

            // Sử dụng SetActive(false) cho Canvas/panel thay vì cả GameObject
            playerHud.gameObject.SetActive(false);
            buildMenu.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(false);
            if (timeDisplay != null) timeDisplay.gameObject.SetActive(false);
        }

        public void ShowSoundSetting()
        {
            pauseMenu.SetActive(false);
            soundSettingCanvas.SetActive(true);
        }

        public void HideSoundSetting()
        {
            soundSettingCanvas.SetActive(false);
            pauseMenu.SetActive(true);
        }

        public void ResumeGame()
        {
            Time.timeScale = 1; // Bỏ pause game
            pauseMenu.SetActive(false);
            soundSettingCanvas.SetActive(false);


            // Kích hoạt lại UI elements
            playerHud.gameObject.SetActive(true);
            buildMenu.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(true);
            if (timeDisplay != null) timeDisplay.gameObject.SetActive(true);
        }

        /// <summary>
        /// Bật nếu đang tắt, tắt nếu đang bật. Dùng cho hotkey ESC.
        /// </summary>
        public void TogglePauseMenu()
        {
            if (pauseMenu != null && pauseMenu.activeSelf)
                ResumeGame();
            else
                ShowPauseMenu();
        }
    }
}
