# Kế Hoạch Refactor: Input System (Android → WebGL Ready)

> **Mục tiêu**: Thêm keyboard support cho WebGL mà **không phá vỡ** flow Android hiện tại.  
> **Nguyên tắc cốt lõi**: Chỉ thêm tầng mới (`InputManager`) + sửa tối thiểu file cũ.

---

## ⚠️ QUY TẮC BẮT BUỘC CHO AGENT

> Agent **PHẢI** đọc và tuân thủ các quy tắc này trước khi chạm vào bất kỳ file nào.

1. **KHÔNG xóa** các public method `MoveLeftDown()`, `MoveLeftUp()`, `MoveRightDown()`, `MoveRightUp()`, `UseCoinDownAction()`, `UseCoinUpAction()` trong `PlayerController.cs` — chúng vẫn cần cho flow cũ.
2. **KHÔNG thay đổi** logic `UseCoinAction()` (hold-press timer) trong `PlayerController.cs` — chỉ thêm nguồn trigger từ keyboard.
3. **KHÔNG sửa** `SkillButtonUI.cs` ngoài phần thêm key hint text field.
4. **KHÔNG sửa** `GuiCanvasManager.cs` — `PlatformUIAdapter` sẽ là script riêng biệt.
5. Mọi thay đổi trong `PlayerGui.cs` chỉ được **redirect** target call, không được thay đổi logic hay flow.
6. Script mới phải nằm trong **đúng namespace** tương ứng với vị trí file.
7. **KHÔNG dùng** `#if UNITY_WEBGL` ở bất kỳ chỗ nào ngoài `InputManager.cs` và `PlatformUIAdapter.cs`.
8. Sau mỗi step, **kiểm tra** không có compile error trước khi sang step tiếp theo.

---

## 📁 Files Liên Quan

| File | Action |
|------|--------|
| `KeyMapConfig.cs` | 🆕 Tạo mới — ScriptableObject lưu toàn bộ key binding |
| `InputManager.cs` | 🆕 Tạo mới — đọc key từ `KeyMapConfig` |
| `PlayerController.cs` | ✏️ Sửa nhẹ: `FixedUpdate` đọc từ `InputManager` |
| `PlayerGui.cs` | ✏️ Sửa nhẹ: Redirect EventTrigger target sang `InputManager` |
| `SkillButtonUI.cs` | ✏️ Thêm: field `keyHintText` + đọc label từ `KeyMapConfig` |
| `PlatformUIAdapter.cs` | 🆕 Tạo mới hoàn toàn |

---

## STEP 1A — Tạo `KeyMapConfig.cs` (ScriptableObject)

**Vị trí file**: `Assets/Script/Controller/KeyMapConfig.cs`  
**Namespace**: `Script.Controller`

### Mục đích:

Tập trung **toàn bộ key binding** vào 1 asset duy nhất. Muốn đổi phím → chỉ cần mở asset này trong Inspector, không cần đụng code.

### Yêu cầu:

- Kế thừa `ScriptableObject`.
- Dùng `[CreateAssetMenu]` để tạo asset từ menu Unity.
- Chứa tất cả `KeyCode` cho mọi action hiện tại.
- Mỗi field có `[Tooltip]` giải thích rõ.
- Có thêm `AltKey` (phím phụ) cho movement để hỗ trợ cả WASD lẫn Arrow key mà không hardcode.

```csharp
using UnityEngine;

namespace Script.Controller
{
    [CreateAssetMenu(fileName = "KeyMapConfig", menuName = "Config/KeyMap Config")]
    public class KeyMapConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Phím chính di chuyển trái")]
        public KeyCode moveLeft     = KeyCode.A;
        [Tooltip("Phím phụ di chuyển trái (Arrow)")]
        public KeyCode moveLeftAlt  = KeyCode.LeftArrow;
        [Tooltip("Phím chính di chuyển phải")]
        public KeyCode moveRight    = KeyCode.D;
        [Tooltip("Phím phụ di chuyển phải (Arrow)")]
        public KeyCode moveRightAlt = KeyCode.RightArrow;

        [Header("Actions")]
        [Tooltip("Giữ để dùng coin / nạp coin vào công trình")]
        public KeyCode useCoin = KeyCode.F;

        [Header("Skills")]
        [Tooltip("Skill slot 0")]
        public KeyCode skill1 = KeyCode.Q;
        [Tooltip("Skill slot 1")]
        public KeyCode skill2 = KeyCode.E;
        [Tooltip("Skill slot 2")]
        public KeyCode skill3 = KeyCode.R;
    }
}
```

