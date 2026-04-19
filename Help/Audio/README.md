# Hệ Thống Audio - Tài Liệu Chi Tiết

## Tổng Quan

Hệ thống audio sử dụng kiến trúc **Registry + Poll** để tối ưu hóa performance bằng cách tránh các operation O(n) trong Update() cho distance culling.

## ⚠️ QUAN TRỌNG - YÊU CẦU BẮT BUỘC

### TẤT CẢ SFX PHẢI DÙNG AudioEmitter

**BẮT BUỘC**: Khi phát SFX (Sound Effects), **KHÔNG BAO GIỜ** gọi trực tiếp `AudioManager.PlaySFXByKey()` hoặc `AudioManager.GetAudioSourceFromPool()`.

**ĐÚNG CÁCH**:
```csharp
// ✅ Đúng: Dùng AudioEmitter
_audioEmitter.PlaySound("ShieldBot_Activate");
```

**SAI CÁCH**:
```csharp
// ❌ Sai: Không gọi trực tiếp AudioManager
AudioManager.Instance.PlaySFXByKey("ShieldBot_Activate", transform.position);
```

### Tại Sao Phải Dùng AudioEmitter?

1. **Lifecycle Management**: AudioEmitter tự gọi `StopAll()` trong `OnDisable()` → khi enemy die/disable, tất cả sounds tự dừng
2. **Auto-Cleanup**: Không cần manual cleanup, tránh AudioSource leaks
3. **Distance Culling**: PlayerAudioZone tự động set range, AudioEmitter tự stop khi player ra khỏi vùng
4. **Architecture**: AudioEmitter làm middle layer giữa behavior và AudioManager, giúp quản lý sound theo vòng đời object

### Khi Nào Dùng AudioManager Trực Tiếp?

Chỉ dùng `AudioManager` trực tiếp cho:
- **BGM**: `AudioManager.Instance.PlayBGM(clip)`
- **ENV (Ambient)**: `AudioManager.Instance.PlayENV(clip)`
- **UI Sound**: `AudioManager.Instance.PlayUISound(soundKey)`

**KHÔNG** dùng cho SFX spatial sounds (enemy attack, footstep, v.v.)

---

### Các Thành Phần Chính

```
AudioManager (Singleton)
├── AudioPool (Object Pooling)
├── SoundRegistry (Data)
└── PlayerTransform (Reference)

PlayerAudioZone (gắn trên Player)
└── Poll AudioEmitterRegistry

AudioEmitterRegistry (Static)
└── Danh sách tất cả AudioEmitter

AudioEmitter (gắn trên GameObject)
├── OnRangeChanged Event
└── PlaySound/StopSound API
```
[AudioManager] SoundRegistry Debug - Total sounds: 16
  - UI_Click | Clip: UI_Click | Min: 0m | Max: 0m
  - Guide_Show | Clip: Guide_Show | Min: 0m | Max: 0m
  - Player_Lose | Clip: Player_Lose | Min: 0m | Max: 0m
  - Footstep_Run | Clip: Footstep_Run | Min: 2m | Max: 15m
  - Footstep_Walk | Clip: Footstep_Walk | Min: 2m | Max: 10m
  - Bow_Draw | Clip: Bow_Draw | Min: 3m | Max: 15m
  - Bow_Release | Clip: Bow_Release | Min: 3m | Max: 20m
  - Arrow_HitGround | Clip: Arrow_HitGround | Min: 2m | Max: 15m
  - Coin_Pickup | Clip: Coin_Pickup | Min: 2m | Max: 10m
  - Coin_Drop | Clip: Coin_Drop | Min: 2m | Max: 10m
  - ShieldBot_Activate | Clip: ShieldBot_Activate | Min: 3m | Max: 15m
  - ShieldBot_Shock | Clip: ShieldBot_Shock | Min: 3m | Max: 20m
  - Portal_Activate | Clip: Portal_Activate | Min: 3m | Max: 15m
  - Portal_Normal | Clip: Portal_Normal | Min: 3m | Max: 10m
  - Denizen_JobChange | Clip: Denizen_JobChange | Min: 3m | Max: 15m
  - Test_Sound | Clip: playerbosst | Min: 0m | Max: 0m

