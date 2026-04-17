using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Script.Audio
{
    /// <summary>
    /// Trái tim của hệ thống âm thanh. Singleton, không bị hủy khi chuyển Scene.
    ///
    /// ─── SETUP TRONG UNITY ───────────────────────────────────────────────────
    /// 1. Tạo GameObject tên "[AudioManager]" trong Scene đầu tiên (Bootstrap).
    /// 2. Gắn script AudioManager, AudioPool vào GameObject đó.
    /// 3. Trong Inspector:
    ///    - audioMixer   → Asset AudioMixer của bạn
    ///    - bgmSourceA/B → Tạo 2 child GameObject, mỗi cái có AudioSource, kéo vào
    ///    - envSource    → Tạo 1 child GameObject, có AudioSource, kéo vào
    ///    - playerTransform → Kéo Transform của Player (hoặc gán runtime qua SetPlayerTransform)
    /// 4. Trong Audio Mixer, Expose các tham số:
    ///    "MasterVolume", "BGMVolume", "ENVVolume", "SFXVolume"
    ///    (chuột phải vào Volume → Expose to Script → đặt tên đúng)
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(AudioPool))]
    public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────

    [Header("Audio Mixer (bắt buộc)")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("BGM Sources (tạo 2 child GameObject với AudioSource)")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    [Header("ENV Source (tạo 1 child GameObject với AudioSource)")]
    [SerializeField] private AudioSource envSource;

    [Header("Player - dùng để tính khoảng cách âm thanh")]
    [SerializeField] private Transform playerTransform;

    [Header("Sound Registry")]
    [SerializeField] private SoundRegistry soundRegistry;

    public SoundRegistry SoundRegistry => soundRegistry;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────

    private AudioPool       _pool;
    private MusicController _music;
    private VolumeSettings  _volume;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // Singleton pattern: hủy bản sao nếu đã có Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        _pool   = GetComponent<AudioPool>();
        _music  = new MusicController(bgmSourceA, bgmSourceB, this);
        _volume = new VolumeSettings(audioMixer);
        _volume.LoadAndApply();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(3f);
        if (playerTransform == null)
            Debug.LogWarning("[AudioManager] Player Transform chưa được gán! Âm thanh sẽ không suy giảm theo khoảng cách.");
    }

    // ─────────────────────────────────────────────
    //  BGM API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Phát nhạc nền với crossfade mượt.
    /// Gọi lại với clip mới để chuyển bài, bất kể đang phát bài nào.
    /// </summary>
    /// <param name="clip">AudioClip cần phát.</param>
    /// <param name="fadeDuration">Thời gian crossfade (giây).</param>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.5f)
        => _music.TransitionTo(clip, fadeDuration);

    /// <summary>Dừng nhạc nền với fade out.</summary>
    public void StopBGM(float fadeDuration = 1.0f)
        => _music.StopAll(fadeDuration);

    /// <summary>Trả về true nếu đang có nhạc nền phát.</summary>
    public bool IsBGMPlaying => _music.IsPlaying;

    // ─────────────────────────────────────────────
    //  ENV API
    // ─────────────────────────────────────────────

    /// <summary>Phát âm thanh môi trường (gió, mưa, sóng biển...).</summary>
    public void PlayENV(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        envSource.clip = clip;
        envSource.loop = loop;
        envSource.Play();
    }

    /// <summary>Dừng âm thanh môi trường.</summary>
    public void StopENV() => envSource.Stop();

    // ─────────────────────────────────────────────
    //  SFX API - One-shot tại vị trí cố định
    // ─────────────────────────────────────────────

    /// <summary>
    /// Phát SFX một lần tại vị trí thế giới.
    /// Âm lượng được tính theo khoảng cách X từ vị trí đó đến Player.
    /// Phù hợp cho: tiếng nhặt đồ, tiếng nổ, tiếng đánh.
    /// </summary>
    /// <param name="worldPosition">Vị trí phát âm thanh trong world space.</param>
    /// <param name="minDistance">Khoảng cách (trục X) mà âm thanh to tối đa (đơn vị Unity).</param>
    /// <param name="maxDistance">Khoảng cách (trục X) âm thanh hoàn toàn im.</param>
    /// <param name="volumeScale">Nhân thêm âm lượng (0-1).</param>
    public void PlaySFXAtPosition(
        AudioClip clip,
        Vector3   worldPosition,
        float     minDistance  = 2f,
        float     maxDistance  = 15f,
        float     volumeScale  = 1f)
    {
        if (clip == null) return;

        float volume = CalculateDistanceVolume(worldPosition, minDistance, maxDistance) * volumeScale;
        if (volume <= 0.001f) return; // Quá xa, không phát tiết kiệm tài nguyên

        AudioSource source = _pool.Get(worldPosition);
        source.clip   = clip;
        source.volume = volume;
        source.loop   = false;
        source.Play();

        StartCoroutine(ReturnAfterClip(source, clip.length));
    }

    // ─────────────────────────────────────────────
    //  SFX API - Bám theo Transform (NPC di chuyển)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Phát SFX bám theo Transform. Âm lượng cập nhật mỗi frame theo khoảng cách.
    /// Phù hợp cho: bước chân NPC, tiếng búa gõ của thợ rèn, tiếng nước chảy di chuyển.
    ///
    /// QUAN TRỌNG: Gọi .Stop() trên kết quả trả về khi NPC die/disable!
    /// Nếu không, AudioSource sẽ bị "mồ côi" trong pool.
    /// Tốt nhất: lưu handle vào biến và xử lý trong OnDisable().
    /// </summary>
    /// <returns>FollowingSound handle. Dùng để Stop() sau này.</returns>
    public FollowingSound PlaySFXFollowing(
        AudioClip clip,
        Transform target,
        bool      loop        = false,
        float     minDistance = 2f,
        float     maxDistance = 15f)
    {
        if (clip == null || target == null) return null;

        AudioSource source = _pool.Get(target.position);
        source.clip   = clip;
        source.volume = CalculateDistanceVolume(target.position, minDistance, maxDistance);
        source.loop   = loop;
        source.Play();

        var handle = new FollowingSound(source, target, this, minDistance, maxDistance);
        StartCoroutine(handle.UpdateRoutine());
        return handle;
    }

    // ─────────────────────────────────────────────
    //  SFX API - Key-based (SoundRegistry)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Phát SFX một lần tại vị trí thế giới bằng key từ SoundRegistry.
    /// </summary>
    /// <param name="soundKey">Key của âm thanh trong SoundRegistry.</param>
    /// <param name="worldPosition">Vị trí phát âm thanh trong world space.</param>
    /// <param name="minDistance">Khoảng cách (trục X) mà âm thanh to tối đa (đơn vị Unity).</param>
    /// <param name="maxDistance">Khoảng cách (trục X) âm thanh hoàn toàn im.</param>
    /// <param name="volumeScale">Nhân thêm âm lượng (0-1).</param>
    public void PlaySFXByKey(
        string soundKey,
        Vector3 worldPosition,
        float minDistance = 0f,
        float maxDistance = 0f,
        float volumeScale = 1f)
    {
        if (soundRegistry == null)
        {
            Debug.LogError("[AudioManager] SoundRegistry chưa được gán trong Inspector!");
            return;
        }

        var entry = soundRegistry.GetEntry(soundKey);
        if (entry == null)
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh với key: {soundKey}");
            return;
        }

        // Sử dụng khoảng cách mặc định từ SoundEntry nếu không được truyền
        float actualMinDist = minDistance > 0 ? minDistance : entry.minDistance;
        float actualMaxDist = maxDistance > 0 ? maxDistance : entry.maxDistance;

        PlaySFXAtPosition(entry.clip, worldPosition, actualMinDist, actualMaxDist, volumeScale);
    }

    /// <summary>
    /// Phát SFX bám theo Transform bằng key từ SoundRegistry.
    /// </summary>
    /// <param name="soundKey">Key của âm thanh trong SoundRegistry.</param>
    /// <param name="target">Transform cần bám theo.</param>
    /// <param name="loop">Có lặp lại không.</param>
    /// <param name="minDistance">Khoảng cách (trục X) mà âm thanh to tối đa.</param>
    /// <param name="maxDistance">Khoảng cách (trục X) âm thanh hoàn toàn im.</param>
    /// <returns>FollowingSound handle. Dùng để Stop() sau này.</returns>
    public FollowingSound PlaySFXFollowingByKey(
        string soundKey,
        Transform target,
        bool loop = false,
        float minDistance = 0f,
        float maxDistance = 0f)
    {
        if (soundRegistry == null)
        {
            Debug.LogError("[AudioManager] SoundRegistry chưa được gán trong Inspector!");
            return null;
        }

        var entry = soundRegistry.GetEntry(soundKey);
        if (entry == null)
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh với key: {soundKey}");
            return null;
        }

        // Sử dụng khoảng cách mặc định từ SoundEntry nếu không được truyền
        float actualMinDist = minDistance > 0 ? minDistance : entry.minDistance;
        float actualMaxDist = maxDistance > 0 ? maxDistance : entry.maxDistance;

        return PlaySFXFollowing(entry.clip, target, loop, actualMinDist, actualMaxDist);
    }

    // ─────────────────────────────────────────────
    //  Volume API
    // ─────────────────────────────────────────────

    public void SetMasterVolume(float value) => _volume.SetVolume(VolumeSettings.PARAM_MASTER, value);
    public void SetBGMVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_BGM, value);
    public void SetENVVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_ENV, value);
    public void SetSFXVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_SFX, value);

    public float GetMasterVolume() => _volume.GetVolume(VolumeSettings.PARAM_MASTER);
    public float GetBGMVolume()    => _volume.GetVolume(VolumeSettings.PARAM_BGM);
    public float GetENVVolume()    => _volume.GetVolume(VolumeSettings.PARAM_ENV, 0.7f);
    public float GetSFXVolume()    => _volume.GetVolume(VolumeSettings.PARAM_SFX);

    public void ResetAllVolumes()  => _volume.ResetToDefaults();

    // ─────────────────────────────────────────────
    //  Runtime Configuration
    // ─────────────────────────────────────────────

    /// <summary>
    /// Cập nhật tham chiếu đến Player. Gọi khi Player được spawn vào scene.
    /// Ví dụ: AudioManager.Instance.SetPlayerTransform(player.transform);
    /// </summary>
    public void SetPlayerTransform(Transform playerTf) => playerTransform = playerTf;

    // ─────────────────────────────────────────────
    //  Distance Volume Calculation
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tính âm lượng (0-1) dựa trên khoảng cách TRỤC X giữa nguồn âm và Player.
    ///
    /// Lý do chỉ dùng trục X (không phải Vector3.Distance):
    ///   Game 2D side-scroll như Kingdom Two Crowns chỉ có ý nghĩa khoảng cách ngang.
    ///   Dùng Vector3 full sẽ tạo âm thanh lạ khi NPC và Player cùng X nhưng khác Z/Y.
    ///
    /// Đường cong: sqrt (căn bậc hai) → gần với Logarithmic Rolloff tự nhiên,
    ///   mượt hơn Linear nhưng không "rụng" đột ngột như Logarithmic của Unity.
    /// </summary>
    public float CalculateDistanceVolume(Vector3 sourcePos, float minDist, float maxDist)
    {
        if (playerTransform == null) return 1f;

        float distX = Mathf.Abs(sourcePos.x - playerTransform.position.x);

        if (distX <= minDist) return 1f;
        if (distX >= maxDist) return 0f;

        float t = (distX - minDist) / (maxDist - minDist); // 0 → 1 khi đi từ min đến max
        return 1f - Mathf.Sqrt(t);                          // Logarithmic-like curve
    }

    // ─────────────────────────────────────────────
    //  Internal (dùng bởi FollowingSound)
    // ─────────────────────────────────────────────

    /// <summary>Trả AudioSource về pool. Gọi nội bộ bởi FollowingSound.Stop().</summary>
    internal void ReturnToPool(AudioSource source) => _pool.Return(source);

    // ─────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────

    /// <summary>Chờ clip phát xong thì trả AudioSource về pool.</summary>
    private IEnumerator ReturnAfterClip(AudioSource source, float clipLength)
    {
        // Chờ thêm 0.15s buffer để tránh cắt tiếng đột ngột
        yield return new WaitForSeconds(clipLength + 0.15f);

        // Double-check: nếu source đã được pool trả lại (Return() gọi từ nơi khác), bỏ qua
        if (source != null && !source.isPlaying)
            _pool.Return(source);
    }
}
}
