# 📋 ARCHER IMPLEMENTATION GUIDE - Kingdom Two Crowns Style

## 🎯 MỤC TIÊU DOCUMENT
Tài liệu này hướng dẫn chi tiết implementation **Archer State Machine** theo phong cách Kingdom Two Crowns, với focus vào:
- ✅ State machine hoàn chỉnh cho Archer
- ✅ Integration với WallAndStumpManager & GuardManager
- ✅ Test cases cụ thể cho từng state
- ⚠️ **KHÔNG** implement logic bắn đạn (chỉ animation + Debug.Log)

---

## 🏗️ KIẾN TRÚC TỔNG QUAN

### Archer Role Clarification
```
QUAN TRỌNG: Archer chỉ có 1 JOB duy nhất là "Archer"
- Ban ngày: State = Hunting (đi săn)
- Ban đêm: State = Guard states (canh tường)
- KHÔNG có role "Hunter" và "Guard" riêng biệt!
```

### State Machine Overview
```
┌─────────────────────────────────────────────────────┐
│                  ARCHER STATE MACHINE                │
├─────────────────────────────────────────────────────┤
│                                                      │
│  Idle (Entry Point)                                 │
│    │                                                 │
│    ├─> IsDaytime? ──YES──> Hunting                  │
│    │                                                 │
│    └─> IsNighttime? ──YES──> HoldingWall            │
│                                                      │
│  Hunting (Ban ngày săn bắn)                         │
│    │                                                 │
│    ├─> Detect Enemy ──> AttackingFromWall           │
│    ├─> Detect Animal ──> AttackingFromWall          │
│    └─> Night Falls ──> ReturningToBase              │
│                                                      │
│  HoldingWall (Đứng canh tường)                      │
│    │                                                 │
│    ├─> Detect Enemy ──> AttackingFromWall           │
│    ├─> Wall HP < 10 ──> RetreatingToInnerWall      │
│    └─> Day Breaks ──> Hunting                       │
│                                                      │
│  AttackingFromWall (Đang tấn công)                  │
│    │                                                 │
│    ├─> Target Lost ──> Previous State               │
│    ├─> Target Dead ──> Previous State               │
│    └─> Wall HP < 10 ──> RetreatingToInnerWall      │
│                                                      │
│  RetreatingToInnerWall (Rút lui về wall phía sau)   │
│    │                                                 │
│    ├─> Reached Wall ──> HoldingWall                 │
│    └─> No Wall Behind ──> LastStandDefense          │
│                                                      │
│  ReturningToBase (Quay về base ban đêm)             │
│    │                                                 │
│    └─> Reached Wall ──> HoldingWall                 │
│                                                      │
│  LastStandDefense (Tử chiến tại chỗ)                │
│    │                                                 │
│    └─> Fight until death                            │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

## 📦 CẤU TRÚC ARCHERSTATUS.CS (UPDATED)

### Enum States
```csharp
public enum ArcherState
{
    Idle,                    // Entry point - check day/night
    Hunting,                 // Ban ngày đi săn
    HoldingWall,             // Đứng canh tường
    AttackingFromWall,       // Đang tấn công từ vị trí
    RetreatingToInnerWall,   // Rút lui về wall phía sau
    ReturningToBase,         // Quay về base (ban đêm)
    LastStandDefense         // Tử chiến (không còn wall)
}
```

### Required Variables (Thêm vào ArcherStatus)
```csharp
[Header("Combat Settings")]
public float attackRange = 8f;         // Tầm bắn (phải <= detectRange)
public float detectRange = 10f;        // Tầm phát hiện (dùng cho RayCast)
public float attackCooldown = 2.0f;    // Cooldown giữa các lần bắn
public float lastAttackTime = 0f;      // Thời điểm bắn lần cuối

[Header("Movement Settings")]
public float walkSpeed = 1.0f;
public float runSpeed = 2.5f;
public float huntingMoveSpeed = 1.2f;  // Tốc độ khi hunting
public float currentSpeed = 0f;

[Header("Wall Defense Settings")]
public float standBehindWallOffset = 0.8f; // Khoảng cách đứng sau wall
public GameObject currentTarget;           // Target hiện tại
public GameObject assignedWall;            // Wall được assign
public GameObject previousWall;            // Wall trước đó (cho retreat)

[Header("Flank Assignment")]
public bool isLeftFlank = false;  // false = phải, true = trái

[Header("State Management")]
public ArcherState currentState = ArcherState.Idle;
public ArcherState previousState = ArcherState.Idle; // Lưu state trước đó
```

---

## 🎮 ARCHERBEHAVIOR.CS - STATE MACHINE IMPLEMENTATION

### 1. Required Components & References
```csharp
public class ArcherBehavior : MonoBehaviour, INPCBehavior, ISaveable, IHealth, ICoinShield
{
    // References
    private ArcherStatus status;
    private RayCastHelper rayHelper;
    private Animator animator;
    
    // Managers
    private GameTimeManager timeManager;
    private WallAndStumpManager wallManager;
    private GuardManager guardManager;
    
    private void Awake()
    {
        status = GetComponent<ArcherStatus>();
        rayHelper = GetComponent<RayCastHelper>();
        animator = GetComponent<Animator>();
        
        // Setup RayCast
        InitializeRayCast();
    }
    
