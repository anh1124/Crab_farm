# Báo cáo Hệ thống State Machine của Denizen (Cấu trúc Cơ sở)

Hệ thống Denizen dùng Modular State Machine. Đổi Job (nghề) mà vẫn giữ physics, `DenizenStatus` và prefab gốc. Job **đã gắn sẵn** trên prefab; đổi nghề chỉ enable/disable, không `AddComponent`/`Destroy` Behavior.

Hướng dẫn prefab và luồng tool: [JobSystemGuide.md](../JobSystemGuide.md).

## 1. Thành phần nền tảng

### DenizenRoleController

Driver đổi role, swap Animator, bridge save/load (`ISaveable`). Nhận injection manager qua `Setup(...)` rồi `DenizenContext.Setup` + `InjectContext` xuống từng Job.

`UpdateBehavior` của AI **không** chạy trên Controller: từng Job đăng ký `NPCUpdateManager`.

### DenizenStatus

Kho dữ liệu vòng đời: Role, coin, `currentTool`, `currentTarget`, `coinTarget`, `isSkillCasting`, `skillAllowsMovement`, `isFearPending`, speed, enum state Archer/Villager.

### DenizenContext

Cache để tránh `GetComponent` trong pulse AI.

- Cục bộ: Animator, SpriteRenderer, Movement, Status, SkillManager, RayCast / CircleDetect, ba Job, ArcherShooting, CoinSharing.
- Toàn cục: `IGuardProvider`, `IGameTimeProvider`, `WallAndStumpManager`, `NPCUpdateManager`; ArrowPool inject vào `ArcherShooting`.
- Implement `IItemHandle`: forward `HandleItem` sang Job đang `enabled`.

`CurrentJob` = Job child nào đang enabled (Vagrant / Villager / Archer).

---

## 2. Luồng vận hành

### Khởi tạo

1. **Save:** `RestoreData` ghi `_pendingRoleFromSave`; `Setup()` mới `ApplyRole`.
2. **Spawn mới:** mặc định `NPCRole.Vagrant`. `Start()` tự `DenizenManager.InitializeNewDenizen` nếu chưa được Setup (skill/debug spawn).

Role được hỗ trợ: Vagrant, Villager, Archer. Worker / Farmer → `IsRoleSupported == false`.

### ChangeRole

1. Tắt `isSkillCasting`, `Movement.Stop()`, xóa target/coinTarget.
2. Disable Job cũ, gán tag, enable Job mới, swap Animator Controller + `Rebind` nếu controller đổi.
3. Job `OnEnable`: reset detector, schedule, đăng ký manager (`VagrantManager`, `GuardManager.RegisterArcher`, `NPCUpdateManager`).

Không destroy Job cũ — chỉ `enabled = false`.

---

## 3. Di chuyển và diễn họa

### DenizenMovement

Bám `_moveTargetPos`. Chỉ move khi `Status.isMoving` **và** `CanMove` **và** (`!isSkillCasting` **hoặc** `skillAllowsMovement`).

Tốc độ: đối số `SetTarget(pos, speed)` hoặc fallback `walkSpeed`. Job gán `walkSpeed` / `runSpeed` / `huntingMoveSpeed` tùy state.

### Animation

Đổi state trong `DenizenJob` gọi `ResetAllAnimatorParams()` rồi state tự `Play` / trigger.

---

## 4. Tài liệu Job

- [Archer](Denizen_Archer_Job.md)
- [Villager](Denizen_Villager_Job.md)
- [Vagrant](Denizen_Vagrant_Job.md)
