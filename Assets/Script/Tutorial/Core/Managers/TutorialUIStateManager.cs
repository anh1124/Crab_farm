using UnityEngine;
using System.Collections.Generic;

namespace Script.Tutorial.Core.Managers
{
    /// <summary>
    /// Quản lý việc bật/tắt các UI element và chức năng không cần thiết trong quá trình Tutorial
    /// </summary>
    public class TutorialUIStateManager : MonoBehaviour
    {
        [Header("UI Elements (GameObjects)")]
        [SerializeField] private GameObject skillButton;
        [SerializeField] private GameObject useCoinButton;
        [SerializeField] private GameObject healBar;
        [SerializeField] private GameObject manaBar;
        [SerializeField] private GameObject buildMenuButton;
        [SerializeField] private GameObject coinStockBar;
        
        [Header("Player Scripts")]
        [Tooltip("Script quản lý skill của player cần bị vô hiệu hóa")]
        [SerializeField] private MonoBehaviour playerSkillManager; 

        // Maps lưu trữ các đối tượng
        private Dictionary<string, GameObject> _uiElements;
        private Dictionary<string, MonoBehaviour> _scriptElements;

        private void Awake()
        {
            // Khởi tạo map chứa các tham chiếu
            _uiElements = new Dictionary<string, GameObject>()
            {
                { "skillButton", skillButton },
                { "useCoinButton", useCoinButton },
                { "healBar", healBar },
                { "manaBar", manaBar },
                { "buildMenuButton", buildMenuButton },
                { "coinStockBar", coinStockBar }
            };
            
            _scriptElements = new Dictionary<string, MonoBehaviour>()
            {
                { "playerSkillManager", playerSkillManager }
            };
        }

        /// <summary>
        /// Bật/tắt một element cụ thể thông qua tên chuỗi (string)
        /// </summary>
        /// <param name="elementName">Tên của biến (vd: "skillButton", "playerSkillManager")</param>
        /// <param name="isActive">true để hiển thị/bật, false để ẩn/tắt</param>
        public void SetActiveElement(string elementName, bool isActive)
        {
            if (_uiElements != null && _uiElements.TryGetValue(elementName, out GameObject go))
            {
                if (go != null) go.SetActive(isActive);
            }
            else if (_scriptElements != null && _scriptElements.TryGetValue(elementName, out MonoBehaviour script))
            {
                if (script != null) script.enabled = isActive;
            }
            else
            {
                Debug.LogWarning($"[TutorialUIStateManager] Element not found in map: {elementName}");
            }
        }

        /// <summary>
        /// Bật/tắt tất cả các phần tử liên quan cùng lúc
        /// </summary>
        /// <param name="isActive">true để hiển thị và bật chức năng, false để ẩn và tắt</param>
        public void SetActiveAll(bool isActive)
        {
            // Tắt/bật GameObjects
            if (_uiElements != null)
            {
                foreach (var go in _uiElements.Values)
                {
                    if (go != null) go.SetActive(isActive);
                }
            }

            // Tắt/bật Scripts
            if (_scriptElements != null)
            {
                foreach (var script in _scriptElements.Values)
                {
                    if (script != null) script.enabled = isActive;
                }
            }
        }

        // --- Các hàm tiện ích ---

        /// <summary>
        /// Gọi hàm này khi bắt đầu Tutorial để ẩn mọi thứ
        /// </summary>
        public void DisableForTutorial()
        {
            SetActiveAll(false);
            Debug.Log("[TutorialUIStateManager] Disabled unnecessary UI and scripts for tutorial.");
        }

        /// <summary>
        /// Gọi hàm này khi kết thúc Tutorial để trả lại trạng thái bình thường
        /// </summary>
        public void EnableAfterTutorial()
        {
            SetActiveAll(true);
            Debug.Log("[TutorialUIStateManager] Enabled UI and scripts after tutorial.");
        }
    }
}
