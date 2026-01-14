# Bootstrap System Guide

## 🎯 Tổng Quan

Bootstrap System là framework để **quản lý initialization của data structures** trong game loading sequence. Nó cho phép các scripts tự đăng ký và được initialize theo thứ tự đúng trong Phase 1 của StartGameScrene.

## 📋 Kiến Trúc

### Interface Core: IDataListBootstrap

```csharp
public interface IDataListBootstrap
{
    int Order { get; }        // Thứ tự initialize (quan trọng!)
    void InitializeData(PrefabData context); // Method initialize chính
}
```

### Cách Hoạt Động

1. **Scripts tự đăng ký:** Implement `IDataListBootstrap`
2. **StartGameScrene auto-detect:** Tìm tất cả scripts implement interface
3. **Sắp xếp theo Order:** Đảm bảo dependencies đúng
4. **Initialize tuần tự:** Gọi `InitializeData()` với `PrefabData`

## 🏗️ Ví Dụ Thực Tế: TownCenterDataList

### 1. Implement Interface

```csharp
using Script.Data_Model.Interfaces;

public class TownCenterDataList : MonoBehaviour, IDataListBootstrap
{
    // ... class members ...

    // 📍 THỰC HIỆN INTERFACE
    public int Order => 110; // Chạy thứ 110 (sau WallDataList)

    public void InitializeData(PrefabData context)
    {
        InitializeWallData(); // Method thực sự setup data
    }
}
```

### 2. Initialize Logic

```csharp
private void InitializeWallData()
{
    if(townCentrerList == null)
        townCentrerList = new List<TownCenterData>();
    else
        townCentrerList.Clear();

    // Setup data cho từng tier từ PrefabData
    townCentrerList.Add(new TownCenterData
    {
        tier = 0,
        upgradeCost = 3,
        curentPrefab = PrefabData.Instance.townCenterList[0],
    });
    townCentrerList.Add(new TownCenterData
    {
        tier = 1,
        upgradeCost = 7,
        curentPrefab = PrefabData.Instance.townCenterList[1],
    });
    // ... tier 2, 3, 4
}
```

### 3. Sử Dung Data

```csharp
// Khi spawn town center từ save file
public TownCenterData GetByTier(int tier)
{
    var data = townCentrerList.Find(d => d.tier == tier);
    if (data == null)
    {
        Debug.LogError($"Không tìm thấy data cho tier {tier}");
        return null;
    }
    return data; // {tier, upgradeCost, curentPrefab}
}
```

## 🔄 Flow Hoạt Động

### Phase 1: StartGameScrene.InitializeCoreData()

```csharp
private void InitializeCoreData()
{
    // 1) Check PrefabData ready
    if (PrefabData.Instance == null) { /* error */ }

    // 2) 🚀 BOOTSTRAP SYSTEM ACTIVATE
    var dataLists = FindObjectsOfType<MonoBehaviour>(true)
        .OfType<IDataListBootstrap>()     // Tìm scripts đã register
        .OrderBy(x => x.Order)            // Sắp xếp theo thứ tự
        .ToList();

    Debug.Log($"Initializing {dataLists.Count} data managers...");

    foreach (var list in dataLists)
    {
        list.InitializeData(PrefabData.Instance); // Gọi initialize
        Debug.Log($"✅ {list.GetType().Name} initialized");
    }
}
```

### Khi Town Center Được Load

```csharp
// Trong EntitySaveManager.LoadAllContainer()
// Khi gặp town center với tier = 2 trong save file:

GameObject townCenterPrefab = TownCenterDataList.Instance
    .GetByTier(2)      // Lấy data từ bootstrap
    .curentPrefab;     // Dùng prefab đã setup

// Instantiate với prefab đúng cho tier 2
GameObject townCenter = Instantiate(townCenterPrefab, position, rotation);
```

## 🎯 Tại Sao Cần Bootstrap?