---

## Các Script Chi Tiết

### 1. AudioManager.cs

**Vị trí**: `Assets/Script/Audio/Manager/AudioManager.cs`

**Mô tả**: Singleton, trái tim của hệ thống audio. Quản lý toàn bộ audio trong game.

**Chức năng chính**:
- Singleton pattern với DontDestroyOnLoad
- Quản lý AudioPool cho SFX
- Quản lý BGM với crossfade (2 AudioSource)
- Quản lý ENV (ambient sound)
- Quản lý VolumeSettings với AudioMixer
- Auto-set cullingDistance từ SoundRegistry

**Fields quan trọng**:
```csharp
public static AudioManager Instance { get; private set; }

[SerializeField] private AudioMixer audioMixer;
[SerializeField] private AudioSource bgmSourceA, bgmSourceB; // Crossfade
[SerializeField] private AudioSource envSource; // Ambient
[SerializeField] private Transform playerTransform;
[SerializeField] private float cullingDistance = 50f; // Auto-set từ SoundRegistry
[SerializeField] private SoundRegistry soundRegistry;

public Transform PlayerTransform { get; set; }
public float CullingDistance => cullingDistance;
public SoundRegistry SoundRegistry => soundRegistry;
public AudioPool Pool => _pool;
```

**API quan trọng**:
```csharp
// BGM (dùng trực tiếp cho background music)
public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f)
public void StopBGM(float fadeDuration = 1.0f)

// ENV (dùng trực tiếp cho ambient sound)
public void PlayENV(AudioClip clip, bool loop = true)
public void StopENV()

// UI Sound (dùng trực tiếp cho UI sound 2D)
public void PlayUISound(string soundKey)

// ⚠️ SFX methods - KHÔNG DÙNG TRỰC TIẾP
// Dùng AudioEmitter.PlaySound() thay vì các methods dưới đây
public void PlaySFXAtPosition(AudioClip clip, Vector3 worldPosition, ...)
public void PlaySFXByKey(string key, Vector3 worldPosition, ...)
public void PlaySFXFollowing(AudioClip clip, Transform target, ...)

// Pool methods - KHÔNG DÙNG TRỰC TIẾP
// AudioEmitter tự quản lý pool
public AudioSource GetAudioSourceFromPool(Vector3 worldPosition)
public void ReturnAudioSourceToPool(AudioSource source)
```

**Setup trong Unity**:
1. Tạo GameObject tên "[AudioManager]"
2. Gắn AudioManager, AudioPool
3. Tạo 2 child cho BGM (AudioSource)
4. Tạo 1 child cho ENV (AudioSource)
5. Gán AudioMixer, SoundRegistry
6. Set playerTransform hoặc gán runtime

---

### 2. AudioPool.cs

**Vị trí**: `Assets/Script/Audio/Helper/AudioPool.cs`

**Mô tả**: Object pooling cho AudioSource để tránh tạo/hủy GameObject liên tục.

**Chức năng chính**:
- Tạo sẵn AudioSource khi Awake (initialPoolSize)
- Dynamic expansion khi cần (tới maxPoolSize)
- Reset state khi return về pool
- Debug count cho tracking

**Fields quan trọng**:
```csharp
[SerializeField] private AudioMixerGroup sfxMixerGroup;
[SerializeField] private int initialPoolSize = 12;
[SerializeField] private int maxPoolSize = 50;

[SerializeField] private int _availableCount; // Debug
[SerializeField] private int _inUseCount; // Debug

private Queue<AudioSource> _available;
private HashSet<AudioSource> _inUse;
```

**API quan trọng**:
```csharp
public AudioSource Get(Vector3 worldPosition)
public void Return(AudioSource source)
public void ReturnAll()
```

**Lợi ích**:
- Tốn ít CPU/GC so với tạo/hủy GameObject
- Reuse AudioSource cho nhiều sound
- Giới hạn maxPoolSize để tránh overflow

---

### 3. AudioEmitterRegistry.cs

**Vị trí**: `Assets/Script/Audio/Helper/AudioEmitterRegistry.cs`

