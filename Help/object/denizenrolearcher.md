# Archer Role Documentation

## Overview
**Archer** là đơn vị chiến đấu tầm xa cơ bản, chịu trách nhiệm săn bắt động vật hoang dã (ban ngày) và phòng thủ chống quái vật (ban đêm).

## Components & Configuration

*   **Role:** `NPCRole.Archer`
*   **Behavior Script:** `ArcherBehavior.cs`
*   **Detection:** Sử dụng `CircleDetectHelper` (Quét bán kính tròn).
*   **Manager:** Được quản lý bởi `GuardManager`.
*   **Data:** Sử dụng chung `DenizenStatus` (Lấy `RunSpeed`, `WalkSpeed`, `archerDetectRange`...).

## Behavior State Machine

Archer hoạt động dựa trên máy trạng thái (State Machine) đơn giản, được điều phối trong `UpdateBehavior()`:

### 1. Hunting (Săn bắt - Ban ngày)
*   **Điều kiện:** Ban ngày (6h - 19h) VÀ Không có kẻ thù (Safe).
*   **Hành vi:**
    *   Đi lang thang (`Idle`) tìm động vật (Rabbit, Deer) trong vùng `archerDetectRange`.
    *   Khi phát hiện con mồi -> Chuyển sang `Attack`.
    *   Nếu mục tiêu chạy quá xa hoặc chết -> Quay lại `Idle` / `Hunting`.
*   **Khu vực hoạt động:** Tự do di chuyển ở ngoại vi để săn mồi.

### 2. Defending (Phòng thủ - Ban đêm)
*   **Điều kiện:** Ban đêm (19h - 6h) HOẶC Có lệnh rút lui.
*   **Hành vi:**
    *   Di chuyển về vị trí phòng thủ được `GuardManager` chỉ định.
    *   **Vị trí ưu tiên:**
        1.  Sau bức tường (`Wall`) gần nhất ở phía mình phụ trách (Trái/Phải).
        2.  Nếu không có tường -> Rút về **TownCenter Plaza**.
    *   Đứng yên tại slot được giao, quay mặt ra phía ngoài để sẵn sàng bắn.

### 3. Attacking (Tấn công)
*   **Điều kiện:** Phát hiện mục tiêu (`Enemy` hoặc `Animal`) trong tầm `CircleDetectHelper`.
*   **Hành vi:**
    *   Đứng lại (`Idle`) hoặc giữ khoảng cách (`Kiting` - chưa implement full).
    *   Quay mặt về phía mục tiêu.
    *   Chơi animation `Attack`.
    *   Bắn Projectile (Mũi tên) về phía mục tiêu thông qua `DenizenAnimationEvents` (Animation Event trigger).
    *   **Cooldown:** Dựa trên Animation speed.

### 4. Fear (Sợ hãi/Rút lui)
*   **Điều kiện:** Bị tấn công cận chiến hoặc Quái vật quá đông/quá gần mà không có tường che chắn (Logic đang được refine).
*   **Hành vi:** Chạy ngược hướng nguy hiểm hoặc chạy về TownCenter.

## Detection System (Hệ thống Tầm nhìn)

*   **Script:** `CircleDetectHelper.cs`
*   **Cơ chế:**
    *   Sử dụng `Physics2D.OverlapCircleAll`.
    *   **Bán kính quét:** Lấy từ `DenizenStatus.detectRangeRaycast` (Tên biến cũ, thực tế là `archerDetectRange`).
    *   **Layer Mask:** Chỉ quét các layer quan trọng (`Enemy`, `Animal`).
*   **Gizmos:** Hiển thị vòng tròn màu **Hồng (Magenta)** trong Scene khi chọn Archer.

## Interaction with Managers

### GuardManager
*   **Vai trò:** "Sếp" quản lý trực tiếp Archer.
*   **Chức năng:**
    *   Phân chia Archer về 2 phía (Trái/Phải) dựa trên vị trí spawn ban đầu.
    *   Tính toán vị trí đứng (Slot) sau tường để Archer không chồng chéo nhau.
    *   Ra lệnh rút lui (`Retreat`) hoặc tấn công (`Attack`).

### DenizenManager
*   **Vai trò:** Quản lý chung vòng đời.
*   **Chức năng:**
    *   Lưu trữ tham chiếu `ArcherController` (Animator).
    *   Xử lý khi Archer chết (Unregister).

## Debugging

*   **Gizmos:**
    *   **Màu Hồng:** Tầm phát hiện (`detectRange`).
    *   **Màu Vàng:** Đường đi đến mục tiêu (`targetPosition`).
*   **Inspector:**
    *   Theo dõi `ArcherBehavior.currentState` để biết Archer đang làm gì.
    *   Kiểm tra `Target` để xem Archer đang nhắm vào ai.