### Vấn Đề Nếu Không Có Bootstrap

```csharp
// ❌ Khi load save mà TownCenterDataList chưa init
TownCenterDataList.Instance.GetByTier(2) // → null!

// Game crash hoặc spawn sai prefab
GameObject townCenter = Instantiate(null, position, rotation); // 💥 ERROR
```

### Giải Pháp Với Bootstrap

```csharp
// ✅ Bootstrap đã chạy trong Phase 1
TownCenterDataList.Instance.GetByTier(2)
// → {tier: 2, upgradeCost: 10, curentPrefab: TownCenterTier2}

// Spawn thành công
GameObject townCenter = Instantiate(correctPrefab, position, rotation); // ✅
```

## 📊 So Sánh Với Hardcoded Approach

| Aspect | Bootstrap System | Hardcoded trong StartGameScrene |
|--------|------------------|----------------------------------|
| **Thêm data list mới** | Implement interface | Sửa code StartGameScrene |
| **Quản lý thứ tự** | `int Order` property | Hardcode sequence |
| **Error handling** | Centralized | Scattered |
| **Maintenance** | Easy | Hard |
| **Dependencies** | Explicit qua Order | Implicit, dễ break |

## 🚀 Cách Sử Dung Bootstrap

### Bước 1: Tạo Script

```csharp
using Script.Data_Model.Interfaces;

public class MyNewDataList : MonoBehaviour, IDataListBootstrap
{
    // Singleton pattern
    public static MyNewDataList Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
```

### Bước 2: Implement Interface

```csharp
// Xác định thứ tự (hỏi team về Order phù hợp)
public int Order => 120; // Chạy sau TownCenterDataList (110)

public void InitializeData(PrefabData context)
{
    // Setup data từ PrefabData
    LoadMyDataFromPrefabData(context);
}
```

### Bước 3: Put Vào Scene

- Drag script vào GameObject trong scene
- Bootstrap system sẽ tự động detect và gọi `InitializeData()`
- **Không cần sửa StartGameScrene!**

## 🎯 Best Practices

### Order Management
- **100-199**: Data Lists (WallDataList: 100, TownCenterDataList: 110, etc.)
- **200-299**: Manager Scripts
- **300+**: Specialized Systems

### Error Handling
```csharp
public void InitializeData(PrefabData context)
{
    try
    {
        if (context == null)
            throw new System.Exception("PrefabData context is null");

        // Initialize logic here
        LoadData(context);
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[{GetType().Name}] Initialization failed: {ex.Message}");
        throw; // Re-throw để StartGameScrene biết
    }
}
```

### Validation
```csharp
public void InitializeData(PrefabData context)
{
    // Validate dependencies
    if (context.myRequiredData == null)
    {
        Debug.LogError($"[{GetType().Name}] Required data missing in PrefabData");
        return;
    }

    // Initialize
    SetupData(context);
}
```

## 🔧 Troubleshooting

### Script Không Được Gọi
- ✅ Kiểm tra script có attach vào GameObject trong scene
- ✅ Kiểm tra implement đúng `IDataListBootstrap`
- ✅ Check console cho error messages

### Sai Thứ Tự
- ✅ Kiểm tra `Order` value
- ✅ Debug log để verify execution order
- ✅ Ask team về Order conflicts

### Data Null Sau Initialize
- ✅ Kiểm tra `PrefabData.Instance` có data cần thiết
- ✅ Validate trong `InitializeData()` method
- ✅ Check singleton pattern đúng

## 🎉 Kết Luận

Bootstrap System là framework **extensible và maintainable** để manage initialization dependencies. Nó cho phép:

- **Self-registration:** Scripts tự declare cần initialize
- **Order management:** Đảm bảo dependencies đúng
- **Centralized control:** StartGameScrene orchestrate tất cả
- **Easy extension:** Thêm data list mới không cần sửa core code

**Đây là design pattern rất hiệu quả cho game initialization!** 🚀
