Tóm tắt từng thay đổi
GameTimeManager.cs — Chỉ thêm 1 dòng
csharppublic float SecondsPerDay => realSecondsPerGameDay;
CelestialManager cần giá trị này để biết tốc độ di chuyển thực tế. Conflict IsDaytime vs NIGHT_HOUR đã được resolve trong file bạn paste — không cần sửa thêm.

CelestialManager.cs — Viết lại hoàn toàn
Req 3 — Smooth movement: Thay vì đọc totalMinutes (nhảy từng phút), dùng Time.deltaTime để tự advance _smoothDayProgress mỗi frame. Khi SetGameTime() jump lớn hơn 5 phút game thì snap thẳng, không lag.
Req 1 & 2 — Coexist + luôn có ít nhất 1 cái: Sun hiện 6:00→19:00, Moon hiện 17:30→6:00 (ngày hôm sau). Overlap 1.5 giờ từ 17:30→19:00, moon fade in từ alpha 0.3→1.0 trong khoảng đó. Không có khoảng trống nào trên bầu trời.
Req 4 — Color thay sprite: Background giờ chỉ cần 1 sprite trắng, SpriteRenderer.color tự lerp qua 6 màu chọn trong inspector theo giờ game. Không còn 5 sprite day backgrounds.
Req 5 — Waypoint path: List Transform làm điểm di chuyển, Sun đi từ WP[0]→WP[last] trong 6:00→19:00. Moon đảm bảo t=0.5 (middle waypoint) đúng lúc 0:00, kết thúc tại WP[last] lúc 6:00.