### Sau khi tạo script:

1. Trong Unity: chuột phải vào folder `Assets/Resources/Config` → **Create → Config → KeyMap Config**.
2. Đặt tên asset là `KeyMapConfig` (tên này `InputManager` sẽ load bằng `Resources.Load`).
3. Kiểm tra default value đúng theo bảng:

| Action | Key mặc định |
|--------|-------------|
| Move Left | A / ←  |
| Move Right | D / → |
| Use Coin | F |
| Skill 1 | Q |
| Skill 2 | E |
| Skill 3 | R |

> ⚠️ **Quy tắc cho Agent**: File asset phải nằm trong thư mục `Resources` để có thể dùng `Resources.Load`. Nếu dự án chưa có folder này, tạo `Assets/Resources/Config/`.

---

## STEP 1B — Tạo `InputManager.cs` (Script mới)

**Vị trí file**: `Assets/Script/Controller/InputManager.cs`  
**Namespace**: `Script.Controller`

### Yêu cầu chức năng:

- Singleton pattern (giống `PlayerController`).
- Có field `public KeyMapConfig keyMap` — gán trong Inspector **hoặc** tự load từ `Resources` nếu null.
- Expose các **property** (không phải field) sau:
  - `bool MoveLeft` — private set
  - `bool MoveRight` — private set
  - `bool UseCoin` — private set (trạng thái held)
  - `bool UseCoinDown` — private set (fired 1 frame khi nhấn xuống)
  - `bool UseCoinUp` — private set (fired 1 frame khi nhả ra)
  - `bool Skill1`, `bool Skill2`, `bool Skill3` — private set
- Expose `public KeyMapConfig KeyMap => keyMap` để các script khác (VD: `SkillButtonUI`) đọc tên phím để hiển thị hint.
- Trong `Awake()`: nếu `keyMap == null` thì `keyMap = Resources.Load<KeyMapConfig>("Config/KeyMapConfig")`.
- Trong `Update()`:
  - **Chỉ** dùng `#if UNITY_WEBGL || UNITY_STANDALONE` để đọc keyboard.
  - Reset one-frame events (`UseCoinDown`, `UseCoinUp`, `Skill1/2/3`) về `false` ở đầu mỗi frame TRƯỚC khi check input.
  - **Đọc key từ `keyMap`**, không hardcode `KeyCode` trực tiếp.
- Các **public Set methods** (cho Mobile UI gọi vào): giữ nguyên như cũ.

### Code mẫu khung:

```csharp
using UnityEngine;

namespace Script.Controller
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Key Binding Config")]
        [Tooltip("Kéo KeyMapConfig asset vào đây. Nếu để trống sẽ tự load từ Resources/Config/KeyMapConfig")]
        public KeyMapConfig keyMap;

        // Expose để SkillButtonUI và các UI khác đọc tên phím
        public KeyMapConfig KeyMap => keyMap;

        public bool MoveLeft  { get; private set; }
        public bool MoveRight { get; private set; }

        public bool UseCoin     { get; private set; }
        public bool UseCoinDown { get; private set; }
        public bool UseCoinUp   { get; private set; }

        public bool Skill1 { get; private set; }
        public bool Skill2 { get; private set; }
        public bool Skill3 { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // Fallback: tự load nếu không được gán trong Inspector
            if (keyMap == null)
                keyMap = Resources.Load<KeyMapConfig>("Config/KeyMapConfig");

            if (keyMap == null)
                Debug.LogError("[InputManager] Không tìm thấy KeyMapConfig! Tạo asset tại Resources/Config/KeyMapConfig");
        }

        private void Update()
        {
            // Reset one-frame events mỗi đầu frame
            UseCoinDown = false;
            UseCoinUp   = false;
            Skill1 = Skill2 = Skill3 = false;

#if UNITY_WEBGL || UNITY_STANDALONE
            if (keyMap == null) return;

            MoveLeft  = Input.GetKey(keyMap.moveLeft)  || Input.GetKey(keyMap.moveLeftAlt);
            MoveRight = Input.GetKey(keyMap.moveRight) || Input.GetKey(keyMap.moveRightAlt);

            if (Input.GetKeyDown(keyMap.useCoin)) { UseCoin = true;  UseCoinDown = true; }
            if (Input.GetKeyUp(keyMap.useCoin))   { UseCoin = false; UseCoinUp   = true; }

            if (Input.GetKeyDown(keyMap.skill1)) Skill1 = true;
            if (Input.GetKeyDown(keyMap.skill2)) Skill2 = true;
            if (Input.GetKeyDown(keyMap.skill3)) Skill3 = true;
#endif
        }

        // ── Mobile Set Methods ─────────────────────────────────────────────
        public void SetMoveLeft(bool v)  => MoveLeft  = v;
        public void SetMoveRight(bool v) => MoveRight = v;

        public void SetUseCoinDown() { UseCoin = true;  UseCoinDown = true; }
        public void SetUseCoinUp()   { UseCoin = false; UseCoinUp   = true; }

        public void SetSkill(int slotIndex)
        {
            if (slotIndex == 0)      Skill1 = true;
            else if (slotIndex == 1) Skill2 = true;
            else if (slotIndex == 2) Skill3 = true;
        }
    }
}
```

