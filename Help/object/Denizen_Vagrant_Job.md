# Báo cáo Hệ thống State Machine: Vagrant (Người lang thang)

Vagrant là đối tượng nhân lực .

## 1. Logic Vận hành và Thu nạp

VagrantJob quản lý các hành vi cơ bản nhằm mục tiêu chuyển đổi vai trò thành cư dân chính thức:

### Ưu tiên Thu nạp (Recruitment Priority)
- Cơ chế: Đây là ưu tiên cao nhất của Vagrant. AI luôn chủ động tìm kiếm các đồng Coin lân cận để tự nâng cấp bản thân.
- Phân tích mục tiêu: Sử dụng RayCastHelper để phát hiện Item có Layer là Coin. Khi thấy Coin, AI sẽ khóa mục tiêu vào biến coinTarget.

### Phản ứng Sợ hãi (Fear Response)
- Tương tự như các Denizen khác, Vagrant sẽ chuyển sang trạng thái bỏ chạy khi bị tấn công hoặc thấy kẻ thù lọt vào tầm cảm biến, nhằm bảo toàn lực lượng lao động tương lai cho người chơi.

---

## 2. Phân tích chi tiết các Trạng thái (Detailed States)

### VagrantIdleState (Trạng thái Lang thang)
- Hành vi: Di chuyển ngẫu nhiên quanh tọa độ gốc (HomePos) của trại Vagrant.
- Nhịp độ: Sử dụng walkSpeed thấp để tạo cảm giác di chuyển tự do.

### VagrantGetCoinState (Trạng thái Tiếp cận Coin)
- Kích hoạt: Khi coinTarget không null.
- Hành vi: Vagrant tăng tốc độ di chuyển về phía đồng Coin.
- Kết thúc: Khi chạm vào Coin, hàm HandleItem() sẽ được gọi để thực hiện quá trình chuyển nghề.

### VagrantFearState (Trạng thái Sợ hãi)
- Hành vi: Chạy trốn khỏi nguồn nguy hiểm. Khi ở trạng thái này, Vagrant sẽ tạm thời bỏ qua việc tìm kiếm Coin cho đến khi cảm thấy an toàn.

---

## 3. Quy trình Chuyển đổi Vai trò (Role Transition)

Đây là bước ngoặt quan trọng nhất trong vòng đời của Vagrant:
1. Tiếp xúc: Vagrant va chạm với Item Coin.
2. Xử lý Item: VagrantItemHandler thực hiện việc thu nhận đồng Coin.
3. Kích hoạt chuyển nghề: Gọi hàm ChangeRole(NPCRole.Villager) thông qua DenizenRoleController.
4. Tái cấu trúc:
   - Toàn bộ script VagrantJob bị vô hiệu hóa.
   - Script VillagerJob được kích hoạt.
   - Animator được cập nhật sang bộ diễn họa của Dân làng.
   - Tag của GameObject đổi từ Vagrant sang Villager.

---

## 4. Quản lý Quần thể (VagrantManager Integration)

- Đăng ký: Mỗi Vagrant khi sinh ra đều tự đăng ký với VagrantManager.
- Phân bổ: Manager chịu trách nhiệm duy trì số lượng Vagrant tại các trại dựa trên cấp độ của vương quốc.
- Đồng bộ thời gian: Vào các khung giờ chuyển ca (Shift Change - 6h và 18h), Vagrant có thể thực hiện việc đánh giá lại trạng thái Idle để làm mới hành vi di chuyển.

---

## 5. Tham số Kỹ thuật Quan trọng

- coinTarget: Tham chiếu đến đồng Coin đang nhắm tới.
- HomePos: Tọa độ gốc của trại nơi Vagrant sinh sống, dùng làm mốc cho việc đi lang thang.
- walkSpeed / runSpeed: Tốc độ chuẩn cho các hành vi di chuyển cơ bản.

---
