# Denizen Object Documentation

## Overview
**Denizen** là đối tượng nhân vật cơ bản nhất ,
bao gồm các vai trò:
 **Vagrant** (Kẻ lang thang), 
 **Villager** (Dân làng), 
 **Archer** (Cung thủ),
 **Builder** (Thợ xây - chưa implement), 
 **Farmer** (Nông dân - chưa implement).

Mọi Denizen đều dùng chung một Prefab cơ bản và thay đổi hành vi (Behavior) cũng như ngoại hình (Animation) dựa trên `NPCRole`.

## GameObject Structure (Hierarchy)

```text
Denizen (GameObject)
├── Sprite (SpriteRenderer) - Hiển thị hình ảnh nhân vật
├── Collider (BoxCollider2D/CircleCollider2D) - Xử lý va chạm vật lý/Trigger
├── Rigidbody2D - Xử lý vật lý (Kinematic/Dynamic)
└── [Components] - Các script quản lý logic
```

## Inspector Configuration

### Tag & Layer
*   **Tag:** `Denizen` (Mặc định).
*   **Layer:** `Ally` (Dùng cho Phe ta).

### Essential Components (Bắt buộc)

1.  **`SpriteRenderer`**:
    *   Hiển thị hình ảnh.
    *   **Sorting Layer:** `Entities` (hoặc tương đương).

2.  **`Animator`**:
    *   **Controller:** Được thay đổi động (RuntimeAnimatorController) bởi `DenizenRoleController`.

3.  **`BoxCollider2D`**:
    *   **Is Trigger:** `True` (Dùng để detect va chạm, không chặn vật lý).
    *   **Offset/Size:** Cần chỉnh vừa vặn với Sprite.

4.  **`Rigidbody2D`**:
    *   **Body Type:** `Kinematic` (Dùng script để di chuyển).
    *   **Simulated:** True.
    *   **Use Full Kinematic Contacts:** True (để detect trigger).
    *   **Constraints:** Freeze Rotation Z.

### Script Components (Logic - Theo thứ tự trong Inspector)

1.  **`Ray Cast Helper (Script)`**:
    *   Hỗ trợ Raycast quét phía trước (Dùng cho Vagrant/Villager).

2.  **`Denizen Role Controller (Script)`**:
    *   Quản lý việc chuyển đổi Role (`ChangeRole`).

3.  **`Denizen Status (Script)`**:
    *   Lưu trữ dữ liệu chỉ số (HP, Speed, Coins...).

4.  **`Denizen Animation Events (Script)`**:
    *   Bắt sự kiện từ Animation (ví dụ: Fire arrow, Step sound...).

5.  **`Archer Behavior (Script)`**:
    *   Logic behavior cho Cung thủ.

6.  **`Vagrant Behavior (Script)`**:
    *   Logic behavior cho Kẻ lang thang.

7.  **`Villager Behavior (Script)`**:
    *   Logic behavior cho Dân làng.

8.  **`Circle Detect Helper (Script)`**:
    *   Hỗ trợ quét vùng tròn xung quanh (Dùng cho Archer).

## Role Switching Flow (Quy trình đổi nghề)

Khi `DenizenRoleController.ChangeRole(newRole)` được gọi:

1.  **Unregister Old Role:**
    *   Gỡ bỏ khỏi Manager cũ (ví dụ: Archer rời khỏi GuardManager).
2.  **Apply New Role:**
    *   Tắt Behavior cũ.
    *   Bật Behavior mới (ví dụ: `ArcherBehavior` enabled).
    *   Đổi Animator Controller.
    *   Cập nhật `DenizenStatus`.
    *   **Cấu hình Detect:**
        *   Nếu là **Archer**: Bật `CircleDetectHelper`, có thể tắt `RayCastHelper`.
        *   Nếu là **Khác**: Bật `RayCastHelper`, tắt `CircleDetectHelper`.
3.  **Register New Role:**
    *   Đăng ký vào Manager mới (ví dụ: Archer vào GuardManager).

## Yêu cầu kiểm tra thực tế trên Editor (User Check Update)

Để tài liệu chính xác 100% với Project hiện tại, bạn vui lòng kiểm tra giúp mình các mục sau trên Prefab hoặc Instance trong Scene:

1.  **Layer & Tag chuẩn:**
    *   Layer của Denizen đang là `Default` hay `NPC`?
    *   Tag của Vagrant lúc mới sinh ra là `Untagged` hay `Vagrant`?

2.  **Collider Settings:**
    *   `BoxCollider2D` đang để `Is Trigger` là ON hay OFF?
    *   Có thêm `CircleCollider2D` nào cho vùng trigger nhặt đồ không?

3.  **Rigidbody2D Settings:**
    *   Body Type đang là `Kinematic` hay `Dynamic`? (Ảnh hưởng đến việc va chạm đẩy nhau).

4.  **Script References:**
    *   Kiểm tra xem `DenizenRoleController` có đang tham chiếu trực tiếp đến các Prefab tool hay không (đã refactor sang ToolManager nhưng cần confirm).

*Nếu bạn cung cấp thêm thông tin này, mình sẽ update file `denizen.md` chi tiết hơn!*
