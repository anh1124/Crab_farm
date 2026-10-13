# Báo cáo Hệ thống State Machine: Vagrant (Người lang thang)

Vagrant là nguồn nhân lực: lang thang quanh trại, nhặt coin để trở thành Villager.

## 1. Logic vận hành

### Thu nạp (ưu tiên cao khi không Fear)

`VagrantSensorHandler` + `RayCastHelper` tìm item coin. Khóa `DenizenStatus.coinTarget` rồi vào `VagrantGetCoinState`.

### Fear

`isFearPending` / `isInDanger` → `VagrantFearState`, tạm bỏ săn coin thường. Vẫn có thể nhận coin player ném (`playerPickupAble == false`) qua `VagrantItemHandler`.

---

## 2. Trạng thái

### VagrantIdleState

Lang thang quanh `HomePos` (trại), `walkSpeed`.

### VagrantGetCoinState

Khi `coinTarget != null`: chạy tới coin. Chạm trigger → `HandleItem` đổi nghề.

### VagrantFearState

Chạy khỏi nguy hiểm; không khóa `coinTarget` thường cho đến khi an toàn.

---

## 3. Đổi nghề

1. `LootPickupTrigger` → `DenizenContext` → `VagrantItemHandler`.
2. `AddCoin(1)`, clear `coinTarget`.
3. `DenizenRoleController.ChangeRole(Villager)` **cùng frame** (không coroutine đợi 1 frame).
4. `VagrantJob` disable, `VillagerJob` enable, Animator + tag đổi sang Villager.

---

## 4. VagrantManager

- `OnEnable`: `RegisterVagrant`; `OnDisable`: unregister.
- `DenizenSchedule` giờ 6 và 18 (`OnShiftChange`) làm mới Idle.
- Số lượng trại / kingdom do `VagrantManager` quyết định ngoài Job.

---

## 5. Tham số

- `coinTarget`: coin đang nhắm.
- `HomePos`: mốc lang thang.
- `walkSpeed` / `runSpeed`: đi thường vs Fear / chạy lấy coin.
