[trước khi đọc tài liệu ,tôi là user thực tế  , tôi sẽ luôn viết comment trong [] để tránh việc bạn bị nhầm với AI khác ]

# Enemy State Machine Architecture
### Dự án: Crab Farm | Môi trường: Unity 2D | Style: Kingdom Two Crowns

---

## ⚠️ GHI CHÚ CHO AI TIẾP THEO — ĐỌC TRƯỚC KHI PHÂN TÍCH

**Yêu cầu hiện tại của User (tính đến thời điểm tạo file này):**

> Chỉ implement những gì liên quan đến **cấu trúc code core của một Enemy** — đủ làm khung để sau này dễ dàng thêm và sửa các loại Enemy mới. Không cần implement đầy đủ mọi feature.
>
> Bắt đầu bằng enemy ví dụ: **ShieldBot** — có thể di chuyển và tấn công. **Không có** chức năng nhặt coin (LootingState) và **không có** chức năng rút về cổng (ReturnToPortalState).
>
> Toàn bộ script liên quan đến ShieldBot được ghi trực tiếp vào tài liệu này như một phần của kiến trúc.[tôi muốn triển khai làm shieldbot trước để test lỗi trực quan]

**Lịch sử review tài liệu này:**
- `v1` — Kiến trúc gốc do AI tạo từ kế hoạch `npc_enemy_system_report.md`
- `v2` → `v3` — Đã qua 2 vòng review (Reviewer #1 + Reviewer #2) và 1 vòng phán quyết (Arbitrator). 8 fix đã được apply vào v3.
- `v4 (file này)` — Sửa các lỗi còn tồn tại sau v3, thu gọn về scope thực tế, bổ sung ShieldBot làm ví dụ cụ thể.

**Những gì đã bị xác định là lỗi trong v3 và được sửa trong v4:**
1. `LootingState` trở thành **unreachable** sau Fix #2 (không có state nào chuyển sang nó). Đây là lỗi do fix #2 đưa vào. → Trong v4: `LootingState` và `ReturnToPortalState` được giữ lại trong base framework nhưng **không phải scope hiện tại** — ghi rõ, không xóa.
2. `MoveToTargetState`: condition `ctx.LootTarget != null && ctx.CurrentTarget != null` — `CurrentTarget != null` là thừa vì đã pass qua guard trên. → Sửa gọn thành `ctx.LootTarget = null` đơn giản.
3. `IdleState` dùng `ctx.TargetManager.IsReady` — property này **chưa tồn tại** trong `EnemyTargetManager`. → Giữ comment nhưng dùng fallback không crash đến khi property được implement.
4. Cache trigger dùng `System.Array.FindAll` + LINQ lẫn lộn → Thống nhất về LINQ.

---

## 1. Tổng quan kiến trúc

Enemy AI được tổ chức theo **State Pattern** — mỗi state là một class riêng, `EnemyAIController` chỉ giữ nhiệm vụ chuyển state, không chứa logic của bất kỳ state nào.

```
EnemyAIController
    │
    ├── currentState.Enter()
    ├── currentState.Update()
    └── currentState.Exit()
         │
         ├── IdleState           ← dùng cho mọi enemy
         ├── PatrolState         ← dùng cho mọi enemy
         ├── MoveToTargetState   ← dùng cho mọi enemy
         ├── AttackState         ← dùng cho mọi enemy
         ├── DeadState           ← dùng cho mọi enemy
         │
         ├── LootingState        ← feature optional (enemy có greed)
         └── ReturnToPortalState ← feature optional (enemy có greed )[greed là sự tham lam, chúng sẽ đi cướp đồ của player]
```

**Triết lý mở rộng:** State nào không dùng thì không override — Enemy đơn giản như ShieldBot chỉ dùng 5 state đầu. Enemy phức tạp hơn sau này kế thừa thêm state tùy nhu cầu.

---

## 2. Cấu trúc thư mục
[tôi thấy tài liệu này nói khá rõ ràng về statemachine , nhưng để triển khai thực tế , ta cũng cần status (theo kiến trức hệ thống tôi đang làm thì mỗi gameobject dạng npc sẽ có status và name+behavior để phân biệt hành vi, tôi hi vọng các bạn bổ xung thêm status và đổi các state đặc thù (variants của từng loại enemy)thành tên thống nhất là name + behavior  để đồng bộ cấu trúc hiện tại theo tên gọi)]
```
E:\unity\Crab_farm\Assets\Script\Game\Enemy\
│
├── EnemyAIController.cs          ← script gắn lên GameObject, điều phối state
├── EnemyContext.cs               ← dữ liệu chia sẻ giữa các state
│
├── Core\
│   └── EnemyState.cs             ← abstract base class cho mọi state
│
├── States\                       ← States dùng chung cho mọi loại Enemy
│   ├── IdleState.cs
│   ├── PatrolState.cs
│   ├── MoveToTargetState.cs
│   ├── AttackState.cs
│   ├── LootingState.cs           ← optional, chỉ dùng cho enemy có greed
│   ├── ReturnToPortalState.cs    ← optional, chỉ dùng cho enemy có greed
│   └── DeadState.cs
│
└── Variants\                     ← States đặc thù từng loại Enemy
    └── ShieldBot\
        └── ShieldBotAttackState.cs
```

---

## 3. Script Core

### `EnemyState.cs` — Abstract base class

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\Core\EnemyState.cs

namespace Script.Game.Enemy.Core
{
    public abstract class EnemyState
    {
        public abstract void Enter(EnemyContext ctx);
        public abstract void Update(EnemyContext ctx);
        public abstract void Exit(EnemyContext ctx);
    }
}
```

---

### `EnemyContext.cs` — Túi dữ liệu chia sẻ giữa các state

Script này **không phải MonoBehaviour**, chỉ là một class chứa dữ liệu. Các State được phép ghi vào Context khi cần cập nhật trạng thái runtime (`CurrentTarget`, `CurrentGreed`, `LootTarget`). Những field chỉ được set một lần lúc khởi tạo (`MoveSpeed`, `AttackRange`...) thì không nên ghi lại từ State.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\EnemyContext.cs

using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyContext
    {
        // References
        public GameObject Self;
        public Animator Animator;
        public EnemyAIController Controller;

        // External systems — inject từ Controller, State không gọi Singleton trực tiếp
        public EnemyTargetManager TargetManager;
        public EnemyPool Pool;

        // Target
        public GameObject CurrentTarget;
        public GameObject LootTarget;   // coin/item Enemy nhắm tới — chỉ dùng cho enemy có greed

        // Greed — chỉ dùng cho enemy có greed mechanic (ví dụ CircleHead)
        // Enemy không có greed (ví dụ ShieldBot) để nguyên giá trị mặc định, không đọc field này
        public float GreedThreshold;
        public float CurrentGreed;

        // Stats
        public float MoveSpeed;
        public float AttackRange;
        public float AttackCooldown = 1f;

        // Patrol
        public Vector3 SpawnPosition;
        public float PatrolRadius = 10f;

        // Portal — chỉ dùng cho enemy có ReturnToPortalState
        public Vector3 PortalPosition;
        // LƯU Ý: PortalPosition không tự cập nhật nếu Portal di chuyển runtime.
        // Nếu Portal có thể bị phá hoặc di chuyển, cần inject reference GameObject thay vì Vector3 tĩnh.
/*[ở đây portal không thể di chuyển nhưng chúng có thể bị phá hủy ,chú ý, enemy không nhớ vị trí portal, tôi có 
public class EnemyPortalManager : MonoBehaviour
    {
        public static EnemyPortalManager Instance { get; private set; }

        public GameObject portalL;
        public GameObject portalR;

 để quản lý việc enemy đi ra từ đâu nên cần thay đổi chút ,enemy sẽ lưu portal , nếu portal nó đi ra bị phá hủy thì enemy sẽ hỏi EnemyPortalManager để lấy portal khác ,nếu không còn portal nào thì nó sẽ tiếp tục tấn công trong state green?
]*/
        // Trạng thái sống — dùng để chặn AI chạy sau khi chết
        public bool IsAlive = true;

        // Flag defer recycle — tránh gọi Pool.ReturnEnemy từ bên trong Enter/Exit
        public bool PendingRecycle = false;

        // Throttle GetTarget trong PatrolState
        // Không thay đổi sau khi enemy đã spawn — PatrolState dùng field này làm ngưỡng kiểm tra liên tục
        public float PatrolCheckInterval = 0.3f;

        // Helpers
        public bool GreedIsFull => CurrentGreed >= GreedThreshold;
    }
}
```

---

### `EnemyAIController.cs` — Script duy nhất gắn lên GameObject Enemy

Script này chỉ làm đúng một việc: **giữ state hiện tại và điều phối vòng lặp**. Đây là nơi duy nhất inject các Singleton vào Context.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\EnemyAIController.cs

using System.Linq;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;

namespace Script.Game.Enemy
{
    public class EnemyAIController : MonoBehaviour
    {
/* user ghi chú :
tôi có 1 script TotalEnemyList  lưu danh sách và thông tin enemy , bạn tham khảo và bổ sung*/

        [Header("Stats")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackRange = 0.8f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float greedThreshold = 5f;
        [SerializeField] private float patrolRadius = 10f;

        private EnemyState _currentState;
        private EnemyContext _ctx;

        // Cache tên trigger một lần trong Awake.
        // Animator.parameters trả về array copy mỗi lần gọi — nếu reset trong ChangeState mỗi frame
        // mà không cache, sẽ tạo allocation liên tục. Cache giải quyết vấn đề này.
        private string[] _animatorTriggers;

        private void Awake()
        {
            _ctx = new EnemyContext
            {
                Self           = gameObject,
                Animator       = GetComponent<Animator>(),
                Controller     = this,
                MoveSpeed      = moveSpeed,
                AttackRange    = attackRange,
                AttackCooldown = attackCooldown,
                GreedThreshold = greedThreshold,
                PatrolRadius   = patrolRadius,
                SpawnPosition  = transform.position,
                TargetManager  = EnemyTargetManager.Instance,
                Pool           = EnemyPool.Instance
            };

            _animatorTriggers = _ctx.Animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Trigger)
                .Select(p => p.name)
                .ToArray();
        }

        private void Start()
        {
            ChangeState(new IdleState());
        }

        private void Update()
        {
            // Guard: không chạy AI sau khi đã chết
            if (!_ctx.IsAlive) return;

            // Defer recycle ra khỏi Enter/Exit để tránh corrupt state machine.
            // Gọi Pool.ReturnEnemy từ trong Enter/Exit sẽ disable GameObject
            // trong khi ChangeState chưa return xong.
            if (_ctx.PendingRecycle)
            {
                _ctx.PendingRecycle = false;
                if (_ctx.Pool != null)
                    _ctx.Pool.ReturnEnemy(_ctx.Self);
                else
                {
                    // Fallback: Pool không tồn tại (scene không có EnemyPool, hoặc
                    // Pool bị destroy trước enemy khi unload scene).
                    Debug.LogWarning($"[EnemyAIController] {_ctx.Self.name}: Pool is null, deactivating directly.");
                    gameObject.SetActive(false);
                }
                return;
            }

            _currentState?.Update(_ctx);
        }

        public void ChangeState(EnemyState newState)
        {
            _currentState?.Exit(_ctx);

            // Reset toàn bộ Animator trigger trước khi Enter state mới.
            // Tránh trigger còn trong queue từ state cũ fire sai vào animation state mới
            // khi state exit trước khi Animator kịp consume trigger đã set.
            foreach (var t in _animatorTriggers)
                _ctx.Animator.ResetTrigger(t);

            _currentState = newState;
            _currentState.Enter(_ctx);
        }

        // Gọi từ IHealth.Die() khi Enemy hết máu
        public void OnDeath()
        {
            // Guard tránh double-call trong cùng một frame:
            // Nếu 2 projectile chạm Enemy cùng physics step, Unity gọi OnTriggerEnter2D
            // 2 lần liên tiếp — guard này đủ để chặn.
            if (!_ctx.IsAlive) return;

            _ctx.IsAlive = false;
            ChangeState(new DeadState());
        }

        // Gọi từ Pool khi tái sử dụng Enemy
        public void ResetEnemy(Vector3 portalPosition)
        {
            // Bước 1: Exit state cũ với context nguyên trạng.
            // Đảm bảo Exit thấy đúng context lúc enemy "đang chết/recycle",
            // không bị ảnh hưởng bởi các field reset phía sau.
            _currentState?.Exit(_ctx);
            _currentState = null;

            // Bước 2: Reset toàn bộ context.
            _ctx.IsAlive        = true;
            _ctx.CurrentGreed   = 0;
            _ctx.CurrentTarget  = null;
            _ctx.LootTarget     = null;
            _ctx.PendingRecycle = false;
            _ctx.PortalPosition = portalPosition;
            // ASSUMPTION: Enemy luôn spawn từ Portal → SpawnPosition = PortalPosition.
            // Nếu sau này có loại enemy spawn từ vị trí khác, cần truyền spawnPosition riêng.
            _ctx.SpawnPosition  = portalPosition;

            // Bước 3: Enter state mới với context đã sạch.
            // Tách khỏi ChangeState() để không reset trigger khi mới spawn
            // (Animator chưa có trigger nào pending từ trước).
            _currentState = new IdleState();
            _currentState.Enter(_ctx);
        }
    }
}
```

---

## 4. States dùng chung

### `IdleState.cs` — Tìm mục tiêu khi mới spawn hoặc mất target

> **Về `IsReady` guard:** Lý tưởng, `IdleState` nên chờ `TargetManager.IsReady` trước khi query để tránh nhảy `PatrolState` ngay frame đầu khi raycast chưa refresh. Tuy nhiên `EnemyTargetManager` **chưa expose property `IsReady`** — cần bổ sung sau. Hiện tại: nếu `GetTarget` trả về null (raycast chưa kịp), enemy vào `PatrolState` và sẽ tìm lại sau 0.3s — chấp nhận được cho giai đoạn này.

> **Lý do không gọi `ChangeState` trong `Enter`:** Call stack sẽ là `ChangeState(Idle)` → `Idle.Enter()` → `ChangeState(MoveToTarget)` → `Idle.Exit()` — Exit chạy **sau** Enter đã gọi state khác, thứ tự không thể đoán khi Exit có cleanup logic.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\IdleState.cs

using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class IdleState : EnemyState
    {
        public override void Enter(EnemyContext ctx)
        {
            ctx.Animator.SetTrigger("Idle");
        }

        public override void Update(EnemyContext ctx)
        {
            // TODO: thêm guard khi EnemyTargetManager expose IsReady:
            // if (!ctx.TargetManager.IsReady) return;

            var target = ctx.TargetManager.GetTarget(ctx.Self);

            if (target != null)
            {
                ctx.CurrentTarget = target;
                ctx.Controller.ChangeState(new MoveToTargetState());
            }
            else
            {
                ctx.Controller.ChangeState(new PatrolState());
            }
        }

        public override void Exit(EnemyContext ctx) { }
    }
}
```

---

### `PatrolState.cs` — Đi dạo khi chưa có mục tiêu

> Điểm patrol tính từ `SpawnPosition`, không phải vị trí hiện tại — tránh enemy drift ra khỏi khu vực spawn.

> Với 50+ enemy patrol đồng thời, `_checkTimer` throttle tần suất gọi `GetTarget` xuống còn `PatrolCheckInterval` (mặc định 0.3s/lần) để giảm tải performance.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\PatrolState.cs

using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Utils;

namespace Script.Game.Enemy.States
{
    public class PatrolState : EnemyState
    {
        private Vector3 _patrolTarget;
        private float _waitTimer;
        private float _checkTimer;
        private const float WaitTime = 1.5f;

        public override void Enter(EnemyContext ctx)
        {
            ctx.Animator.SetTrigger("Walk");
            _waitTimer = 0;
            _checkTimer = ctx.PatrolCheckInterval; // cho phép check ngay lần đầu
            _patrolTarget = GetRandomPatrolPoint(ctx);
        }

        public override void Update(EnemyContext ctx)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= ctx.PatrolCheckInterval)
            {
                _checkTimer = 0;
                var target = ctx.TargetManager.GetTarget(ctx.Self);
                if (target != null)
                {
                    ctx.CurrentTarget = target;
                    ctx.Controller.ChangeState(new MoveToTargetState());
                    return;
                }
            }

            bool reached = MovementUtility.MoveTowardsTarget(
                ctx.Self, _patrolTarget, ctx.MoveSpeed
            );

            if (reached)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= WaitTime)
                {
                    _waitTimer = 0;
                    _patrolTarget = GetRandomPatrolPoint(ctx);
                }
            }
        }

        public override void Exit(EnemyContext ctx) { }

        private Vector3 GetRandomPatrolPoint(EnemyContext ctx)
        {
            float offsetX = Random.Range(-ctx.PatrolRadius, ctx.PatrolRadius);
            return new Vector3(ctx.SpawnPosition.x + offsetX, ctx.SpawnPosition.y, 0);
        }
    }
}
```

---

### `MoveToTargetState.cs` — Di chuyển đến mục tiêu

> **LootTarget và combat:** `LootTarget` được set bởi `OnTriggerEnter2D` khi enemy va vào coin (collision-based). Khi đang chase `CurrentTarget`, nếu va vào coin thì **clear `LootTarget` ngay** — tránh coin bị "ghost-reserved" (set vào ctx nhưng không có state nào xử lý vì không ai chuyển sang `LootingState` khi đang có combat target).

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\MoveToTargetState.cs

using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Utils;

namespace Script.Game.Enemy.States
{
    public class MoveToTargetState : EnemyState
    {
        public override void Enter(EnemyContext ctx)
        {
            ctx.Animator.SetTrigger("Walk");
        }

        public override void Update(EnemyContext ctx)
        {
            if (ctx.CurrentTarget == null)
            {
                ctx.Controller.ChangeState(new IdleState());
                return;
            }

            if (ctx.GreedIsFull)
            {
                ctx.LootTarget = null;
                ctx.Controller.ChangeState(new ReturnToPortalState());
                return;
            }

            float dist = Vector3.Distance(
                ctx.Self.transform.position,
                ctx.CurrentTarget.transform.position
            );

            if (dist <= ctx.AttackRange)
            {
                ctx.Controller.ChangeState(new AttackState());
                return;
            }

            // Đang chase combat target → clear LootTarget ngay nếu có.
            // Không để coin bị ghost-reserved trong ctx đến khi enemy reset.
            ctx.LootTarget = null;

            MovementUtility.MoveTowardsTarget(
                ctx.Self, ctx.CurrentTarget.transform.position, ctx.MoveSpeed
            );
        }

        public override void Exit(EnemyContext ctx) { }
    }
}
```