**Mô tả**: Static registry lưu danh sách tất cả AudioEmitter đang active trong Scene.

**Chức năng chính**:
- Static class, không cần MonoBehaviour
- AudioEmitter tự Register/Unregister trong OnEnable/OnDisable
- PlayerAudioZone poll registry để check distance

**API quan trọng**:
```csharp
public static void Register(AudioEmitter emitter)
public static void Unregister(AudioEmitter emitter)
public static IReadOnlyList<AudioEmitter> All => _all
public static int Count => _all.Count
```

**Tại sao static thay vì Singleton**:
- Không cần setup trong Editor
- Không phụ thuộc thứ tự Awake()
- AudioEmitter tự quản lý lifecycle

**Scene transition**:
- Khi load Scene mới, AudioEmitter bị Destroy → OnDestroy → Unregister tự động
- _all tự dọn sạch, không cần Clear() thủ công

---

### 4. AudioEmitter.cs

**Vị trí**: `Assets/Script/Audio/Helper/AudioEmitter.cs`

**Mô tả**: Component gắn lên GameObject (enemy, NPC, campfire, VFX...) để phát sound.

**⚠️ QUAN TRỌNG - API CHÍNH CHO SFX**

AudioEmitter là **API CHÍNH** cho tất cả SFX spatial sounds. Không gọi AudioManager trực tiếp cho SFX.

**Chức năng chính**:
- Tự Register/Unregister vào AudioEmitterRegistry
- SetInRange() được gọi bởi PlayerAudioZone
- OnRangeChanged event cho Behavior scripts
- PlaySound/StopSound API với loop support
- Auto-return pool cho one-shot sounds

**Fields quan trọng**:
```csharp
public event Action<bool> OnRangeChanged; // true = vào vùng, false = ra vùng

private bool _isInRange;
private Dictionary<string, AudioSource> _activeSounds;
private Dictionary<string, bool> _loopFlags;

[SerializeField] private bool _debugInRange;
[SerializeField] private List<string> _activeSoundsDebug;
```

**API quan trọng**:
```csharp
public void SetInRange(bool inRange) // Gọi bởi PlayerAudioZone
public bool IsInRange => _isInRange

public void PlaySound(
    string soundKey,
    bool loop = false,
    float minDistance = 0f,
    float maxDistance = 0f,
    float volumeScale = 1f,
    bool forceReplace = false)

public void StopSound(string soundKey)
public void StopAll()
public bool IsPlaying(string soundKey)
public int ActiveSoundCount => _activeSounds.Count
```

**Vòng đời sound**:
1. PlayerAudioZone → SetInRange(true) → OnRangeChanged fired
2. Behavior lắng nghe → PlaySound("key", loop: true)
3. Player đi xa → SetInRange(false) → StopAll() tự động

**Loop vs One-shot**:
- **Loop sound** (lửa trại, bước chân): `PlaySound("key", loop: true)` → tồn tại đến khi StopSound() hoặc SetInRange(false)
- **One-shot** (tiếng đánh, tiếng nhặt đồ): `PlaySound("key", loop: false)` → auto-return pool khi clip xong

---

### 5. PlayerAudioZone.cs

**Vị trí**: `Assets/Script/Audio/Helper/PlayerAudioZone.cs`

**Mô tả**: Gắn lên Player GameObject, poll AudioEmitterRegistry để check distance.

**Chức năng chính**:
- Poll registry mỗi checkInterval (0.1s mặc định)
- Tính khoảng cách 2D (X và Y) cho game 2D
- SetInRange() cho từng emitter
- Gizmo visualization trong Editor

**Fields quan trọng**:
```csharp
[SerializeField] private float checkInterval = 0.1f;
[SerializeField] private bool showGizmo = true;
[SerializeField] private int _emitterCountInRange; // Debug
```

**API quan trọng**:
```csharp
public void CheckEmitterImmediate(AudioEmitter emitter) // Cho AudioTest
```

**Cách hoạt động**:
1. Start() → Cập nhật AudioManager.PlayerTransform
2. PollRoutine() → Mỗi 0.1s → BatchCheck()
3. BatchCheck() → Iterate AudioEmitterRegistry.All → Tính distance → SetInRange()

