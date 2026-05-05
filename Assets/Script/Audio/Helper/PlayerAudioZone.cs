using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Gắn lên Player GameObject.
    /// Vai trò: (1) đăng ký PlayerTransform vào AudioManager, (2) hiển thị gizmo vùng nghe 2D,
    /// (3) in debug log trạng thái emitters mỗi 0.5s khi IsDebug bật.
    ///
    /// Logic polling range (0.3s/lần, SetInRange) đã chuyển sang AudioManager.Update().
    /// </summary>
    public class PlayerAudioZone : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Gizmo")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.6f);

        [Header("Debug")]
        [SerializeField] private bool isDebug = false;
        [SerializeField] private int _emitterCountInRange; // read-only in inspector

        #endregion

        #region Private Fields

        private AudioManager _audioManager;
        private float _debugTimer = 0f;
        private const float DEBUG_INTERVAL = 0.5f;

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
            if (!IsDebug) return;

            _debugTimer += Time.deltaTime;
            if (_debugTimer >= DEBUG_INTERVAL)
            {
                _debugTimer = 0f;
                LogDebugStatus();
            }
        }

        #endregion

        #region Debug

        private bool IsDebug => isDebug
            || (_audioManager != null ? _audioManager.IsDebug
                : (AudioManager.Instance != null && AudioManager.Instance.IsDebug));

        private void LogDebugStatus()
        {
            var emitters = AudioEmitterRegistry.All;
            Vector2 playerPos = transform.position;
            float cullingDist = _audioManager != null
                ? _audioManager.CullingDistance
                : AudioConstants.DEFAULT_CULLING_DISTANCE;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(
                $"<color=white>[PlayerAudioZone] {emitters.Count} emitters | Culling:{cullingDist:F0}m | " +
                $"Player:({playerPos.x:F1},{playerPos.y:F1})</color>");

            int inRangeCount = 0;
            for (int i = 0; i < emitters.Count; i++)
            {
                var emitter = emitters[i];
                if (emitter == null) continue;

                Vector2 pos  = emitter.transform.position;
                float   dist = Vector2.Distance(playerPos, pos);
                string  name = emitter.gameObject.name;
                bool inRange = emitter.IsInRange;

                if (inRange) inRangeCount++;

                var sounds = emitter.GetDebugStatus();

                if (!inRange)
                {
                    // Chỉ hiện log vắn tắt cho các emitter ngoài tầm nếu không có gì đặc biệt
                    // hoặc hiện hết để debug theo yêu cầu.
                    sb.AppendLine($"- {name} @ ({pos.x:F1},{pos.y:F1}) dist:{dist:F1}m <color=red>[OUT OF RANGE]</color>");
                }
                else if (sounds.Count == 0)
                {
                    sb.AppendLine(
                        $"- {name} @ ({pos.x:F1},{pos.y:F1}) dist:{dist:F1}m " +
                        $"<color=gray>[no active sounds]</color>");
                }
                else
                {
                    sb.AppendLine(
                        $"- {name} @ ({pos.x:F1},{pos.y:F1}) dist:{dist:F1}m");

                    foreach (var s in sounds)
                    {
                        // xanh lam = loop | xanh lá = oneshot
                        string col  = s.isLoop ? "#00ffff" : "#00ff00";
                        string type = s.isLoop ? "LOOP" : "SHOT";
                        string stop = s.isPlaying ? "" : " <color=red>[STOPPED]</color>";
                        sb.AppendLine(
                            $"    <color={col}>[{type}] {s.key} | vol:{s.volume:F2}{stop} (Range:{s.minDist:F1}-{s.maxDist:F1}m)</color>");
                    }
                }
            }

            _emitterCountInRange = inRangeCount;
            sb.Append($"<color=yellow>In-range: {inRangeCount}/{emitters.Count}</color>");
            Debug.Log(sb.ToString());
        }

        #endregion

        #region Gizmo

        private void OnDrawGizmos()
        {
            // Không vẽ khi không select để tránh rác scene view
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            float cullingDist = Application.isPlaying && _audioManager != null
                ? _audioManager.CullingDistance
                : AudioConstants.DEFAULT_CULLING_DISTANCE;

            Gizmos.color = gizmoColor;

            // Vẽ vòng tròn 2D thay vì sphere 3D
            DrawCircle2D(transform.position, cullingDist, 64);
        }

        private static void DrawCircle2D(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius,
                                                    Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        #endregion
    }
}
