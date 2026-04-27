using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Script.GUI_Controller
{
    public enum UIState
    {
        Normal,         // Game đang chạy bình thường, chỉ có PlayerHUD
        PauseMenu,      // Pause menu đang mở
        SoundSetting,   // Sound setting đang mở
    }

    public class GuiCanvasManager : MonoBehaviour
    {
        public static GuiCanvasManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private UIState _currentState = UIState.Normal;
        public UIState CurrentState => _currentState;

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
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            // Nếu đang có panel khác, đóng nó trước
            CloseAnyOpenPanel();

            _currentState = UIState.PauseMenu;
            Time.timeScale = 0; // Bật pause
            pauseMenu.SetActive(true);

            // Tạm dừng toàn bộ âm thanh thế giới (BGM, SFX, ENV)
            Script.Audio.AudioManager.Instance?.PauseNonUISounds(true);

            // Sử dụng SetActive(false) cho Canvas/panel thay vì cả GameObject
            playerHud.gameObject.SetActive(false);
            buildMenu.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(false);
            if (timeDisplay != null) timeDisplay.gameObject.SetActive(false);
        }

        public void ShowSoundSetting()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            _currentState = UIState.SoundSetting;
            pauseMenu.SetActive(false);
            soundSettingCanvas.SetActive(true);
        }

        public void HideSoundSetting()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            _currentState = UIState.PauseMenu;
            soundSettingCanvas.SetActive(false);
            pauseMenu.SetActive(true);
        }

        public void ResumeGame()
        {
            _currentState = UIState.Normal;
            Time.timeScale = 1; // Bỏ pause game
            pauseMenu.SetActive(false);
            soundSettingCanvas.SetActive(false);

            // Tiếp tục âm thanh thế giới
            Script.Audio.AudioManager.Instance?.PauseNonUISounds(false);

            // Kích hoạt lại UI elements
            playerHud.gameObject.SetActive(true);
            buildMenu.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(true);
            if (timeDisplay != null) timeDisplay.gameObject.SetActive(true);
        }

        /// <summary>
        /// Đóng bất kỳ panel nào đang mở (trừ PlayerHUD).
        /// </summary>
        private void CloseAnyOpenPanel()
        {
            // Không có panel nào cần đóng
        }

        /// <summary>
        /// Xử lý phím ESC theo state hiện tại.
        /// - Normal: Mở PauseMenu
        /// - PauseMenu: Resume game
        /// - SoundSetting: Quay về PauseMenu
        /// </summary>
        public void HandleEscapeKey()
        {
            switch (_currentState)
            {
                case UIState.Normal:
                    ShowPauseMenu();
                    break;

                case UIState.PauseMenu:
                    ResumeGame();
                    break;

                case UIState.SoundSetting:
                    // Từ sound setting quay về pause menu
                    HideSoundSetting();
                    break;

                default:
                    ResumeGame();
                    break;
            }
        }

        /// <summary>
        /// Bật nếu đang tắt, tắt nếu đang bật. Dùng cho hotkey ESC (backward compatible).
        /// </summary>
        [Obsolete("Use HandleEscapeKey() instead for proper state management")]
        public void TogglePauseMenu()
        {
            HandleEscapeKey();
        }
    }
}
