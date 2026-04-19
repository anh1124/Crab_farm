using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Script.Audio
{
    /// <summary>
    /// Pool tái sử dụng AudioSource để tránh tạo/hủy GameObject liên tục.
    /// Gắn vào cùng GameObject với AudioManager.
    /// </summary>
    public class AudioPool : MonoBehaviour
{
    [Tooltip("AudioMixerGroup dành cho SFX - set trong Inspector")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Tooltip("Số AudioSource tạo sẵn khi khởi động")]
    [SerializeField] private int initialPoolSize = AudioConstants.DEFAULT_POOL_SIZE;

    [Tooltip("Số AudioSource tối đa (dynamic expansion)")]
    [SerializeField] private int maxPoolSize = AudioConstants.MAX_POOL_SIZE;

    [Header("Debug (read-only)")]
    [SerializeField] private int _availableCount;
    [SerializeField] private int _inUseCount;

    private readonly Queue<AudioSource>  _available = new Queue<AudioSource>();
    private readonly HashSet<AudioSource> _inUse    = new HashSet<AudioSource>();

    private static int _totalCreated = 0; // Counter để đặt tên unique cho pooled sources

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
            CreateSource();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lấy một AudioSource từ pool, đặt tại worldPosition.
    /// Pool sẽ tự mở rộng nếu hết nguồn (tới maxPoolSize).
    /// </summary>
    public AudioSource Get(Vector3 worldPosition)
    {
        // Check nếu đã vượt quá maxPoolSize
        if (_available.Count == 0 && _inUse.Count >= maxPoolSize)
        {
            Debug.LogWarning($"[AudioPool] Đã đạt giới hạn maxPoolSize ({maxPoolSize}). Không thể phát thêm âm thanh.");
            return null;
        }

        AudioSource source = (_available.Count > 0)
            ? _available.Dequeue()
            : CreateSource();

        source.gameObject.SetActive(true);
        source.transform.position = worldPosition;

        _inUse.Add(source);
        UpdateDebugCount();

        Debug.Log($"[AudioPool] Get: source={source.name}, active={source.gameObject.activeSelf}, parent={source.transform.parent.name}");

        return source;
    }

    /// <summary>
    /// Trả AudioSource về pool. Reset toàn bộ trạng thái.
    /// An toàn khi gọi nhiều lần với cùng một source.
    /// </summary>
    public void Return(AudioSource source)
    {
        if (source == null || !_inUse.Contains(source))
            return;

        source.Stop();
        source.clip        = null;
        source.loop        = false;
        source.volume      = 1f;
        source.pitch       = 1f;
        source.panStereo   = 0f;
        source.gameObject.SetActive(false);
        source.transform.SetParent(transform);

        _inUse.Remove(source);
        _available.Enqueue(source);
        UpdateDebugCount();
    }

    /// <summary>Thu hồi tất cả AudioSource đang dùng về pool (ví dụ: khi chuyển Scene).</summary>
    public void ReturnAll()
    {
        // Copy để tránh modify collection khi đang iterate
        var snapshot = new List<AudioSource>(_inUse);
        foreach (var s in snapshot)
            Return(s);
    }

    // ─────────────────────────────────────────────
    //  Debug
    // ─────────────────────────────────────────────

    private void UpdateDebugCount()
    {
        _availableCount = _available.Count;
        _inUseCount = _inUse.Count;
    }

    // ─────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────

    private AudioSource CreateSource()
    {
        var go = new GameObject($"PooledAudio_{_totalCreated:00}");
        _totalCreated++;
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        // spatialBlend = 1 (3D) - để Unity tự tính distance attenuation
        source.spatialBlend = 1f;
        source.playOnAwake  = false;

        // Cấu hình 3D spatial audio với custom curve
        // Tại maxDistance vẫn còn 30% volume
        source.rolloffMode = AudioRolloffMode.Custom;

        AnimationCurve rolloffCurve = AudioCurveCache.GetDefaultRolloffCurve(
            AudioConstants.DEFAULT_MIN_DISTANCE,
            AudioConstants.DEFAULT_MAX_DISTANCE,
            AudioConstants.DEFAULT_VOLUME_AT_MAX);

        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        source.minDistance = AudioConstants.DEFAULT_MIN_DISTANCE;
        source.maxDistance = AudioConstants.DEFAULT_MAX_DISTANCE;

        go.SetActive(false);
        _available.Enqueue(source);
        return source;
    }
}
}