---

### `AttackState.cs` — Tấn công mục tiêu (Wall, Lính, TownCenter)

> **Timer check trước, cộng sau** — tránh drift tích lũy khi cần sync với animation.

> **Attack timer vs Animation Event:** Cách hiện tại gọi `Attack()` theo timer, damage xảy ra cùng frame với `SetTrigger` — trước khi animation swing kịp play. Nếu cần damage sync đúng hit frame, dùng **Animation Event** thay vì timer. Nếu không cần sync chặt, timer hiện tại là đủ.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\AttackState.cs

using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class AttackState : EnemyState
    {
        private float _attackTimer;

        public override void Enter(EnemyContext ctx)
        {
            ctx.Animator.SetTrigger("Attack");
            _attackTimer = ctx.AttackCooldown; // cho phép đánh ngay lần đầu
        }

        public override void Update(EnemyContext ctx)
        {
            if (ctx.CurrentTarget == null)
            {
                ctx.Controller.ChangeState(new IdleState());
                return;
            }

            float dist = Vector3.Distance(
                ctx.Self.transform.position,
                ctx.CurrentTarget.transform.position
            );

            if (dist > ctx.AttackRange)
            {
                ctx.Controller.ChangeState(new MoveToTargetState());
                return;
            }

            // Check TRƯỚC, cộng SAU — tránh drift timer
            if (_attackTimer >= ctx.AttackCooldown)
            {
                _attackTimer = 0;
                PerformAttack(ctx);
            }
            _attackTimer += Time.deltaTime;
        }

        public override void Exit(EnemyContext ctx)
        {
            _attackTimer = 0;
        }

        // Virtual để subclass override hành vi đánh cụ thể
        protected virtual void PerformAttack(EnemyContext ctx)
        {
            // TODO: gọi IAttacker.Attack() khi EnemyStatus hoàn thiện
            // ctx.Self.GetComponent<IAttacker>()?.Attack();
        }
    }
}
```

---

### `DeadState.cs` — Trạng thái chết

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\DeadState.cs

using Script.Game.Enemy.Core;

namespace Script.Game.Enemy.States
{
    public class DeadState : EnemyState
    {
        public override void Enter(EnemyContext ctx)
        {
            ctx.Animator.SetTrigger("Die");
            // Không recycle ngay — để animation chết chạy xong.
            // Pool.ReturnEnemy được gọi từ Animation Event hoặc Coroutine bên ngoài.
        }

        /// <summary>
        /// Intentionally empty.
        /// INVARIANT: State này CHỈ được enter qua EnemyAIController.OnDeath(),
        /// method đó set IsAlive = false TRƯỚC khi gọi ChangeState.
        /// EnemyAIController.Update() guard trên IsAlive = false ngăn method này chạy.
        /// Nếu ChangeState(new DeadState()) được gọi từ nơi khác mà không set IsAlive = false
        /// trước, đây là vi phạm invariant — không phải lỗi của state này.
        /// </summary>
        public override void Update(EnemyContext ctx) { }

        public override void Exit(EnemyContext ctx) { }
    }
}
```

