# 📊 BOOTSTRAP SYSTEM ANALYSIS

## Mục Đích
Phân tích các Manager/DataList để quyết định script nào nên đưa vào hệ thống `IDataListBootstrap`.

---

## 🔍 TIÊU CHÍ ĐÁNH GIÁ

Một script **NÊN** được đưa vào Bootstrap nếu:
1. ✅ Là **Singleton** (static Instance)
2. ✅ Cần khởi tạo **data từ PrefabData** hoặc Resources
3. ✅ Có **dependencies** với các Manager khác (cần thứ tự rõ ràng)
4. ✅ Được sử dụng **sớm** trong game lifecycle (Phase 1-2 của StartGameScrene)

Một script **KHÔNG NÊN** vào Bootstrap nếu:
1. ❌ Là ScriptableObject (data thuần, không cần runtime init)
2. ❌ Chỉ xử lý logic runtime (không init data)
3. ❌ Không có dependencies hoặc thứ tự init không quan trọng
4. ❌ Được init/spawn động trong game (không phải lúc start)

---

## 📋 PHÂN TÍCH CHI TIẾT

### ✅ **ĐÃ TRONG BOOTSTRAP**

#### 1. `GuardManager` ✅
- **Order:** 1 (High Priority)
- **Dependencies:** WallAndStumpManager (optional, cached in Start)
- **Init Logic:** Singleton + Cache WallAndStumpManager reference
- **Kết luận:** ✅ **ĐÚNG** - Cần init sớm cho defense system

#### 2. `TownCenterDataList` ✅
- **Order:** 110 (Low Priority)
- **Dependencies:** PrefabData.Instance.townCenterList
- **Init Logic:** Load tier data từ PrefabData
- **Kết luận:** ✅ **ĐÚNG** - Cần data từ PrefabData

---

### 🟡 **CẦN CÂN NHẮC**

#### 3. `WallAndStumpManager` 🟡
- **Dependencies:** 
  - GuardManager (optional, được cache trong Start)
  - Utility.GetTownCenter() (runtime lookup)
- **Init Logic:** 
  - Singleton trong Awake
  - UpdateDefendableWalls trong Start
- **Được dùng bởi:**
  - GuardManager (Order 1)
  - WallBehavior (runtime registration)
- **Đề xuất:** ⚠️ **NÊN** đưa vào Bootstrap với **Order 0** (trước GuardManager)
- **Lý do:** 
  - GuardManager phụ thuộc vào nó
  - Cần init sớm để walls có thể register ngay khi load
  - Không có dependencies phức tạp

#### 4. `BuildingManager` 🟡
- **Dependencies:** 
  - Resources.Load (JSON config)
  - TownCenterStatus (runtime)
- **Init Logic:**
  - LoadTownCenterConfigsFromJson() trong Start
  - Load file `TownCenterBuildingLimits.json` từ Resources
- **Được dùng bởi:**
  - BuildMenu (UI)
  - WallBehavior (CanBuildingBeUpgraded)
- **Đề xuất:** 🟡 **CÓ THỂ** đưa vào Bootstrap với **Order 50**
- **Lý do:**
  - Load JSON config cần PrefabData? **KHÔNG** - chỉ cần Resources
  - Có thể init trong Start bình thường
  - **Tuy nhiên**, nếu muốn đảm bảo config ready khi UI spawn -> nên Bootstrap

#### 5. `GameTimeManager` 🟡
- **Dependencies:** 
  - LoadFromSave.Instance.worldInfo (khi load save)
  - Coroutine system
- **Init Logic:**
  - Singleton
  - Start Coroutine để tick time
- **Được dùng bởi:**
  - VagrantBehavior (subscribe hourly events)
  - ArcherBehavior (sẽ subscribe)
  - EnemyWaveManager
- **Đề xuất:** ⚠️ **NÊN** đưa vào Bootstrap với **Order 10** (sau WallAndStumpManager, trước logic managers)
- **Lý do:**
  - Nhiều systems subscribe vào time events
  - Cần đảm bảo ready trước khi entities spawn
  - **KHÔNG LÀM:** Vì nó start Coroutine, Bootstrap call trong Awake có thể gây lỗi. **Để nguyên Start()**

---

### ❌ **KHÔNG NÊN VÀO BOOTSTRAP**