> ⚠️ **Lưu ý cho Agent**: `UseCoinDown`/`UseCoinUp` từ mobile phải được reset ở frame kế tiếp. Thêm flag nội bộ `_pendingCoinReset` nếu cần để không mất event trong cùng frame.

---

## STEP 2 — Sửa `PlayerController.cs`

**Chỉ sửa 2 vùng**, không đụng gì khác.

### Vùng A — `FixedUpdate()`: Đọc direction từ `InputManager`

Thay:
```csharp
private void FixedUpdate()
{
    Movement(direction);
    UseCoinAction();
}
```

Thành:
```csharp
private void FixedUpdate()
{
    // Đọc direction từ InputManager nếu có
    if (InputManager.Instance != null)
    {
        if (InputManager.Instance.MoveLeft)       direction = -1;
        else if (InputManager.Instance.MoveRight) direction = 1;
        else if (!InputManager.Instance.MoveLeft && !InputManager.Instance.MoveRight)
        {
            // Chỉ reset direction về 0 khi không có mobile input nào đang giữ
            // (Mobile sẽ vẫn set direction qua MoveLeftDown/Up hooks)
        }
    }

    Movement(direction);
    UseCoinAction();
}
```

> ⚠️ **Vấn đề quan trọng**: Mobile dùng `MoveLeftDown()` → `direction = -1`, `MoveLeftUp()` → `direction = 0`. Keyboard dùng `GetKey` (held). Hai nguồn phải không xung đột.  
> **Giải pháp**: Trong `FixedUpdate`, **ưu tiên InputManager** nếu keyboard đang được nhấn, còn không thì để nguyên `direction` do mobile hook set.

**Cách implement đúng**:
```csharp
private void FixedUpdate()
{
    if (InputManager.Instance != null)
    {
        if (InputManager.Instance.MoveLeft)        direction = -1;
        else if (InputManager.Instance.MoveRight)  direction =  1;
        // Nếu không có key nào → không reset, để mobile hook tự quản lý direction
    }

    Movement(direction);

    // Coin: trigger từ InputManager keyboard
    if (InputManager.Instance != null)
    {
        if (InputManager.Instance.UseCoinDown) UseCoinDownAction();
        if (InputManager.Instance.UseCoinUp)   UseCoinUpAction();
    }

    UseCoinAction();
}
```

> ⚠️ Với keyboard, `coinButtonHeld` được trigger bởi `UseCoinDownAction()` (đặt `coinButtonHeld = true`). Flow giữ nguyên hoàn toàn, chỉ thêm nguồn kích hoạt.

### Vùng B — Giữ nguyên hoàn toàn

Các method sau **KHÔNG được sửa**: `MoveLeftDown()`, `MoveLeftUp()`, `MoveRightDown()`, `MoveRightUp()`, `UseCoinDownAction()`, `UseCoinUpAction()`, `UseCoinAction()`.

---

## STEP 3 — Sửa `PlayerGui.cs`

**Chỉ sửa phần `Start()`**: Redirect EventTrigger của move buttons và coin sang `InputManager` thay vì `PlayerController`.

Thay:
```csharp
AddEventTriggers(moveLeft,  PlayerController.PlayerController.Instance.MoveLeftDown,  PlayerController.PlayerController.Instance.MoveLeftUp);
AddEventTriggers(moveRight, PlayerController.PlayerController.Instance.MoveRightDown, PlayerController.PlayerController.Instance.MoveRightUp);
AddEventTriggers(useCoin,   PlayerController.PlayerController.Instance.UseCoinDownAction, PlayerController.PlayerController.Instance.UseCoinUpAction);
```

