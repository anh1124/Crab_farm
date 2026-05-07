using UnityEngine;
using System.Collections.Generic;

namespace Script.Tutorial.Core.Managers
{
    /// <summary>
    /// Quản lý việc bật/tắt các UI element trong Tutorial.
    /// Mọi thay đổi trạng thái đều được ghi vào TutorialUIOverrideManager để không bị
    /// GuiCanvasManager ghi đè khi ResumeGame() được gọi.
    /// </summary>
    public class TutorialUIStateManager : MonoBehaviour
    {
        [Header("UI Elements (GameObjects)")]
        [SerializeField] private GameObject useCoinButton;
        [SerializeField] private GameObject healBar;
        [SerializeField] private GameObject manaBar;
        [SerializeField] private GameObject buildMenuButton;
        [SerializeField] private GameObject coinStockBar;
        
        [Header("Player Scripts")]
        [Tooltip("Script quản lý skill của player. Sẽ tự động lấy từ PlayerController.Instance lúc runtime.")]
        // Bỏ [SerializeField] để lấy động
        private MonoBehaviour playerSkillManager; 

        // Maps lưu trữ các đối tượng - exposed để OverrideManager có thể ReapplyOverrides
        private Dictionary<string, GameObject> _uiElements;
        private Dictionary<string, MonoBehaviour> _scriptElements;

        // Expose để TutorialUIOverrideManager.ReapplyOverrides() dùng
        public Dictionary<string, GameObject> UIElements => _uiElements;
        public Dictionary<string, MonoBehaviour> ScriptElements => _scriptElements;

        private void Awake()
        {
            _uiElements = new Dictionary<string, GameObject>()
            {
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

            ValidateInspectorAssignments();
        }

        private void ValidateInspectorAssignments()
        {
            foreach (var kvp in _uiElements)
            {
                if (kvp.Value == null)
                    Debug.LogError($"[TutorialUIStateManager] LỖI: Trường UI '{kvp.Key}' chưa được gán trong Inspector của {gameObject.name}");
            }

            foreach (var kvp in _scriptElements)
            {
                if (kvp.Value == null)
                    Debug.LogError($"[TutorialUIStateManager] LỖI: Trường Script '{kvp.Key}' chưa được gán trong Inspector của {gameObject.name}");
            }
        }

        /// <summary>
        /// Bật/tắt một element cụ thể thông qua tên chuỗi (string).
        /// Trạng thái được ghi vào TutorialUIOverrideManager để chống bị ghi đè bởi GuiCanvasManager.
        /// </summary>
        public void SetActiveElement(string elementName, bool isActive)
        {
            bool found = false;

            if (_uiElements != null && _uiElements.TryGetValue(elementName, out GameObject go))
            {
                found = true;
                if (go != null)
                {
                    go.SetActive(isActive);
                    TutorialUIOverrideManager.Instance?.SetDesiredState(elementName, isActive);
                }
                else Debug.LogError($"[TutorialUIStateManager] LỖI: Element UI '{elementName}' được gọi nhưng đối tượng trong Map lại bị Null!");
            }
            else if (_scriptElements != null && _scriptElements.ContainsKey(elementName))
            {
                found = true;
                if (_scriptElements[elementName] == null && elementName == "playerSkillManager")
                {
                    if (Script.Controller.PlayerController.PlayerController.Instance != null)
                    {
                        _scriptElements["playerSkillManager"] = Script.Controller.PlayerController.PlayerController.Instance.SkillManager;
                    }
                }
                
                MonoBehaviour script = _scriptElements[elementName];
                if (script != null)
                {
                    script.enabled = isActive;
                    TutorialUIOverrideManager.Instance?.SetDesiredState(elementName, isActive);
                }
                else Debug.LogError($"[TutorialUIStateManager] LỖI: Element Script '{elementName}' được gọi nhưng đối tượng trong Map lại bị Null!");
            }

            if (!found)
                Debug.LogError($"[TutorialUIStateManager] LỖI: Truyền sai tên '{elementName}'. Không tìm thấy tên này trong danh sách quản lý!");
        }

        /// <summary>
        /// Bật/tắt tất cả các phần tử. Ghi toàn bộ trạng thái vào OverrideManager.
        /// </summary>
        public void SetActiveAll(bool isActive)
        {
            Debug.Log($"[TutorialUIStateManager] Bắt đầu gọi SetActiveAll({isActive})...");

            if (_uiElements != null)
            {
                foreach (var kvp in _uiElements)
                {
                    if (kvp.Value != null)
                    {
                        bool beforeState = kvp.Value.activeSelf;
                        kvp.Value.SetActive(isActive);
                        Debug.Log($"[TutorialUIStateManager] Lệnh gọi UI '{kvp.Key}' (Trạng thái cũ: {beforeState} -> Mới: {isActive})");
                        TutorialUIOverrideManager.Instance?.SetDesiredState(kvp.Key, isActive);
                    }
                    else Debug.LogError($"[TutorialUIStateManager] SetActiveAll: Element UI '{kvp.Key}' bị Null!");
                }
            }

            if (_scriptElements != null)
            {
                foreach (var kvp in _scriptElements)
                {
                    if (kvp.Value != null)
                    {
                        bool beforeState = kvp.Value.enabled;
                        kvp.Value.enabled = isActive;
                        Debug.Log($"[TutorialUIStateManager] Lệnh gọi Script '{kvp.Key}' (Trạng thái cũ: {beforeState} -> Mới: {isActive})");
                        TutorialUIOverrideManager.Instance?.SetDesiredState(kvp.Key, isActive);
                    }
                    else Debug.LogWarning($"[TutorialUIStateManager] SetActiveAll: Element Script '{kvp.Key}' bị Null! Có thể Player chưa spawn.");
                }
            }
        }

        /// <summary>
        /// Áp dụng lại trạng thái đã lưu từ OverrideManager. 
        /// Gọi sau khi GuiCanvasManager.ResumeGame() restore UI để sửa lại những gì bị ghi đè.
        /// </summary>
        public void ReapplyTutorialStates()
        {
            TutorialUIOverrideManager.Instance?.ReapplyOverrides(_uiElements, _scriptElements);
        }

        public void DisableForTutorial() => SetActiveAll(false);
        public void EnableAfterTutorial()
        {
            SetActiveAll(true);
            TutorialUIOverrideManager.Instance?.ClearAllOverrides();
        }
    }
}
