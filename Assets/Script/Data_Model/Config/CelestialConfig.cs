using UnityEngine;

namespace Script.Data_Model
{
    [CreateAssetMenu(fileName = "CelestialConfig", menuName = "Config/Celestial Config")]
    public class CelestialConfig : ScriptableObject
    {
        [Header("Sun Timing")]
        [Tooltip("Giờ mặt trời xuất hiện tại chân trời hướng Đông")]
        public float sunRiseHour = 6f;
        
        [Tooltip("Giờ mặt trời lặn tại chân trời hướng Tây")]
        public float sunSetHour = 19f;

        [Header("Moon Timing")]
        [Tooltip("Giờ mặt trăng bắt đầu xuất hiện")]
        public float moonRiseHour = 17.5f;
        
        [Tooltip("Giờ mặt trăng biến mất")]
        public float moonSetHour = 6f;
        
        [Tooltip("Độ trong suốt của mặt trăng ngay khi mới xuất hiện (lúc còn mờ)")]
        [Range(0f, 1f)] 
        public float moonDuskAlpha = 0.3f;
        
        [Tooltip("Thời lượng fade-in của mặt trăng (đơn vị: giờ game)")]
        public float moonFadeInHours = 1f;

        [Header("Sky Colors")]
        [Tooltip("Màu lúc 06:00 — bình minh")]
        public Color colorEarlyMorning = new Color(1.0f, 0.72f, 0.42f);
        
        [Tooltip("Màu lúc 09:00 — sáng")]
        public Color colorMorning = new Color(0.55f, 0.78f, 1.0f);
        
        [Tooltip("Màu lúc 12:00 — trưa")]
        public Color colorNoon = new Color(0.75f, 0.90f, 1.0f);
        
        [Tooltip("Màu lúc 15:00 — chiều")]
        public Color colorAfternoon = new Color(1.0f, 0.92f, 0.70f);
        
        [Tooltip("Màu lúc 18:00 — hoàng hôn")]
        public Color colorDusk = new Color(1.0f, 0.42f, 0.15f);
        
        [Tooltip("Màu ban đêm")]
        public Color colorNight = new Color(0.04f, 0.05f, 0.18f);

        [Header("Grounding")]
        [Tooltip("Nếu bật, bầu trời sẽ neo theo đáy màn hình thay vì tâm.")]
        public bool lockToCameraBottom = true;
        
        [Tooltip("Độ lệch Y so với tính toán.")]
        public float yOffset = 0f;
    }
}
