using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;

namespace Script.Manager.WorldManager
{
    /// <summary>
    /// Quản lý bầu trời.
    /// Scene hierarchy được giả định là:
    ///   Sky             ← skyRoot (cái này follow camera)
    ///   ├── SkyBackground
    ///   └── Celestial
    ///       ├── Sun
    ///       ├── Moon
    ///       └── StarPath
    ///
    /// CelestialManager chỉ cần biết 3 thứ:
    ///   1. skyRoot    → GameObject Sky để move theo camera
    ///   2. sunRenderer / moonRenderer → để hiển thị/ẩn và set vị trí
    ///   3. waypoints  → đường di chuyển của Sun/Moon
    /// </summary>
    public class CelestialManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector Fields
        // ──────────────────────────────────────────────────────────────────────────

        [Header("Sky Root")]
        [Tooltip("GameObject Sky gốc trong Scene — sẽ được move theo camera mỗi frame.")]
        public Transform skyRoot;

        [Header("Renderers")]
        public SpriteRenderer sunRenderer;
        public SpriteRenderer moonRenderer;
        public SpriteRenderer backgroundRenderer;

        [Header("Waypoints (con của StarPath)")]
        [Tooltip("Waypoints cho quỹ đạo Sun/Moon. Index 0 = chân trời trái, index cuối = chân trời phải.")]
        public List<Transform> waypoints = new List<Transform>();

        [Header("Config")]
        [Tooltip("Nếu không kéo vào, script sẽ tự tìm trong Resources/Data.")]
        public CelestialConfig config;

        private IGameTimeProvider _gameTimeProvider;

        public void Setup(IGameTimeProvider timeProvider)
        {
            _gameTimeProvider = timeProvider;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────────────────────

        private Camera _mainCamera;
        private float _smoothDayProgress;

        // ──────────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Tự tìm Camera
            _mainCamera = Camera.main;

            // Tự tìm Config nếu chưa được gán
            if (config == null)
            {
                config = Resources.Load<CelestialConfig>("Data/CelestialConfig");
                if (config == null)
                    Debug.LogWarning("[CelestialManager] Không tìm thấy CelestialConfig trong Resources/Data/. Hãy kéo vào Inspector.");
            }
        }

        private void Start()
        {
            if (_gameTimeProvider == null) return;

            _smoothDayProgress = GetRawDayProgress();

            float startHour = _smoothDayProgress * 24f;
            UpdateSkyColor(startHour);
            UpdateSun(startHour);
            UpdateMoon(startHour);
        }

        private void Update()
        {
            if (_gameTimeProvider == null) return;

            AdvanceSmoothProgress();

            float hour = _smoothDayProgress * 24f;
            UpdateSun(hour);
            UpdateMoon(hour);
            UpdateSkyColor(hour);
        }

