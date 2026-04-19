using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ConfirmationMenu : MonoBehaviour
{
    public enum MenuPriority { Low = 0, Normal = 1, High = 2, Critical = 3 }

    private struct MenuRequest
    {
        public string Header;
        public string Content;
        public System.Action OnConfirm;
        public System.Action OnCancel;
        public MenuPriority Priority;
        public string DebugOwner;
    }

    [Header("UI Components")]
    public GameObject panel; // Khung menu
    public TextMeshProUGUI headerText; // Tiêu đề của menu
    public TextMeshProUGUI contentText; // Nội dung của menu
    public Button cancelButton; // Nút Cancel
    public Button confirmButton; // Nút Confirm

    private List<MenuRequest> _queue = new List<MenuRequest>();
    private MenuRequest _current;
    private bool _isShowing;

    /// <summary>
    /// Thêm một yêu cầu hiển thị menu vào hệ thống ưu tiên.
    /// </summary>
    public void Show(string header, string content, System.Action onConfirm, 
                     System.Action onCancel = null, 
                     MenuPriority priority = MenuPriority.Normal, 
                     string debugOwner = "Unknown")
    {
        var req = new MenuRequest
        {
            Header = header,
            Content = content,
            OnConfirm = onConfirm,
            OnCancel = onCancel,
            Priority = priority,
            DebugOwner = debugOwner
        };

        if (!_isShowing)
        {
            ShowImmediate(req);
            return;
        }

        if (priority > _current.Priority)
        {
            // Tạm dừng panel hiện tại -> đẩy vào đầu hàng đợi
            _queue.Insert(0, _current);
            ShowImmediate(req);
        }
        else
        {
            // Xếp hàng đợi theo độ ưu tiên giảm dần
            _queue.Add(req);
            _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    private void ShowImmediate(MenuRequest req)
    {
        _isShowing = true;
        _current = req;

        panel.SetActive(true);
        headerText.text = req.Header;
        contentText.text = req.Content;

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            req.OnCancel?.Invoke();
            CloseMenu(false);
        });

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            req.OnConfirm?.Invoke();
            CloseMenu(false);
        });
    }

    /// <summary>
    /// Hàm dành cho nội bộ hoặc các luồng bắt buộc tắt (như Game Over Reset).
    /// </summary>
    public void CloseMenu(bool isCriticalDestroy = false)
    {
        if (isCriticalDestroy && _queue.Count > 0)
        {
            foreach (var item in _queue)
            {
                Debug.LogWarning($"[ConfirmationMenu] Drop request từ '{item.DebugOwner}' vì Game Over action/Scene load.");
            }
            _queue.Clear();
        }

        _isShowing = false;
        panel.SetActive(false);
        ResetMenu();

        if (_queue.Count > 0)
        {
            var next = _queue[0];
            _queue.RemoveAt(0);
            ShowImmediate(next);
        }
    }

    // Hàm gọi cũ giữ lại alias nếu cần (Priority Normal mặc định)
    [System.Obsolete("Dùng Show() thay thế.")]
    public void Initialize(string header, string content, System.Action onConfirm)
    {
        Show(header, content, onConfirm, null, MenuPriority.Normal, "LegacyInitialize");
    }

    private void ResetMenu()
    {
        if (headerText != null) headerText.text = string.Empty;
        if (contentText != null) contentText.text = string.Empty;

        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
    }
}