**Distance calculation**:
```csharp
Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);
Vector2 emitterPos = new Vector2(emitter.transform.position.x, emitter.transform.position.y);
float distance = Vector2.Distance(playerPos, emitterPos);
float cullingDist = AudioManager.Instance?.CullingDistance ?? 50f;
bool inRange = distance <= cullingDist;
```

**Setup**:
- Gắn component lên Player
- PlayerController tự gắn component (đã implement)
- Không cần setup thêm

---

### 6. SoundRegistry.cs

**Vị trí**: `Assets/Script/Audio/Data/SoundRegistry.cs`

**Mô tả**: Data registry lưu trữ SoundEntry cho tất cả sound keys.

**Chức năng chính**:
- Dictionary key → SoundEntry
- GetEntry() để lấy SoundEntry
- GetAllKeys() để lấy tất cả keys

**SoundEntry**:
```csharp
[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
    public float minDistance = 2f;
    public float maxDistance = 15f;
}
```

**Setup trong Unity**:
- Tạo ScriptableObject hoặc MonoBehaviour
- Add SoundEntry trong Inspector
- Gán vào AudioManager.soundRegistry

---

### 7. AudioTest.cs

**Vị trí**: `Assets/Script/Audio/Helper/AudioTest.cs`

**Mô tả**: Test utility để test sound từ SoundRegistry bằng key.

**Chức năng chính**:
- Tạo test object tại (0,0,0) với AudioEmitter
- CheckEmitterImmediate() trước khi phát
- Debug log với distance info
- Auto-select key với số để test nhanh

**Setup trong Unity**:
- Gắn script lên GameObject bất kỳ
- Set testKey hoặc dùng số để auto-select
- Nhấn T để test

---

## Luồng Channel - Cách Từng Channel Hoạt Động

### 1. SFX Channel (Spatial Sound Effects)

**Mục đích**: Tiếng động có vị trí không gian (enemy attack, footstep, explosion, v.v.)

**Luồng dữ liệu**:
```
GameObject (Enemy/NPC)
└── AudioEmitter component
    ├── OnEnable() → Register vào AudioEmitterRegistry
    └── Behavior script gọi PlaySound("key")
        └── AudioManager.GetAudioSourceFromPool()
            └── AudioPool.Get() → AudioSource từ pool
                ├── Set clip, volume, minDistance, maxDistance
                ├── Set custom rolloff curve
                ├── Set parent vào AudioEmitter.transform
                └── source.Play()
                    └── One-shot: Auto-return pool sau clip
                    └── Loop: Tồn tại đến khi StopSound()
```

**Distance culling**:
```
PlayerAudioZone (mỗi 0.1s)
└── Poll AudioEmitterRegistry.All
    └── Tính distance 2D giữa player và emitter
        └── if distance > cullingDistance
            └── emitter.SetInRange(false)
                └── emitter.StopAll()
                    └── Return tất cả AudioSource về pool
```

**Lifecycle cleanup**:
```
GameObject.Disable/Destroy
└── AudioEmitter.OnDisable()
    ├── Unregister khỏi AudioEmitterRegistry
    └── StopAll()
        └── Return tất cả AudioSource về pool
```

**Ví dụ**: ShieldBot spawn → PlaySound("ShieldBot_Activate") → Enemy die → OnDisable → StopAll → Pool returned

---

### 2. BGM Channel (Background Music)

**Mục đích**: Nhạc nền game, crossfade khi đổi bài

**Luồng dữ liệu**:
```
AudioManager
├── 2 AudioSource (bgmSourceA, bgmSourceB) cho crossfade
└── PlayBGM(clip, fadeDuration)
    ├── Kiểm tra source nào đang play
    ├── Fade out source hiện tại (volume 1 → 0)
    ├── Fade in source mới (volume 0 → 1)
    └── Loop = true
```

**Không dùng pool**: BGM dùng dedicated AudioSource, không qua AudioPool

**Không có distance culling**: BGM play toàn scene, không phụ thuộc vị trí player

**Ví dụ**: Change scene → Fade out current BGM → Fade in new BGM

---

### 3. ENV Channel (Ambient Sound)