---

### States optional — chỉ dùng cho enemy có greed mechanic

`LootingState` và `ReturnToPortalState` là một phần của framework nhưng **không phải scope hiện tại**. Giữ lại trong codebase để enemy phức tạp sau này dùng, không xóa.

Xem chi tiết implementation tại nhánh feature khi cần implement greed mechanic.

---

## 5. ShieldBot — Enemy ví dụ đầu tiên

ShieldBot là enemy đơn giản nhất: **di chuyển và tấn công**, không nhặt coin, không rút về cổng. Toàn bộ behavior dùng lại 5 state base, chỉ override `AttackState` để thêm animation riêng.

### Cấu trúc file ShieldBot

```
States\Variants\ShieldBot\
└── ShieldBotAttackState.cs     ← override animation, giữ nguyên toàn bộ logic attack
```

GameObject Inspector của ShieldBot:
```
GameObject: ShieldBot
 ├── EnemyAIController.cs   ← cấu hình stats trong Inspector
 ├── Animator               ← có các trigger: Idle, Walk, Attack, Die, ShieldBash
 ├── Rigidbody2D
 └── Collider2D
```

---

### `ShieldBotAttackState.cs`

ShieldBot có animation tấn công riêng ("ShieldBash") thay vì animation "Attack" mặc định. Toàn bộ logic timer, cooldown, transition kiểm tra distance — kế thừa hoàn toàn từ `AttackState`.