CelestialSetupTool.cs — Cập nhật
Tạo đúng 5 waypoint theo hình arc trong Scene View, bỏ background sprite setup (giờ chỉ cần sprite trắng), thêm Undo support và checklist log sau khi tạo.




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
        // Inspector Fields
        // ──────────────────────────────────────────────────────────────────────────

        [Header("References")]
        public SpriteRenderer sunRenderer;
        public SpriteRenderer moonRenderer;
        public SpriteRenderer backgroundRenderer;
        public Camera mainCamera;

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

        // ──────────────────────────────────────────────────────────────────────────
        // Private State
        // ──────────────────────────────────────────────────────────────────────────

        private Camera _camera;

        /// <summary>Tiến trình ngày mượt mà (0.0 → 1.0), được drive bởi Time.deltaTime.</summary>
        private float _smoothDayProgress;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _camera = mainCamera != null ? mainCamera : Camera.main;
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
            if (GameTimeManager.Instance == null || _camera == null) return;

            AdvanceSmoothProgress();

            float currentHour = _smoothDayProgress * 24f;
            UpdateSun(currentHour);
            UpdateMoon(currentHour);
            UpdateSkyColor(currentHour);
        }

        private void LateUpdate()
        {
            // Sky object theo camera theo trục X, giữ nguyên Y và Z của chính nó.
            // Waypoints là con của Sky nên tự động đi theo.
            if (_camera == null) return;
            transform.position = new Vector3(
                _camera.transform.position.x,
                transform.position.y,
                transform.position.z
            );
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






// ══════════════════════════════════════════════════════════════════════════════
// THÊM VÀO GameTimeManager.cs — trong region Save/Load Integration
// (đặt cạnh public int GetTotalMinutes)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Số giây thực tế tương đương 1 ngày game.
/// CelestialManager dùng giá trị này để drive smooth movement theo Time.deltaTime.
/// </summary>
public float SecondsPerDay => realSecondsPerGameDay;

// ══════════════════════════════════════════════════════════════════════════════
// SỬA IsDaytime (đã được sửa trong codebase hiện tại — chỉ ghi lại để xác nhận)
// ══════════════════════════════════════════════════════════════════════════════

// TRƯỚC (dùng magic number 18):
// public bool IsDaytime => GameHour >= MORNING_HOUR && GameHour < 18;

// SAU (dùng NIGHT_HOUR = 19, đã fix trong file hiện tại):
// public bool IsDaytime => GameHour >= MORNING_HOUR && GameHour < NIGHT_HOUR;
// → Xác nhận: file hiện tại ĐÃ dùng NIGHT_HOUR, conflict đã được resolve.






using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Script.Manager.WorldManager;

namespace Script.Editor
{
    /// <summary>
    /// Tool tạo toàn bộ Sky hierarchy trong scene.
    /// Menu: Tools → Setup CelestialManager
    ///
    /// Hierarchy được tạo:
    ///   Sky
    ///    ├── Background  (SpriteRenderer — cần assign sprite trắng)
    ///    ├── Sun         (SpriteRenderer)
    ///    ├── Moon        (SpriteRenderer)
    ///    └── Waypoints
    ///         ├── WP_0  (chân trời trái)
    ///         ├── WP_1
    ///         ├── WP_2  (đỉnh)
    ///         ├── WP_3
    ///         └── WP_4  (chân trời phải)
    /// </summary>
    public class CelestialSetupTool : EditorWindow
    {
        [MenuItem("Tools/Setup CelestialManager")]
        public static void SetupCelestialManager()
        {
            // Guard: không tạo trùng
            CelestialManager existing = Object.FindObjectOfType<CelestialManager>();
            if (existing != null)
            {
                Debug.LogWarning($"[CelestialSetupTool] CelestialManager đã tồn tại trên object: '{existing.gameObject.name}'. Hủy setup.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Guard: cần camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[CelestialSetupTool] Không tìm thấy Main Camera trong scene. Hãy đặt tag 'MainCamera' cho camera trước.");
                return;
            }

            // Đảm bảo Sorting Layer "Sky" tồn tại
            EnsureSortingLayer("Sky");

            // ── Tạo Sky root ──────────────────────────────────────────────────────
            GameObject skyRoot = new GameObject("Sky");
            skyRoot.transform.position = new Vector3(
                mainCam.transform.position.x,
                0f,
                0f
            );

            CelestialManager manager = skyRoot.AddComponent<CelestialManager>();
            manager.mainCamera = mainCam;

            // ── Background ────────────────────────────────────────────────────────
            // Background cần sprite màu TRẮNG để SpriteRenderer.color tint đúng.
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(skyRoot.transform);
            bgObj.transform.localPosition = new Vector3(0f, 0f, 10f); // đẩy về sau
            SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sortingLayerName = "Sky";
            bgRenderer.sortingOrder = 0;
            bgRenderer.color = Color.white;
            manager.backgroundRenderer = bgRenderer;

            // ── Sun ───────────────────────────────────────────────────────────────
            GameObject sunObj = new GameObject("Sun");
            sunObj.transform.SetParent(skyRoot.transform);
            sunObj.transform.localPosition = new Vector3(-manager.waypoints.Count, 5f, 9f);
            SpriteRenderer sunRenderer = sunObj.AddComponent<SpriteRenderer>();
            sunRenderer.sortingLayerName = "Sky";
            sunRenderer.sortingOrder = 2;
            manager.sun = sunObj.transform;
            manager.sunRenderer = sunRenderer;

            // ── Moon ──────────────────────────────────────────────────────────────
            GameObject moonObj = new GameObject("Moon");
            moonObj.transform.SetParent(skyRoot.transform);
            moonObj.transform.localPosition = new Vector3(5f, 5f, 9f);
            SpriteRenderer moonRenderer = moonObj.AddComponent<SpriteRenderer>();
            moonRenderer.sortingLayerName = "Sky";
            moonRenderer.sortingOrder = 2;
            manager.moon = moonObj.transform;
            manager.moonRenderer = moonRenderer;

            // ── Waypoints ─────────────────────────────────────────────────────────
            // Tạo 5 waypoint theo hình cung nửa ellipse.
            // Người dùng có thể kéo chỉnh lại trong scene view.
            //
            // Hình dung (nhìn từ trên):
            //   WP0              WP4
            //      WP1    WP3
            //          WP2
            // Thực ra là arc từ trái → phải, WP2 là đỉnh cao nhất.
            GameObject waypointsRoot = new GameObject("Waypoints");
            waypointsRoot.transform.SetParent(skyRoot.transform);
            waypointsRoot.transform.localPosition = Vector3.zero;

            // Định nghĩa arc: 5 điểm theo hình bán ellipse
            // radiusX theo hướng ngang, radiusY theo hướng dọc
            float rX = 20f;
            float rY = 10f;
            int waypointCount = 5;
            List<Transform> wps = new List<Transform>();

            for (int i = 0; i < waypointCount; i++)
            {
                // Góc từ π (trái) → 0 (phải), chiều từ trái sang phải
                float t = i / (float)(waypointCount - 1);       // 0 → 1
                float angle = Mathf.PI - t * Mathf.PI;          // π → 0

                float x = Mathf.Cos(angle) * rX;                // -rX → +rX
                float y = Mathf.Sin(angle) * rY;                //  0  → rY → 0

                GameObject wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(waypointsRoot.transform);
                wp.transform.localPosition = new Vector3(x, y, 9f);
                wps.Add(wp.transform);
            }

            manager.waypoints = wps;

            // ── Focus inspector ───────────────────────────────────────────────────
            Selection.activeGameObject = skyRoot;
            Undo.RegisterCreatedObjectUndo(skyRoot, "Setup CelestialManager");

            Debug.Log(
                "[CelestialSetupTool] Sky hierarchy đã được tạo!\n" +
                "Việc cần làm:\n" +
                "  1. Assign sprite TRẮNG cho Background SpriteRenderer\n" +
                "  2. Assign sprite mặt trời cho Sun SpriteRenderer\n" +
                "  3. Assign sprite mặt trăng cho Moon SpriteRenderer\n" +
                "  4. Kéo chỉnh các WP_x trong Scene View để tạo arc theo ý muốn\n" +
                "  5. Thêm 'Sky' vào Sorting Layers nếu chưa có (đã cố gắng tự thêm)"
            );
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Utility: Tự thêm Sorting Layer nếu chưa có
        // ──────────────────────────────────────────────────────────────────────────

        private static void EnsureSortingLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );

            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null) return;

            // Kiểm tra đã tồn tại chưa
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(i);
                if (layer.FindPropertyRelative("name").stringValue == layerName) return;
            }

            // Thêm mới
            sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
            SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = layerName;
            newLayer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();

            tagManager.ApplyModifiedProperties();
            Debug.Log($"[CelestialSetupTool] Đã thêm Sorting Layer '{layerName}'.");
        }
    }
}