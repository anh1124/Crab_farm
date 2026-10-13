# Báo cáo Hệ thống State Machine: Villager (Dân làng)

Villager là role kinh tế trung gian: lang thang plaza, nhặt coin/tool, đổi thành Archer khi nhặt cung. **Chưa** thực thi công việc tại công trình (gõ búa, nông nghiệp) — `Worker` / `Farmer` chưa có Job.

## 1. Logic vận hành và cảm biến

### Cổng sợ hãi (Fear Gate)

Ưu tiên cao nhất trong `VillagerJob.UpdateBehavior` (sau watchdog detector và skill lock).

- Bật `isFearPending` khi nhận sát thương còn coin.
- Bỏ dở Idle / RunTool / ToTown, vào `VillagerFearState`, dùng `runSpeed`.

### VillagerSensorHandler

`RayCastHelper` quét theo hướng nhìn (watchdog bật lại nếu detector tắt). Ưu tiên coin và tool trên đất; item hợp lệ thì tiếp cận / để `LootPickupTrigger` + `VillagerItemHandler` xử lý.

### Cổng đêm

`GameTimeManager.IsDaytime == false` (`NIGHT_HOUR = 19`). Nếu không đứng trong plaza → `VillagerToTownState`.

---

## 2. Các trạng thái

### VillagerIdleState

Lang thang trong vùng plaza. Chờ `ToolManager` gán tool hoặc nhặt coin.

### VillagerRunToolState

**Không** phải “làm việc tại công trình”.

Kích hoạt khi `AssignTool(PhysicalItem)` — `currentTool` trỏ tool **trên đất**, `AssignedCarrier` = Villager.

Logic: chạy (`runSpeed`) tới transform của tool, anim `run`. Tới nơi thì `Stop()` và chờ trigger pickup (cooldown item có thể làm chưa nhặt ngay). Tool mất / bị người khác `pickedBy` → về Idle.

Nhặt xong: `VillagerItemHandler` đổi role theo `ItemType` (Bow live; Hammer/Scythe bị RoleController từ chối).

### VillagerToTownState

Ban đêm, ra khỏi plaza: dừng lang thang, về plaza.

### VillagerFearState

Chạy khỏi nguồn nguy hiểm. Kết thúc khi hết fear / vào vùng an toàn (theo logic state).

### VillagerDropCoinState

Đứng yên (`CanMove = false`), `DenizenCoinSharing.TryDropCoin()` khi `CanDropCoin()`. Nguy hiểm, hết coin, hoặc không drop được → Idle.

---

## 3. Kinh tế

### Công cụ

`ToolManager` + `DenizenStatus.currentTool` (`PhysicalItem`).

| ItemType | Ý định | Thực tế |
|----------|--------|---------|
| `Tool_Bow` | Archer | `ChangeRole(Archer)` thành công |
| `Tool_Hammer` | Worker | `ChangeRole` bị chặn |
| `Tool_Scythe` | Farmer | `ChangeRole` bị chặn |

Không có rìu / chặt cây trong handler hiện tại.

### Hình phạt sát thương

Trong `VillagerJob.ReceiveDamage`:

1. Còn coin: `SubtractCoin(1)` + `Spawner.DropFromDenizenHit`.
2. `CurrentCoins <= 0` → `ChangeRole(Vagrant)` ngay.
3. Còn coin → `isFearPending = true`.

Villager không dùng HP; coin là “máu kinh tế”.

---

## 4. Tham số

- `isFearPending`: ép Fear.
- `currentTool`: tool đang được giao để **chạy tới nhặt**, không phải công cụ cầm làm việc công trình.
- `walkSpeed` / `runSpeed`: đi thường vs sợ / chạy lấy tool.
- `villagerState`: enum lưu state (Idle, ToTown, RunTool, Fear, DropCoin) cho save/load.
