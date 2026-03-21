# Crab Farm — Hướng dẫn Fix Performance
> Dựa trên kết quả Audit 1–8 | 14 lỗi | Ước tính phục hồi: +45~80 FPS ở 50 NPC

---

## PHẦN A — FIX TRONG UNITY EDITOR
> Không cần sửa code. Mở prefab/scene và thay đổi trực tiếp trên Inspector.

---

### [EDITOR-1] 🔴 HIGH — Đổi Animator Culling Mode trên 6 NPC Prefab

**Vấn đề:** Tất cả Animator đang dùng `AlwaysAnimate (0)` — Unity chạy toàn bộ animation state machine kể cả khi NPC nằm ngoài màn hình.

**Các prefab cần sửa:**
- `Denizen.prefab`
- `Archer.prefab`
- `SpearMan.prefab`
- `Guard.prefab`
- `shieldBot.prefab`
- `circleHead.prefab`

**Cách sửa:**
1. Mở từng prefab trong Unity
2. Chọn GameObject gốc (root)
3. Trong component `Animator` → tìm field **Culling Mode**
4. Đổi sang giá trị tương ứng:

| Prefab                                         |                                    Culling Mode nên dùng            | Lý do|
| Archer, SpearMan, Guard, shieldBot, circleHead | `Cull Update Transforms`                                            | NPC quan trọng, cần giữ state logic|
| Denizen                                        | `Cull Completely`                                                   | NPC nền, ít quan trọng, tắt hẳn khi off-screen|

> `Cull Update Transforms` = dừng cập nhật xương/transform khi off-screen nhưng vẫn giữ state machine logic.
> `Cull Completely` = dừng hoàn toàn mọi thứ khi off-screen.

**Ước tính FPS phục hồi: +20~35 FPS**

---

### [EDITOR-2] 🟡 MEDIUM — Kiểm tra Layer Mask trên BuildingPlacementManager

**Vấn đề:** Một số `Physics2D.RaycastAll` trong `BuildingPlacementManager.cs` đang dùng layermask `~0` (scan tất cả layer) thay vì chỉ scan layer cần thiết.

**Cách sửa:**
1. Tìm GameObject `BuildingPlacementManager` trong scene Hierarchy
2. Trong Inspector → xem các field LayerMask được gán
3. Đảm bảo chỉ tick các layer thực sự cần thiết cho placement (Ground, Building, v.v.)
4. **Bỏ tick** tất cả layer không liên quan (NPC, Projectile, UI, v.v.)

**Ước tính FPS phục hồi:** Nhỏ khi không dùng building, nhưng loại bỏ spike khi mở build menu.

---

## PHẦN B — FIX TRONG CODE
> Các lỗi yêu cầu chỉnh sửa file `.cs`. Sắp xếp theo mức độ ưu tiên.

---

### [CODE-1] 🔴 HIGH — Cache `List<GameObject>` và `ContactFilter2D` trong Helper classes
**File:** `RayCastHelper.cs` (line 66, 82) | `CircleDetectHelper.cs` (line 59, 72)

**Vấn đề:** Mỗi tick mỗi NPC tạo ra `new List<GameObject>` và `new ContactFilter2D()` hoàn toàn mới. Với 50 NPC × 5Hz = **1000+ heap allocations/giây** → GC chạy liên tục → frame hitch đột ngột.

**Fix `CircleDetectHelper.cs`:**
```csharp
// TRƯỚC:
public List<GameObject> ScanCircle(Vector2 origin, float radius, LayerMask mask)
{
    var filter = new ContactFilter2D();           // ❌ allocation mỗi lần gọi
    filter.SetLayerMask(mask);
    // ...
    return new List<GameObject>(detectedObjects); // ❌ allocation mỗi lần gọi
}

// SAU:
private ContactFilter2D _filter;               // ✅ cache 1 lần
private readonly List<GameObject> _resultCache = new List<GameObject>(); // ✅ tái sử dụng

private void Awake()
{
    _filter = new ContactFilter2D();
    // LayerMask có thể set ở đây nếu cố định,
    // hoặc set trong ScanCircle nếu thay đổi theo lần gọi
}

public List<GameObject> ScanCircle(Vector2 origin, float radius, LayerMask mask)
{
    _filter.SetLayerMask(mask);  // chỉ update mask, không tạo object mới
    _resultCache.Clear();        // ✅ clear, không tạo list mới
    // fill _resultCache thay vì detectedObjects...
    return _resultCache;
}
```

