using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Script.GUI_Controller.StartMenu
{
    public class ConfirmationPanelController : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public Button confirmButton;
        public Button cancelButton;

        private Action onConfirm;
        private Action onCancel;

        private void Start()
        {
            confirmButton.onClick.AddListener(() => {
                onConfirm?.Invoke();
                // Luôn quay lại panel ChooseSave sau khi xác nhận xong
                StartMenuNavigator.Instance.ShowPanel(MenuPanel.ChooseSave);
            });

            cancelButton.onClick.AddListener(() => {
                if (onCancel != null)
                {
                    onCancel.Invoke();
                }
                else
                {
                    StartMenuNavigator.Instance.ShowPanel(MenuPanel.ChooseSave);
                }
            });
        }

        public void Show(string title, string message, Action confirmAction, Action cancelAction = null)
        {
            titleText.text = title;
            messageText.text = message;
            onConfirm = confirmAction;
            onCancel = cancelAction;
        }

        [ContextMenu("Auto Assign UI")]
        public void AutoAssignUI()
        {
            if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            if (messageText == null) messageText = transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
            if (confirmButton == null) confirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();
            if (cancelButton == null) cancelButton = transform.Find("CancelButton")?.GetComponent<Button>();
            Debug.Log("[ConfirmationPanelController] Đã tự động gán UI.");
        }
    }
}
