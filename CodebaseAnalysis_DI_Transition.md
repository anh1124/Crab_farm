# Phân tích Codebase cho việc chuyển đổi sang Dependency Injection (DI)

## 1. Danh sách Singleton hiện có

Dưới đây là các Singleton chủ chốt được tìm thấy trong codebase, cùng với các đơn vị gọi (callers).

| Tên Class | Namespace | Đường dẫn File | Callers (Các class gọi .Instance) |
| :--- | :--- | :--- | :--- |
| **PrefabData** | `Script.Data_Model` | `Assets/Script/PrefabData.cs` | ChunkManager, GuardManager, WorldFileManager, ArcherJob, WallBehavior, v.v. |
| **ArrowPool** | `Script.Game.Combat` | `Assets/Script/Game/Combat/ArrowPool.cs` | ArrowController, ArcherAttackState, StartGameScrene |
| **GuardManager** | `Script.Game.Mobs.MobsManager` | `Assets/Script/Manager/MobsManager/GuardManager.cs` | ArcherJob, GuardAlarmSystem, GuardSquadSystem, StartGameScrene |
| **WorldFileManager**| `Script.Save_Controller` | `Assets/Script/Manager/SaveManager/WorldFileManager.cs` | StartGameScrene, ChunkManager, PrefabData (Tech Sync) |
| **ChunkManager** | `Script.Manager.WorldManager` | `Assets/Script/Manager/WorldManager/ChunkManager.cs` | WorldFileManager, EnemyWaveManager, StartGameScrene |
| **EnemyPool** | `Script.Game.Enemy` | `Assets/Script/Entities/Mobs/Enemys/EnemyPool.cs` | Spawner, EnemyWaveManager, StartGameScrene |
| **TotalEnemyList** | `Script.Game.Enemy` | `Assets/Script/Entities/Mobs/Enemys/Common/TotalEnemyList.cs` | EnemyPool, StartGameScrene |
| **PlayerController**| `Script.Controller.PlayerController` | `Assets/Script/Controller/PlayerController/PlayerController.cs` | StartGameScrene, PlayerGui, BuildMenu |
| **BuildMenu** | `Script.Controller.GUI_Controller.PlayerGui` | `Assets/Script/Controller/GUI Controller/PlayerGui/BuildMenu.cs` | StartGameScrene, PlayerController |
| **EntitySaveManager**| `Script.Save_Controller` | `Assets/Script/Manager/SaveManager/EntitySaveManager.cs` | WorldFileManager, ChunkManager, StartGameScrene |

---

## 2. Bản đồ Dependency

Bảng dưới đây liệt kê các mối quan hệ phụ thuộc giữa các Manager chính.

| Manager | Cần (Depends on) | Được cần bởi (Used by) |
| :--- | :--- | :--- |
| **PrefabData** | `WorldFileManager` (Tech) | Hầu như toàn bộ hệ thống |
| **ArrowPool** | `PrefabData` (Prefab) | `ArcherAttackState`, `ArrowController` |
| **GuardManager** | `BuildingManager` (TownCenter), `PrefabData` | `ArcherJob`, `GuardAlarm`, `GuardSquad` |
| **ChunkManager** | `PrefabData`, `EntitySaveManager` | `WorldFileManager`, `EnemyWaveManager` |
| **BuildMenu** | `PlayerController`, `BuildingManager` | `PlayerGui`, `StartGameScrene` |
| **WorldFileManager**| `LoadFromSave`, `EntitySaveManager`, `ChunkManager` | `StartGameScrene`, `PrefabData` |
| **EnemyPool** | `TotalEnemyList`, `PrefabData` | `Spawner`, `EnemyWaveManager` |

> [!WARNING]
> **Dependency vòng phát hiện ⚠️:**
> `PrefabData` <-> `WorldFileManager`. 
> `PrefabData` gọi `WorldFileManager.Instance.SaveTechStatus()` để lưu công nghệ, trong khi `WorldFileManager` dùng `PrefabData` để đồng bộ dữ liệu lúc load.
> *Đề xuất:* Nên tách `TechManager` riêng hoặc dùng Interface để phá vỡ vòng lặp.

