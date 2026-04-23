# Hệ Thống Audio - Tài Liệu Chi Tiết

## Tổng Quan

Hệ thống audio sử dụng kiến trúc **Registry + Poll** để tối ưu hóa performance bằng cách tránh các operation O(n) trong Update() cho distance culling. Hệ thống hỗ trợ spatial audio 2D, object pooling, demand-based loop sounds, và crossfading music.

## ⚠️ QUAN TRỌNG - YÊU CẦU BẮT BUỘC

### TẤT CẢ SFX PHẢI DÙNG AudioEmitter

**BẮT BUỘC**: Khi phát SFX (Sound Effects), **KHÔNG BAO GIỜ** gọi trực tiếp `AudioManager.PlaySFXByKey()` hoặc `AudioManager.GetAudioSourceFromPool()`.

**ĐÚNG CÁCH**:
```csharp
// ✅ Đúng: Dùng AudioEmitter
_audioEmitter.PlaySound("ShieldBot_Activate");
```

**SAI CÁCH**:
```csharp
// ❌ Sai: Không gọi trực tiếp AudioManager cho SFX
AudioManager.Instance.PlaySFXByKey("ShieldBot_Activate", transform.position);
```

### Tại Sao Phải Dùng AudioEmitter?

1. **Lifecycle Management**: AudioEmitter tự gọi `StopAll()` trong `OnDisable()` → khi object bị hủy/ẩn, âm thanh tự dừng.
2. **Auto-Cleanup**: Tự động trả pool, tránh rò rỉ (leak) AudioSource.
3. **Demand System**: Loop sounds chỉ chiếm AudioSource khi Player ở trong vùng nghe (tiết kiệm pool cực lớn).
4. **Distance Culling**: PlayerAudioZone tự động kích hoạt/hủy sound dựa trên khoảng cách.
5. **Mono Audio**: Toàn bộ âm thanh được force về mono (spatialBlend = 0) để đơn giản hóa việc quản lý.

### Khi Nào Dùng AudioManager Trực Tiếp?

Chỉ dùng `AudioManager` trực tiếp cho các âm thanh toàn cục (Global):
- **BGM (Nhạc nền)**: `AudioManager.Instance.PlayBGM(clip)`
- **ENV (Môi trường)**: `AudioManager.Instance.PlayENV(clip)`
- **UI Sound (2D)**: `AudioManager.Instance.PlayUISound(soundKey)`

**KHÔNG** dùng cho SFX không gian (enemy attack, footstep, v.v.).

---

## Cấu Trúc Hệ Thống

```
AudioManager (Singleton)
├── AudioPool (Object Pooling)
├── MusicController (Crossfade Logic)
├── VolumeSettings (PlayerPrefs + Mixer)
└── SoundRegistry (Data Asset)

AudioEmitterRegistry (Static)
└── Danh sách tất cả AudioEmitter đang active

Player (GameObject)
├── PlayerAudioZone (Culling Sensor)
└── AudioListener (Dùng AudioListenerManager để di chuyển từ Camera sang Player)

AudioEmitter (Component trên Entity)
├── Demand Queue (Cho loop sounds)
└── Active Sound Handles (Quản lý pool sources)
```

---

## Các Script Chi Tiết

### 1. AudioManager.cs
**Vị trí**: `Assets/Script/Audio/Manager/AudioManager.cs`
**Mô tả**: Trái tim của hệ thống. Quản lý pooled audio, volume, và global sounds.
- **Auto-Culling**: Tự động tính toán `cullingDistance` dựa trên `maxDistance` lớn nhất trong `SoundRegistry` cộng thêm một khoảng offset (`AudioConstants.CULLING_DISTANCE_OFFSET`).
- **2D Spatial**: Sử dụng `CalculateDistanceVolume` dựa trên trục X để phù hợp với gameplay 2D side-scrolling.

### 2. AudioEmitter.cs
**Vị trí**: `Assets/Script/Audio/Helper/AudioEmitter.cs`
**Mô tả**: Component chính để phát SFX trên các object.
- **Demand System (Mới)**: Khi gọi `PlaySound(key, loop: true)`, emitter sẽ đăng ký một "demand". Nó chỉ thực sự lấy AudioSource từ pool khi Player đi vào vùng nghe. Khi Player đi xa, nó trả AudioSource về pool nhưng vẫn nhớ demand đó để phát lại khi Player quay lại.
- **One-shot instances**: Hỗ trợ phát nhiều instance của cùng một sound key đồng thời nhờ GUID-based handles.
- **Anti-Spam**: Giới hạn tối thiểu 0.1s giữa các lần phát cùng một sound key (`MIN_PLAY_INTERVAL`).

### 3. AudioListenerManager.cs
**Vị trí**: `Assets/Script/Audio/Helper/AudioListenerManager.cs`
**Mô tả**: Quản lý vị trí của `AudioListener`.
- **Vấn đề**: Spatial audio tính dựa trên vị trí của Listener. Nếu Listener ở Camera (thường xa Player), spatial audio sẽ bị sai.
- **Giải pháp**: Di chuyển `AudioListener` component từ Camera sang Player GameObject khi Player spawn, và trả lại Camera khi Player chết.