    private void Start()
    {
        // Get managers
        timeManager = GameTimeManager.Instance;
        wallManager = WallAndStumpManager.Instance;
        guardManager = GuardManager.Instance;
        
        // Register to GuardManager
        if (guardManager != null)
        {
            guardManager.RegisterArcher(gameObject);
        }
        
        // Subscribe to time events
        SubscribeToTimeEvents();
        
        // Initial state
        status.currentState = ArcherState.Idle;
    }
    
    private void InitializeRayCast()
    {
        // Setup tags to detect
        rayHelper.tagsToDetect.Clear();
        rayHelper.tagsToDetect.Add("Enemy");
        
        // TODO: Add hunting targets from PrefabData
        // rayHelper.tagsToDetect.Add("Animal");
        // rayHelper.tagsToDetect.Add("Deer");
        
        // Set ray length (detectRange + buffer)
        rayHelper.SetRayLength(status.detectRange + 1.5f);
    }
    
    private void SubscribeToTimeEvents()
    {
        if (timeManager != null)
        {
            // 18:00 - Chuyển sang ban đêm
            timeManager.SubscribeToHourlyEvent(18, OnNightFalls);
            
            // 06:00 - Chuyển sang ban ngày
            timeManager.SubscribeToHourlyEvent(6, OnDayBreaks);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe time events
        if (timeManager != null)
        {
            timeManager.UnsubscribeFromHourlyEvent(18, OnNightFalls);
            timeManager.UnsubscribeFromHourlyEvent(6, OnDayBreaks);
        }
        
        // Unregister from GuardManager
        if (guardManager != null)
        {
            guardManager.UnregisterArcher(gameObject);
        }
    }
}
```

### 2. Main Update Loop
```csharp
public void UpdateBehavior()
{
    // Update ray detection every frame
    rayHelper.DrawRay();
    
    // State machine
    switch (status.currentState)
    {
        case ArcherState.Idle:
            HandleIdleState();
            break;
            
        case ArcherState.Hunting:
            HandleHuntingState();
            break;
            
        case ArcherState.HoldingWall:
            HandleHoldingWallState();
            break;
            
        case ArcherState.AttackingFromWall:
            HandleAttackingFromWallState();
            break;
            
        case ArcherState.RetreatingToInnerWall:
            HandleRetreatingToInnerWallState();
            break;
            
        case ArcherState.ReturningToBase:
            HandleReturningToBaseState();
            break;
            
        case ArcherState.LastStandDefense:
            HandleLastStandDefenseState();
            break;
    }
}
```

### 3. State Handlers

#### 3.1 HandleIdleState()
```csharp
private void HandleIdleState()
{
    // Check time of day and transition
    if (timeManager != null && timeManager.IsDaytime)
    {
        Debug.Log("[Archer] Day time detected, switching to Hunting");
        TransitionToState(ArcherState.Hunting);
    }
    else
    {
        Debug.Log("[Archer] Night time detected, switching to HoldingWall");
        TransitionToState(ArcherState.ReturningToBase);
    }
}
```

#### 3.2 HandleHuntingState()
```csharp
private void HandleHuntingState()
{
    // PRIORITY 1: Check for enemies/animals in detection range
    var detectedObjects = rayHelper.GetHitObjects();
    
    if (detectedObjects.Count > 0)
    {
        GameObject target = GetPriorityTarget(detectedObjects);
        
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            
            // Check if target is within attack range
            if (distance <= status.attackRange)
            {
                status.currentTarget = target;
                Debug.Log($"[Archer] Target detected: {target.name} at distance {distance:F2}");
                TransitionToState(ArcherState.AttackingFromWall);
                return;
            }
        }
    }
    
    // PRIORITY 2: Continue hunting movement
    MoveForwardHunting();
    
    // Play hunting animation
    if (animator != null)
    {
        animator.Play("archerMove");
    }
}

private void MoveForwardHunting()
{
    // Move forward based on facing direction
    float direction = transform.localScale.x > 0 ? 1f : -1f;
    
    Vector3 movement = new Vector3(direction * status.huntingMoveSpeed * Time.deltaTime, 0, 0);
    transform.position += movement;
    
    status.currentSpeed = status.huntingMoveSpeed;
}

private GameObject GetPriorityTarget(List<GameObject> detectedObjects)
{
    // Priority: Enemy > Animal
    GameObject enemyTarget = null;
    GameObject animalTarget = null;
    
    foreach (var obj in detectedObjects)
    {
        if (obj.CompareTag("Enemy"))
        {
            enemyTarget = obj;
            break; // Enemy has highest priority
        }
        // TODO: Uncomment when animal system is implemented
        // else if (obj.CompareTag("Animal") || obj.CompareTag("Deer"))
        // {
        //     animalTarget = obj;
        // }
    }
    
    return enemyTarget ?? animalTarget;
}
```

#### 3.3 HandleHoldingWallState()
```csharp
private void HandleHoldingWallState()
{
    // PRIORITY 1: Check wall HP
    if (status.assignedWall != null)
    {
        var wallStatus = status.assignedWall.GetComponent<WallStatus>();
        if (wallStatus != null && wallStatus.currentHp < 10)
        {
            Debug.Log($"[Archer] Wall HP critical! {wallStatus.currentHp}/10, retreating!");
            TransitionToState(ArcherState.RetreatingToInnerWall);
            return;
        }
    }
    
    // PRIORITY 2: Check for enemies
    var detectedObjects = rayHelper.GetHitObjects();
    if (detectedObjects.Count > 0)
    {
        GameObject target = GetPriorityTarget(detectedObjects);
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance <= status.attackRange)
            {
                status.currentTarget = target;
                Debug.Log($"[Archer] Enemy detected while holding wall: {target.name}");
                TransitionToState(ArcherState.AttackingFromWall);
                return;
            }
        }
    }
    
    // PRIORITY 3: Move to wall position if not there yet
    if (status.assignedWall == null)
    {
        AssignWallPosition();
    }
    
    if (status.assignedWall != null)
    {
        Vector3 targetPosition = GetWallDefensePosition(status.assignedWall);
        float distanceToWall = Vector2.Distance(transform.position, targetPosition);
        
        if (distanceToWall > 0.2f)
        {
            MoveTowardsPosition(targetPosition);
            if (animator != null) animator.Play("archerMove");
        }
        else
        {
            // Reached position, face outward
            status.currentSpeed = 0f;
            FaceOutward();
            if (animator != null) animator.Play("archerIdle");
        }
    }
}

