using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton điều khiển hiệu ứng nghiêng camera theo hướng portal.
/// Chỉ tác động lên Rotation (Z-axis), KHÔNG đụng tới Position
/// để tránh xung đột với CameraFollowPlayer.
///
/// Cơ chế dual-portal cancel:
///   - Portal gọi RequestShake() thay vì ShakeToward() trực tiếp.
///   - LateUpdate() thu thập tất cả request trong frame, nếu có cả 2 hướng
///     ngược nhau → cancel tất cả (không nghiêng).
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings")]
    [Tooltip("Số lần rung mặc định")]
    public int defaultShakeCount = 5;

    [Tooltip("Khoảng thời gian mỗi lần rung (giây)")]
    public float defaultInterval = 0.2f;

    [Tooltip("Góc nghiêng tối đa (độ)")]
    public float defaultTiltAngle = 2f;

    [Tooltip("Giảm giá trị góc nghiêng mỗi chu kỳ (0 = không giảm, 0.3 = giảm 30%/chu kỳ)\n" +
             "Ví dụ: angle=5°, decay=0.3 → chu kỳ 1=5°, chu kỳ 2=3.5°, chu kỳ 3=2°...")]
    [Range(0f, 0.9f)]
    public float tiltDecayPerCycle = 0.3f;

    [Tooltip("Số lần rung cộng thêm khi shake both (Bidirectional).\n" +
             "Vì rung qua lại cần nhiều chu kỳ hơn để cảm nhận rõ (VD: 5 + 3 = 8 lần).")]
    public int bidirectionalExtraShakes = 3;

    [Header("Test Settings")]
    [Tooltip("Khi bấm ContextMenu \"Test Shake LEFT/RIGHT\", script sẽ tự tạo 1 portal giả lập nằm cách \n" +
             "camera một khoảng bằng testOffset (theo đơn vị World Space) về phía trái hoặc phải.\n" +
             "Tăng lên nếu camera không thấy nghiêng, giảm xuống nếu chỉ test gần.")]
    public float testOffset = 20f;

    // Coroutine đang chạy (để dừng nếu có shake mới)
    private Coroutine _shakeCoroutine;

    // ─── Pending request system (dùng cho dual-portal cancel) ───────────────
    private struct ShakeRequest
    {
        public Vector3 portalWorldPosition;
        public int     shakeCount;
        public float   interval;
        public float   tiltAngle;
    }

    // Dùng nullable để biết có request hay không mà không cần thêm flag riêng
    private ShakeRequest? _pendingLeft;   // request từ portal phía TRÁI camera
    private ShakeRequest? _pendingRight;  // request từ portal phía PHẢI camera

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Không DontDestroyOnLoad — Camera không cần persist qua scene
        }
        else
        {
            // Dùng Destroy(this) thay vì Destroy(gameObject) để không xóa
            // CameraFollowPlayer đang dùng chung GameObject này
            Destroy(this);
        }
    }

    /// <summary>
    /// Thu thập các request trong frame, áp dụng dual-portal cancel logic,
    /// sau đó chạy shake hoặc bỏ qua.
    /// </summary>
    private void LateUpdate()
    {
        bool hasLeft  = _pendingLeft.HasValue;
        bool hasRight = _pendingRight.HasValue;

        if (hasLeft && hasRight)
        {
            // 2 portal ngược hướng → cancel, dừng shake hiện tại nếu đang chạy
            StopCurrentShake();
        }
        else if (hasLeft)
        {
            var req = _pendingLeft.Value;
            StartShakeInternal(req.portalWorldPosition, req.shakeCount, req.interval, req.tiltAngle);
        }
        else if (hasRight)
        {
            var req = _pendingRight.Value;
            StartShakeInternal(req.portalWorldPosition, req.shakeCount, req.interval, req.tiltAngle);
        }

        // Xóa pending sau mỗi frame
        _pendingLeft  = null;
        _pendingRight = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Entry point dành cho các caller cần dual-portal cancel (EnemyPortalBehavior).
    /// Request được thu thập và xử lý ở LateUpdate.
    /// </summary>
    public void RequestShake(Vector3 portalWorldPosition,
                             int   shakeCount = -1,
                             float interval   = -1f,
                             float tiltAngle  = -1f)
    {
        int   count = shakeCount < 0 ? defaultShakeCount : shakeCount;
        float ivl   = interval   < 0 ? defaultInterval   : interval;
        float angle = tiltAngle  < 0 ? defaultTiltAngle  : tiltAngle;

        var req = new ShakeRequest
        {
            portalWorldPosition = portalWorldPosition,
            shakeCount          = count,
            interval            = ivl,
            tiltAngle           = angle
        };

        // Phân loại hướng so với camera
        bool isRight = portalWorldPosition.x >= transform.position.x;
        if (isRight)
            _pendingRight = req;
        else
            _pendingLeft = req;
    }

    /// <summary>
    /// Shake trực tiếp, bypass dual-portal cancel (dùng cho caller không phải portal).
    /// </summary>
    public void ShakeToward(Vector3 portalWorldPosition,
                            int   shakeCount = -1,
                            float interval   = -1f,
                            float tiltAngle  = -1f)
    {
        int   count = shakeCount < 0 ? defaultShakeCount : shakeCount;
        float ivl   = interval   < 0 ? defaultInterval   : interval;
        float angle = tiltAngle  < 0 ? defaultTiltAngle  : tiltAngle;

        StartShakeInternal(portalWorldPosition, count, ivl, angle);
    }

    /// <summary>
    /// Dừng shake ngay lập tức và reset rotation về 0.
    /// </summary>
    public void StopShake()
    {
        StopCurrentShake();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ContextMenu Tests (chỉ dùng trong Editor)
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Test Shake LEFT")]
    private void TestShakeLeft()
    {
        // Giả lập portal bên trái camera
        Vector3 fakePortalLeft = transform.position + Vector3.left * testOffset;
        RequestShake(fakePortalLeft);
        Debug.Log($"[CameraShake] Test LEFT – fake portal at {fakePortalLeft}");
    }

    [ContextMenu("Test Shake RIGHT")]
    private void TestShakeRight()
    {
        // Giả lập portal bên phải camera
        Vector3 fakePortalRight = transform.position + Vector3.right * testOffset;
        RequestShake(fakePortalRight);
        Debug.Log($"[CameraShake] Test RIGHT – fake portal at {fakePortalRight}");
    }

    [ContextMenu("Test Shake BOTH (bidirectional jitter)")]
    private void TestShakeBoth()
    {
        // Khi 2 portal kéo ngược chiều → camera rung giật qua lại (không nghiêng hẳn 1 bên)
        ShakeBidirectional();
        Debug.Log("[CameraShake] Test BOTH – bidirectional jitter");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void StartShakeInternal(Vector3 portalWorldPosition, int count, float ivl, float angle)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(ShakeRoutine(portalWorldPosition, count, ivl, angle));
    }

    /// <summary>
    /// Rung giật qua lại (trái → phải → trái...) dùng khi 2 portal kéo ngược chiều nhau.
    /// Bypass RequestShake/cancel check, gọi trực tiếp.
    /// </summary>
    public void ShakeBidirectional(int shakeCount = -1, float interval = -1f, float tiltAngle = -1f)
    {
        int   count = shakeCount < 0 ? defaultShakeCount : shakeCount;
        float ivl   = interval   < 0 ? defaultInterval   : interval;
        float angle = tiltAngle  < 0 ? defaultTiltAngle  : tiltAngle;

        // Cộng thêm số lần rung riêng cho chế độ Bidirectional
        count += bidirectionalExtraShakes;

        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(BidirectionalRoutine(count, ivl, angle));
    }

    private void StopCurrentShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }
        transform.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutine nội bộ
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine(Vector3 portalWorldPosition,
                                     int   shakeCount,
                                     float interval,
                                     float tiltAngle)
    {
        // Portal bên PHẢI camera → nghiêng phải → Z âm (chiều kim đồng hồ)
        // Portal bên TRÁI  camera → nghiêng trái → Z dương
        float dirX    = portalWorldPosition.x - transform.position.x;
        float tiltDir = dirX >= 0f ? -1f : 1f;

        Quaternion neutral = Quaternion.identity;
        float halfInterval = interval * 0.5f;

        for (int i = 0; i < shakeCount; i++)
        {
            // Giảm góc nghiêng mỗi chu kỳ theo hệ số decay
            float currentAngle = tiltAngle * Mathf.Pow(1f - tiltDecayPerCycle, i);
            Quaternion tiltTarget = Quaternion.Euler(0f, 0f, tiltDir * currentAngle);

            // --- Giai đoạn 1: Nghiêng về phía portal ---
            float elapsed = 0f;
            while (elapsed < halfInterval)
            {
                float t = elapsed / halfInterval;
                transform.rotation = Quaternion.Lerp(neutral, tiltTarget, EaseInOut(t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = tiltTarget;

            // --- Giai đoạn 2: Trả về thẳng ---
            elapsed = 0f;
            while (elapsed < halfInterval)
            {
                float t = elapsed / halfInterval;
                transform.rotation = Quaternion.Lerp(tiltTarget, neutral, EaseInOut(t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = neutral;
        }

        // Đảm bảo camera hoàn toàn thẳng sau khi xong
        transform.rotation = Quaternion.identity;
        _shakeCoroutine = null;
    }

    // Smoothstep ease in-out
    private static float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Rung gi\u1eadt qua l\u1ea1i: m\u1ed7i chu k\u1ef3 \u0111\u1ea3o chi\u1ec1u nghi\u00eang (+/-), t\u1ea1o c\u1ea3m gi\u00e1c ch\u1ea5n v\u1ea1t.
    /// </summary>
    private IEnumerator BidirectionalRoutine(int shakeCount, float interval, float tiltAngle)
    {
        Quaternion neutral = Quaternion.identity;
        float cycleTime = interval; // Một chu kỳ là một lần lắc qua trái hoặc phải

        // Bắt đầu từ trục giữa, chúng ta thực hiện shakeCount lần lắc qua lại
        for (int i = 0; i < shakeCount; i++)
        {
            // Lắc qua lại: 0 -> Phải, 1 -> Trái, 2 -> Phải...
            float tiltDir = (i % 2 == 0) ? -1f : 1f;
            
            // Vị trí bắt đầu của chu kỳ này
            Quaternion startRot = transform.rotation;
            
            // Tính góc mục tiêu với decay
            float currentAngle = tiltAngle * Mathf.Pow(1f - tiltDecayPerCycle, i);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, tiltDir * currentAngle);

            float elapsed = 0f;
            while (elapsed < cycleTime)
            {
                float t = elapsed / cycleTime;
                transform.rotation = Quaternion.Lerp(startRot, targetRot, EaseInOut(t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRot;
        }

        // Sau khi hoàn thành các lần lắc, trả về vị trí cân bằng
        float returnElapsed = 0f;
        float returnTime = cycleTime;
        Quaternion lastRot = transform.rotation;
        while (returnElapsed < returnTime)
        {
            float t = returnElapsed / returnTime;
            transform.rotation = Quaternion.Lerp(lastRot, neutral, EaseInOut(t));
            returnElapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        _shakeCoroutine = null;
    }
}