        private void LateUpdate()
        {
            // Di chuyển toàn bộ Sky theo camera — mọi thứ bên trong Sky tự chạy theo
            if (skyRoot != null && _mainCamera != null)
            {
                float x = _mainCamera.transform.position.x;
                float y = _mainCamera.transform.position.y - _mainCamera.orthographicSize + (config != null ? config.yOffset : 0f);
                skyRoot.position = new Vector3(x, y, skyRoot.position.z);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Smooth Progress
        // ──────────────────────────────────────────────────────────────────────────

        private void AdvanceSmoothProgress()
        {
            float rawProgress = GetRawDayProgress();

            float diff = rawProgress - _smoothDayProgress;
            if (diff > 0.5f)  diff -= 1f;
            if (diff < -0.5f) diff += 1f;

            const float snapThreshold = 5f / 1440f;
            if (Mathf.Abs(diff) > snapThreshold)
            {
                _smoothDayProgress = rawProgress;
                return;
            }

            float progressPerSecond = 1f / 240f; // Default 240s per day if provider is missing SecondsPerDay property
            // In a better design, SecondsPerDay should be in IGameTimeProvider
            // For now, using the value from GameTimeManager or a constant
            _smoothDayProgress += progressPerSecond * Time.deltaTime;
            _smoothDayProgress %= 1f;
        }

        private float GetRawDayProgress()
        {
            return (_gameTimeProvider.GetTotalMinutes % 1440) / 1440f;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Sun
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdateSun(float hour)
        {
            if (sunRenderer == null || waypoints.Count < 2 || config == null) return;

            bool visible = hour >= config.sunRiseHour && hour < config.sunSetHour;
            sunRenderer.enabled = visible;
            if (!visible) return;

            float t = Mathf.InverseLerp(config.sunRiseHour, config.sunSetHour, hour);
            sunRenderer.transform.position = GetPathPosition(t);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Moon
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdateMoon(float hour)
        {
            if (moonRenderer == null || waypoints.Count < 2 || config == null) return;

            float moonT = GetMoonT(hour);
            bool visible = moonT >= 0f;
            moonRenderer.enabled = visible;
            if (!visible) return;

            moonRenderer.transform.position = GetPathPosition(moonT);

            Color c = moonRenderer.color;
            c.a = GetMoonAlpha(hour);
            moonRenderer.color = c;
        }

        private float GetMoonT(float hour)
        {
            if (config == null) return -1f;

            if (hour >= config.moonRiseHour)
                return Mathf.InverseLerp(config.moonRiseHour, 24f, hour) * 0.5f;

            if (hour < config.moonSetHour)
                return 0.5f + Mathf.InverseLerp(0f, config.moonSetHour, hour) * 0.5f;

            return -1f;
        }

        private float GetMoonAlpha(float hour)
        {
            if (config == null) return 1f;

            if (hour >= config.moonRiseHour && hour < config.moonRiseHour + config.moonFadeInHours)
            {
                float p = Mathf.InverseLerp(config.moonRiseHour, config.moonRiseHour + config.moonFadeInHours, hour);
                return Mathf.Lerp(config.moonDuskAlpha, 1f, p);
            }
            return 1f;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Waypoint Path
        // ──────────────────────────────────────────────────────────────────────────

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

        private void UpdateSkyColor(float hour)
        {
            if (backgroundRenderer == null || config == null) return;
            backgroundRenderer.color = SampleSkyColor(hour);
        }

        private Color SampleSkyColor(float hour)
        {
            if (hour < 6f)   return Color.Lerp(config.colorNight,        config.colorEarlyMorning, Mathf.InverseLerp(4f,  6f,  hour));
            if (hour < 9f)   return Color.Lerp(config.colorEarlyMorning, config.colorMorning,      Mathf.InverseLerp(6f,  9f,  hour));
            if (hour < 12f)  return Color.Lerp(config.colorMorning,      config.colorNoon,         Mathf.InverseLerp(9f,  12f, hour));
            if (hour < 15f)  return Color.Lerp(config.colorNoon,         config.colorAfternoon,    Mathf.InverseLerp(12f, 15f, hour));
            if (hour < 18f)  return Color.Lerp(config.colorAfternoon,    config.colorDusk,         Mathf.InverseLerp(15f, 18f, hour));
            if (hour < 21f)  return Color.Lerp(config.colorDusk,         config.colorNight,        Mathf.InverseLerp(18f, 21f, hour));
            return config.colorNight;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Editor Gizmos
        // ──────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.color = (i == 0 || i == waypoints.Count - 1) ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
            }

            if (!Application.isPlaying || _gameTimeProvider == null || config == null) return;

            float raw  = (_gameTimeProvider.GetTotalMinutes % 1440) / 1440f;
            float hour = raw * 24f;

            if (hour >= config.sunRiseHour && hour < config.sunSetHour)
            {
                float t = Mathf.InverseLerp(config.sunRiseHour, config.sunSetHour, hour);
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