**Fix `RayCastHelper.cs` — tương tự:**
```csharp
private ContactFilter2D _filter;
private readonly List<GameObject> _resultCache = new List<GameObject>();

private void Awake()
{
    _filter = new ContactFilter2D();
}

public List<GameObject> DrawRay(...)
{
    _filter.SetLayerMask(targetMask);
    _resultCache.Clear();
    // fill _resultCache...
    return _resultCache;
}
```

> ⚠️ **Lưu ý quan trọng:** Sau khi sửa, caller không được giữ reference đến list trả về qua nhiều frame vì nó sẽ bị clear ở lần gọi tiếp theo. Nếu cần giữ kết quả, caller tự copy: `var copy = new List<GameObject>(result);`

**Ước tính FPS phục hồi: +10~20 FPS + loại bỏ frame hitch đột ngột**

---

### [CODE-3] 🔴 HIGH — Bỏ gọi `animator.Play()` thừa trong ArcherBehavior
**File:** `ArcherBehavior.cs` (line 539, 607, 615, 642, 669)

**Vấn đề:** `animator.Play()` được gọi tại 5 chỗ bên trong một tick duy nhất. Gọi `Play()` khi animation đang chạy sẽ reset về frame 0 và force Unity re-evaluate state machine — lãng phí ngay cả khi state không đổi.

**Fix — thêm helper method để cache state:**
```csharp
// Thêm vào ArcherBehavior.cs (và VagrantBehavior.cs tương tự):
private string _currentAnimState = "";

private void SetAnimState(string newState)
{
    if (_currentAnimState == newState) return; // ✅ skip nếu không đổi
    _currentAnimState = newState;
    _animator.Play(newState);
}

// Thay tất cả animator.Play("XYZ") thành SetAnimState("XYZ")
// Ví dụ:
// TRƯỚC: animator.Play("Attack");  (line 539)
// SAU:   SetAnimState("Attack");
```

**Ước tính FPS phục hồi: +5~10 FPS**

---



### [CODE-5] 🟡 MEDIUM — Dùng CoinPool thay `Instantiate()` trực tiếp
**File:** `VillagerBehavior.cs` (line 715) | `ArcherBehavior.cs` (line 998)

**Vấn đề:** Hai script này bypass `CoinPool` và gọi `Instantiate()` trực tiếp — gây heap fragmentation và frame hitch mỗi lần drop coin.

**Fix:**
```csharp
// TRƯỚC (VillagerBehavior.cs line 715 và ArcherBehavior.cs line 998):
Instantiate(Spawner.Instance.coinPrefab, transform.position, Quaternion.identity);

// SAU — dùng pool đã có sẵn:
Spawner.Instance.DropByDenizen(transform.position);
// Hoặc nếu API pool là:
// CoinPool.Instance.Get(transform.position);
```

**Ước tính FPS phục hồi: +3~5 FPS + loại bỏ micro-hitch khi drop coin**

---

### [CODE-6] 🟡 MEDIUM — Fix `BuildingPlacementManager` — ContactFilter2D và RaycastAll
**File:** `BuildingPlacementManager.cs` (line 266, 286, 349, 401, 477)

**Vấn đề:** `RaycastAll` được gọi **mỗi frame** trong `Update()` khi player đang đặt building, kèm `new ContactFilter2D()` được tạo mới mỗi frame. Đây là bottleneck nặng nhất khi build mode đang active.

**Fix:**
```csharp
// Thêm cache ở đầu class:
private ContactFilter2D _placementFilter;
private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[20]; // pre-allocated buffer

private void Awake()
{
    _placementFilter = new ContactFilter2D();
    _placementFilter.SetLayerMask(placementLayerMask); // dùng layer mask cụ thể
    _placementFilter.useTriggers = false;
}

// Trong Update() — thay RaycastAll:
// TRƯỚC:
var hits = Physics2D.RaycastAll(origin, direction);

// SAU (NonAlloc — không tạo array mới):
int hitCount = Physics2D.Raycast(origin, direction, _placementFilter, _hitBuffer);
for (int i = 0; i < hitCount; i++)
{
    // xử lý _hitBuffer[i]
}
```

**Ước tính FPS phục hồi:** Không ảnh hưởng khi không build, nhưng loại bỏ lag khi mở build menu.

---

### [CODE-7] 🟡 MEDIUM — `PhysicalItem.cs` — Raycast trong Update và Coroutine per item
**File:** `PhysicalItem.cs` (line 195)

**Vấn đề:** Mỗi PhysicalItem (coin, tool đang rơi) chạy `Raycast` trong `Update()` và có một Coroutine `Cooldown` riêng. Khi nhiều coin rơi cùng lúc → nhiều Raycast/frame + nhiều coroutine song song.

