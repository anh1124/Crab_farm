using System.Collections.Generic;
using Script.GUI_Controller;
using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// Singleton - Quản lý hint buttons và hint panel.
    /// Vòng tuần hoàn: HintTrigger → HintManager → HintPanelUI + HintSaveManager
    /// </summary>
    public class HintManager : MonoBehaviour
    {
        public static HintManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _isDebug = true;

        [Header("Hint Panel")]
        [SerializeField] private HintPanelUI _panelView;

        [Header("Hint Button Spawn")]
        [SerializeField] private Transform _hintButtonContainer;
        [SerializeField] private GameObject _hintButtonPrefab;

        private HintDataSO _currentData;
        private List<GameObject> _activeButtons = new List<GameObject>();

        private void Log(string message)
        {
            if (_isDebug)
                Debug.Log($"[HintManager] {message}");
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Log("Initialized");
            }
            else
            {
                Log("Another instance found, destroying this");
                Destroy(gameObject);
                return;
            }

            if (_panelView == null)
                Debug.LogError("[HintManager] _panelView chưa được gán!");

            if (_hintButtonContainer == null)
                Debug.LogError("[HintManager] _hintButtonContainer chưa được gán!");

            if (_hintButtonPrefab == null)
                Debug.LogError("[HintManager] _hintButtonPrefab chưa được gán!");
        }

        /// <summary>
        /// Spawn hint buttons từ list data. Gọi từ HintTrigger khi player enter.
        /// </summary>
        public void SpawnHintButtons(List<HintDataSO> hintDataList)
        {
            Log($"SpawnHintButtons called with {hintDataList?.Count ?? 0} items");

            if (hintDataList == null || hintDataList.Count == 0)
            {
                Log("hintDataList null or empty, returning");
                return;
            }
            if (_hintButtonPrefab == null)
            {
                Log("_hintButtonPrefab is null!");
                return;
            }
            if (_hintButtonContainer == null)
            {
                Log("_hintButtonContainer is null!");
                return;
            }

            // Xóa buttons cũ nếu có
            ClearHintButtons();

            foreach (var data in hintDataList)
            {
                if (data == null) continue;
                if (IsIgnored(data.title)) continue;

                var btnGO = Instantiate(_hintButtonPrefab, _hintButtonContainer);
                _activeButtons.Add(btnGO);

                var hintButton = btnGO.GetComponent<HintButton>();
                if (hintButton != null)
                {
                    hintButton.Setup(data);
                    Log($"Setup button with data: {data.title}");
                }
                else
                {
                    Log("Button prefab missing HintButton component!");
                }
            }
        }

        /// <summary>
        /// Xóa tất cả hint buttons. Gọi từ HintTrigger khi player exit.
        /// </summary>
        public void ClearHintButtons()
        {
            Log($"Clearing {_activeButtons.Count} buttons");
            foreach (var btn in _activeButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _activeButtons.Clear();
        }

        /// <summary>
        /// Mở hint panel với data. Gọi từ HintButton.
        /// </summary>
        public void OpenPanel(HintDataSO data)
        {
            Log($"OpenPanel called with: {(data != null ? data.title : "NULL")}");

            if (data == null)
            {
                Log("data is null, returning");
                return;
            }

            _currentData = data;

            // Đánh dấu đã đọc
            if (HintSaveManager.Instance != null && !HintSaveManager.Instance.IsRead(data.title))
                HintSaveManager.Instance.SetRead(data.title);

            // Cập nhật lại trạng thái hiển thị của tất cả hint buttons (unread dot)
            RefreshAllHintButtons();

            bool isIgnored = HintSaveManager.Instance != null && 
                             HintSaveManager.Instance.IsIgnored(data.title);

            // Mở panel qua GuiCanvasManager hoặc trực tiếp
            if (GuiCanvasManager.Instance != null)
            {
                Log("Using GuiCanvasManager to show panel");
                GuiCanvasManager.Instance.ShowHintPanel();
            }
            else if (_panelView != null)
            {
                Log("GuiCanvasManager null, activating _panelView directly");
                _panelView.gameObject.SetActive(true);
            }
            else
            {
                Log("ERROR: Neither GuiCanvasManager nor _panelView available!");
            }

            if (_panelView != null)
            {
                Log("Calling _panelView.Show()");
                _panelView.Show(data, isIgnored, OnIgnoreChanged, ClosePanel);
            }
            else
            {
                Log("ERROR: _panelView is null!");
            }
        }

        /// <summary>
        /// Đóng hint panel. Gọi từ HintPanelUI close button.
        /// </summary>
        public void ClosePanel()
        {
            Log("ClosePanel called");

            if (GuiCanvasManager.Instance != null)
                GuiCanvasManager.Instance.HideHintPanel();
            else if (_panelView != null)
                _panelView.gameObject.SetActive(false);

            _currentData = null;
        }

        private void OnIgnoreChanged(bool isIgnored)
        {
            if (_currentData == null || HintSaveManager.Instance == null) return;

            string title = _currentData.title;

            if (isIgnored)
            {
                HintSaveManager.Instance.SetIgnored(title);
                // Sử dụng CloseHintAndResume để đóng panel và resume game đúng cách
                if (GuiCanvasManager.Instance != null)
                    GuiCanvasManager.Instance.CloseHintAndResume();
                else
                    ClosePanel();
            }
            else
            {
                HintSaveManager.Instance.ClearIgnored(title);
            }
        }

        private bool IsIgnored(string title)
        {
            return HintSaveManager.Instance != null && 
                   HintSaveManager.Instance.IsIgnored(title);
        }

        /// <summary>
        /// Cập nhật trạng thái hiển thị (unread dot) của tất cả hint buttons đang active.
        /// Gọi sau khi đọc hint để cập nhật UI ngay lập tức.
        /// </summary>
        private void RefreshAllHintButtons()
        {
            foreach (var btnGO in _activeButtons)
            {
                if (btnGO == null) continue;
                var hintButton = btnGO.GetComponent<HintButton>();
                if (hintButton != null)
                {
                    hintButton.UpdateUnreadState();
                }
            }
        }
    }

    // Obsolete classes for backward compatibility
    [System.Obsolete("Use HintManager. Kept for backward-compatible Unity references.")]
    public class HintUIManager : HintManager { }

    [System.Obsolete("Use HintManager. Kept for backward-compatible Unity references.")]
    public class TutorialUIManager : HintManager { }
}