---

## 3. Phân tích StartGameScrene.cs

Tệp này đóng vai trò là **Bootstrapper** nhưng hiện tại đang gọi Singletons trực tiếp.

- **Các manager được tạo thủ công (new GameObject):** `GuardManager`, `ArrowPool`.
- **Thứ tự Phase hiện tại trong Start():**
    1.  **Phase 1 (Core Data):** Init `PrefabData`, `GuardManager` (tự tạo), `IDataListBootstrap` (TownCenterData, v.v.), `ArrowPool` (tự tạo), `EnemyBase`.
    2.  **Phase 2 (World Info):** `WorldFileManager` load data world.
    3.  **Phase 3 (UI Setup):** `SetupGUI` (Pause menu).
    4.  **Phase 4 (Save Load):** `LoadFromSave` thực hiện nạp thực thể.
    5.  **Phase 5 (Player):** Spawn player và sync camera.

- **MANAGER GIẢ ĐỊNH (Không check null mà dùng Instance):** `LoadFromSave`, `EntitySaveManager`, `PrefabData` (đôi khi).

---

## 4. Phân loại Manager theo mức độ ưu tiên

### Nhóm A — Thay đổi trước (Ít phụ thuộc, lợi ích lớn)
1.  **PrefabData**: Là nguồn gốc của mọi dữ liệu. Cần chuyển sang DI để ArcherJob, v.v. không gọi Instance.
2.  **ArrowPool / CoinPool**: Dễ dàng bọc qua interface `IObjectPool`.
3.  **TotalEnemyList**: Load dữ liệu tĩnh, không phụ thuộc gameplay lồng chéo.

### Nhóm B — Thay đổi sau (Dependency phức tạp)
1.  **GuardManager**: Liên quan chặt chẽ đến hệ thống Wall và Archer.
2.  **WorldFileManager / LoadFromSave**: Cần tái cấu trúc để không bị phụ thuộc vòng với PrefabData.
3.  **ChunkManager**: Phụ thuộc sâu vào hệ thống Save và Factory.

### Nhóm C — Giữ nguyên Singleton (Hệ thống toàn cục)
1.  **InputManager**: Hầu hết mọi nút bấm/hành động đều cần.
2.  **GameTimeManager**: Hàng trăm entity cần check ngày/đêm mỗi frame.
3.  **ScreenShakeController**: Hiệu ứng visual đơn giản.

---

## 5. Đề xuất Interface cho Nhóm A

### Manager: PrefabData
- **Callers dùng:** `GetPrefabByID()`, `player`, `arrow`, `GetTownCenterByLevel()`.
- **Interface đề xuất:**
```csharp
public interface IPrefabProvider {
    GameObject GetPrefabByID(string id);
    GameObject GetPlayerPrefab();
    GameObject GetArrowPrefab();
    GameObject GetTownCenter(int level);
}
```

### Manager: ArrowPool
- **Callers dùng:** `GetArrow()`, `ReturnArrow()`.
- **Interface đề xuất:**
```csharp
public interface IArrowPool {
    GameObject GetArrow();
    void ReturnArrow(GameObject arrow);
}
```

---

## 6. Xác định thứ tự Init hợp lý (Phase 3 mới)

Dựa trên dependency, thứ tự gọi Init() nên là:
1. `PrefabData.Init()` (Dữ liệu gốc sẵn sàng)
2. `WorldFileManager.Init()` (Xác định context thế giới/save path)
3. `TotalEnemyList.Init()` (Nạp meta-data cho quái)
4. `ArrowPool.Init()` / `EnemyPool.Init()` (Pre-warm các pool)
5. `ChunkManager.Init()` (Tạo map base)
6. `EntitySaveManager.Init()` (Chuẩn bị hệ thống save)
7. `LoadFromSave.Load()` (Điền các thực thể vào map)
8. `GuardManager.Init()` (Sau khi tường đã spawn, mới set vị trí lính)
9. `PlayerController.Init()` (Khi thế giới đã hoàn thiện)
