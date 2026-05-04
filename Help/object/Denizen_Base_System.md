# Báo cáo Hệ thống State Machine của Denizen (Cấu trúc Cơ sở)

Hệ thống Denizen (Cư dân) được xây dựng trên một kiến trúc Modular State Machine chuyên sâu. Điểm đặc biệt của hệ thống là khả năng hoán đổi toàn bộ Job (nghề nghiệp) mà vẫn giữ nguyên được các thành phần vật lý và dữ liệu nền tảng.

## 1. Các thành phần nền tảng (Foundation Architecture)

Sự vận hành của Denizen dựa trên sự phối hợp chặt chẽ của các Module sau:

### DenizenRoleController (Bộ điều hướng chính)
- Nhiệm vụ: Là driver chính của toàn bộ hệ thống Denizen. 
- Cơ chế: Quản lý trực tiếp việc hoán đổi Job, điều phối chu kỳ cập nhật (Update), thay đổi Runtime Animator Controller và quyết định hành vi AI tổng quát của Job đang hoạt động.
- Setup: Chịu trách nhiệm nhận tiêm các tham chiếu từ Manager và phân phối xuống các Job con để duy trì tính nhất quán.

### DenizenStatus (Dữ liệu chính và Lưu trữ)
- Nhiệm vụ: Là kho dữ liệu chính dùng chung cho một Denizen trong suốt vòng đời.
- Cơ chế: Lưu trữ toàn bộ các tham số runtime và các cờ hiệu (Flags) quan trọng. Đây là thành phần chịu trách nhiệm chính trong việc cung cấp dữ liệu cho hệ thống Lưu/Tải (Save/Load).
- Dữ liệu: Chứa thông tin về Role, số lượng Coin, công cụ, và các trạng thái điều khiển như isSkillCasting, isFearPending.

### DenizenContext (Kho dữ liệu tham chiếu)
- Nhiệm vụ: Chứa toàn bộ các liên kết cần thiết để tránh việc gọi GetComponent liên tục trong vòng lặp Update.
- Tham chiếu cục bộ: Animator, SpriteRenderer, DenizenMovement, DenizenStatus, CircleDetectHelper/RayCastHelper.
- Tham chiếu toàn cục: GuardManager, TimeManager, WallAndStumpManager, NPCUpdateManager, ArrowPool.

---

## 2. Luồng vận hành chi tiết (Detailed Workflow)

### Quy trình Khởi tạo (Initialization Pipeline)
Hệ thống hỗ trợ hai luồng khởi tạo riêng biệt để đảm bảo tính linh hoạt:
1. Khởi tạo từ Save: Dữ liệu trạng thái cũ được khôi phục và bơm trực tiếp thông qua hàm Setup(), giúp cư dân quay lại đúng vị trí và nghề nghiệp đã lưu trước đó.
2. Khởi tạo từ Hệ thống: Khởi tạo mặc định thông qua Spawner.cs. Tại luồng này, dữ liệu khởi đầu luôn được thiết lập mặc định là Vagrant (Người lang thang) với các chỉ số cơ bản nhất.

### Cơ chế Hoán đổi nghề nghiệp (Role Transition)
Khi ChangeRole() được thực hiện:
1. Giải phóng: Hủy đăng ký tại Manager cũ, tắt cờ isSkillCasting và dừng mọi chuyển động hiện tại.
2. Hoán đổi: Tắt script Job cũ, bật Job mới. Animator thực hiện Rebind() để cập nhật bộ Trigger mới.
3. Làm sạch dữ liệu: Reset các mục tiêu cũ (currentTarget, coinTarget) để Job mới bắt đầu từ trạng thái sạch.
4. Đăng ký mới: Job mới thực hiện chu trình OnEnable() để thiết lập các lịch trình (DenizenSchedule) riêng.

---

## 3. Hệ thống Di chuyển và Diễn hoạt

### DenizenMovement (Kinematic Movement)
- Cơ chế: Hoạt động dựa trên một Vector3 làm đích đến (_moveTargetPos). Thành phần này luôn cố gắng điều khiển Denizen di chuyển về phía tọa độ này một cách liên tục.
- Lọc điều kiện: Hành vi di chuyển chỉ được thực thi nếu không bị khóa bởi các trạng thái từ Status (CanMove == false), đang trong quá trình thi triển kỹ năng (isSkillCasting == true), hoặc các điều kiện logic khác.
- Tốc độ: Hệ thống điều chỉnh linh hoạt giữa hai cấp độ tốc độ chính là walkSpeed (di chuyển thường, làm việc) và runSpeed (rút lui, hoảng sợ). Tốc độ được Job gán trực tiếp tùy theo ngữ cảnh hành động.

### Đồng bộ Diễn hoạt (Animation Sync)
- Sử dụng SetTrigger và SetBool để điều khiển Animator.
- Trước khi đổi State, hệ thống luôn thực hiện ResetAllAnimatorParams() để tránh việc các Trigger cũ làm sai lệch diễn hoạt của State mới.

---

## 4. Liên kết Tài liệu chi tiết từng Job

Hệ thống phân cấp các quyết định cụ thể xuống từng Job script:
- [Tài liệu Archer: Chiến thuật phòng thủ và săn bắn](file:///e:/unity/Crab_farm/Help/object/Denizen_Archer_Job.md)
- [Tài liệu Villager: Quản lý công cụ và kinh tế](file:///e:/unity/Crab_farm/Help/object/Denizen_Villager_Job.md)
- [Tài liệu Vagrant: Quy trình thu nạp và sinh tồn](file:///e:/unity/Crab_farm/Help/object/Denizen_Vagrant_Job.md)

---
