using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Script.Tutorial
{
    /// <summary>
    /// UI hiển thị dialog với support cho cả skipable và conditional
    /// </summary>
    public class TutorialDialogPanel : MonoBehaviour
    {
        [Header("Panel Anchor")]
        [Tooltip("Panel làm anchor/background cho dialog tutorial. Click vào panel để next step")]
        [SerializeField] private GameObject panel;
        [Tooltip("Button component trên panel (kéo chính panel vào nếu có Button component)")]
        [SerializeField] private Button panelButton;
        
        [Header("Tutorial Title")]
        [Tooltip("Text hiển thị tên/title của tutorial (vd: 'Hướng dẫn cơ bản')")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Dialog Content")]
        [Tooltip("Text hiển thị nội dung dialog nói chuyện")]
        [SerializeField] private TextMeshProUGUI dialogText;
        
        [Header("Buttons")]
        [Tooltip("Nút next step dialog (chỉ hiện khi Skipable)")]
        [SerializeField] private Button nextStepButton;
        [Tooltip("Nút skip tutorial hiện tại, chuyển sang tutorial tiếp theo")]
        [SerializeField] private Button skipTutorialButton;
        
        private void Start()
        {
            // Hook button events
            if (nextStepButton != null)
            {
                nextStepButton.onClick.AddListener(OnNextStepButtonClicked);
            }
            
            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.AddListener(OnSkipTutorialButtonClicked);
            }
            
            // Hook panel button click để next step
            if (panelButton != null)
            {
                panelButton.onClick.AddListener(OnNextStepButtonClicked);
            }
        }
        
        private void OnDestroy()
        {
            if (nextStepButton != null)
                nextStepButton.onClick.RemoveListener(OnNextStepButtonClicked);
            
            if (skipTutorialButton != null)
                skipTutorialButton.onClick.RemoveListener(OnSkipTutorialButtonClicked);
            
            if (panelButton != null)
                panelButton.onClick.RemoveListener(OnNextStepButtonClicked);
        }
        
        /// <summary>
        /// Click nút Next Step - next dialog
        /// </summary>
        private void OnNextStepButtonClicked()
        {
            TutorialController.Instance?.OnDialogClicked();
        }
        
        /// <summary>
        /// Click nút Skip Tutorial - chuyển sang tutorial tiếp theo
        /// </summary>
        private void OnSkipTutorialButtonClicked()
        {
            TutorialController.Instance?.EndTutorial();
        }
        
        public void Show(string title, string dialog, TutorialDialogType type)
        {
            panel?.SetActive(true);
            
            // Title (tên tutorial)
            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(title) ? "" : $"<b>{title}</b>";
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            
            // Dialog content (nội dung nói chuyện)
            dialogText?.SetText(dialog);
            
            
            // Hiện/hide next step button theo type
            if (nextStepButton != null)
                nextStepButton.gameObject.SetActive(type == TutorialDialogType.Skipable);
        }
        
        public void Hide()
        {
            panel?.SetActive(false);
        }
        
        public void Reset()
        {
            // Clear text
            if (titleText != null)
            {
                titleText.text = "";
                titleText.gameObject.SetActive(false);
            }
            if (dialogText != null) dialogText.text = "";
            
            // Hide buttons
            nextStepButton?.gameObject.SetActive(false);
            
            // Hide panel
            panel?.SetActive(false);
        }
    }
}
