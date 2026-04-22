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

        [Header("UI Components")]
        [SerializeField, Tooltip("Bảng UI thông báo thua cuộc")] 
        private GameObject panel;

        [SerializeField, Tooltip("Tiêu đề (Nút giữa trên cùng)")] 
        private TextMeshProUGUI headerText;

        [SerializeField, Tooltip("Nội dung thông báo (Ngày sống sót)")] 
        private TextMeshProUGUI contentText;

        [SerializeField, Tooltip("Nút bên PHẢI (Thường là Confirm/Menu chính)")] 
        private Button confirmButton;

        [SerializeField, Tooltip("Nút bên TRÁI (Thường là Cancel/Load Save)")] 
        private Button cancelButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            IsActive = false; // Đảm bảo cờ reset khi scene (chứa GameOverHandler) mới load lên
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            // Tự động lấy tham chiếu từ GuiCanvasManager.Instance.confirmationMenu
            if (GuiCanvasManager.Instance != null && GuiCanvasManager.Instance.confirmationMenu != null)
            {
                var menu = GuiCanvasManager.Instance.confirmationMenu;
                if (panel == null) panel = menu.panel;
                if (headerText == null) headerText = menu.headerText;
                if (contentText == null) contentText = menu.contentText;
                if (confirmButton == null) confirmButton = menu.confirmButton;
                if (cancelButton == null) cancelButton = menu.cancelButton;
            }

            // Cảnh báo nếu vẫn thiếu
            if (panel == null) Debug.LogWarning("[GameOverHandler] Thiếu 'Panel'! Hãy kéo GameObject bảng thông báo vào đây.");
            if (confirmButton == null) Debug.LogWarning("[GameOverHandler] Thiếu 'Nút bên PHẢI'! Hãy kéo Button vào đây.");
            if (cancelButton == null) Debug.LogWarning("[GameOverHandler] Thiếu 'Nút bên TRÁI'! Hãy kéo Button vào đây.");
        }

        public void TriggerGameOver()
        {
            IsActive = true;
            EnsureReferences();

            // Phát âm thanh game over
            if (Script.Audio.AudioManager.Instance != null)
                Script.Audio.AudioManager.Instance.PlayUISound("Player_Lose");

            var gtm = FindObjectOfType<GameTimeManager>();
            int survivedDays = gtm != null ? gtm.GetElapsedDays() : 0;
            Time.timeScale = 0f; // Pause game

            if (GuiCanvasManager.Instance != null && GuiCanvasManager.Instance.confirmationMenu != null)
            {
                GuiCanvasManager.Instance.confirmationMenu.Show(
                    "Game Over",
                    $"You survived {survivedDays} days.\n",
                    onConfirm: () =>
                    {
                        Time.timeScale = 1f; // Bỏ pause trước khi load lại menu
                        // Gọi CloseMenu với isCriticalDestroy = true để dọn dẹp hàng đợi
                        GuiCanvasManager.Instance.confirmationMenu.CloseMenu(true);
                        SceneManager.LoadScene("MainMenuScene");
                    },
                    onCancel: () =>
                    {
                        Time.timeScale = 1f;
                        // Gọi CloseMenu với isCriticalDestroy = true để dọn dẹp hàng đợi
                        GuiCanvasManager.Instance.confirmationMenu.CloseMenu(true);
                        // Tải lại Scene hiện tại sẽ kích hoạt LoadFromSave lần nữa với path đã lưu trong Prefs
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
                    },
                    priority: ConfirmationMenu.MenuPriority.Critical,
                    debugOwner: "GameOverHandler"
                );

                // Ghi đè chữ trên nút Cancel thành Load Save
                if (cancelButton != null)
                {
                    var btnText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "Load save lần cuối";
                }

                // Ghi đè chữ trên nút Confirm thành Về Menu chính
                if (confirmButton != null)
                {
                    var btnText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "Về Menu chính";
                }
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
