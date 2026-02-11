using System;
using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Item;
using Script.Game.Structures.TownCenter;
using Script.Save_Controller;
using Script.Game.Mobs.MobsManager; 
using Script.Data_Model; 
using Script.Game.Item.Coin; 
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using Script.Game.Other;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager.WorldManager;
using Script.Manager; // For GuardManager access via GuardManager.Instance if needed

namespace Script.Game.Mobs.Subjects
{
    // Renamed from ArcherController to ArcherBehavior to reflect Behavior + Status architecture
    [RequireComponent(typeof(DenizenStatus))]
    [RequireComponent(typeof(RayCastHelper))]
    [RequireComponent(typeof(Animator))]
    public class ArcherBehavior : MonoBehaviour, IHealth, ICoinShield, INPCBehavior
    {
        #region Constants
        
        // Wall Defense Thresholds
        private const float WALL_ARRIVAL_DISTANCE = 0.5f;
        
        // Retreat Thresholds
        private const float RETREAT_COMPLETE_DISTANCE = 2.0f;
        
        // Combat Timing
        private const float ATTACK_READY_BUFFER = 0.1f;
        private const float TARGET_CHECK_INTERVAL = 0.2f;

        // Hunting
        // HUNTING_RANGE is now fetched from PrefabData
        private const float HUNTING_PATROL_INTERVAL = 5.0f;
        
        #endregion

        // References
        private DenizenStatus status;
        private RayCastHelper rayHelper;
        private Animator animator;
        
        // Managers
        private GameTimeManager timeManager;
        private WallAndStumpManager wallManager;
        private GuardManager guardManager;

        // State Tracking
        private bool isSubscribedToTimeEvents = false;
        private GameObject pendingTarget = null;
        private float lastTargetCheckTime = 0f;

        // Hunting specific
        private Vector3? currentHuntingTargetPos = null;
        private float lastHuntingMoveTime = 0f;
        
        // Caching
        private GameObject cachedRetreatTarget = null;
        
        [Header("Debug")]
        [SerializeField] private bool isDebugMovePos = false;

        // --- IHealth Interface Properties ---
        public bool HasCoinShield => status.CurrentCoins > 0;
        public bool HasHp => true;
        
        public int CurrentCoin
        {
            get => status.CurrentCoins;
            set => status.CurrentCoins = value;
        }

        public int MaxCoin
        {
            get => status.MaxCoins;
            set => status.MaxCoins = value;
        }

        public int CurrentHp
        {
            get => status.CurrentHp;
            set => status.CurrentHp = value;
        }

        public int MaxHp
        {
            get => status.MaxHp;
            set => status.MaxHp = value;
        }

        public bool IsDead => CurrentHp <= 0;

        private void Awake()
        {
            gameObject.tag = "Archer";
            status = GetComponent<DenizenStatus>();
            if (status == null)
            {
                status = gameObject.AddComponent<DenizenStatus>();
            }

            rayHelper = GetComponent<RayCastHelper>();
            if (rayHelper == null)
            {
               rayHelper = gameObject.AddComponent<RayCastHelper>();
            }
            
            animator = GetComponent<Animator>();
            
            InitializeRayCast();
        }
        


        private void Start()
        {
            // Get managers
            timeManager = GameTimeManager.Instance;
            wallManager = WallAndStumpManager.Instance;
            guardManager = GuardManager.Instance;
            
            // Check alarm state
            if (guardManager != null && guardManager.isEnemyAttack)
            {
                status.archerState = (DenizenStatus.ArcherState)DenizenStatus.ArcherState.ReturningToBase;
            }
            
            // Subscribe to time events
            SubscribeToTimeEvents();
            
            // Initial state check (only if not already set by logic above)
            if (timeManager != null && !guardManager.isEnemyAttack)
            {
                status.archerState = timeManager.IsDaytime ? DenizenStatus.ArcherState.Hunting : DenizenStatus.ArcherState.HoldingWall;
            }
        }
        
        private void OnEnable() // Re-subscribe when enabled (active behavior)
        {

             
             if (guardManager == null) guardManager = GuardManager.Instance;
             // Registration handled by DenizenRoleController
             
             SubscribeToTimeEvents();
        }
        
        private void OnDisable()
        {

             
             UnsubscribeFromTimeEvents();
             // Unregistration handled by DenizenRoleController
        }

        private void InitializeRayCast()
        {
            if (rayHelper == null) return;
            
            // Allow status references to be used, assuming they are set defaults in DenizenStatus or Inspector
            rayHelper.SetRayLength(status.detectRange + 1.5f);
        }
        