> **Lưu ý double trigger:** `base.Enter()` set trigger "Attack", sau đó override set thêm "ShieldBash". Cả hai vào queue của Animator trong cùng frame. Để tránh "Attack" fire trước "ShieldBash", cần đảm bảo trong Animator: transition "ShieldBash" có priority cao hơn "Attack", hoặc chỉ dùng một trigger. Cách đơn giản nhất: trong `ShieldBotAttackState.Enter()` **không gọi `base.Enter()`** và set trigger "ShieldBash" trực tiếp — nhưng vẫn phải khởi tạo `_attackTimer` thủ công.

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\States\Variants\ShieldBot\ShieldBotAttackState.cs

using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;

namespace Script.Game.Enemy.States.Variants.ShieldBot
{
    public class ShieldBotAttackState : AttackState
    {
        public override void Enter(EnemyContext ctx)
        {
            // KHÔNG gọi base.Enter() để tránh double trigger ("Attack" + "ShieldBash").
            // Khởi tạo _attackTimer trực tiếp qua reflection hoặc exposed method.
            // Vì _attackTimer là private trong base, cần đổi thành protected:
            //   protected float _attackTimer;  ← đổi access modifier trong AttackState.cs
            ctx.Animator.SetTrigger("ShieldBash");
            _attackTimer = ctx.AttackCooldown; // cho phép đánh ngay lần đầu
        }

