# HỆ THỐNG ENEMY PORTAL
**Unity 2D – Kingdom-style Portal Spawning System**

---

## 1. Kiến trúc tổng quan

Hệ thống gồm 4 script độc lập. Mỗi script chỉ biết thứ mình cần biết, không làm việc của script khác.

| Script | Trách nhiệm chính | KHÔNG làm |
|---|---|---|
| `EnemyPortalManager` | Spawn cổng, giữ ref, cung cấp `GetActivePortals()` | Không spawn wave, không biết player stats |
| `EnemyPortalBehavior` | Quản lý state, delay, gọi screenshake, drop loot | Không tự spawn wave, không gọi WaveManager trực tiếp |
| `EnemyWaveManager` | Hỏi Manager lấy danh sách portal active, tính power, spawn enemy | Không biết portal ở đâu, không tự spawn cổng |
| `ScreenShakeController` | Rung camera khi được gọi, singleton trên MainCamera | Không chứa logic game, chỉ là utility |

---

## 2. Flow vận hành theo ngày

### Điều kiện khởi động
Hệ thống chỉ chạy sau khi `GameManager.GameStarted == true`. Biến này được set khi player xây xong TownCenter.

### Chu trình mỗi ngày
1. **Sáng** – `EnemyPortalManager` kiểm tra từng bên (trái/phải). Nếu bên đó `null` thì spawn 1 portal duy nhất trong biên spawn đã cấu hình.
2. **Ban ngày** – Portal ở trạng thái `Dormant`. `DelayDays` giảm dần. Player có thể tấn công chủ động.
3. **Tối** – Nếu `DelayDays <= 0`, portal chuyển sang `Active`. Manager ghi nhận vào danh sách active.
4. **Đêm** – `EnemyWaveManager` hỏi `GetActivePortals()`, tính power và spawn enemy.

### Quy tắc spawn cổng
- Mỗi bên tối đa 1 portal tồn tại tại một thời điểm.
- Kiểm tra mỗi sáng: nếu `portal == null` thì spawn, không thì bỏ qua.
- Sau khi portal bị phá → ngày hôm sau mới spawn lại (vì ref đã về `null`).

---

## 3. Hệ thống Delay kích hoạt

```
// Khi spawn portal:
int delay = (worldDay.Current <= 15) ? 1 : 0;
portal.Setup(delay);
```

**Trước ngày 15:**
- Portal spawn sáng → `DelayDays = 1`
- Đêm hôm đó: không active
- Sáng hôm sau: `DelayDays` giảm về 0
- Đêm hôm sau: portal chính thức Active
- → Player luôn có ít nhất 1 ngày để phản ứng

**Sau ngày 15:**
- Portal spawn sáng → `DelayDays = 0`
- Đêm hôm đó: active ngay
- Vẫn đảm bảo 1 cycle ngày–đêm (spawn ban ngày, active ban đêm)

> **Mở rộng sau:** delay có thể là 1–4 tùy difficulty. Logic không thay đổi, chỉ thay giá trị khởi tạo.

### State machine portal

| Trạng thái | Điều kiện vào | Điều kiện thoát | Cho phép spawn enemy |
|---|---|---|---|
| `Dormant` | Vừa spawn | `DelayDays <= 0` + đêm | Không |
| `Active` | Đêm sau khi delay xong | Portal bị phá | Có |
| `Destroyed` | HP về 0 | — | Không |

---

## 4. EnemyPortalManager.cs

**Trách nhiệm:** Spawn portal, giữ ref, cung cấp API cho WaveManager.

```csharp
public class EnemyPortalManager : MonoBehaviour
{
    public static EnemyPortalManager Instance { get; private set; }

    [Header("Spawn Bounds")]
    public float leftMin, leftMax;
    public float rightMin, rightMax;
    public float groundY;
    public GameObject portalPrefab;

    private EnemyPortalBehavior leftPortal;
    private EnemyPortalBehavior rightPortal;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Gọi từ DayCycleManager mỗi sáng
    public void OnMorning()
    {
        if (!GameManager.Instance.GameStarted) return;

        TrySpawnPortal(ref leftPortal, leftMin, leftMax);
        TrySpawnPortal(ref rightPortal, rightMin, rightMax);
    }

    void TrySpawnPortal(ref EnemyPortalBehavior portal, float min, float max)
    {
        if (portal != null) return; // đã có, bỏ qua

        float x = Random.Range(min, max);
        Vector2 pos = new Vector2(x, groundY);
        GameObject go = Instantiate(portalPrefab, pos, Quaternion.identity);
        portal = go.GetComponent<EnemyPortalBehavior>();

        int delay = (WorldDayManager.Instance.Current <= 15) ? 1 : 0;
        portal.Setup(delay);
    }

    // API cho EnemyWaveManager
    public List<EnemyPortalBehavior> GetActivePortals()
    {
        var list = new List<EnemyPortalBehavior>();
        if (leftPortal  != null && leftPortal.State  == PortalState.Active) list.Add(leftPortal);
        if (rightPortal != null && rightPortal.State == PortalState.Active) list.Add(rightPortal);
        return list;
    }

    // Gọi từ EnemyPortalBehavior khi bị phá
    public void OnPortalDestroyed(EnemyPortalBehavior portal)
    {
        if (leftPortal  == portal) leftPortal  = null;
        if (rightPortal == portal) rightPortal = null;
    }
}
```

---

## 5. EnemyPortalBehavior.cs

**Trách nhiệm:** Quản lý state, delay, screenshake, drop loot, tracking stolenCoins.

