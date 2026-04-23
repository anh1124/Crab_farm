# Hệ Thống Audio - Sơ Đồ Kiến Trúc

## Tổng Quan Architecture

```mermaid
graph TB
    subgraph "Singleton Core"
        AM[AudioManager]
        AP[AudioPool]
        SR[SoundRegistry]
    end
    
    subgraph "Registry System"
        AER[AudioEmitterRegistry<br/>Static]
    end
    
    subgraph "Player System"
        PC[PlayerController]
        PAZ[PlayerAudioZone]
    end
    
    subgraph "Emitter System"
        AE[AudioEmitter]
        BEH[Behavior Script<br/>CampfireBehavior]
    end
    
    subgraph "Audio Sources (All Mono)"
        BGM1[BGM Source A<br/>spatialBlend=0]
        BGM2[BGM Source B<br/>spatialBlend=0]
        ENV[ENV Source<br/>spatialBlend=0]
        SFX[SFX Pool<br/>spatialBlend=0]
    end
    
    AM --> AP
    AM --> SR
    AM --> BGM1
    AM --> BGM2
    AM --> ENV
    AM --> SFX
    AM --> PAZ
    
    PC --> PAZ
    PAZ --> AER
    PAZ --> AE
    
    AE --> AER
    AE --> AP
    AE --> BEH
    
    BEH --> AE
    
    AER --> AE
    
    style AM fill:#ff9999
    style AER fill:#99ff99
    style PAZ fill:#9999ff
    style AE fill:#ffff99
```

---

## Component Relationships

```mermaid
graph LR
    AM[AudioManager] -->|Singleton| AM
    AM -->|Manages| AP[AudioPool]
    AM -->|References| SR[SoundRegistry]
    AM -->|Controls| BGM[BGM Sources<br/>Mono]
    AM -->|Controls| ENV[ENV Source<br/>Mono]
    AM -->|Debug Fields| DV[_masterVolume<br/>_bgmVolume<br/>_envVolume<br/>_sfxVolume<br/>_uiVolume]
    
    PAZ[PlayerAudioZone] -->|Polls| AER[AudioEmitterRegistry]
    PAZ -->|Sets Range| AE[AudioEmitter]
    PAZ -->|Gets| AM.CullingDistance
    
    AE -->|Registers| AER
    AE -->|Gets AudioSource| AP
    AE -->|Fires Event| BEH[Behavior]
    
    BEH -->|Calls PlaySound| AE
    
    AER -->|Contains| AE
    
    style AM fill:#ff9999
    style AER fill:#99ff99
    style PAZ fill:#9999ff
    style AE fill:#ffff99
```

---

## Data Flow - Startup

```mermaid
sequenceDiagram
    participant GM as GameManager
    participant AM as AudioManager
    participant AP as AudioPool
    participant SR as SoundRegistry
    participant PC as PlayerController
    participant PAZ as PlayerAudioZone
    
    GM->>AM: Awake()
    AM->>AM: Singleton Setup
    AM->>AM: Initialize()
    AM->>AP: Get Component
    AM->>AP: Awake()
    AP->>AP: Create Pooled Sources<br/>(initialPoolSize)
    AM->>SR: Get SoundRegistry
    AM->>AM: AutoSetCullingDistance()
    AM->>SR: GetAllKeys()
    SR-->>AM: List of keys
    AM->>SR: GetEntry(key) loop
    SR-->>AM: maxDistance
    AM->>AM: Set cullingDistance = max
    
    GM->>PC: SetUpControllerPlayer()
    PC->>PC: Add PlayerAudioZone (if missing)
    PC->>AM: Set PlayerTransform
    
    PAZ->>PAZ: Start()
    PAZ->>AM: Set PlayerTransform
    PAZ->>PAZ: StartCoroutine(PollRoutine())
```

---

## Data Flow - AudioEmitter Registration

```mermaid
sequenceDiagram
    participant GO as GameObject
    participant AE as AudioEmitter
    participant AER as AudioEmitterRegistry
    
    GO->>GO: Enable
    GO->>AE: OnEnable()
    AE->>AER: Register(this)
    AER->>AER: Add to _all List
    AER-->>AE: Registered
    
    Note over AE: PlayerAudioZone can now<br/>find this emitter
    
    GO->>GO: Disable
    GO->>AE: OnDisable()
    AE->>AE: StopAll()
    AE->>AER: Unregister(this)
    AER->>AER: Remove from _all List
    
    GO->>GO: Destroy
    GO->>AE: OnDestroy()
    AE->>AER: Unregister(this) (backup)
```

---

## Data Flow - Distance Culling