**Mục đích**: Tiếng môi trường (wind, water, birds, v.v.)

**Luồng dữ liệu**:
```
AudioManager
├── 1 AudioSource (envSource) cho ambient
└── PlayENV(clip, loop)
    ├── Set clip vào envSource
    ├── Loop = true
    └── Play()
```

**Không dùng pool**: ENV dùng dedicated AudioSource, không qua AudioPool

**Không có distance culling**: ENV play toàn scene, không phụ thuộc vị trí player

**Ví dụ**: Daytime → PlayENV("Birds") → Nighttime → PlayENV("Crickets")

---

### 4. UI Channel (UI Sound Effects)

**Mục đích**: Tiếng giao diện UI (button click, hover, v.v.) - 2D sound

**Luồng dữ liệu**:
```
UI Button/Slider
└── OnClick/OnHover event
    └── AudioManager.PlayUISound("key")
        └── Get SoundEntry từ SoundRegistry
            └── uiSource.PlayOneShot(clip) hoặc Play()
                ├── spatialBlend = 0 (2D sound)
                └── Không có distance attenuation
```

**Dedicated AudioSource**: UI dùng 1 AudioSource riêng (uiSource), không qua AudioPool

**Không có distance culling**: UI sound là 2D, không phụ thuộc vị trí player

**Volume control**: Volume được control qua AudioMixer PARAM_UI

**Ví dụ**: Click button → PlayUISound("UI_Click") → 2D sound phát ngay lập tức

---

## Tóm Tắt Channel

| Channel | AudioSource | Pool | Distance Culling | Spatial | Use Case |
|---------|-------------|------|------------------|---------|----------|
| **SFX** | Dynamic (AudioPool) | ✅ | ✅ (PlayerAudioZone) | ✅ (3D) | Enemy attack, footstep, explosion |
| **BGM** | Dedicated (2 sources) | ❌ | ❌ | ❌ (2D) | Background music |
| **ENV** | Dedicated (1 source) | ❌ | ❌ | ❌ (2D) | Ambient sound |
| **UI** | Dedicated (1 source) | ❌ | ❌ | ❌ (2D) | Button click, hover |

---

## Luồng Hoạt Động

### 1. Khởi Tạo (Startup)

```
AudioManager.Awake()
├── Singleton setup
├── Initialize()
│   ├── Get AudioPool
│   ├── Create MusicController
│   ├── Load VolumeSettings
│   └── AutoSetCullingDistanceFromRegistry()
│       └── Tìm maxDistance lớn nhất từ SoundRegistry
│           └── Set cullingDistance = maxDistance
└── Start() coroutine
    └── Warning nếu playerTransform null sau 3s

PlayerController.SetUpControllerPlayer()
├── Gắn PlayerAudioZone component (nếu chưa có)
└── AudioManager.PlayerTransform = player.transform

PlayerAudioZone.Start()
├── AudioManager.PlayerTransform = transform
└── StartCoroutine(PollRoutine())
```

### 2. AudioEmitter Được Enable

```
GameObject.Enable
└── AudioEmitter.OnEnable()
    └── AudioEmitterRegistry.Register(this)
        └── Thêm vào danh sách _all
```

### 3. PlayerAudioZone Poll

```
PlayerAudioZone.PollRoutine() (mỗi 0.1s)
└── BatchCheck()
    └── Iterate AudioEmitterRegistry.All
        └── Tính distance 2D
        └── Check distance <= cullingDistance
        └── emitter.SetInRange(inRange)
            ├── if !inRange → StopAll()
            └── OnRangeChanged?.Invoke(inRange)
```

### 4. Behavior Script Phát Sound

```
CampfireBehavior.OnEnable()
└── Subscribe: audioEmitter.OnRangeChanged += OnPlayerEnterRange

PlayerAudioZone.SetInRange(true)
└── OnRangeChanged(true) fired
    └── CampfireBehavior.OnPlayerEnterRange(true)
        └── audioEmitter.PlaySound("Campfire_Crackle", loop: true)
            └── Get AudioSource từ AudioPool
            ├── Set clip, volume, minDistance, maxDistance
            ├── Set custom rolloff curve
            ├── Set parent vào AudioEmitter
            └── source.Play()
```