Thành:
```csharp
AddEventTriggers(moveLeft,
    () => InputManager.Instance.SetMoveLeft(true),
    () => InputManager.Instance.SetMoveLeft(false));

AddEventTriggers(moveRight,
    () => InputManager.Instance.SetMoveRight(true),
    () => InputManager.Instance.SetMoveRight(false));

AddEventTriggers(useCoin,
    () => InputManager.Instance.SetUseCoinDown(),
    () => InputManager.Instance.SetUseCoinUp());
```

Thêm using ở đầu file:
```csharp
using Script.Controller;
```

**Không sửa gì khác** trong `PlayerGui.cs`.

---

## STEP 4 — Tạo `PlatformUIAdapter.cs` (Script mới)

**Vị trí file**: `Assets/Script/Controller/GUI Controller/PlatformUIAdapter.cs`  
**Namespace**: `Script.Controller.GUI_Controller`

### Yêu cầu chức năng:

- Gắn vào cùng GameObject với `PlayerGui` hoặc root Canvas.
- References (kéo thả trong Inspector):
  - `GameObject mobileMovementControls` — container chứa nút trái/phải
  - `GameObject coinButtonObject` — nút useCoin (nếu muốn ẩn trên web)
- Trong `Start()`: dùng `#if UNITY_WEBGL || UNITY_STANDALONE` để ẩn mobile controls.
- **Không** ẩn skill buttons — skill vẫn hiện trên tất cả platform.

```csharp
namespace Script.Controller.GUI_Controller
{
    public class PlatformUIAdapter : MonoBehaviour
    {
        [Header("Mobile-Only Controls (ẩn trên WebGL/PC)")]
        public GameObject mobileMovementControls;
        public GameObject coinButtonObject;

        private void Start()
        {
#if UNITY_WEBGL || UNITY_STANDALONE
            if (mobileMovementControls != null) mobileMovementControls.SetActive(false);
            if (coinButtonObject != null)       coinButtonObject.SetActive(false);
#else
            if (mobileMovementControls != null) mobileMovementControls.SetActive(true);
            if (coinButtonObject != null)       coinButtonObject.SetActive(true);
#endif
        }
    }
}
```

---

## STEP 5 — Sửa `SkillButtonUI.cs`: Thêm Key Hint

**Chỉ thêm**, không xóa hay đổi logic cũ.

### A — Thêm field mới vào Inspector section:

```csharp
[Header("Key Hint (WebGL/PC only)")]
[SerializeField] private TMP_Text keyHintText; // Text nhỏ hiện "Q", "E", "R"
// KHÔNG có keyHintLabel string nữa — đọc từ KeyMapConfig để tự động đồng bộ khi đổi phím
```

### B — Thêm vào cuối `Init()`:

```csharp
SetupKeyHint();
```

### C — Thêm method mới `SetupKeyHint()`:

Đọc tên phím **trực tiếp từ `InputManager.Instance.KeyMap`** theo `_slotIndex`, thay vì dùng string tĩnh. Như vậy nếu sau này đổi phím trong asset, key hint tự cập nhật mà không cần sửa prefab.

```csharp
private void SetupKeyHint()
{
    if (keyHintText == null) return;

#if UNITY_WEBGL || UNITY_STANDALONE
    // Lấy KeyCode từ KeyMapConfig theo slot, convert sang tên hiển thị ngắn gọn
    var km = Script.Controller.InputManager.Instance?.KeyMap;
    if (km != null)
    {
        KeyCode key = _slotIndex switch
        {
            0 => km.skill1,
            1 => km.skill2,
            2 => km.skill3,
            _ => KeyCode.None
        };
        keyHintText.text = key != KeyCode.None ? KeyCodeToLabel(key) : "";
    }
    keyHintText.gameObject.SetActive(true);
#else
    keyHintText.gameObject.SetActive(false);
#endif
}

/// <summary>
/// Convert KeyCode sang label ngắn để hiển thị trên UI.
/// VD: KeyCode.Q → "Q", KeyCode.Alpha1 → "1"
/// </summary>
private string KeyCodeToLabel(KeyCode key)
{
    string name = key.ToString();
    // Bỏ prefix "Alpha" cho số (Alpha1 → 1)
    if (name.StartsWith("Alpha")) return name.Substring(5);
    // Các key đặc biệt có thể thêm case ở đây sau
    return name;
}
```

