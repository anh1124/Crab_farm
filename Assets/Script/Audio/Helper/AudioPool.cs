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
    [SerializeField] private int initialPoolSize = 12;

    private readonly Queue<AudioSource>  _available = new Queue<AudioSource>();
    private readonly HashSet<AudioSource> _inUse    = new HashSet<AudioSource>();

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
    /// Pool sẽ tự mở rộng nếu hết nguồn.
    /// </summary>
    public AudioSource Get(Vector3 worldPosition)
    {
        AudioSource source = (_available.Count > 0)
            ? _available.Dequeue()
            : CreateSource();

        source.gameObject.SetActive(true);
        source.transform.position = worldPosition;

        _inUse.Add(source);
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
    //  Private
    // ─────────────────────────────────────────────

    private AudioSource CreateSource()
    {
        var go = new GameObject($"PooledAudio_{_available.Count + _inUse.Count:00}");
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        // spatialBlend = 0 (2D) - chúng ta tự tính volume theo khoảng cách
        // để tránh hiện tượng âm thanh lệch phase kỳ lạ trong game 2D
        source.spatialBlend = 0f;
        source.playOnAwake  = false;

        go.SetActive(false);
        _available.Enqueue(source);
        return source;
    }
}
}
