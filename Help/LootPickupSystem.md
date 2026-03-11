# Hệ Thống Nhặt Vật Phẩm (Loot Pickup System)

Tài liệu này hướng dẫn cách sử dụng và cấu hình module `LootPickupTrigger`, cũng như giải thích luồng hoạt động giữa nó và các thực thể khác như Player và Denizen.

## 1. Tổng Quan
`LootPickupTrigger` đóng vai trò là **"Cảm biến" (Sensor)** được gắn trên các thực thể có khả năng nhặt đồ (Player, NPCs). Nó chịu trách nhiệm phát hiện va chạm với `PhysicalItem` (Đồng xu, Công cụ) và chuyển tiếp yêu cầu xử lý tới **"Bộ não" (Brain)** của thực thể đó.

Hệ thống được thiết kế theo nguyên lý tách biệt:
- **Sensor (`LootPickupTrigger`)**: Phát hiện va chạm và kiểm tra các quy tắc vật lý/pháp lý cơ bản (item đã bị ai nhặt chưa? có đang trong cooldown không?).
- **Brain (`IItemHandle`)**: Quyết định xem có thực sự muốn nhận vật phẩm đó hay không dựa trên logic nghề nghiệp/trạng thái hiện tại.

---

## 2. Cấu Hình (Inspector Settings)

Khi gắn `LootPickupTrigger` vào một GameObject (thường là một child object có Collider2D set là IsTrigger), bạn sẽ thấy các thông số sau:

- **Can Pickup Coin**: Nếu bật, thực thể này có thể nhặt đồng xu.
- **Can Pickup Tool**: Nếu bật, thực thể này có thể nhặt các công cụ (Cung, búa, v.v.).

---

## 3. Luồng Hoạt Động Kỹ Thuật

### Bước 1: Phát hiện va chạm
Khi một `PhysicalItem` đi vào vùng Trigger:
1. Script kiểm tra xem đối tượng va chạm có component `PhysicalItem` không.
2. Kiểm tra `CheckItemRule`:
    - `pickedBy`: Nếu item đã có chủ (đang bay về phía ai đó), bỏ qua.
    - **Cooldown**: Kiểm tra các cờ `playerPickupAble`, `enemyPickupAble`, `denizensPickupAble` của item.

### Bước 2: Truy vấn "Bộ não"
Nếu quy tắc cơ bản hợp lệ, script sẽ gọi:
```csharp
_itemReceiver.HandleItem(itemGameObject);
```
`_itemReceiver` là bất kỳ script nào ở Parent implement interface `IItemHandle`.

### Bước 3: Hoàn tất
- Nếu `HandleItem` trả về **true**: Script sẽ đánh dấu `item.pickedBy = owner`, sau đó Deactivate và Destroy vật phẩm sau 0.1 giây.
- Nếu trả về **false**: Vật phẩm vẫn tồn tại trên mặt đất.

---

## 4. Tương Tác Với Các Role

### Đối Với Player (`PlayerBehavior`)
- **Coin**: Khi `HandleItem` nhận coin, nó cộng trực tiếp vào `status.coin`.
- **Tool**: Hiện tại Player tương tác với Tool qua hệ thống khác hoặc bỏ qua qua trigger này.

### Đối Đối Với Vagrant (`VagrantBehavior`)
- **Coin**: Khi nhặt được coin, `HandleItem` trả về true và kích hoạt Coroutine `TransformToVillagerNextFrame()`.
- **Kết quả**: Vagrant chuyển nghề thành Villager ngay lập tức.

### Đối Với Villager (`VillagerBehavior`)
- **Tool**: Khi nhặt được Tool (Cung, Búa), `HandleItem` sẽ:
    1. Xác định loại Tool.
    2. Gọi `ChangeRoleByTool(itemType)`.
- **Kết quả**: Villager chuyển thành Archer (nếu nhặt Cung) hoặc Worker (nếu nhặt Búa).

### Đối Đối Với Archer (`ArcherBehavior`)
- **Coin**: Archer vẫn nhặt coin để nạp lại "Giáp xu" (`ICoinShield`).
- **Logic**: Nếu số xu hiện tại < Max xu, Archer sẽ chấp nhận nhặt thêm.

---

## 5. Cách Thiết Lập Cho Một NPC Mới
1. Tạo một GameObject con dưới NPC của bạn (ví dụ tên là `PickupArea`).
2. Thêm **Collider2D** (ví dụ `CircleCollider2D`), tích chọn **Is Trigger**.
3. Thêm script **`LootPickupTrigger`**.
4. Đảm bảo script chính của NPC (ở Parent) implement interface **`IItemHandle`**.
    - Triển khai hàm `bool HandleItem(GameObject item)` để xử lý logic nhận đồ.

> [!TIP]
> Bạn có thể tùy chỉnh phạm vi nhặt đồ bằng cách thay đổi bán kính của Collider2D trên child object này mà không ảnh hưởng đến hitbox chiến đấu của NPC.
