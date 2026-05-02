using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Script.Manager.WorldManager;

namespace Script.GUI_Controller
{
    public class GameOverHandler : MonoBehaviour
    {
        public static GameOverHandler Instance { get; private set; }
        
        /// <summary>Cờ đánh dấu đang trong trạng thái Game Over để khóa InputManager</summary>
        public static bool IsActive { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            IsActive = false; // Đảm bảo cờ reset khi scene mới load lên
        }

        /// <summary>
        /// Entry point chính khi Player hoặc TownCenter bị tiêu diệt.
        /// </summary>
        public void TriggerGameOver()
        {
            if (IsActive) return; // Tránh gọi nhiều lần
            IsActive = true;

            PlayGameOverSound();
            PauseGame();
            ShowGameOverPanel();
        }

        private void PlayGameOverSound()
        {
            if (Script.Audio.AudioManager.Instance != null)
                Script.Audio.AudioManager.Instance.PlayUISound("Player_Lose");
        }

        private void PauseGame()
        {
            Time.timeScale = 0f;
        }

        private void ShowGameOverPanel()
        {
            var menu = GuiCanvasManager.Instance != null ? GuiCanvasManager.Instance.confirmationMenu : null;
            if (menu == null)
            {
                Debug.LogError("[GameOverHandler] Không tìm thấy ConfirmationMenu từ GuiCanvasManager! Game Over panel không thể hiển thị.");
                return;
            }

            // Kích hoạt GameObject chứa menu
            menu.gameObject.SetActive(true);

            int survivedDays = GetSurvivedDays();
            
            menu.Show(
                header: "Game Over",
                content: $"You survived {survivedDays} days.\n",
                onConfirm: HandleReturnToMainMenu,
                onCancel: HandleLoadLastSave,
                priority: ConfirmationMenu.MenuPriority.Critical,
                debugOwner: "GameOverHandler"
            );

            UpdateMenuButtonTexts(menu);
        }

        private int GetSurvivedDays()
        {
            var gtm = FindObjectOfType<GameTimeManager>();
            return gtm != null ? gtm.GetElapsedDays() : 0;
        }

        private void UpdateMenuButtonTexts(ConfirmationMenu menu)
        {
            if (menu.cancelButton != null)
            {
                var btnText = menu.cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Load last save  ";
            }

            if (menu.confirmButton != null)
            {
                var btnText = menu.confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Main menu";
            }
        }

        private void HandleReturnToMainMenu()
        {
            Time.timeScale = 1f;
            CleanupMenu();
            SceneManager.LoadScene("MainMenuScene");
        }

        private void HandleLoadLastSave()
        {
            Time.timeScale = 1f;
            CleanupMenu();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }

        private void CleanupMenu()
        {
            var menu = GuiCanvasManager.Instance != null ? GuiCanvasManager.Instance.confirmationMenu : null;
            if (menu != null)
            {
                menu.CloseMenu(isCriticalDestroy: true);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                IsActive = false;
            }
        }
    }
}