private void AssignWallPosition()
{
    if (wallManager != null)
    {
        status.assignedWall = wallManager.GetFarthestWall(status.isLeftFlank);
        if (status.assignedWall != null)
        {
            Debug.Log($"[Archer] Assigned to wall: {status.assignedWall.name} on {(status.isLeftFlank ? "LEFT" : "RIGHT")} flank");
        }
        else
        {
            Debug.LogWarning("[Archer] No wall available for assignment!");
        }
    }
}

private Vector3 GetWallDefensePosition(GameObject wall)
{
    float direction = status.isLeftFlank ? 1f : -1f;
    return new Vector3(
        wall.transform.position.x + direction * status.standBehindWallOffset,
        wall.transform.position.y,
        transform.position.z
    );
}

private void FaceOutward()
{
    // Face away from town center (outward)
    float scaleX = status.isLeftFlank ? -1f : 1f;
    transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
}
```

#### 3.4 HandleAttackingFromWallState()
```csharp
private void HandleAttackingFromWallState()
{
    // PRIORITY 1: Check if wall needs retreating
    if (status.assignedWall != null)
    {
        var wallStatus = status.assignedWall.GetComponent<WallStatus>();
        if (wallStatus != null && wallStatus.currentHp < 10)
        {
            Debug.Log($"[Archer] Wall critical during attack! Retreating!");
            status.currentTarget = null;
            TransitionToState(ArcherState.RetreatingToInnerWall);
            return;
        }
    }
    
    // PRIORITY 2: Validate target
    if (status.currentTarget == null || !status.currentTarget.activeInHierarchy)
    {
        Debug.Log("[Archer] Target lost, returning to previous state");
        status.currentTarget = null;
        ReturnToPreviousState();
        return;
    }
    
    // PRIORITY 3: Check target distance
    float distance = Vector2.Distance(transform.position, status.currentTarget.transform.position);
    
    if (distance > status.attackRange)
    {
        Debug.Log($"[Archer] Target moved out of range ({distance:F2} > {status.attackRange})");
        status.currentTarget = null;
        ReturnToPreviousState();
        return;
    }
    
    // PRIORITY 4: Face target
    FaceTarget(status.currentTarget.transform.position);
    
    // PRIORITY 5: Attack on cooldown
    if (Time.time - status.lastAttackTime >= status.attackCooldown)
    {
        PerformAttack();
    }
    
    // Stay in idle animation between attacks
    if (animator != null && Time.time - status.lastAttackTime > 0.5f)
    {
        animator.Play("archerIdle");
    }
}

private void PerformAttack()
{
    // ⚠️ IMPORTANT: NO PROJECTILE LOGIC YET
    // This is just animation + debug for testing
    
    Debug.Log($"[Archer] >>> ATTACK <<< Target: {status.currentTarget.name} at position {status.currentTarget.transform.position}");
    
    // Play draw animation
    if (animator != null)
    {
        animator.Play("archerDraw");
    }
    
    // Update attack time
    status.lastAttackTime = Time.time;
    
    // TODO: Implement projectile system later
    // For now, just visual feedback
}

// Animation Event Callback (called from archerDraw animation)
public void OnDrawComplete()
{
    Debug.Log("[Archer] Draw animation complete");
    
    // Play release animation
    if (animator != null)
    {
        animator.Play("archerRelease");
    }
}

// Animation Event Callback (called from archerRelease animation)
public void OnArrowRelease()
{
    Debug.Log("[Archer] *** ARROW RELEASED *** (No projectile spawned yet)");
    
    // TODO: Spawn projectile here when projectile system is ready
    // Example:
    // GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
    // arrow.GetComponent<Projectile>().Initialize(status.currentTarget);
}