#### 6. `WallDataList` ❌
- **Type:** ScriptableObject
- **Kết luận:** ❌ **KHÔNG** - Data thuần, không cần runtime init

#### 7. `BuildingPlacementManager` ❌
- **Dependencies:** Player, Camera, Input
- **Init Logic:** Runtime interaction, không init data
- **Kết luận:** ❌ **KHÔNG** - Logic runtime, không phải data init

#### 8. `DenizenManager` ❌
- **Dependencies:** VillagerManager (optional)
- **Init Logic:** Singleton only, register/unregister runtime
- **Kết luận:** ❌ **KHÔNG** - Chỉ quản lý runtime registration, không init data

#### 9. `VillagerManager` / `VagrantManager` ❌
- **Init Logic:** Runtime management
- **Kết luận:** ❌ **KHÔNG** - Sub-managers quản lý runtime entities

#### 10. `EnemyWaveManager` ❌
- **Dependencies:** GameTimeManager, EnemyData
- **Init Logic:** Runtime wave generation
- **Kết luận:** ❌ **KHÔNG** - Logic runtime, không init data từ PrefabData

#### 11. `EntitySaveManager` ❌
- **Init Logic:** Container setup trong Awake/Start
- **Kết luận:** ❌ **KHÔNG** - Đã có logic riêng, không phụ thuộc PrefabData init

#### 12. `WorldFileManager` ❌
- **Init Logic:** Load/Save world info
- **Kết luận:** ❌ **KHÔNG** - File I/O system, không phải data init từ PrefabData

#### 13. `ChunkManager` ❌
- **Init Logic:** Map generation
- **Kết luận:** ❌ **KHÔNG** - Procedural generation, không cần Bootstrap

#### 14. `TechManager` ❌
- **Init Logic:** Tech upgrades
- **Kết luận:** ❌ **KHÔNG** - Runtime management

#### 15. `GuiCanvasManager` ❌
- **Init Logic:** UI setup
- **Kết luận:** ❌ **KHÔNG** - UI system, init riêng

#### 16. `PlayerSkillManager` ❌
- **Init Logic:** Player-specific logic
- **Kết luận:** ❌ **KHÔNG** - Player component, không phải global manager

---

## 🎯 KẾT LUẬN & ĐỀ XUẤT

### Scripts NÊN đưa vào Bootstrap:

| Script | Order | Lý do |
|--------|-------|-------|
| `WallAndStumpManager` | **0** | Phải có trước GuardManager |
| `GuardManager` | **1** | Defense system core (đã có) |
| `TownCenterDataList` | **110** | Load tier data (đã có) |

### Scripts CÓ THỂ xem xét:

| Script | Order | Điều kiện |
|--------|-------|-----------|
| `BuildingManager` | **50** | Nếu muốn đảm bảo config ready sớm |

### Scripts KHÔNG NÊN:

- Tất cả các manager còn lại (runtime logic hoặc ScriptableObject)

---

## ⚠️ LƯU Ý QUAN TRỌNG

### GameTimeManager - Trường hợp đặc biệt
Mặc dù `GameTimeManager` rất quan trọng và nhiều systems phụ thuộc, **KHÔNG NÊN** đưa vào Bootstrap vì:
1. Nó start `Coroutine` trong `Start()`.
2. Bootstrap gọi `InitializeData` trong `Awake` → Coroutine chưa thể start.
3. **Giải pháp:** Giữ nguyên init trong `Awake/Start` như hiện tại.

### Thứ tự khuyến nghị:
```
Order 0:  WallAndStumpManager (Base structure management)
Order 1:  GuardManager (Defense coordination)
Order 50: BuildingManager (Optional - config loading)
Order 110: TownCenterDataList (Data list)
```

---

## 🔧 HÀNH ĐỘNG ĐỀ XUẤT

### Bước 1: Thêm WallAndStumpManager vào Bootstrap
- Implement `IDataListBootstrap`
- Set `Order = 0`
- Move `UpdateDefendableWalls()` vào `InitializeData()`

### Bước 2: (Optional) Thêm BuildingManager
- Nếu muốn đảm bảo config ready
- Set `Order = 50`

### Bước 3: Update IDataListBootstrap.cs comment
- Cập nhật danh sách registered implementations

---

**Ngày tạo:** 2026-01-29  
**Tác giả:** AI Assistant + User Review
