using UnityEngine;
using UnityEngine.UI;

namespace Script.Tutorial
{
    /// <summary>
    /// Đặt lên prefab HintButton trong scene.
    /// Khi click sẽ gọi HintManager mở panel với data tương ứng.
    /// </summary>
    public class HintButton : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _isDebug = true;

        [Header("Hint Data")]
        [SerializeField] private HintDataSO _hintDataSO;

        [Header("Unread Indicator")]
        [SerializeField] private GameObject _unreadDot;

        private Button _button;

        private void Log(string message)
        {
            if (_isDebug)
                Debug.Log($"[HintButton] {message}");
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
                _button = GetComponentInChildren<Button>();

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
                Log("Button listener added");
            }
            else
            {
                Log("ERROR: No Button found on this GameObject or children!");
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        /// <summary>
        /// Gọi từ HintManager để setup data cho button.
        /// </summary>
        public void Setup(HintDataSO data)
        {
            Log($"Setup called with: {(data != null ? data.title : "NULL")}");

            _hintDataSO = data;
            UpdateUnreadState();
        }

        private void OnClick()
        {
            Log("Button clicked!");

            if (_hintDataSO == null)
            {
                Log("ERROR: _hintDataSO is null");
                return;
            }

            if (HintManager.Instance == null)
            {
                Log("ERROR: HintManager.Instance is null");
                return;
            }

            Log($"Opening panel for: {_hintDataSO.title}");
            HintManager.Instance.OpenPanel(_hintDataSO);
        }

        /// <summary>
        /// Cập nhật trạng thái unread dot dựa trên HintSaveManager.
        /// </summary>
        public void UpdateUnreadState()
        {
            if (_unreadDot == null || _hintDataSO == null) return;

            bool isRead = HintSaveManager.Instance != null && 
                          HintSaveManager.Instance.IsRead(_hintDataSO.title);
            _unreadDot.SetActive(!isRead);
        }

        public HintDataSO GetData() => _hintDataSO;
    }
}
