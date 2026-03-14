using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    /// <summary>
    /// Quản lý toàn bộ bầu trời: quỹ đạo mặt trời/mặt trăng theo waypoint,
    /// hiệu ứng xuất hiện cùng lúc lúc hoàng hôn, và chuyển màu background mượt mà.
    ///
    /// TIMELINE:
    ///   Mặt trời  : 06:00 → 19:00 (waypoint[0] → waypoint[last])
    ///   Mặt trăng : 17:30 → 06:00 (waypoint[0] → middle@midnight → waypoint[last])
    ///   Giao nhau : 17:30 → 19:00 (moon fade in, cả hai cùng hiện)
    /// </summary>
    public class CelestialManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector F ields 
        // ──────────────────────────────────────────────────────────────────────────

        [Header("References")]
        public SpriteRenderer sunRenderer;
        public SpriteRenderer moonRenderer;
        public SpriteRenderer backgroundRenderer;
        
        [Header("Follow Targets")]
        [Tooltip("Transform mục tiêu để bầu trời (Background) bám theo.")]
        public Transform backgroundFollowTarget;
        [Tooltip("Transform mục tiêu để Mặt Trời/Mặt Trăng bám theo.")]
        public Transform celestialFollowTarget;

        [Header("Waypoints — trái → phải (con của Sky object)")]
        [Tooltip("Danh sách Transform làm điểm di chuyển. Index 0 = chân trời trái, cuối = chân trời phải.")]
        public List<Transform> waypoints = new List<Transform>();

        [Header("Sun Timing")]
        [Tooltip("Giờ mặt trời xuất hiện tại waypoint[0]")]
        public float sunRiseHour = 6f;
        [Tooltip("Giờ mặt trời biến mất tại waypoint[last]")]
        public float sunSetHour = 19f;

        [Header("Moon Timing")]
        [Tooltip("Giờ mặt trăng bắt đầu xuất hiện (cùng lúc với mặt trời)")]
        public float moonRiseHour = 17.5f;
        [Tooltip("Giờ mặt trăng biến mất — nên = sunRiseHour để không có khoảng trống")]
        public float moonSetHour = 6f;
        [Tooltip("Alpha của mặt trăng ngay khi mới xuất hiện (lúc còn mờ chiều tối)")]
        [Range(0f, 1f)] public float moonDuskAlpha = 0.3f;
        [Tooltip("Thời lượng fade-in của mặt trăng (đơn vị: giờ game)")]
        public float moonFadeInHours = 1f;

        [Header("Sky Colors — chọn màu theo giờ, background tự lerp")]
        [Tooltip("Màu lúc 06:00 — bình minh")]
        public Color colorEarlyMorning = new Color(1.0f, 0.72f, 0.42f);
        [Tooltip("Màu lúc 09:00 — sáng")]
        public Color colorMorning      = new Color(0.55f, 0.78f, 1.0f);
        [Tooltip("Màu lúc 12:00 — trưa")]
        public Color colorNoon         = new Color(0.75f, 0.90f, 1.0f);
        [Tooltip("Màu lúc 15:00 — chiều")]
        public Color colorAfternoon    = new Color(1.0f, 0.92f, 0.70f);
        [Tooltip("Màu lúc 18:00 — hoàng hôn")]
        public Color colorDusk         = new Color(1.0f, 0.42f, 0.15f);
        [Tooltip("Màu ban đêm")]
        public Color colorNight        = new Color(0.04f, 0.05f, 0.18f);

        [Header("Grounding")]
        [Tooltip("Nếu bật, Sky root (Y=0) sẽ luôn ghim vào đáy camera. Giúp đường chân trời ổn định khi camera di chuyển dọc.")]
        public bool lockToCameraBottom = true;
        [Tooltip("Độ lệch Y so với đáy màn hình.")]
        public float yOffset = 0f;

        // ──────────────────────────────────────────────────────────────────────────
        // Private State
        private Camera _bgCamera;
        private Camera _celestialCamera;

        // Container object cho waypoints, sun, moon để di chuyển độc lập với background
        private Transform _celestialsRoot;

        /// <summary>Tiến trình ngày mượt mà (0.0 → 1.0), được drive bởi Time.deltaTime.</summary>
        private float _smoothDayProgress;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (backgroundFollowTarget != null) _bgCamera = backgroundFollowTarget.GetComponent<Camera>();
            if (celestialFollowTarget != null) _celestialCamera = celestialFollowTarget.GetComponent<Camera>();

            // Tạo một root ảo để chứa Sun, Moon và Waypoints để chúng có thể di chuyển cùng nhau
            _celestialsRoot = new GameObject("CelestialsRoot").transform;
            _celestialsRoot.SetParent(transform);
            
            if (sunRenderer != null) sunRenderer.transform.SetParent(_celestialsRoot, true);
            if (moonRenderer != null) moonRenderer.transform.SetParent(_celestialsRoot, true);
            if (waypoints.Count > 0 && waypoints[0] != null && waypoints[0].parent != null)
            {
                waypoints[0].parent.SetParent(_celestialsRoot, true);
            }
        }

        private void Start()
        {
            if (GameTimeManager.Instance == null) return;

            // Khởi tạo smooth progress từ giá trị thực tế để tránh lerp từ 0
            _smoothDayProgress = GetRawDayProgress();

            // Đặt trạng thái ban đầu ngay lập tức (không chờ frame đầu)
            float startHour = _smoothDayProgress * 24f;
            UpdateSkyColor(startHour);
            UpdateSun(startHour);
            UpdateMoon(startHour);
        }

        private void Update()
        {
            if (GameTimeManager.Instance == null) return;

            AdvanceSmoothProgress();

            float currentHour = _smoothDayProgress * 24f;
            UpdateSun(currentHour);
            UpdateMoon(currentHour);
            UpdateSkyColor(currentHour);
        }

        private void LateUpdate()
        {
            // Update Background Root
            if (backgroundFollowTarget != null && backgroundRenderer != null)
            {
                float bgX = backgroundFollowTarget.position.x;
                float bgY = backgroundFollowTarget.position.y;

                if (lockToCameraBottom)
                {
                    if (_bgCamera != null) bgY = backgroundFollowTarget.position.y - _bgCamera.orthographicSize + yOffset;
                    else bgY = backgroundFollowTarget.position.y + yOffset;
                }
                
                // Tránh set position nếu Target chính là Background (để khỏi xung đột tự set)
                if (backgroundFollowTarget != backgroundRenderer.transform)
                {
                    backgroundRenderer.transform.position = new Vector3(bgX, bgY, backgroundRenderer.transform.position.z);
                }
            }

            // Update Celestials Root (Sun, Moon, Waypoints)
            if (celestialFollowTarget != null && _celestialsRoot != null)
            {
                float celX = celestialFollowTarget.position.x;
                float celY = celestialFollowTarget.position.y;

                if (lockToCameraBottom)
                {
                    if (_celestialCamera != null) celY = celestialFollowTarget.position.y - _celestialCamera.orthographicSize + yOffset;
                    else celY = celestialFollowTarget.position.y + yOffset;
                }
                _celestialsRoot.position = new Vector3(celX, celY, _celestialsRoot.position.z);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Smooth Progress
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Advance smooth progress theo thời gian thực thay vì nhảy theo phút game.
        /// Snap ngay khi có jump lớn (load save, context menu set time).
        /// </summary>
        private void AdvanceSmoothProgress()
        {
            float rawProgress = GetRawDayProgress();

            // Tính khoảng cách có tính wrap (ví dụ: 0.99 vs 0.01 = 0.02, không phải 0.98)
            float diff = rawProgress - _smoothDayProgress;
            if (diff > 0.5f)  diff -= 1f;
            if (diff < -0.5f) diff += 1f;

            // Snap nếu lệch quá 5 phút game (do SetGameTime jump)
            const float snapThreshold = 5f / 1440f;
            if (Mathf.Abs(diff) > snapThreshold)
            {
                _smoothDayProgress = rawProgress;
                return;
            }

            // Advance bình thường: 1 ngày game = realSecondsPerGameDay giây thực
            float progressPerSecond = 1f / GameTimeManager.Instance.SecondsPerDay;
            _smoothDayProgress += progressPerSecond * Time.deltaTime;
            _smoothDayProgress %= 1f;
        }

        private float GetRawDayProgress()
        {
            return (GameTimeManager.Instance.GetTotalMinutes % 1440) / 1440f;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Sun
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdateSun(float currentHour)
        {
            if (sunRenderer == null || waypoints.Count < 2) return;

            bool visible = currentHour >= sunRiseHour && currentHour < sunSetHour;
            sunRenderer.enabled = visible;
            if (!visible) return;

            float t = Mathf.InverseLerp(sunRiseHour, sunSetHour, currentHour);
            sunRenderer.transform.position = GetPathPosition(t);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Moon
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdateMoon(float currentHour)
        {
            if (moonRenderer == null || waypoints.Count < 2) return;

            float moonT = GetMoonT(currentHour);
            bool visible = moonT >= 0f;
            moonRenderer.enabled = visible;
            if (!visible) return;

            moonRenderer.transform.position = GetPathPosition(moonT);

            // Fade in lúc vừa xuất hiện (coexist với mặt trời)
            Color c = moonRenderer.color;
            c.a = GetMoonAlpha(currentHour);
            moonRenderer.color = c;
        }

        /// <summary>
        /// Trả về t (0→1) trên waypoint path cho mặt trăng.
        /// Đảm bảo t=0.5 (middle) đúng lúc nửa đêm (0:00).
        /// Trả về -1 nếu mặt trăng không hiển thị.
        ///
        /// Chia làm 2 phase:
        ///   Phase 1: moonRiseHour → 24:00  →  t: 0.0 → 0.5
        ///   Phase 2: 00:00 → moonSetHour   →  t: 0.5 → 1.0
        /// </summary>
        private float GetMoonT(float currentHour)
        {
            if (currentHour >= moonRiseHour)
            {
                // Phase 1: rising (chiều → nửa đêm)
                return Mathf.InverseLerp(moonRiseHour, 24f, currentHour) * 0.5f;
            }
            else if (currentHour < moonSetHour)
            {
                // Phase 2: setting (nửa đêm → sáng)
                return 0.5f + Mathf.InverseLerp(0f, moonSetHour, currentHour) * 0.5f;
            }

            return -1f; // không hiển thị (khoảng moonSetHour → moonRiseHour)
        }

        private float GetMoonAlpha(float currentHour)
        {
            // Fade in từ moonDuskAlpha → 1.0 trong khoảng moonFadeInHours
            if (currentHour >= moonRiseHour && currentHour < moonRiseHour + moonFadeInHours)
            {
                float fadeProgress = Mathf.InverseLerp(moonRiseHour, moonRiseHour + moonFadeInHours, currentHour);
                return Mathf.Lerp(moonDuskAlpha, 1f, fadeProgress);
            }
            return 1f;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Waypoint Path
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Nội suy tuyến tính giữa các waypoint.
        /// t=0 → waypoints[0], t=1 → waypoints[last].
        /// </summary>
        private Vector3 GetPathPosition(float t)
        {
            t = Mathf.Clamp01(t);
            int count = waypoints.Count;

            if (count == 0) return transform.position;
            if (count == 1) return waypoints[0].position;

            float scaledT = t * (count - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaledT), 0, count - 2);
            float localT = scaledT - index;

            return Vector3.Lerp(waypoints[index].position, waypoints[index + 1].position, localT);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Sky Color
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdateSkyColor(float currentHour)
        {
            if (backgroundRenderer == null) return;
            backgroundRenderer.color = SampleSkyColor(currentHour);
        }

        /// <summary>
        /// Lerp màu bầu trời theo giờ. Không có sprite swap, chỉ đổi màu SpriteRenderer.
        /// Background cần là sprite màu trắng để hiển thị màu tint đúng.
        /// </summary>
        private Color SampleSkyColor(float hour)
        {
            // Key: (giờ bắt đầu, giờ kết thúc, màu đầu, màu cuối)
            if (hour < 6f)
            {
                // Pre-dawn: night → earlyMorning (bắt đầu transition từ 4h)
                return Color.Lerp(colorNight, colorEarlyMorning, Mathf.InverseLerp(4f, 6f, hour));
            }
            if (hour < 9f)   return Color.Lerp(colorEarlyMorning, colorMorning,   Mathf.InverseLerp(6f,  9f,  hour));
            if (hour < 12f)  return Color.Lerp(colorMorning,      colorNoon,      Mathf.InverseLerp(9f,  12f, hour));
            if (hour < 15f)  return Color.Lerp(colorNoon,         colorAfternoon, Mathf.InverseLerp(12f, 15f, hour));
            if (hour < 18f)  return Color.Lerp(colorAfternoon,    colorDusk,      Mathf.InverseLerp(15f, 18f, hour));
            if (hour < 21f)  return Color.Lerp(colorDusk,         colorNight,     Mathf.InverseLerp(18f, 21f, hour));

            return colorNight;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Editor Gizmos — hiển thị path và các waypoint trong Scene View
        // ──────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            // Vẽ đường path
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // Vẽ các waypoint sphere
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.color = (i == 0 || i == waypoints.Count - 1) ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
            }

            // Preview vị trí sun/moon tại thời gian hiện tại (nếu đang play)
            if (!Application.isPlaying || GameTimeManager.Instance == null) return;

            float raw = (GameTimeManager.Instance.GetTotalMinutes % 1440) / 1440f;
            float hour = raw * 24f;

            if (hour >= sunRiseHour && hour < sunSetHour)
            {
                float t = Mathf.InverseLerp(sunRiseHour, sunSetHour, hour);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(GetPathPosition(t), 0.4f);
            }

            float moonT = GetMoonT(hour);
            if (moonT >= 0f)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(GetPathPosition(moonT), 0.35f);
            }
        }
#endif
    }
}