```mermaid
sequenceDiagram
    participant PAZ as PlayerAudioZone
    participant AM as AudioManager
    participant AER as AudioEmitterRegistry
    participant AE as AudioEmitter
    
    PAZ->>PAZ: PollRoutine() (every 0.1s)
    PAZ->>PAZ: BatchCheck()
    
    loop For each emitter
        PAZ->>AER: Get All
        AER-->>PAZ: List<AudioEmitter>
        
        PAZ->>PAZ: Calculate Distance 2D
        PAZ->>AM: Get CullingDistance
        AM-->>PAZ: cullingDistance
        
        alt distance <= cullingDistance
            PAZ->>AE: SetInRange(true)
            AE->>AE: _isInRange = true
            AE->>AE: Fire OnRangeChanged(true)
        else distance > cullingDistance
            PAZ->>AE: SetInRange(false)
            AE->>AE: _isInRange = false
            AE->>AE: StopAll()
            AE->>AE: Fire OnRangeChanged(false)
        end
    end
```

---

## Data Flow - Loop Sound (Campfire Example)

```mermaid
sequenceDiagram
    participant PAZ as PlayerAudioZone
    participant AE as AudioEmitter
    participant BEH as CampfireBehavior
    participant AM as AudioManager
    participant AP as AudioPool
    participant AS as AudioSource
    
    PAZ->>AE: SetInRange(true)
    AE->>AE: _isInRange = true
    AE->>BEH: OnRangeChanged(true)
    
    BEH->>AE: PlaySound("Campfire_Crackle", loop: true)
    AE->>AE: Check _isInRange
    AE->>AM: Get SoundRegistry
    AM-->>AE: SoundEntry
    AE->>AM: GetAudioSourceFromPool()
    AM->>AP: Get(worldPosition)
    AP->>AP: Dequeue or Create
    AP->>AS: SetActive(true)
    AP-->>AM: AudioSource
    AM-->>AE: AudioSource
    
    AE->>AS: Configure (clip, volume, loop)
    AE->>AS: Set Mono (spatialBlend=0)
    AE->>AS: Set Parent (AudioEmitter)
    AE->>AS: Play()
    
    Note over AS: Looping sound playing
    
    PAZ->>AE: SetInRange(false)
    AE->>AE: StopAll()
    AE->>AS: Stop()
    AE->>AP: ReturnAudioSourceToPool()
    AP->>AS: Reset State
    AP->>AS: SetActive(false)
    AP->>AS: Set Parent (AudioPool)
    AP->>AP: Enqueue to _available
```

---

## Data Flow - One-Shot Sound

```mermaid
sequenceDiagram
    participant BEH as Behavior
    participant AE as AudioEmitter
    participant AM as AudioManager
    participant AP as AudioPool
    participant AS as AudioSource
    
    BEH->>AE: PlaySound("Sword_Hit", loop: false)
    AE->>AE: Check _isInRange
    AE->>AM: GetAudioSourceFromPool()
    AM->>AP: Get(worldPosition)
    AP-->>AM: AudioSource
    AM-->>AE: AudioSource
    
    AE->>AS: Configure (clip, volume, loop=false)
    AE->>AS: Play()
    AE->>AE: StartCoroutine(ReturnAfterClip)
    
    Note over AS: One-shot playing
    
    AE->>AE: Wait for clip.length + 0.1s
    AE->>AE: Check if still in _activeSounds
    AE->>AP: ReturnAudioSourceToPool()
    AP->>AS: Reset State
    AP->>AS: SetActive(false)
    AP->>AS: Set Parent (AudioPool)
```

---

## Audio Pool Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Available: Create (Awake)
    Available --> InUse: Get()
    InUse --> Available: Return()
    InUse --> [*]: Destroy (Scene change)
    
    state Available {
        [*] --> Queued
        Queued --> [*]
    }
    
    state InUse {
        [*] --> Active
        Active --> Playing
        Playing --> Returning: Stop/End
        Returning --> [*]
    }
```

---

## Distance Culling Logic

```mermaid
graph TB
    subgraph "PlayerAudioZone"
        A[Get Player Position] --> B[Get Emitter Position]
        B --> C[Calculate Distance 2D]
        C --> D{Distance <= CullingDistance?}
    end
    
    subgraph "AudioManager"
        E[Auto-set CullingDistance<br/>from SoundRegistry]
    end
    
    subgraph "AudioEmitter"
        D -->|Yes| F[SetInRange true]
        D -->|No| G[SetInRange false]
        F --> H[Fire OnRangeChanged true]
        G --> I[StopAll]
        G --> J[Fire OnRangeChanged false]
    end
    
    E --> D
```

---

## Sound Attenuation Curve

```mermaid
graph LR
    subgraph "Volume vs Distance"
        A[0m<br/>100%] --> B[minDistance<br/>100%]
        B --> C[maxDistance<br/>0%]
    end
    
    style A fill:#90EE90
    style B fill:#90EE90
    style C fill:#FFB6C1
