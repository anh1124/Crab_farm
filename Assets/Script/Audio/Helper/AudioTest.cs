using UnityEngine;

namespace Script.Audio
{
    public enum AudioChannelType
    {
        SFX, // Sound Effects
        BGM, // Background Music
        ENV  // Environment
    }

    /// <summary>
    /// Script test đơn giản để test âm thanh từ SoundRegistry.
    /// Gắn vào GameObject bất kỳ, nhập key từ SoundRegistry, nhấn T để test.
    /// </summary>
    public class AudioTest : MonoBehaviour
    {
        [Header("Cài đặt test")]
        [Tooltip("Chọn channel âm thanh")]
        [SerializeField] private AudioChannelType channelType = AudioChannelType.SFX;

        [Tooltip("Chọn key âm thanh từ SoundRegistry để test")]
        [SerializeField] private int selectedKeyIndex = 0;

        [Tooltip("Danh sách keys từ SoundRegistry (tự động điền)")]
        [SerializeField] private string[] availableKeys;

        // Runtime key (được cập nhật từ selectedKeyIndex)
        private string testKey;

        [Header("Object tạo ra")]
        [Tooltip("Object được tạo ra khi test (sẽ tái sử dụng object này)")]
        public GameObject testObj;

        // AudioEmitter component để quản lý âm thanh
        private AudioEmitter _audioEmitter;

        // Cache min/max distance từ SoundEntry để vẽ debug
        private float _cachedMinDistance;
        private float _cachedMaxDistance;

        private void Awake()
        {
            // Populate keys khi runtime
            if (AudioManager.Instance != null && AudioManager.Instance.SoundRegistry != null)
            {
                availableKeys = AudioManager.Instance.SoundRegistry.GetAllKeys();

                // Sync testKey với selectedKeyIndex
                if (availableKeys != null && selectedKeyIndex >= 0 && selectedKeyIndex < availableKeys.Length)
                {
                    testKey = availableKeys[selectedKeyIndex];
                }
            }
        }

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

            // Populate keys nếu testKey rỗng hoặc availableKeys rỗng
            if (string.IsNullOrEmpty(testKey) || availableKeys == null || availableKeys.Length == 0)
            {
                availableKeys = AudioManager.Instance.SoundRegistry.GetAllKeys();
                if (availableKeys != null && availableKeys.Length > 0)
                {
                    // Sử dụng key đầu tiên nếu selectedKeyIndex không hợp lệ
                    if (selectedKeyIndex < 0 || selectedKeyIndex >= availableKeys.Length)
                    {
                        selectedKeyIndex = 0;
                    }
                    testKey = availableKeys[selectedKeyIndex];
                    Debug.Log($"[AudioTest] Auto-selected key: {testKey} (index: {selectedKeyIndex})");
                }
                else
                {
                    Debug.LogError("[AudioTest] SoundRegistry không có keys nào!");
                    return;
                }
            }

            if (string.IsNullOrEmpty(testKey))
            {
                Debug.LogWarning("[AudioTest] testKey vẫn đang rỗng sau khi populate!");
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
                // Thêm AudioEmitter component
                _audioEmitter = testObj.AddComponent<AudioEmitter>();
                Debug.Log($"[AudioTest] Đã tạo test object với AudioEmitter tại (0,0,0)");
            }

            // Kiểm tra nếu AudioEmitter chưa có (trường hợp object đã tồn tại từ trước)
            if (_audioEmitter == null)
            {
                _audioEmitter = testObj.GetComponent<AudioEmitter>();
                if (_audioEmitter == null)
                {
                    _audioEmitter = testObj.AddComponent<AudioEmitter>();
                }
            }

            // Check range ngay lập tức trước khi phát (bypass poll delay)
            if (AudioManager.Instance?.PlayerTransform != null)
            {
                var playerZone = AudioManager.Instance.PlayerTransform.GetComponent<PlayerAudioZone>();
                if (playerZone != null)
                {
                    playerZone.CheckEmitterImmediate(_audioEmitter);
                }
            }

            // Lấy thông tin player và camera
            Vector3 playerPos = Vector3.zero;
            if (AudioManager.Instance.PlayerTransform != null)
            {
                playerPos = AudioManager.Instance.PlayerTransform.position;
            }

