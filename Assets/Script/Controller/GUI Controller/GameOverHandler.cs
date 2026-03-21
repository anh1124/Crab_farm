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
            EnsureReferences();
            
            int survivedDays = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetElapsedDays() : 0;
            
            if (panel != null) panel.SetActive(true);

            if (headerText != null) headerText.text = "Bạn đã thua";
            if (contentText != null) contentText.text = $"Bạn đã sống sót {survivedDays} ngày.\nBạn đã làm rất tốt!";

            // Nút bên trái: Load save lần cuối
            if (cancelButton != null) 
            {
                cancelButton.gameObject.SetActive(true);
                var btnText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Load save lần cuối";
                
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => {
                    Time.timeScale = 1f;
                    // Tải lại Scene hiện tại sẽ kích hoạt LoadFromSave lần nữa với path đã lưu trong Prefs
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
                });
            }

            // Nút bên phải: Về Menu chính
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
                var btnText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Về Menu chính";
                
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f; // Bỏ pause trước khi load lại menu
                    SceneManager.LoadScene("MainMenuScene");
                });
            }

            Time.timeScale = 0f; // Pause game
        }
    }
}
