# Test Cases cho WallAndStumpManager ↔ GuardManager Interaction
=================================================================

## Mục tiêu
Kiểm tra tương tác giữa **WallAndStumpManager** và **GuardManager** trong các tình huống:
- Đăng ký wall mới (peacetime vs combat)
- Wall nhận damage và trigger event critical HP
- Wall được sửa chữa (heal/repair)
- Event subscription/cleanup

## Hướng dẫn Test
- Test thủ công trong Unity Editor (Play Mode)
- Quan sát **Console logs** để verify event flow
- Kiểm tra `GuardManager.currentLeftWall` và `currentRightWall` trong Inspector
- Follow test cases theo thứ tự (có dependencies)

---

## Group A: Wall Registration & Defensive Wall Assignment

### [ ] A.1 - Đăng ký Wall mới (Hòa bình)
**Setup:**
- Không có enemy (GuardManager.detectedEnemies.Count = 0)
- Xây wall mới **xa hơn** wall hiện tại

**Action:**
1. Xây wall mới (hoặc spawn wall prefab)
2. WallAndStumpManager.RegisterWall() được gọi tự động

**Expect:**
- `WallAndStumpManager.UpdateDefendableWalls()` chạy
- `GuardManager.UpdateDefensiveWalls()` **được gọi** (vì không combat)
- `currentLeftWall` hoặc `currentRightWall` update sang wall mới
- Console log: `"[GuardManager] Updated LEFT/RIGHT defensive wall to: [WallName]"`

**Behavior:**
- Archers đang `HoldingWall` hoặc `ReturningToBase` → `assignedWall` thay đổi
- Archers di chuyển đến wall mới

**Pass Condition:**
✅ Wall mới được assign vào GuardManager
✅ Archers nhận được wall assignment mới

---

### [ ] A.2 - Đăng ký Wall mới (Đang combat)
**Setup:**
- Có enemy (spawn enemy hoặc trigger alarm)
- GuardManager.isEnemyAttack = true
- Xây wall mới xa hơn **trong lúc đang combat**

**Action:**
1. Spawn enemy → Kích hoạt alarm
2. Xây wall mới

**Expect:**
- `WallAndStumpManager.RegisterWall()` chạy
- `UpdateDefendableWalls()` chạy
- `GuardManager.UpdateDefensiveWalls()` **KHÔNG được gọi** (combat lock)
- `currentLeftWall` và `currentRightWall` **KHÔNG thay đổi**

**Behavior:**
- Archers vẫn ở wall cũ (không rời vị trí đang thủ)
- Sau khi hết combat (`ClearAlarm()`) → Lúc đó mới update wall mới

**Pass Condition:**
✅ Defensive walls KHÔNG đổi trong combat
✅ Console KHÔNG có log "Updated defensive wall" trong combat

---

## Group B: Wall HP & Event System

### [ ] B.1 - Wall Critical HP → Event bắn đến GuardManager
**Setup:**
- Wall đang được assign vào `GuardManager.currentLeftWall`
- HP wall > criticalHPThreshold (default: 10)

**Action:**
1. Damage wall xuống dưới `criticalHPThreshold`
2. Ví dụ: currentHP = 50 → damage 45 → HP = 5 (< 10)

**Expect:**
1. `WallBehavior.TakeDamage()` chạy
2. `CheckAndFireCriticalEvent()` check HP < threshold
3. Event `OnCriticalHP` được bắn (invoke với GameObject wall)
4. `GuardManager.HandleWallCritical(wall)` nhận event
5. `NotifyWallCritical()` được gọi
6. Console log: `"⚠️ Wall Critical Event: [WallName] (LEFT/RIGHT)"`

**Behavior:**
- Archers đang ở wall đó nhận lệnh retreat (nếu có tường phía sau)
- State chuyển `RetreatingToInnerWall` hoặc `HoldingWall` (last stand)

**Pass Condition:**
✅ Event fire đúng 1 lần khi HP < threshold lần đầu
✅ GuardManager nhận được event và log ra console
✅ `WallAndStumpManager.CheckWallCriticalHP()` KHÔNG gọi `NotifyGuardManagerWallCritical` (tránh duplicate)

---

### [ ] B.2 - Wall được Heal (chưa đầy máu)
**Setup:**
- Wall bị damage: HP = 50/100

**Action:**
1. Heal wall +10 → HP = 60/100
2. Heal wall +10 → HP = 70/100
3. ... (nhiều lần heal nhưng chưa đầy)

