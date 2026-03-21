using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton điều khiển hiệu ứng rung/nghiêng camera.
/// Chỉ tác động lên Rotation (Z-axis), KHÔNG đụng tới Position
/// để tránh xung đột với CameraFollowPlayer.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings")]
    [Tooltip("Số lần rung mặc định")]
    public int defaultShakeCount = 2;

    [Tooltip("Khoảng cách thời gian giữa mỗi lần rung (giây)")]
    public float defaultInterval = 0.2f;

    [Tooltip("Góc nghiêng tối đa (độ)")]
    public float defaultTiltAngle = 4f;

    // Coroutine đang chạy (dùng để dừng nếu có shake mới)
    private Coroutine _shakeCoroutine;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Không DontDestroyOnLoad vì script này nằm trên Camera
            // (Camera thường không bị destroy giữa scene trong Kingdom Two Crowns style)
        }
        else
        {
            // Nếu đã có instance rồi thì destroy component này (không destroy cả GameObject
            // vì CameraFollowPlayer cũng đang dùng chung object)
            Destroy(this);
        }
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Shake camera hướng về phía portalWorldPosition.
    /// Dùng giá trị mặc định nếu không truyền tham số.
    /// </summary>
    /// <param name="portalWorldPosition">Vị trí world của portal (để tính hướng nghiêng)</param>
    /// <param name="shakeCount">Số lần rung. -1 = dùng mặc định</param>
    /// <param name="interval">Khoảng thời gian mỗi lần rung. -1 = dùng mặc định</param>
    /// <param name="tiltAngle">Góc nghiêng (độ). -1 = dùng mặc định</param>
    public void ShakeToward(Vector3 portalWorldPosition,
                            int   shakeCount = -1,
                            float interval   = -1f,
                            float tiltAngle  = -1f)
    {
        int   count = shakeCount < 0 ? defaultShakeCount : shakeCount;
        float ivl   = interval   < 0 ? defaultInterval   : interval;
        float angle = tiltAngle  < 0 ? defaultTiltAngle  : tiltAngle;

        // Dừng shake cũ trước khi bắt đầu shake mới
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(ShakeRoutine(portalWorldPosition, count, ivl, angle));
    }

    /// <summary>
    /// Dừng shake ngay lập tức và reset rotation về 0.
    /// </summary>
    public void StopShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }
        transform.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────
    // Coroutine nội bộ
    // ─────────────────────────────────────────────

    private IEnumerator ShakeRoutine(Vector3 portalWorldPosition,
                                     int   shakeCount,
                                     float interval,
                                     float tiltAngle)
    {
        // Tính hướng nghiêng:
        //   Portal bên PHẢI camera → nghiêng phải → Z âm (chiều kim đồng hồ)
        //   Portal bên TRÁI  camera → nghiêng trái → Z dương
        float dirX    = portalWorldPosition.x - transform.position.x;
        float tiltDir = dirX >= 0f ? -1f : 1f;

        Quaternion neutral      = Quaternion.identity;
        Quaternion tiltTarget   = Quaternion.Euler(0f, 0f, tiltDir * tiltAngle);

        float halfInterval = interval * 0.5f;

        for (int i = 0; i < shakeCount; i++)
        {
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

    // Ease in-out để chuyển động mượt hơn (smoothstep)
    private static float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
