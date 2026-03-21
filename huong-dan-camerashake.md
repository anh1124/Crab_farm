# Hướng dẫn tích hợp CameraShake + EnemyWindPortal

---

## 1. Tổng quan kiến trúc

Mục tiêu: Camera nghiêng nhẹ về hướng portal khi portal kích hoạt hoặc spawn kẻ địch, **không xung đột** với `CameraFollowPlayer` đang có.

| Script | Điều khiển thuộc tính nào | Ghi chú |
|---|---|---|
| `CameraFollowPlayer` (hiện tại) | `transform.position` | Giữ nguyên, không sửa |
| `CameraShake` (mới) | `transform.rotation` (chỉ Z-axis) | Tách biệt hoàn toàn |

Vì hai script dùng **hai thuộc tính khác nhau**, chúng không ghi đè lẫn nhau.

---

## 2. Script CameraShake – Những gì cần làm

### 2.1 Singleton
- Dùng pattern `public static CameraShake Instance` giống `CameraFollowPlayer`
- **Lưu ý:** Vì gắn cùng GameObject với `CameraFollowPlayer`, dùng `Destroy(this)` thay vì `Destroy(gameObject)` nếu duplicate — tránh xóa mất component kia

### 2.2 Các tham số cần khai báo (public, chỉnh được trong Inspector)
- `defaultShakeCount` — số lần rung mặc định (gợi ý: 2)
- `defaultInterval` — thời gian mỗi chu kỳ rung tính bằng giây (gợi ý: 0.2s)
- `defaultTiltAngle` — góc nghiêng tối đa tính bằng độ (gợi ý: 3–5°)

### 2.3 Hàm public chính
```
ShakeToward(Vector3 portalWorldPosition, int shakeCount, float interval, float tiltAngle)
```
- Nếu không truyền tham số → dùng giá trị default
- Tính hướng nghiêng: so sánh `portalWorldPosition.x` với `transform.position.x`
  - Portal bên phải → Z rotation âm (nghiêng phải)
  - Portal bên trái → Z rotation dương (nghiêng trái)
- Dùng **Coroutine** để chạy animation, dùng `Lerp` hoặc `SmoothDamp` cho mượt
- Luôn **stop coroutine cũ** trước khi start coroutine mới (tránh nhiều shake chồng nhau)
- Sau khi xong → reset `transform.rotation = Quaternion.identity`

### 2.4 Cấu trúc một chu kỳ rung
```
1 chu kỳ = Nghiêng về phía portal (half interval) → Trả về thẳng (half interval)
Lặp lại shakeCount lần
```

---

## 3. Gắn vào scene

- Gắn `CameraShake` vào **cùng GameObject** với `CameraFollowPlayer` (tức là Camera)
- Không cần tạo GameObject riêng

---

## 4. Tích hợp vào EnemyWindPortal – Những chỗ cần thêm

Bạn chỉ cần thêm **2 điểm gọi** trong code hiện có:

### Điểm 1: Khi portal kích hoạt lần đầu
Tìm chỗ portal được bật (hàm `Activate`, `OnEnable`, hoặc `Start`) → thêm:
```
CameraShake.Instance?.ShakeToward(transform.position, shakeCount: 3, interval: 0.15f, tiltAngle: 5f)
// Shake mạnh hơn bình thường để báo hiệu portal xuất hiện
```

### Điểm 2: Mỗi lần spawn kẻ địch
Tìm chỗ `Instantiate` hoặc hàm spawn → thêm ngay sau:
```
CameraShake.Instance?.ShakeToward(transform.position)
// Dùng giá trị default là đủ
```

### Null check
Luôn dùng `?.` hoặc check `if (CameraShake.Instance != null)` trước khi gọi — tránh lỗi nếu CameraShake chưa được gắn.

---

## 5. Tham số gợi ý theo cảm giác

| Tình huống | shakeCount | interval | tiltAngle |
|---|---|---|---|
| Portal kích hoạt | 3 | 0.15s | 5° |
| Spawn kẻ địch thường | 2 | 0.2s | 3–4° |
| Spawn boss / nhiều địch | 4 | 0.12s | 6° |

Điều chỉnh theo cảm giác thực tế khi chạy game.

---

## 6. Những điều KHÔNG nên làm

- **Không sửa `transform.position`** trong CameraShake — đó là việc của `CameraFollowPlayer`
- **Không dùng `transform.rotation` ở bất kỳ script nào khác** trên cùng Camera object
- **Không `DontDestroyOnLoad`** cho CameraShake nếu Camera cũng không dùng DontDestroyOnLoad

---

## 7. Checklist trước khi test

- [ ] `CameraShake` gắn vào Camera GameObject
- [ ] `CameraShake.Instance` không null khi chạy (kiểm tra bằng Debug.Log)
- [ ] Gọi `ShakeToward` ở đúng 2 điểm trong EnemyWindPortal
- [ ] Chạy game, đứng gần portal bên phải → camera nghiêng phải ✓
- [ ] Chạy game, đứng gần portal bên trái → camera nghiêng trái ✓
- [ ] Camera không giật hoặc nhảy vị trí sau khi shake ✓