**Expect:**
- `WallBehavior.Heal()` chạy
- HP tăng lên
- `OnWallRepaired` event **KHÔNG được gọi** (vì chưa đầy máu)
- `UpdateDefendableWalls()` **KHÔNG được gọi** (tránh spam)

**Behavior:**
- Wall HP tăng nhưng không trigger update logic đội hình

**Pass Condition:**
✅ Console KHÔNG có log "OnWallRepaired" hoặc "Updated defendable walls"
✅ Không spam update mỗi lần heal

---

### [ ] B.3 - Wall được Heal ĐẦY MÁU
**Setup:**
- Wall bị damage: HP = 90/100

**Action:**
1. Heal wall +15 → HP = 100/100 (đầy)

**Expect:**
1. `WallBehavior.Heal()` check `currentHp >= maxHp` → TRUE
2. `OnWallRepaired(gameObject)` **được gọi**
3. `UpdateDefendableWalls()` được gọi
4. Nếu không combat → `GuardManager.UpdateDefensiveWalls()` được gọi
5. Console log: `"[WallAndStumpManager] Wall [name] repaired to HP=[100]"`

**Behavior:**
- GuardManager re-evaluate defensive walls
- Archers có thể nhận wall assignment mới (nếu có wall khác tốt hơn)

**Pass Condition:**
✅ `OnWallRepaired` chỉ gọi **1 LẦN** khi đầy máu
✅ `UpdateDefendableWalls` chỉ gọi 1 lần

---

### [ ] B.4 - Stump Repair → Wall Resurrection
**Setup:**
- Wall state = `Stump` (đã bị phá)
- HP = 0

**Action:**
1. Heal stump → HP tăng dần
2. Khi HP >= maxHP → `RepairStump()` được gọi

**Expect:**
1. `WallBehavior.Heal()` check `state == Stump && HP >= maxHP`
2. Gọi `RepairStump()`
3. State chuyển: `Stump` → `Wall`
4. `OnWallRepaired` event fire
5. `UpdateDefendableWalls()` được gọi
6. `childBlocker` active lại (physics collision)
7. Hàm `Heal()` **return ngay** sau `RepairStump()` (không gọi logic ở dưới nữa)

**Behavior:**
- Stump hồi sinh thành Wall
- Wall có thể được assign vào GuardManager nếu là farthest defendable

**Pass Condition:**
✅ State change: Stump → Wall
✅ `OnWallRepaired` fire đúng 1 lần
✅ KHÔNG có duplicate calls (kiểm tra console log count)

---

## Group C: Event Subscription Management

### [ ] C.1 - Subscribe event khi Wall được assign vào GuardManager
**Setup:**
- GuardManager chưa có wall (hoặc có wall A)
- Có wall B mới xây

**Action:**
1. `GuardManager.UpdateDefensiveWalls()` được gọi
2. Wall B trở thành `currentLeftWall` mới

**Expect:**
1. Unsubscribe wall A cũ: `oldBehavior.OnCriticalHP -= HandleWallCritical`
2. Subscribe wall B mới: `newBehavior.OnCriticalHP += HandleWallCritical`
3. Console log: `"[GuardManager] Updated LEFT defensive wall to: WallB"`

**Behavior:**
- Event của wall cũ không còn được lắng nghe
- Event của wall mới sẽ trigger `HandleWallCritical` khi critical

**Pass Condition:**
✅ Không có memory leak (event unsubscribed đúng)
✅ Damage wall B → Event fire và GuardManager nhận được

---

### [ ] C.2 - Cleanup khi GuardManager bị destroy
**Setup:**
- GuardManager đang có `currentLeftWall` và `currentRightWall`

**Action:**
1. Destroy GuardManager GameObject (hoặc Scene unload)

**Expect:**
1. `GuardManager.OnDestroy()` chạy
2. Unsubscribe tất cả wall events:
   - `leftBehavior.OnCriticalHP -= HandleWallCritical`
   - `rightBehavior.OnCriticalHP -= HandleWallCritical`
3. Clear lists: `leftFlankArchers`, `rightFlankArchers`, `detectedEnemies`

**Behavior:**
- Không có null reference errors
- Không có "callback to destroyed object" errors

**Pass Condition:**
✅ Console KHÔNG có error khi destroy
✅ Memory Profiler không thấy orphaned event subscriptions

---

## Group D: Performance & Edge Cases

### [ ] D.1 - Heal spam KHÔNG gây spam UpdateDefendableWalls
**Setup:**
- Wall HP = 50/100

