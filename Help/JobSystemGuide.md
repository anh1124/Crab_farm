# 📖 Hướng Dẫn Hệ Thống Nghề Nghiệp (Job System)

Tài liệu này giải thích chi tiết về cơ chế chuyển nghề, cấu trúc Prefab nhân vật (Denizen), và quy trình chuyển đổi cụ thể của từng Role trong game.

---

## 🏗️ 1. Cấu Trúc Prefab Gốc (Denizen)

Đây là khung xương chung cho tất cả các loại nhân vật (Vagrant, Villager, Archer, Worker, Farmer). Khi khởi tạo, nó chưa mang logic cụ thể nào cho đến khi code chạy.

**Danh sách Component bắt buộc trong Unity Inspector:**

1.  **Transform**: Vị trí, góc xoay.
2.  **SpriteRenderer**: Hiển thị hình ảnh nhân vật.
3.  **Animator**: Điều khiển chuyển động.
    *   *Lưu ý*: Controller ban đầu có thể để trống, code sẽ tự gán.
4.  **Rigidbody2D**: Xử lý vật lý (Dynamic/Kinematic).
5.  **Collider2D**: Xử lý va chạm (Box/Capsule/Circle).
6.  **DenizenController (Quan Trọng ⚠️)**: Script quản lý chính.
    *   **Phải kéo thả** các Animation Controller Asset vào các ô tương ứng:
        *   `Vagrant Controller`
        *   `Villager Controller`
        *   `Archer Controller`
        *   `Worker Controller`
        *   `Farmer Controller`
7.  **RayCastHelper**: Hỗ trợ cảm biến môi trường (Optional - Code tự thêm nếu thiếu).

❌ **KHÔNG CẦN ADD SẴN**: `VagrantBehavior`, `VillagerBehavior`, `ArcherStatus`... Code sẽ tự động thêm (`AddComponent`) khi chuyển nghề.

---

## 🔄 2. Cơ Chế Chuyển Nghề

Logic trung tâm nằm tại `DenizenController.cs`.

### Biến `CurrentRole`
Xác định nghề nghiệp hiện tại.
*   Mặc định khi Spawn: Được set trong Inspector hoặc Code khởi tạo (VD: `NPCRole.Villager` hoặc `NPCRole.Vagrant`).

### Hàm `ApplyRole(NPCRole role)`
Đây là hàm "phép thuật" thực hiện chuyển đổi:
1.  **Gán Tag**: Đổi tag của GameObject thành tên nghề (VD: "Archer").
2.  **Add Components**:
    *   Thêm `Status` script tương ứng (VD: `ArcherStatus`).
    *   Thêm `Behavior` script tương ứng (VD: `ArcherBehavior`).
3.  **Đổi Animation**: Thay thế `runtimeAnimatorController` của Animator bằng Controller tương ứng đã setup trong Inspector.
4.  **Chuyển Dữ Liệu**: Chuyển coins hoặc chỉ số cũ sang nghề mới (qua hàm `TransferData`).
5.  **Xóa Logic Cũ**: Các script Behavior/Status của nghề cũ sẽ bị `Destroy`.

---

## 🎬 3. Quy Trình Chuyển Nghề Cụ Thể

### A. Từ Vagrant ➔ Villager 💰
Quá trình này tự động khi Vagrant nhặt được tiền.

1.  **Trigger**: Vagrant di chuyển và va chạm (`OnTriggerEnter2D`) với object có tag **"Coin"**.
2.  **Xử lý (VagrantBehavior)**:
    *   Gọi `PickUpCoin()` -> Dùng `Utility.ProcessCoinPickup` để xóa đồng xu.
    *   Chạy Coroutine đợi 1 frame.
    *   Gọi `DenizenController.ChangeRole(NPCRole.Villager)`.
3.  **Kết quả**: Vagrant biến mất, trở thành Villager, có sẵn 1 xu trong túi.

### B. Từ Villager ➔ Archer / Worker / Farmer 🛠️
Quá trình này xảy ra khi Villager nhận được công cụ (Tool).

1.  **Spawn Tool**: Người chơi hoặc hệ thống tạo ra một **ToolItem** (Cung, Búa, Liềm).
    *   Tool này tự đăng ký với `VillagerManager`.
2.  **Phân Công**: `VillagerManager` tìm Villager gần nhất đang thất nghiệp (Idle) và giao nhiệm vụ "Đi lấy tool".
3.  **Di Chuyển**: Villager chạy đến vị trí Tool (`RunToGetTool`).
4.  **Trigger**: Villager chạm vào Tool.
5.  **Xử lý (VillagerBehavior)**:
    *   Xác nhận đúng tool được giao.
    *   Claim quyền sở hữu Tool (để tránh người khác nhặt tranh).
    *   Kiểm tra loại Tool (`ToolType`):
        *   `Bow` ➔ Gọi `ChangeRole(NPCRole.Archer)`
        *   `Hammer` ➔ Gọi `ChangeRole(NPCRole.Worker)`
        *   `Scythe` ➔ Gọi `ChangeRole(NPCRole.Farmer)`
    *   Destroy object Tool.
6.  **Kết quả**: Villager biến thành nghề tương ứng với công cụ vừa nhặt.

---

## 🛠️ 4. Hệ Thống ToolItem

File: `ToolItem.cs`

Mỗi Tool (Prefab thả ngoài đất) chứa component `ToolItem`, có biến `type` định nghĩa nghề nghiệp nó đại diện:

| ToolType (Enum) | Nghề Tương Ứng (NPCRole)    |
|-----------------|-----------------------------|
| **Bow**         | Archer 🏹                  |
| **Hammer**      | Worker 🔨                  |
| **Scythe**      | Farmer 🌾                  |


---

