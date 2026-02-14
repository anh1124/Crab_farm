# Arrow System - Trajectory Predictor Approach
## Simple Physics với Intelligent Angle Selection

---

## 📚 MỤC LỤC

1. [Bối cảnh và Đặt vấn đề](#bối-cảnh)
2. [Phần 1: Simplified Projectile Physics](#phần-1-simplified-physics)
3. [Phần 2: Trajectory Predictor System](#phần-2-trajectory-predictor)
4. [Phần 3: Accuracy System](#phần-3-accuracy-system)
5. [Phần 4: Arrow Rotation Physics](#phần-4-arrow-rotation)
6. [Phần 5: Performance Optimization](#phần-5-performance-optimization)
7. [Implementation Code](#implementation-code)
8. [Test Cases](#test-cases)

---

## 🎮 BỐI CẢNH

### Yêu cầu hệ thống:

**Game Design Requirements:**
- Archer bắn arrow với quỹ đạo parabol tự nhiên
- Arrow **không cần trúng 100% chính xác** target (như người thật)
- Góc bắn dao động, không lặp lại pattern
- Arrow tự động tránh tường bằng cách chọn góc thông minh
- Hệ thống accuracy để upgrade archer

**Technical Constraints:**
- Unity 2D, Rigidbody2D Dynamic
- Sprite arrow 64x64, góc 45°, pivot ở đuôi (bottom-left)
- Performance: 50+ archers, 100-200 arrows đồng thời

**Triết lý thiết kế:**

```
❌ CÁCH CŨ (Over-engineered):
"Tính toán ballistic hoàn hảo để arrow trúng 100% target"
→ Phức tạp, không tự nhiên, cần calibration

✅ CÁCH MỚI (Game-ready):
"Archer chọn góc bắn trong vùng hợp lệ, có dao động tự nhiên"
→ Đơn giản, realistic, không cần calibration
```

---

## 🎯 PHẦN 1: SIMPLIFIED PROJECTILE PHYSICS

### 🎯 BÀI TOÁN 1.1: Tại sao không dùng Force Calculation?

**Vấn đề với cách cũ:**

```csharp
// BAD: Phức tạp, cần deviation
float force = CalculateForce(distance);
float deviation = PrefabData.instance.ballisticDeviation; // Cần test thủ công
float actualForce = force + deviation;
rb.velocity = CalculateVelocity(actualForce, angle);
```

**Vấn đề:**
- Phải calibrate deviation cho từng khoảng cách
- Công thức phức tạp (sin, cos, ballistic equations)
- Không linh hoạt với terrain khác nhau
- Arrow luôn bay "hoàn hảo" → không tự nhiên

**Giải pháp đơn giản:**

```csharp
// GOOD: Đơn giản, không cần calibration
float projectileSpeed = 15f; // Cố định
Vector2 direction = GetShootDirection(angle);
rb.velocity = direction * projectileSpeed;
```

**Ưu điểm:**
- 1 biến duy nhất: `projectileSpeed`
- Không cần tính toán phức tạp
- Gravity tự nhiên tạo parabol
- Dễ tweak, dễ hiểu

---

### 🎯 BÀI TOÁN 1.2: Projectile Speed vs Force

**So sánh:**

| Approach | Physics | Tweaking | Natural? |
|----------|---------|----------|----------|
| Force-based | Realistic ballistic | Phức tạp, cần deviation | Quá hoàn hảo |
| Speed-based | Simple velocity | 1 slider | Có dao động |

**Công thức:**

```
Speed-based:
velocity = direction × speed

Direction:
direction.x = cos(angle)
direction.y = sin(angle)

Gravity tự động:
Unity physics engine áp dụng gravity mỗi frame
→ Arrow bay theo parabol
```

**Ví dụ:**

```csharp
// Archer setup
[SerializeField] private float projectileSpeed = 15f;
[SerializeField] private float gravityScale = 2f;

// Shoot
float angle = 45f; // degree
Vector2 direction = new Vector2(
    Mathf.Cos(angle * Mathf.Deg2Rad),
    Mathf.Sin(angle * Mathf.Deg2Rad)
);

arrow.rb.velocity = direction * projectileSpeed;
arrow.rb.gravityScale = gravityScale;

// Kết quả: Arrow bay theo parabol tự nhiên
```

---

### 🎯 BÀI TOÁN 1.3: Chọn góc như thế nào?

**Vấn đề cũ:**
```
Tính góc để arrow trúng đích 100%
→ Phức tạp, cần solve ballistic equation
→ Luôn trúng = không tự nhiên
```

**Giải pháp mới:**
```
Thu thập các góc KHÔNG ĐỤNG TƯỜNG
→ Chọn random 1 góc
→ Có dao động, tự nhiên
```

**Ví dụ:**

```
Target cách 10 units, có tường cao 3 units ở giữa

Test các góc: 30°, 35°, 40°, 45°, 50°, 55°, 60°

30° → Đụng tường ❌
35° → Đụng tường ❌
40° → Pass ✓
45° → Pass ✓
50° → Pass ✓
55° → Pass ✓
60° → Bay quá cao, rơi xa ❌

→ validAngles = [40°, 45°, 50°, 55°]
→ Chọn random: 50° ← Góc bắn lần này
→ Lần sau chọn: 45°
→ Lần sau nữa: 55°

→ ĐA DẠNG, KHÔNG LẶP!
```

---

## 🔮 PHẦN 2: TRAJECTORY PREDICTOR SYSTEM

### 🎯 BÀI TOÁN 2.1: Dự đoán quỹ đạo

**Mục tiêu:**
- Trước khi bắn, mô phỏng arrow sẽ bay như thế nào
- Check xem có đụng tường không
- Không thực sự spawn arrow

**Thuật toán:**

```
1. Tạo danh sách góc test: [35°, 40°, 45°, 50°, 55°]
2. Với mỗi góc:
   a. Tính velocity ban đầu
   b. Mô phỏng physics từng bước nhỏ
   c. Check collision với tường
   d. Nếu pass → thêm vào validAngles
3. Chọn random 1 góc trong validAngles
```

**Mô phỏng physics:**

```csharp
Vector2 SimulateTrajectory(Vector2 startPos, Vector2 velocity, float gravity)
{
    Vector2 pos = startPos;
    float timeStep = 0.1f; // Mô phỏng mỗi 0.1s
    int maxSteps = 50;     // Tối đa 5 giây
    
    for (int i = 0; i < maxSteps; i++)
    {
        Vector2 prevPos = pos;
        
        // Physics update
        velocity.y -= gravity * timeStep;
        pos += velocity * timeStep;
        
        // Check collision bằng Linecast
        RaycastHit2D hit = Physics2D.Linecast(prevPos, pos, wallLayer);
        
        if (hit.collider != null)
        {
            return Vector2.zero; // Đụng tường
        }
        
        // Check nếu đã bay qua target đủ xa
        if (pos.x > targetPos.x + 5f)
        {
            return pos; // Simulation complete
        }
    }
    
    return pos;
}
```

---

### 🎯 BÀI TOÁN 2.2: Thu thập góc hợp lệ

**Parameters:**

```csharp
[Header("Trajectory Settings")]
[SerializeField] private float minAngle = 30f;      // Góc tối thiểu
[SerializeField] private float maxAngle = 70f;      // Góc tối đa
[SerializeField] private float angleStep = 5f;      // Bước nhảy góc
[SerializeField] private int simulationSteps = 50;  // Số bước mô phỏng
[SerializeField] private float simulationTimeStep = 0.1f;
```

**Logic:**

```csharp
List<float> FindValidAngles(Vector2 shootPos, Vector2 targetPos)
{
    List<float> validAngles = new List<float>();
    
    // Tạo danh sách góc test
    for (float angle = minAngle; angle <= maxAngle; angle += angleStep)
    {
        // Tính velocity
        Vector2 direction = AngleToDirection(angle);
        Vector2 velocity = direction * projectileSpeed;
        
        // Mô phỏng quỹ đạo
        bool hitWall = SimulateAndCheckWall(shootPos, velocity);
        
        if (!hitWall)
        {
            validAngles.Add(angle);
        }
    }
    
    return validAngles;
}
```

**Ví dụ kết quả:**

```
Input:
- minAngle = 30°
- maxAngle = 70°
- angleStep = 5°
- Có tường cao 3 units

Test:
30° → Wall hit ❌
35° → Wall hit ❌
40° → Clear ✓
45° → Clear ✓
50° → Clear ✓
55° → Clear ✓
60° → Clear ✓
65° → Too high, miss target ❌
70° → Too high, miss target ❌

Result:
validAngles = [40°, 45°, 50°, 55°, 60°]
```

---

### 🎯 BÀI TOÁN 2.3: Chọn góc cuối cùng

**Strategy 1: Random đơn giản**

```csharp
float SelectAngle(List<float> validAngles)
{
    if (validAngles.Count == 0)
    {
        return 45f; // Fallback angle
    }
    
    int randomIndex = Random.Range(0, validAngles.Count);
    return validAngles[randomIndex];
}
```

**Strategy 2: Weighted random (theo accuracy)**

```csharp
float SelectAngleWeighted(List<float> validAngles, float accuracy)
{
    if (validAngles.Count == 0) return 45f;
    
    // accuracy = 1.0 → chọn góc tốt nhất (gần target nhất)
    // accuracy = 0.0 → random hoàn toàn
    
    if (accuracy >= 0.8f)
    {
        // Chọn trong top 30% góc tốt nhất
        int topCount = Mathf.Max(1, validAngles.Count / 3);
        int randomIndex = Random.Range(0, topCount);
        return validAngles[randomIndex]; // Giả sử đã sort
    }
    else
    {
        // Random toàn bộ
        return validAngles[Random.Range(0, validAngles.Count)];
    }
}
```

---

## 🎮 PHẦN 3: ACCURACY SYSTEM

### 🎯 BÀI TOÁN 3.1: Archer không bắn hoàn hảo

**Concept:**

```
Archer Level 1 (accuracy = 0.3):
→ Chọn random trong TẤT CẢ validAngles
→ Có thể bắn góc quá cao/thấp
→ Arrow rơi xa target

Archer Level 5 (accuracy = 0.8):
→ Chọn trong top 2-3 góc tốt nhất
→ Thường trúng gần target
→ Vẫn có dao động nhỏ
```

**Implementation:**

```csharp
[Header("Combat Stats")]
[SerializeField] private float accuracy = 0.5f; // 0.0 - 1.0

float SelectAngleByAccuracy(List<float> validAngles)
{
    if (validAngles.Count == 0) return 45f;
    
    // Sort angles by "quality" (gần optimal nhất)
    validAngles.Sort((a, b) => 
        CompareAngleQuality(a, b, targetPosition)
    );
    
    // Chọn subset dựa trên accuracy
    int selectCount = Mathf.CeilToInt(
        Mathf.Lerp(validAngles.Count, 1, accuracy)
    );
    
    selectCount = Mathf.Clamp(selectCount, 1, validAngles.Count);
    
    // Random trong subset
    int index = Random.Range(0, selectCount);
    return validAngles[index];
}
```

**Compare angle quality:**

```csharp
int CompareAngleQuality(float angleA, float angleB, Vector2 target)
{
    // Mô phỏng cả 2 góc
    Vector2 landingA = SimulateLanding(angleA);
    Vector2 landingB = SimulateLanding(angleB);
    
    // Distance đến target
    float distA = Vector2.Distance(landingA, target);
    float distB = Vector2.Distance(landingB, target);
    
    return distA.CompareTo(distB); // Sort ascending
}
```

---

### 🎯 BÀI TOÁN 3.2: Spread nhỏ (optional)

**Thêm spread nhỏ vào góc:**

```csharp
float ApplySpread(float baseAngle, float accuracy)
{
    // accuracy cao → spread thấp
    float maxSpread = Mathf.Lerp(10f, 2f, accuracy);
    
    float spread = Random.Range(-maxSpread, maxSpread);
    return baseAngle + spread;
}
```

**Ví dụ:**

```
Base angle: 45°
Accuracy 0.3 → spread ±8° → Final: 37° - 53°
Accuracy 0.9 → spread ±2.5° → Final: 42.5° - 47.5°
```

---

### 🎯 BÀI TOÁN 3.3: Upgrade system

**Archer stats:**

```csharp
[System.Serializable]
public class ArcherStats
{
    public float projectileSpeed = 15f;
    public float accuracy = 0.5f;         // Core stat
    public float attackSpeed = 1.0f;      // Attacks per second
    public int damage = 10;
}
```

**Upgrade:**

```csharp
void UpgradeAccuracy()
{
    stats.accuracy += 0.1f;
    stats.accuracy = Mathf.Clamp01(stats.accuracy);
    
    // Accuracy 1.0 = always hit target zone
}
```

---

## 🔄 PHẦN 4: ARROW ROTATION PHYSICS

### 🎯 BÀI TOÁN 4.1: Sprite và Pivot

**Sprite của bạn:**
```
64×64 pixels, góc 45°
Đuôi (pivot): bottom-left (0, 0)
Đầu: top-right (64, 64)
```

**Tại sao pivot ở đuôi?**
- Arrow xoay quanh đuôi (tự nhiên)
- Spawn tại firePoint = đuôi ở đúng vị trí
- Physics rotation đơn giản

---

### 🎯 BÀI TOÁN 4.2: Xoay theo velocity

**Vấn đề sprite 45°:**

```
Rotation = 0° trong Unity:
→ Sprite hiển thị góc 45° (do sprite vẽ chéo)

Muốn: rotation = 0° → sprite nằm NGANG
```

**Giải pháp:**

```csharp
[SerializeField] private float spriteBaseAngle = 45f;

void UpdateRotation()
{
    // Góc của velocity
    float velocityAngle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
    
    // Thêm offset cho sprite
    float visualAngle;
    
    if (rb.velocity.x >= 0)
    {
        visualAngle = velocityAngle + spriteBaseAngle;
        spriteRenderer.flipY = false;
    }
    else
    {
        visualAngle = velocityAngle - spriteBaseAngle;
        spriteRenderer.flipY = true;
    }
    
    transform.rotation = Quaternion.Euler(0, 0, visualAngle);
}
```

---

### 🎯 BÀI TOÁN 4.3: Đầu mũi tên tự động rơi xuống

**Physics tự nhiên:**

```
Frame 0 (Launch):
velocity = (10, 10) → angle = 45° → Arrow hướng lên ↗

Frame 20 (Peak):
velocity = (10, 0) → angle = 0° → Arrow nằm ngang →

Frame 40 (Fall):
velocity = (10, -10) → angle = -45° → Arrow hướng xuống ↘
```

**Code:**

```csharp
void Update()
{
    if (!isActive) return;
    
    if (rb.velocity.magnitude > 0.5f)
    {
        UpdateRotation(); // Tự động xoay theo velocity
    }
}
```

**Không cần code đặc biệt!** Gravity tự động thay đổi velocity.y → rotation tự động thay đổi.

---

## ⚡ PHẦN 5: PERFORMANCE OPTIMIZATION

### 🎯 BÀI TOÁN 5.1: Dynamic → Kinematic

**Vấn đề:**
```
200 arrows trên màn hình
150 arrows đã dừng
Nhưng vẫn có 200 Dynamic Rigidbody2D
→ Physics engine xử lý TẤT CẢ
→ Lag
```

**Giải pháp:**

```csharp
void FixedUpdate()
{
    if (!isActive) return;
    
    if (rb.velocity.magnitude < 0.5f)
    {
        DeactivatePhysics();
    }
}

void DeactivatePhysics()
{
    isActive = false;
    rb.velocity = Vector2.zero;
    rb.bodyType = RigidbodyType2D.Kinematic; // ← KEY
    
    // Physics engine bỏ qua arrow này
}
```

**Kết quả:**
```
200 arrows:
- 50 đang bay (Dynamic) → Physics engine xử lý
- 150 đã dừng (Kinematic) → Physics engine bỏ qua
→ CPU giảm 70%
```

---

### 🎯 BÀI TOÁN 5.2: Object Pooling

**Vấn đề:**
```csharp
// BAD
void Shoot()
{
    Instantiate(arrowPrefab); // Tốn CPU
    Destroy(arrow, 3f);       // Garbage Collection
}
```

**Giải pháp:**
```csharp
// GOOD
void Shoot()
{
    GameObject arrow = pool.GetArrow(); // Reuse
    // ... shoot ...
    pool.ReturnArrow(arrow); // Recycle
}
```

---

### 🎯 BÀI TOÁN 5.3: Early Exit Pattern

```csharp
void Update()
{
    // Check inactive ngay đầu
    if (!isActive) return; // ← 150/200 arrows exit tại đây
    
    // Chỉ 50 arrows chạy đến đây
    UpdateRotation();
}
```

---

## 💻 IMPLEMENTATION CODE

### Code 1: TrajectoryPredictor.cs (CORE)

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mô phỏng quỹ đạo và tìm góc bắn hợp lệ
/// </summary>
public static class TrajectoryPredictor
{
    // Settings
    private const int DEFAULT_SIMULATION_STEPS = 30;      // Giảm từ 50 → 30 (đủ cho 2.4s)
    private const float DEFAULT_TIME_STEP = 0.08f;        // Giảm từ 0.1 → 0.08
    
    /// <summary>
    /// Tìm tất cả góc không đụng tường
    /// </summary>
    public static List<float> FindValidAngles(
        Vector2 shootPos,
        Vector2 targetPos,
        float projectileSpeed,
        float gravity,
        float minAngle,
        float maxAngle,
        float angleStep,
        LayerMask wallLayer)
    {
        List<float> validAngles = new List<float>();
        
        for (float angle = minAngle; angle <= maxAngle; angle += angleStep)
        {
            if (IsAngleValid(shootPos, targetPos, angle, projectileSpeed, gravity, wallLayer))
            {
                validAngles.Add(angle);
            }
        }
        
        return validAngles;
    }
    
    /// <summary>
    /// Check 1 góc có hợp lệ không (không đụng tường)
    /// </summary>
    private static bool IsAngleValid(
        Vector2 shootPos,
        Vector2 targetPos,
        float angle,
        float speed,
        float gravity,
        LayerMask wallLayer)
    {
        // Tính velocity ban đầu
        Vector2 direction = AngleToDirection(angle, targetPos - shootPos);
        Vector2 velocity = direction * speed;
        
        // Mô phỏng quỹ đạo
        Vector2 pos = shootPos;
        
        for (int step = 0; step < DEFAULT_SIMULATION_STEPS; step++)
        {
            Vector2 prevPos = pos;
            
            // Physics update
            velocity.y -= gravity * DEFAULT_TIME_STEP;
            pos += velocity * DEFAULT_TIME_STEP;
            
            // Check collision với tường
            RaycastHit2D hit = Physics2D.Linecast(prevPos, pos, wallLayer);
            
            if (hit.collider != null)
            {
                return false; // Đụng tường
            }
            
            // Check nếu đã bay qua target đủ xa (stop simulation)
            float distanceToTarget = Vector2.Distance(pos, targetPos);
            if (pos.x > targetPos.x + 5f || distanceToTarget > 20f)
            {
                break; // Đủ xa
            }
        }
        
        return true; // Không đụng tường
    }
    
    /// <summary>
    /// Mô phỏng và trả về điểm rơi cuối cùng
    /// </summary>
    public static Vector2 SimulateLanding(
        Vector2 shootPos,
        float angle,
        float speed,
        float gravity,
        Vector2 horizontalDirection)
    {
        Vector2 direction = AngleToDirection(angle, horizontalDirection);
        Vector2 velocity = direction * speed;
        Vector2 pos = shootPos;
        
        for (int step = 0; step < DEFAULT_SIMULATION_STEPS; step++)
        {
            velocity.y -= gravity * DEFAULT_TIME_STEP;
            pos += velocity * DEFAULT_TIME_STEP;
            
            // Stop khi rơi xuống đất (giả định y = 0)
            if (pos.y <= shootPos.y - 2f)
            {
                break;
            }
        }
        
        return pos;
    }
    
    /// <summary>
    /// Convert góc thành direction vector
    /// </summary>
    private static Vector2 AngleToDirection(float angle, Vector2 horizontalDirection)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        
        Vector2 dir = new Vector2(
            Mathf.Cos(angleRad),
            Mathf.Sin(angleRad)
        );
        
        // Flip nếu bắn sang trái
        if (horizontalDirection.x < 0)
        {
            dir.x = -dir.x;
        }
        
        return dir;
    }
}
```

---

### Code 2: ArcherShooting.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ArcherShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ArrowPool arrowPool;
    
    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float gravityScale = 2f;
    
    [Header("Trajectory Settings")]
    [SerializeField] private float minAngle = 30f;
    [SerializeField] private float maxAngle = 70f;
    [SerializeField] private float angleStep = 5f;
    
    [Header("Combat Stats")]
    [SerializeField] private float accuracy = 0.5f; // 0.0 - 1.0
    
    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    /// <summary>
    /// Bắn arrow đến target
    /// </summary>
    public void ShootAt(Vector2 targetPosition)
    {
        // 1. Tính gravity thực (cho simulation)
        float actualGravity = Physics2D.gravity.y * gravityScale;
        
        // 2. Tìm các góc hợp lệ
        List<float> validAngles = TrajectoryPredictor.FindValidAngles(
            firePoint.position,
            targetPosition,
            projectileSpeed,
            actualGravity,  // Gravity value cho simulation
            minAngle,
            maxAngle,
            angleStep,
            wallLayer
        );
        
        if (validAngles.Count == 0)
        {
            Debug.LogWarning("No valid angles found! Using fallback.");
            validAngles.Add(45f);
        }
        
        // 3. Chọn góc dựa trên accuracy
        float selectedAngle = SelectAngle(validAngles, accuracy, targetPosition, actualGravity);
        
        if (showDebugInfo)
        {
            Debug.Log($"Valid angles: {string.Join(", ", validAngles)} | Selected: {selectedAngle}°");
        }
        
        // 4. Spawn arrow
        GameObject arrowObj = arrowPool.GetArrow();
        arrowObj.transform.position = firePoint.position;
        
        // 5. Calculate velocity
        Vector2 direction = CalculateDirection(selectedAngle, targetPosition);
        Vector2 velocity = direction * projectileSpeed;
        
        // 6. Initialize arrow
        ArrowController arrow = arrowObj.GetComponent<ArrowController>();
        arrow.Initialize(velocity, selectedAngle, gravityScale); // Truyền gravityScale (hệ số), KHÔNG phải actualGravity
    }
    
    /// <summary>
    /// Chọn góc dựa trên accuracy (OPTIMIZED với caching)
    /// </summary>
    private float SelectAngle(List<float> validAngles, float accuracy, Vector2 targetPos, float actualGravity)
    {
        if (validAngles.Count == 1)
        {
            return validAngles[0];
        }
        
        // OPTIMIZATION: Cache landing positions trước khi sort
        Dictionary<float, float> angleDistances = new Dictionary<float, float>();
        
        foreach (float angle in validAngles)
        {
            Vector2 landing = TrajectoryPredictor.SimulateLanding(
                firePoint.position,
                angle,
                projectileSpeed,
                actualGravity,
                targetPos - (Vector2)firePoint.position
            );
            
            float distance = Vector2.Distance(landing, targetPos);
            angleDistances[angle] = distance;
        }
        
        // Sort angles by cached distance (không simulate lại)
        validAngles.Sort((a, b) => angleDistances[a].CompareTo(angleDistances[b]));
        
        // Chọn subset dựa trên accuracy
        // accuracy = 1.0 → chọn góc tốt nhất
        // accuracy = 0.0 → random toàn bộ
        int selectCount = Mathf.CeilToInt(
            Mathf.Lerp(validAngles.Count, 1, accuracy)
        );
        
        selectCount = Mathf.Clamp(selectCount, 1, validAngles.Count);
        
        // Random trong subset
        int index = Random.Range(0, selectCount);
        return validAngles[index];
    }
    
    /// <summary>
    /// Calculate direction từ góc
    /// </summary>
    private Vector2 CalculateDirection(float angle, Vector2 targetPos)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        
        Vector2 dir = new Vector2(
            Mathf.Cos(angleRad),
            Mathf.Sin(angleRad)
        );
        
        // Flip nếu target ở bên trái
        Vector2 toTarget = targetPos - (Vector2)firePoint.position;
        if (toTarget.x < 0)
        {
            dir.x = -dir.x;
        }
        
        return dir;
    }
}
```

---

### Code 3: ArrowController.cs

```csharp
using UnityEngine;
using Script.Game.Enemy.Interfaces;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ArrowController : MonoBehaviour
{
    #region References
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private ArrowPool pool;
    #endregion
    
    #region Settings
    [Header("Damage")]
    [SerializeField] private int damage = 10;
    
    [Header("Rotation")]
    [SerializeField] private float spriteBaseAngle = 45f;
    [SerializeField] private float minVelocityToRotate = 0.5f;
    
    [Header("Stick Behavior")]
    [SerializeField] private bool enableSticking = true;
    [SerializeField] private LayerMask stickableLayers;  // Enemy, Ground layers
    
    [Header("Lifetime")]
    [SerializeField] private float stuckLifetime = 3f;  // Lifetime SAU KHI stuck
    [SerializeField] private float maxAirTime = 6f;     // Fallback nếu bay mãi không chạm
    #endregion
    
    #region State
    private bool hasDealtDamage = false;
    private bool isActive = true;
    private bool isStuck = false;
    private float lifeTimer = 0f;           // Chỉ đếm khi isStuck = true
    private float timeSinceSpawn = 0f;      // Fallback timer
    private float deactivateThreshold = 0.5f;
    #endregion
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// Initialize arrow với velocity
    /// </summary>
    public void Initialize(Vector2 velocity, float launchAngle, float gravityScaleValue)
    {
        // Reset state
        hasDealtDamage = false;
        isActive = true;
        isStuck = false;
        lifeTimer = 0f;          // KHÔNG set = 3s ở đây
        timeSinceSpawn = 0f;     // Reset spawn timer
        
        // Set physics
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = velocity;
        rb.gravityScale = gravityScaleValue; // Hệ số, không phải giá trị gravity
        rb.angularVelocity = 0f;
        
        // Set initial rotation
        SetInitialRotation(velocity);
    }
    
    void Update()
    {
        // Fallback timer: Tránh arrow bay mãi không return
        timeSinceSpawn += Time.deltaTime;
        
        if (!isStuck && timeSinceSpawn > maxAirTime)
        {
            // Arrow bay quá lâu (6s) mà không chạm gì → cleanup
            Debug.LogWarning("Arrow exceeded max air time, returning to pool");
            ReturnToPool();
            return;
        }
        
        // CHỈ countdown lifetime KHI ĐÃ STUCK
        if (isStuck)
        {
            lifeTimer -= Time.deltaTime;
            
            if (lifeTimer <= 0f)
            {
                ReturnToPool();
                return;
            }
        }
        
        // Skip rotation nếu inactive
        if (!isActive) return;
        
        // Update rotation khi đang bay
        if (rb.velocity.magnitude > minVelocityToRotate)
        {
            UpdateRotation();
        }
    }
    
    void FixedUpdate()
    {
        if (!isActive) return;
        
        if (rb.velocity.magnitude < deactivateThreshold)
        {
            DeactivatePhysics();
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        
        // Damage logic
        if (!hasDealtDamage)
        {
            TryDealDamage(collision.gameObject);
        }
        
        // Stick logic
        if (enableSticking && !isStuck)
        {
            TryStickToTarget(collision);
        }
        
        // Deactivate physics
        DeactivatePhysics();
    }
    
    /// <summary>
    /// Thử stick arrow vào target (enemy/ground)
    /// BẮT ĐẦU đếm lifetime TẠI ĐÂY
    /// </summary>
    private void TryStickToTarget(Collision2D collision)
    {
        // Check layer có stick được không
        int layer = collision.gameObject.layer;
        if (((1 << layer) & stickableLayers) == 0)
        {
            return; // Không stick vào layer này
        }
        
        isStuck = true;
        lifeTimer = stuckLifetime; // ← BẮT ĐẦU đếm 3s TẠI ĐÂY
        
        // Parent arrow vào target để follow khi target di chuyển
        transform.SetParent(collision.transform);
        
        // Giữ position tại collision point
        if (collision.contactCount > 0)
        {
            Vector2 contactPoint = collision.contacts[0].point;
            transform.position = contactPoint;
        }
    }
    
    private void SetInitialRotation(Vector2 velocity)
    {
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        
        if (velocity.x >= 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, angle + spriteBaseAngle);
            spriteRenderer.flipY = false;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, angle - spriteBaseAngle);
            spriteRenderer.flipY = true;
        }
    }
    
    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
        
        if (rb.velocity.x >= 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, angle + spriteBaseAngle);
            spriteRenderer.flipY = false;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, angle - spriteBaseAngle);
            spriteRenderer.flipY = true;
        }
    }
    
    private void DeactivatePhysics()
    {
        isActive = false;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Không cần Invoke - lifeTimer countdown trong Update sẽ tự động ReturnToPool
    }
    
    private void TryDealDamage(GameObject target)
    {
        IHealth health = target.GetComponent<IHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
    
    private void ReturnToPool()
    {
        // Cleanup parent nếu stuck
        if (isStuck && transform.parent != null)
        {
            transform.SetParent(null);
            isStuck = false;
        }
        
        if (pool != null)
        {
            // Reset state before return
            isActive = false;
            hasDealtDamage = false;
            
            // Return to pool
            gameObject.SetActive(false);
            pool.ReturnArrow(gameObject);
        }
        else
        {
            // No pool - destroy (fallback)
            Destroy(gameObject);
        }
    }
    
    public void SetPool(ArrowPool poolRef)
    {
        pool = poolRef;
    }
}
```

---

### Code 4: ArrowPool.cs (Hybrid Pool 50-250)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hybrid Arrow Pool:
/// - Preload: 50 arrows
/// - Max: 250 arrows
/// - Auto-expand khi thiếu
/// </summary>
public class ArrowPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int minPoolSize = 50;   // Preload
    [SerializeField] private int maxPoolSize = 250;  // Hard limit
    [SerializeField] private Transform poolParent;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    private int totalCreated = 0;
    
    void Awake()
    {
        // Setup pool parent
        if (poolParent == null)
        {
            poolParent = new GameObject("ArrowPool").transform;
            poolParent.SetParent(transform);
        }
        
        // Preload minimum arrows
        for (int i = 0; i < minPoolSize; i++)
        {
            CreateArrow();
        }
        
        Debug.Log($"ArrowPool initialized: {minPoolSize} arrows preloaded, max {maxPoolSize}");
    }
    
    /// <summary>
    /// Get arrow from pool (auto-expand nếu cần)
    /// </summary>
    public GameObject GetArrow()
    {
        // Nếu pool rỗng
        if (pool.Count == 0)
        {
            // Kiểm tra có thể expand không
            if (totalCreated < maxPoolSize)
            {
                CreateArrow();
                Debug.Log($"Pool expanded: {totalCreated}/{maxPoolSize}");
            }
            else
            {
                Debug.LogWarning($"Pool limit reached! {maxPoolSize} arrows active.");
                return null; // Không thể tạo thêm
            }
        }
        
        // Dequeue và activate
        GameObject arrow = pool.Dequeue();
        arrow.SetActive(true);
        return arrow;
    }
    
    /// <summary>
    /// Return arrow to pool
    /// </summary>
    public void ReturnArrow(GameObject arrow)
    {
        if (arrow == null) return;
        
        // Deactivate và return
        arrow.SetActive(false);
        arrow.transform.SetParent(poolParent);
        arrow.transform.position = Vector3.zero;
        arrow.transform.rotation = Quaternion.identity;
        
        // Enqueue lại
        pool.Enqueue(arrow);
    }
    
    /// <summary>
    /// Create new arrow instance
    /// </summary>
    private void CreateArrow()
    {
        GameObject arrow = Instantiate(arrowPrefab, poolParent);
        arrow.SetActive(false);
        
        // Set pool reference
        ArrowController controller = arrow.GetComponent<ArrowController>();
        if (controller != null)
        {
            controller.SetPool(this);
        }
        
        pool.Enqueue(arrow);
        totalCreated++;
    }
    
    /// <summary>
    /// Get pool statistics (for debugging)
    /// </summary>
    public void LogPoolStats()
    {
        int available = pool.Count;
        int active = totalCreated - available;
        Debug.Log($"Pool Stats - Total: {totalCreated}, Active: {active}, Available: {available}");
    }
    
    /// <summary>
    /// Public getters
    /// </summary>
    public int TotalCreated => totalCreated;
    public int Available => pool.Count;
    public int Active => totalCreated - pool.Count;
}
```

**Giải thích:**

**Preload 50:**
```csharp
for (int i = 0; i < minPoolSize; i++)
{
    CreateArrow(); // totalCreated++
}
// Start game → 50 arrows ready
```

**Auto-expand đến 250:**
```csharp
if (pool.Count == 0)
{
    if (totalCreated < maxPoolSize)
    {
        CreateArrow(); // Tạo thêm arrow
    }
    else
    {
        return null; // Đạt giới hạn
    }
}
```

**Lifetime 3s:**
- Arrow tự return sau 3 giây (countdown trong Update)
- ReturnArrow() → SetActive(false) → Enqueue

**Capacity calculation:**
```
40 archers × 1 arrow/s = 40 arrows/s
Lifetime 3s
Active average = 40 × 3 = 120 arrows
Max 250 → còn dư 130 arrows buffer
```

---

## 🧪 TEST CASES

### Test 1: Multiple Valid Angles

```
Setup:
- Target 10 units away
- No wall
- minAngle = 30°, maxAngle = 70°, step = 5°

Expected:
validAngles = [30°, 35°, 40°, 45°, 50°, 55°, 60°, 65°, 70°]

Shoot 10 times:
→ Góc khác nhau mỗi lần
→ Đa dạng, không lặp pattern
```

### Test 2: Wall Blocking

```
Setup:
- Wall height 3 units at x=5
- Target at x=10
- minAngle = 30°

Expected:
validAngles = [50°, 55°, 60°, ...] (các góc thấp bị loại)
```

### Test 3: Accuracy Effect

```
Archer accuracy = 0.3:
→ Chọn random trong TẤT CẢ validAngles
→ Arrow rơi xa target

Archer accuracy = 0.9:
→ Chọn trong top 10% góc tốt nhất
→ Arrow rơi gần target
```

---

### Test 4: Pool Management

```
Setup:
- minPoolSize = 50
- maxPoolSize = 250

Test A: Preload
Start game → pool.LogPoolStats()
Expected: Total=50, Active=0, Available=50

Test B: Normal usage
40 archers shoot → Wait 0.5s → pool.LogPoolStats()
Expected: Total=50, Active=40, Available=10

Test C: Auto-expand
60 archers shoot → Wait 0.5s → pool.LogPoolStats()
Expected: Total=60, Active=60, Available=0 (expanded 10 arrows)

Test D: Lifetime return
60 archers shoot → Wait 3s → pool.LogPoolStats()
Expected: Total=60, Active=0, Available=60 (all returned)

Test E: Max limit
Simulate 250 active arrows → Try GetArrow()
Expected: Returns null, Debug.LogWarning "Pool limit reached"
```

---

### Test 5: Lifetime Management (Stuck-Based)

```
Setup:
- stuckLifetime = 3f
- maxAirTime = 6f

Test A: Normal flight + stuck
Shoot arrow → Arrow bay 1.2s → Chạm ground
Expected:
- t=0s: spawn, lifeTimer = 0
- t=1.2s: chạm ground, isStuck = true, lifeTimer = 3s
- t=4.2s: lifeTimer = 0, ReturnToPool()
Total lifetime: 4.2s ✓

Test B: Multiple arrows staggered
Shoot arrows at t=0, 0.5, 1.0
Arrow 1 chạm at t=1.2 → returns at t=4.2
Arrow 2 chạm at t=1.7 → returns at t=4.7
Arrow 3 chạm at t=2.2 → returns at t=5.2
Pattern: Flight time + 3s stuck ✓

Test C: Stuck to moving enemy
Arrow hits enemy → isStuck = true → Parent to enemy
Enemy walks around for 2s → Arrow follows
Wait 1s more → Arrow returns, parent cleanup ✓

Test D: Fallback - arrow bay mãi
Shoot arrow straight up với force rất cao
Arrow bay lên không rơi xuống (bug)
Expected: At t=6s → maxAirTime exceeded → ReturnToPool()
No memory leak ✓

Test E: Bounce off wall (không stick)
Arrow hits wall (not in stickableLayers)
→ isStuck = false
→ Arrow bounces, keeps flying
→ Eventually hits ground → isStuck = true → countdown 3s
```

---

## ✅ CHECKLIST

### Phase 0: Critical Fixes
- [ ] ⚠️ Gravity: Truyền `gravityScale` (hệ số) vào arrow, KHÔNG phải `actualGravity`
- [ ] ⚠️ Simulation: Dùng `actualGravity` (value) cho TrajectoryPredictor
- [ ] ⚠️ Optimization: Cache landing positions trước khi sort
- [ ] ⚠️ Stick: Enable sticking + setup stickableLayers
- [ ] ⚠️ Lifetime: 3s countdown trong Update (không dùng Invoke)

### Phase 1: Simplified Physics
- [ ] Remove all ballistic calculation
- [ ] projectileSpeed constant (15f recommended)
- [ ] rb.velocity = direction × speed
- [ ] gravityScale = 2.0 (tweakable)
- [ ] Test: Arrow bay parabol tự nhiên

### Phase 2: Trajectory Predictor
- [ ] TrajectoryPredictor.cs created
- [ ] FindValidAngles() works
- [ ] Simulation: 30 steps × 0.08s
- [ ] Linecast wall detection
- [ ] Test: Multiple angles collected (debug log)

### Phase 3: Accuracy System
- [ ] SelectAngle() với accuracy parameter
- [ ] Cache landing positions (Dictionary)
- [ ] Sort angles by cached distance
- [ ] Weighted selection
- [ ] Test: accuracy 0.3 vs 0.9

### Phase 4: Rotation
- [ ] spriteBaseAngle = 45°
- [ ] UpdateRotation() every frame
- [ ] flipY for left direction
- [ ] Đầu arrow rơi xuống tự nhiên

### Phase 5: Stick Behavior
- [ ] enableSticking setting
- [ ] stickableLayers mask (Enemy, Ground)
- [ ] transform.SetParent on collision
- [ ] Cleanup parent before return pool
- [ ] Test: Arrow stick vào moving enemy

### Phase 6: Pool Management (Hybrid 50-250)
- [ ] minPoolSize = 50 (preload)
- [ ] maxPoolSize = 250 (hard limit)
- [ ] totalCreated tracking
- [ ] GetArrow() auto-expand logic
- [ ] GetArrow() return null khi đạt limit
- [ ] ReturnArrow() SetActive(false) + Enqueue
- [ ] Test: Pool expand từ 50 → 120 → 180
- [ ] Test: 40 archers × 3s = ~120 arrows active
- [ ] Test: Pool không vượt 250

### Phase 7: Lifetime Management (Stuck-Based)
- [ ] stuckLifetime = 3f
- [ ] maxAirTime = 6f (fallback)
- [ ] lifeTimer KHÔNG reset trong Initialize (= 0)
- [ ] timeSinceSpawn tracking trong Update
- [ ] lifeTimer chỉ countdown KHI isStuck = true
- [ ] lifeTimer = stuckLifetime KHI TryStickToTarget()
- [ ] Fallback: timeSinceSpawn > 6s → ReturnToPool
- [ ] Test: Arrow bay 1s + stuck 3s = 4s total
- [ ] Test: Fallback cleanup arrow bay mãi
- [ ] Test: isStuck = false → không countdown

### Phase 8: Optimization
- [ ] Dynamic → Kinematic when velocity < 0.5
- [ ] Early exit pattern in Update/FixedUpdate
- [ ] Test: 200 arrows, 58+ FPS stable

### Final Verification
- [ ] No gravity confusion (gravityScale vs actualGravity)
- [ ] No redundant simulations (cache works)
- [ ] Arrow sticks to targets properly
- [ ] Pool không vượt 250 arrows
- [ ] Lifetime chỉ đếm SAU KHI stuck (không đếm khi bay)
- [ ] Fallback cleanup arrows bay mãi (6s)
- [ ] Performance target met (58+ FPS)
- [ ] Code clean, no warnings

---

## 📝 TECHNICAL NOTES

### ⚠️ Quan trọng - Phân biệt Gravity

**SAI:**
```csharp
// Truyền gravity value vào gravityScale
float actualGravity = Physics2D.gravity.y * gravityScale; // -19.62
arrow.rb.gravityScale = actualGravity; // ❌ SAI
```

**ĐÚNG:**
```csharp
// Dùng gravity value cho simulation
float actualGravity = Physics2D.gravity.y * gravityScale; // -19.62
TrajectoryPredictor.FindValidAngles(..., actualGravity, ...); // ✓ ĐÚNG

// Truyền gravityScale (hệ số) vào arrow
arrow.Initialize(velocity, angle, gravityScale); // gravityScale = 2.0
arrow.rb.gravityScale = gravityScale; // ✓ ĐÚNG
```

**Tại sao:**
- `rb.gravityScale` là **hệ số nhân** với `Physics2D.gravity`
- `Physics2D.gravity.y` = -9.81 (mặc định Unity)
- `gravityScale` = 2.0 → gravity thực = -9.81 × 2.0 = -19.62

---

### 🚀 Optimization - Cache Landing Positions

**Vấn đề:**
```csharp
// Sort gọi CompareAngleQuality nhiều lần
validAngles.Sort((a, b) => {
    SimulateLanding(a);  // ← Simulate lại
    SimulateLanding(b);  // ← Simulate lại
});
// Với 8 góc, O(n log n) → ~24 comparisons → 48 simulations!
```

**Giải pháp:**
```csharp
// Cache trước khi sort
Dictionary<float, float> angleDistances = new Dictionary<float, float>();
foreach (float angle in validAngles)
{
    Vector2 landing = SimulateLanding(angle);
    angleDistances[angle] = Vector2.Distance(landing, target);
}

// Sort dựa trên cached distance
validAngles.Sort((a, b) => angleDistances[a].CompareTo(angleDistances[b]));
// 8 simulations duy nhất → giảm 6x!
```

---

### 🎯 Stick Behavior

**Setup trong Unity:**

1. **Tạo Layers:**
   - Layer 8: `Enemy`
   - Layer 9: `Ground`

2. **Arrow Inspector:**
   - Enable Sticking: ✓
   - Stickable Layers: Enemy, Ground

**Logic:**
- Arrow collision → Check layer
- Nếu là stickable layer → Parent vào target
- Arrow follow target khi target di chuyển
- Cleanup parent trước khi return pool

**Ví dụ:**
```
Enemy di chuyển (walking):
→ Arrow stuck vào enemy
→ Arrow.parent = Enemy
→ Enemy walk → Arrow follow ✓
```

---

### ⚡ Simulation Performance

**Before optimization:**
```
50 steps × 0.1s = 5 giây simulation
10 góc × 50 steps = 500 linecast
```

**After optimization:**
```
30 steps × 0.08s = 2.4 giây simulation
10 góc × 30 steps = 300 linecast
→ Giảm 40% linecast
```

**Tại sao 2.4s đủ:**
- Arrow speed = 15 units/s
- Target distance = 10-15 units
- Time to target ≈ 1-2 giây
- 2.4s buffer đủ cho arc cao

---

### 🔄 Pool Management (Hybrid 50-250)

**Chiến lược:**

```
Start game:
→ Preload 50 arrows (inactive)
→ totalCreated = 50
→ pool.Count = 50

Combat starts:
→ 40 archers bắn → pool.Count giảm
→ Sau 3s arrows return → pool.Count tăng

Pool rỗng + chưa đạt max:
→ Auto-expand (CreateArrow)
→ totalCreated++

Đạt 250 arrows:
→ GetArrow() return null
→ Archer skip shot (rare case)
```

**Capacity calculation:**

| Scenario | Arrows/s | Flight time | Stuck time | Total lifetime | Active | Max | Status |
|----------|----------|-------------|------------|----------------|--------|-----|--------|
| Normal | 40 | 1.5s | 3s | 4.5s | 180 | 250 | ✓ Safe |
| Heavy | 60 | 1.5s | 3s | 4.5s | 270 | 250 | ⚠ May hit limit |
| Extreme | 80 | 1.5s | 3s | 4.5s | 360 | 250 | ❌ Pool limit |

**Note:** Total active = Arrows/s × Total lifetime (flight + stuck)

**Reality check:**
- Flight time thực tế: 0.8-2 giây (tùy khoảng cách)
- Stuck time: 3 giây (cố định)
- Total: 3.8-5 giây
- 40 archers → ~160-200 arrows active → Vẫn safe với limit 250

---

### ⏱️ Lifetime Logic (Stuck-Based)

**❗ QUAN TRỌNG - Arrow KHÔNG đếm 3s từ lúc spawn ❗**

**Vòng đời arrow:**

```
1. Spawn
   └─ timeSinceSpawn = 0
   └─ lifeTimer = 0
   └─ isStuck = false

2. Bay trong không trung
   └─ timeSinceSpawn++ (fallback safety)
   └─ lifeTimer KHÔNG đếm (= 0)
   └─ Arrow tồn tại không giới hạn

3. Chạm ground/enemy (OnCollisionEnter2D)
   └─ isStuck = true
   └─ lifeTimer = stuckLifetime (3s)  ← BẮT ĐẦU đếm TẠI ĐÂY
   └─ Parent vào target

4. Stuck countdown
   └─ lifeTimer -= Time.deltaTime
   └─ Khi lifeTimer <= 0 → ReturnToPool()

5. Return to pool
   └─ Cleanup parent
   └─ SetActive(false)
```

**Fallback safety:**

```csharp
if (!isStuck && timeSinceSpawn > maxAirTime)
{
    // Arrow bay quá lâu (6s) không chạm gì
    // → Cleanup để tránh memory leak
    ReturnToPool();
}
```

**Tại sao cần fallback:**
- Arrow bay ra khỏi map
- Arrow bay lên cao mãi không rơi (edge case)
- Bug logic khiến arrow không collision
- maxAirTime = 6s (gấp đôi lifetime bình thường)

**Ví dụ timeline:**

```
t=0.0s: Arrow spawn, bay lên
t=0.5s: Arrow đạt đỉnh parabol
t=1.2s: Arrow chạm ground ← isStuck = true, lifeTimer = 3s
t=2.2s: lifeTimer = 2s
t=3.2s: lifeTimer = 1s
t=4.2s: lifeTimer = 0s → ReturnToPool()

Total lifetime: 4.2s (1.2s flying + 3s stuck)
```

**Edge case - Bay mãi:**

```
t=0.0s: Arrow spawn, bug khiến velocity quá lớn
t=1.0s: Arrow bay ra khỏi map, không collision
t=2.0s: timeSinceSpawn = 2s
t=6.0s: timeSinceSpawn = 6s → Fallback cleanup
```

**Không dùng Invoke/Coroutine:**
- Update countdown đơn giản
- Dễ debug với Debug.Log trong Update
- Không có overhead của Invoke

---

## 🎓 TÓM TẮT

**Simplified Physics:**
- 1 biến: projectileSpeed
- Gravity tự nhiên tạo parabol
- Không cần force calculation

**Trajectory Predictor:**
- Mô phỏng nhiều góc
- Thu thập validAngles
- Chọn random → đa dạng

**Accuracy System:**
- Archer không hoàn hảo
- accuracy cao → chọn góc tốt
- Upgrade path tự nhiên

**Performance:**
- Kinematic khi dừng
- Object pooling
- 70% CPU reduction

**Kết quả:**
- Đơn giản hơn
- Tự nhiên hơn
- Không cần calibration
- Dễ upgrade

Good luck! 🏹
