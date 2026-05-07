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
        // Purge destroyed entries from _inUse (defensive cleanup)
        _inUse.RemoveWhere(s => s == null);

        // Drain destroyed sources from the available queue
        while (_available.Count > 0 && _available.Peek() == null)
            _available.Dequeue();

        // Check nếu đã vượt quá maxPoolSize
        if (_available.Count == 0 && _inUse.Count >= maxPoolSize)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsDebug)
                Debug.LogWarning($"[AudioPool] Đã đạt giới hạn maxPoolSize ({maxPoolSize}). Không thể phát thêm âm thanh.");
            return null;
        }

        AudioSource source = (_available.Count > 0)
            ? _available.Dequeue()
            : CreateSource();

        // Guard: source vẫn có thể null/destroyed nếu queue bị corrupted
        if (source == null)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsDebug)
                Debug.LogWarning("[AudioPool] Dequeued a destroyed AudioSource, creating new one.");
            source = CreateSource();
            if (source == null) return null;
        }

        source.gameObject.SetActive(true);
        source.transform.position = worldPosition;

        _inUse.Add(source);
        UpdateDebugCount();

        if (AudioManager.Instance != null && AudioManager.Instance.IsDebug)
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
        source.spread      = 0f;
        source.spatialBlend = 1f; // Mặc định Stereo cho lần dùng sau
        source.panStereo    = 0f; // Reset panning
        source.gameObject.SetActive(false);

        // Tránh SetParent error khi parent GameObject đang được deactivate
        // Chỉ reparent nếu parent không phải là null và không đang bị destroy
        if (source.transform.parent != null && (source.transform.parent.gameObject == null || source.transform.parent.gameObject.activeInHierarchy))
        {
            source.transform.SetParent(transform);
        }

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

    /// <summary>Tạm dừng hoặc tiếp tục tất cả âm thanh đang phát.</summary>
    public void PauseAll(bool pause)
    {
        foreach (var source in _inUse)
        {
            if (source == null) continue;
            if (pause) source.Pause(); else source.UnPause();
        }
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
        // MONO: spatialBlend = 0 (2D) - không có 3D positioning
        source.spatialBlend = 0f;
        source.playOnAwake  = false;

        // MONO: Không cần 3D spatial audio curves
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 0f;
        source.maxDistance = 1f;

        go.SetActive(false);
        _available.Enqueue(source);
        return source;
    }
}
}
