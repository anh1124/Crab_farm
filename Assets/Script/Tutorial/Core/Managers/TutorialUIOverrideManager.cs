using UnityEngine;
using System.Collections.Generic;

namespace Script.Tutorial.Core.Managers
{
    /// <summary>
    /// Singleton lưu trữ trạng thái mong muốn (desired state) của từng UI element trong Tutorial.
    /// GuiCanvasManager sẽ hỏi class này trước khi SetActive bất kỳ panel nào.
    /// </summary>
    public class TutorialUIOverrideManager : MonoBehaviour
    {
        public static TutorialUIOverrideManager Instance { get; private set; }

        private readonly Dictionary<string, bool> _desiredStates = new Dictionary<string, bool>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Cập nhật trạng thái mong muốn của một element.
        /// Gọi bởi TutorialUIStateManager mỗi khi SetActive được thực thi.
        /// </summary>
        public void SetDesiredState(string elementName, bool isActive)
        {
            _desiredStates[elementName] = isActive;
            Debug.Log($"[TutorialUIOverrideManager] Đã lưu trạng thái override cho '{elementName}' = {isActive}");
        }

        /// <summary>
        /// Lấy trạng thái mong muốn của một element.
        /// Nếu không có trong map, mặc định trả về true (bình thường).
        /// </summary>
        public bool GetDesiredState(string elementName)
        {
            return _desiredStates.TryGetValue(elementName, out bool state) ? state : true;
        }

        /// <summary>
        /// Kiểm tra Tutorial có đang override element này không.
        /// </summary>
        public bool HasOverride(string elementName)
        {
            return _desiredStates.ContainsKey(elementName);
        }

        /// <summary>
        /// Xóa toàn bộ trạng thái override (khi Tutorial kết thúc).
        /// </summary>
        public void ClearAllOverrides()
        {
            Debug.Log("[TutorialUIOverrideManager] Xóa toàn bộ trạng thái override.");
            _desiredStates.Clear();
        }

        /// <summary>
        /// Áp dụng lại tất cả trạng thái mong muốn lên đối tượng thực (gọi sau khi GuiCanvasManager restore UI).
        /// </summary>
        public void ReapplyOverrides(Dictionary<string, GameObject> uiElements, Dictionary<string, MonoBehaviour> scriptElements)
        {
            Debug.Log($"[TutorialUIOverrideManager] Bắt đầu ReapplyOverrides. Số override đang có: {_desiredStates.Count}");
            foreach (var kvp in _desiredStates)
            {
                if (uiElements != null && uiElements.TryGetValue(kvp.Key, out GameObject go))
                {
                    if (go != null)
                    {
                        bool beforeState = go.activeSelf;
                        go.SetActive(kvp.Value);
                        Debug.Log($"[TutorialUIOverrideManager] Reapply UI '{kvp.Key}': state trước đó = {beforeState}, state sau khi ép = {kvp.Value}");
                    }
                }
                else if (scriptElements != null && scriptElements.TryGetValue(kvp.Key, out MonoBehaviour script))
                {
                    if (script != null)
                    {
                        bool beforeState = script.enabled;
                        script.enabled = kvp.Value;
                        Debug.Log($"[TutorialUIOverrideManager] Reapply Script '{kvp.Key}': state trước đó = {beforeState}, state sau khi ép = {kvp.Value}");
                    }
                }
            }
        }
    }
}
