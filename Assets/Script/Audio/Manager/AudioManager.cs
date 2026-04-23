using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Script.Audio
{
    /// <summary>
    /// Enum định nghĩa các audio channel trong hệ thống.
    /// </summary>
    public enum AudioChannel
    {
        Master,
        BGM,
        ENV,
        SFX,
        UI
    }

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
    public class AudioManager : MonoBehaviour, IAudioSourceProvider
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

    [Header("UI Source (tạo 1 child GameObject với AudioSource)")]
    [SerializeField] private AudioSource uiSource;

    [Header("Player - dùng để tính khoảng cách âm thanh")]
    [SerializeField] private Transform playerTransform;

    public Transform PlayerTransform
    {
        get => playerTransform;
        set => playerTransform = value;
    }

    [Header("Distance Culling")]
    [Tooltip("Khoảng cách tối đa để phát âm thanh xa player (như Minecraft lazy chunk)")]
    [SerializeField] private float cullingDistance = 50f;

    [Header("Debug")]
    [Tooltip("Bật/tắt debug log trong Console")]
    [SerializeField] private bool isDebug = false;

    public bool IsDebug => isDebug;
    public float CullingDistance => cullingDistance;

    [Header("Sound Registry")]
    [SerializeField] private SoundRegistry soundRegistry;

    public SoundRegistry SoundRegistry => soundRegistry;
    public AudioPool Pool => _pool;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────

    private AudioPool _pool;
    private MusicController _music;
    private VolumeSettings _volume;

    // ─────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────

    public VolumeSettings VolumeSettings => _volume;
    public bool IsPaused { get; private set; }

    [Header("Debug - Current Volume Values (Read Only)")]
    [SerializeField] private float _masterVolume;
    [SerializeField] private float _bgmVolume;
    [SerializeField] private float _envVolume;
    [SerializeField] private float _sfxVolume;
    [SerializeField] private float _uiVolume;

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

        // Load settings ngay lập tức để Emitters có thể truy cập trong Start/Awake của chúng
        if (_volume != null)
            {
                _volume.LoadAndApply();
                if (isDebug) Debug.Log("[AudioManager] Volume and Settings applied in Awake");

                // Đọc giá trị từ PlayerPrefs vào debug fields
                _masterVolume = _volume.GetVolume(VolumeSettings.PARAM_MASTER);
                _bgmVolume    = _volume.GetVolume(VolumeSettings.PARAM_BGM);
                _envVolume    = _volume.GetVolume(VolumeSettings.PARAM_ENV);
                _sfxVolume    = _volume.GetVolume(VolumeSettings.PARAM_SFX);
                _uiVolume     = _volume.GetVolume(VolumeSettings.PARAM_UI);
            }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Khi load scene mới, thường chúng ta muốn dọn dẹp các sound cũ đang dangling
        // User yêu cầu "pause", nhưng trong thực tế chuyển scene nên là ReturnAll để giải phóng pool
        if (isDebug) Debug.Log($"[AudioManager] Scene Loaded: {scene.name}. Returning all non-UI sounds to pool.");
        
        // Dọn dẹp BGM/ENV nếu cần (tùy game, ở đây giả định dọn sạch)
        // StopBGM(0.5f); 
        StopENV();
        
        if (_pool != null)
            _pool.ReturnAll();
        
        IsPaused = false;
    }


    private IEnumerator Start()
    {
        // Chờ để check playerTransform
        yield return new WaitForSeconds(AudioConstants.PLAYER_CHECK_DELAY);
        if (playerTransform == null)
            if (isDebug) Debug.LogWarning("[AudioManager] Player Transform chưa được gán! Âm thanh sẽ không suy giảm theo khoảng cách.");
    }

    private void Initialize()
    {
        _pool   = GetComponent<AudioPool>();
        _music  = new MusicController(bgmSourceA, bgmSourceB, this, () => _bgmVolume);
        _volume = new VolumeSettings(audioMixer);

        // MONO: Set tất cả AudioSources thành 2D (mono)
        SetupMonoSource(bgmSourceA);
        SetupMonoSource(bgmSourceB);
        SetupMonoSource(envSource);
        SetupMonoSource(uiSource);

        // Auto-set cullingDistance từ maxDistance lớn nhất trong SoundRegistry
        AutoSetCullingDistanceFromRegistry();
    }

    private void SetupMonoSource(AudioSource source)
    {
        if (source != null)
        {
            source.spatialBlend = 0f; // 2D mono
            source.panStereo = 0f;    // Center
        }
    }

    private void AutoSetCullingDistanceFromRegistry()
    {
        if (soundRegistry == null) return;

        float maxDistanceFound = 0f;

        // Tìm maxDistance lớn nhất từ tất cả SoundEntry
        var allKeys = soundRegistry.GetAllKeys();
        foreach (var key in allKeys)
        {
            var entry = soundRegistry.GetEntry(key);
            if (entry != null && entry.maxDistance > maxDistanceFound)
            {
                maxDistanceFound = entry.maxDistance;
            }
        }

        // Set cullingDistance = maxDistance lớn nhất + offset
        if (maxDistanceFound > 0f)
        {
            cullingDistance = maxDistanceFound + AudioConstants.CULLING_DISTANCE_OFFSET;
            if (isDebug) Debug.Log($"[AudioManager] Auto-set cullingDistance to {cullingDistance}f (max distance from SoundRegistry + {AudioConstants.CULLING_DISTANCE_OFFSET}f offset)");
        }
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
        envSource.volume = _envVolume; // Dùng volume từ field
        envSource.Play();
    }

    /// <summary>Dừng âm thanh môi trường.</summary>
    public void StopENV() => envSource.Stop();

    // ─────────────────────────────────────────────
    //  Pause API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tạm dừng/Tiếp tục toàn bộ âm thanh (trừ UI).
    /// Dùng khi Pause Game hoặc chuyển Scene.
    /// </summary>
    public void PauseNonUISounds(bool pause)
    {
        if (isDebug) Debug.Log($"[AudioManager] PauseNonUISounds: {pause}");
        IsPaused = pause;

        // 1. BGM
        if (pause) bgmSourceA.Pause(); else bgmSourceA.UnPause();
        if (pause) bgmSourceB.Pause(); else bgmSourceB.UnPause();

        // 2. ENV
        if (pause) envSource.Pause(); else envSource.UnPause();

        // 3. Pooled SFX
        if (_pool != null)
            _pool.PauseAll(pause);
    }

    // ─────────────────────────────────────────────
    //  Mono Runtime Config
    // ─────────────────────────────────────────────

    /// <summary>
    /// Áp dụng chế độ Mono cho mọi âm thanh đang phát trong hệ thống.
    /// Gọi đồng bộ từ VolumeSettings sau khi load hoặc đổi cài đặt.
    /// </summary>
    public void ApplyMonoAudioGlobally(bool isMono)
    {
        if (isDebug) Debug.Log($"[AudioManager] ApplyMonoAudioGlobally: {isMono}");

        // Cập nhật cho tất cả AudioEmitter trong scene
        var emitters = AudioEmitterRegistry.All;
        for (int i = 0; i < emitters.Count; i++)
        {
            if (emitters[i] != null)
                emitters[i].ApplyMonoSetting(isMono);
        }

        // Với các âm thanh ngắn gọn SFX do AudioManager tự quản (One-Shot), do quá nhanh nên không cần restart
        // Nếu cần có thể bổ sung update cho active handle trong AudioManager sau.
    }


    // ─────────────────────────────────────────────
    //  UI API - 2D sound, không distance culling
    // ─────────────────────────────────────────────

    /// <summary>
    /// Phát âm thanh UI (button click, menu navigation...).
    /// Luôn phát, không có distance culling, 2D sound.
    /// Phù hợp cho: UI button clicks, menu sounds.
    /// </summary>
    /// <param name="soundKey">Key của âm thanh trong SoundRegistry.</param>
    /// <param name="volumeScale">Nhân thêm âm lượng (0-1).</param>
    public void PlayUISound(string soundKey, float volumeScale = 1f)
    {
        if (soundRegistry == null)
        {
            if (isDebug) Debug.LogError("[AudioManager] SoundRegistry chưa được gán trong Inspector!");
            return;
        }

        var entry = soundRegistry.GetEntry(soundKey);
        if (entry == null)
        {
            if (isDebug) Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh với key: {soundKey}");
            return;
        }

        if (uiSource == null)
        {
            if (isDebug) Debug.LogError("[AudioManager] uiSource chưa được gán trong Inspector!");
            return;
        }

        if (isDebug) Debug.Log($"[AudioManager] MixerGroup mixer ID: {uiSource.outputAudioMixerGroup?.audioMixer.GetInstanceID()}");
        if (isDebug && uiSource.outputAudioMixerGroup?.audioMixer != null && uiSource.outputAudioMixerGroup.audioMixer.GetFloat("UIVolume", out float actual))
            Debug.Log($"[AudioManager] Actual UIVolume in mixer: {actual} dB");

        uiSource.clip = entry.clip;
        uiSource.volume = volumeScale * _uiVolume; // Nhân với volume từ field
        uiSource.spatialBlend = 0f; // 2D sound
        uiSource.loop = false;
        uiSource.Play();
    }

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
    /// <param name="bypassCulling">Bỏ qua distance culling (cho âm thanh quan trọng).</param>
    public void PlaySFXAtPosition(
        AudioClip clip,
        Vector3   worldPosition,
        float     minDistance  = 2f,
        float     maxDistance  = 15f,
        float     volumeScale  = 1f,
        bool      bypassCulling = false)
    {
        if (clip == null) return;

        // Check distance TRƯỚC khi xin pool để tối ưu
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, worldPosition);

            // Distance culling: dùng max của cullingDistance và maxDistance
            // Để sound với maxDistance lớn vẫn được phát ngay cả khi xa
            float effectiveCullingDistance = Mathf.Max(cullingDistance, maxDistance);
            if (!bypassCulling && distance > effectiveCullingDistance)
            {
                return; // Quá xa, không phát
            }

            // Kiểm tra nếu quá xa maxDistance, không phát để tiết kiệm tài nguyên
            if (distance > maxDistance) return;
        }

        AudioSource source = _pool.Get(worldPosition);
        if (source == null) return; // Pool đã hết (đạt maxPoolSize)

        bool isMono = _volume != null && _volume.GetMonoAudio();
        source.clip = clip;
        source.loop = false;

        if (isMono)
        {
            source.spatialBlend = 0f;
            source.panStereo = 0f;
            source.volume = volumeScale * CalculateDistanceVolume(worldPosition, minDistance, maxDistance);
        }
        else
        {
            source.spatialBlend = 1f;
            source.panStereo = 0f;
            source.volume = volumeScale;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;

            // Tạo custom rolloff curve: full volume trong min, 30% volume tại max
            AnimationCurve rolloffCurve = new AnimationCurve();
            rolloffCurve.AddKey(0f, 1f);
            rolloffCurve.AddKey(minDistance, 1f);
            rolloffCurve.AddKey(maxDistance, 0.3f);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }

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
        source.clip = clip;
        source.loop = loop;

        bool isMono = _volume != null && _volume.GetMonoAudio();
        if (isMono)
        {
            source.spatialBlend = 0f;
            source.panStereo = 0f;
        }
        else
        {
            source.spatialBlend = 1f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            
            // Reapply curve since it's stereof
            AnimationCurve rolloffCurve = new AnimationCurve();
            rolloffCurve.AddKey(0f, 1f);
            rolloffCurve.AddKey(minDistance, 1f);
            rolloffCurve.AddKey(maxDistance, 0.3f);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }
        
        // volume base calculation
        source.volume = CalculateDistanceVolume(target.position, minDistance, maxDistance);

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
    /// <param name="bypassCulling">Bỏ qua distance culling (cho âm thanh quan trọng).</param>
    public void PlaySFXByKey(
        string soundKey,
        Vector3 worldPosition,
        float minDistance = 0f,
        float maxDistance = 0f,
        float volumeScale = 1f,
        bool bypassCulling = false)
    {
        if (soundRegistry == null)
        {
            if (isDebug) Debug.LogError("[AudioManager] SoundRegistry chưa được gán trong Inspector!");
            return;
        }

        var entry = soundRegistry.GetEntry(soundKey);
        if (entry == null)
        {
            if (isDebug) Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh với key: {soundKey}");
            return;
        }

        // Sử dụng khoảng cách mặc định từ SoundEntry nếu không được truyền
        float actualMinDist = minDistance > 0 ? minDistance : entry.minDistance;
        float actualMaxDist = maxDistance > 0 ? maxDistance : entry.maxDistance;

        PlaySFXAtPosition(entry.clip, worldPosition, actualMinDist, actualMaxDist, volumeScale, bypassCulling);
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
            if (isDebug) Debug.LogError("[AudioManager] SoundRegistry chưa được gán trong Inspector!");
            return null;
        }

        var entry = soundRegistry.GetEntry(soundKey);
        if (entry == null)
        {
            if (isDebug) Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh với key: {soundKey}");
            return null;
        }

        // Sử dụng khoảng cách mặc định từ SoundEntry nếu không được truyền
        float actualMinDist = minDistance > 0 ? minDistance : entry.minDistance;
        float actualMaxDist = maxDistance > 0 ? maxDistance : entry.maxDistance;

        return PlaySFXFollowing(entry.clip, target, loop, actualMinDist, actualMaxDist);
    }

    // ─────────────────────────────────────────────
    //  Pool API - cho AudioEmitter
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lấy AudioSource từ pool (dùng bởi AudioEmitter).
    /// </summary>
    public AudioSource GetAudioSourceFromPool(Vector3 worldPosition)
    {
        return _pool.Get(worldPosition);
    }

    /// <summary>
    /// Trả AudioSource về pool (dùng bởi AudioEmitter).
    /// </summary>
    public void ReturnAudioSourceToPool(AudioSource source)
    {
        _pool.Return(source);
    }

    // ─────────────────────────────────────────────
    //  IAudioSourceProvider Implementation
    // ─────────────────────────────────────────────

    /// <summary>Lấy AudioSource từ pool (interface implementation).</summary>
    AudioSource IAudioSourceProvider.Rent(Vector3 worldPosition)
    {
        return _pool.Get(worldPosition);
    }

    /// <summary>Trả AudioSource về pool (interface implementation).</summary>
    void IAudioSourceProvider.Return(AudioSource source)
    {
        _pool.Return(source);
    }

    /// <summary>Lấy SoundEntry từ registry (interface implementation).</summary>
    SoundRegistry.SoundEntry IAudioSourceProvider.GetEntry(string soundKey)
    {
        if (soundRegistry == null) return null;
        return soundRegistry.GetEntry(soundKey);
    }

    /// <summary>
    /// Tính volume dựa trên khoảng cách (interface implementation).
    /// Dùng bởi AudioEmitter khi Mono mode để áp volume thủ công.
    /// </summary>
    float IAudioSourceProvider.CalculateDistanceVolume(Vector3 sourcePos, float minDist, float maxDist)
        => CalculateDistanceVolume(sourcePos, minDist, maxDist);

    /// <summary>Trả về trạng thái Mono Audio hiện tại (interface implementation).</summary>
    bool IAudioSourceProvider.GetMonoAudio()
        => _volume != null && _volume.GetMonoAudio();

    // ─────────────────────────────────────────────
    //  Volume API
    // ─────────────────────────────────────────────

    public void SetMasterVolume(float value) => _volume.SetVolume(VolumeSettings.PARAM_MASTER, value);
    public void SetBGMVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_BGM, value);
    public void SetENVVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_ENV, value);
    public void SetSFXVolume(float value)    => _volume.SetVolume(VolumeSettings.PARAM_SFX, value);
    public void SetUIVolume(float value)     => _volume.SetVolume(VolumeSettings.PARAM_UI, value);

    public float GetMasterVolume() => _masterVolume;
    public float GetBGMVolume()    => _bgmVolume;
    public float GetENVVolume()    => _envVolume;
    public float GetSFXVolume()    => _sfxVolume;
    public float GetUIVolume()     => _uiVolume;

    public void ResetAllVolumes()  => _volume.ResetToDefaults();

    /// <summary>
    /// Helper method để lấy volume theo channel enum (dùng bởi UI components).
    /// </summary>
    public float GetVolumeForChannel(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Master => GetMasterVolume(),
            AudioChannel.BGM    => GetBGMVolume(),
            AudioChannel.ENV    => GetENVVolume(),
            AudioChannel.SFX    => GetSFXVolume(),
            AudioChannel.UI     => GetUIVolume(),
            _ => 1f
        };
    }

    /// <summary>
    /// Helper method để set volume theo channel enum (dùng bởi UI components).
    /// </summary>
    public void SetVolumeForChannel(AudioChannel channel, float value)
    {
        switch (channel)
        {
            case AudioChannel.Master: SetMasterVolume(value); _masterVolume = value; break;
            case AudioChannel.BGM:    SetBGMVolume(value);    _bgmVolume = value;    break;
            case AudioChannel.ENV:    SetENVVolume(value);    _envVolume = value;    break;
            case AudioChannel.SFX:    SetSFXVolume(value);    _sfxVolume = value;    break;
            case AudioChannel.UI:     SetUIVolume(value);     _uiVolume = value;     break;
        }
    }

    /// <summary>
    /// Cập nhật debug volume values trong Inspector.
    /// </summary>
    [ContextMenu("Refresh Debug Volumes")]
    public void RefreshDebugVolumes()
    {
        _masterVolume = GetMasterVolume();
        _bgmVolume    = GetBGMVolume();
        _envVolume    = GetENVVolume();
        _sfxVolume    = GetSFXVolume();
        _uiVolume     = GetUIVolume();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ─────────────────────────────────────────────
    //  Debug
    // ─────────────────────────────────────────────

    /// <summary>
    /// Debug: Kiểm tra mixer instance và actual volume value.
    /// Có thể gọi từ Inspector (ContextMenu).
    /// </summary>
    [ContextMenu("Debug: Audio Mixer")]
    public void DebugAudioMixer()
    {
        // Check 1: Mixer instance match
        var sourceMixer = uiSource?.outputAudioMixerGroup?.audioMixer;
        if (isDebug) Debug.Log($"[AudioManager] VolumeSettings mixer: {_volume.GetMixer()?.name} (ID: {_volume.GetMixer()?.GetInstanceID()})");
        if (isDebug) Debug.Log($"[AudioManager] uiSource mixer: {sourceMixer?.name} (ID: {sourceMixer?.GetInstanceID()})");

        // Check 2: Actual value in mixer
        if (sourceMixer != null && sourceMixer.GetFloat("UIVolume", out float actual))
            if (isDebug) Debug.Log($"[AudioManager] Actual UIVolume in mixer: {actual} dB");
        else
            if (isDebug) Debug.LogError("[AudioManager] Cannot read UIVolume from mixer!");
    }

    /// <summary>
    /// Debug: Log toàn bộ sound trong SoundRegistry ra Console.
    /// Có thể gọi từ Inspector (ContextMenu).
    /// </summary>
    [ContextMenu("Debug: Log SoundRegistry")]
    public void DebugLogSoundRegistry()
    {
        if (soundRegistry == null)
        {
            if (isDebug) Debug.LogError("[AudioManager] SoundRegistry is null!");
            return;
        }

        var allKeys = soundRegistry.GetAllKeys();
        var logBuilder = new System.Text.StringBuilder();
        logBuilder.AppendLine($"[AudioManager] SoundRegistry Debug - Total sounds: {allKeys.Length}");

        foreach (var key in allKeys)
        {
            var entry = soundRegistry.GetEntry(key);
            if (entry != null)
            {
                string clipName = entry.clip != null ? entry.clip.name : "null";
                logBuilder.AppendLine($"  - {key} | Clip: {clipName} | Min: {entry.minDistance}m | Max: {entry.maxDistance}m");
            }
            else
            {
                logBuilder.AppendLine($"  - {key} | NULL ENTRY");
            }
        }

        if (isDebug) Debug.Log(logBuilder.ToString());
    }

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
        // Chờ thêm buffer để tránh cắt tiếng đột ngột
        yield return new WaitForSeconds(clipLength + AudioConstants.BUFFER_TIME);

        // Force return về pool dù source có đang play hay không
        // để tránh leak nếu source bị stuck
        if (source != null)
        {
            if (source.isPlaying)
                source.Stop();
            _pool.Return(source);
        }
    }
}
}