private void FaceTarget(Vector3 targetPosition)
{
    float direction = targetPosition.x > transform.position.x ? 1f : -1f;
    transform.localScale = new Vector3(direction, transform.localScale.y, transform.localScale.z);
}
```

#### 3.5 HandleRetreatingToInnerWallState()
```csharp
private void HandleRetreatingToInnerWallState()
{
    // Check if there's a wall to retreat to
    if (status.previousWall == null && status.assignedWall != null)
    {
        status.previousWall = status.assignedWall;
    }
    
    // Get wall behind current position
    GameObject wallBehind = wallManager.GetWallBehind(status.previousWall);
    
    if (wallBehind == null)
    {
        Debug.Log("[Archer] NO WALL BEHIND! Entering LastStandDefense!");
        TransitionToState(ArcherState.LastStandDefense);
        return;
    }
    
    // Move to wall behind
    Vector3 targetPosition = GetWallDefensePosition(wallBehind);
    float distance = Vector2.Distance(transform.position, targetPosition);
    
    if (distance > 0.5f)
    {
        MoveTowardsPosition(targetPosition);
        if (animator != null) animator.Play("archerMove");
    }
    else
    {
        // Reached new wall
        status.assignedWall = wallBehind;
        status.previousWall = null;
        Debug.Log($"[Archer] Reached retreat wall: {wallBehind.name}");
        TransitionToState(ArcherState.HoldingWall);
    }
}
```

#### 3.6 HandleReturningToBaseState()
```csharp
private void HandleReturningToBaseState()
{
    // Find nearest wall
    if (status.assignedWall == null)
    {
        GameObject nearestWall = wallManager.GetNearestWall(transform.position);
        
        if (nearestWall != null)
        {
            status.assignedWall = nearestWall;
            Debug.Log($"[Archer] Returning to nearest wall: {nearestWall.name}");
        }
        else
        {
            Debug.LogWarning("[Archer] No wall found to return to!");
            TransitionToState(ArcherState.Idle);
            return;
        }
    }
    
    // Move to wall
    Vector3 targetPosition = GetWallDefensePosition(status.assignedWall);
    float distance = Vector2.Distance(transform.position, targetPosition);
    
    if (distance > 0.5f)
    {
        MoveTowardsPosition(targetPosition);
        if (animator != null) animator.Play("archerMove");
    }
    else
    {
        Debug.Log("[Archer] Reached base wall");
        TransitionToState(ArcherState.HoldingWall);
    }
}
```

#### 3.7 HandleLastStandDefenseState()
```csharp
private void HandleLastStandDefenseState()
{
    // Stand ground and fight to the death
    Debug.Log("[Archer] LAST STAND - Fighting to the death!");
    
    // Stop all movement
    status.currentSpeed = 0f;
    
    // Check for enemies continuously
    var detectedObjects = rayHelper.GetHitObjects();
    
    if (detectedObjects.Count > 0)
    {
        GameObject target = GetPriorityTarget(detectedObjects);
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance <= status.attackRange)
            {
                status.currentTarget = target;
                FaceTarget(target.transform.position);
                
                // Attack on cooldown
                if (Time.time - status.lastAttackTime >= status.attackCooldown)
                {
                    PerformAttack();
                }
            }
        }
    }
    else
    {
        // No enemies, just stand idle
        if (animator != null) animator.Play("archerIdle");
    }
}
```

### 4. Helper Methods
```csharp
private void MoveTowardsPosition(Vector3 targetPosition)
{
    Vector3 direction = (targetPosition - transform.position).normalized;
    transform.position += direction * status.runSpeed * Time.deltaTime;
    status.currentSpeed = status.runSpeed;
    
    // Face movement direction
    if (direction.x != 0)
    {
        float scaleX = direction.x > 0 ? 1f : -1f;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
    }
}

private void TransitionToState(ArcherState newState)
{
    if (status.currentState == newState) return;
    
    Debug.Log($"[Archer] State transition: {status.currentState} -> {newState}");
    
    status.previousState = status.currentState;
    status.currentState = newState;
    
    // State entry logic
    OnStateEnter(newState);
}

private void OnStateEnter(ArcherState state)
{
    switch (state)
    {
        case ArcherState.Hunting:
            status.currentSpeed = status.huntingMoveSpeed;
            break;
            
        case ArcherState.HoldingWall:
            status.currentSpeed = 0f;
            break;
            
        case ArcherState.AttackingFromWall:
            status.currentSpeed = 0f;
            break;
            
        case ArcherState.ReturningToBase:
            status.currentSpeed = status.runSpeed;
            break;
            
        case ArcherState.RetreatingToInnerWall:
            status.currentSpeed = status.runSpeed;
            break;
    }
}

private void ReturnToPreviousState()
{
    // Return to previous state (Hunting or HoldingWall)
    if (status.previousState == ArcherState.Hunting || 
        status.previousState == ArcherState.HoldingWall)
    {
        TransitionToState(status.previousState);
    }
    else
    {
        // Default fallback
        TransitionToState(ArcherState.Idle);
    }
}
```

### 5. Time Event Handlers
```csharp
private void OnNightFalls()
{
    Debug.Log("[Archer] NIGHT FALLS - Returning to base");
    
    // All archers return to walls at night
    if (status.currentState == ArcherState.Hunting)
    {
        TransitionToState(ArcherState.ReturningToBase);
    }
}

private void OnDayBreaks()
{
    Debug.Log("[Archer] DAY BREAKS - Starting to hunt");
    
    // All archers start hunting during day
    if (status.currentState == ArcherState.HoldingWall || 
        status.currentState == ArcherState.ReturningToBase)
    {
        TransitionToState(ArcherState.Hunting);
    }
}
```

### 6. Damage Handling (IHealth Implementation)
```csharp
public bool TakeDamage(int damage, GameObject whoDoDamage)
{
    Debug.Log($"[Archer] Taking {damage} damage from {whoDoDamage?.name}");
    
    // IMPORTANT: Wait for next UpdateBehavior() to react
    // Do NOT change state immediately
    
    // Coin shield first
    if (HasCoinShield && currentCoins > 0)
    {
        ConsumeCoinShield();
        if (animator != null) animator.Play("archerHit");
        
        // Notify GuardManager about attack
        if (guardManager != null)
        {
            guardManager.RaiseAlarm(whoDoDamage);
        }
        
        return true;
    }
    
    // HP damage
    if (HasHp)
    {
        currentHp -= damage;
        if (animator != null) animator.Play("archerHit");
        
        // Notify GuardManager
        if (guardManager != null)
        {
            guardManager.RaiseAlarm(whoDoDamage);
        }
        
        if (currentHp <= 0)
        {
            Die(whoDoDamage);
            return false;
        }
        
        return true;
    }
    
    return true;
}

