# REAL CURRENT STATUS - Before Archer State Machine Implementation

## 1. Summary
Project hiện tại **hoàn toàn chưa có** `GuardManager`. `WallAndStumpManager` đã tồn tại và có các logic cơ bản về quản lý danh sách tường (Register/Unregister/Sort), nhưng **thiếu hoàn toàn** các logic liên quan đến phòng thủ (Defendable Walls, Critical HP check, integration với Guard system). `WallBehavior` chưa có kết nối nào để báo cáo tình trạng nguy cấp (Critical HP) lên manager. `ArcherStatus` thiếu các biến quan trọng để định danh vị trí phòng thủ.

Tóm lại: Hệ thống cơ sở vật chất (Wall) đã có khung sườn, nhưng hệ thống an ninh (Guard/Defense) là con số 0.

## 2. So sánh chi tiết theo từng phần của guide

### PART 1: WallAndStumpManager bổ sung hàm hỗ trợ
- criticalHPThreshold: [Không]
- defendableLeftWalls & defendableRightWalls: [Không]
- CheckWallCriticalHP(): [Không]
- UpdateDefendableWalls(): [Không]
- GetFarthestDefendableWall(): [Không]
- GetDefendableWallBehind(): [Không] (chỉ có `GetWallBehind` cơ bản, chưa check điều kiện defendable/HP)
- IsWallDefendable(), OnWallRepaired(), debug helpers: [Không]

### PART 2: GuardManager implementation
- Singleton pattern: [Không tồn tại file]
- leftFlankArchers & rightFlankArchers: [Không tồn tại file]
- RegisterArcher() / UnregisterArcher(): [Không tồn tại file]
- RaiseAlarm() / ClearAlarm() / alarm timer: [Không tồn tại file]
- NotifyWallCritical(): [Không tồn tại file]
- Debug helpers: [Không tồn tại file]

### PART 3: Integration & WallBehavior
- WallBehavior.TakeDamage() gọi CheckWallCriticalHP(): [Không] (Chỉ trừ HP và gọi `Die()`/`Collapse()` khi <= 0)
- OnWallDestroyed() unregister & update defendable walls: [Partial] (Có gọi `UnregisterWall`, nhưng không gọi `UpdateDefendableWalls`)
- Heal() (optional): [Có] (Đã implement logic hồi máu và sửa stump, nhưng chưa notify manager để update defendable list)

### PART 4: Data Models (Phát hiện thêm)
- **ArcherStatus.cs**:
  - Thiếu biến `public bool isLeftFlank;`
  - Thiếu biến `public GameObject assignedWall;`

## 3. Những thứ ĐÃ HOÀN THÀNH tốt
- `WallAndStumpManager` đã có Singleton và logic sorting tường (xa/gần) hoạt động tốt.
- `WallBehavior` xử lý tốt việc chuyển đổi giữa Wall/Stump và logic Upgrade/Repair.
- `WallStatus` đã đầy đủ các trường HP cần thiết.
- `ArcherStatus` đã define sẵn các State cần thiết (`HoldingWall`, `RetreatingToInnerWall`).

## 4. Những thứ THIẾU NẶNG / SAI / NGUY HIỂM (đánh số mức độ 1–5)
1. **Thiếu GuardManager (Mức 5)**: Không có brain để quản lý quân lính, Archer sẽ không biết đi đâu về đâu.
2. **Thiếu Defendable Logic (Mức 5)**: `WallAndStumpManager` không biết tường nào còn thủ được, dẫn đến việc không thể chỉ định điểm rút lui (Retreat) cho lính.
3. **Thiếu Archer Flank/Queue Data (Mức 4)**: `ArcherStatus` thiếu biến quy định flank (`isLeftFlank`), không thể phân bổ quân cân bằng.
4. **Thiếu kết nối Wall -> Manager (Mức 3)**: Wall bị đánh gần vỡ nhưng không ai biết để chạy.

## 5. Priority Order MỚI (dựa trên thực tế hiện tại)
1. **Update ArcherStatus.cs**: Thêm field `isLeftFlank`, `assignedWall`.
2. **Update WallAndStumpManager.cs**: Thêm logic `defendableWalls`, `CheckWallCriticalHP`, `criticalHPThreshold`.
3. **Create GuardManager.cs**: Tạo mới hoàn toàn theo guide.
4. **Update WallBehavior.cs**: Hook vào `TakeDamage` để gọi `CheckWallCriticalHP`.
5. **Implement Archer State Machine**: (Future step - sau khi các bước trên hoàn tất).

## 6. Câu hỏi / thông tin còn thiếu tôi cần cung cấp thêm
- Guide chưa đề cập rõ việc `ArcherStatus` cần thêm field (tôi tự phát hiện ra khi đối chiếu logic `RegisterArcher`).
- Giá trị `criticalHPThreshold` nên là bao nhiêu % của MaxHP hay là số cố định? (Guide để int = 10, tôi sẽ giữ default này nếu không có chỉ đạo khác).
