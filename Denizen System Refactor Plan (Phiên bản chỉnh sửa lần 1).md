Dưới đây là phiên bản **chỉnh sửa** của `denizen_refactor_plan.md`, dựa trên các xác nhận của bạn và kết quả audit.  
Các thay đổi chính được đánh dấu bằng **`[UPDATED]`** hoặc giải thích trong comment.

---

# Denizen System Refactor Plan (Phiên bản chỉnh sửa)

> **Mục tiêu:** Chuyển từ kiến trúc MonoBehaviour-per-role sang State Pattern giống Enemy system.  
> **Nguyên tắc bất biến:** Root GameObject của mỗi Denizen **không bao giờ bị destroy hay replace** – khi đổi nghề, chỉ thay đổi Job component (brain) chứ không destroy/hồi sinh đối tượng.  
> **Người đọc:** AI code agent — đọc toàn bộ file này trước khi viết bất kỳ dòng code nào.

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Cấu trúc thư mục](#2-cấu-trúc-thư-mục)
3. [Cấu trúc Prefab (Inspector)](#3-cấu-trúc-prefab-inspector)
4. [Sơ đồ quan hệ giữa các class](#4-sơ-đồ-quan-hệ-giữa-các-class)
5. [Phase 1 — Infrastructure](#5-phase-1--infrastructure)
6. [Phase 2 — VagrantJob](#6-phase-2--vagrantjob)
7. [Phase 3 — VillagerJob](#7-phase-3--villagerjob)
8. [Phase 4 — ArcherJob](#8-phase-4--archerjob)
9. [Phase 5 — Cleanup & Migration](#9-phase-5--cleanup--migration)
10. [Hướng dẫn setup Inspector](#10-hướng-dẫn-setup-inspector)
11. [Các lưu ý quan trọng](#11-các-lưu-ý-quan-trọng)
12. [Compatibility Layer – Các script ngoài cần cập nhật](#12-compatibility-layer--các-script-ngoài-cần-cập-nhật)

---

## 1. Tổng quan kiến trúc

### Vấn đề hiện tại

Mỗi file `*Behavior.cs` đang gánh quá nhiều trách nhiệm cùng lúc: quản lý state machine, di chuyển, animation, detection, combat, save/load. Kết quả là mỗi file vượt 1000 dòng và rất khó mở rộng.

### Kiến trúc mới — 3 tầng

```
Tầng 1 — Điều phối (DenizenCore)
    Biết role hiện tại là gì, bật/tắt Job tương ứng.

Tầng 2 — Job (VagrantJob / VillagerJob / ArcherJob)
    MonoBehaviour được enable/disable theo role.
    Mỗi Job sở hữu một tập States được pre-alloc sẵn.
    Chỉ delegate xuống currentState, không chứa logic state.

Tầng 3 — State (plain C# class)
    Mỗi State chỉ làm đúng 1 việc.
    Nhận DenizenContext thay vì tự GetComponent.
    Không kế thừa MonoBehaviour → 0 overhead khi inactive.
```

### So sánh với Enemy system

| Thành phần Enemy | Thành phần Denizen tương ứng |
|---|---|
| `EnemyAIController` | `DenizenCore` |
| `EnemyContext` | `DenizenContext` |
| `EnemyState` (abstract) | `DenizenState` (abstract) |
| Concrete states (Move, Combat...) | VagrantIdleState, ArcherHuntingState... |
| Logic tick qua NPCUpdateManager | Giữ nguyên `NPCUpdateManager` |
| Frame tick qua MonoBehaviour.Update | `DenizenMovement.Update()` |

---

## 2. Cấu trúc thư mục

```
Assets/Script/Game/Mobs/Denizen/
│
├── Core/
│   ├── DenizenCore.cs              ← thay thế DenizenRoleController.cs
│   ├── DenizenContext.cs           ← plain C# class, túi refs được cache
│   ├── DenizenState.cs             ← abstract base, plain C# class
│   ├── DenizenJob.cs               ← abstract MonoBehaviour
│   ├── DenizenMovement.cs          ← MonoBehaviour, tách movement ra khỏi behavior
│   └── DenizenAnimationEvents.cs   ← giữ file, chỉnh cách route
│
├── Jobs/
│   ├── Vagrant/
│   │   ├── VagrantJob.cs
│   │   ├── States/
│   │   │   ├── VagrantIdleState.cs
│   │   │   ├── VagrantFearState.cs
│   │   │   └── VagrantGetCoinState.cs
│   │
│   ├── Villager/
│   │   ├── VillagerJob.cs
│   │   ├── States/
│   │   │   ├── VillagerIdleState.cs
│   │   │   ├── VillagerFearState.cs
│   │   │   ├── VillagerToTownState.cs
│   │   │   ├── VillagerRunToolState.cs
│   │   │   └── VillagerDropCoinState.cs
│   │
│   └── Archer/
│       ├── ArcherJob.cs
│       ├── States/
│       │   ├── ArcherIdleState.cs
│       │   ├── ArcherHuntingState.cs
│       │   ├── ArcherHoldWallState.cs
│       │   ├── ArcherRetreatState.cs
│       │   ├── ArcherReturnToBaseState.cs
│       │   └── ArcherDropCoinState.cs
│
└── Shared/                         ← KHÔNG đụng vào các file này
    ├── DenizenStatus.cs
    ├── DenizenTimeHandler.cs
    ├── DenizenCoinSharing.cs
    └── ArcherShooting.cs
```

> **File cũ:** `VagrantBehavior.cs`, `VillagerBehavior.cs`, `ArcherBehavior.cs`, `DenizenRoleController.cs` — giữ nguyên cho đến Phase 5, xóa sau khi mọi thứ chạy ổn.

---

## 3. Cấu trúc Prefab (Inspector)

### Hierarchy

```
Denizen (root GameObject)
│
├── [Scripts trên root]
│   ├── DenizenCore
│   ├── DenizenContext (UPDATED: MonoBehaviour)
│   ├── DenizenStatus
│   ├── DenizenTimeHandler
│   ├── DenizenMovement
│   ├── DenizenCoinSharing
│   ├── DenizenAnimationEvents
│   ├── SpriteRenderer
│   └── Animator
│
├── Child: Jobs
│   ├── VagrantJob          ← enabled = true (default role)
│   ├── VillagerJob         ← enabled = false
│   └── ArcherJob           ← enabled = false
│
├── Child: Detection
│   ├── RayCastHelper
│   └── CircleDetectHelper
│
└── Child: ArcherEquipment
    └── ArcherShooting
```

### Lý do phân chia

- **Root** chứa những thứ luôn active bất kể role. DenizenStatus, DenizenMovement, Animator không bao giờ bị tắt.
- **Child: Jobs** chứa các MonoBehaviour được enable/disable. Tách sang child giữ root Inspector gọn. Ba Job này tồn tại suốt vòng đời prefab, chỉ thay đổi trạng thái enabled.
- **Child: Detection** chứa sensor. Tách ra cho phép sau này đặt offset transform độc lập với sprite pivot nếu cần.
- **Child: ArcherEquipment** chứa weapon logic. Khi thêm Worker/Farmer sau này, `BuilderEquipment`, `FarmerEquipment` sẽ nằm tại đây theo cùng pattern.

---

## 4. Sơ đồ quan hệ giữa các class

### Quan hệ tổng thể

```
DenizenCore
    │   tạo và inject
    ▼
DenizenContext ──────────────────────────────────────────┐
    │   được truyền vào                                   │
    ▼                                                     │
DenizenJob (abstract MonoBehaviour)                      │
    │   enable/disable bởi DenizenCore                   │
    │   sở hữu                                           │
    ▼                                                     │
DenizenState (abstract C# class)  ◄──── nhận ctx ────────┘
    ├── Enter(ctx)
    ├── Update(ctx)        ← logic tick, gọi qua NPCUpdateManager
    ├── FrameUpdate(ctx)   ← movement tick, gọi qua MonoBehaviour.Update
    └── Exit(ctx)
```

### Luồng đổi role

```
ChangeRole(newRole) được gọi
        │
        ▼
DenizenCore.ChangeRole(newRole)
        │
        ├─► UnregisterFromManager(oldRole)
        ├─► currentJob.enabled = false   ← trigger OnDisable() → currentState.Exit()
        ├─► currentRole = newRole
        ├─► context.Status.Role = newRole
        ├─► ApplyJob(newRole)             ← set animator controller, tag, currentJob
        ├─► currentJob.enabled = true     ← trigger OnEnable() → enter initial state
        └─► RegisterWithManager()
```

### Luồng đổi state trong Job

```
job.ChangeState(nextState)
        │
        ├─► currentState.Exit(context)
        ├─► ResetAnimatorParams()         ← reset TẤT CẢ bool/trigger về default
        ├─► currentState = nextState
        └─► currentState.Enter(context)
```

### Luồng Update mỗi frame

```
NPCUpdateManager.Tick()
        │
        └─► DenizenCore.UpdateBehavior()
                │
                └─► currentJob.UpdateBehavior()
                        │
                        └─► currentState.Update(context)    ← AI decisions

MonoBehaviour.Update() trên DenizenMovement
        │
        └─► xử lý _moveTarget, _moveSpeed, transform.position +=

MonoBehaviour.Update() trên currentJob (nếu cần FrameUpdate)
        │
        └─► currentState.FrameUpdate(context)               ← smooth movement
```

---

## 5. Phase 1 — Infrastructure

> Tạo các file nền tảng. **Chưa xóa hay sửa bất kỳ file cũ nào.** Các component mới sẽ được thêm vào prefab và chạy song song với hệ thống cũ trong quá trình test.

### 5.1 `DenizenState.cs` (abstract, plain C# class)

**Namespace:** `Script.Game.Mobs.Denizen.Core`

**Mô tả:** Base class cho tất cả states. Không kế thừa MonoBehaviour.

**Các method cần có:**

- `virtual void Enter(DenizenContext ctx)` — override để khởi tạo state, set speed, reset timer. Mặc định rỗng.
- `abstract void Update(DenizenContext ctx)` — bắt buộc override, chứa AI logic và điều kiện chuyển state.
- `virtual void FrameUpdate(DenizenContext ctx)` — override nếu state cần smooth movement 60fps. Mặc định rỗng.
- `virtual void Exit(DenizenContext ctx)` — override để dọn dẹp, clear target. Mặc định rỗng.

---

### 5.2 DenizenContext.cs (MonoBehaviour)

**Namespace:** `Script.Game.Mobs.Denizen.Core`

**Mô tả:** Component chứa tất cả references được cache sẵn. Việc gán tham chiếu có thể thực hiện thủ công trên Inspector để tối ưu hiệu năng (Zero-cost at Runtime). 

**Tính năng đặc biệt:**
- Có hàm `[ContextMenu("Auto Find References")]` để tự động điền các field trong Editor.
- Tự gán các Singletons (Managers) trong `Awake()` nếu chưa được gán trên Inspector.

**Fields — chia theo region:**

```
#region Core — mọi Job đều dùng
    DenizenStatus Status
    DenizenCore Core
    DenizenMovement Movement
    Animator Animator
    Transform Transform
    GameObject GameObject
#endregion

#region Detection — refs tới Child: Detection
    RayCastHelper RayCaster
    CircleDetectHelper CircleDetect
#endregion

#region Managers — cached, không gọi .Instance trong game loop
    GuardManager GuardManager
    GameTimeManager TimeManager
    WallAndStumpManager WallManager
    NPCUpdateManager NPCUpdateManager
#endregion

#region Archer-specific — chỉ ArcherJob và ArcherStates dùng
    // Comment rõ: các field dưới đây null với Vagrant/Villager, kiểm tra null trước khi dùng
    ArcherShooting ArcherShooting
    DenizenCoinSharing CoinSharing
#endregion
```

**Constructor `DenizenContext(GameObject go)`:**
- `GetComponent<>()` cho tất cả root components.
- `GetComponentInChildren<>()` cho Detection child và ArcherEquipment child.
- Gán tất cả Manager refs qua `.Instance`.
- Không throw exception nếu một component không tồn tại — để null, state sẽ null-check khi dùng.

---

### 5.3 `DenizenJob.cs` (abstract MonoBehaviour)

**Namespace:** `Script.Game.Mobs.Denizen.Core`

**Mô tả:** Base class cho VagrantJob, VillagerJob, ArcherJob. Quản lý state machine. Không chứa logic game cụ thể.

**Fields:**

- `protected DenizenContext Context` — được inject bởi DenizenCore sau khi construct.
- `protected DenizenState CurrentState` — state đang chạy.

**Method cần có:**

- `public void InjectContext(DenizenContext ctx)` — DenizenCore gọi sau khi tạo context. Lưu ctx vào field.
- `protected abstract DenizenState GetInitialState()` — mỗi Job khai báo state mặc định khi enable.
- `protected abstract void PreAllocateStates()` — gọi trong Awake, tạo tất cả state instances bằng `new`. Không dùng Instantiate.
- `public void ChangeState(DenizenState next)` — thực hiện đúng 3 bước: Exit cũ → ResetAnimatorParams → Enter mới. Bước ResetAnimatorParams gọi method riêng.
- `protected virtual void ResetAnimatorParams()` — reset tất cả Animator bool/trigger về false/default. Override trong từng Job nếu animator params khác nhau.
- `protected virtual void OnEnterJob()` — DenizenCore gọi sau khi enable. Dùng để subscribe detector events.
- `protected virtual void OnExitJob()` — DenizenCore gọi trước khi disable. Dùng để unsubscribe.
- `public void UpdateBehavior()` — implements `INPCBehavior`. Chỉ gọi `CurrentState?.Update(Context)`.
- `private void Update()` — chỉ gọi `CurrentState?.FrameUpdate(Context)`.
- `protected virtual void OnEnable()` — gọi `OnEnterJob()`, gọi `ChangeState(GetInitialState())`.
- `protected virtual void OnDisable()` — gọi `CurrentState?.Exit(Context)`, gọi `OnExitJob()`.

> **Lưu ý quan trọng:** `ChangeState()` phải kiểm tra `if (next == CurrentState) return` để tránh re-enter cùng state. `ResetAnimatorParams()` phải được gọi giữa Exit và Enter, không phải trước Exit hay sau Enter.

---

### 5.4 `DenizenMovement.cs` (MonoBehaviour)

**Namespace:** `Script.Game.Mobs.Denizen.Core`

**Mô tả:** Tách toàn bộ movement logic hiện đang bị duplicate trong cả 3 Behavior cũ. Là component duy nhất được phép ghi `transform.position` theo từng frame.

**Fields:**

- `private Vector3 _moveTarget`
- `private float _moveSpeed`
- `private bool _isMovingToTarget`

**Properties:**

- `public bool HasReachedTarget` — true khi distance < threshold (0.1f).
- `public bool IsMoving` — get `_isMovingToTarget`.

**Methods:**

- `public void MoveTo(Vector3 target, float speed)` — set _moveTarget, _moveSpeed, _isMovingToTarget = true.
- `public void StopMovement()` — set _isMovingToTarget = false, speed = 0.
- `public void FaceTarget(Vector3 targetPos)` — set localScale.x theo hướng.
- `public void FaceOutward(bool isLeftFlank)` — archer-specific, set scale theo flank.
- `public void FlipTowards(Vector3 targetPos)` — alias của FaceTarget, dùng cho Vagrant/Villager.
- `private void Update()` — nếu _isMovingToTarget: tính direction, MoveTowards, check arrive.

---

### 5.5 `DenizenCore.cs` (MonoBehaviour)

**Namespace:** `Script.Game.Mobs.Denizen.Core`

**Mô tả:** Thay thế `DenizenRoleController`. Slim hơn nhiều vì không còn chứa state logic. Chỉ điều phối Job và serve as central hub.

**Fields:**

- `public NPCRole CurrentRole`
- `private DenizenContext _context`
- `[SerializeField] private VagrantJob _vagrantJob` — kéo từ Inspector (Child: Jobs)
- `[SerializeField] private VillagerJob _villagerJob` — kéo từ Inspector
- `[SerializeField] private ArcherJob _archerJob` — kéo từ Inspector
- `private DenizenJob _currentJob`
- `private DenizenStatus _status`
- `[HideInInspector] private NPCRole _previousRole`

**Methods:**

- `private void Awake()` — khởi tạo `_context = new DenizenContext(gameObject)`, inject context vào tất cả 3 Jobs, DisableAllJobs(), cache _status.
- `private void Start()` — ApplyRole(CurrentRole), RegisterWithManager().
- `public void ChangeRole(NPCRole newRole)` — xem luồng ở mục 4. Giữ nguyên logic unregister/register Manager.
- `private void ApplyRole(NPCRole role)` — enable đúng Job, set animator controller, tag, cập nhật _currentJob.
- `private void DisableAllJobs()` — tắt cả 3 Job components.
- `public void UpdateBehavior()` — implements INPCBehavior, delegate xuống `_currentJob?.UpdateBehavior()`.
- `public void OnAnimationEvent(string eventName)` — DenizenAnimationEvents gọi vào đây. DenizenCore delegate xuống `_currentJob?.HandleAnimationEvent(eventName)`.
- `OnDayStart()`, `OnNightStart()`, `OnArcherHuntingTime()` — giữ nguyên signature, delegate xuống đúng Job.
- Giữ nguyên toàn bộ `ISaveable`, `IItemHandle` implementation — chỉ đổi target từ behavior cũ sang job mới.

> **Lưu ý OnValidate:** Giữ nguyên toàn bộ `#if UNITY_EDITOR` block từ DenizenRoleController.cs cũ — logic này đúng và quan trọng.

---
Trước khi gán Animator Controller mới, gọi _animator.Rebind() hoặc ít nhất reset các parameter thông dụng bằng cách lưu danh sách parameter names từ controller cũ và reset chúng về default. Tuy nhiên cách an toàn nhất là gọi _animator.Rebind() (reset toàn bộ state machine về trạng thái ban đầu) trước khi gán controller mới.

Thêm dòng:

// Trong ApplyRole, trước khi gán animator.runtimeAnimatorController = newController
if (_context.Animator != null)
{
    _context.Animator.Rebind();
}

### 5.6 `DenizenAnimationEvents.cs` (chỉnh nhẹ)

**Mô tả:** Giữ file chung cho tất cả animation events. **Routing qua DenizenCore.**

**Thay đổi:**

- Thêm field `private DenizenCore _core`.
- Trong `Awake()`, cache `_core = GetComponent<DenizenCore>()`.
- Tất cả event methods (`OnDrawComplete`, `OnArrowRelease`, và future events) đều gọi `_core?.OnAnimationEvent("EventName")` thay vì `GetComponent<ArcherBehavior>()` trực tiếp.
- **Trong giai đoạn chuyển tiếp (song song):** Nếu `_core` null (khi chưa thay thế hoàn toàn), fallback về logic cũ (gọi trực tiếp ArcherBehavior) để không break game.

**Ví dụ:**

```csharp
public void OnDrawComplete()
{
    if (_core != null)
    {
        _core.OnAnimationEvent("DrawComplete");
    }
    else
    {
        // Fallback cho đến khi xóa behavior cũ
        var archer = GetComponent<ArcherBehavior>();
        if (archer != null && archer.enabled) archer.HandleDrawComplete();
    }
}
```

**Lý do:** DenizenCore biết Job nào đang active và sẽ route đúng. DenizenAnimationEvents không cần biết về ArcherJob, BuilderJob hay bất kỳ Job cụ thể nào.

---

## 6. Phase 2 — VagrantJob

> Implement Job đơn giản nhất để validate pattern trước khi làm Villager/Archer. **Giữ nguyên VagrantBehavior.cs hoạt động song song** – chỉ khi VagrantJob đã chạy ổn định mới xóa.

### 6.1 `VagrantJob.cs`

**Kế thừa:** `DenizenJob`

**State instances (pre-alloc trong Awake):**

- `private VagrantIdleState _idleState`
- `private VagrantFearState _fearState`
- `private VagrantGetCoinState _getCoinState`

**Override methods:**

- `PreAllocateStates()` — `new` tất cả 3 state instances.
- `GetInitialState()` — return `_idleState`.
- `OnEnterJob()` — subscribe `context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected`. Đặt rayCaster tags = `TargetLists.VagrantTargets`. Set isActive = true.
- `OnExitJob()` — unsubscribe, set isActive = false.
- `HandleAnimationEvent(string)` — Vagrant không có animation events đặc biệt, để trống hoặc ignore.

**Method riêng:**

- `private void OnObjectsDetected(List<GameObject> objects)` — phân loại Enemy/Coin, gọi `ChangeState(_fearState)` hoặc `ChangeState(_getCoinState)`. Logic này được chuyển từ `VagrantBehavior.OnObjectsDetected()`.

**Coroutines:** `RandomFlipRoutine` — giữ nguyên, chạy trong OnEnable, stop trong OnDisable.

---

### 6.2 States của VagrantJob

Mỗi state chỉ đọc/ghi các field **thực sự cần persist** qua save trong `DenizenStatus`. Các biến tạm (timer cục bộ, target cục bộ) là **field của State class**, không phải DenizenStatus.

#### `VagrantIdleState`

**Fields riêng:** _(không cần, mọi thứ có trong ctx.Status)_

- `Enter(ctx)` — không làm gì đặc biệt.
- `Update(ctx)` — nếu `ctx.Status.vagrantState == Fear` → `job.ChangeState(fearState)`. Nếu `coinTarget != null` → `job.ChangeState(getCoinState)`. Ngược lại chọn random walk target.
- `FrameUpdate(ctx)` — nếu đang có move target thì `ctx.Movement.MoveTo(...)`. Set animation "walk" hoặc "idle".
- `Exit(ctx)` — không cần.

> **Lưu ý:** State không tự gọi `ChangeState()` trực tiếp — nó gọi qua `job` reference. Cần truyền `job` vào state. Cách đơn giản: State constructor nhận `VagrantJob job` và lưu làm field. Hoặc dùng `ctx.Core` rồi cast — nhưng cách constructor sạch hơn.

#### `VagrantFearState`

**Fields riêng:** `private float _fearEndTime`

- `Enter(ctx)` — set `_fearEndTime = Time.time + 30f`. Set animation "fear". `ctx.Movement.StopMovement()`.
- `Update(ctx)` — nếu `Time.time >= _fearEndTime` → `job.ChangeState(idleState)`.
- `FrameUpdate(ctx)` — không di chuyển, chỉ đứng im.
- `Exit(ctx)` — không cần.

#### `VagrantGetCoinState`

- `Enter(ctx)` — validate `ctx.Status.coinTarget != null`.
- `Update(ctx)` — nếu coinTarget null/inactive → `job.ChangeState(idleState)`. Update _moveTarget = coinTarget.position.
- `FrameUpdate(ctx)` — `ctx.Movement.MoveTo(coinTarget.position, runSpeed)`. Set animation "run".
- `Exit(ctx)` — không cần.

---

## 7. Phase 3 — VillagerJob

### 7.1 `VillagerJob.cs`

**Kế thừa:** `DenizenJob`

**State instances:**

- `VillagerIdleState`, `VillagerFearState`, `VillagerToTownState`, `VillagerRunToolState`, `VillagerDropCoinState`

**Override methods:**

- `PreAllocateStates()` — new tất cả 5 states.
- `GetInitialState()` — return `_idleState` hoặc `_toTownState` tuỳ theo giờ hiện tại (kiểm tra `TimeManager.GameHour`).
- `OnEnterJob()` — subscribe RayCaster. Set tags = `TargetLists.VillagerTargets`.
- `OnExitJob()` — unsubscribe. Cleanup `currentTool` nếu có.
- `HandleAnimationEvent(string)` — Villager không có animation events, để trống.

**Xử lý `isFearPending` flag:**

Trong `OnEnterJob()` (hoặc cuối `Awake()` khi role được áp dụng), kiểm tra `ctx.Status.isFearPending`. Nếu true: tắt cờ, gọi `ChangeState(_fearState)` thay vì `GetInitialState()`. Đây là cơ chế cross-role fear khi Archer bị hạ xuống Villager.

**Tool assignment:**

- `public void AssignTool(PhysicalItem tool)` — giữ nguyên logic từ VillagerBehavior.AssignTool(). Gọi `ChangeState(_runToolState)`.
- `public void TransitionToIdle()` — public method để states gọi khi cần về Idle.

---

### 7.2 States của VillagerJob

#### `VillagerIdleState`

**Fields riêng:** `private float _lastToolCheckTime`

- `Enter(ctx)` — reset `_lastToolCheckTime = -999f` để trigger tool check ngay.
- `Update(ctx)` — priority check theo thứ tự: (1) isInDanger → ChangeState Fear; (2) currentTool != null → ChangeState RunTool; (3) ban đêm → ChangeState ToTown; (4) có Player trong range và có coin → ChangeState DropCoin. Nếu đủ thời gian: `CheckForTools()`.
- `FrameUpdate(ctx)` — `ctx.Movement.MoveTo(randomPlazaPos, walkSpeed)`. Animation "walk".
- `CheckForTools()` — logic từ VillagerBehavior.CheckForToolsOnSpawn().

#### `VillagerFearState`

- `Enter(ctx)` — set animation "run", chọn random plaza pos làm flee target.
- `Update(ctx)` — đếm xuôi `ctx.Status.fearTimer`. Khi về 0: set `isInDanger = false`, chuyển sang Idle hoặc ToTown tuỳ giờ.
- `FrameUpdate(ctx)` — `ctx.Movement.MoveTo(fleeTarget, runSpeed)`. Nếu tới nơi thì chọn target mới.
- `Exit(ctx)` — không cần.

#### `VillagerToTownState`

- `Enter(ctx)` — chọn random plaza position.
- `Update(ctx)` — nếu sang ban ngày → ChangeState Idle. Nếu isInDanger → ChangeState Fear.
- `FrameUpdate(ctx)` — MoveTo plaza, animation "walk".

#### `VillagerRunToolState`

- `Enter(ctx)` — validate tool không null.
- `Update(ctx)` — nếu tool null/picked → `job.TransitionToIdle()`. Nếu isInDanger → ChangeState Fear.
- `FrameUpdate(ctx)` — `ctx.Movement.MoveTo(tool.transform.position, runSpeed)`. Animation "run".
- `Exit(ctx)` — không clear currentTool ở đây — việc clear được xử lý khi pickup trigger.

#### `VillagerDropCoinState`

- `Enter(ctx)` — stop movement, animation "idle".
- `Update(ctx)` — nếu Player không còn trong range hoặc `CoinSharing.CanDropCoin() == false` → ChangeState về state phù hợp. Ngược lại gọi `ctx.CoinSharing.TryDropCoin()`.
- `FrameUpdate(ctx)` — không di chuyển.

---

## 8. Phase 4 — ArcherJob

### 8.1 `ArcherJob.cs`

**Kế thừa:** `DenizenJob`

**State instances:**

- `ArcherIdleState`, `ArcherHuntingState`, `ArcherHoldWallState`, `ArcherRetreatState`, `ArcherReturnToBaseState`, `ArcherDropCoinState`

**Override methods:**

- `PreAllocateStates()` — new tất cả 6 states.
- `GetInitialState()` — kiểm tra `GuardManager.isEnemyAttack`: nếu true return `_returnToBaseState`. Nếu false: `TimeManager.IsDaytime` ? `_huntingState` : `_holdWallState`.
- `OnEnterJob()` — subscribe `CircleDetect.OnDetectedObjectsChanged`. Set tags từ `TargetLists.ArcherTargets` + thêm Player tag riêng cho detect (không phải combat). Set `circleDetect.isActive = true`.
- `OnExitJob()` — unsubscribe CircleDetect, set isActive = false.
- `HandleAnimationEvent(string eventName)` — switch trên eventName: "DrawComplete" → current state phải là `ArcherHoldWallState` hoặc `ArcherHuntingState` → gọi `archerShooting` logic; "ArrowRelease" → tương tự. Có thể cast `CurrentState` sang interface `IArcherCombat` nếu muốn sạch hơn.

**Public methods để States gọi:**

- `public void OnObjectsDetected(List<GameObject> objects)` — logic từ ArcherBehavior.OnObjectsDetected(). Tìm combat target, tìm Player để DropCoin. Update `ctx.Status.currentTarget`. Gọi ChangeState nếu cần.
- `public void ForceRetreat(GameObject defensiveWall)` — GuardManager gọi trực tiếp vào Job. Cache retreat target, ChangeState RetreatState.
- `public void OnGuardManagerStatusChanged(bool isEnemyAttack)` — GuardManager gọi khi báo động thay đổi.
- `public void OnDayBreaks()` — DenizenCore route vào đây từ DenizenTimeHandler.
- `public void OnNightFalls()` — DenizenCore route vào đây từ DenizenTimeHandler.

---

### 8.2 States của ArcherJob

Mỗi ArcherState nhận `ArcherJob job` qua constructor để gọi `job.ChangeState()`. Các biến runtime như `huntingTargetPos`, `lastPatrolTime` là **field của State**, không lưu vào DenizenStatus.

#### `ArcherIdleState`

- `Update(ctx)` — check time, delegate sang HuntingState hoặc ReturnToBaseState.

#### `ArcherHuntingState`

**Fields riêng:** `private Vector3? _huntingTargetPos`, `private float _lastMoveTime`

- `Enter(ctx)` — reset `_huntingTargetPos = null`.
- `Update(ctx)` — nếu `ctx.Status.currentTarget != null` → dừng patrol, quay mặt về target, check cooldown attack. Nếu không có target → cập nhật patrol point nếu đến nơi hoặc timeout. Nếu GuardManager báo alarm → `job.ChangeState(returnToBaseState)`.
- `FrameUpdate(ctx)` — MoveTo _huntingTargetPos nếu không có target. Animation "archerMove".
- Logic `CalculateHuntingPatrolPosition()` — giữ nguyên từ ArcherBehavior, đặt làm private method của state này.

#### `ArcherHoldWallState`

- `Enter(ctx)` — `ctx.Movement.StopMovement()`. Face outward.
- `Update(ctx)` — nếu có target: face target, check attack cooldown, gọi attack. Nếu không có target: lấy defensive position từ GuardManager, di chuyển nếu chưa đến.
- Animation "archerIdle" khi đứng chờ, "archerMove" khi đi về wall.

> **Lưu ý attack animation:** Khi gọi attack, set animation "archerDraw". Sau đó Animation Event sẽ gọi vào ArcherJob → "DrawComplete" → set "archerRelease". "ArrowRelease" event → bắn đạn → set về "archerIdle". Không tự ý set animation về idle trong state — để Animation Event lo.

#### `ArcherRetreatState`

**Fields riêng:** `private GameObject _retreatTarget` — được set bởi `ArcherJob.ForceRetreat()` trước khi ChangeState.

- `Enter(ctx)` — validate _retreatTarget. Nếu null → `job.ChangeState(returnToBaseState)`.
- `Update(ctx)` — kiểm tra distance tới retreat pos. Nếu đến nơi: cập nhật assignedWall, ChangeState HoldWall.
- `FrameUpdate(ctx)` — MoveTo retreat position, animation "archerMove".
- `Exit(ctx)` — clear _retreatTarget.

#### `ArcherReturnToBaseState`

- `Enter(ctx)` — lấy wall từ GuardManager, update `ctx.Status.assignedWall`.
- `Update(ctx)` — nếu đến defensive position → ChangeState HoldWall.
- `FrameUpdate(ctx)` — MoveTo defensive position, animation "archerMove".

#### `ArcherDropCoinState`

- `Enter(ctx)` — `ctx.Movement.StopMovement()`. Save `previousArcherState` vào `ctx.Status.previousArcherState`.
- `Update(ctx)` — nếu Player không còn trong CircleDetect hoặc `CoinSharing.CanDropCoin() == false` → ChangeState về previousArcherState. Ngược lại gọi TryDropCoin(). Animation "archerIdle".
- `Exit(ctx)` — không cần.

---

## 9. Phase 5 — Cleanup & Migration

> Thực hiện sau khi tất cả Job/State đã chạy ổn định trong editor và build, và sau khi đã chuyển hết các script ngoài tham chiếu sang DenizenCore.

**Các file cần xóa:**

- `VagrantBehavior.cs`
- `VillagerBehavior.cs`
- `ArcherBehavior.cs`
- `DenizenRoleController.cs`

**Kiểm tra trước khi xóa:**

1. Chạy game, thử đổi role qua Inspector → không có lỗi.
2. Thử save/load → Denizen restore đúng role và state.
3. Thử combat flow: Enemy tấn công Archer → Archer rớt xuống Villager → Villager Fear → hết coin → Vagrant.
4. Thử time events: đến giờ 19 → Villager về town, Archer về wall. Đến giờ 7 → Archer đi hunt.
5. Kiểm tra GuardManager → báo động → tất cả Archer chuyển ReturnToBase.
6. **Kiểm tra các script ngoài đã được cập nhật** (xem mục 12).

**Sau khi xóa:** Chạy lại toàn bộ test trên. Đảm bảo không còn reference đến file cũ trong bất kỳ script nào.

---

## 10. Hướng dẫn setup Inspector

### Bước 1 — Prefab root

1. Mở Prefab Denizen trong Prefab Mode.
2. Trên root GameObject, **thêm** các component mới: `DenizenCore`, `DenizenMovement`, `DenizenCoinSharing` (nếu chưa có). **Giữ lại các Behavior cũ và DenizenRoleController** – chúng sẽ được disable sau khi chuyển xong.
3. (Tuỳ chọn) Để dễ test, bạn có thể **disable** `DenizenRoleController` và các Behavior cũ, chỉ enable các component mới.

### Bước 2 — Tạo child objects

Tạo 3 child GameObject trực tiếp dưới root:

| Tên child | Components cần thêm |
|---|---|
| `Jobs` | `VagrantJob`, `VillagerJob`, `ArcherJob` |
| `Detection` | `RayCastHelper`, `CircleDetectHelper` |
| `ArcherEquipment` | `ArcherShooting` |

**Lưu ý child Jobs:** Sau khi thêm 3 component, **chỉ để `VagrantJob` enabled = true**, tắt `VillagerJob` và `ArcherJob`.

### Bước 3 — Kéo references vào DenizenCore

Chọn root GameObject, tìm component `DenizenCore`, kéo:

- Field `_vagrantJob` ← drag component `VagrantJob` từ child `Jobs`.
- Field `_villagerJob` ← drag component `VillagerJob` từ child `Jobs`.
- Field `_archerJob` ← drag component `ArcherJob` từ child `Jobs`.
- Field `_context` ← drag component `DenizenContext` (cùng nằm trên root).

> **Mẹo setup nhanh:** Sau khi gán `DenizenContext`, hãy click chuột phải vào component này trên Inspector và chọn **"Auto Find References"**. Nó sẽ tự động tìm và điền tất cả các component con và script liên quan cho bạn.

### Bước 4 — Kiểm tra DenizenManager

Mở `DenizenManager` trong scene, đảm bảo các Animator Controller vẫn được assign đúng:

- `vagrantAnimatorController`
- `villagerAnimatorController`
- `archerAnimatorController`

Những field này vẫn được dùng bởi `DenizenCore.ApplyRole()` như cũ.

### Bước 5 — Verify trong Play Mode

1. Play scene, chọn một Denizen.
2. Kiểm tra: chỉ đúng 1 Job component enabled trong Inspector.
3. Thử Context Menu "Debug Village State" (vẫn giữ trên DenizenCore).
4. Thử đổi `CurrentRole` trên Inspector trong Play Mode → Job enable/disable đúng.

---

## 11. Các lưu ý quan trọng
 Bổ sung kiểm tra OnDestroy trong DenizenCore
Hiện tại DenizenRoleController có OnDestroy để unregister. Plan nên ghi rõ DenizenCore cũng phải override OnDestroy để:

Unsubscribe khỏi các manager (GuardManager, DenizenManager, NPCUpdateManager).

Dừng các coroutines nếu có (như RandomFlipRoutine trong VagrantJob hiện đang chạy ở OnEnable/OnDisable, nhưng cần dừng khi object bị destroy).

4. Thêm hướng dẫn chi tiết về DenizenTimeHandler cập nhật
Trong mục 12 (Compatibility Layer), liệt kê rõ:

DenizenTimeHandler sẽ thay _roleController bằng _core = GetComponent<DenizenCore>() và gọi _core.OnDayStart()...

Đảm bảo rằng các phương thức OnDayStart, OnNightStart, OnArcherHuntingTime được định nghĩa trong DenizenCore và delegate đến Job tương ứng.

5. Rủi ro khác: GuardManager gọi ForceRetreat trực tiếp
Trong Phase 4, plan có ArcherJob.ForceRetreat(GameObject defensiveWall). Tuy nhiên, GuardManager hiện tại có thể đang gọi GetComponent<ArcherBehavior>().ForceRetreat(). Cần cập nhật GuardManager để tìm ArcherJob (thông qua DenizenCore hoặc trực tiếp) để không bị break. Điều này đã nằm trong mục 12 nhưng nên nhấn mạnh: sau khi chuyển hoàn toàn sang Job mới, GuardManager sẽ phải lấy component ArcherJob thay vì ArcherBehavior.

Fallback AnimationEvents có thể gây double-route

Plan cho phép fallback gọi trực tiếp ArcherBehavior khi _core == null.
Nhưng khi migration chạy song song (Core/JOb mới đã bật một phần), cần đảm bảo không xảy ra trường hợp cả Core route lẫn fallback đều gọi (plan hiện chưa nêu rõ điều kiện chặn để tránh double).
Thứ tự reset khi đổi role vs đổi state

ResetAnimatorParams thuộc ChangeState, còn khi đổi role thì chủ yếu plan dựa vào Animator.Rebind() trước khi gán controller mới.
Khi implement, cần đảm bảo không có “thừa trigger/param” từ controller cũ làm ảnh hưởng controller mới (Enemy reset trigger names theo ChangeState; Denizen plan reset theo Rebind khi ApplyRole).
Migration dependencies (GuardManager/DenizenManager) phải chuyển đúng “nguồn sự thật”

Nếu GuardManager/DenizenManager vẫn còn tìm ArcherBehavior/DenizenRoleController trong giai đoạn migrate, logic đồng bộ sẽ bị lệch (đúng như mục 12 đã cảnh báo, nhưng đây là “điểm dễ sai” khi code).
Khung “movement chỉ do DenizenMovement ghi transform” phải được giữ nghiêm

Kế hoạch muốn Denizen giống Enemy ở chỗ frame update được quản lý theo tầng.
Khi implement, nếu còn movement code nằm lẫn trong Job/State, sẽ làm “đồng bộ tick/frame” kém giống Enemy.

### Về DenizenStatus — không được slim down trong refactor này

`DenizenStatus` hiện tại còn chứa một số runtime temp vars (`currentTarget`, `currentHuntingTargetPos` tương đương). Về lý thuyết nên chuyển sang State fields. **Tuy nhiên, không làm trong refactor này** vì ảnh hưởng save/load system. Chỉ thêm field mới vào State, giữ nguyên DenizenStatus, xử lý sau trong sprint riêng.

Denizen không sử dụng object pooling; chúng được spawn một lần và tồn tại suốt vòng đời (bất tử). Do đó không cần xử lý reset state khi object được tái sử dụng. Tuy nhiên, khi Denizen bị destroy (ví dụ do scene reload), OnDestroy vẫn phải đảm bảo unregister khỏi các manager và cleanup events.
ví dụ: khi archer bị tấn công => rơi tool => thành villager => bị tấn công => hết coin => thành vagrant . lúc này vagrant không thể bị tấn công (không cần logic nhận dame ở vagrant)

### Về State constructor và Job reference

Mỗi State cần reference đến Job của nó để gọi `ChangeState()`. Cách được khuyến nghị: **truyền Job vào constructor**.

```
// Ví dụ pattern:
// new VagrantIdleState(this)       ← "this" là VagrantJob
// state lưu: private VagrantJob _job
// state gọi: _job.ChangeState(_job.FearState)  ← FearState là public property của Job
```

Các States chỉ được phép gọi `_job.ChangeState()`. Không được gọi `_job.ChangeRole()` — đó là quyền của DenizenCore.

### Về `ResetAnimatorParams()`

Đây là method hay bị bỏ quên nhất. Khi Archer đang ở "archerDraw" rồi đột ngột đổi sang VillagerJob, nếu không reset Animator params thì VillagerJob sẽ kế thừa Animator state cũ. `ResetAnimatorParams()` trong mỗi Job phải set tất cả bool params về false và clear tất cả triggers của **đúng Animator Controller** của Job đó.

### Về thread safety của NPCUpdateManager

`UpdateBehavior()` được gọi từ NPCUpdateManager với frequency thấp. `FrameUpdate()` được gọi từ MonoBehaviour.Update() mỗi frame. Hai loop này **không chạy đồng thời** vì Unity single-threaded, nhưng cần đảm bảo State không bị ChangeState giữa chừng trong cùng một frame — sử dụng `if (next == CurrentState) return` guard trong `ChangeState()`.

### Về ISaveable

`DenizenCore` implement `ISaveable` và delegate sang `DenizenStatus.CaptureData()` / `RestoreData()`. Logic này giữ nguyên hoàn toàn. Sau `RestoreData()`, DenizenCore gọi `ChangeRole(savedRole)` như cũ — lúc này Job tương ứng sẽ tự vào đúng state dựa trên data đã restore trong DenizenStatus.

---

## 12. Compatibility Layer – Các script ngoài cần cập nhật

Khi xóa `DenizenRoleController` và các Behavior cũ, những script sau cần được sửa để tham chiếu đến `DenizenCore` thay thế:

- **`DenizenTimeHandler.cs`** – hiện tại gọi `_roleController.OnDayStart()`… Cần đổi thành gọi `GetComponent<DenizenCore>()?.OnDayStart()` (hoặc lưu reference).
- **`DenizenManager.cs`** – nếu có tham chiếu đến `DenizenRoleController` để quản lý danh sách, cần đổi sang `DenizenCore`.
- **`GuardManager.cs`** – khi gọi `ForceRetreat` hoặc `OnGuardManagerStatusChanged`, hiện tại có thể đang gọi vào `ArcherBehavior`. Cần chuyển sang `ArcherJob` thông qua `DenizenCore` hoặc trực tiếp qua `GetComponent<ArcherJob>()`.
- **`NpcUtility.cs`** – nếu dùng để spawn denizen, cần đảm bảo prefab mới đã có `DenizenCore` thay vì `DenizenRoleController`.
- **Bất kỳ script nào khác** gọi `GetComponent<DenizenRoleController>()` hoặc `GetComponent<VagrantBehavior>()`… cần được kiểm tra và thay thế bằng `DenizenCore`.

**Thời điểm cập nhật:** Các script này nên được sửa dần trong **Phase 4–5**, sau khi các Job mới đã hoạt động ổn định trên prefab và trước khi xóa file cũ.

---

## Tóm tắt lộ trình

1. **Phase 1** – Tạo infrastructure (Core, Context, State, Job, Movement).  
2. **Phase 2** – Implement VagrantJob và các state, test song song với VagrantBehavior cũ.  
3. **Phase 3** – Implement VillagerJob, test song song với VillagerBehavior cũ.  
4. **Phase 4** – Implement ArcherJob, test song song với ArcherBehavior cũ, đồng thời cập nhật các script ngoài.  
5. **Phase 5** – Xóa các file cũ, chạy full test, kết thúc refactor.

---

**Ghi chú cuối:** Plan này có thể điều chỉnh trong quá trình thực hiện nếu phát sinh vấn đề chưa lường trước. AI code agent nếu được yêu cầu viết code sẽ luôn đọc lại toàn bộ plan hiện tại trước khi bắt đầu.