        protected override void PerformAttack(EnemyContext ctx)
        {
            // ShieldBot gây sát thương theo cơ chế riêng — ví dụ knockback
            // TODO: ctx.Self.GetComponent<ShieldBotStatus>()?.ShieldBash();
            Debug.Log($"[ShieldBot] {ctx.Self.name} ShieldBash!");
        }
    }
}
```

> **Yêu cầu thay đổi trong `AttackState.cs`:** Đổi `private float _attackTimer` thành `protected float _attackTimer` để subclass truy cập trực tiếp mà không cần reflection.

**`AttackState.cs` — dòng cần sửa:**
```csharp
// Trước:
private float _attackTimer;

// Sau:
protected float _attackTimer;
```

---

### `EnemyAIController` cho ShieldBot — không cần file riêng

ShieldBot **dùng cùng `EnemyAIController.cs`** với mọi enemy khác. Sự khác biệt duy nhất là ở `Start()` — khởi tạo state ban đầu với `ShieldBotAttackState` được inject vào khi cần.

Cách làm sạch nhất: `EnemyAIController` expose một virtual method để subclass override state khởi đầu:

```csharp
// Thêm vào EnemyAIController.cs
protected virtual EnemyState CreateAttackState() => new AttackState();
```

Sau đó tạo `ShieldBotAIController.cs` kế thừa:

```csharp
// E:\unity\Crab_farm\Assets\Script\Game\Enemy\ShieldBotAIController.cs

