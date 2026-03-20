# Plan v3 — Hướng dẫn khắc phục lỗi

> Áp dụng sau khi đã triển khai v3 thành công. Sửa theo thứ tự ưu tiên từ cao xuống thấp.

---

## Fix 1 🔴 — `ResourceStat.SetMax` không clamp `_current`

**File:** `Assets/Script/Data Model/Status/ResourceStat.cs`

**Vấn đề:** Nếu `_current = 100` và gọi `SetMax(50)`, kết quả là `_current=100, _max=50`.  
`fill.fillAmount = current / max` sẽ ra `> 1` — thanh bar tràn.

**Sửa hàm `SetMax`:**

```csharp
// TRƯỚC
public void SetMax(float max, bool refillCurrent = false)
{
    _max = max;
    if (refillCurrent) _current = max;
    OnChanged?.Invoke(_current, _max);
}

// SAU
public void SetMax(float max, bool refillCurrent = false)
{
    _max = Mathf.Max(max, 0f);                       // chặn max âm
    _current = refillCurrent
        ? _max
        : Mathf.Min(_current, _max);                 // clamp current về max mới
    OnChanged?.Invoke(_current, _max);
}
```

---

## Fix 2 🔴 — `ResourceStat` private fields không serialize

**File:** `Assets/Script/Data Model/Status/ResourceStat.cs`

**Vấn đề:** `_current` và `_max` là `private` không có `[SerializeField]` — Unity không serialize chúng.  
Domain reload trong Editor hoặc Load scene sẽ reset về `0` thay vì giữ giá trị từ `ApplyConfig`.

**Thêm `[SerializeField]` vào hai field:**

```csharp
// TRƯỚC
private float _current;
private float _max;

// SAU
[SerializeField] private float _current;
[SerializeField] private float _max;
```

> **Lưu ý:** `[SerializeField]` giúp debug trong Inspector (Read-only khi chạy)  
> và đảm bảo Unity serialize đúng qua domain reload.  
> Giá trị runtime vẫn do `ApplyConfig → SetMax()` kiểm soát — không cần đặt tay trong Inspector.

---

## Fix 3 🟡 — `ResourceBarUI` thiếu `OnDestroy`

**File:** `Assets/Script/Controller/GUI Controller/PlayerGui/ResourceBarUI.cs`

**Vấn đề:** Khi Player bị Destroy, `ResourceBarUI` không unsubscribe khỏi `ResourceStat` → memory leak.

**Thêm hàm `OnDestroy`:**

```csharp
private void OnDestroy()
{
    if (_resource == null) return;
    _resource.OnChanged   -= OnResourceChanged;
    _resource.OnUITrigger -= NotifyUITrigger;
}
```

> Đặt ngay bên dưới hàm `Setup()` cho dễ tìm.

---

## Fix 4 🟡 — `ResourceType` enum còn lơ lửng sau khi bỏ khỏi `ResourceBarUI`

**File:** `Assets/Script/Entities/Mobs/Player/PlayerSkillManager.cs`

**Vấn đề:** Plan v3 bỏ `ResourceType` khỏi `ResourceBarUI` nhưng `PlayerSkillManager.CanAfford`  
vẫn dùng `ResourceType.Mana` / `ResourceType.Health` — enum này cần có nhà rõ ràng.

**Quyết định:** Giữ enum trong `SkillBase.cs` (nó là skill data — hợp lý hơn UI).  
Thêm comment xác nhận vị trí:

```csharp
// Trong SkillBase.cs — ResourceType sống ở đây sau khi bỏ khỏi ResourceBarUI
public enum ResourceType { Health, Mana }
```

Không cần sửa `PlayerSkillManager` — chỉ cần đảm bảo `using` trỏ đúng namespace của `SkillBase`.

---

## Fix 5 🟡 — Thêm comment giới hạn cho `OnUITrigger`

**File:** `Assets/Script/Data Model/Status/ResourceStat.cs`

**Vấn đề:** `OnUITrigger` là UI concern nằm trong data class — dễ bị lạm dụng thêm logic game vào sau.

**Thêm comment giới hạn:**

```csharp
// TRƯỚC
public event Action OnUITrigger;

// SAU
/// Bắn để trigger hiệu ứng UI nhất thời (shake, flash).
/// Chỉ dành cho visual feedback — không đặt logic game vào handler này.
/// Nếu cần nhiều loại effect hơn, cân nhắc tách sang UIFeedbackChannel riêng.
public event Action OnUITrigger;
```

---

## Fix 6 🟢 — Bổ sung regression test vào Verification Plan

**File:** `implementation_plan_v3.md` — bảng Verification

**Thêm 2 test case vào cuối bảng:**

| Test case | Kết quả mong đợi |
|---|---|
| Player di chuyển và tấn công sau refactor | Không có `NullReferenceException` trong Console |
| Load scene mới, vào Play Mode | HP và Mana bar hiển thị đúng giá trị từ `ApplyConfig`, không về `0` |

---

## Checklist hoàn thành

- [ ] Fix 1 — `SetMax` clamp `_current`
- [ ] Fix 2 — `[SerializeField]` trên `_current`, `_max`
- [ ] Fix 3 — Thêm `OnDestroy` vào `ResourceBarUI`
- [ ] Fix 4 — Xác nhận `ResourceType` enum nằm trong `SkillBase`
- [ ] Fix 5 — Thêm comment giới hạn `OnUITrigger`
- [ ] Fix 6 — Bổ sung regression test vào verification table
- [ ] Chạy Play Mode — kiểm tra toàn bộ test case trong Verification Plan v3