private void Die(GameObject killer)
{
    Debug.Log($"[Archer] DIED - Killed by {killer?.name}");
    
    if (animator != null)
    {
        animator.Play("archerDie");
    }
    
    // Unregister from managers
    if (guardManager != null)
    {
        guardManager.UnregisterArcher(gameObject);
    }
    
    // Drop coins if any
    if (currentCoins > 0)
    {
        // TODO: Spawn coin drops
    }
    
    // Destroy after animation
    Destroy(gameObject, 1.5f);
}
```

---

## 🛡️ GUARDMANAGER.CS - NEW COMPONENT

### Purpose
GuardManager quản lý tập thể các archer trong chế độ phòng thủ, bao gồm:
- Đăng ký/hủy đăng ký archer
- Phân chia archer vào 2 bên (left/right flank)
- Raise alarm khi có enemy xuất hiện
- Điều phối retreat khi wall bị phá

### Implementation
```csharp
using System.Collections.Generic;
using UnityEngine;
using Script.Manager.WorldManager;

namespace Script.Game.Mobs.MobsManager
{
    public class GuardManager : MonoBehaviour
    {
        public static GuardManager Instance { get; private set; }
        
        [Header("Archer Management")]
        public List<GameObject> leftFlankArchers = new List<GameObject>();
        public List<GameObject> rightFlankArchers = new List<GameObject>();
        
        [Header("Alarm System")]
        public bool isAlarmActive = false;
        public GameObject alarmSource; // Enemy that triggered alarm
        
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
        }
        
        #region Archer Registration
        public void RegisterArcher(GameObject archer)
        {
            if (archer == null) return;
            
            var behavior = archer.GetComponent<ArcherBehavior>();
            if (behavior == null) return;
            
            var status = archer.GetComponent<ArcherStatus>();
            if (status == null) return;
            
            // Assign to flank based on current position or next available
            if (leftFlankArchers.Count <= rightFlankArchers.Count)
            {
                leftFlankArchers.Add(archer);
                status.isLeftFlank = true;
                Debug.Log($"[GuardManager] Registered {archer.name} to LEFT flank ({leftFlankArchers.Count} total)");
            }
            else
            {
                rightFlankArchers.Add(archer);
                status.isLeftFlank = false;
                Debug.Log($"[GuardManager] Registered {archer.name} to RIGHT flank ({rightFlankArchers.Count} total)");
            }
        }
        
        public void UnregisterArcher(GameObject archer)
        {
            if (archer == null) return;
            
            if (leftFlankArchers.Contains(archer))
            {
                leftFlankArchers.Remove(archer);
                Debug.Log($"[GuardManager] Unregistered {archer.name} from LEFT flank");
            }
            else if (rightFlankArchers.Contains(archer))
            {
                rightFlankArchers.Remove(archer);
                Debug.Log($"[GuardManager] Unregistered {archer.name} from RIGHT flank");
            }
        }
        #endregion
        
        #region Alarm System
        public void RaiseAlarm(GameObject enemy)
        {
            if (enemy == null) return;
            
            if (!isAlarmActive)
            {
                isAlarmActive = true;
                alarmSource = enemy;
                Debug.Log($"[GuardManager] 🚨 ALARM RAISED! Enemy: {enemy.name}");
                
                // Notify all archers to return to walls
                NotifyAllArchersReturnToWalls();
            }
        }
        
        public void ClearAlarm()
        {
            isAlarmActive = false;
            alarmSource = null;
            Debug.Log("[GuardManager] Alarm cleared");
        }
        
        private void NotifyAllArchersReturnToWalls()
        {
            // Left flank archers
            foreach (var archer in leftFlankArchers)
            {
                if (archer == null) continue;
                
                var behavior = archer.GetComponent<ArcherBehavior>();
                var status = archer.GetComponent<ArcherStatus>();
                
                if (behavior != null && status != null)
                {
                    // Only transition if currently hunting
                    if (status.currentState == ArcherStatus.ArcherState.Hunting)
                    {
                        Debug.Log($"[GuardManager] Ordering {archer.name} to return to wall");
                        // NOTE: State will change in next UpdateBehavior() call
                        // We set a flag or call a method that will be checked
                        // For now, they will naturally transition when they detect enemy
                    }
                }
            }
            
            // Right flank archers
            foreach (var archer in rightFlankArchers)
            {
                if (archer == null) continue;
                
                var behavior = archer.GetComponent<ArcherBehavior>();
                var status = archer.GetComponent<ArcherStatus>();
                
                if (behavior != null && status != null)
                {
                    if (status.currentState == ArcherStatus.ArcherState.Hunting)
                    {
                        Debug.Log($"[GuardManager] Ordering {archer.name} to return to wall");
                    }
                }
            }
        }
        #endregion
        