using Script.Game.Enemy.States.Variants.ShieldBot;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy
{
    public class ShieldBotAIController : EnemyAIController
    {
        protected override EnemyState CreateAttackState()
            => new ShieldBotAttackState();
    }
}
```

Đặt `ShieldBotAIController` lên GameObject ShieldBot thay vì `EnemyAIController` base. Không thay đổi bất kỳ dòng nào trong base class.

> Tuy nhiên, cách này đòi hỏi `MoveToTargetState` biết dùng `CreateAttackState()` thay vì `new AttackState()` hardcode. Nếu chưa muốn refactor ngay, cách đơn giản hơn là **thay thẳng `new AttackState()` bằng `new ShieldBotAttackState()`** trong một `ShieldBotAIController` override `Start()`:

```csharp
// Cách đơn giản hơn — ShieldBotAIController chỉ cần override Start
public class ShieldBotAIController : EnemyAIController
{
    protected override void Start()
    {
        // Không gọi base.Start() — tự quản lý state khởi đầu
        ChangeState(new IdleState()); // ShieldBot vẫn bắt đầu từ Idle
    }
}
```

Và trong `MoveToTargetState`, thay `new AttackState()` thành gọi factory method qua Context hoặc Controller:

```csharp
// Trong MoveToTargetState.Update() — thay vì hardcode:
ctx.Controller.ChangeState(new AttackState());

