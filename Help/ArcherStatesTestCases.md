# Test Cases cho Archer State Machine & Wall-Guard Manager Integration
================================================================================

## Mục tiêu
Đảm bảo Archer chuyển đổi trạng thái chính xác theo:
1. **Thời gian** (Ngày/Đêm)
2. **Mối đe dọa** (Enemy attack)
3. **Tương tác WallAndStumpManager ↔ GuardManager** (Event-driven)

## Hướng dẫn Test
- Sử dụng **GameTimeManager** trong Inspector để chỉnh giờ (Sáng: 7h, Tối: 19h)
- Sử dụng **GuardManager/ArcherStateMachineTest** để giả lập enemy
- Quan sát **State** của Archer trong Inspector (`ArcherStatus → currentState`)
- Kiểm tra **Console logs** để verify event flow
- Theo dõi **assignedWall** trong ArcherStatus để kiểm tra wall assignment

---

## PART 1: Archer State Machine (Core Behavior)

### Case 1: Hòa bình (Ban ngày)
#### [ ] 1.1 Khởi đầu ngày mới
- **Setup:** Set time = Morning (7h). Clear alarm (`GuardManager.detectedEnemies.Count = 0`)
- **Expected State:** `Hunting`
- **Behavior:**
  - Archer đi ra khỏi tường về phía rừng
  - Di chuyển random trong `archerHuntingRange` (lấy từ `PrefabData.archerHuntingRange`)
  - **Debug Check:** Blue raycast line hướng ra ngoài (outward from defensive wall)

#### [ ] 1.2 Đang đi săn gặp địch
- **Setup:** Spawn enemy trong tầm Raycast của Archer đang Hunting
- **Expected Flow:**
  1. Archer detect enemy qua RayCastHelper (tag "Enemy")
  2. Gọi `GuardManager.RegisterEnemy(enemyGameObject)`
  3. State chuyển `ReturningToBase`
- **Behavior:**
  - Archer chạy về `assignedWall` (currentLeftWall hoặc currentRightWall)
  - GuardManager bật `isEnemyAttack = true`
  - **Tất cả Archer khác** cũng chuyển về `ReturningToBase`

---

### Case 2: Phòng thủ (Báo động)
#### [ ] 2.1 Báo động kích hoạt
- **Setup:** `GuardManager.isEnemyAttack = true` (qua RegisterEnemy)
- **Expected State:** Tất cả Archer → `ReturningToBase`
- **Behavior:**
  - Chạy nhanh về `assignedWall`
  - Khi đến tường → chuyển `HoldingWall`
  - **Guard Visualization:** Hunting range line (blue) vẫn hiển thị

#### [ ] 2.2 Chiến đấu tại tường
- **Setup:** Archer ở `HoldingWall`, enemy trong `attackRange`
- **Expected:**
  - Animation: `archerDraw` → `archerRelease`
  - Quay mặt về địch (flip sprite nếu cần)
- **Behavior:**
  - Bắn tên (qua animation event `OnDrawComplete`)
  - **Không di chuyển** khỏi vị trí thủ

#### [ ] 2.3 Hết báo động (Ban ngày) - AlertCooldownRoutine
- **Setup:**
  1. Xóa hết Enemy (destroy hoặc trigger `EnemyBehavior.OnDespawn`)
  2. Chờ 30s (`ALARM_COOLDOWN_DURATION`)
  3. Time = Daytime
- **Expected Flow:**
  1. `GuardManager.HandleEnemyDespawn()` → `detectedEnemies.Count = 0`
  2. `AlertCooldownRoutine` starts
  3. After 30s → `ClearAlarm()` → `UpdateDefensiveWalls()`
  4. Archer state: `HoldingWall` → `Hunting`
- **Behavior:**
  - Archer rời vị trí thủ, đi ra ngoài săn tiếp
  - **Console log:** "✅ Alarm cleared - Returning to peacetime duties"

---

### Case 3: Ban đêm (Night Time)
#### [ ] 3.1 Trời tối (19h) - Không có enemy
- **Setup:** Set time = Night (19h), `isEnemyAttack = false`
- **Expected State:**
  - Nếu đang Hunting → `ReturningToBase`
  - Nếu đã ở trong → `HoldingWall`
- **Behavior:**
  - **KHÔNG được đi Hunting**
  - Đứng canh tại tường suốt đêm
  - Check: `GameTimeManager.Instance.IsDaytime = false` trong Update loop

#### [ ] 3.2 Hết báo động (Ban đêm)
- **Setup:**
  1. Enemy attack ban đêm
  2. Diệt hết địch
  3. Chờ 30s cooldown
- **Expected:**
  - `ClearAlarm()` vẫn chạy
  - Archer check `IsDaytime = false` → Vẫn ở `HoldingWall`
  - **KHÔNG chuyển** sang `Hunting`
