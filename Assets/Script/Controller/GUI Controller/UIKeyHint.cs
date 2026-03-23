using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Script.Controller;

namespace Script.Controller.GUI_Controller
{
    /// <summary>
    /// Gắn script này vào các Button UI (hoặc prefab như Confirm/Cancel).
    /// Nó sẽ tự động cập nhật text hiển thị phím (ví dụ "C", "V") 
    /// theo KeyMapConfig và tự động bấm nút nếu người dùng nhấn phím đó.
    /// </summary>
    public class UIKeyHint : MonoBehaviour
    {
        public enum KeyAction
        {
            Skill1, Skill2, Skill3,
            OpenBuildMenu, OpenPauseMenu,
            Accept, Cancel, UseCoin,
            MoveLeft, MoveRight
        }

        [Header("Mapping")]
        [Tooltip("Hành động mà nút này đại diện")]
        public KeyAction action;
        
        [Header("Visuals (Tùy chọn)")]
        [Tooltip("GameObject con chứa text/khung nền (nếu muốn ẩn/hiện tùy ngữ cảnh)")]
        public GameObject hintContainer;
        [Tooltip("TextMeshPro UI (nếu dùng)")]
        public TMP_Text tmpText;

        [Header("Interaction (Tùy chọn)")]
        [Tooltip("Nút sẽ được Invoke() khi người dùng bấm phím. Nếu để trống sẽ cố tự tìm trên GameObject này.")]
        public Button targetButton;

        private KeyCode mappedKey = KeyCode.None;

        private void Awake()
        {
            if (targetButton == null)
            {
                targetButton = GetComponent<Button>();
            }
        }

        private void Start()
        {
            SetupKey();
        }

        private void OnEnable()
        {
            // Cập nhật lại key lỡ người chơi đổi phím lúc đang chơi
            SetupKey();
        }

        private void SetupKey()
        {
            if (InputManager.Instance == null || InputManager.Instance.KeyMap == null) return;
            var km = InputManager.Instance.KeyMap;

            mappedKey = action switch
            {
                KeyAction.Skill1 => km.skill1,
                KeyAction.Skill2 => km.skill2,
                KeyAction.Skill3 => km.skill3,
                KeyAction.OpenBuildMenu => km.openBuildMenu,
                KeyAction.OpenPauseMenu => km.openPauseMenu,
                KeyAction.Accept => km.accept,
                KeyAction.Cancel => km.cancel,
                KeyAction.UseCoin => km.useCoin,
                KeyAction.MoveLeft => km.moveLeft,
                KeyAction.MoveRight => km.moveRight,
                _ => KeyCode.None
            };

            string keyName = KeyCodeToLabel(mappedKey);
            
            if (tmpText != null) tmpText.text = keyName;

            // Mặc định ẩn container cho đến khi có input từ bàn phím
            if (hintContainer != null) hintContainer.SetActive(false);
        }

        private void Update()
        {
            // 1. Quản lý hiển thị Visual
            if (hintContainer != null)
            {
                bool shouldShow = InputManager.Instance != null && InputManager.Instance.AnyKeyUsed;
                if (hintContainer.activeSelf != shouldShow)
                {
                    hintContainer.SetActive(shouldShow);
                }
            }

            // 2. Lắng nghe Input phím bấm
            // Script này chỉ Update khi GameObject này đang được active trên màn hình 
            // nên không sợ bị bấm nhầm lúc tắt Menu.
            if (mappedKey != KeyCode.None && targetButton != null && targetButton.interactable)
            {
                if (Input.GetKeyDown(mappedKey))
                {
                    targetButton.onClick.Invoke();
                    
                    // Nếu bấm phím này thì chắc chắn là đang dùng Input, update cờ ở Manager
                    if (InputManager.Instance != null && !InputManager.Instance.AnyKeyUsed)
                    {
                        // Reflecting internal flag in InputManager, wait, AnyKeyUsed property is private set.
                        // However, InputManager Update will catch the key press anyway.
                    }
                }
            }
        }

        /// <summary>
        /// Rút gọn tên phím (ví dụ: Alpha1 -> 1)
        /// </summary>
        private string KeyCodeToLabel(KeyCode key)
        {
            if (key == KeyCode.None) return "";
            string name = key.ToString();
            if (name.StartsWith("Alpha")) return name.Substring(5);
            return name;
        }
    }
}
