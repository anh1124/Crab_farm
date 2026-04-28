using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Audio;
using Script.Manager.SaveManager;
using Script.Save_Controller;

namespace Script.GUI_Controller.StartMenu
{
    public class ChooseSavePanelController : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject emptySaveText;
        public GameObject worldInfoContainer;
        public TextMeshProUGUI worldNameText;
        public TextMeshProUGUI worldPlayTimeText;
        public TextMeshProUGUI totalWorldText;

        [Header("Buttons")]
        public Button backButton;
        public Button playButton;
        public Button deleteButton;
        public Button nextButton;
        public Button prevButton;
        public Button newGameButton;
        public Button playTutorialButton;
        public Button openWorldInfoButton;

        [Header("Tutorial")]
        public TutorialPromptController tutorialPrompt; // Kéo từ cùng GameObject hoặc child

        private int currentWorldIndex = 0;
        private WorldInfo[] worldInfos;
        private int totalWorld = 0;

        private void Start()
        {
            backButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.Home);
            });

            nextButton.onClick.AddListener(NextSave);
            prevButton.onClick.AddListener(PrevSave);

            playButton.onClick.AddListener(() => {
                if (worldInfos != null && currentWorldIndex >= 0 && currentWorldIndex < totalWorld)
                    StartMenuNavigator.Instance.LoadWorld(worldInfos[currentWorldIndex].Name);
            });

            deleteButton.onClick.AddListener(() => {
                if (worldInfos != null && currentWorldIndex >= 0 && currentWorldIndex < totalWorld)
                {
                    ShowDeleteConfirmation(worldInfos[currentWorldIndex].Name);
                }
                else
                {
                    Debug.LogWarning($"[ChooseSavePanelController] Delete conditions not met. worldInfos: {worldInfos?.Length ?? 0}");
                }
            });
            newGameButton.onClick.AddListener(() => {
                tutorialPrompt.ShowTutorialPrompt(
                    onConfirm: () => { /* Load tutorial scene (đã xử lý trong TutorialPromptController) */ },
                    onCancel: () => StartMenuNavigator.Instance.ShowPanel(MenuPanel.CreateNewWorld)
                );
            });

            playTutorialButton.onClick.AddListener(() => {
                tutorialPrompt.OpenTutorialScene();
            });

            openWorldInfoButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                var infoPanel = StartMenuNavigator.Instance.showWorldInfoPanel
                                .GetComponent<ShowWorldInfoPanelController>();
                if (infoPanel != null && worldInfos != null && currentWorldIndex >= 0 && currentWorldIndex < totalWorld)
                {
                    infoPanel.DisplayWorldInfo(worldInfos[currentWorldIndex]);
                    StartMenuNavigator.Instance.ShowPanel(MenuPanel.ShowWorldInfo);
                }
            });
        }

        private void ShowDeleteConfirmation(string worldName)
        {
            var panel = StartMenuNavigator.Instance.confirmationPanel;
            if (panel == null)
            {
                Debug.LogError("[ChooseSavePanelController] confirmationPanel is null!");
                return;
            }

            string title = "Delete World";
            string message = $"Are you sure you want to delete the world \"{worldName}\"?";
            System.Action onConfirm = () => StartMenuNavigator.Instance.DeleteWorld(worldName);

            var confirmCtrl = panel.GetComponent<ConfirmationPanelController>();
            if (confirmCtrl != null)
            {
                confirmCtrl.Show(title, message, onConfirm);
            }
            else
            {
                var legacyConfirm = panel.GetComponent<ConfirmationMenu>();
                if (legacyConfirm != null)
                {
                    legacyConfirm.Show(title, message, onConfirm, debugOwner: "ChooseSavePanelController");
                }
                else
                {
                    Debug.LogError("[ChooseSavePanelController] No confirmation component found!");
                    return;
                }
            }
            StartMenuNavigator.Instance.ShowPanel(MenuPanel.Confirmation);
        }

        private void OnEnable()
        {
            RefreshList(); // Mỗi khi panel được bật thì làm mới danh sách save
        }

        public void RefreshList()
        {
            if (WorldSaveListManager.Instance == null) return;
            worldInfos = WorldSaveListManager.Instance.GetAllWorldInfos(); 
            totalWorld = worldInfos != null ? worldInfos.Length : 0;

            if (totalWorld == 0)
            {
                emptySaveText.SetActive(true);
                worldInfoContainer.SetActive(false);
                currentWorldIndex = 0;
            }
            else
            {
                emptySaveText.SetActive(false);
                worldInfoContainer.SetActive(true);
                currentWorldIndex = Mathf.Clamp(currentWorldIndex, 0, totalWorld - 1);
                DisplayCurrentWorld();
            }
            UpdateTotalWorldText();
        }

        private void DisplayCurrentWorld()
        {
            if (worldInfos == null || totalWorld == 0) return;
            WorldInfo info = worldInfos[currentWorldIndex];
            worldNameText.text = info.Name;
            worldPlayTimeText.text = info.CreationDate.ToString(); 
        }

        private void NextSave()
        {
            AudioManager.Instance?.PlayUISound("UI_Click");
            if (totalWorld == 0) return;
            currentWorldIndex = (currentWorldIndex + 1) % totalWorld;
            DisplayCurrentWorld();
            UpdateTotalWorldText();
        }

        private void PrevSave()
        {
            AudioManager.Instance?.PlayUISound("UI_Click");
            if (totalWorld == 0) return;
            currentWorldIndex = (currentWorldIndex - 1 + totalWorld) % totalWorld;
            DisplayCurrentWorld();
            UpdateTotalWorldText();
        }

        private void UpdateTotalWorldText()
        {
            totalWorldText.text = $"World {currentWorldIndex + 1}/{totalWorld}";
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            // Tìm Buttons
            if (backButton == null) backButton = FindInChild<Button>("Back");
            if (playButton == null) playButton = FindInChild<Button>("Play");
            if (deleteButton == null) deleteButton = FindInChild<Button>("Delete");
            if (nextButton == null) nextButton = FindInChild<Button>("Next");
            if (prevButton == null) prevButton = FindInChild<Button>("Prev");
            if (newGameButton == null) newGameButton = FindInChild<Button>("New");
            if (playTutorialButton == null) playTutorialButton = FindInChild<Button>("Tutorial");
            if (openWorldInfoButton == null) openWorldInfoButton = FindInChild<Button>("Info");

            // Tìm Texts
            if (worldNameText == null) worldNameText = FindInChild<TextMeshProUGUI>("Name");
            if (worldPlayTimeText == null) worldPlayTimeText = FindInChild<TextMeshProUGUI>("Time");
            if (totalWorldText == null) totalWorldText = FindInChild<TextMeshProUGUI>("Total");

            // Tìm Containers
            if (worldInfoContainer == null) worldInfoContainer = transform.Find("WorldInfoContainer")?.gameObject;
            if (emptySaveText == null) emptySaveText = transform.Find("EmptySaveText")?.gameObject;

            // Tìm Tutorial Prompt
            if (tutorialPrompt == null) tutorialPrompt = GetComponentInChildren<TutorialPromptController>(true);

            Debug.Log("[ChooseSavePanelController] Đã tự động gán UI. Hãy kiểm tra lại các field.");
        }

        private T FindInChild<T>(string key) where T : Component
        {
            foreach (var item in GetComponentsInChildren<T>(true))
            {
                if (item.name.Contains(key, System.StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }
    }
}
