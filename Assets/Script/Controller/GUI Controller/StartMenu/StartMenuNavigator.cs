using UnityEngine;
using UnityEngine.SceneManagement;
using Script.Audio;
using Script.Manager.SaveManager;

namespace Script.GUI_Controller.StartMenu
{
    public enum MenuPanel
    {
        Home,
        ChooseSave,
        Options,
        CreateNewWorld,
        ShowWorldInfo,
        Confirmation
    }

    public class StartMenuNavigator : MonoBehaviour
    {
        public static StartMenuNavigator Instance { get; private set; }

        [Header("Panel References")]
        public GameObject homePanel;
        public GameObject chooseSavePanel;
        public GameObject optionsPanel;
        public GameObject createNewWorldPanel;
        public GameObject showWorldInfoPanel;
        public GameObject confirmationPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (WorldSaveListManager.Instance == null)
                Debug.LogError("[StartMenuNavigator] WorldSaveListManager không tìm thấy!");

            WorldSaveListManager.Instance?.CreateSavesFolderIfNotExists();
            ShowPanel(MenuPanel.Home);
        }

        public void ShowPanel(MenuPanel panel)
        {
            // Tắt tất cả panel
            homePanel.SetActive(false);
            chooseSavePanel.SetActive(false);
            optionsPanel.SetActive(false);
            createNewWorldPanel.SetActive(false);
            showWorldInfoPanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);

            // Bật panel được yêu cầu
            switch (panel)
            {
                case MenuPanel.Home: homePanel.SetActive(true); break;
                case MenuPanel.ChooseSave: chooseSavePanel.SetActive(true); break;
                case MenuPanel.Options: optionsPanel.SetActive(true); break;
                case MenuPanel.CreateNewWorld: createNewWorldPanel.SetActive(true); break;
                case MenuPanel.ShowWorldInfo: showWorldInfoPanel.SetActive(true); break;
                case MenuPanel.Confirmation: if (confirmationPanel != null) confirmationPanel.SetActive(true); break;
            }
        }

        // Hành động toàn cục
        public void LoadWorld(string worldName)
        {
            AudioManager.Instance?.PlayUISound("UI_Click");
            string path = WorldSaveListManager.Instance?.GetWorldFolderPath(worldName);
            if (path != null)
            {
                WorldSaveListManager.Instance.SetActiveWorldPath(path);
                SceneManager.LoadScene("GamePlayScene");
            }
            else Debug.LogError("StartMenuNavigator: Không tìm thấy đường dẫn world");
        }

        public void DeleteWorld(string worldName)
        {
            AudioManager.Instance?.PlayUISound("UI_Click");
            WorldSaveListManager.Instance?.DeleteWorldSave(worldName);
            // Yêu cầu panel Load Game làm mới danh sách sau khi xoá
            var chooseCtrl = chooseSavePanel.GetComponent<ChooseSavePanelController>();
            chooseCtrl?.RefreshList();
        }

        public void CreateNewWorld(string worldName, string seed)
        {
            AudioManager.Instance?.PlayUISound("UI_Click");
            if (string.IsNullOrEmpty(seed))
                seed = GenerateRandomSeed();

            bool success = WorldSaveListManager.Instance.CreateWorldSave(worldName, seed);
            if (success)
                LoadWorld(worldName);
        }

        private string GenerateRandomSeed()
        {
            System.Random random = new System.Random();
            return random.Next(0, 99999999).ToString("D8");
        }

        [ContextMenu("Auto Assign Panels")]
        public void AutoAssignPanels()
        {
            // Thử tìm dựa trên tên GameObject phổ biến
            if (homePanel == null) homePanel = FindPanel("Home");
            if (chooseSavePanel == null) chooseSavePanel = FindPanel("ChooseSave");
            if (optionsPanel == null) optionsPanel = FindPanel("Options");
            if (createNewWorldPanel == null) createNewWorldPanel = FindPanel("CreateNewWorld");
            if (showWorldInfoPanel == null) showWorldInfoPanel = FindPanel("ShowWorldInfo");
            if (confirmationPanel == null) confirmationPanel = FindPanel("Confirmation");

            Debug.Log("[StartMenuNavigator] Đã hoàn tất tự động gán panel. Hãy kiểm tra lại trong Inspector.");
        }

        private GameObject FindPanel(string key)
        {
            // Tìm trong con của object này trước
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains(key, System.StringComparison.OrdinalIgnoreCase) && child.name.Contains("Panel", System.StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}
