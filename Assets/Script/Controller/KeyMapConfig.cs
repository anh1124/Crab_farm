using UnityEngine;

namespace Script.Controller
{
    /// <summary>
    /// ScriptableObject lưu toàn bộ key binding cho game.
    /// Muốn đổi phím → chỉ cần mở asset này trong Inspector, không cần đụng code.
    /// Tạo asset: chuột phải trong Project → Create → Config → KeyMap Config
    /// </summary>
    [CreateAssetMenu(fileName = "KeyMapConfig", menuName = "Config/KeyMap Config")]
    public class KeyMapConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Phím chính di chuyển trái (WASD)")]
        public KeyCode moveLeft     = KeyCode.A;
        [Tooltip("Phím phụ di chuyển trái (Arrow Keys)")]
        public KeyCode moveLeftAlt  = KeyCode.LeftArrow;
        [Tooltip("Phím chính di chuyển phải (WASD)")]
        public KeyCode moveRight    = KeyCode.D;
        [Tooltip("Phím phụ di chuyển phải (Arrow Keys)")]
        public KeyCode moveRightAlt = KeyCode.RightArrow;

        [Header("Actions")]
        [Tooltip("Giữ để dùng coin / nạp coin vào công trình")]
        public KeyCode useCoin = KeyCode.F;

        [Header("Skills")]
        [Tooltip("Skill slot 0")]
        public KeyCode skill1 = KeyCode.Q;
        [Tooltip("Skill slot 1")]
        public KeyCode skill2 = KeyCode.E;
        [Tooltip("Skill slot 2")]
        public KeyCode skill3 = KeyCode.R;

        [Header("Menus & UI")]
        [Tooltip("Mở menu xây dựng")]
        public KeyCode openBuildMenu = KeyCode.B;
        [Tooltip("Mở menu tạm dừng")]
        public KeyCode openPauseMenu = KeyCode.Escape;
        [Tooltip("Chấp nhận / Đồng ý")]
        public KeyCode accept        = KeyCode.C;
        [Tooltip("Hủy / Đóng")]
        public KeyCode cancel        = KeyCode.V;
    }
}