        private void SubscribeToTimeEvents()
        {
            if (isSubscribedToTimeEvents || timeManager == null) return;
            
            try
            {
                timeManager.SubscribeToHourlyEvent(18, OnNightFalls);
                timeManager.SubscribeToHourlyEvent(6, OnDayBreaks);
                isSubscribedToTimeEvents = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Archer] Failed to subscribe to time events: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromTimeEvents();
            // Unregistration handled by Controller
            CleanupReferences();
        }

        private void UnsubscribeFromTimeEvents()
        {
            if (isSubscribedToTimeEvents && timeManager != null)
            {
                try
                {
                    timeManager.UnsubscribeFromHourlyEvent(18, OnNightFalls);
                    timeManager.UnsubscribeFromHourlyEvent(6, OnDayBreaks);
                    isSubscribedToTimeEvents = false;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Archer] Error unsubscribing: {e.Message}");
                }
            }
        }



        private void CleanupReferences()
        {
            if (status != null)
            {
                status.currentTarget = null;
                status.assignedWall = null;
                status.previousWall = null;
            }
            cachedRetreatTarget = null;
            pendingTarget = null;
        }

        // --- INPCBehavior implementation ---
        public void UpdateBehavior()
        {
            if (IsDead || status == null || animator == null)
            {
                if (!IsDead && status == null)
                    Debug.LogError("[Archer] UpdateBehavior called with null status!");
                return;
            }

            if (rayHelper != null) 
            {
                try
                {
                    rayHelper.DrawRay();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Archer] RayCast error: {e.Message}");
                }
            }
            
