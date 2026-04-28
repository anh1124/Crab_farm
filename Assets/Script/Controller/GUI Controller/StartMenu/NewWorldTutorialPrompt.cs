using UnityEngine;
using UnityEngine.UI;

namespace Script.Tutorial
{
    /// <summary>
    /// Hiện ConfirmPanel khi ấn NewWorld, dùng PlayerPrefs lưu số lần hiện (tối đa 2)
    /// </summary>
    public class NewWorldTutorialPrompt : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button newWorldButton;
        [SerializeField] private ConfirmationMenu confirmationMenu;
        
        [Header("Settings")]
        [SerializeField] private int maxPromptCount = 2;
        [SerializeField] private string promptPrefKey = "TutorialPromptCount";
        
        private void Start()
        {
            if (newWorldButton != null)
            {
                newWorldButton.onClick.AddListener(OnNewWorldClicked);
            }
        }
        
        private void OnNewWorldClicked()
        {
            int promptCount = PlayerPrefs.GetInt(promptPrefKey, 0);
            
            if (promptCount >= maxPromptCount)
            {
                // Đã hiện đủ lần, không hỏi nữa
                LoadNewWorld();
                return;
            }
            
            // Hiện ConfirmPanel
            if (confirmationMenu != null)
            {
                confirmationMenu.Show(
                    header: "Play Tutorial?",
                    content: "Would you like to play the tutorial before starting a new world?\n\nThe tutorial will teach you the basic gameplay.",
                    onConfirm: () =>
                    {
                        IncrementPromptCount();
                        LoadTutorialScene();
                    },
                    onCancel: () =>
                    {
                        IncrementPromptCount();
                        LoadNewWorld();
                    },
                    priority: ConfirmationMenu.MenuPriority.Normal,
                    debugOwner: "NewWorldTutorialPrompt"
                );
            }
            else
            {
                LoadNewWorld();
            }
        }
        
        private void IncrementPromptCount()
        {
            int count = PlayerPrefs.GetInt(promptPrefKey, 0);
            PlayerPrefs.SetInt(promptPrefKey, count + 1);
            PlayerPrefs.Save();
        }
        
        private void LoadTutorialScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TutorialScene");
        }
        
        private void LoadNewWorld()
        {
            // Gọi hàm tạo new world có sẵn
            Debug.Log("[NewWorldTutorialPrompt] Loading new world...");
            // StartMenuController.Instance?.CreateNewWorld();
        }
    }
}
