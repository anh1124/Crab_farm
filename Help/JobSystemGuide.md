# Hướng Dẫn Hệ Thống Nghề Nghiệp (Job System)

Tài liệu này mô tả cơ chế chuyển nghề **đang chạy trong code**. Chi tiết state machine từng nghề nằm ở `Help/object/`.

Kiến trúc cũ (`DenizenController`, `*Behavior`, `AddComponent` lúc đổi nghề) **không còn dùng**. File legacy nằm ở `Help/*-legacy.md` và `Assets/Script/Help/denizenlegacy/`.

---

## 1. Cấu trúc Prefab Denizen

Một prefab Denizen dùng chung cho Vagrant, Villager và Archer. Role không tạo script mới lúc runtime: **Job đã nằm sẵn trên prefab** (thường là child), `DenizenRoleController` chỉ `enabled = true/false`.

### Component bắt buộc (root)

Theo `RequireComponent` trên `DenizenRoleController`:

| Component | Vai trò |
|-----------|---------|
| `DenizenRoleController` | Đổi role, swap Animator, save/load |
| `DenizenStatus` | Dữ liệu dùng chung (coin, tool, flags) |
| `DenizenContext` | Cache tham chiếu + bridge `IItemHandle` |
| `DenizenMovement` | Di chuyển kinematic theo `_moveTargetPos` |
| `DenizenCoinSharing` | Chia/rơi coin |
| `DenizenAnimationEvents` | Nhận Animation Event |
| `Animator` / `SpriteRenderer` | Diễn họa (có thể nằm child Skin) |
| `SkillManager` | Kỹ năng (Archer bắn cung) |
| `DamageFlashEffect` | Flash khi nhận damage |
| `AudioEmitter` | SFX theo entity |

Thêm trên prefab (không Require nhưng cần cho AI):

- `Rigidbody2D`, `Collider2D`
- Child **Detection**: `RayCastHelper`, `CircleDetectHelper`
- Child **PickupArea**: `LootPickupTrigger` + Collider IsTrigger
- Child Jobs: `VagrantJob`, `VillagerJob`, `ArcherJob` (gắn Animator Controller trên từng Job hoặc lấy từ `DenizenManager`)
- `ArcherShooting` (child, dùng khi là Archer)

**Phải add sẵn trên prefab.** Code **không** `AddComponent` Behavior/Status khi đổi nghề.

**Chưa có Job:** `NPCRole.Worker` và `NPCRole.Farmer`. `IsRoleSupported` trả `false` — gọi `ChangeRole` sang hai role này sẽ bị từ chối, giữ role cũ.

---

## 2. Cơ chế chuyển nghề

Logic trung tâm: `DenizenRoleController.cs`.

### `CurrentRole`

Mặc định `NPCRole.Vagrant`. Save/load ghi vào `DenizenStatus.Role` rồi `Setup()` gọi `ApplyRole`.

### `ChangeRole(NPCRole newRole)`

1. Bỏ qua nếu role trùng **và** đã có Job đang chạy.
2. Từ chối nếu role chưa có Job trên `DenizenContext`.
3. Tắt `isSkillCasting`, `Movement.Stop()`, xóa `currentTarget` / `coinTarget`.
4. Disable Job cũ → `ApplyRole(newRole)`.

### `ApplyRole(NPCRole role)`

1. `Animator.Rebind()`, disable mọi Job.
2. Gán tag (`Vagrant` / `Villager` / `Archer`).
3. Chọn Job tương ứng, bật `enabled = true` (kích hoạt `OnEnable` của Job: sensor, schedule, đăng ký manager).
4. Swap `runtimeAnimatorController` từ `Job.JobController` hoặc fallback `DenizenManager` (`vagrant/villager/archerAnimatorController`).
5. Đồng bộ `CurrentRole` + `Status.Role`, bắn `OnRoleChanged`.

Coin **không** transfer riêng: cùng một `DenizenStatus` xuyên suốt vòng đời.

---

## 3. Quy trình chuyển nghề cụ thể

### A. Vagrant → Villager (nhặt coin)

1. `LootPickupTrigger` chạm `PhysicalItem` loại `Coin`.
2. `DenizenContext.HandleItem` → `VagrantJob` → `VagrantItemHandler`.
3. Handler `AddCoin(1)` rồi `ChangeRole(NPCRole.Villager)` **ngay trong cùng lần gọi** (không đợi 1 frame).
4. Khi đang Fear: không nhặt coin thường; vẫn nhận coin do player ném (`playerPickupAble == false`).

### B. Villager → Archer (và Worker/Farmer — chưa live)

1. Tool trên đất là `PhysicalItem` (`ItemType.Tool_Bow` / `Tool_Hammer` / `Tool_Scythe`), tự `RegisterTool` với `ToolManager`.
2. `ToolManager` tìm Villager idle gần nhất qua `DenizenManager.GetNearestIdleVillager`, gọi `VillagerJob.AssignTool`.
3. Villager vào `VillagerRunToolState`: **chạy tới vị trí tool trên đất** (không làm việc tại công trình).
4. Khi trigger pickup, `VillagerItemHandler`:
   - `Tool_Bow` → `ChangeRole(Archer)` — **hoạt động**.
   - `Tool_Hammer` → `ChangeRole(Worker)` — **bị chặn** (chưa có Job).
   - `Tool_Scythe` → `ChangeRole(Farmer)` — **bị chặn** (chưa có Job).

### C. Giáng cấp khi mất coin

- Villager hết coin sau hit → `ChangeRole(Vagrant)`.
- Archer còn > 1 coin: trừ 1 và drop coin. Còn 1: drop bow, `isFearPending`, `ChangeRole(Villager)`.

---

## 4. Hệ thống Tool (`PhysicalItem` + `ToolManager`)

Không còn `ToolItem.cs` / enum `ToolType`.

| `ItemType` | Role đích | Trạng thái |
|------------|-----------|------------|
| `Tool_Bow` | Archer | Live |
| `Tool_Hammer` | Worker | Handler gọi ChangeRole, Job chưa có |
| `Tool_Scythe` | Farmer | Handler gọi ChangeRole, Job chưa có |

`ToolManager`:

- `RegisterTool` / `UnregisterTool`
- Gán `AssignedCarrier` để tránh tranh tool
- `OnUnexpectedPickup`, `OnToolAbandoned`, `OnVillagerBecameIdle` để tái phân bổ tool mồ côi

---

## 5. Tài liệu chi tiết

- [Denizen Base](object/Denizen_Base_System.md)
- [Villager](object/Denizen_Villager_Job.md)
- [Archer](object/Denizen_Archer_Job.md)
- [Vagrant](object/Denizen_Vagrant_Job.md)
- [Loot Pickup](LootPickupSystem.md)
