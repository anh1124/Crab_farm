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
    /// Không cần Collider — PlayerAudioZone poll Registry mỗi 0.3s để set range.
    ///
    /// VÒNG ĐỜI CỦA MỘT SOUND:
    ///   PlayerAudioZone → SetInRange(true) → OnRangeChanged fired
    ///   → Behavior lắng nghe → PlaySound("key", loop: true)
    ///   → Player đi xa → SetInRange(false) → StopAll() tự động
    ///
    /// MONO AUDIO:
    ///   Khi isMono=true: spatialBlend=0f (2D) + manual volume dựa trên khoảng cách.
    ///   KHÔNG dùng spread=360 vì Unity vẫn dùng vị trí 3D để tính panning trái/phải.
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
            public readonly Coroutine ReturnCoroutine; // one-shot: return timer | mono loop: volume updater

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
        /// true  = vào vùng nghe → nên bắt đầu phát
        /// false = ra ngoài vùng → AudioEmitter đã StopAllActiveSounds(), không cần làm gì thêm
        /// </summary>
        public event Action<bool> OnRangeChanged;

        // ─────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────

        private bool _isInRange = false;

        // soundKey → ActiveSoundHandle gom nhóm AudioSource, loop flag, và coroutine
        private readonly Dictionary<string, ActiveSoundHandle> _activeSounds = new();
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private const float MIN_PLAY_INTERVAL = 0.1f;

        // Demand queue: soundKey → PlaySoundConfig (chỉ loop sounds)
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
            AudioEmitterRegistry.Unregister(this);
        }

        // ─────────────────────────────────────────────
        //  Range API — gọi bởi PlayerAudioZone
        // ─────────────────────────────────────────────

        /// <summary>
        /// Được gọi bởi PlayerAudioZone mỗi 0.3s.
        /// Chỉ fire event khi trạng thái thực sự thay đổi (tránh spam).
        /// </summary>
        public void SetInRange(bool inRange)
        {
            if (_isInRange == inRange) return;

            _isInRange = inRange;
            _debugInRange = inRange;

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
        /// Phát âm thanh theo soundKey từ SoundRegistry.
        ///
        /// LOOP SOUND (lửa trại, portal...):
        ///   PlaySound("Campfire_Crackle", new PlaySoundConfig(loop: true))
        ///   → Đăng ký demand, chỉ tiêu AudioSource khi Player trong vùng.
        ///
        /// ONE-SHOT (tiếng đánh, tiếng nhặt đồ...):
        ///   PlaySound("Sword_Hit")
        ///   → Phát ngay, tự trả pool khi xong.
        /// </summary>
        public void PlaySound(string soundKey, PlaySoundConfig config = default)
        {
            if (Provider == null)
            {
                Debug.LogError("[AudioEmitter] Provider is null!");
                return;
            }

            // Chặn spam cùng sound quá nhanh
            float currentTime = Time.time;
            if (_lastPlayTimes.TryGetValue(soundKey, out float lastPlayTime))
            {
                if (currentTime - lastPlayTime < MIN_PLAY_INTERVAL)
                    return;
            }

            if (config.Loop)
            {
                _demandedSounds[soundKey] = config;
                if (_isInRange)
                    TriggerDemandedPlay(soundKey);
                return;
            }

            PlayOneShot(soundKey, config);
            _lastPlayTimes[soundKey] = Time.time;
        }

        // ─────────────────────────────────────────────
        //  Private Playback
        // ─────────────────────────────────────────────

        /// <summary>Phát one-shot sound ngay lập tức (không demand).</summary>
        private void PlayOneShot(string soundKey, PlaySoundConfig config)
        {
            if (!_isInRange)
            {
                Debug.Log($"[AudioEmitter] PlayOneShot: key='{soundKey}' - outside culling range, SKIP");
                return;
            }

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

            bool isMono = Provider.GetMonoAudio();

            source.clip = entry.clip;
            source.loop = false;

            // Nếu volumeScale là 0 (do dùng default struct), ta mặc định là 1.0f
            float volumeScale = config.VolumeScale > 0f ? config.VolumeScale : 1f;

            if (isMono)
            {
                // MONO FIX: spatialBlend=0f → 2D sound, equal volume cả 2 tai.
                source.spatialBlend = 0f;
                source.panStereo    = 0f;
                source.volume       = volumeScale
                    * Provider.CalculateDistanceVolume(transform.position, actualMin, actualMax);
            }
            else
            {
                // STEREO: spatialBlend=1f, Unity xử lý 3D panning + custom rolloff curve.
                source.spatialBlend = 1f;
                source.volume       = volumeScale;
                source.minDistance  = actualMin;
                source.maxDistance  = actualMax;


                AnimationCurve curve = AudioCurveCache.GetDefaultRolloffCurve(
                    actualMin, actualMax, AudioConstants.DEFAULT_VOLUME_AT_MAX);
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            }

            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            string instanceId = $"{soundKey}_{Guid.NewGuid()}";
            Coroutine returnCoroutine = StartCoroutine(ReturnAfterClip(instanceId, source, entry.clip.length));
            _activeSounds[instanceId] = new ActiveSoundHandle(source, false, returnCoroutine);

            source.Play();
        }

        /// <summary>Kích hoạt play demanded loop sound khi vào range.</summary>
        private void TriggerDemandedPlay(string soundKey)
        {
            if (!_demandedSounds.TryGetValue(soundKey, out var config)) return;
            if (_activeSounds.ContainsKey(soundKey)) return; // đang play rồi

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

            bool isMono = Provider.GetMonoAudio();

            source.clip = entry.clip;
            source.loop = true;

            if (isMono)
            {
                // MONO FIX: spatialBlend=0f + manual volume.
                // MonoVolumeUpdater coroutine sẽ cập nhật volume mỗi frame khi Player di chuyển.
                source.spatialBlend = 0f;
                source.panStereo    = 0f;
                source.volume       = config.VolumeScale
                    * Provider.CalculateDistanceVolume(transform.position, actualMin, actualMax);
            }
            else
            {
                // STEREO: spatialBlend=1f, Unity tự xử lý rolloff.
                source.spatialBlend = 1f;
                source.volume       = config.VolumeScale;
                source.minDistance  = actualMin;
                source.maxDistance  = actualMax;

                AnimationCurve curve = AudioCurveCache.GetDefaultRolloffCurve(
                    actualMin, actualMax, AudioConstants.DEFAULT_VOLUME_AT_MAX);
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            }

            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            // Mono loop cần coroutine để cập nhật volume liên tục khi Player di chuyển
            Coroutine monoUpdater = isMono
                ? StartCoroutine(MonoVolumeUpdater(source, config.VolumeScale, actualMin, actualMax))
                : null;

            _activeSounds[soundKey] = new ActiveSoundHandle(source, true, monoUpdater);

            source.Play();
        }

        /// <summary>Dừng âm thanh cụ thể và trả pool.</summary>
        public void StopSound(string soundKey)
        {
            StopSound(soundKey, keepDemand: false);
        }

        /// <param name="keepDemand">Nếu true, giữ demand trong queue (dùng khi culling by range)</param>
        public void StopSound(string soundKey, bool keepDemand)
        {
            var keysToRemove = _activeSounds.Keys.Where(k => k.StartsWith(soundKey)).ToList();

            foreach (var key in keysToRemove)
            {
                if (!_activeSounds.TryGetValue(key, out var handle)) continue;

                if (handle.ReturnCoroutine != null)
                    StopCoroutine(handle.ReturnCoroutine);

                if (handle.Source != null)
                {
                    handle.Source.Stop();
                    Provider?.Return(handle.Source);
                }

                _activeSounds.Remove(key);
            }

            if (!keepDemand)
                _demandedSounds.Remove(soundKey);
        }

        /// <summary>Dừng tất cả active sounds nhưng giữ demand queue.</summary>
        private void StopAllActiveSounds()
        {
            var keys = new List<string>(_activeSounds.Keys);
            foreach (var key in keys)
                StopSound(key, keepDemand: true);
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
        //  Mono Setting API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Áp dụng lại Mono/Stereo setting lên tất cả sound đang active.
        /// Gọi khi user toggle trong Settings UI.
        /// Loop sounds cần restart để switch spatialBlend đúng cách.
        /// </summary>
        public void ApplyMonoSetting(bool isMono)
        {
            // Lấy danh sách loop sounds đang active
            var loopKeys = new List<string>();
            foreach (var kvp in _activeSounds)
            {
                if (kvp.Value.IsLoop) loopKeys.Add(kvp.Key);
            }

            foreach (var key in loopKeys)
            {
                // Stop instance cũ nhưng giữ demand để trigger lại với setting mới
                StopSound(key, keepDemand: true);
                TriggerDemandedPlay(key);
            }
        }

        // ─────────────────────────────────────────────
        //  Internal Coroutines
        // ─────────────────────────────────────────────

        /// <summary>Chờ clip phát xong thì trả AudioSource về pool.</summary>
        private IEnumerator ReturnAfterClip(string soundKey, AudioSource source, float clipLength)
        {
            yield return new WaitForSeconds(clipLength + AudioConstants.BUFFER_TIME);

            if (_activeSounds.ContainsKey(soundKey))
                StopSound(soundKey);
        }

        /// <summary>
        /// Cập nhật volume mỗi frame cho loop sound ở Mono mode.
        /// Cần thiết vì spatialBlend=0f nên Unity không tự rolloff theo khoảng cách.
        /// Coroutine tự dừng khi AudioSource bị destroy hoặc stop.
        /// </summary>
        private IEnumerator MonoVolumeUpdater(AudioSource source, float baseVolume, float minDist, float maxDist)
        {
            while (source != null && source.isPlaying)
            {
                float vol = baseVolume * Provider.CalculateDistanceVolume(transform.position, minDist, maxDist);
                source.volume = vol;
                yield return null; // cập nhật mỗi frame
            }
        }
    }
}