### 4. MusicController.cs
**Vị trí**: `Assets/Script/Audio/Manager/MusicController.cs`
**Mô tả**: Sử dụng 2 `AudioSource` để thực hiện hiệu ứng **Crossfade** (mờ dần bài cũ, rõ dần bài mới) khi chuyển nhạc nền.

### 5. VolumeSettings.cs
**Vị trí**: `Assets/Script/Audio/Helper/VolumeSettings.cs`
**Mô tả**: Quản lý lưu trữ cài đặt (Master, BGM, SFX, ENV, UI) vào `PlayerPrefs`.
- **Force Mono**: `GetMonoAudio()` luôn trả về `true` - toàn bộ hệ thống dùng mono audio.
- **Volume Fields**: Giá trị volume được lưu vào `PlayerPrefs` và đồng bộ vào các fields trong `AudioManager`.

### 6. PlayerAudioZone.cs
**Vị trí**: `Assets/Script/Audio/Helper/PlayerAudioZone.cs`
**Mô tả**: Sensor trên Player, thực hiện "Poll" (kiểm tra) `AudioEmitterRegistry` mỗi 0.1s để cập nhật trạng thái `InRange` cho các emitter. Đây là cơ chế giúp hệ thống không tốn CPU cho các object ở quá xa.

### 7. AudioConstants.cs
**Vị trí**: `Assets/Script/Audio/Helper/AudioConstants.cs`
**Mô tả**: Chứa toàn bộ hằng số cấu hình hệ thống:
- `BUFFER_TIME`: 0.1s (tránh cắt tiếng đột ngột).
- `DEFAULT_POOL_SIZE`: 12.
- `MAX_POOL_SIZE`: 50.
- `CULLING_DISTANCE_OFFSET`: 35 unit.

### 8. AudioCurveCache.cs
**Vị trí**: `Assets/Script/Audio/Helper/AudioCurveCache.cs`
**Mô tả**: Tối ưu hóa hiệu năng bằng cách cache các `AnimationCurve` (rolloff curves). Tránh việc tạo mới object liên tục gây áp lực lên GC (Garbage Collector).

---

## Luồng Hoạt Động (Workflow)

### Phát Loop Sound (Ví dụ: Campfire)
1. Gắn `AudioEmitter` vào Campfire.
2. Trong code Campfire, gọi `_emitter.PlaySound("Campfire_Loop", new PlaySoundConfig(loop: true))`.
3. `AudioEmitter` lưu vào **Demand Queue** (chưa tiêu tốn AudioSource từ pool).
4. Khi Player đi lại gần (trong `cullingDistance`):
   - `PlayerAudioZone` gọi `emitter.SetInRange(true)`.
   - `AudioEmitter` kiểm tra Demand Queue → Thấy "Campfire_Loop" → Lấy AudioSource từ pool → Phát sound.
5. Khi Player đi xa:
   - `PlayerAudioZone` gọi `emitter.SetInRange(false)`.
   - `AudioEmitter` dừng sound → Trả AudioSource về pool (nhưng vẫn giữ demand).

### Phát One-shot Sound (Ví dụ: Sword Hit)
1. `_emitter.PlaySound("Sword_Hit")`.
2. Nếu emitter đang `InRange` → Lấy AudioSource và phát ngay lập tức.
3. Tự động trả về pool sau khi `clip.length + BUFFER_TIME` kết thúc.

---

## Mono Audio (Force Mono)

Toàn bộ hệ thống audio được thiết kế để chạy ở chế độ **mono** để đơn giản hóa việc quản lý:
- **Tất cả AudioSource**: `spatialBlend = 0f` (2D sound).
- **BGM, ENV, UI Sources**: Được set mono trong `AudioManager.Initialize()`.
- **Pooled SFX**: `AudioPool.CreateSource()` tạo sources với `spatialBlend = 0f`.
- **AudioEmitter**: `VolumeSettings.GetMonoAudio()` luôn trả về `true`.

---

## Best Practices
1. **Luôn dùng AudioEmitter**: Đây là tầng trung gian duy nhất xử lý đúng logic âm thanh.
2. **Key-based Sound**: Luôn cấu hình âm thanh trong `SoundRegistry` để tận dụng culling và attenuation mặc định.
3. **Loop Sound cleanup**: Luôn gọi `StopSound()` hoặc đảm bảo GameObject bị Disable/Destroy để giải phóng pool.

- **Sound không phát?**
  - Kiểm tra `SoundRegistry` đã có clip gán cho key đó chưa.
  - Kiểm tra `PlayerTransform` trong `AudioManager` đã được gán chưa (thường tự gán khi Player spawn).
  - Kiểm tra xem emitter có bị Disable không (OnDisable sẽ StopAll).
- **Sound bị cắt đột ngột?**
  - Kiểm tra `maxDistance` trong registry. Nếu Player di chuyển quá nhanh ra khỏi vùng culling, sound sẽ bị thu hồi.
- **Lỗi hết Pool?**
  - Nếu `_inUseCount` trong `AudioPool` đạt 50, kiểm tra xem có script nào đang Loop sound mà quên gọi `StopSound` khi object không dùng tới nữa không.
