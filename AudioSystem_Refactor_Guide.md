# Hướng Dẫn Refactor: Audio System → Registry + Poll

> **Dành cho:** Unity 2D Side-scroll (Kingdom Two Crowns style)
> **Mục tiêu:** Loại bỏ `Update()` O(n) mỗi frame, không cần Collider, hỗ trợ VFX/object không có physics

---

## Mục Lục

1. [Tổng quan kiến trúc mới](#1-tổng-quan-kiến-trúc-mới)
2. [Danh sách file cần thay đổi](#2-danh-sách-file-cần-thay-đổi)
3. [Bước 1 — Tạo AudioEmitterRegistry](#bước-1--tạo-audioemitterregistry)
4. [Bước 2 — Refactor AudioEmitter](#bước-2--refactor-audioemitter)
5. [Bước 3 — Tạo PlayerAudioZone](#bước-3--tạo-playeraudiozone)
6. [Bước 4 — Refactor AudioManager](#bước-4--refactor-audiomanager)
7. [Bước 5 — Refactor AudioPool](#bước-5--refactor-audiomanager)
8. [Bước 6 — Ví dụ CampfireBehavior](#bước-6--ví-dụ-campfirebehavior)
9. [Bước 7 — Setup trong Unity Editor](#bước-7--setup-trong-unity-editor)
10. [Checklist kiểm tra sau refactor](#checklist-kiểm-tra-sau-refactor)
11. [Bảng so sánh trước / sau](#bảng-so-sánh-trước--sau)
12. [Câu hỏi thường gặp](#câu-hỏi-thường-gặp)

---

## 1. Tổng Quan Kiến Trúc Mới

```
┌─────────────────────────────────────────────────────┐
│                   PLAYER                            │
│   ┌─────────────────────────────────────────┐       │
│   │  PlayerAudioZone (MonoBehaviour)        │       │
│   │  - Coroutine poll mỗi 0.2s             │       │
│   │  - Đọc danh sách từ Registry           │       │
│   │  - Tính distX (trục X only)            │       │
│   │  - Gọi emitter.SetInRange(true/false)  │       │
│   └─────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────┘
                          │
                          │ mỗi 0.2s
                          ▼
┌─────────────────────────────────────────────────────┐
│           AudioEmitterRegistry (static)             │
│  List<AudioEmitter> _all                            │
│  - Register() / Unregister()                        │
│  - Không có MonoBehaviour, không có Update()        │
└─────────────────────────────────────────────────────┘
          │                           │
          │ iterate                   │
          ▼                           ▼
┌──────────────────┐       ┌──────────────────────────┐
│  AudioEmitter A  │       │  AudioEmitter B (VFX)    │
│  (Campfire)      │       │  (Không có Collider)     │
│  _isInRange=true │       │  _isInRange=false        │
│  PlaySound(loop) │       │  → không phát gì         │
└──────────────────┘       └──────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────────────┐
│                  AudioManager                        │
│  ┌───────────────┐   ┌──────────────────────────┐   │
│  │  AudioPool    │   │  MusicController (BGM)   │   │
│  │  (unchanged)  │   │  VolumeSettings          │   │
│  └───────────────┘   └──────────────────────────┘   │
└──────────────────────────────────────────────────────┘
```

**Luồng hoạt động:**

```
[mỗi 0.2s] PlayerAudioZone.BatchCheck()
    → foreach emitter in AudioEmitterRegistry.All
        → tính distX
        → emitter.SetInRange(distX <= cullingDistance)
            → nếu false : StopAll() + fire OnRangeChanged(false)
            → nếu true  : fire OnRangeChanged(true)
                → CampfireBehavior.HandleRangeChanged(true)
                    → _audio.PlaySound("Campfire_Crackle", loop: true)
```

---

## 2. Danh Sách File Cần Thay Đổi

| File | Hành động | Ghi chú |
|------|-----------|---------|
| `AudioEmitterRegistry.cs` | **Tạo mới** | Class tĩnh thuần |
| `AudioEmitter.cs` | **Sửa** | Thêm Register/Unregister, SetInRange, OnRangeChanged, loop support |
| `PlayerAudioZone.cs` | **Tạo mới** | Gắn lên Player |
| `AudioManager.cs` | **Sửa** | Xóa `_activeAudioSources`, xóa `Update()` distance check |
| `AudioPool.cs` | **Không thay đổi** | Giữ nguyên |
| `SoundRegistry.cs` | **Không thay đổi** | Giữ nguyên |
| `MusicController.cs` | **Không thay đổi** | Giữ nguyên |
| `CampfireBehavior.cs` | **Tạo mới (ví dụ)** | Pattern cho mọi looping sound |

---

## Bước 1 — Tạo AudioEmitterRegistry

Tạo file mới: `Assets/Scripts/Audio/AudioEmitterRegistry.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Registry trung tâm — lưu danh sách tất cả AudioEmitter đang active trong Scene.
    /// Class tĩnh thuần: không cần MonoBehaviour, không cần GameObject, không cần DontDestroyOnLoad.
    ///
    /// Tại sao static thay vì Singleton MonoBehaviour?
    ///   - Không cần setup trong Editor
    ///   - Không bị phụ thuộc thứ tự Awake()
    ///   - AudioEmitter tự đăng ký/hủy đăng ký trong OnEnable/OnDisable
    ///
    /// Lưu ý Scene transition:
    ///   Khi load Scene mới, các AudioEmitter bị Destroy → OnDestroy → Unregister tự động.
    ///   _all sẽ tự dọn sạch. Không cần gọi Clear() thủ công.
    /// </summary>
    public static class AudioEmitterRegistry
    {
        private static readonly List<AudioEmitter> _all = new List<AudioEmitter>();

        /// <summary>Đăng ký emitter khi được enable. Gọi từ AudioEmitter.OnEnable().</summary>
        public static void Register(AudioEmitter emitter)
        {
            if (emitter != null && !_all.Contains(emitter))
                _all.Add(emitter);
        }

        /// <summary>Hủy đăng ký khi disable hoặc destroy. Gọi từ AudioEmitter.OnDisable/OnDestroy().</summary>
        public static void Unregister(AudioEmitter emitter)
        {
            _all.Remove(emitter);
        }

        /// <summary>Toàn bộ danh sách emitter đang active — dùng bởi PlayerAudioZone.</summary>
        public static IReadOnlyList<AudioEmitter> All => _all;

        /// <summary>Debug: số lượng emitter đang đăng ký.</summary>
        public static int Count => _all.Count;
    }
}
```

---

## Bước 2 — Refactor AudioEmitter

Đây là file thay đổi nhiều nhất. Các thay đổi chính:

- Thêm `OnEnable` / `OnDisable` / `OnDestroy` để tự đăng ký Registry
- Thêm `_isInRange` flag và `SetInRange()`
- Thêm `OnRangeChanged` event
- Thêm `loop` parameter vào `PlaySound()`
- Sửa `ReturnAfterClip` chỉ chạy khi `loop = false`
- **Xóa** logic distance check trùng lặp (AudioManager đã không track nữa)

```csharp
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
            if (!_isInRange) return;

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
                StopSound(soundKey); // Force replace → dừng cái cũ
            }

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

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f,        1f);
            curve.AddKey(actualMin, 1f);
            curve.AddKey(actualMax, 0.3f);
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
                StartCoroutine(ReturnAfterClip(soundKey, source, entry.clip.length));
        }

        /// <summary>Dừng âm thanh cụ thể và trả pool về.</summary>
        public void StopSound(string soundKey)
        {
            if (!_activeSounds.TryGetValue(soundKey, out AudioSource source)) return;

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
            yield return new WaitForSeconds(clipLength + 0.1f);

            // Chỉ trả pool nếu âm thanh vẫn còn trong dict (chưa bị StopSound())
            if (_activeSounds.ContainsKey(soundKey))
                StopSound(soundKey);
        }
    }
}
```

---

## Bước 3 — Tạo PlayerAudioZone

Tạo file mới: `Assets/Scripts/Audio/PlayerAudioZone.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Gắn lên Player GameObject.
    /// Poll AudioEmitterRegistry mỗi checkInterval giây.
    /// Tính khoảng cách trục X (phù hợp game 2D side-scroll).
    /// Không dùng Physics/Collider.
    ///
    /// SETUP:
    ///   1. Gắn component này lên Player
    ///   2. Điều chỉnh cullingDistance trong Inspector
    ///   3. Không cần setup gì thêm
    /// </summary>
    public class PlayerAudioZone : MonoBehaviour
    {
        [Header("Vùng nghe")]
        [Tooltip("Khoảng cách trục X tối đa để AudioEmitter được phép phát âm thanh")]
        [SerializeField] private float cullingDistance = 50f;

        [Header("Performance")]
        [Tooltip("Giây giữa mỗi lần poll. 0.2s = đủ mượt, không đáng kể về CPU")]
        [SerializeField] private float checkInterval = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private int _emitterCountInRange; // Inspector debug

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            // Cập nhật AudioManager về PlayerTransform
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetPlayerTransform(transform);

            StartCoroutine(PollRoutine());
        }

        // ─────────────────────────────────────────────
        //  Poll
        // ─────────────────────────────────────────────

        private IEnumerator PollRoutine()
        {
            // WaitForSeconds tạo một lần, tái sử dụng → không tốn GC
            var wait = new WaitForSeconds(checkInterval);

            while (true)
            {
                yield return wait;
                BatchCheck();
            }
        }

        private void BatchCheck()
        {
            float playerX = transform.position.x;
            int inRangeCount = 0;

            var emitters = AudioEmitterRegistry.All;
            for (int i = 0; i < emitters.Count; i++)
            {
                var emitter = emitters[i];
                if (emitter == null) continue;

                // Side-scroll: chỉ cần khoảng cách X
                float distX = Mathf.Abs(emitter.transform.position.x - playerX);
                bool inRange = distX <= cullingDistance;

                emitter.SetInRange(inRange);

                if (inRange) inRangeCount++;
            }

            _emitterCountInRange = inRangeCount; // Inspector debug
        }

        // ─────────────────────────────────────────────
        //  Gizmo (Editor visualization)
        // ─────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            // Vẽ vùng culling dưới dạng hình chữ nhật (trục X là quan trọng)
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.DrawCube(
                transform.position,
                new Vector3(cullingDistance * 2f, 20f, 1f));

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(cullingDistance * 2f, 20f, 1f));
        }
    }
}
```

---

## Bước 4 — Refactor AudioManager

Chỉ cần **xóa** 2 phần trong AudioManager hiện tại:

### 4a. Xóa `_activeAudioSources` dictionary

```csharp
// XÓA dòng này:
private Dictionary<AudioSource, float> _activeAudioSources = new Dictionary<AudioSource, float>();
```

### 4b. Xóa toàn bộ method `Update()`

```csharp
// XÓA toàn bộ method này:
private void Update()
{
    if (playerTransform == null) return;
    var toRemove = new List<AudioSource>();
    foreach (var kvp in _activeAudioSources)
    {
        // ...
    }
    foreach (var source in toRemove)
    {
        _activeAudioSources.Remove(source);
    }
}
```

### 4c. Xóa các method Pool API không còn cần thiết

```csharp
// XÓA 3 method này (AudioEmitter không cần gọi nữa):
public void RegisterAudioSource(AudioSource source, float maxDistance) { ... }
public void UnregisterAudioSource(AudioSource source) { ... }
// ReturnAudioSourceToPool giữ lại — vẫn cần

```

### 4d. Thêm public property cho Pool (AudioEmitter cần dùng transform)

```csharp
// Đã có sẵn trong code gốc, kiểm tra có chưa:
public AudioPool Pool => _pool;
```

### AudioManager sau khi refactor (phần thay đổi)

```csharp
// ─────────────────────────────────────────────
//  Private State (sau refactor)
// ─────────────────────────────────────────────

private AudioPool _pool;
private MusicController _music;
private VolumeSettings _volume;

// KHÔNG CÒN: private Dictionary<AudioSource, float> _activeAudioSources

// ─────────────────────────────────────────────
//  Unity Lifecycle (sau refactor)
// ─────────────────────────────────────────────

private void Awake() { /* giữ nguyên */ }
private void Initialize() { /* giữ nguyên */ }

// KHÔNG CÒN: private void Update() { ... }

private IEnumerator Start()
{
    yield return new WaitForSeconds(3f);
    if (playerTransform == null)
        Debug.LogWarning("[AudioManager] Player Transform chưa được gán!");
}

// ─────────────────────────────────────────────
//  Pool API (sau refactor — đơn giản hơn)
// ─────────────────────────────────────────────

public AudioSource GetAudioSourceFromPool(Vector3 worldPosition)
    => _pool.Get(worldPosition);

public void ReturnAudioSourceToPool(AudioSource source)
    => _pool.Return(source);

// KHÔNG CÒN: RegisterAudioSource(), UnregisterAudioSource()
```

---

## Bước 5 — Giữ Nguyên AudioPool

`AudioPool.cs` không cần thay đổi gì. Thiết kế đã tốt.

---

## Bước 6 — Ví dụ CampfireBehavior

Đây là **pattern mẫu** cho mọi object có loop sound.

```csharp
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Ví dụ về cách sử dụng AudioEmitter cho object có loop sound.
    /// Áp dụng tương tự cho: waterfall, wind emitter, forge, market ambiance...
    ///
    /// KHÔNG cần Update(), KHÔNG cần check distance thủ công.
    /// Tất cả do PlayerAudioZone → AudioEmitter → OnRangeChanged xử lý.
    /// </summary>
    [RequireComponent(typeof(AudioEmitter))]
    public class CampfireBehavior : MonoBehaviour
    {
        [Header("Sound Keys (phải trùng với SoundRegistry)")]
        [SerializeField] private string fireSoundKey   = "Campfire_Crackle";
        [SerializeField] private string emberSoundKey  = "Campfire_Ember"; // Âm thanh phụ (tùy chọn)

        [Header("State")]
        [SerializeField] private bool _isLit = true; // Lửa đang cháy?

        private AudioEmitter _audio;

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            _audio = GetComponent<AudioEmitter>();
        }

        private void OnEnable()
        {
            _audio.OnRangeChanged += HandleRangeChanged;
        }

        private void OnDisable()
        {
            // Quan trọng: Unsubscribe để tránh memory leak
            _audio.OnRangeChanged -= HandleRangeChanged;
        }

        // ─────────────────────────────────────────────
        //  Range Handler
        // ─────────────────────────────────────────────

        private void HandleRangeChanged(bool inRange)
        {
            if (inRange)
                StartSounds();
            // inRange = false → AudioEmitter.StopAll() đã được gọi tự động
            // Không cần làm gì thêm khi ra vùng
        }

        private void StartSounds()
        {
            if (!_isLit) return; // Lửa đã tắt → không phát

            _audio.PlaySound(fireSoundKey,  loop: true);
            _audio.PlaySound(emberSoundKey, loop: true, volumeScale: 0.4f);
        }

        // ─────────────────────────────────────────────
        //  Game Logic API
        // ─────────────────────────────────────────────

        /// <summary>Tắt lửa (mưa, nước...). Dừng âm thanh ngay lập tức.</summary>
        public void Extinguish()
        {
            _isLit = false;
            _audio.StopAll();
        }

        /// <summary>Bật lửa lại. Chỉ phát âm thanh nếu player đang trong vùng.</summary>
        public void Ignite()
        {
            _isLit = true;
            // AudioEmitter.IsInRange check → chỉ phát nếu player gần
            if (_audio.IsInRange)
                StartSounds();
        }
    }
}
```

---

## Bước 7 — Setup Trong Unity Editor

### 7.1 Cấu trúc Scene

```
[AudioManager]          ← GameObject tồn tại từ Bootstrap Scene
  ├── AudioManager.cs
  ├── AudioPool.cs
  ├── BGM_A            ← Child với AudioSource
  ├── BGM_B            ← Child với AudioSource
  └── ENV              ← Child với AudioSource

Player
  ├── PlayerController.cs
  ├── PlayerAudioZone.cs   ← THÊM MỚI vào đây
  └── ...

Campfire_01
  ├── SpriteRenderer
  ├── CampfireBehavior.cs
  ├── AudioEmitter.cs      ← THÊM vào đây (không cần Collider)
  └── ParticleSystem (VFX)

VFX_WaterfallSplash        ← Không có Collider — vẫn hoạt động được
  ├── ParticleSystem
  └── AudioEmitter.cs
```

### 7.2 Inspector Settings

**PlayerAudioZone (trên Player):**

| Field | Giá trị gợi ý | Ghi chú |
|-------|---------------|---------|
| Culling Distance | 50 | Nên bằng AudioManager.cullingDistance |
| Check Interval | 0.2 | Giây. Giảm xuống 0.1 nếu cần mượt hơn |
| Show Gizmo | true | Tắt khi build production |

**AudioEmitter (trên mỗi object):**

| Field | Ghi chú |
|-------|---------|
| Không có Inspector field | Tự động Register qua OnEnable |

**AudioManager:**
- Xóa field `cullingDistance` nếu muốn (đã chuyển sang PlayerAudioZone)
- Hoặc giữ lại để dùng như reference value chia sẻ

### 7.3 SoundRegistry — thêm entries mới

```
SoundRegistry Asset
  └── Group: "Environment"
        ├── key: "Campfire_Crackle"  clip: [fire loop]  min: 1  max: 12
        ├── key: "Campfire_Ember"    clip: [ember loop]  min: 0.5  max: 8
        ├── key: "Waterfall_Roar"    clip: [water loop]  min: 2  max: 20
        └── key: "Wind_Howl"         clip: [wind loop]   min: 5  max: 30
```

---

## Checklist Kiểm Tra Sau Refactor

### Compile check

- [ ] `AudioEmitterRegistry.cs` tạo mới, không lỗi
- [ ] `AudioEmitter.cs` có `OnEnable`/`OnDisable` gọi Registry
- [ ] `AudioEmitter.cs` có `_isInRange` guard trong `PlaySound()`
- [ ] `AudioEmitter.cs` có `OnRangeChanged` event
- [ ] `PlayerAudioZone.cs` tạo mới, không lỗi
- [ ] `AudioManager.cs` đã xóa `_activeAudioSources` và `Update()`
- [ ] Không còn lỗi "X không tồn tại" sau khi xóa method cũ

### Runtime check

- [ ] Đứng cạnh Campfire → nghe tiếng lửa
- [ ] Đi xa khỏi Campfire → tiếng tắt (delay ~0.2s là bình thường)
- [ ] Đến gần lại → tiếng bật lại
- [ ] Extinguish campfire khi đang đứng cạnh → tiếng dừng ngay
- [ ] Ignite campfire từ xa → không nghe tiếng (đúng)
- [ ] Ignite campfire khi đứng cạnh → nghe tiếng ngay
- [ ] Scene transition → không bị memory leak (Registry tự dọn)
- [ ] `AudioEmitterRegistry.Count` trong Debug = số emitter đang active trong Scene

### Performance check (Profiler)

- [ ] `Update()` của AudioManager không còn trong Profiler
- [ ] `PlayerAudioZone.BatchCheck()` xuất hiện mỗi ~0.2s, không mỗi frame
- [ ] Pool không bị leak (số lượng AudioSource trong pool ổn định)

---

## Bảng So Sánh Trước / Sau

| Tiêu chí | Trước (Update per-frame) | Sau (Registry + Poll) |
|----------|--------------------------|----------------------|
| Distance check | O(n) mỗi frame (60 lần/s) | O(n) mỗi 0.2s (5 lần/s) |
| Cần Collider | Không | Không |
| Hỗ trợ VFX object | Có (nhưng cần thêm vào dict thủ công) | Có (tự động qua OnEnable) |
| Loop sound | Bị phá bởi `ReturnAfterClip` | Đúng — loop tồn tại đến khi StopSound() |
| Ai chịu trách nhiệm distance | AudioManager + AudioEmitter (trùng lặp) | PlayerAudioZone (một nơi duy nhất) |
| Phản ứng range change | Tức thì | ~200ms delay (không cảm nhận được) |
| Số dòng code | Nhiều hơn, phân tán | Ít hơn, rõ ràng trách nhiệm |
| Memory leak risk | Cao (dict không dọn đúng) | Thấp (OnDisable tự Unregister) |
| Debug trong Inspector | Khó (dict không serialize) | Dễ (count và bool visible) |

---

## Câu Hỏi Thường Gặp

**Q: 200ms delay có gây ra tiếng bật/tắt đột ngột không?**

A: Không. Tiếng âm thanh bắt đầu fade dựa trên khoảng cách (AudioSource rolloff curve) từ trước đó rồi. Khi `SetInRange(false)` gọi `StopAll()`, âm thanh thực ra đã rất nhỏ hoặc im lặng rồi. Delay 200ms không nghe ra.

**Q: Nếu muốn 2D game dùng Vector2 thay vì chỉ trục X?**

A: Sửa `BatchCheck()` trong `PlayerAudioZone`:

```csharp
// Thay dòng distX:
float dist = Vector2.Distance(
    new Vector2(emitter.transform.position.x, emitter.transform.position.y),
    new Vector2(transform.position.x, transform.position.y));
bool inRange = dist <= cullingDistance;
```

**Q: Nếu Campfire nằm ở tầng trên (Y khác) thì sao?**

A: Với KTC-style game, các layer thường có Y cố định per-layer. Nếu game bạn có nhiều tầng Y quan trọng, dùng `Vector2.Distance` như trên. Nếu thuần side-scroll, trục X là đủ.

**Q: Nhiều AudioEmitter cùng phát 1 key ("Step") từ nhiều NPC — pool có đủ không?**

A: Mỗi AudioEmitter instance tự quản lý key riêng. "Step" của NPC A và "Step" của NPC B là 2 AudioSource khác nhau trong pool. Pool 50 source là đủ cho game KTC scale.

**Q: Tôi muốn một số âm thanh quan trọng không bị cull (ví dụ boss music)?**

A: Bypass hoàn toàn AudioEmitter — dùng trực tiếp `AudioManager.Instance.PlayBGM()` hoặc `PlaySFXAtPosition(..., bypassCulling: true)` cho những trường hợp này.

**Q: Scene chuyển cảnh — Registry có bị stale reference không?**

A: Không. Khi GameObject bị Destroy, Unity gọi `OnDisable` → `OnDestroy`. AudioEmitter đã `Unregister` trong `OnDisable`. Registry tự sạch.

---

*Tài liệu này được tạo cho dự án game side-scroll Unity 2D, kiến trúc Audio System v2.0*
