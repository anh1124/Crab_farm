# Hướng Dẫn Hệ Thống Skill (v2.0)

Hệ thống Skill hiện tại được thiết kế theo hướng **Data-Driven** (dựa trên dữ liệu) giúp việc tạo mới, thay đổi và quản lý kỹ năng trở nên cực kỳ linh hoạt mà không cần can thiệp nhiều vào code.

---

## 1. Cách tạo và thêm Skill mới

### Bước A: Tạo Script cho Skill (Nếu muốn logic đặc biệt)
Nếu bạn muốn tạo một Skill mới hoàn toàn (VD: Bắn cung, Hồi máu), hãy tạo một script C# kế thừa từ `SkillBase`:
1. Tạo file `.cs` mới.
2. Viết logic vào hàm `OnStart`, `OnHit`, hoặc `OnEnd`.
3. Lưu ý: Thêm attribute `[CreateAssetMenu]` để có thể tạo file Asset.

### Bước B: Tạo Skill Asset (Dữ liệu)
1. Trong Unity, chuột phải vào thư mục dữ liệu (VD: `Assets/Data/Skills/`).
2. Chọn **Create > Skills > Player > [Tên Skill của bạn]**.
3. Một file `.asset` sẽ được tạo ra. Điền các thông số vào Inspector:
   - **Resource**: Chọn Type (Mana/Stamina) và nhập `Resource Cost`.
   - **UI**: Kéo Icon (Sprite) và đặt tên hiển thị.
   - **Animation**: Nhập `Animation Trigger` và tích/bỏ tích `Use Animation` (Tắt nếu skill spawn tức thì).
   - **Stats**: Nhập `Damage`, `Cooldown`.

### Bước C: Nhập kho (Skill Library)
Để hệ thống có thể tìm thấy skill mới:
1. Chọn file `SkillLibrary.asset` (nằm trong `Assets/.../Resources/Data/`).
2. Chuột phải vào file này trong cửa sổ Project -> Chọn **"Auto Populate Skills"**.
3. Skill mới của bạn sẽ tự động được thêm vào kho dữ liệu toàn cầu.

---

## 2. Cấu trúc Hệ thống

Hệ thống gồm 4 thành phần chính:

1. **SkillBase (Data)**: Chứa thông tin "tĩnh" của skill (giá tiền, icon, thời gian hồi).
2. **SkillManager (Logic)**: Gắn trên Player/NPC. Quản lý các slot skill, trạng thái (Ready, Cooling, Executing) và thực hiện trừ tài nguyên.
3. **SkillLibrary (Database)**: Một Singleton dữ liệu nằm trong folder `Resources`. Cho phép truy cập bất kỳ skill nào trong game thông qua code: `SkillLibrary.Instance.GetSkillByName("...")`.
4. **SkillButtonUI (Visual)**: Hiển thị trạng thái của một slot skill lên màn hình.

---

## 3. Cơ chế hoạt động của UI (Skill Button)

Mỗi nút Skill trên màn hình hoạt động như một "máy trạng thái" (State Machine) tự động cập nhật mỗi frame:

- **Kiểm tra Kỹ năng**: Nếu slot đó không có kỹ năng nào được trang bị (`null`), nút sẽ tự động ẩn đi (`SetActive(false)`).
- **Trạng thái Sẵn sàng (Ready)**:
    - UI liên tục hỏi `PlayerBehavior` xem Player có đủ tài nguyên (Mana/Stamina) không.
    - **Thiếu tài nguyên**: Hiện lớp phủ `ResourceMask` (Màu xanh cho Mana, màu vàng cho Stamina) và khóa nút.
    - **Đủ tài nguyên**: Ẩn Mask, nút có thể nhấn được.
- **Trạng thái Thi triển (Executing)**:
    - Khi vừa nhấn nút, UI hiện lớp phủ màu tối để báo hiệu đang đánh.
    - Nếu Skill có `Use Animation = false`, trạng thái này sẽ kết thúc ngay lập tức.
- **Trạng thái Hồi chiêu (Cooling)**:
    - UI kích hoạt `CooldownGroup`. 
    - `CooldownRadialImage` sẽ xoay theo hình tròn (Filled Radial) dựa trên tỉ lệ thời gian còn lại.
    - `CooldownText` hiển thị số giây đếm ngược (làm tròn lên hoặc hiện số thập phân nếu dưới 1s).
- **Tự động cập nhật**: Nếu bạn dùng lệnh `EquipSkill` để đổi chiêu khác vào slot, UI sẽ tự động nhận ra sự thay đổi để vẽ lại Icon và cập nhật số tiền tiêu hao ngay lập tức.

---
*Tài liệu này được lưu tại `Assets/helper/SkillSystem_Guide.md`*