### 5. Player Ra Khỏi Vùng

```
PlayerAudioZone.SetInRange(false)
└── OnRangeChanged(false) fired
    └── AudioEmitter.StopAll()
        └── Return tất cả AudioSource về AudioPool
```

### 6. AudioEmitter Được Disable

```
GameObject.Disable
└── AudioEmitter.OnDisable()
    ├── AudioEmitterRegistry.Unregister(this)
    └── StopAll()
        └── Return AudioSource về pool
```

---

## Distance Culling

### Khái Niệm

**cullingDistance** (AudioManager):
- Global threshold để quyết định có tạo AudioSource không
- Auto-set từ maxDistance lớn nhất trong SoundRegistry
- Nếu distance > cullingDistance → không phát, tiết kiệm pool/CPU

**maxDistance** (SoundEntry):
- Khoảng cách attenuation của từng sound
- AudioSource tự tính volume theo rolloff curve
- Volume về 0 tại maxDistance (custom curve)

### Ví Dụ

```
Player
  │
  ├──── 8 units ────► Campfire     → maxDistance=12   → NGHE ĐƯỢC (trong cả 2)
  │
  ├──── 20 units ───► Waterfall    → maxDistance=25   → NGHE ĐƯỢC (trong culling, chưa tới max)
  │
  ├──── 55 units ───► Distant_NPC → maxDistance=10   → KHÔNG PHÁT (ngoài culling=50)
  │
  └──── 55 units ───► Boss_Roar   → maxDistance=80   → VẪN PHÁT (max > culling)
```

### Custom Rolloff Curve

```csharp
AnimationCurve curve = new AnimationCurve();
curve.AddKey(0f,        1f); // Full volume tại 0
curve.AddKey(actualMin, 1f); // Full volume trong minDistance
curve.AddKey(actualMax, 0f); // Volume về 0 tại maxDistance
source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
```

---

## Loop Sound Pattern

### Demand-Based Audio System (Mới)

**Mục đích**: Loop sounds theo vòng đời object (portal, campfire) không bị dừng khi player ra xa, nhưng chỉ rent AudioSource khi trong range để tiết kiệm pool.

**Cơ chế hoạt động**:
```
Loop Sound Demand System
├── PlaySound(key, loop: true)
│   ├── Chỉ đăng ký demand (không rent AudioSource ngay)
│   ├── Thêm vào _demandedSounds dictionary
│   └── Nếu trong range → TriggerDemandedPlay() ngay
│
├── PlayerAudioZone (mỗi 0.1s)
│   └── SetInRange(true/false)
│       ├── Vào range: TriggerDemandedPlay() cho tất cả demanded sounds
│       └── Ra range: StopAllActiveSounds() (nhưng giữ demand queue)
│
└── StopSound(key)
    └── Xóa khỏi demand queue + stop active sound
```

**Lợi ích**:
- **Loop sound luôn theo vòng đời**: Portal luôn demand "Portal_Normal" dù player ở xa
- **Tiết kiệm pool**: Chỉ rent AudioSource khi thực sự trong range
- **PlayerAudioZone như sensor**: Mỗi 0.1s kích hoạt play khi cần thiết
- **Không lãng phí resources**: Loop sound ở xa không rent AudioSource từ pool

**Ví dụ - EnemyPortalBehavior**:
```csharp
public class EnemyPortalBehavior : MonoBehaviour
{
    private AudioEmitter _audioEmitter;

    public void Setup(...)
    {
        // Portal dormant → demand Portal_Normal loop
        // Chỉ rent AudioSource khi player vào range
        _audioEmitter?.PlaySound("Portal_Normal", new PlaySoundConfig(loop: true));
    }

    public void ForceActivate()
    {
        // Portal active → stop dormant, demand activate loop
        _audioEmitter?.StopSound("Portal_Normal");
        _audioEmitter?.PlaySound("Portal_Activate", new PlaySoundConfig(loop: true));
    }

    // KHÔNG cần OnRangeChanged logic - demand system tự handle
    // KHÔNG cần IsInRange check - demand system tự handle
}
```

