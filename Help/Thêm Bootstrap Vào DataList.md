# Hướng Dẫn: Thêm Bootstrap Vào DataList

## 🎯 Mục Tiêu

Hướng dẫn chi tiết cách thêm một DataList mới vào Bootstrap System để nó được initialize tự động trong Phase 1 của StartGameScrene.

## 📋 Điều Kiện Tiên Quyết

- ✅ Hiểu Bootstrap System (đọc `Bootstrap.md`)
- ✅ Có DataList class cần thêm bootstrap
- ✅ Biết Order phù hợp (hỏi team lead)

## 🚀 Các Bước Thêm Bootstrap

### Bước 1: Implement Interface

```csharp
using Script.Data_Model.Interfaces;

public class MyNewDataList : MonoBehaviour, IDataListBootstrap
{
    // ... existing code ...
}
```

### Bước 2: Thêm Order Property

```csharp
// Xác định thứ tự chạy (hỏi team để tránh conflict)
public int Order => 120; // Ví dụ: chạy sau TownCenterDataList (110)

// Order đang dùng: WallAndStumpManager 0, GuardManager 1, TownCenterDataList 110.
// Tránh trùng các giá trị trên. WallDataList không bootstrap (ScriptableObject).
```

### Bước 3: Implement InitializeData Method

```csharp
public void InitializeData(PrefabData context)
{
    try
    {
        // Validate context
        if (context == null)
            throw new System.Exception("PrefabData context is null");

        // Initialize your data from PrefabData
        InitializeMyData(context);

        Debug.Log($"[{GetType().Name}] Bootstrap initialization completed");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[{GetType().Name}] Bootstrap initialization failed: {ex.Message}");
        throw; // Re-throw để StartGameScrene biết
    }
}
```

### Bước 4: Tạo Initialize Logic

```csharp
private void InitializeMyData(PrefabData context)
{
    // Setup singleton
    if (Instance == null)
    {
        Instance = this;
    }
    else
    {
        Destroy(gameObject);
        return;
    }

    // Initialize data structures
    if (myDataList == null)
        myDataList = new List<MyData>();

    // Load data từ PrefabData
    foreach (var prefab in context.myPrefabs)
    {
        myDataList.Add(new MyData
        {
            prefab = prefab,
            // ... other properties
        });
    }

    Debug.Log($"Initialized {myDataList.Count} items in {GetType().Name}");
}
```

### Bước 5: Put Script Vào Scene

1. Mở scene chính (GamePlayScene)
2. Tìm GameObject phù hợp (thường là Managers hoặc Data)
3. Add component `MyNewDataList`
4. **✅ Script sẽ tự động được detect và gọi `InitializeData()`**

---

## 📝 Ví Dụ Hoàn Chỉnh: EnemyDataList

### 1. Script Structure

```csharp
using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;

namespace Script.Game.Enemies
{
    public class EnemyDataList : MonoBehaviour, IDataListBootstrap
    {
        [System.Serializable]
        public class EnemyData
        {
            public int enemyType;
            public GameObject prefab;
            public int health;
            public int damage;
        }

        public static EnemyDataList Instance { get; private set; }
        public List<EnemyData> enemyList;

        // 📍 BOOTSTRAP IMPLEMENTATION
        public int Order => 130; // Chạy sau TownCenterDataList

        public void InitializeData(PrefabData context)
        {
            try
            {
                if (context == null)
                    throw new System.Exception("PrefabData context is null");

                InitializeEnemyData(context);
                Debug.Log("[EnemyDataList] Bootstrap initialization completed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EnemyDataList] Bootstrap initialization failed: {ex.Message}");
                throw;
            }
        }

        private void InitializeEnemyData(PrefabData context)
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Initialize data
            if (enemyList == null)
                enemyList = new List<EnemyData>();

            enemyList.Clear();

            // Load từ PrefabData (giả sử có context.enemyPrefabs)
            if (context.enemyPrefabs != null)
            {
                for (int i = 0; i < context.enemyPrefabs.Length; i++)
                {
                    enemyList.Add(new EnemyData
                    {
                        enemyType = i,
                        prefab = context.enemyPrefabs[i],
                        health = 100 + (i * 50), // Example health scaling
                        damage = 10 + (i * 5)    // Example damage scaling
                    });
                }
            }

            Debug.Log($"[EnemyDataList] Initialized {enemyList.Count} enemy types");
        }

        // Usage method
        public EnemyData GetEnemyData(int enemyType)
        {
            return enemyList.Find(e => e.enemyType == enemyType);
        }
    }
}
```

