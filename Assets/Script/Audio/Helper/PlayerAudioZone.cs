using System.Collections;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Gắn lên Player GameObject.
    /// Poll AudioEmitterRegistry mỗi checkInterval giây.
    /// Tính khoảng cách 2D (X và Y) phù hợp game 2D.
    /// Không dùng Physics/Collider.
    /// Chia làm 10 bucket để phân phối load qua các frame.
    ///
    /// SETUP:
    ///   1. Gắn component này lên Player
    ///   2. cullingDistance được auto-set từ AudioManager
    ///   3. Không cần setup gì thêm
    /// </summary>
    public class PlayerAudioZone : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Performance")]
        [Tooltip("Giây giữa mỗi lần poll toàn bộ emitters.")]
        [SerializeField] private float checkInterval = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool  showGizmo     = true;
        [SerializeField] private Color gizmoFillColor = new Color(0f, 1f, 0.5f, 0.15f);
        [SerializeField] private Color gizmoWireColor = new Color(0f, 1f, 0.5f, 0.8f);
        [SerializeField] private int   _emitterCountInRange;

        #endregion

        #region Private Fields

        private Vector2      _playerPos2D;   // Cache, update mỗi BatchCheck
        private AudioManager _audioManager;  // Cache, tránh gọi Instance nhiều lần

        private float _timer = 0f;
        private int _bucketIndex = 0;
        private const int BUCKET_COUNT = 10; // Chia làm 10 nhóm để phân phối load

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _audioManager = AudioManager.Instance;

            if (_audioManager != null)
                _audioManager.PlayerTransform = transform;
        }

        private void Update()
        {
            var emitters = AudioEmitterRegistry.All;
            if (emitters.Count == 0) return;

            _timer += Time.deltaTime;

            // Mỗi bucket được xử lý mỗi checkInterval (0.3s)
            // Full cycle (10 buckets) mất 10 * 0.3s = 3s
            if (_timer >= checkInterval)
            {
                _timer = 0f;
                ProcessCurrentBucket();
                _bucketIndex = (_bucketIndex + 1) % BUCKET_COUNT;
            }
        }

        #endregion

        #region Poll

        private void ProcessCurrentBucket()
        {
            // Cache playerPos và cullingDist 1 lần cho cả bucket
            _playerPos2D = transform.position;
            float cullingDist = GetCullingDistance();

            var emitters = AudioEmitterRegistry.All;
            int count = emitters.Count;
            if (count == 0) return;

            // Tính bucket size và range
            int bucketSize = Mathf.CeilToInt((float)count / BUCKET_COUNT);
            int startIndex = _bucketIndex * bucketSize;
            int endIndex = Mathf.Min(startIndex + bucketSize, count);

            if (startIndex >= count) return;

            int inRangeCount = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                var emitter = emitters[i];
                if (emitter == null) continue;

                bool inRange = IsEmitterInRange(emitter, cullingDist);

                // Debug log cho portal chỉ khi trong range
                if (inRange && emitter.gameObject.name.Contains("Portal"))
                {
                    float distance = Vector2.Distance(_playerPos2D, (Vector2)emitter.transform.position);
                    Debug.Log($"[PlayerAudioZone] Portal: {emitter.gameObject.name}, Distance: {distance:F2}m, Culling: {cullingDist:F2}m, InRange: {inRange}");
                }

                emitter.SetInRange(inRange);

                if (inRange) inRangeCount++;
            }

            _emitterCountInRange = inRangeCount;
        }

        private void BatchCheck()
        {
            // Đã thay thế bằng ProcessCurrentBucket bucket-based
            // Giữ lại cho backward compatibility nếu cần
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check ngay lập tức cho một emitter cụ thể (cho AudioTest / debug).
        /// </summary>
        public void CheckEmitterImmediate(AudioEmitter emitter)
        {
            if (emitter == null) return;

            _playerPos2D = transform.position;
            float cullingDist = GetCullingDistance();
            bool  inRange     = IsEmitterInRange(emitter, cullingDist);

            Debug.Log($"[PlayerAudioZone] CheckEmitterImmediate: " +
                      $"distance={Vector2.Distance(_playerPos2D, (Vector2)emitter.transform.position):F2}m, " +
                      $"cullingDistance={cullingDist:F2}m, inRange={inRange}");

            emitter.SetInRange(inRange);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Tính khoảng cách 2D giữa player và emitter, so sánh với cullingDist.
        /// </summary>
        private bool IsEmitterInRange(AudioEmitter emitter, float cullingDist)
        {
            Vector2 emitterPos = emitter.transform.position;
            return Vector2.Distance(_playerPos2D, emitterPos) <= cullingDist;
        }

        /// <summary>
        /// Lấy cullingDistance từ AudioManager đã cache.
        /// Fallback về AudioConstants nếu manager chưa sẵn sàng.
        /// </summary>
        private float GetCullingDistance()
            => _audioManager != null
                ? _audioManager.CullingDistance
                : AudioConstants.DEFAULT_CULLING_DISTANCE;

        #endregion

        #region Gizmo

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            // Dùng Instance trực tiếp vì Gizmo chạy trong Editor (không có Start)
            float cullingDist = Application.isPlaying && _audioManager != null
                ? _audioManager.CullingDistance
                : AudioConstants.DEFAULT_CULLING_DISTANCE;

            Gizmos.color = gizmoFillColor;
            Gizmos.DrawSphere(transform.position, cullingDist);

            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireSphere(transform.position, cullingDist);
        }

        #endregion
    }
}
