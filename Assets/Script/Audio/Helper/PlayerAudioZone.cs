using System.Collections;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Gắn lên Player GameObject.
    /// Poll AudioEmitterRegistry mỗi checkInterval giây.
    /// Tính khoảng cách 2D (X và Y) phù hợp game 2D.
    /// Không dùng Physics/Collider.
    ///
    /// SETUP:
    ///   1. Gắn component này lên Player
    ///   2. cullingDistance được auto-set từ AudioManager
    ///   3. Không cần setup gì thêm
    /// </summary>
    public class PlayerAudioZone : MonoBehaviour
    {
        [Header("Performance")]
        [Tooltip("Giây giữa mỗi lần poll. 0.1s")]
        [SerializeField] private float checkInterval = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private int _emitterCountInRange; // Inspector debug

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            // Cập nhật AudioManager về PlayerTransform
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayerTransform = transform;

            StartCoroutine(PollRoutine());
        }

        // ─────────────────────────────────────────────
        //  Poll
        // ─────────────────────────────────────────────

        private IEnumerator PollRoutine()
        {
            // WaitForSeconds tạo một lần, tái sử dụng → không tốn GC
            var wait = new WaitForSeconds(checkInterval);

            while (true)
            {
                yield return wait;
                BatchCheck();
            }
        }

        private void BatchCheck()
        {
            Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);
            int inRangeCount = 0;

            var emitters = AudioEmitterRegistry.All;
            for (int i = 0; i < emitters.Count; i++)
            {
                var emitter = emitters[i];
                if (emitter == null) continue;

                // 2D: tính khoảng cách X và Y
                Vector2 emitterPos = new Vector2(emitter.transform.position.x, emitter.transform.position.y);
                float distance = Vector2.Distance(playerPos, emitterPos);

                // Lấy cullingDistance từ AudioManager
                float cullingDist = AudioManager.Instance?.CullingDistance ?? 50f;
                bool inRange = distance <= cullingDist;

                emitter.SetInRange(inRange);

                if (inRange) inRangeCount++;
            }

            _emitterCountInRange = inRangeCount; // Inspector debug
        }

        /// <summary>
        /// Check ngay lập tức cho một emitter cụ thể (cho AudioTest).
        /// </summary>
        public void CheckEmitterImmediate(AudioEmitter emitter)
        {
            if (emitter == null) return;

            Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 emitterPos = new Vector2(emitter.transform.position.x, emitter.transform.position.y);
            float distance = Vector2.Distance(playerPos, emitterPos);

            // Lấy cullingDistance từ AudioManager
            float cullingDist = AudioManager.Instance?.CullingDistance ?? 50f;
            bool inRange = distance <= cullingDist;

            Debug.Log($"[PlayerAudioZone] CheckEmitterImmediate: distance={distance:F2}m, cullingDistance={cullingDist:F2}m, inRange={inRange}");

            emitter.SetInRange(inRange);
        }

        // ─────────────────────────────────────────────
        //  Gizmo (Editor visualization)
        // ─────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            // Lấy cullingDistance từ AudioManager
            float cullingDist = AudioManager.Instance?.CullingDistance ?? 50f;

            // Vẽ vùng culling dưới dạng hình chữ nhật (trục X là quan trọng)
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.DrawCube(
                transform.position,
                new Vector3(cullingDist * 2f, 20f, 1f));

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(cullingDist * 2f, 20f, 1f));
        }
    }
}
