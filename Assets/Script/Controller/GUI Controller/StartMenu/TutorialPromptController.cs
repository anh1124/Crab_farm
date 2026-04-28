using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Script.GUI_Controller.StartMenu
{
    /// <summary>
    /// Controller hiển thị ConfirmationPanel hỏi có chơi tutorial không
    /// </summary>
    public class TutorialPromptController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button tutorialButton; // Nút mở tutorial trực tiếp (optional)
        
        private GameObject _confirmationPanel;
        private GameObject ConfirmationPanel => _confirmationPanel ??= StartMenuNavigator.Instance?.confirmationPanel;
        
        [Header("Target Scene")]
        [SerializeField] private string tutorialSceneName = "TutorialScene";
        
        // Callbacks cho StartMenuController
        private System.Action _onConfirmTutorial;
        private System.Action _onCancelTutorial;
        
        private void Start()
        {
            // Setup tutorial button nếu có
            if (tutorialButton != null)
                tutorialButton.onClick.AddListener(OpenTutorialScene);
        }
        
        /// <summary>
        /// Hiện ConfirmationPanel hỏi có chơi tutorial không
        /// </summary>
        /// <param name="onConfirm">Callback khi player chọn Có (load tutorial)</param>
        /// <param name="onCancel">Callback khi player chọn Không (tiếp tục new game)</param>
        public void ShowTutorialPrompt(System.Action onConfirm, System.Action onCancel)
        {
            _onConfirmTutorial = onConfirm;
            _onCancelTutorial = onCancel;
            
            if (ConfirmationPanel == null)
            {
                Debug.LogError("[TutorialPromptController] confirmationPanel is null! Hãy assign trong StartMenuNavigator.");
                // Fallback: gọi cancel luôn
                _onCancelTutorial?.Invoke();
                return;
            }

            ConfirmationMenu confirmationMenu = ConfirmationPanel.GetComponent<ConfirmationMenu>();
            if (confirmationMenu != null)
            {
                ConfirmationPanel.SetActive(true);
                confirmationMenu.Show(
                    "Play Tutorial?",
                    "Would you like to play the tutorial before starting a new world?\n\nThe tutorial will teach you the basic gameplay.",
                    onConfirm: OnConfirm,
                    onCancel: OnCancel,
                    priority: ConfirmationMenu.MenuPriority.Normal,
                    debugOwner: "TutorialPromptController"
                );
            }
            else
            {
                // Thử với ConfirmationPanelController mới
                var confirmCtrl = ConfirmationPanel.GetComponent<ConfirmationPanelController>();
                if (confirmCtrl != null)
                {
                    ConfirmationPanel.SetActive(true);
                    confirmCtrl.Show(
                        "Play Tutorial?",
                        "Would you like to play the tutorial before starting a new world?\n\nThe tutorial will teach you the basic gameplay.",
                        confirmAction: OnConfirm,
                        cancelAction: OnCancel
                    );
                }
                else
                {
                    Debug.LogError("[TutorialPromptController] Confirmation component not found!");
                    _onCancelTutorial?.Invoke();
                }
            }
        }
        
        private void OnConfirm()
        {
            // Có - Load Tutorial Scene
            SceneManager.LoadScene(tutorialSceneName);
            _onConfirmTutorial?.Invoke();
        }
        
        private void OnCancel()
        {
            // Không - Gọi callback cancel
            _onCancelTutorial?.Invoke();
        }
        
        /// <summary>
        /// Mở Tutorial Scene trực tiếp (từ button riêng)
        /// </summary>
        public void OpenTutorialScene()
        {
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");
            SceneManager.LoadScene(tutorialSceneName);
        }
    }
}
