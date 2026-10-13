# Hệ Thống Nhặt Vật Phẩm (Loot Pickup System)

Tài liệu mô tả module `LootPickupTrigger` và luồng xử lý với Player, Denizen (Job) và Enemy **theo code hiện tại**.

## 1. Tổng Quan

`LootPickupTrigger` là **cảm biến** gắn trên child object (vùng pickup). Nó phát hiện `PhysicalItem` (coin, công cụ) rồi hỏi **bộ não** (`IItemHandle`) có nhận item hay không.

Tách vai trò:

- **Sensor (`LootPickupTrigger`)**: va chạm, rule pháp lý (đã có chủ? phe nào được nhặt?).
- **Brain (`IItemHandle`)**: quyết định theo nghề / túi đầy / trạng thái Fear.

Trên Denizen, `IItemHandle` **không** nằm ở Job. `DenizenContext` implement interface và forward sang `CurrentJob.HandleItem`.

---

## 2. Cấu Hình Inspector

Gắn trên child (ví dụ `PickupArea`) có `Collider2D` **Is Trigger**:

- **Can Pickup Coin**: cho phép nhặt `ItemType.Coin`.
- **Can Pickup Tool**: cho phép nhặt tool (`Tool_Bow`, `Tool_Hammer`, `Tool_Scythe`).
- **Auto Pickup Without Receiver**: nếu bật, bỏ qua `IItemHandle` — miễn `CheckItemRule` pass là nhặt. Dùng khi object không có brain.
- **Is Debug**: log từng bước.

---

## 3. Luồng Kỹ Thuật

### Bước 1: Phát hiện

`OnTriggerEnter2D` lấy `PhysicalItem`. Nếu không có thì bỏ.

`CheckItemRule`:

1. `pickedBy != null` → bỏ.
2. Coin nhưng `canPickupCoin == false` / tool nhưng `canPickupTool == false` → bỏ.
3. Coin đang `floating` và cả `playerPickupAble` lẫn `denizensPickupAble` đều false → bỏ.
4. Theo phe của owner (tag Player / Enemy / còn lại = Denizen) kiểm tra `playerPickupAble` / `enemyPickupAble` / `denizensPickupAble`.

Player đầy coin được chặn sớm trong `IsPlayerFullOfCoin` (trước khi hỏi brain).

### Bước 2: Quyết định

- `autoPickupWithoutReceiver == true` → luôn nhặt.
- Ngược lại: `GetComponentInParent<IItemHandle>().HandleItem(item)`.

### Bước 3: Hoàn tất nếu nhận `true`

1. `pickedBy = owner`.
2. `SetActive(false)`.
3. Coin: phát `Coin_Pickup` qua `AudioEmitter` trên owner; trả về `CoinPool` (`ReturnCoin`). Không có pool thì `Destroy` sau 0.1s.
4. Tool: `Destroy` sau 0.1s.

Nếu `HandleItem` trả `false`, item vẫn nằm trên đất.

---

## 4. Ai implement `IItemHandle`

| Thực thể | Script | Hành vi |
|----------|--------|---------|
| Denizen | `DenizenContext` → Job handler | Xem từng role bên dưới |
| Player | `PlayerItemHandler` | Coin: `AddCoin(1)` nếu chưa đầy. Tool: TODO, hiện trả false |
| Enemy | `EnemyAIController` | Nhặt theo logic enemy (bow/hammer/scythe được liệt kê trong switch) |

Không còn `VagrantBehavior` / `VillagerBehavior` / `ArcherBehavior`.

### Vagrant (`VagrantItemHandler`)

- Nhặt coin → `AddCoin(1)` + `ChangeRole(Villager)` ngay.
- Fear: bỏ coin thường; vẫn nhận coin player ném (`playerPickupAble == false`).

### Villager (`VillagerItemHandler`)

- `Tool_Bow` → `ChangeRole(Archer)` (live).
- `Tool_Hammer` / `Tool_Scythe` → gọi `ChangeRole(Worker/Farmer)` nhưng RoleController **từ chối** vì chưa có Job.
- Coin: nhặt nếu `!IsFull`.
- Tool được `ToolManager` gán (`currentTool`): nhặt xong clear `currentTool`.

### Archer (`ArcherItemHandler`)

- Coin nếu `!IsFull` → `AddCoin(1)` (giáp xu qua `DenizenStatus`, không qua interface `ICoinShield` riêng).

---

## 5. Thiết lập NPC mới

1. Child `PickupArea` + `Collider2D` Is Trigger.
2. Gắn `LootPickupTrigger`, bật coin/tool phù hợp.
3. Parent implement `IItemHandle` **hoặc** Denizen dùng `DenizenContext` sẵn có.
4. Owner nên có `AudioEmitter` nếu muốn tiếng nhặt coin.

Phạm vi nhặt = bán kính collider child, không đụng hitbox chiến đấu.