**One-shot sounds vẫn play ngay lập tức**:
- `PlaySound(key, loop: false)` → rent AudioSource ngay, không qua demand system
- Loop sounds mới đi qua demand queue

---

### Sử Dụng OnRangeChanged Event (Legacy)

**Lưu ý**: Với demand-based system mới, OnRangeChanged event không còn cần thiết cho loop sounds. Loop sounds tự demand và PlayerAudioZone tự kích hoạt play khi vào range.

OnRangeChanged vẫn hữu ích cho các logic khác (ví dụ: visual effect khi player vào vùng).

```csharp
public class CampfireBehavior : MonoBehaviour
{
    private AudioEmitter _audioEmitter;

    private void OnEnable()
    {
        _audioEmitter = GetComponent<AudioEmitter>();
        // Với demand system, loop sound không cần OnRangeChanged
        _audioEmitter.PlaySound("Campfire_Crackle", new PlaySoundConfig(loop: true));

        // OnRangeChanged chỉ dùng cho logic khác
        _audioEmitter.OnRangeChanged += OnPlayerEnterRange;
    }

    private void OnPlayerEnterRange(bool inRange)
    {
        if (inRange)
        {
            // Logic visual effect khi player vào vùng
            // Loop sound đã được demand system handle
        }
    }

    private void OnDisable()
    {
        _audioEmitter.OnRangeChanged -= OnPlayerEnterRange;
    }
}
```

### Tại Sao Không Spam PlaySound

- **Tốn pool**: Mỗi lần play lấy AudioSource từ pool
- **Tốn CPU**: Kiểm tra mỗi frame không cần thiết
- **Không hiệu quả**: Loop sound đã có sẵn trong demand queue

### Ưu Điểm Demand-Based

- **Efficient**: Loop sound chỉ rent AudioSource khi trong range
- **Lifecycle**: Loop sound luôn theo vòng đời object, không bị dừng
- **Clean**: Không cần IsInRange check trong Behavior
- **Auto-stop**: AudioEmitter tự StopAllActiveSounds() khi ra vùng (nhưng giữ demand)

---

## Debug & Testing

### AudioPool Debug

Trong Inspector của AudioPool:
- `_availableCount`: Số lượng AudioSource sẵn sàng
- `_inUseCount`: Số lượng AudioSource đang active

### AudioEmitter Debug

Trong Inspector của AudioEmitter:
- `_debugInRange`: Trạng thái inRange
- `_activeSoundsDebug`: Danh sách sound đang phát

### PlayerAudioZone Debug

Trong Inspector của PlayerAudioZone:
- `_emitterCountInRange`: Số lượng emitter trong vùng
- Gizmo visualization (khi chọn object)

### AudioTest

- Tạo test object tại (0,0,0)
- CheckEmitterImmediate() trước khi phát
- Debug log với distance info
- Auto-select key với số

---

## Troubleshooting

### Không Nghe Được Sound

1. Kiểm tra AudioManager.Instance có null không
2. Kiểm tra SoundRegistry có gán đúng không
3. Kiểm tra playerTransform có gán không
4. Kiểm tra AudioEmitter có trong vùng không (CheckEmitterImmediate)
5. Kiểm tra AudioPool có hết nguồn không (maxPoolSize)

### Sound Vẫn Phát Dù Xa

1. Kiểm tra cullingDistance có đúng không
2. Kiểm tra maxDistance của SoundEntry
3. Kiểm tra custom rolloff curve
4. Kiểm tra volumeScale

### Pool Không Hiển Thị Active Sources

1. Kiểm tra _inUseCount trong Inspector
2. Kiểm tra AudioSource parent (AudioEmitter hoặc AudioPool)
3. Kiểm tra GameObject active state

---

## File Path Summary

```
Assets/Script/Audio/
├── Manager/
│   └── AudioManager.cs
├── Helper/
│   ├── AudioPool.cs
│   ├── AudioEmitterRegistry.cs
│   ├── AudioEmitter.cs
│   ├── PlayerAudioZone.cs
│   └── AudioTest.cs
├── Data/
│   └── SoundRegistry.cs
└── Example/
    └── CampfireBehavior.cs
```
