using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Audio;

namespace Script.GUI_Controller.StartMenu
{
    public class CreateWorldPanelController : MonoBehaviour
    {
        public TMP_InputField worldNameInput;
        public TMP_InputField seedInput;
        public Button createButton;
        public Button backButton;

        private bool hasClickedWorldName = false;

        private void Start()
        {
            backButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.ChooseSave);
            });

            createButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                string name = worldNameInput.text;
                string seed = seedInput.text;
                StartMenuNavigator.Instance.CreateNewWorld(name, seed);
            });

            worldNameInput.onSelect.AddListener(_ => OnInputWorldNameClick());

            // Giới hạn input seed
            seedInput.characterLimit = 10;
            seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        private void OnEnable()
        {
            AutoFillWorldName();
            hasClickedWorldName = false;
            seedInput.text = "";
        }

        private void AutoFillWorldName()
        {
            // Note: Application.persistentDataPath should be used instead of hardcoded paths
            string basePath = System.IO.Path.Combine(Application.persistentDataPath, "saves");
            string baseName = "new_world";
            int index = 1;
            string potentialName = baseName;
            while (System.IO.Directory.Exists(System.IO.Path.Combine(basePath, potentialName)))
            {
                potentialName = $"{baseName} {index}";
                index++;
            }
            worldNameInput.text = potentialName;
        }

        private void OnInputWorldNameClick()
        {
            if (!hasClickedWorldName)
            {
                worldNameInput.text = "";
                hasClickedWorldName = true;
            }
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            if (worldNameInput == null) worldNameInput = transform.Find("WorldNameInput")?.GetComponent<TMP_InputField>();
            if (seedInput == null) seedInput = transform.Find("SeedInput")?.GetComponent<TMP_InputField>();
            if (createButton == null) createButton = transform.Find("CreateButton")?.GetComponent<Button>();
            if (backButton == null) backButton = transform.Find("BackButton")?.GetComponent<Button>();
            Debug.Log("[CreateWorldPanelController] Đã tự động gán UI.");
        }
    }
}
