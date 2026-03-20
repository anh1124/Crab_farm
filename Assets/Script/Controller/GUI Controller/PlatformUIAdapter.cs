using UnityEngine;

namespace Script.Controller.GUI_Controller
{
    /// <summary>
    /// Xử lý UI theo nền tảng / trạng thái input runtime.
    ///
    /// Logic mới (theo yêu cầu):
    ///   - Mobile controls (nút trái/phải, coin) LUÔN hiện — UI hoạt động trên mọi platform.
    ///   - Keyboard cũng hoạt động song song trên mọi platform.
    ///   - KHÔNG ẩn UI dựa theo platform build.
    ///   - (Optional) Gắn thêm vào đây nếu muốn show/hide UI theo runtime sau này.
    ///
    /// Setup:
    ///   - Gắn script này vào Canvas hoặc PlayerHUD GameObject.
    /// </summary>
    public class PlatformUIAdapter : MonoBehaviour
    {
        [Header("Mobile-Only Controls")]
        [Tooltip("Container chứa nút Left + Right. Luôn bật — keyboard cũng chạy song song.")]
        public GameObject mobileMovementControls;
        [Tooltip("Nút dùng coin. Luôn bật — phím F cũng dùng được đồng thời.")]
        public GameObject coinButtonObject;

        private void Start()
        {
            // Đảm bảo mobile controls luôn được bật khi bắt đầu.
            // Keyboard và UI hoạt động song song — không ẩn theo platform.
            if (mobileMovementControls != null) mobileMovementControls.SetActive(true);
            if (coinButtonObject != null)       coinButtonObject.SetActive(true);
        }
    }
}
