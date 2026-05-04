# Báo cáo Hệ thống State Machine của Enemy

Hệ thống AI của Enemy được xây dựng dựa trên mô hình State Machine (Máy trạng thái) linh hoạt, cho phép tách biệt logic quyết định và logic thực thi. Dưới đây là phân tích chi tiết về cấu trúc, luồng vận hành và các tham số tùy chỉnh.

## 1. Các thành phần cốt lõi (Core Components)

Hệ thống bao gồm 4 thành phần chính tương tác chặt chẽ với nhau:

- EnemyAIController: Bộ não trung tâm, quản lý việc chuyển đổi giữa các State và điều phối vòng đời của Enemy.
- EnemyContext: Đối tượng chứa toàn bộ tham chiếu cần thiết (Transform, Animator, Status, SkillManager...) giúp các State truy cập dữ liệu nhanh chóng.
- EnemyStatus: Lưu trữ chỉ số (HP, Speed, Damage) và các cờ hiệu điều khiển hành vi như IgnoreReturnToPortal, IsSkillCasting.
- EnemyState (Base Class): Lớp cơ sở định nghĩa cấu trúc chung cho mọi trạng thái.

---

## 2. Luồng vận hành hệ thống

### Nhịp đập AI (Optimized Pulse - UpdateBehavior)
Được điều phối bởi NPCUpdateManager để tối ưu hóa hiệu suất xử lý hàng loạt, Controller thực hiện các "nhịp đập" kiểm tra điều kiện để quyết định chuyển đổi trạng thái:
1. Kiểm tra sinh tồn: Nếu IsAlive là false, chuyển ngay sang DeadState.
2. Khóa trạng thái khi thi triển (IsSkillCasting): Nếu đang trong quá trình thực thi kỹ năng, AI sẽ giữ nguyên trạng thái hiện tại để bảo đảm tính toàn vẹn của logic hành động.
3. Tấn công tập trung: Mọi hành vi công kích đều được thực hiện thông qua SkillManager. AI không tự ý gây sát thương mà chỉ đóng vai trò điều phối kỹ năng.
4. Logic thời gian:
   - Ban đêm: Ưu tiên truy tìm mục tiêu thông qua EnemyTargetManager.
   - Ban ngày: Điều hướng rút lui về HomePortal (trừ khi IgnoreReturnToPortal được kích hoạt).

### Giai đoạn Thực thi (Update)
Mỗi trạng thái sẽ tự thực hiện logic vận hành cụ thể:
- Logic di chuyển (Movement): Được kiểm duyệt qua bộ lọc 3 lớp: SkillAllowsMovement (từ kỹ năng), CanMove (từ trạng thái cơ thể) và AllowMovementInState (từ bản chất trạng thái).
- Lưu ý về CanMove: Đây là cờ hiệu trạng thái đặc biệt từ EnemyStatus (thường dùng cho mục đích kiểm thử hoặc các trạng thái khống chế cứng), không bị ghi đè bởi các logic thông thường của State để đảm bảo tính nhất quán của hệ thống.
- Diễn hoạt: Kích hoạt các Trigger Animator thông qua SetTriggerSafely để đồng bộ hóa hình ảnh và logic.

---

## 3. Danh mục các Trạng thái hệ thống

| Trạng thái      | Lớp xử lý                 | Mô tả nhiệm                                                                                                    vụ                                                                                 |
| :---            | :---                      | :---                                                                                           |
| Nghỉ ngơi       | IdleState                 | Duy trì trạng thái tĩnh khi không có mục tiêu hoặc đang chờ lệnh.                 |
| Tiếp cận        | MoveToTargetState         | Tính toán và di chuyển về phía mục tiêu ưu tiên (Town Center/Player).            |
| Công kích       | AttackState               | Thực hiện tấn công thông qua việc điều phối các kỹ năng trong SkillManager.               |
| Rút lui         | ReturnToPortalState       | Điều hướng Enemy quay trở lại Portal khi điều kiện thời gian thỏa mãn.                 |
| Hoàn tất        | DeadState                 | Xử lý diễn hoạt kết thúc, giải phóng tài nguyên và thực hiện quy trình hoàn trả về Pool. |

---

## 4. Tùy chỉnh tham số hệ thống

### IgnoreReturnToPortal (Bỏ qua cổng rút lui)
- Tác dụng: Cho phép Enemy bỏ qua logic rút lui bắt buộc vào ban ngày.
- Hành vi: Enemy duy trì trạng thái công kích mục tiêu thay vì tìm đường về Portal.
- Ứng dụng: Dành cho các đơn vị Boss, quái vật đặc biệt hoặc các đợt tấn công theo kịch bản.

### IsSkillCasting (Khóa logic khi thi triển)
- Tác dụng: Ngăn chặn AI thay đổi trạng thái trong khi một kỹ năng đang được thực hiện.
- Hành vi: Đảm bảo các chuỗi hành động và diễn hoạt của kỹ năng không bị AI ngắt quãng giữa chừng.

---

## 5. Kế thừa và mở rộng (Inheritance)

Hệ thống cho phép mở rộng linh hoạt thông qua việc kế thừa từ EnemyAIController. Ví dụ điển hình là ShieldBot:

### ShieldBotAIController
- Đặc thù: Bổ sung logic điều khiển Shock Attack và quản lý hệ thống lá chắn vật lý.
- Cơ chế ghi đè:
  - GetAttackState: Trả về ShieldBotAttackState để thực hiện các đòn đánh diện rộng đặc thù.
  - GetDeadState: Trả về ShieldBotDeadState để tích hợp các hiệu ứng âm thanh và hình ảnh kết thúc riêng biệt.
- Animation Events: Tự quản lý các sự kiện đồng bộ từ Animator (như bật/tắt Collider gây sát thương) và phản hồi ngược lại cho SkillManager.

---

## 6. Cơ chế phối hợp với SkillManager

AI và SkillManager hoạt động theo mô hình Ủy thác:
1. AI (AttackState) xác định thời điểm và mục tiêu để tấn công.
2. AI yêu cầu SkillManager thực thi kỹ năng thông qua ExecuteSkill.
3. SkillManager quản lý thời gian hồi, diễn hoạt và gây sát thương thực tế.
4. Sau khi hoàn tất, tín hiệu OnAttackAnimationComplete sẽ giải phóng cờ khóa để AI quay lại nhịp đập quyết định.

---
