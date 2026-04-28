using UnityEngine;
using UnityEngine.UI;
using Script.Audio;

namespace Script.GUI_Controller.StartMenu
{
    public class OptionsPanelController : MonoBehaviour
    {
        public Button backButton;

        private void Start()
        {
            backButton.onClick.AddListener(() => {
                AudioManager.Instance?.PlayUISound("UI_Click");
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.Home);
            });
        }

        private void OnEnable()
        {
            // Làm mới các slider âm lượng
            var sliders = GetComponentsInChildren<Script.Audio.AudioVolumeSlider>(true);
        foreach (var slider in sliders)
                slider.Refresh();
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            if (backButton == null) backButton = transform.Find("BackButton")?.GetComponent<Button>();
            Debug.Log("[OptionsPanelController] Đã tự động gán UI.");
        }
    }
}