        #region Wall Management Integration
        // Called by WallAndStumpManager when wall HP < 10
        public void NotifyWallCritical(GameObject wall, bool isLeftSide)
        {
            Debug.Log($"[GuardManager] ⚠️ WALL CRITICAL: {wall.name} on {(isLeftSide ? "LEFT" : "RIGHT")} side");
            
            List<GameObject> affectedArchers = isLeftSide ? leftFlankArchers : rightFlankArchers;
            
            foreach (var archer in affectedArchers)
            {
                if (archer == null) continue;
                
                var status = archer.GetComponent<ArcherStatus>();
                if (status != null && status.assignedWall == wall)
                {
                    Debug.Log($"[GuardManager] Notifying {archer.name} that their wall is critical");
                    // Archer will handle retreat in next UpdateBehavior()
                }
            }
        }
        #endregion
        
        #region Debug Info
        public int GetTotalArchers()
        {
            return leftFlankArchers.Count + rightFlankArchers.Count;
        }
        
        public void DebugPrintArcherDistribution()
        {
            Debug.Log($"[GuardManager] Archer Distribution - Left: {leftFlankArchers.Count}, Right: {rightFlankArchers.Count}");
        }
        #endregion
    }
}
```

---

## 🏰 WALLANDSTUMPMANAGER.CS - MODIFICATIONS NEEDED

### Add Critical HP Notification
```csharp
// Add to WallAndStumpManager.cs

[Header("Critical HP Settings")]
public int criticalHPThreshold = 10;

// Add this method
public void CheckWallCriticalHP(GameObject wall)
{
    if (wall == null) return;
    
    var wallStatus = wall.GetComponent<WallStatus>();
    if (wallStatus == null) return;
    
    if (wallStatus.currentHp < criticalHPThreshold)
    {
        // Determine which side
        float wallX = wall.transform.position.x;
        float townCenterX = townCenter.transform.position.x;
        bool isLeftSide = wallX < townCenterX;
        
        // Notify GuardManager
        if (GuardManager.Instance != null)
        {
            GuardManager.Instance.NotifyWallCritical(wall, isLeftSide);
        }
        
        // Mark wall as non-defendable (optional)
        // This prevents new archers from being assigned here
    }
}

// Call this method from WallBehavior when HP changes
```

### Add to WallBehavior.cs
```csharp
// In WallBehavior.cs, when HP is reduced:

public void TakeDamage(int damage)
{
    status.currentHp -= damage;
    
    // Check if critical
    if (WallAndStumpManager.Instance != null)
    {
        WallAndStumpManager.Instance.CheckWallCriticalHP(gameObject);
    }
    
    if (status.currentHp <= 0)
    {
        DestroyWall();
    }
}
```

---

## 📝 PREFABDATA HUNTING TARGETS (TODO)

### Create PrefabData.cs (if not exists)
```csharp
using System.Collections.Generic;
using UnityEngine;

public class PrefabData : MonoBehaviour
{
    public static PrefabData Instance { get; private set; }
    
