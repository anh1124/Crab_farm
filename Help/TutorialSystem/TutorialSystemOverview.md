# Hệ Thống Tutorial - Tổng Quan và Cách Hoạt Động

## 1. Kiến Trúc Tổng Quan

Hệ thống Tutorial được xây dựng theo hướng 
- Data-Driven (Dựa trên dữ liệu) 
- Event-Based (Dựa trên sự kiện).

 (Controller)
 (ScriptableObjects)
 (Executor)
 (Condition Checker)

### Các Thành Phần Chính:

*   TutorialController (Core): Quản lý chính ,chạy lần lượt các tutorial trong tutorial registry
*   TutorialScriptSO & TutorialDialogLineSO (Data)
*   TutorialActionExecutor (Logic): Thực hiện các hành động cụ thể khi một bước tutorial bắt đầu (ví dụ: spawn tiền, hiện mũi tên chỉ dẫn).
*   TutorialConditionChecker (Logic): Lắng nghe các sự kiện từ thế giới game (như người chơi nâng cấp nhà, đi vào vùng chỉ định) lắng nghe để khi  nào thì next tutoral
*   TutorialDialogPanel (UI): Hiển thị nội dung hội thoại lên màn hình cho người chơi.

---

## 2. Quy Trình Hoạt Động (Workflow)

### Bước 1: Khởi tạo
Khi Scene Tutorial bắt đầu, `TutorialController` sẽ:
1. Load danh sách các `TutorialScriptSO` được cấu hình.
2. Thiết lập các listener thông qua `TutorialConditionChecker` để theo dõi các đối tượng trong game (như TownCenter).
3. Bắt đầu script đầu tiên ở dialog đầu tiên.

### Bước 2: Hiển thị Dialog và Thực thi Action
Với mỗi dòng hội thoại (`TutorialDialogLineSO`):
1. `TutorialDialogPanel` hiển thị tên người nói và nội dung.
2. `TutorialActionExecutor` kiểm tra xem dòng này có `onStartAction` nào không (ví dụ: `SpawnCoinCloud`) và thực hiện nó ngay lập tức.

### Bước 3: Chờ đợi và Chuyển tiếp
Hệ thống sẽ chờ để chuyển sang dòng tiếp theo dựa trên `dialogType`:
*   Skipable: Người chơi chỉ cần click vào dialog để tiếp tục.
*   ExitConditional: Hệ thống sẽ "khóa" việc click. Nó chờ `TutorialConditionChecker` gửi tín hiệu rằng `conditionName` tương ứng (ví dụ: "TownCenterUpgraded") đã hoàn thành.

### Bước 4: Hoàn thành
Khi hết các dòng trong một script, Controller chuyển sang script tiếp theo. Khi hết tất cả script, một thông báo hoàn thành sẽ hiện ra và đưa người chơi về Main Menu.

---

## 3. Các Loại Action và Condition Hiện Có

### Action (Hành động khi bắt đầu):
*   `SpawnCoinCloud`: Tạo một cụm xu rơi xuống tại vị trí chỉ định.
*   `ShowArrowToTownCenter`: Hiển thị mũi tên chỉ vào Nhà Chính.
*   `PulseCoinButton`: Làm nút dùng xu nhấp nháy để thu hút sự chú ý.

### Condition (Điều kiện để vượt qua):
*   `TownCenterEntered`: Người chơi đi vào vùng của Nhà Chính.
*   `TownCenterUpgraded`: Nhà Chính đã được nâng cấp thành công.
*   `CoinSlotsShown`: Các ô nạp xu của công trình hiện ra.
*   `ToUseCoinButton`: Người chơi đã nhấn vào nút dùng xu.

---

## 4. Đánh Giá Thực Trạng (Assessment)

### Ưu điểm:
1.  Tính Modular cao: Dễ dàng bảo trì và mở rộng bằng cách thêm các Action hoặc Condition mới mà không làm ảnh hưởng đến luồng chính.
2.  Dễ sử dụng cho Game Designer: Việc tạo thêm tutorial mới chủ yếu thực hiện thông qua việc tạo và cấu hình ScriptableObject trong Unity Editor, không cần viết thêm code cho mỗi bước.
3.  Tách biệt UI và Logic: Panel hội thoại hoạt động độc lập, có thể thay đổi giao diện dễ dàng.

### Nhược điểm và Hạn chế:
1.  String-based Conditions: Việc sử dụng chuỗi văn bản (`string`) để định danh condition dễ dẫn đến sai sót (typo) khi cấu hình trong Editor.
2.  Khả năng mở rộng Action: Hiện tại các Action được xử lý bằng `switch-case` trong `TutorialActionExecutor`. Nếu số lượng action tăng lên quá nhiều, file này sẽ trở nên cồng kềnh.
3.  Phụ thuộc vào TownCenter: `TutorialConditionChecker` hiện đang code cứng việc tìm kiếm và lắng nghe `TownCenterBehavior`. Nếu tutorial cần tương tác với nhiều loại công trình khác nhau, cần refactor lại phần này theo hướng generic hơn.

---

## 5. Các File Liên Quan

**Core (Logic & Controller)**
- `Assets/Script/Tutorial/Core/TutorialController.cs`
- `Assets/Script/Tutorial/Core/TutorialActionExecutor.cs`
- `Assets/Script/Tutorial/Core/TutorialConditionChecker.cs`

**Core/Managers (Quản lý các chức năng phụ)**
- `Assets/Script/Tutorial/Core/Managers/TutorialGuideArrowController.cs`
- `Assets/Script/Tutorial/Core/Managers/TutorialCoinCloudManager.cs`
- `Assets/Script/Tutorial/Core/Managers/TutorialCoinButtonPulse.cs`

**Data (Dữ liệu & Cấu hình)**
- `Assets/Script/Tutorial/Data/TutorialScriptSO.cs`
- `Assets/Script/Tutorial/Data/TutorialDialogLineSO.cs`
- `Assets/Script/Tutorial/Data/TutorialActionType.cs`
- `Assets/Script/Tutorial/Data/TutorialDialogType.cs`

**UI (Giao diện)**
- `Assets/Script/Tutorial/UI/TutorialDialogPanel.cs`