// Dùng factory:
ctx.Controller.ChangeState(ctx.Controller.CreateAttackState());
```

---

## 6. Sơ đồ chuyển state — ShieldBot

```
Spawn → ResetEnemy()
  ↓
[Idle] ──── GetTarget() ────→ target found → [MoveToTarget]
  │                                               │
  └── null ──→ [Patrol]                          ├── dist <= AttackRange → [ShieldBotAttack]
        │                                         │          │
        └── GetTarget() (0.3s) → found ──────────┘    target null/dead → [Idle]
                                                   │
                                        dist > AttackRange → [MoveToTarget]

Bất kỳ state nào
  └── OnDeath() → [Dead] → animation xong → Pool.Return()
```

**ShieldBot không có path đến `LootingState` hoặc `ReturnToPortalState`.** Đây là by design — không phải thiếu sót.

---

## 7. Sơ đồ chuyển state — Enemy đầy đủ (tham khảo cho sau này)

```
Spawn → ResetEnemy()
  ↓
[Idle] ──── GetTarget() ────────────────────→ [MoveToTarget]
  │                                                 │
  └── null ──→ [Patrol]                            ├── dist <= AttackRange → [Attack]
        │                                           │        │
        └── GetTarget() (0.3s) → found ────────────┘   target null/dead → [Idle]
                                                    │
                        ctx.LootTarget được set bởi OnTriggerEnter2D,
                        chỉ xử lý khi CurrentTarget == null (patrol/idle flow)
                                                    │
                                                [Looting]
                                                    │
                                   ├── greed chưa full → [Idle]
                                   └── greed full ──→ [ReturnToPortal]
                                                            │
                              PendingRecycle → EnemyAIController.Update → Pool.ReturnEnemy()

Bất kỳ state nào
  └── OnDeath() → IsAlive = false → [Dead] → animation xong → Pool.Return()
```

> **Lưu ý về LootingState trigger:** Theo thiết kế, `LootingState` được vào từ `PatrolState` hoặc `IdleState` khi enemy không có combat target và `LootTarget != null`. `MoveToTargetState` **không trigger LootingState** — khi đang chase mục tiêu chiến đấu, coin bị clear khỏi ctx ngay. Phần logic check `LootTarget` trong `PatrolState`/`IdleState` là **TODO** — chưa implement trong scope hiện tại.
[user ghi chú :   looting state là di chuyển đến vị trí item mà enemy thấy, không bao gồm hàm nhặt item, có 1 script riêng quản lý khi enemy trigger vào item sẽ kích hoạt tự động nhặt nếu tag thuộc  prefabdata.instance.enemyLootTags.

việc định nghĩa enemy này có thể nhặt đồ hay không phải được định nghĩa ngay trong json và hàm load ,bạn phải sửa lại kịch bản sao chu phù hợp việc load này ]
---

## 8. Inspector và GameObject setup

Chỉ có **một script gắn lên GameObject** mỗi loại enemy:

```
GameObject: ShieldBot
 ├── ShieldBotAIController.cs  ← thay EnemyAIController base
 ├── Animator                  ← triggers: Idle, Walk, ShieldBash, Die
 ├── Rigidbody2D
 └── Collider2D
```

Inspector sạch. Không có state class nào hiện trong Inspector vì tất cả là plain C# class.

---

## 9. Hướng mở rộng sau này

Khi thêm loại enemy mới:

```csharp
// Ví dụ: ArcherEnemy — tấn công tầm xa, không patrol
public class ArcherAttackState : AttackState
{
    protected override void PerformAttack(EnemyContext ctx)
    {
        // Bắn projectile thay vì melee
    }
}

