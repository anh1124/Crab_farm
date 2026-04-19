using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Component âm thanh gắn lên từng GameObject (enemy, NPC, campfire, VFX...).
    /// Tự đăng ký vào AudioEmitterRegistry khi Enable, tự hủy khi Disable.
    /// Không cần Collider — PlayerAudioZone poll Registry mỗi 0.2s để set range.
    ///
    /// VÒNG ĐỜI CỦA MỘT SOUND:
    ///   PlayerAudioZone → SetInRange(true) → OnRangeChanged fired
    ///   → Behavior lắng nghe → PlaySound("key", loop: true)
    ///   → Player đi xa → SetInRange(false) → StopAll() tự động
    /// </summary>
    public class AudioEmitter : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        //  Internal Types
        // ─────────────────────────────────────────────

        /// <summary>
        /// Handle gom nhóm state của một sound đang active.
        /// Thay thế 3 dictionary song song (_activeSounds, _loopFlags, _activeCoroutines).
        /// </summary>
        private readonly struct ActiveSoundHandle
        {
            public readonly AudioSource Source;
            public readonly bool IsLoop;
            public readonly Coroutine ReturnCoroutine;

            public ActiveSoundHandle(AudioSource source, bool isLoop, Coroutine returnCoroutine)
            {
                Source = source;
                IsLoop = isLoop;
                ReturnCoroutine = returnCoroutine;
            }
        }

        // ─────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────

        /// <summary>
        /// Fired khi trạng thái in/out range thay đổi.
        /// Behavior scripts lắng nghe event này để bật/tắt loop sound.
        /// true  = vào vùng nghe → nên bắt đầu phát
        /// false = ra ngoài vùng → AudioEmitter đã StopAll(), không cần làm gì thêm
        /// </summary>
        public event Action<bool> OnRangeChanged;

        // ─────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────

        private bool _isInRange = false;

        // soundKey → ActiveSoundHandle gom nhóm AudioSource, loop flag, và coroutine
        private readonly Dictionary<string, ActiveSoundHandle> _activeSounds = new();
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private const float MIN_PLAY_INTERVAL = 0.1f; // Minimum time between same sound plays

        // Demand queue: soundKey → PlaySoundConfig (chỉ loop sounds)
        // PlayerAudioZone sẽ kích hoạt play khi emitter trong range
        private readonly Dictionary<string, PlaySoundConfig> _demandedSounds = new();

        // Dependency injection cho audio source provider
        private IAudioSourceProvider _provider;

        /// <summary>
        /// Setup provider qua dependency injection.
        /// Nếu không gọi, sẽ fallback về AudioManager.Instance (backward compatible).
        /// </summary>
        public void Setup(IAudioSourceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Provider với fallback: dùng injected provider hoặc AudioManager.Instance.
        /// </summary>
        private IAudioSourceProvider Provider
            => _provider ?? AudioManager.Instance as IAudioSourceProvider;

        // ─────────────────────────────────────────────
        //  Debug
        // ─────────────────────────────────────────────

        [Header("Debug (read-only)")]
        [SerializeField] private bool _debugInRange;

#if UNITY_EDITOR
        /// <summary>Debug: danh sách sound keys đang active (Editor-only, auto-sync)</summary>
        public IEnumerable<string> DebugActiveKeys => _activeSounds.Keys;
#endif

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void OnEnable()
        {
            AudioEmitterRegistry.Register(this);
        }

        private void OnDisable()
        {
            AudioEmitterRegistry.Unregister(this);
            StopAll(); // Trả hết pool khi obj disable
        }

        private void OnDestroy()
        {
            // OnDisable đã gọi StopAll() trước OnDestroy trong Unity
            // Unregister lại để chắc chắn không còn reference
            AudioEmitterRegistry.Unregister(this);
        }

        // ─────────────────────────────────────────────
        //  Range API — gọi bởi PlayerAudioZone
        // ─────────────────────────────────────────────

        /// <summary>
        /// Được gọi bởi PlayerAudioZone mỗi 0.1s.
        /// Chỉ fire event khi trạng thái thực sự thay đổi (tránh spam).
        /// </summary>
        public void SetInRange(bool inRange)
        {
            if (_isInRange == inRange) return; // Không đổi → bỏ qua

            _isInRange = inRange;
            _debugInRange = inRange; // Inspector debug

            if (!inRange)
            {
                // Ra vùng → dừng tất cả active sounds (nhưng giữ demand queue)
                StopAllActiveSounds();
            }
            else
            {
                // Vào vùng → kích hoạt tất cả demanded loop sounds
                foreach (var soundKey in _demandedSounds.Keys)
                {
                    TriggerDemandedPlay(soundKey);
                }
            }

            OnRangeChanged?.Invoke(inRange);
        }

        /// <summary>
        /// Force set inRange = true (cho AudioTest hoặc debug).
        /// Cẩn thận: chỉ dùng khi biết chắc chắn player gần emitter.
        /// </summary>
        public void ForceInRange()
        {
            SetInRange(true);
        }

        /// <summary>Kiểm tra emitter có trong vùng nghe không.</summary>
        public bool IsInRange => _isInRange;

        // ─────────────────────────────────────────────
        //  Sound API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Phát âm thanh theo soundKey từ SoundRegistry (API mới với config).
        ///
        /// LOOP SOUND (lửa trại, bước chân...):
        ///   PlaySound("Campfire_Crackle", new PlaySoundConfig(loop: true))
        ///   → Tồn tại đến khi StopSound() hoặc SetInRange(false)
        ///
        /// ONE-SHOT (tiếng đánh, tiếng nhặt đồ...):
        ///   PlaySound("Sword_Hit")
        ///   → Tự động trả pool khi clip xong
        /// </summary>
        /// <param name="soundKey">Key trong SoundRegistry.</param>
        /// <param name="config">Config tùy chọn (default = PlaySoundConfig.Default).</param>
        public void PlaySound(string soundKey, PlaySoundConfig config = default)
        {
            // Guard: Provider chưa sẵn sàng
            if (Provider == null)
            {
                Debug.LogError("[AudioEmitter] Provider is null!");
                return;
            }

            // Guard: Chặn spam cùng sound quá nhanh (dưới 0.1s)
            float currentTime = Time.time;
            if (_lastPlayTimes.TryGetValue(soundKey, out float lastPlayTime))
            {
                if (currentTime - lastPlayTime < MIN_PLAY_INTERVAL)
                {
                    return; // Quá sát nhau, bỏ qua
                }
            }

            // Loop sound: chỉ đăng ký demand, không play ngay
            if (config.Loop)
            {
                _demandedSounds[soundKey] = config;
                // Nếu đang trong range, kích hoạt play ngay
                if (_isInRange)
                {
                    TriggerDemandedPlay(soundKey);
                }
                return;
            }

            // One-shot sound: play ngay lập tức
            PlayOneShot(soundKey, config);

            // Update last play time
            _lastPlayTimes[soundKey] = Time.time;
        }

        /// <summary>Phát one-shot sound ngay lập tức (không demand).</summary>
        private void PlayOneShot(string soundKey, PlaySoundConfig config)
        {
            var entry = Provider.GetEntry(soundKey);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioEmitter] Key không tồn tại: '{soundKey}'");
                return;
            }

            float actualMin = config.MinDistance > 0f ? config.MinDistance : entry.minDistance;
            float actualMax = config.MaxDistance > 0f ? config.MaxDistance : entry.maxDistance;

            // Xin pool
            AudioSource source = Provider.Rent(transform.position);
            if (source == null)
            {
                Debug.LogWarning("[AudioEmitter] Pool đã hết!");
                return;
            }

            // Cấu hình source
            source.clip         = entry.clip;
            source.volume       = config.VolumeScale;
            source.minDistance  = actualMin;
            source.maxDistance  = actualMax;
            source.loop         = false;
            source.spatialBlend = 1f;

            AnimationCurve curve = AudioCurveCache.GetDefaultRolloffCurve(actualMin, actualMax, AudioConstants.DEFAULT_VOLUME_AT_MAX);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

            // Bám theo transform (vẫn di chuyển cùng obj)
            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            // Lưu state gom nhóm trong ActiveSoundHandle
            string instanceId = $"{soundKey}_{Guid.NewGuid()}";
            Coroutine returnCoroutine = StartCoroutine(ReturnAfterClip(instanceId, source, entry.clip.length));

            _activeSounds[instanceId] = new ActiveSoundHandle(source, false, returnCoroutine);

            source.Play();
        }

        /// <summary>Kích hoạt play demanded sound (gọi bởi PlayerAudioZone khi vào range).</summary>
        private void TriggerDemandedPlay(string soundKey)
        {
            if (!_demandedSounds.TryGetValue(soundKey, out var config)) return;
            if (_activeSounds.ContainsKey(soundKey)) return; // Đang play rồi

            var entry = Provider.GetEntry(soundKey);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioEmitter] Key không tồn tại: '{soundKey}'");
                return;
            }

            float actualMin = config.MinDistance > 0f ? config.MinDistance : entry.minDistance;
            float actualMax = config.MaxDistance > 0f ? config.MaxDistance : entry.maxDistance;

            AudioSource source = Provider.Rent(transform.position);
            if (source == null)
            {
                Debug.LogWarning("[AudioEmitter] Pool đã hết!");
                return;
            }

            // Cấu hình source
            source.clip         = entry.clip;
            source.volume       = config.VolumeScale;
            source.minDistance  = actualMin;
            source.maxDistance  = actualMax;
            source.loop         = true;
            source.spatialBlend = 1f;

            AnimationCurve curve = AudioCurveCache.GetDefaultRolloffCurve(actualMin, actualMax, AudioConstants.DEFAULT_VOLUME_AT_MAX);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            _activeSounds[soundKey] = new ActiveSoundHandle(source, true, null);

            source.Play();
        }

        /// <summary>Dừng âm thanh cụ thể và trả pool về.</summary>
        public void StopSound(string soundKey)
        {
            // Find all instances that start with the soundKey prefix
            var keysToRemove = _activeSounds.Keys.Where(k => k.StartsWith(soundKey)).ToList();

            foreach (var key in keysToRemove)
            {
                if (!_activeSounds.TryGetValue(key, out var handle)) continue;

                // Dừng coroutine nếu đang chạy (chỉ có cho one-shot)
                if (handle.ReturnCoroutine != null)
                {
                    StopCoroutine(handle.ReturnCoroutine);
                }

                // Dừng AudioSource
                if (handle.Source != null)
                {
                    handle.Source.Stop();
                    Provider?.Return(handle.Source);
                }

                // Xóa khỏi dict
                _activeSounds.Remove(key);
            }

            // Xóa khỏi demand queue nếu là loop sound
            _demandedSounds.Remove(soundKey);
        }

        /// <summary>Dừng tất cả âm thanh đang active (không clear demand queue).</summary>
        private void StopAllActiveSounds()
        {
            var keys = new List<string>(_activeSounds.Keys);
            foreach (var key in keys)
                StopSound(key);
        }

        /// <summary>Dừng tất cả âm thanh và xóa demand queue.</summary>
        public void StopAll()
        {
            StopAllActiveSounds();
            _demandedSounds.Clear();
        }

        /// <summary>Kiểm tra âm thanh đang phát theo key.</summary>
        public bool IsPlaying(string soundKey)
            => _activeSounds.Keys.Any(k => k.StartsWith(soundKey) &&
                _activeSounds.TryGetValue(k, out var handle) &&
                handle.Source != null &&
                handle.Source.isPlaying);

        /// <summary>Số âm thanh đang phát.</summary>
        public int ActiveSoundCount => _activeSounds.Count;

        // ─────────────────────────────────────────────
        //  Internal
        // ─────────────────────────────────────────────

        private IEnumerator ReturnAfterClip(string soundKey, AudioSource source, float clipLength)
        {
            // Buffer nhỏ để tránh cắt tiếng đột ngột
            yield return new WaitForSeconds(clipLength + AudioConstants.BUFFER_TIME);

            // Chỉ trả pool nếu âm thanh vẫn còn trong dict (chưa bị StopSound())
            if (_activeSounds.ContainsKey(soundKey))
            {
                StopSound(soundKey);
            }
        }
    }
}