### 2. Add To Scene

1. Mở **GamePlayScene**
2. Tìm GameObject **"DataManagers"** (hoặc tạo mới)
3. **Add Component** → **EnemyDataList**
4. **Save Scene**

### 3. Test Bootstrap

```csharp
// Sau khi game start, EnemyDataList sẽ tự động initialize
// Trong code khác có thể sử dụng:

var enemyData = EnemyDataList.Instance.GetEnemyData(1);
GameObject enemy = Instantiate(enemyData.prefab, position, rotation);
```

---

## 🔧 Troubleshooting

### Script Không Được Gọi

**Checklist:**
- ✅ Script có attach vào scene?
- ✅ Implement đúng `IDataListBootstrap`?
- ✅ `Order` value hợp lý?
- ✅ Check console có error?

**Debug:**
```csharp
// Add debug trong InitializeData
Debug.Log($"[MyDataList] Order: {Order}, Initializing...");
```

### Sai Thứ Tự

**Symptoms:**
- Data dependencies không đúng
- Script chạy trước khi cần

**Fix:**
```csharp
// Tăng Order để chạy sau
public int Order => 150; // Thay vì 130
```

### PrefabData Null

**Symptoms:**
- `context` parameter null

**Fix:**
```csharp
// Validate trong InitializeData
if (context == null)
{
    Debug.LogError($"[{GetType().Name}] PrefabData context is null!");
    return;
}
```

### Singleton Conflict

**Symptoms:**
- Multiple instances created

**Fix:**
```csharp
// Ensure singleton in InitializeData
if (Instance == null)
{
    Instance = this;
}
else
{
    Debug.LogWarning($"[{GetType().Name}] Multiple instances detected, destroying duplicate");
    Destroy(gameObject);
    return;
}
```

---

## 🎯 Best Practices

### 1. Error Handling
```csharp
public void InitializeData(PrefabData context)
{
    try
    {
        // All initialization logic
        ValidateAndInitialize(context);
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[{GetType().Name}] Critical error: {ex.Message}");
        throw; // Fail loading if critical
    }
}
```

### 2. Validation
```csharp
private void ValidateAndInitialize(PrefabData context)
{
    // Validate context
    if (context == null) throw new System.Exception("Context null");

    // Validate required data
    if (context.myRequiredPrefabs == null)
        throw new System.Exception("Required prefabs missing");

    // Initialize
    InitializeInternal(context);
}
```

### 3. Logging
```csharp
private void InitializeInternal(PrefabData context)
{
    Debug.Log($"[{GetType().Name}] Initializing {context.someData.Length} items...");

    // ... initialization ...

    Debug.Log($"[{GetType().Name}] Successfully initialized {myList.Count} items");
}
```

### 4. Order Coordination

Thực tế hiện tại: **0** WallAndStumpManager, **1** GuardManager, **110** TownCenterDataList. List mới lấy số khác (ví dụ 120) và ghi vào comment trong `IDataListBootstrap.cs`.

---

## 🔄 Migration: Từ Manual → Bootstrap

### Nếu Đang Manual Initialize

```csharp
// OLD WAY - Manual trong StartGameScrene
private void LoadEnemyData()
{
    // Manual initialization code
    EnemyDataList.Instance.InitializeData(PrefabData.Instance);
}
```

### Chuyển Sang Bootstrap

```csharp
// NEW WAY - Implement interface
public class EnemyDataList : MonoBehaviour, IDataListBootstrap
{
    public int Order => 130;

    public void InitializeData(PrefabData context)
    {
        // Move manual code here
        InitializeMyData(context);
    }
}

// Remove manual call từ StartGameScrene
// Script tự động được gọi qua bootstrap system
```

---

## 🎉 Kết Luận

**Thêm bootstrap vào DataList:**
1. ✅ Implement `IDataListBootstrap`
2. ✅ Add `Order` property
3. ✅ Implement `InitializeData(PrefabData context)`
4. ✅ Put script vào scene
5. ✅ Test và validate

**Bootstrap system sẽ tự động detect và initialize script của bạn trong Phase 1!** 🚀