// Ví dụ: CircleHead — có greed, nhặt coin, rút về cổng
// Dùng đầy đủ tất cả 7 state bao gồm LootingState + ReturnToPortalState
// Không cần override gì — behavior mặc định đủ dùng
```

**Quy tắc:** State nào không cần thay đổi thì không override. Chỉ tạo file mới khi behavior thực sự khác.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class TotalEnemyList : MonoBehaviour
    {

        public GameObject circleHead;
        
        public GameObject shieldBot;
        public GameObject twoHeadBot;

        public GameObject stormHead;
        public GameObject toucher;
        
        public static TotalEnemyList Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
               
            }
            else
            {
                Destroy(gameObject);
            }

            InitializeEnemyData();
        }

        // Class cơ sở cho dữ liệu của mọi enemy
        [System.Serializable]
        public class EnemyDataBase
        {
            public int id;
            public string enemyName;
            public float attackSpeed;
            public int dame;
            public int hp;
            public int speed;
            public GameObject enemyPrefab; // Not loaded from JSON
            public string description;
            public int power;//sử dụng để tích chiến lực,dẫn đến số lượng loại này sẽ spawn ra
        }

        [System.Serializable]
        public class EnemyDataConfigWrapper
        {
            public List<EnemyDataBase> enemies;
        }

        public List<EnemyDataBase> EnemyDataList = new List<EnemyDataBase>();

        private void InitializeEnemyData()
        {
            // Load JSON from Resources
            TextAsset jsonFile = Resources.Load<TextAsset>("DataConfig/EnemyDataConfig");
            if (jsonFile == null)
            {
                Debug.LogError("Cannot find EnemyDataConfig.json in Resources/DataConfig");
                return;
            }

            EnemyDataConfigWrapper config = JsonUtility.FromJson<EnemyDataConfigWrapper>(jsonFile.text);

            if (config != null && config.enemies != null)
            {
                foreach (var data in config.enemies)
                {
                    // Map prefab based on name
                    switch (data.enemyName)
                    {
                        case "circleHead":
                            data.enemyPrefab = circleHead;
                            break;
                        case "shieldBot":
                            data.enemyPrefab = shieldBot;
                            break;
                        case "twoHeadBot":
                            data.enemyPrefab = twoHeadBot;
                            break;
                        case "stormHead":
                            data.enemyPrefab = stormHead;
                            break;
                        case "toucher":
                            data.enemyPrefab = toucher;
                            break;
                        default:
                            Debug.LogWarning($"Prefab not found for enemy: {data.enemyName}");
                            break;
                    }
                    EnemyDataList.Add(data);
                }
            }
        }

        // Phương thức lấy dữ liệu enemy theo tên
        public EnemyDataBase GetEnemyDataByName(string enemyName)
        {
            return EnemyDataList.Find(data => data.enemyName == enemyName);
        }
        
        
        // Hàm chọn danh sách quái vật dựa trên maxPower
        public List<(EnemyDataBase Data, int Quantity)> SelectMonstersByPower(int maxPower)
        {
            int monterPowerReturn = 0;
            var selectedMonsters = new List<(EnemyDataBase Data, int Quantity)>();

            var sortedEnemies = EnemyDataList.OrderByDescending(e => e.power).ToList();
            int index = 0;

            while (monterPowerReturn < maxPower)
            {
                var enemy = sortedEnemies[index % sortedEnemies.Count];

                if (monterPowerReturn + enemy.power <= maxPower)
                {
                    monterPowerReturn += enemy.power;

                    var existingMonster = selectedMonsters.FirstOrDefault(m => m.Data == enemy);
                    if (existingMonster != default)
                    {
                        int i = selectedMonsters.FindIndex(m => m.Data == enemy);
                        selectedMonsters[i] = (enemy, existingMonster.Quantity + 1);
                    }
                    else
                    {
                        selectedMonsters.Add((enemy, 1));
                    }
                }

                index++;
            }

            // Disable internal logging as the caller handles it now
            // DisplaySelectedMonsters(selectedMonsters);
            return selectedMonsters;
        }
        public void DisplaySelectedMonsters(List<(EnemyDataBase Data, int Quantity)> selectedMonsters)
        {
            foreach (var monster in selectedMonsters)
            {
                Debug.Log($"Enemy: {monster.Data.enemyName}, Quantity: {monster.Quantity}");
            }
        }

    }
}

E:\unity\Crab_farm\Assets\Art\Resources\DataConfig\EnemyDataConfig.json
có nội dung {
    "enemies": [
        {
            "id": 2,
            "enemyName": "circleHead",
            "attackSpeed": 1,
            "dame": 1,
            "hp": 2,
            "speed": 1,
            "description": "Sạc không dây di động,giúp tăng tốc độ tích năng lượng cho shieldBot gần đó ",
            "power": 1
        }
    ]
}