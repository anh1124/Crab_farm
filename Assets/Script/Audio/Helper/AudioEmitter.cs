using System;
using System.Collections;
using System.Collections.Generic;
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

        // soundKey → AudioSource đang dùng
        private Dictionary<string, AudioSource> _activeSounds
            = new Dictionary<string, AudioSource>();

        // soundKey → loop flag (để biết cái nào không dùng ReturnAfterClip)
        private Dictionary<string, bool> _loopFlags
            = new Dictionary<string, bool>();

        // soundKey → coroutine đang chạy ReturnAfterClip
        private Dictionary<string, Coroutine> _activeCoroutines
            = new Dictionary<string, Coroutine>();

        // ─────────────────────────────────────────────
        //  Debug
        // ─────────────────────────────────────────────

        [Header("Debug (read-only)")]
        [SerializeField] private bool _debugInRange;
        [SerializeField] private List<string> _activeSoundsDebug = new List<string>();

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
        /// Được gọi bởi PlayerAudioZone mỗi 0.2s.
        /// Chỉ fire event khi trạng thái thực sự thay đổi (tránh spam).
        /// </summary>
        public void SetInRange(bool inRange)
        {
            if (_isInRange == inRange) return; // Không đổi → bỏ qua

            _isInRange = inRange;
            _debugInRange = inRange; // Inspector debug

            if (!inRange)
                StopAll(); // Ra vùng → dừng hết âm thanh

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
        /// Phát âm thanh theo soundKey từ SoundRegistry.
        ///
        /// LOOP SOUND (lửa trại, bước chân...):
        ///   PlaySound("Campfire_Crackle", loop: true)
        ///   → Tồn tại đến khi StopSound() hoặc SetInRange(false)
        ///
        /// ONE-SHOT (tiếng đánh, tiếng nhặt đồ...):
        ///   PlaySound("Sword_Hit")
        ///   → Tự động trả pool khi clip xong
        /// </summary>
        /// <param name="soundKey">Key trong SoundRegistry.</param>
        /// <param name="loop">true = loop liên tục, false = one-shot.</param>
        /// <param name="minDistance">0 = dùng giá trị mặc định từ SoundEntry.</param>
        /// <param name="maxDistance">0 = dùng giá trị mặc định từ SoundEntry.</param>
        /// <param name="volumeScale">Nhân thêm âm lượng (0–1).</param>
        /// <param name="forceReplace">Dừng và phát lại nếu đang phát cùng key.</param>
        public void PlaySound(
            string soundKey,
            bool   loop         = false,
            float  minDistance  = 0f,
            float  maxDistance  = 0f,
            float  volumeScale  = 1f,
            bool   forceReplace = false)
        {
            // Guard 1: Không trong vùng → không phát
            if (!_isInRange)
            {
                Debug.LogWarning($"[AudioEmitter] PlaySound('{soundKey}') skipped: not in range");
                return;
            }

            // Guard 2: AudioManager chưa sẵn sàng
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[AudioEmitter] AudioManager.Instance is null!");
                return;
            }

            // Guard 3: Đang phát cùng key
            if (_activeSounds.ContainsKey(soundKey))
            {
                if (!forceReplace) return;
                StopSound(soundKey); // Force replace → dừng cái cũ (bao gồm cả coroutine)
            }

            Debug.Log($"[AudioEmitter] PlaySound('{soundKey}') started, inRange={_isInRange}");

            // Lấy SoundEntry
            var registry = AudioManager.Instance.SoundRegistry;
            if (registry == null)
            {
                Debug.LogError("[AudioEmitter] SoundRegistry is null!");
                return;
            }

            var entry = registry.GetEntry(soundKey);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioEmitter] Key không tồn tại: '{soundKey}'");
                return;
            }

            float actualMin = minDistance > 0f ? minDistance : entry.minDistance;
            float actualMax = maxDistance > 0f ? maxDistance : entry.maxDistance;

            // Xin pool
            AudioSource source = AudioManager.Instance.GetAudioSourceFromPool(transform.position);
            if (source == null)
            {
                Debug.LogWarning("[AudioEmitter] Pool đã hết!");
                return;
            }

            // Cấu hình source
            source.clip         = entry.clip;
            source.volume       = volumeScale;
            source.minDistance  = actualMin;
            source.maxDistance  = actualMax;
            source.loop         = loop;
            source.spatialBlend = 1f;

            AnimationCurve curve = AudioCurveCache.GetDefaultRolloffCurve(actualMin, actualMax, 0f);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

            // Bám theo transform (vẫn di chuyển cùng obj)
            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            // Lưu state
            _activeSounds[soundKey] = source;
            _loopFlags[soundKey]    = loop;

            // Debug list
            _activeSoundsDebug.Add(soundKey);

            source.Play();

            // Chỉ auto-return khi one-shot
            if (!loop)
            {
                Coroutine coroutine = StartCoroutine(ReturnAfterClip(soundKey, source, entry.clip.length));
                _activeCoroutines[soundKey] = coroutine;
            }
        }

        /// <summary>Dừng âm thanh cụ thể và trả pool về.</summary>
        public void StopSound(string soundKey)
        {
            if (!_activeSounds.TryGetValue(soundKey, out AudioSource source)) return;

            // Dừng coroutine nếu đang chạy
            if (_activeCoroutines.TryGetValue(soundKey, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
                _activeCoroutines.Remove(soundKey);
            }

            source.Stop();
            source.transform.SetParent(AudioManager.Instance.Pool.transform);

            AudioManager.Instance.ReturnAudioSourceToPool(source);
            _activeSounds.Remove(soundKey);
            _loopFlags.Remove(soundKey);
            _activeSoundsDebug.Remove(soundKey);
        }

        /// <summary>Dừng tất cả âm thanh. Gọi tự động khi ra khỏi vùng hoặc OnDisable.</summary>
        public void StopAll()
        {
            // Copy keys để tránh modify dict khi iterate
            var keys = new List<string>(_activeSounds.Keys);
            foreach (var key in keys)
                StopSound(key);
        }

        /// <summary>Kiểm tra âm thanh đang phát theo key.</summary>
        public bool IsPlaying(string soundKey)
            => _activeSounds.TryGetValue(soundKey, out var src) && src != null && src.isPlaying;

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
                _activeCoroutines.Remove(soundKey); // Xóa reference trước khi StopSound
                StopSound(soundKey);
            }
        }
    }
}
