using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Audio;

namespace Script.GUI_Controller.StartMenu
{
    public class HomePanelController : MonoBehaviour
    {
        [Header("Buttons")]
        public Button loadGameButton;
        public Button settingsButton;
        public TextMeshProUGUI versionText;

        private void Start()
        {
            loadGameButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.ChooseSave);
            });

            settingsButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.Options);
            });

        // Nếu có thêm nút (info) thì gán tương tự
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            if (loadGameButton == null) loadGameButton = transform.Find("LoadGameButton")?.GetComponent<Button>();
            if (settingsButton == null) settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();
            if (versionText == null) versionText = GetComponentInChildren<TextMeshProUGUI>(true);
            Debug.Log("[HomePanelController] Đã tự động gán UI.");
        }
    }
}