> ⚠️ `keyHintText` là **optional** — nếu null thì skip, không throw error.  
> ⚠️ `SetupKeyHint()` gọi trong `Init()` — đảm bảo `InputManager.Instance` đã tồn tại trước thời điểm này (Script Execution Order).

---

## STEP 6 — Setup trong Unity Editor

> Đây là hướng dẫn cho developer (không phải agent code), nhưng agent cần comment rõ trong code.

1. **Tạo folder** `Assets/Resources/Config/` nếu chưa có.
2. Chuột phải vào folder đó → **Create → Config → KeyMap Config** → đặt tên `KeyMapConfig`.
3. **Tạo GameObject** tên `InputManager` trong scene Gameplay, gắn script `InputManager.cs`.
   - Kéo asset `KeyMapConfig` vào field `keyMap` trên component. (Nếu không kéo, script sẽ tự `Resources.Load`.)
4. Trên Canvas/PlayerHUD:
   - Gắn `PlatformUIAdapter.cs`
   - Kéo container nút trái/phải vào `mobileMovementControls`
   - Kéo nút useCoin vào `coinButtonObject`
5. Trên mỗi `SkillButtonUI` prefab:
   - Thêm TMP_Text child tên `KeyHint` vào góc dưới phải của button
   - Gán vào field `keyHintText`
   - **Không cần** set label tay — tự đọc từ `KeyMapConfig`
6. **Để đổi phím**: Chỉ cần mở asset `KeyMapConfig` trong Inspector và thay `KeyCode`. Tất cả hint UI và keyboard detection tự cập nhật.

---

## Luồng Hoàn Chỉnh Sau Refactor

```
         [KeyMapConfig Asset]
               ↓ (đọc KeyCode)
[Keyboard: Input.GetKey]        [UI Button: PointerDown/Up]
        ↓                                   ↓
   InputManager.Update()        InputManager.Set...()
              ↘                ↙
           InputManager (state: MoveLeft, MoveRight, ...)
                   ↓                     ↓
     PlayerController.FixedUpdate()   SkillButtonUI.SetupKeyHint()
            reads InputManager          reads KeyMap → hiển thị "Q/E/R"
                   ↓
           Movement(direction)
           UseCoinAction()
```

---

## Checklist Xác Nhận (Agent tự kiểm tra)

- [ ] `KeyMapConfig.cs` compile không lỗi, `[CreateAssetMenu]` đúng path
- [ ] Asset `KeyMapConfig` được tạo tại `Resources/Config/KeyMapConfig`
- [ ] `InputManager.cs` compile không lỗi, singleton khởi tạo đúng
- [ ] `InputManager.Awake()` fallback `Resources.Load` nếu `keyMap` null
- [ ] `InputManager.Update()` đọc key từ `keyMap.*` — không có `KeyCode.*` hardcode nào
- [ ] `PlayerController.FixedUpdate()` đọc `InputManager.Instance` với null check
- [ ] `PlayerGui.Start()` gọi `InputManager.Instance.Set...()` thay vì `PlayerController.Instance`
- [ ] `UseCoinDownAction()` và `UseCoinUpAction()` trong `PlayerController` vẫn còn nguyên
- [ ] `PlatformUIAdapter.cs` compile không lỗi
- [ ] `SkillButtonUI.SetupKeyHint()` đọc từ `InputManager.Instance.KeyMap`, không có string label tĩnh
- [ ] `SkillButtonUI.cs` không có breaking change với prefab hiện tại (`keyHintText` nullable)
- [ ] Không có circular dependency giữa các namespace

---

## Rủi Ro Cần Lưu Ý

| Rủi ro | Cách xử lý |
|--------|-----------|
| `InputManager.Instance` null trong `FixedUpdate` | Wrap tất cả bằng `if (InputManager.Instance != null)` |
| Mobile + Keyboard cùng lúc conflict direction | Keyboard chỉ override khi key đang được nhấn, mobile hook vẫn set bình thường |
| `UseCoinDown`/`Up` bị miss (reset trước khi `FixedUpdate` đọc) | `FixedUpdate` chạy sau `Update`, đảm bảo đọc được trong cùng frame |
| `PlayerGui.Start()` chạy trước `InputManager.Awake()` | `InputManager` phải có Script Execution Order cao hơn hoặc dùng null check |