**Fix — gộp Raycast vào bucket hoặc giảm tần suất:**
```csharp
// TRƯỚC: Raycast trong Update() — chạy mỗi frame
void Update()
{
    // ... falling logic với Raycast ...
}

// SAU: dùng timer để giảm tần suất
private float _checkTimer = 0f;
private const float CHECK_INTERVAL = 0.05f; // check 20 lần/giây thay vì 60

void Update()
{
    _checkTimer += Time.deltaTime;
    if (_checkTimer < CHECK_INTERVAL) return;
    _checkTimer = 0f;

    // ... falling logic với Raycast ...
}
```

**Fix Coroutine — thay bằng timer đơn giản:**
```csharp
// TRƯỚC:
StartCoroutine(Cooldown()); // mỗi item spawn = 1 coroutine mới

// SAU:
private float _pickupCooldown = 0f;

void Update()
{
    if (_pickupCooldown > 0f)
        _pickupCooldown -= Time.deltaTime;
}

bool CanPickup() => _pickupCooldown <= 0f;

void OnSpawn()
{
    _pickupCooldown = cooldownDuration; // không cần coroutine
}
```
còn 
---

### [CODE-8] 🟡 MEDIUM — `ToolManager.cs` — RemoveAll trong tick của Villager
**File:** `ToolManager.cs` (line 147)

**Vấn đề:** `availableTools.RemoveAll(...)` được gọi mỗi 2s trong tick của mỗi Villager. `RemoveAll` duyệt toàn bộ list và có thể gây boxing nếu dùng lambda.

**Fix:**
```csharp
// TRƯỚC:
availableTools.RemoveAll(t => t == null || !t.IsAvailable());

// SAU — tránh lambda allocation, dùng for loop ngược:
for (int i = availableTools.Count - 1; i >= 0; i--)
{
    if (availableTools[i] == null || !availableTools[i].IsAvailable())
        availableTools.RemoveAt(i);
}
```

---

### [CODE-9] 🟢 LOW — `PlayerItemHandler.cs` — string allocation trên pickup
**File:** `PlayerItemHandler.cs` (line 25)

**Vấn đề:** `item.name.ToLower()` tạo một string object mới mỗi lần có trigger pickup.

**Fix:**
```csharp
// TRƯỚC:
if (item.name.ToLower() == "coin")

// SAU — không allocation:
if (item.CompareTag("Coin"))
// Hoặc nếu dùng enum/type:
if (item.GetComponent<CoinItem>() != null)
```

---

### [CODE-10] 🟢 LOW — Denizen thiếu pool khi spawn Bow
**File:** `DenizenRoleController.cs` (line 187)

**Vấn đề:** Khi Archer transition thành Vagrant/Villager, Bow được `Instantiate()` trực tiếp không qua pool.

**Fix ngắn hạn:**
```csharp
// TRƯỚC:
var bow = Instantiate(bowPrefab, transform.position, Quaternion.identity);

// SAU — nếu chưa có BowPool, ít nhất cache lại và tái sử dụng:
// Tạo BowPool nhỏ tương tự ArrowPool.cs đã có sẵn trong project
// Hoặc spawn tại scene load và disable, enable lại khi cần
```

---

## TỔNG KẾT

### Thứ tự thực hiện được khuyến nghị

```
Bước 1 — EDITOR-1 (5 phút)     → Đổi cullingMode 6 prefab → Test FPS ngay
Bước 2 — CODE-2   (15 phút)    → Distance gate trong NPCUpdateManager
Bước 3 — CODE-1   (30 phút)    → Cache List + ContactFilter2D trong Helper classes
Bước 4 — CODE-3   (15 phút)    → Cache animator state trong ArcherBehavior
Bước 5 — CODE-4   (10 phút)    → Fix DropCoinsCoroutine flag
Bước 6 — CODE-5   (10 phút)    → Dùng CoinPool thay Instantiate
Bước 7 — CODE-6   (20 phút)    → Fix BuildingPlacementManager
Bước 8 — CODE-7   (20 phút)    → Fix PhysicalItem Update + Coroutine
Bước 9 — CODE-8,9,10           → Các fix nhỏ còn lại
```

### Ước tính FPS phục hồi

| Nhóm fix | FPS dự kiến |
|----------|-------------|
| Bước 1 (Editor only) | +20~35 FPS |
| Bước 1 + 2 + 3 | +45~60 FPS |
| Tất cả HIGH issues | +50~70 FPS |
| Toàn bộ 10 lỗi | +55~80 FPS |

**Mục tiêu thực tế sau fix:** 120~150 FPS ổn định ở 50 NPC (từ mức hiện tại ~60 FPS).

---
