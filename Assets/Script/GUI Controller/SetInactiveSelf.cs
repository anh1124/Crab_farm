using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI_Controller
{
    /// <summary>
    /// Script đơn giản để set inactive GameObject của chính nó.
    /// Dùng cho các nút cancel/close để đóng panel.
    /// </summary>
    public class SetInactiveSelf : MonoBehaviour
    {
        [Header("Button")]
        [Tooltip("Kéo button vào đây, khi ấn button sẽ đóng GameObject này")]
        public Button targetButton;

        private void Awake()
        {
            if (targetButton != null)
            {
                targetButton.onClick.AddListener(SetInactive);
            }
            else
            {
                Debug.LogWarning("[SetInactiveSelf] targetButton chưa được gán trong Inspector!");
            }
        }

        /// <summary>
        /// Set inactive GameObject của chính nó.
        /// Gọi method này từ button's onClick event.
        /// </summary>
        public void SetInactive()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (targetButton != null)
            {
                targetButton.onClick.RemoveListener(SetInactive);
            }
        }
    }
}