- **Behavior:**
  - "Peaceful Night Guard" mode (không combat nhưng vẫn canh)

---

### Case 4: Rút lui khi tường nguy hiểm
#### [ ] 4.1 Tường Critical HP (Event-Driven)
- **Setup:**
  1. Giảm HP của `currentLeftWall` xuống dưới `criticalHPThreshold` (default: 10)
  2. WallBehavior.TakeDamage() triggers
- **Expected Flow:**
  1. `WallBehavior.CheckAndFireCriticalEvent()`
  2. Event `OnCriticalHP` bắn đến `GuardManager.HandleWallCritical()`
  3. `GuardManager.NotifyWallCritical()` gọi
  4. Archer nhận lệnh retreat
- **Expected State:** `RetreatingToInnerWall`
- **Behavior:**
  - Chạy về bức tường phía sau (lấy từ `WallAndStumpManager.GetDefendableWallBehind()`)
  - Nếu **không còn tường phía sau** → `HoldingWall` (Last Stand mode)

#### [ ] 4.2 Không có tường phòng thủ
- **Setup:**
  1. Tất cả walls bị phá (hoặc HP < critical)
  2. `defendableLeftWalls.Count = 0`
- **Expected:**
  - `GetFarthestDefendableWall()` returns `null`
  - Archer vẫn ở trạng thái hiện tại (không crash)
  - **Console warning:** Log về việc không tìm thấy wall

---

## PART 2: Wall & Guard Manager Integration Tests (Event-Driven Logic)

### Test Group A: Wall Registration & Update
#### [ ] A.1 Wall mới được xây (Peacetime)
- **Setup:**
  1. `GuardManager.detectedEnemies.Count = 0`
  2. Xây Wall mới xa hơn wall hiện tại
- **Expected Flow:**
  1. `WallAndStumpManager.RegisterWall()` → `UpdateDefendableWalls()`
  2. Check `detectedEnemies.Count == 0` → TRUE
  3. `GuardManager.UpdateDefensiveWalls()` được gọi
  4. `currentLeftWall/currentRightWall` update sang wall mới
- **Archer Behavior:**
  - Nếu đang `HoldingWall` hoặc `ReturningToBase` → `assignedWall` update
  - Di chuyển đến wall mới

#### [ ] A.2 Wall mới được xây (During Combat)
- **Setup:**
  1. `GuardManager.detectedEnemies.Count > 0` (đang combat)
  2. Xây Wall mới xa hơn
- **Expected Flow:**
  1. `RegisterWall()` → `UpdateDefendableWalls()`
  2. Check `detectedEnemies.Count == 0` → **FALSE**
  3. `GuardManager.UpdateDefensiveWalls()` **KHÔNG được gọi**
  4. Archer **KHÔNG** đổi wall assignment (tránh bỏ vị trí đang thủ giữa combat)
- **Behavior:**
  - Archer tiếp tục thủ wall cũ
  - Sau khi `ClearAlarm()` mới update wall mới

---

### Test Group B: Wall HP Events
#### [ ] B.1 Wall Critical HP → GuardManager Event
- **Setup:**
  1. Wall được assign vào `GuardManager.currentLeftWall`
  2. Giảm HP xuống < `criticalHPThreshold`
- **Expected Flow:**
  1. `WallBehavior.TakeDamage()` → `CheckAndFireCriticalEvent()`
  2. Event `OnCriticalHP` → `GuardManager.HandleWallCritical(wall)`
  3. `NotifyWallCritical()` được gọi
- **Console Check:**
  - "⚠️ Wall Critical Event: [wall.name] (LEFT/RIGHT)"
- **Guard NOT Called:**
  - `WallAndStumpManager.CheckWallCriticalHP()` chỉ log, **KHÔNG gọi** `NotifyGuardManagerWallCritical` (tránh duplicate)

#### [ ] B.2 Wall Repaired → Full HP
- **Setup:**
  1. Wall damaged (HP < maxHP)
  2. Heal wall đến `currentHp >= maxHp`
- **Expected Flow:**
  1. `WallBehavior.Heal()` → check `currentHp >= maxHp` → TRUE
  2. `OnWallRepaired(gameObject)` được gọi
  3. `UpdateDefendableWalls()` update list
  4. Nếu không combat → `GuardManager.UpdateDefensiveWalls()`
- **NOT Called on Partial Heal:**
  - Nếu `currentHp < maxHp` → **KHÔNG gọi** `OnWallRepaired` (tránh spam)

#### [ ] B.3 Stump Repair → Wall Resurrection
- **Setup:**
  1. Wall state = Stump
  2. Heal đủ HP
- **Expected Flow:**
  1. `RepairStump()` được gọi
  2. `OnWallRepaired` event invoke
  3. `UpdateDefendableWalls()`
  4. Hàm `Heal()` **return** ngay sau `RepairStump()` (tránh duplicate calls)