```

**Custom Rolloff Curve**:
```csharp
curve.AddKey(0f,        1f); // Full volume tại 0
curve.AddKey(minDistance, 1f); // Full volume trong minDistance
curve.AddKey(maxDistance, 0f); // Volume về 0 tại maxDistance
```

---

## Event-Driven Loop Sound

```mermaid
graph TB
    subgraph "Behavior Script"
        A[OnEnable] --> B[Subscribe OnRangeChanged]
        B --> C[OnDisable]
        C --> D[Unsubscribe OnRangeChanged]
    end
    
    subgraph "PlayerAudioZone"
        E[SetInRange true] --> F[Fire Event true]
        G[SetInRange false] --> H[Fire Event false]
    end
    
    subgraph "AudioEmitter"
        F --> I[OnRangeChanged true]
        H --> J[OnRangeChanged false]
    end
    
    subgraph "Sound Logic"
        I --> K[PlaySound loop:true]
        J --> L[StopAll Auto]
    end
    
    E --> F
    G --> H
    F --> I
    H --> J
```

---

## Performance Optimization

```mermaid
graph TB
    subgraph "Before Refactor"
        A1[AudioManager.Update] --> A2[Iterate All AudioSources]
        A2 --> A3[Check Distance Each]
        A3 --> A4[O n per frame]
    end
    
    subgraph "After Refactor"
        B1[PlayerAudioZone.Poll] --> B2[Every 0.1s]
        B2 --> B3[Iterate Emitters]
        B3 --> B4[SetInRange]
        B4 --> B5[O n per 0.1s]
        
        C1[AudioEmitter.PlaySound] --> C2[Check _isInRange]
        C2 --> C3[O 1 check]
    end
    
    style A4 fill:#FFB6C1
    style B5 fill:#90EE90
    style C3 fill:#90EE90
```

---

## File Structure

```
Assets/Script/Audio/
├── Manager/
│   └── AudioManager.cs
│       ├── Singleton Pattern
│       ├── AudioPool Management
│       ├── BGM Crossfade (with _bgmVolume field)
│       ├── ENV Control (with _envVolume field)
│       ├── VolumeSettings (PlayerPrefs)
│       ├── Debug Volume Fields (_master, _bgm, _env, _sfx, _ui)
│       ├── Force Mono Audio (spatialBlend=0)
│       └── CullingDistance Auto-set
│
├── Helper/
│   ├── AudioPool.cs
│   │   ├── Object Pooling (Mono: spatialBlend=0)
│   │   ├── Get/Return API
│   │   └── Debug Count
│   │
│   ├── AudioEmitterRegistry.cs
│   │   ├── Static Class
│   │   ├── Register/Unregister
│   │   └── All List
│   │
│   ├── AudioEmitter.cs
│   │   ├── Register/Unregister
│   │   ├── SetInRange
│   │   ├── OnRangeChanged Event
│   │   ├── PlaySound/StopSound (Mono Mode)
│   │   └── Loop Support
│   │
│   ├── PlayerAudioZone.cs
│   │   ├── Poll Routine
│   │   ├── BatchCheck
│   │   ├── Distance 2D Calculation
│   │   └── Gizmo Visualization
│   │
│   └── AudioTest.cs
│       ├── Test Utility
│       ├── Auto-select Key
│       └── Debug Log
│
├── Data/
│   └── SoundRegistry.cs
│       ├── SoundEntry Dictionary
│       ├── GetEntry API
│       └── GetAllKeys API
│
└── Example/
    └── CampfireBehavior.cs
        ├── OnRangeChanged Subscribe
        └── Loop Sound Pattern
```

---

## Key Concepts Summary

### Registry + Poll Architecture
- AudioEmitterRegistry: Static registry lưu danh sách emitter
- PlayerAudioZone: Poll registry mỗi 0.1s để check distance
- Không dùng Update() O(n) trong AudioManager

### Event-Driven Range Changes
- AudioEmitter.OnRangeChanged event
- Behavior scripts subscribe để phát/dừng loop sound
- Auto-stop khi ra vùng

### Object Pooling
- AudioPool tái sử dụng AudioSource
- Tránh tạo/hủy GameObject tốn CPU/GC
- Giới hạn maxPoolSize để tránh overflow

### Distance Culling
- cullingDistance: Global threshold để quyết định phát hay không
- maxDistance: Per-sound attenuation
- Auto-set cullingDistance từ SoundRegistry

### Mono Audio Architecture
- Tất cả AudioSources: `spatialBlend = 0f` (2D)
- Không có 3D positioning hay stereo panning
- Volume controlled qua fields trong AudioManager

### Debug Volume Fields
- `_masterVolume`, `_bgmVolume`, `_envVolume`, `_sfxVolume`, `_uiVolume`
- Hiển thị trong Inspector để debug
- Được cập nhật khi `SetVolumeForChannel()` được gọi
