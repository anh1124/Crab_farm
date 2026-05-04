# Báo cáo Hệ thống State Machine: Villager (Dân làng)

Villager đóng vai trò xương sống của nền kinh tế, chịu trách nhiệm thực thi các công việc tại các công trình và duy trì sự ổn định bên trong vương quốc.

## 1. Logic Vận hành và Cảm biến

Hệ thống điều khiển VillagerJob quản lý các hành vi kinh tế thông qua sự phối hợp của các Module sau:

### Cổng Ưu tiên Sợ hãi (Fear Gate)
- Điều kiện: Cờ isFearPending được bật khi Villager nhận sát thương hoặc phát hiện kẻ thù quá gần.
- Phản hồi: Villager sẽ bỏ dở mọi công việc hiện tại, kích hoạt runSpeed để rút lui về sau các bức tường hoặc Plaza.

### VillagerSensorHandler (Cảm biến RayCast)
- Cơ chế: Sử dụng RayCastHelper để quét các vật thể tương tác theo hướng nhìn.
- Ưu tiên quét: Phát hiện Coin và các Tools nằm trên mặt đất. Khi phát hiện Item hợp lệ, Villager sẽ tự động chuyển trạng thái để tiếp cận và nhặt.

---

## 2. Phân tích chi tiết các Trạng thái (Detailed States)

### VillagerIdleState (Trạng thái Nghỉ)
- Hành vi: Di chuyển lang thang ngẫu nhiên trong vùng Plaza an toàn.
- Nhiệm vụ: Đứng chờ lệnh giao việc hoặc chờ Coin từ người chơi.

### VillagerRunToolState (Trạng thái Thực thi công việc)
- Kích hoạt: Xảy ra khi Villager được sở hữu một công cụ (currentTool) và có một mục tiêu công trình.
- Logic: Villager di chuyển đến vị trí công trình, kích hoạt các diễn họa làm việc (vd: gõ búa, cắt cỏ) và gửi tín hiệu hoàn thành công việc cho hệ thống quản lý công trình.

### VillagerToTownState (Trạng thái Rút lui ban đêm)
- Điều kiện: Sau 19h tối.
- Logic: Villager sẽ dừng mọi hoạt động lang thang bên ngoài (nếu có) để quay về trung tâm Plaza, nơi được bảo vệ bởi các lớp tường phòng thủ dày nhất.

### VillagerFearState (Trạng thái Hoảng loạn)
- Hành vi: Chạy trốn khỏi nguồn nguy hiểm theo hướng ngược lại.
- Chấm dứt: Trạng thái này chỉ kết thúc khi Villager đã vào được vùng an toàn hoặc mối đe dọa biến mất.

---

## 3. Cơ chế Kinh tế đặc thù

### Quản lý Công cụ (Tool Management)
- Villager có khả năng cầm nắm các loại công cụ khác nhau như Búa (Xây dựng), Rìu (Chặt cây), Liềm (Nông nghiệp).
- Loại công cụ đang sở hữu sẽ trực tiếp quy định loại công việc mà Villager có thể thực hiện.

### Cơ chế Hình phạt Sát thương (Coin Loss Penalty)
Đây là logic quan trọng nhất trong việc quản lý quần thể dân làng:
1. Khi bị trúng đòn: Villager không mất máu mà mất 1 Coin mỗi lần.
2. Rơi Item: Đồng Coin bị mất sẽ văng ra mặt đất thông qua hàm Spawner.Instance.DropFromDenizenHit().
3. Giáng cấp (Degrade): Nếu số lượng Coin về 0, Villager sẽ lập tức bị tước bỏ công cụ hiện có và chuyển vai trò trở lại thành Vagrant thông qua DenizenRoleController.

---

## 4. Tham số Kỹ thuật Quan trọng

- isFearPending: Cờ hiệu khẩn cấp điều hướng AI sang trạng thái phòng vệ.
- currentTool: Biến lưu trữ loại công cụ hiện tại để quyết định logic làm việc trong RunToolState.
- walkSpeed / runSpeed: Tốc độ di chuyển tương ứng giữa trạng thái bình thường và trạng thái sợ hãi.

---