- **Behavior:**
  - Stump → Wall (state change)
  - ChildBlocker active lại (physics collision)

---

### Test Group C: Event Subscription/Cleanup
#### [ ] C.1 Wall Subscription khi assign
- **Setup:**
  1. `GuardManager.UpdateDefensiveWalls()` được gọi
  2. Có wall mới assign vào `currentLeftWall`
- **Expected:**
  1. Unsubscribe wall cũ: `oldBehavior.OnCriticalHP -= HandleWallCritical`
  2. Subscribe wall mới: `newBehavior.OnCriticalHP += HandleWallCritical`
- **Memory Check:**
  - Không có memory leak (event đã unsubscribe)

#### [ ] C.2 GuardManager OnDestroy cleanup
- **Setup:** Destroy GuardManager GameObject
- **Expected:**
  1. Unsubscribe tất cả wall events
  2. Clear lists
- **No Errors:**
  - Không có null reference hoặc callback ma

---

### Test Group D: UpdateDefendableWalls Call Frequency
#### [ ] D.1 Heal không spam update
- **Setup:** Heal wall từ HP=50 → HP=100 (mỗi lần +5)
- **Expected:**
  - `UpdateDefendableWalls()` chỉ gọi **1 lần** khi HP đầy
  - **KHÔNG** gọi mỗi lần heal (+5, +5, +5...)
- **Console Check:**
  - Đếm số lần log "Updated defendable walls"

#### [ ] D.2 Critical Flag Reset
- **Setup:**
  1. Wall critical (HP < 10)
  2. Heal lên HP >= threshold (ví dụ: 15)
- **Expected:**
  1. `ResetCriticalFlag()` được gọi
  2. `hasFiredCriticalEvent = false`
  3. Nếu HP lại xuống critical → Event bắn lại được

---

## PART 3: Edge Cases & Regression Tests

### [ ] E.1 Archer spawn khi có sẵn wall
- **Setup:** Có 2 walls (Outer, Inner). Spawn Archer mới
- **Expected:**
  - `assignedWall` = Farthest defendable wall
  - State = `ReturningToBase` hoặc `HoldingWall`

### [ ] E.2 Multiple Archers cùng wall
- **Setup:** 3 Archers cùng được assign `currentRightWall`
- **Expected:**
  - Tất cả di chuyển đến cùng 1 wall
  - Không conflict positions

### [ ] E.3 Wall destroyed khi Archer đang thủ
- **Setup:**
  1. Archer đang `HoldingWall`
  2. Wall bị phá (`Die()` → `Collapse()`)
- **Expected:**
  1. `UpdateDefendableWalls()` loại wall khỏi list
  2. GuardManager detect không còn wall
  3. Archer... (behavior cần define: retreat? last stand?)

### [ ] E.4 Day/Night transition giữa combat
- **Setup:**
  1. Đang combat (có enemy)
  2. Time chuyển từ Day → Night
- **Expected:**
  - Archer VẪN combat bình thường
  - Sau khi hết combat (`ClearAlarm`) → Check time → Nếu Night → `HoldingWall`

---

## Checklist Kiểm tra lỗi thường gặp
- [ ] Animation Event `OnDrawComplete` có null reference?
- [ ] Raycast săn (Blue line) có hướng đúng outward?
- [ ] Archer bị kẹt `ReturningToBase` mãi? (không detect đến đích)
- [ ] Event subscription có memory leak? (check Profiler)
- [ ] Duplicate event calls khi wall critical? (check console log count)
- [ ] Wall assignment update giữa combat? (phải block!)
- [ ] `UpdateDefendableWalls` spam khi heal? (phải chỉ 1 lần khi full HP)

---

## Debug Tools & Console Logs to Monitor

### GuardManager:
```
"[GuardManager] 🚨 Detected NEW Enemy: [name]"
"[GuardManager] ⚔️ ALARM ACTIVATED"
"[GuardManager] Updated LEFT/RIGHT defensive wall to: [name]"
"⚠️ Wall Critical Event: [name] (LEFT/RIGHT)"
"✅ Alarm cleared - Returning to peacetime duties"
```

### WallAndStumpManager:
```
"[WallAndStumpManager] Registered wall [name]"
"[WallAndStumpManager] Wall [name] repaired to HP=[X]"
"[WallAndStumpManager] Updated defendable walls - Left: X, Right: Y"
```

### WallBehavior:
```
"[Wall-Debug] RepairStump: Reactivated childBlocker for [name]"
```

---

## Performance Benchmarks (Optional)
- `UpdateDefendableWalls()` execution time: < 1ms với 10 walls
- Event invoke overhead: negligible
- No GC allocations during normal gameplay loop
