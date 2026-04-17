using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Script test đơn giản để test âm thanh từ SoundRegistry.
    /// Gắn vào GameObject bất kỳ, nhập key từ SoundRegistry, nhấn T để test.
    /// </summary>
    public class AudioTest : MonoBehaviour
    {
        [Header("Cài đặt test")]
        [Tooltip("Chọn key âm thanh từ SoundRegistry để test")]
        [SerializeField] private int selectedKeyIndex = 0;

        [Tooltip("Danh sách keys từ SoundRegistry (tự động điền)")]
        [SerializeField] private string[] availableKeys;

        // Runtime key (được cập nhật từ selectedKeyIndex)
        private string testKey;

        [Header("Object tạo ra")]
        [Tooltip("Object được tạo ra khi test (sẽ tái sử dụng object này)")]
        public GameObject testObj;

        // Cache min/max distance từ SoundEntry để vẽ debug
        private float _cachedMinDistance;
        private float _cachedMaxDistance;

        private void Update()
        {
            // Nhấn T để test âm thanh
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestSound();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Chỉ populate keys khi đang trong Editor và có AudioManager
            if (AudioManager.Instance != null && AudioManager.Instance.SoundRegistry != null)
            {
                availableKeys = AudioManager.Instance.SoundRegistry.GetAllKeys();
            }

            // Sync testKey với selectedKeyIndex
            if (availableKeys != null && selectedKeyIndex >= 0 && selectedKeyIndex < availableKeys.Length)
            {
                testKey = availableKeys[selectedKeyIndex];
            }
        }
#endif

        /// <summary>
        /// Phát âm thanh test.
        /// Có thể gọi từ UI Button hoặc script khác.
        /// </summary>
        public void TestSound()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[AudioTest] AudioManager.Instance is null! Hãy đảm bảo AudioManager có trong scene.");
                return;
            }

            if (string.IsNullOrEmpty(testKey))
            {
                Debug.LogWarning("[AudioTest] testKey đang rỗng! Hãy nhập key từ SoundRegistry.");
                return;
            }

            // Lấy SoundEntry từ SoundRegistry để lấy min/max distance
            var entry = AudioManager.Instance.SoundRegistry.GetEntry(testKey);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioTest] Không tìm thấy SoundEntry với key: {testKey}");
                return;
            }

            // Cache min/max distance để vẽ debug
            _cachedMinDistance = entry.minDistance;
            _cachedMaxDistance = entry.maxDistance;

            // Chỉ tạo object nếu chưa có, nếu đã có thì tái sử dụng
            if (testObj == null)
            {
                testObj = new GameObject($"[AudioTest_{testKey}]");
                testObj.transform.position = Vector3.zero;
                Debug.Log($"[AudioTest] Đã tạo test object tại (0,0,0)");
            }

            Debug.Log($"[AudioTest] Phát âm thanh: {testKey} | Vị trí: {testObj.transform.position} | MinDistance: {_cachedMinDistance}m | MaxDistance: {_cachedMaxDistance}m");
            AudioManager.Instance.PlaySFXByKey(testKey, testObj.transform.position, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance);
        }

        /// <summary>
        /// Test âm thanh với key khác.
        /// </summary>
        public void TestSound(string key)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[AudioTest] AudioManager.Instance is null!");
                return;
            }

            // Lấy SoundEntry từ SoundRegistry để lấy min/max distance
            var entry = AudioManager.Instance.SoundRegistry.GetEntry(key);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioTest] Không tìm thấy SoundEntry với key: {key}");
                return;
            }

            // Cache min/max distance để vẽ debug
            _cachedMinDistance = entry.minDistance;
            _cachedMaxDistance = entry.maxDistance;

            // Chỉ tạo object nếu chưa có, nếu đã có thì tái sử dụng
            if (testObj == null)
            {
                testObj = new GameObject($"[AudioTest_{key}]");
                testObj.transform.position = Vector3.zero;
                Debug.Log($"[AudioTest] Đã tạo test object tại (0,0,0)");
            }

            Debug.Log($"[AudioTest] Phát âm thanh: {key} | Vị trí: {testObj.transform.position} | MinDistance: {_cachedMinDistance}m | MaxDistance: {_cachedMaxDistance}m");
            AudioManager.Instance.PlaySFXByKey(key, testObj.transform.position, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance);
        }

        // ------------------------------------------------------------------
        // VẼ GIZMO DEBUG CHO MIN/MAX DISTANCE
        // ------------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (testObj == null) return;

            Vector3 center = testObj.transform.position;

            // Vẽ vòng tròn minDistance (màu xanh lá)
            if (_cachedMinDistance > 0)
            {
                Gizmos.color = Color.green;
                DrawCircle(_cachedMinDistance, center);
            }

            // Vẽ vòng tròn maxDistance (màu đỏ)
            if (_cachedMaxDistance > 0)
            {
                Gizmos.color = Color.red;
                DrawCircle(_cachedMaxDistance, center);
            }
        }

        /// <summary>
        /// Hàm vẽ vòng tròn 2D dùng Gizmos.
        /// </summary>
        private void DrawCircle(float radius, Vector3 center)
        {
            int segments = 36;
            float angleStep = 360f / segments;

            float angle = 0f;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                angle = i * angleStep;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}
