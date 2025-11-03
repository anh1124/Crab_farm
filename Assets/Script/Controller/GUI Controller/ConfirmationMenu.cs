using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ConfirmationMenu : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject panel; // Khung menu
    public TextMeshProUGUI headerText; // Tiêu đề của menu
    public TextMeshProUGUI contentText; // Nội dung của menu
    public Button cancelButton; // Nút Cancel
    public Button confirmButton; // Nút Confirm

    // Action để gọi khi nhấn Confirm
    private System.Action confirmAction;
    // Khởi tạo menu
    public void Initialize(string header, string content, System.Action onConfirm)
    {
        // Hiển thị menu
        panel.SetActive(true);

        // Thiết lập tiêu đề và nội dung
        headerText.text = header;
        contentText.text = content;

        // Gán hành động Confirm
        confirmAction = onConfirm;

        // Gán sự kiện cho nút
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() => CloseMenu());

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            confirmAction?.Invoke(); // Gọi hàm xác nhận
            CloseMenu();
        });
    }

    // Đóng menu
    private void CloseMenu()
    {
        panel.SetActive(false);
    }
}
