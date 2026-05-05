using UnityEngine;

namespace Script.FloatingText
{
    [CreateAssetMenu(fileName = "Style_", menuName = "FloatingText/Style")]
    public class FloatingTextStyleSO : ScriptableObject
    {
        [Header("Appearance")]
        [Tooltip("Màu sắc của chữ (ví dụ: đỏ cho sát thương, xanh cho hồi máu)")]
        public Color color = Color.white;

        [Tooltip("Kích thước font chữ của TextMeshPro")]
        public float fontSize = 5f;

        [Tooltip("Tiền tố đứng trước giá trị (ví dụ: '-', '+', hoặc các chữ như 'MISS', 'DODGE')")]
        public string prefix = "";

        [Tooltip("true = hiện cả prefix và số, false = chỉ hiện prefix")]
        public bool showValue = true;

        [Header("Spawn")]
        [Tooltip("Khoảng cách lệch so với vị trí gốc (Pivot) của đối tượng khi spawn")]
        public Vector3 defaultOffset = new Vector3(0f, 2f, 0f);

        [Tooltip("Độ ngẫu nhiên theo trục ngang để tránh các dòng chữ bị chồng khít lên nhau")]
        public float randomHorizontalRange = 0.25f;

        [Header("Motion")]
        [Tooltip("Thời gian tồn tại của chữ (giây) trước khi biến mất và quay về pool")]
        public float duration = 1.0f;

        [Tooltip("Tổng khoảng cách chữ sẽ bay lên phía trên")]
        public float moveDistance = 1.5f;

        [Tooltip("Đường cong điều khiển chuyển động bay lên theo thời gian (0 là bắt đầu, 1 là kết thúc)")]
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Đường cong điều khiển độ trong suốt (Alpha) để tạo hiệu ứng mờ dần (Fade out)")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Đường cong điều khiển kích thước (Scale) để tạo hiệu ứng phóng to/thu nhỏ lúc xuất hiện")]
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
    }
}