    [Header("Hunting Target Tags")]
    public List<string> huntingTargetTags = new List<string>()
    {
        "Animal",
        "Deer",
        "Rabbit",
        "Boar"
    };
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

### Use in ArcherBehavior.cs
```csharp
private void InitializeRayCast()
{
    rayHelper.tagsToDetect.Clear();
    rayHelper.tagsToDetect.Add("Enemy");
    
    // Add hunting targets from PrefabData
    if (PrefabData.Instance != null)
    {
        foreach (string tag in PrefabData.Instance.huntingTargetTags)
        {
            if (!rayHelper.tagsToDetect.Contains(tag))
            {
                rayHelper.tagsToDetect.Add(tag);
            }
        }
    }
    
    rayHelper.SetRayLength(status.detectRange + 1.5f);
}
```

---

## 🧪 TEST CASES

### Test Case 1: Day/Night Transition
```
SETUP:
- Spawn 2 archers (1 assigned to left, 1 to right)
- Set time to 12:00 (day time)
- Build 1 wall on each side

EXPECTED BEHAVIOR:
1. At 6:00: Both archers should be in Hunting state
2. Archers walk forward  (normalSpeed)
3. At 18:00: Both archers transition to ReturningToBase
4. Archers run to nearest wall (runSpeed)
5. Archers reach wall and enter HoldingWall state
6. At 06:00: Both archers transition back to Hunting

PASS CRITERIA:
✅ State changes happen at exact time (18:00 and 06:00)
✅ Animation changes (archerMove during hunt, archerIdle when holding)
✅ Debug logs show state transitions
✅ Archers position correctly behind walls
```

### Test Case 2: Archer Distribution Between Flanks
```
SETUP:
- Spawn 6 archers sequentially
- No walls initially

EXPECTED BEHAVIOR:
1. Archer 1: Assigned to LEFT (count: 1L, 0R)
2. Archer 2: Assigned to RIGHT (count: 1L, 1R)
3. Archer 3: Assigned to LEFT (count: 2L, 1R)
4. Archer 4: Assigned to RIGHT (count: 2L, 2R)
5. Archer 5: Assigned to LEFT (count: 3L, 2R)
6. Archer 6: Assigned to RIGHT (count: 3L, 3R)

PASS CRITERIA:
✅ Archers distributed evenly (3 left, 3 right)
✅ Debug logs show correct flank assignment
✅ ArcherStatus.isLeftFlank set correctly
✅ GuardManager lists show correct counts
```

### Test Case 3: Enemy Detection and Attack
```
SETUP:
- 1 archer in Hunting state
- Spawn enemy 5 units ahead of archer
- attackRange = 8, detectRange = 10

EXPECTED BEHAVIOR:
1. Archer walks forward in Hunting
2. RayCast detects enemy when distance < 10
3. Distance check: 5 < 8 (attackRange) ✅
4. Archer transitions to AttackingFromWall
5. Archer faces enemy
6. Every 2 seconds: archerDraw → archerRelease animation
7. Debug log: ">>> ATTACK <<< Target: [enemy name]"
8. If enemy dies: archer returns to Hunting

PASS CRITERIA:
✅ State transitions correctly
✅ Attack cooldown works (2 second intervals)
✅ Animations play in sequence
✅ Debug logs show target info
✅ NO projectile spawned (commented out)
```

### Test Case 4: Wall HP Critical - Retreat
```
SETUP:
- 2 archers on left flank
- 2 walls on left side (Wall A farther, Wall B closer to town)
- Both archers assigned to Wall A
- Set Wall A HP = 15

EXPECTED BEHAVIOR:
1. Archers holding Wall A in HoldingWall state
2. Manually reduce Wall A HP to 9
3. WallBehavior calls WallAndStumpManager.CheckWallCriticalHP()
4. GuardManager receives NotifyWallCritical(Wall A, true)
5. Both archers detect HP < 10 in HandleHoldingWallState()
6. Both archers transition to RetreatingToInnerWall
7. Archers run to Wall B (GetWallBehind returns Wall B)
8. Archers reach Wall B and enter HoldingWall

PASS CRITERIA:
✅ State transitions when HP < 10
✅ Archers retreat to correct wall (behind current)
✅ Movement speed = runSpeed during retreat
✅ Debug logs show retreat notifications
✅ Archers reassign to Wall B
```

### Test Case 5: No Wall Behind - Last Stand
```
SETUP:
- 1 archer on left flank
- Only 1 wall on left side (Wall A)
- Archer assigned to Wall A
- Set Wall A HP = 15

EXPECTED BEHAVIOR:
1. Archer holding Wall A
2. Reduce Wall A HP to 9
3. Archer transitions to RetreatingToInnerWall
4. GetWallBehind(Wall A) returns null
5. Archer transitions to LastStandDefense
6. Archer stops moving (currentSpeed = 0)
7. Archer continues attacking enemies in range
8. Archer fights until death

PASS CRITERIA:
✅ Transition to LastStandDefense when no wall behind
✅ Debug log: "NO WALL BEHIND! Entering LastStandDefense!"
✅ Archer remains stationary
✅ Archer still attacks enemies
✅ Archer does not retreat further
```

### Test Case 6: Alarm System
```
SETUP:
- 4 archers (2 left, 2 right), all in Hunting state
- Spawn enemy near one archer

EXPECTED BEHAVIOR:
1. Archer 1 detects enemy via RayCast
2. Archer 1 takes damage (TakeDamage called)
3. TakeDamage calls GuardManager.RaiseAlarm(enemy)
4. GuardManager sets isAlarmActive = true
5. GuardManager.NotifyAllArchersReturnToWalls() called
6. Debug logs show alarm raised
7. Other archers in Hunting continue hunting
   (they'll transition when they detect enemy themselves)

PASS CRITERIA:
✅ Alarm raised on first damage
✅ GuardManager.isAlarmActive = true
✅ Debug log: "🚨 ALARM RAISED!"
✅ All archer damage calls same RaiseAlarm (should not spam)
```

### Test Case 7: Hunting Target Priority
```
SETUP:
- 1 archer in Hunting
- Spawn 1 animal (tag: "Animal") at distance 6
- Spawn 1 enemy (tag: "Enemy") at distance 7
- Both within detectRange (10)

EXPECTED BEHAVIOR:
1. RayCast detects both objects
2. GetPriorityTarget() returns Enemy (priority over Animal)
3. Archer transitions to AttackingFromWall
4. Archer attacks Enemy first
5. After Enemy dies, archer returns to Hunting
6. Archer then detects Animal
7. Archer attacks Animal

PASS CRITERIA:
✅ Enemy prioritized over Animal
✅ Correct target selected
✅ Debug logs show target priority
```

### Test Case 8: Animation Event Callbacks
```
SETUP:
- 1 archer attacking enemy
- Animation Events set up:
  - archerDraw frame 10: OnDrawComplete()
  - archerRelease frame 5: OnArrowRelease()

EXPECTED BEHAVIOR:
1. PerformAttack() called
2. animator.Play("archerDraw")
3. At frame 10: OnDrawComplete() triggered
4. Debug log: "Draw animation complete"
5. animator.Play("archerRelease")
6. At frame 5: OnArrowRelease() triggered
7. Debug log: "*** ARROW RELEASED *** (No projectile spawned yet)"

PASS CRITERIA:
✅ Animation callbacks fire correctly
✅ Debug logs appear in correct order
✅ No errors about missing animation events
✅ Sequence flows smoothly
```

---

## ⚙️ SETUP CHECKLIST

### Unity Inspector Setup
```
1. Create GuardManager GameObject in Scene
   ☐ Add GuardManager.cs component
   ☐ Singleton should initialize

2. Update GameManager or WorldManager
   ☐ Ensure GuardManager is referenced/initialized

3. Archer Prefab Setup
   ☐ Add ArcherStatus component
   ☐ Add ArcherBehavior component
   ☐ Add RayCastHelper component
   ☐ Configure RayCastHelper:
      - raycastDistance = 10
      - raycastOffsetY = 1
      - isDrawRay = true (for debugging)
   ☐ Setup Animator with controller
   ☐ Add Animation Events:
      - archerDraw → OnDrawComplete (frame 10)
      - archerRelease → OnArrowRelease (frame 5)

4. Wall Prefab Setup
   ☐ Add WallStatus component
   ☐ Add WallBehavior component
   ☐ Set maxHp = 50
   ☐ Set tier = 1

5. Scene Setup
   ☐ Place Town Center
   ☐ Build 2+ walls (1 left, 1 right minimum)
   ☐ Set GameTimeManager time to 12:00 for day testing
```

### Code Integration Checklist
```
☐ Create GuardManager.cs in Scripts/Manager/MobsManager/
☐ Update ArcherStatus.cs with new variables
☐ Update ArcherBehavior.cs with full state machine
☐ Update WallBehavior.cs with CheckWallCriticalHP call
☐ Update WallAndStumpManager.cs with CheckWallCriticalHP method
☐ Create PrefabData.cs (or update existing) with huntingTargetTags
☐ Update DenizenController.ApplyRole() to call GuardManager.RegisterArcher()
☐ Ensure GameTimeManager.IsDaytime property is public
```

---

## 🚨 IMPORTANT NOTES

### 1. Projectile System - NOT IMPLEMENTED
```
⚠️ ALL SHOOTING LOGIC IS PLACEHOLDER:
- PerformAttack() only plays animations
- OnArrowRelease() only logs debug message
- NO GameObject instantiation
- NO physics calculations
- NO damage dealing to target

TODO WHEN READY:
1. Create Arrow prefab with Projectile component
2. Add arrowSpawnPoint Transform to Archer
3. Implement parabolic trajectory calculation
4. Add damage on collision
5. Add particle effects
```

### 2. Hunting Animals - NOT IMPLEMENTED
```
⚠️ ANIMAL HUNTING PLACEHOLDER:
- Tags are added to RayCast detection
- Priority system includes animals
- NO actual animal prefabs exist yet
- NO coin drops from animals

TODO WHEN READY:
1. Create Animal prefabs (Deer, Rabbit, etc.)
2. Add Animal health system
3. Implement coin drop on death
4. Add archer pickup behavior for dropped coins
```

### 3. State Transition Timing
```
⚠️ CRITICAL PATTERN:
- State changes happen in NEXT UpdateBehavior() frame
- TakeDamage() does NOT immediately change state
- Time events (day/night) DO immediately change state
- This matches Vagrant/Villager pattern
```

### 4. Wall Manager Integration
```
⚠️ REQUIRES WALL MANAGER UPDATE:
- Must call CheckWallCriticalHP() when wall takes damage
- Must maintain sorted lists (closest to farthest)
- Must handle wall destruction properly
- GetWallBehind() must return null if no wall behind
```

---

## 📊 EXPECTED DEBUG LOG FLOW

### Successful Day Cycle
```
[Archer] Day time detected, switching to Hunting
[Archer] State transition: Idle -> Hunting
[Archer] Target detected: Goblin at distance 7.50
[Archer] State transition: Hunting -> AttackingFromWall
[Archer] >>> ATTACK <<< Target: Goblin at position (15.0, 0.0, 0.0)
[Archer] Draw animation complete
[Archer] *** ARROW RELEASED *** (No projectile spawned yet)
[Archer] Target lost, returning to previous state
[Archer] State transition: AttackingFromWall -> Hunting
```

### Successful Night Transition
```
[Archer] NIGHT FALLS - Returning to base
[Archer] State transition: Hunting -> ReturningToBase
[Archer] Returning to nearest wall: Wall_Left_01
[Archer] Reached base wall
[Archer] State transition: ReturningToBase -> HoldingWall
[Archer] Assigned to wall: Wall_Left_01 on LEFT flank
```

### Successful Wall Retreat
```
[WallBehavior] Wall_Left_02 HP reduced to 9
[GuardManager] ⚠️ WALL CRITICAL: Wall_Left_02 on LEFT side
[Archer] Wall HP critical! 9/10, retreating!
[Archer] State transition: HoldingWall -> RetreatingToInnerWall
[Archer] Reached retreat wall: Wall_Left_01
[Archer] State transition: RetreatingToInnerWall -> HoldingWall
```

---

## 🎓 FINAL IMPLEMENTATION STEPS

### Phase 1: Basic State Machine (Week 1)
1. ✅ Implement Idle/Hunting/HoldingWall states
2. ✅ Test day/night transitions
3. ✅ Test archer spawning and flank distribution
4. ✅ Verify RayCast detection works

### Phase 2: Combat System (Week 2)
1. ✅ Implement AttackingFromWall state
2. ✅ Setup animation events
3. ✅ Test attack cooldown
4. ✅ Verify target priority system

### Phase 3: Defense System (Week 3)
1. ✅ Create GuardManager
2. ✅ Implement alarm system
3. ✅ Test wall retreat logic
4. ✅ Test last stand defense

### Phase 4: Polish (Week 4)
1. ✅ Fine-tune movement speeds
2. ✅ Balance attack ranges
3. ✅ Test edge cases
4. ✅ Add visual feedback

### Phase 5: Future (After Projectile System)
1. ⏳ Implement arrow projectiles
2. ⏳ Add animal hunting with loot
3. ⏳ Add coin pickup from hunted animals
4. ⏳ Add advanced AI behaviors

---

**END OF IMPLEMENTATION GUIDE**