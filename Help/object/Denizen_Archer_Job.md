# Báo cáo Hệ thống State Machine: Cung thủ (Archer)

Tài liệu phân tích chuyên sâu về kiến trúc điều khiển và luồng dữ liệu (Data Flow) của lớp ArcherJob. Hệ thống này được thiết kế để xử lý đồng thời các nhiệm vụ tuần tra, săn bắn và phòng thủ chiến thuật thông qua sự điều phối từ GuardManager và NPCUpdateManager.

## 1. Cấu trúc Nhịp đập Trí tuệ Nhân tạo (UpdateBehavior Pipeline)

Lớp ArcherJob kế thừa từ base DenizenJob và chịu sự điều phối trực tiếp của DenizenRoleController. Hàm UpdateBehavior là lõi điều khiển chính, xử lý logic theo một chuỗi các cổng lọc (Gates) với thứ tự ưu tiên nghiêm ngặt từ cao xuống thấp:

Cổng phục hồi Cảm biến (Watchdog Gate):
Kiểm tra thành phần CircleDetect. Nếu cảm biến bị tắt đột ngột, hệ thống tự động gọi hàm Enable() để khôi phục trạng thái nhận diện, ngăn ngừa lỗi AI bị đứng im.

Cổng Quét Mục tiêu (Manual Scan Gate):
Thay vì để cảm biến tự chạy trong FixedUpdate gây hao tổn hiệu năng, ArcherJob chủ động gọi ScanCircle() mỗi nhịp đập để thu thập danh sách thực thể xung quanh.

Cổng Khóa Kỹ năng (Skill Lock Gate):
Kiểm tra cờ isSkillCasting từ Context.Status. Nếu đang thi triển kỹ năng (ví dụ: đang giương cung), AI sẽ kết thúc sớm hàm UpdateBehavior, không đưa ra quyết định mới cho đến khi kỹ năng hoàn tất.

Cổng Chiến thuật và Thời gian (Tactical & Time Gate):
Truy vấn TimeManager để xác định chu kỳ ngày/đêm. Nếu là ban đêm, AI kiểm tra vị trí hiện tại thông qua WorldUtility.IsInKingdomSafeZone hoặc IsInPlazaZone. Nếu đứng ngoài vùng an toàn, AI ép buộc thực thi ExecuteDefensiveOrder() và chặn các bản cập nhật trạng thái khác (trừ khi đang trong quá trình ReturnToBase hoặc Retreat).

Cổng Tái đánh giá (Idle Gate):
Nếu trạng thái hiện tại là IdleState, AI gọi DecideFromIdle(). Quá trình này sẽ đánh giá lại toàn bộ môi trường (có báo động từ GuardManager hay không, là ngày hay đêm) để quyết định chuyển sang HuntingState hay các trạng thái phòng thủ.

Cổng Cập nhật Trạng thái (State Update Gate):
Nếu không bị chặn bởi các cổng trên, AI sẽ chuyển quyền điều khiển cho CurrentState.UpdateBehavior() để thực thi logic riêng của từng trạng thái.

## 2. Chi tiết Luồng Dữ liệu Trạng thái (State Data Flow)

### Trạng thái Săn bắn (ArcherHuntingState)
Phạm vi hoạt động: Sử dụng archerHuntingRange để giới hạn khoảng cách di chuyển tính từ điểm gốc của bãi săn (được GuardManager phân bổ theo cánh trái/phải).
Hành vi: Di chuyển ngẫu nhiên trong vùng săn. Nếu CircleDetectHelper quét được mục tiêu hợp lệ, AI lập tức chuyển sang AttackState.

