using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Save_Controller;
using Script.Audio;

namespace Script.GUI_Controller.StartMenu
{
    public class ShowWorldInfoPanelController : MonoBehaviour
    {
        public TextMeshProUGUI worldInfoContent;
        public Button exitButton;

        private void Start()
        {
            exitButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.ChooseSave);
            });
        }

        public void DisplayWorldInfo(WorldInfo info)
        {
            if (info == null) return;
            string text = $"World Name: {info.Name}\n" +
                          $"Seed: {info.Seed}\n" +
                          $"Creation Date: {info.CreationDate}\n" +
                          $"Game Time: {info.GameHour:D2}:{info.GameMinute:D2}\n" +
                          $"Game Date: {info.GameDay}/{info.GameMonth}/{info.GameYear}";
            worldInfoContent.text = text;
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            if (worldInfoContent == null) worldInfoContent = GetComponentInChildren<TextMeshProUGUI>(true);
            if (exitButton == null) exitButton = transform.Find("ExitButton")?.GetComponent<Button>();
            Debug.Log("[ShowWorldInfoPanelController] Đã tự động gán UI.");
        }
    }
}
