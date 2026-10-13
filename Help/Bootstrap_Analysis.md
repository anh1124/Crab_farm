# Bootstrap System — Trạng thái hiện tại

Snapshot đối chiếu `IDataListBootstrap` với code (không còn là đề xuất chưa làm). Comment đăng ký chính thức nằm trong `Assets/Script/Interfaces/IDataListBootstrap.cs`.

---

## Tiêu chí

**Nên** Bootstrap khi: cần thứ tự rõ trước load entity, init data từ `PrefabData`/scene, bị manager khác phụ thuộc sớm.

**Không** Bootstrap khi: ScriptableObject thuần, logic runtime, coroutine/`Start()` bắt buộc, spawn động.

---

## Đã đăng ký

| Script | Order | `InitializeData` làm gì |
|--------|-------|-------------------------|
| `WallAndStumpManager` | **0** | Singleton + cache `GuardManager`. **Không** gọi `UpdateDefendableWalls()` ở đây — tường register runtime qua `RegisterEntity`. |
| `GuardManager` | **1** | Gắn sub-system `GuardAlarmSystem` / `GuardSquadSystem` / `GuardWallSystem` (RequireComponent). **Không còn singleton** `GuardManager.Instance`. |
| `TownCenterDataList` | **110** | Load tier + prefab từ `PrefabData`. |

Orchestrator: `StartGameScrene.InitializeCoreData()` — `FindObjectsOfType<MonoBehaviour>(true).OfType<IDataListBootstrap>().OrderBy(Order)`.

---

## Cố ý không vào Bootstrap

| Script | Lý do |
|--------|--------|
| `GameTimeManager` | Tick bằng coroutine / `Start()`. Nhiều hệ subscribe hourly (Vagrant 6h/18h, wall heal 12h, wave đêm). Giữ Awake/Start. |
| `BuildingManager` | Load JSON + logic runtime. Vẫn init qua `InitializeBuildingBase()`, chưa `IDataListBootstrap`. Có thể cân nhắc Order 50 nếu UI cần config trước spawn. |
| `WallDataList` | ScriptableObject. |
| `BuildingPlacementManager` | Input/placement runtime. |
| `DenizenManager`, `VagrantManager`, `ToolManager` | Đăng ký entity runtime. Không còn `VillagerManager` — tool do `ToolManager` + `DenizenManager.GetNearestIdleVillager`. |
| `EnemyWaveManager`, `EntitySaveManager`, `WorldFileManager`, `ChunkManager`, `TechManager` | Runtime / I/O / generation. |

---

## Thứ tự thực tế

```
Order 0:   WallAndStumpManager
Order 1:   GuardManager
Order 110: TownCenterDataList
```

`StartGameScrene` vẫn tạo `GuardManager` / `TechManager` nếu thiếu trong scene (không thay Bootstrap).

---

## Ghi chú

- `UpdateDefendableWalls` thuộc `GuardWallSystem`, bị **combat lock**: không đổi tường thủ khi `isEnemyAttack`.
- Thêm DataList mới: xem `Bootstrap.md` và `Thêm Bootstrap Vào DataList.md`. Order đang dùng 0–1 và 110; dải 100–199 trong guide cũ (WallDataList: 100) **không** phản ánh implementation — `WallDataList` không bootstrap.

**Cập nhật:** 2026-10-13