### Trạng thái Tấn công (ArcherAttackState)
Quy trình tấn công được thiết kế để đồng bộ hóa hoàn toàn với SkillManager và hệ thống Animation, không xử lý sát thương trực tiếp:
1. Kiểm tra an toàn: Gọi hàm CanAttackFromHere(). Nếu vị trí không an toàn (ví dụ: đứng trước tường khi có địch), AI kiểm tra IsInsideWall(). Nếu đang kẹt trong tường, AI lùi về PreviousState; nếu không, AI lùi về ReturnToBaseState.
2. Kiểm tra tài nguyên: Truy vấn SkillManager.IsReady(0). Nếu kỹ năng chưa hồi xong, AI quay về IdleState.
3. Thực thi kỹ năng: Gọi SkillManager.ExecuteSkill(0). Nếu thành công, cờ isSkillCasting được bật và Animator chạy clip archerDraw.
4. Sự kiện DrawComplete: Nhận từ Animation Event, chuyển Animator sang clip archerRelease.
5. Sự kiện ArrowRelease: Nhận từ Animation Event, gọi ArcherShooting.ShootAt() để lấy mũi tên từ ArrowPool và bắn.
6. Hoàn tất: Báo cáo SkillManager.OnSkillAnimationEnd(0) để tính Cooldown, tắt cờ isSkillCasting và đưa AI về IdleState.

### Trạng thái Phòng thủ (HoldWall và Retreat)
HoldWallState: AI đọc dữ liệu Slot Offset từ GuardManager để xác định vị trí đứng chính xác sau bức tường, đảm bảo khoảng cách đều nhau (archerSlotSpacing) và không bị chồng chéo.
RetreatState: Được gọi khi tường hiện tại bị phá hủy. AI nhận tham chiếu tường phòng thủ lùi sâu hơn từ GuardManager và di chuyển đến đó.

### Trạng thái Rút lui Khẩn cấp (TownCenter Emergency)
Luồng sự kiện khi TownCenter nhận sát thương:
1. TownCenter báo cáo cho GuardManager.
2. GuardManager đặt cờ isEnemyAttack = true và kích hoạt thời gian báo động.
3. GuardManager gọi Wall.ForceFrontlineToPlaza(), xóa bỏ toàn bộ tuyến phòng thủ tường hiện tại (thiết lập safeWall = null).
4. GuardManager phát tín hiệu rút lui cho toàn bộ Squad.
5. ArcherTacticalHandler nhận lệnh. Vì safeWall = null, AI nhận định tuyến phòng thủ đã vỡ và kích hoạt lệnh tử thủ: chuyển thẳng sang ReturnToBaseState để tập trung bảo vệ TownCenter tại khu vực Plaza.

## 3. Hệ thống Cảm biến và Chọn lọc Mục tiêu (Sensors & Targeting)

Bộ chọn lọc mục tiêu (ArcherTargetSelector) xử lý danh sách thực thể từ CircleDetectHelper và phân loại theo mức độ đe dọa:
Ưu tiên 1: Thực thể có tag Enemy (quái vật tấn công trực tiếp). Hàm FindClosestTarget sẽ trả về mục tiêu gần nhất trong nhóm này.
Ưu tiên 2: Thực thể có tag EnemyPortal hoặc Animal. Đây là các mục tiêu thụ động hoặc ít đe dọa hơn. Hệ thống sẽ so sánh khoảng cách và chọn đối tượng gần nhất giữa hai loại này nếu không có Enemy nào xung quanh.

## 4. Cơ chế Vượt quyền (Override Logic)

Cờ ignoreGuardManagerCommand trong ArcherJob đóng vai trò ngắt kết nối AI khỏi chuỗi lệnh của hệ thống phòng thủ trung tâm:
- Khi cờ này được bật, ArcherTacticalHandler sẽ bỏ qua lệnh rút lui từ OnGuardManagerStatusChanged.
- Trong trường hợp ForceRetreat được gọi (tường bị phá), nếu Archer vẫn đang ở vị trí hợp lệ (CanAttackFromHere = true) và có mục tiêu, nó sẽ tiếp tục chiến đấu thay vì bỏ chạy.
- Cơ chế này thường được dùng cho các cung thủ đặc nhiệm hoặc trong quá trình debug hệ thống.

 