```csharp
public enum PortalState { Dormant, Active, Destroyed }

public class EnemyPortalBehavior : MonoBehaviour
{
    public PortalState State { get; private set; }

    [SerializeField] private int maxHp = 5;
    private int hp;
    private int delayDays;
    private int stolenCoins = 0;

    private PortalVisualAnimator visualAnimator;

    void Awake()
    {
        hp = maxHp;
        visualAnimator = GetComponentInChildren<PortalVisualAnimator>();
    }

    public void Setup(int delay)
    {
        delayDays = delay;
        State = PortalState.Dormant;
        ScreenShakeController.Instance.Shake(0.4f, 0.6f);
        visualAnimator.SetDormant();
    }

    // Gọi từ DayCycleManager mỗi sáng
    public void OnNewDay()
    {
        if (State != PortalState.Dormant) return;
        delayDays -= 1;
    }

    // Gọi từ DayCycleManager khi trời tối
    public void OnNightStart()
    {
        if (State == PortalState.Dormant && delayDays <= 0)
        {
            State = PortalState.Active;
            visualAnimator.SetActive();
        }
    }

    // Gọi từ EnemyBehavior khi enemy cướp coin
    public void AddStolenCoins(int amount)
    {
        stolenCoins += amount;
    }

    public void TakeDamage(int dmg)
    {
        if (State == PortalState.Destroyed) return;
        hp -= dmg;
        if (hp <= 0) DestroyPortal();
    }

    private void DestroyPortal()
    {
        State = PortalState.Destroyed;
        EnemyPortalManager.Instance.OnPortalDestroyed(this);
        DropLoot();
        // TODO: Hiệu ứng vỡ cổng
        Destroy(gameObject, 0.5f);
    }

    private void DropLoot()
    {
        int coins = Random.Range(3, 8) + stolenCoins;
        CoinSpawner.Instance.SpawnCoins(transform.position, coins);
        stolenCoins = 0;
    }
}
```

---

## 6. EnemyWaveManager.cs

**Trách nhiệm:** Hỏi Manager lấy portal active, tính power, spawn enemy.

**Quan trọng:** WaveManager KHÔNG giữ reference đến portal. Nó luôn hỏi `GetActivePortals()` mỗi đêm.

```csharp
[System.Serializable]
public class EnemyData
{
    public GameObject prefab;
    public int power; // Gremlin=1, Archer=2, Flier=3, Brute=4
}

public class EnemyWaveManager : MonoBehaviour
{
    public List<EnemyData> enemyList;
    public float basePower = 3f;
    public float growthRate = 1.0f; // tăng sau khi có difficulty

    // Gọi từ DayCycleManager khi trời tối
    public void OnNightStart()
    {
        var portals = EnemyPortalManager.Instance.GetActivePortals();
        if (portals.Count == 0) return;

        float totalPower = basePower + (WorldDayManager.Instance.Current * growthRate);
        float powerPerPortal = totalPower / portals.Count;

        foreach (var portal in portals)
            SpawnWaveAtPortal(portal, powerPerPortal);
    }

    void SpawnWaveAtPortal(EnemyPortalBehavior portal, float budget)
    {
        float remaining = budget;
        while (remaining > 0)
        {
            var affordable = enemyList.Where(e => e.power <= remaining).ToList();
            if (affordable.Count == 0) break;

            var chosen = affordable[Random.Range(0, affordable.Count)];
            var go = Instantiate(chosen.prefab, portal.transform.position, Quaternion.identity);

            // Gán portal nguồn để tracking stolenCoins
            go.GetComponent<EnemyBehavior>().SourcePortal = portal;

            remaining -= chosen.power;
        }
    }
}
```

---

## 7. Cơ chế tracking Stolen Coins

Khi enemy cướp coin của player, nó báo về portal nguồn gốc. Portal tích lũy số coin đó cho đến khi bị phá.

```csharp
// Trong EnemyBehavior:
public EnemyPortalBehavior SourcePortal;

void StealCoinsFromPlayer(int amount)
{
    int stolen = CoinManager.Instance.RemoveCoins(amount);
    if (SourcePortal != null)
        SourcePortal.AddStolenCoins(stolen);
}
```

Khi portal bị phá, toàn bộ `stolenCoins` được drop ra cùng với loot cơ bản.

---

## 8. Checklist triển khai

- [ ] `GameManager` có `bool GameStarted`, set `true` khi TownCenter xây xong
- [ ] `WorldDayManager` có `int Current` cho ngày hiện tại
- [ ] `DayCycleManager` gọi `OnMorning()` và `OnNightStart()` đúng thứ tự cho tất cả listeners
- [ ] `EnemyPortalManager` có singleton `Instance`, gắn vào scene
- [ ] Biên spawn `leftMin/leftMax` và `rightMin/rightMax` đã cấu hình trong Inspector
- [ ] Portal prefab có `EnemyPortalBehavior` và `PortalVisualAnimator` (xem tài liệu 2)
- [ ] `ScreenShakeController` có singleton `Instance`, gắn trên MainCamera
- [ ] `EnemyBehavior` có field `SourcePortal`, được gán khi spawn trong `SpawnWaveAtPortal`
- [ ] `CoinSpawner` có `SpawnCoins(Vector2 pos, int amount)`
- [ ] `EnemyData` ScriptableObject hoặc class có field `power` (int)
- [ ] `EnemyWaveManager` có `List<EnemyData> enemyList`, `basePower`, `growthRate`
