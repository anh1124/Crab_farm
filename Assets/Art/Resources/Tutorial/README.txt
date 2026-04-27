TUTORIAL SYSTEM - SETUP GUIDE
============================

CÁC FILE ĐÃ TẠO:
----------------

1. Data Scripts (ScriptableObjects):
   - TutorialDialogType.cs : Enum định nghĩa loại dialog
   - TutorialDialogLineSO.cs : Data cho 1 dòng dialog
   - TutorialScriptSO.cs : Toàn bộ kịch bản tutorial

2. Core Scripts:
   - TutorialConditionChecker.cs : Singleton lắng nghe events
   - TownCenterTutorialTrigger.cs : Gắn vào TownCenter, báo events
   - TutorialController.cs : Controller chính chạy tutorial

3. UI Scripts:
   - TutorialDialogPanel.cs : UI hiển thị dialog

4. Integration:
   - NewWorldTutorialPrompt.cs : Gắn vào StartMenu, hiện ConfirmPanel

5. Modified:
   - TownCenterBehavior.cs : Thêm gọi TownCenterTutorialTrigger.OnTownCenterUpgraded()


THAO TÁC TRONG UNITY:
---------------------

1. Tạo Tutorial ScriptableObject:
   - Right-click Project → Create → Tutorial → Script
   - Đặt tên: "TutorialScript_Welcome"
   - Kéo các DialogLineSO vào list

2. Tạo Dialog Line ScriptableObjects:
   - Right-click → Create → Tutorial → Dialog Line
   - Cấu hình:
     + Skipable: Click để next
     + Conditional: Điền conditionName (vd: "TownCenterEntered")

3. Setup TutorialScene:
   - Tạo scene mới: "TutorialScene"
   - Thêm empty object "TutorialController":
     + Gắn TutorialController.cs
     + Gắn TutorialConditionChecker.cs
     + Assign script SO và prefabs
   - Thêm Canvas "TutorialDialogPanel":
     + Gắn TutorialDialogPanel.cs
     + Setup UI components (panel, text, hint, indicator)
   - Add Exit Button (ẩn mặc định)

4. Setup StartMenu:
   - Tìm NewWorld button
   - Add NewWorldTutorialPrompt.cs
   - Assign ConfirmationMenu reference


FLOW CHẠY:
----------

1. Click NewWorld → ConfirmPanel hiện (max 2 lần)
2. Chọn "Có" → Load TutorialScene
3. TutorialController spawn TownCenter + coins
4. Chạy dialog:
   - Skipable: Click để next
   - Conditional: Player làm action → Event triggered → Auto next
5. Hết script → Hiện Exit button


CONDITIONS HỖ TRỢ:
------------------
- "TownCenterEntered" : Player đi vào TownCenter trigger
- "TownCenterUpgraded" : TownCenter nâng cấp thành công

Thêm conditions mới bằng cách:
1. Tạo script tương tự TownCenterTutorialTrigger
2. Gọi TutorialConditionChecker.Instance.TriggerCondition("TênCondition")
3. Điền "TênCondition" vào DialogLineSO.conditionName