            try
            {
                // Check GuardManager Alarm State each frame or trust event? 
                // Better to have explicit priority: If alarm -> Returning/Holding
                if (guardManager != null && guardManager.isEnemyAttack && 
                    status.archerState == DenizenStatus.ArcherState.Hunting)
                {
                    TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
                }

                switch (status.archerState)
                {
                    case DenizenStatus.ArcherState.Idle:
                        HandleIdleState();
                        break;
                        
                    case DenizenStatus.ArcherState.Hunting:
                        HandleHuntingState();
                        break;
                        
                    case DenizenStatus.ArcherState.HoldingWall:
                        HandleHoldingWallState();
                        break;
                        
                    case DenizenStatus.ArcherState.RetreatingToInnerWall:
                        HandleRetreatingToInnerWallState();
                        break;
                        
                    case DenizenStatus.ArcherState.ReturningToBase:
                        HandleReturningToBaseState();
                        break;
                        
                    default:
                        Debug.LogError($"[Archer] Unknown state: {status.archerState}");
                        TransitionToState(DenizenStatus.ArcherState.Idle);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Archer] State machine error in {status.archerState}: {e.Message}\n{e.StackTrace}");
                TransitionToState(DenizenStatus.ArcherState.Idle);
            }
        }

        #region State Handlers
        
        // --- PUBLIC METHODS for GuardManager interaction ---
        public void OnGuardManagerStatusChanged(bool isEnemyAttack)
        {
            if (isEnemyAttack)
            {
                // Alarm raised
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
            else
            {
                // Alarm cleared
                 if (timeManager != null && timeManager.IsDaytime)
                 {
                     TransitionToState(DenizenStatus.ArcherState.Hunting);
                 }
                 // If night, stay in HoldingWall (implied)
            }
        }

        public void ForceRetreat(GameObject defensiveWall)
        {
            cachedRetreatTarget = defensiveWall;
            TransitionToState(DenizenStatus.ArcherState.RetreatingToInnerWall);
        }

        // --- HANDLERS ---

        private void HandleIdleState()
        {
            if (timeManager != null && timeManager.IsDaytime)
            {
                TransitionToState(DenizenStatus.ArcherState.Hunting);
            }
            else
            {
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
        }

        private void HandleHuntingState()
        {
            // Scan for targets using RayCast
            if (CheckForTargets()) 
            {
                // If target is Enemy -> Report and Retreat
                if (pendingTarget != null && pendingTarget.CompareTag("Enemy"))
                {
                    if (guardManager != null) guardManager.RegisterEnemy(pendingTarget);
                    TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
                    pendingTarget = null;
                    return;
                }
                
                // If target is Animal -> Stop and Hunt
                if (pendingTarget != null)
                {
                    status.currentTarget = pendingTarget;
                    status.currentSpeed = 0f; // Stop moving
                    FaceTarget(status.currentTarget.transform.position);
                    
                    if (Time.time - status.lastAttackTime >= status.attackCooldown + ATTACK_READY_BUFFER)
                    {
                        PerformAttack();
                    }
                    return;
                }
            }

            // No target found -> Wander logic
            status.currentTarget = null;
            pendingTarget = null;

            if (currentHuntingTargetPos == null || 
                Vector3.Distance(transform.position, currentHuntingTargetPos.Value) < 0.5f ||
                Time.time - lastHuntingMoveTime > HUNTING_PATROL_INTERVAL)
            {
                // Pick new random point within HUNTING_RANGE of the FARTHEST wall
                GameObject baseWall = status.assignedWall;
                
                // If no wall assigned yet, ask GuardManager for current defensive line
                if (baseWall == null && guardManager != null)
                {
                    baseWall = guardManager.GetCurrentDefensiveWall(status.isLeftFlank);
                    status.assignedWall = baseWall;
                }
                
                Vector3 origin = baseWall != null ? baseWall.transform.position : transform.position;
                
                if (guardManager == null) guardManager = GuardManager.Instance;
                float huntingRange = guardManager.archerHuntingRange;
                    
                float randomX = UnityEngine.Random.Range(10f, huntingRange); 
                // Left Flank (Negative X) -> Direction -1 (Outward Left)
                // Right Flank (Positive X) -> Direction +1 (Outward Right)
                float direction = status.isLeftFlank ? -1f : 1f;
                
                currentHuntingTargetPos = new Vector3(
                    origin.x + (direction * randomX), 
                    origin.y, 
                    origin.z
                );
                
                lastHuntingMoveTime = Time.time;
            }

            MoveTowardsPosition(currentHuntingTargetPos.Value, status.walkSpeed); // Use WalkSpeed for hunting patrol
            if (animator != null) animator.Play("archerMove");
        }
        
        private void HandleHoldingWallState()
        {
            // Note: NO Checking Wall Info here. GuardManager handles critical alerts.
            // Note: Combat logic is integrated here.
            
            // 1. Check for Enemies
            if (CheckForTargets() && pendingTarget != null)
            {
                if (pendingTarget.activeInHierarchy && pendingTarget.CompareTag("Enemy"))
                {
                    status.currentTarget = pendingTarget;
                    // Report presence to keep alarm alive
                    if (guardManager != null) guardManager.RegisterEnemy(pendingTarget); 
                    
                    // Attack Logic
                    FaceTarget(status.currentTarget.transform.position);
                    status.currentSpeed = 0f;
                    
                    if (Time.time - status.lastAttackTime >= status.attackCooldown + ATTACK_READY_BUFFER)
                    {
                        PerformAttack();
                    }
                    else
                    {
                        // Cooldown idle
                        if (animator != null) animator.Play("archerIdle");
                    }
                    pendingTarget = null;
                    return;
                }
                // Ignore animals while Holding Wall (defensive duty)
                pendingTarget = null; 
            }
            
            // 2. No enemies -> Guard Posture
            // Ensure we are at the assigned post if valid
            if (status.assignedWall != null)
            {
                 Vector3 targetPosition = GetWallDefensePosition(status.assignedWall);
                 float distanceToWall = Vector2.Distance(transform.position, targetPosition);
            
                 if (distanceToWall > WALL_ARRIVAL_DISTANCE)
                 {
                     MoveTowardsPosition(targetPosition, status.runSpeed);
                     if (animator != null) animator.Play("archerMove");
                     return;
                 }
            }
            
            // Idling at post
            status.currentSpeed = 0f;
            status.currentTarget = null;
            FaceOutward();
            if (animator != null) animator.Play("archerIdle");
        }
        
        private void HandleRetreatingToInnerWallState()
        {
            // Triggered via ForceRetreat or Wall Critical
            if (cachedRetreatTarget == null)
            {
                 // Fallback if target lost
                 TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
                 return;
            }

            Vector3 retreatPos = GetWallDefensePosition(cachedRetreatTarget);
            float distanceToWall = Vector2.Distance(transform.position, retreatPos);
            
            if (distanceToWall <= RETREAT_COMPLETE_DISTANCE)
            {
                // Debug.Log($"[Archer] Retreat complete, now defending {cachedRetreatTarget.name}");
                status.previousWall = status.assignedWall;
                status.assignedWall = cachedRetreatTarget;
                cachedRetreatTarget = null;
                TransitionToState(DenizenStatus.ArcherState.HoldingWall);
            }
            else
            {
                MoveTowardsPosition(retreatPos, status.runSpeed);
                if (animator != null) animator.Play("archerMove");
            }
        }
        
        private void HandleReturningToBaseState()
        {
            // 1. Update/Get Wall Objective from GuardManager
            if (guardManager == null) guardManager = GuardManager.Instance;
            
            // Re-fetch assigned wall if null or just to update
            if (guardManager != null)
            {
                GameObject targetWall = guardManager.GetCurrentDefensiveWall(status.isLeftFlank);
                status.assignedWall = targetWall; 
            }
            
            // 2. If no wall exists (total destruction), stand ground / fallback to Town logic?
            if (status.assignedWall == null)
            {
                 TransitionToState(DenizenStatus.ArcherState.HoldingWall);
                 return;
            }
            
            // 3. Move to Wall
            Vector3 targetPosition = GetWallDefensePosition(status.assignedWall);
            float distance = Vector2.Distance(transform.position, targetPosition);
            
            if (distance > WALL_ARRIVAL_DISTANCE)
            {
                MoveTowardsPosition(targetPosition, status.runSpeed);
                if (animator != null) animator.Play("archerMove");
            }
            else
            {
                // Arrived
                TransitionToState(DenizenStatus.ArcherState.HoldingWall);
            }
        }
        
        #endregion

        #region Combat & Movement Helpers
        
        private bool CheckForTargets()
        {
            // Throttle checks
            if (Time.time - lastTargetCheckTime < TARGET_CHECK_INTERVAL) return false;
            lastTargetCheckTime = Time.time;

            if (TryAcquireTarget(out GameObject newTarget))
            {
                pendingTarget = newTarget;
                return true;
            }
            pendingTarget = null;
            return false;
        }

        private bool TryAcquireTarget(out GameObject target)
        {
            target = null;
            if (rayHelper == null || rayHelper.GetHitObjects() == null) return false;
            
            List<GameObject> detected = rayHelper.GetHitObjects(); // Use built-in getter to be safe
            if (detected.Count == 0) return false;

            GameObject priorityTarget = GetPriorityTarget(detected);
            if (priorityTarget == null || !priorityTarget.activeInHierarchy) return false;

            float distance = Vector3.Distance(transform.position, priorityTarget.transform.position);
            if (distance > status.attackRange) return false;

            target = priorityTarget;
            return true;
        }

        private GameObject GetPriorityTarget(List<GameObject> objects)
        {
            GameObject enemyInfo = null;
            GameObject animalInfo = null;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                if (obj.CompareTag("Enemy"))
                {
                    enemyInfo = obj;
                    break;
                }
                // Check hunting tags
                if (PrefabData.Instance != null && PrefabData.Instance.huntingTargetTags != null)
                {
                    foreach(var tag in PrefabData.Instance.huntingTargetTags)
                    {
                        if(obj.CompareTag(tag))
                        {
                            animalInfo = obj; 
                            break;
                        }
                    }
                }
            }
            // Prioritize Enemy over Animal
            return enemyInfo ?? animalInfo;
        }

        private void PerformAttack()
        {
            if (status.currentTarget != null) 
            {
                 // Debug.Log($"[Archer] Attack {status.currentTarget.name}");
            }
                 
            // Start Animation Sequence: archerDraw -> Event(OnDrawComplete) -> archerRelease -> Event(OnArrowRelease)
            // Note: If target is Animal, animation is same.
            if (animator != null) animator.Play("archerDraw");
            status.lastAttackTime = Time.time;
        }
        
        public void OnDrawComplete()
        {
            if (animator != null) animator.Play("archerRelease");
        }
        
        public void OnArrowRelease()
        {
            // Placeholder arrow release
            // Debug.Log("[Archer] Arrow Released");
        }

        private void MoveTowardsPosition(Vector3 pos, float speed)
        {
            if (isDebugMovePos)
            {
                Debug.DrawLine(transform.position, pos, Color.yellow);
            }

            Vector3 direction = (pos - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            status.currentSpeed = speed;
            
            if (direction.x != 0)
            {
                float scaleX = direction.x > 0 ? 1f : -1f;
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
        }

        private void FaceOutward()
        {
            float scaleX = status.isLeftFlank ? -1f : 1f;
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
        
        private void FaceTarget(Vector3 targetPos)
        {
            float direction = targetPos.x > transform.position.x ? 1f : -1f;
            transform.localScale = new Vector3(direction, transform.localScale.y, transform.localScale.z);
        }

        #endregion

        #region Wall & Logic Helpers
        
        private Vector3 GetWallDefensePosition(GameObject wall)
        {
            if (wall == null) return transform.position;
            
            float direction = status.isLeftFlank ? 1f : -1f; 
            return new Vector3(
                wall.transform.position.x + direction * status.standBehindWallOffset,
                wall.transform.position.y,
                transform.position.z
            );
        }

        private void TransitionToState(DenizenStatus.ArcherState newState)
        {
            if (status.archerState == newState) return;
            
            // Debug.Log($"[Archer] State transition: {status.archerState} -> {newState}");
            OnExitState(status.archerState);
            // status.previousState = status.archerState; // previousState handling can be implicit or added to DenizenStatus if crucial
            status.archerState = newState;
            OnEnterState(newState);
        }

        private void OnEnterState(DenizenStatus.ArcherState state)
        {
            switch (state)
            {
                case DenizenStatus.ArcherState.ReturningToBase:
                case DenizenStatus.ArcherState.RetreatingToInnerWall:
                    status.currentSpeed = status.runSpeed;
                    if (state == DenizenStatus.ArcherState.RetreatingToInnerWall) CacheRetreatTarget();
                    break;
                case DenizenStatus.ArcherState.HoldingWall:
                    status.currentSpeed = 0f;
                    break;
                case DenizenStatus.ArcherState.Hunting:
                    status.currentSpeed = status.walkSpeed; 
                    currentHuntingTargetPos = null;
                    break;
            }
        }

        private void OnExitState(DenizenStatus.ArcherState state)
        {
            switch (state)
            {
                case DenizenStatus.ArcherState.RetreatingToInnerWall:
                    cachedRetreatTarget = null;
                    break;
            }
        }

        private void CacheRetreatTarget()
        {
            if (wallManager == null)
            {
                cachedRetreatTarget = null;
                return;
            }
            try
            {
                cachedRetreatTarget = wallManager.GetDefendableWallBehind(status.previousWall);
                if (cachedRetreatTarget == null)
                {
                     // Debug.LogWarning("[Archer] No inner wall found for retreat");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Archer] Error caching retreat target: {e.Message}");
                cachedRetreatTarget = null;
            }
        }

        private void OnNightFalls()
        {
            if (IsDead || status == null) return;
            if (status.archerState == DenizenStatus.ArcherState.Hunting)
            {
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
        }

        private void OnDayBreaks()
        {
            if (IsDead || status == null) return;
            if (status.archerState == DenizenStatus.ArcherState.HoldingWall ||
                status.archerState == DenizenStatus.ArcherState.ReturningToBase)
            {
                TransitionToState(DenizenStatus.ArcherState.Hunting);
            }
        }

        #endregion

        // --- Interface implementations ---
        
        // Removed explicit ISaveable impl as done by DenizenRoleController through DenizenStatus

        public bool ConsumeCoinShield()
        {
            if (!HasCoinShield) return false;
            CurrentCoin -= 1;
            DropCoins(1);
            if (CurrentCoin == 0)
            {
                Utility.TransformToVillager(gameObject, 0);
            }
            return true;
        }

        public void DropCoins(int amount)
        {
             for (int i = 0; i < amount; i++)
            {
                if (PrefabData.Instance?.coinPrefab != null)
                {
                    Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                    Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + 2f);
                    
                    if (Spawner.Instance != null && EntitySaveManager.Instance != null)
                    {
                        GameObject coin = Instantiate(Spawner.Instance.coinPrefab, spawnPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);
                        // Coin logic...
                        Coin coinScript = coin.GetComponent<Coin>();
                        if (coinScript != null)
                        {
                            coinScript.playerPickupAble = true;
                            coinScript.denizensPickupAble = false;
                            coinScript.floating = false;
                            coinScript.CallFunctionCoolDown("Denizen");
                            Spawner.Instance.InitializeCoinThrow(coin, dropDirection);
                        }
                    }
                }
            }
        }

        public void TakeDamage(int damage) => TakeDamage(damage, null);

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (guardManager != null && whoDoDame != null)
            {
                if (whoDoDame.CompareTag("Enemy"))
                {
                    guardManager.RegisterEnemy(whoDoDame);
                }
            }
            
            if (HasCoinShield && ConsumeCoinShield())
            {
                return true; 
            }

            CurrentHp -= damage;
            if (IsDead)
            {
                Die(whoDoDame);
            }
            return true;
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        }

        public void Die() => Die(null);
        
        public void Die(GameObject whoDoDame)
        {
            // Debug.Log($"[Archer] Died by {whoDoDame?.name ?? "unknown"}");
            // Unregister from GuardManager
            if (guardManager != null) guardManager.UnregisterArcher(gameObject);
            Destroy(gameObject);
        }
    }
}
