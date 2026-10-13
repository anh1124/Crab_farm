# Hệ Thống Tutorial - Tổng Quan và Cách Hoạt Động

## 1. Kiến Trúc

Hệ thống Tutorial **data-driven** (ScriptableObject) và **event-based** (condition string).

### Thành phần chính

- `TutorialController`: chạy lần lượt các `TutorialScriptSO` trong registry.
- `TutorialScriptSO` / `TutorialDialogLineSO`: dữ liệu hội thoại, action, điều kiện thoát.
- `TutorialActionExecutor`: map **tên chuỗi** `methodName` → hàm (dictionary, không còn enum `TutorialActionType` hay switch-case).
- `TutorialConditionChecker`: lắng nghe event thế giới, gọi `TriggerCondition(name)` khi xong bước `ExitConditional`.
- `TutorialDialogPanel`: UI hội thoại.

Controller `RequireComponent` thêm: `TutorialGuideArrowController`, `TutorialCoinCloudManager`, `TutorialUIStateManager`, `TutorialSkillController`, `TutorialUIOverrideManager`, `TutorialBuildingManager`. `TutorialRabbitManager` được inject vào executor khi init.

---

## 2. Workflow

### Khởi tạo

Khi scene tutorial chạy, `TutorialController`:

1. Load danh sách `TutorialScriptSO`.
2. `TutorialConditionChecker.SetupTownCenterListener` / `SetupCoinSlotListener` khi có TownCenter.
3. Bắt đầu script đầu, dòng dialog đầu.

### Dialog + Action

Mỗi `TutorialDialogLineSO`:

1. Panel hiện speaker + nội dung.
2. `TutorialActionExecutor.ExecuteOnStartAction` chạy mọi phần tử trong `onStartActions` (nhiều action / dòng).

Gõ đúng `methodName` đã đăng ký. Tên cũ (`SpawnCoinCloud`, `ShowArrowToTownCenter`, `PulseCoinButton`) **không còn** trong registry.

### Chuyển dòng (`TutorialDialogType`)

- `Skipable`: click dialog để next.
- `ExitConditional`: khóa click; chờ `conditionName` khớp `OnConditionMet`.

### Hoàn thành

Hết dòng → script kế. Hết registry → hoàn thành tutorial (luồng menu do Controller xử lý).

---

## 3. Action registry hiện tại

Khóa phải khớp `TutorialActionData.methodName`:

| methodName | Tham số chính | Việc làm |
|------------|---------------|----------|
| `ActiveCoinCloud` | `position`, `param` (số coin) | Spawn cụm xu |
| `ShowGuideArrowToTarget` | `stringParam` = tag Unity | Mũi tên chỉ object theo tag |
| `HideGuideArrow` | — | Tắt mọi mũi tên hướng dẫn |
| `SetUIElementActive` | `stringParam`, `boolParam` | Bật/tắt 1 UI |
| `SetAllUIActive` | `boolParam` | Bật/tắt toàn bộ UI tutorial |
| `HideAllUI` / `ShowAllUI` | — | Shortcut all false / all true |
| `HideElement` / `ShowElement` | `stringParam` | Shortcut 1 element |
| `SetSkillActive` | `boolParam` | Bật/tắt SkillManager |
| `AddSkill` | `stringParam` tên skill, `param` slot | Thêm skill |
| `RemoveSkill` | `param` slot | Gỡ skill |
| `ClearSkills` | — | Xóa hết skill |
| `AddBuildingToTutorialBuildMenu` | `stringParam` = `BuildingType` | Thêm building vào menu tutorial |
| `ClearTutorialBuildMenu` | — | Xóa menu building tutorial |
| `SpawnRabbits` | `param` số lượng | Spawn thỏ |

Không còn `TutorialCoinButtonPulse` / action `PulseCoinButton`.

---

## 4. Condition hiện có

`TutorialConditionChecker.TriggerCondition`:

| Tên | Nguồn |
|-----|--------|
| `TownCenterEntered` | Player vào vùng TownCenter |
| `TownCenterUpgraded` | TownCenter nâng cấp |
| `CoinSlotsShown` | `CoinSlotVisual.OnSlotsShown` |
| `BuildingPlacementSuccess` | `BuildingPlacementManager.OnBuildingPlaced` |
| `BuildMenuOpened` | `BuildMenu.OnBuildMenuOpened` |
| `SummonVagrantUsed` | Skill `SpawnVagrantSkill` execute |
| `RoleChangedToVillager` | `DenizenRoleController.OnRoleChanged` |
| `RoleChangedToArcher` | cùng event, role Archer |
| `BowPurchased` | Vendor thêm item `BowVendor` |

`ToUseCoinButton` **không** còn trong checker.

---

## 5. Đánh giá thực trạng

Ưu điểm: tách data/UI/logic; designer cấu hình SO; action mới = thêm 1 entry dictionary.

Hạn chế:

1. Condition và action vẫn là **string** — typo trên SO sẽ chỉ ra warning runtime.
2. ConditionChecker vẫn gắn TownCenter cụ thể cho enter/upgrade; các event khác đã generic hơn (building, skill, role, vendor).

---

## 6. File liên quan

**Core**

- `Assets/Script/Tutorial/Core/TutorialController.cs`
- `Assets/Script/Tutorial/Core/TutorialActionExecutor.cs`
- `Assets/Script/Tutorial/Core/TutorialConditionChecker.cs`
- `Assets/Script/Tutorial/Core/TownCenterTutorialTrigger.cs`

**Managers**

- `TutorialGuideArrowController.cs`
- `TutorialCoinCloudManager.cs`
- `TutorialUIStateManager.cs`
- `TutorialSkillController.cs`
- `TutorialUIOverrideManager.cs`
- `TutorialBuildingManager.cs`
- `TutorialRabbitManager.cs`

**Data**

- `TutorialScriptSO.cs`
- `TutorialDialogLineSO.cs`
- `TutorialDialogType.cs`

**UI**

- `TutorialDialogPanel.cs`