            // Kiểm tra xem obj có nằm trong camera không
            bool isInCamera = false;
            Camera mainCamera = Camera.main;
            if (mainCamera != null && testObj != null)
            {
                Vector3 viewportPoint = mainCamera.WorldToViewportPoint(testObj.transform.position);
                isInCamera = viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                             viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                             viewportPoint.z > 0;
            }

            Debug.Log($"[AudioTest] Phát âm thanh: {testKey} | Channel: {channelType} | Vị trí phát: {testObj.transform.position} | Vị trí Player: {playerPos} | Trong Camera: {(isInCamera ? "Có" : "Không")} | MinDistance: {_cachedMinDistance}m | MaxDistance: {_cachedMaxDistance}m");

            // Phát âm thanh dựa trên channel được chọn
            // AudioTest chỉ tương tác với AudioEmitter, không gọi AudioManager trực tiếp
            switch (channelType)
            {
                case AudioChannelType.SFX:
                    _audioEmitter.PlaySound(testKey, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
                case AudioChannelType.BGM:
                    // BGM nên dùng AudioManager.PlayBGM trực tiếp, không dùng AudioEmitter
                    // Nhưng để test, ta dùng AudioEmitter với loop
                    _audioEmitter.PlaySound(testKey, loop: true, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
                case AudioChannelType.ENV:
                    _audioEmitter.PlaySound(testKey, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
            }
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
                // Thêm AudioEmitter component
                _audioEmitter = testObj.AddComponent<AudioEmitter>();
                Debug.Log($"[AudioTest] Đã tạo test object với AudioEmitter tại (0,0,0)");
            }

            // Kiểm tra nếu AudioEmitter chưa có (trường hợp object đã tồn tại từ trước)
            if (_audioEmitter == null)
            {
                _audioEmitter = testObj.GetComponent<AudioEmitter>();
                if (_audioEmitter == null)
                {
                    _audioEmitter = testObj.AddComponent<AudioEmitter>();
                }
            }

            // Check range ngay lập tức trước khi phát (bypass poll delay)
            if (AudioManager.Instance?.PlayerTransform != null)
            {
                var playerZone = AudioManager.Instance.PlayerTransform.GetComponent<PlayerAudioZone>();
                if (playerZone != null)
                {
                    playerZone.CheckEmitterImmediate(_audioEmitter);
                }
            }

            // Lấy thông tin player và camera
            Vector3 playerPos = Vector3.zero;
            if (AudioManager.Instance.PlayerTransform != null)
            {
                playerPos = AudioManager.Instance.PlayerTransform.position;
            }

            // Kiểm tra xem obj có nằm trong camera không
            bool isInCamera = false;
            Camera mainCamera = Camera.main;
            if (mainCamera != null && testObj != null)
            {
                Vector3 viewportPoint = mainCamera.WorldToViewportPoint(testObj.transform.position);
                isInCamera = viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                             viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                             viewportPoint.z > 0;
            }

            Debug.Log($"[AudioTest] Phát âm thanh: {key} | Channel: {channelType} | Vị trí phát: {testObj.transform.position} | Vị trí Player: {playerPos} | Trong Camera: {(isInCamera ? "Có" : "Không")} | MinDistance: {_cachedMinDistance}m | MaxDistance: {_cachedMaxDistance}m");

            // Phát âm thanh dựa trên channel được chọn
            // AudioTest chỉ tương tác với AudioEmitter, không gọi AudioManager trực tiếp
            switch (channelType)
            {
                case AudioChannelType.SFX:
                    _audioEmitter.PlaySound(key, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
                case AudioChannelType.BGM:
                    // BGM nên dùng AudioManager.PlayBGM trực tiếp, không dùng AudioEmitter
                    // Nhưng để test, ta dùng AudioEmitter với loop
                    _audioEmitter.PlaySound(key, loop: true, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
                case AudioChannelType.ENV:
                    _audioEmitter.PlaySound(key, minDistance: _cachedMinDistance, maxDistance: _cachedMaxDistance, forceReplace: true);
                    break;
            }
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