**Action:**
1. Spam heal nhiều lần (mỗi lần +5 HP):
   - Heal → HP = 55
   - Heal → HP = 60
   - Heal → HP = 65
   - ... (10 lần heal)
   - Heal → HP = 100 (đầy)

**Expect:**
- Console log `"Updated defendable walls"` chỉ xuất hiện **1 LẦN** (khi HP = 100)
- KHÔNG xuất hiện mỗi lần heal

**Behavior:**
- Performance tốt, không spam tính toán

**Pass Condition:**
✅ Search Console filter "Updated defendable" → Chỉ 1 kết quả
✅ Không có lag khi builder đang sửa wall

---

### [ ] D.2 - Critical Flag Reset & Re-trigger
**Setup:**
- Wall HP = 100/100
- criticalThreshold = 10

**Action:**
1. **Lần 1:** Damage wall xuống HP = 5 (< 10) → Event fire lần 1
2. Heal wall lên HP = 20 (> 10) → Flag reset
3. **Lần 2:** Damage wall xuống HP = 5 (< 10) lần nữa → Event phải fire lại

**Expect:**
- Event `OnCriticalHP` fire **2 lần** (lần 1 và lần 2)
- `hasFiredCriticalEvent` flag được reset khi HP > threshold
- `ResetCriticalFlag()` được gọi trong `Heal()`

**Behavior:**
- Flag không bị "stuck" ở true
- Event có thể re-trigger nếu wall bị damage lại

**Pass Condition:**
✅ Console log "⚠️ Wall Critical Event" xuất hiện 2 lần
✅ GuardManager nhận được event cả 2 lần

---

### [ ] D.3 - Không có Wall nào defendable
**Setup:**
- Tất cả walls bị phá hoặc HP < criticalThreshold
- `defendableLeftWalls.Count = 0`
- `defendableRightWalls.Count = 0`

**Action:**
1. Gọi `GuardManager.UpdateDefensiveWalls()`

**Expect:**
- `GetFarthestDefendableWall()` returns `null`
- `currentLeftWall = null`
- `currentRightWall = null`
- GuardManager không crash

**Behavior:**
- Archers vẫn hoạt động (không bị null reference)
- Có thể ở trạng thái "no wall to defend" (last stand hoặc hunting nếu daytime)

**Pass Condition:**
✅ Không có NullReferenceException
✅ Console có warning log về việc không tìm thấy wall

---

### [ ] D.4 - Wall destroyed trong combat
**Setup:**
- Đang combat
- Archer đang `HoldingWall` tại wall A

**Action:**
1. Wall A bị phá → `Die()` → `Collapse()` → State = Stump
2. `UpdateDefendableWalls()` loại wall A khỏi list

**Expect:**
- Wall A không còn trong `defendableLeftWalls`
- GuardManager phải handle "wall destroyed mid-combat"
- Archer cần retreat hoặc reassign (behavior tùy design)

**Behavior:**
- (Cần clarify design: Archer retreat ngay hay đứng yên?)
- Không crash

**Pass Condition:**
✅ Game không crash
✅ Archer có behavior hợp lý

---

## Checklist tổng hợp trước khi commit

### Critical (Phải PASS):
- [ ] A.1: Wall update trong peacetime
- [ ] A.2: Wall KHÔNG update trong combat
- [ ] B.1: Critical HP event fire đúng
- [ ] B.3: OnWallRepaired chỉ 1 lần khi đầy máu
- [ ] C.1: Event subscription đúng
- [ ] D.1: Heal không spam update

### Important (Nên PASS):
- [ ] B.4: Stump resurrection
- [ ] C.2: OnDestroy cleanup
- [ ] D.2: Critical flag reset

### Edge Cases (Tốt nếu PASS):
- [ ] D.3: Handle no defendable walls
- [ ] D.4: Wall destroyed mid-combat

---

## Console Logs cần check

### Success:
```
[GuardManager] Updated LEFT defensive wall to: Wall_Tier1
[WallAndStumpManager] Wall Wall_Tier1 repaired to HP=100
✅ Alarm cleared - Returning to peacetime duties
```

### Critical Event:
```
⚠️ Wall Critical Event: Wall_Tier1 (LEFT)
```

### Errors cần tránh:
```
❌ NullReferenceException: Object reference not set...
❌ MissingReferenceException: The object of type 'WallBehavior' has been destroyed...
```

---

## Notes
- Tất cả tests phải chạy trong **Play Mode**
- Một số tests có dependencies (phải chạy theo thứ tự)
- Nếu test FAIL → Check Console errors → Review code logic
- Dùng Unity Profiler để check memory leaks (C.2)
