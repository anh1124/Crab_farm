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
    [RequireComponent(typeof(DenizenStatus))]
    [RequireComponent(typeof(RayCastHelper))] // Vẫn giữ require vì RoleController có thể add
    [RequireComponent(typeof(Script.Game.Other.CircleDetectHelper))]
    [RequireComponent(typeof(Animator))]
    public class ArcherBehavior : MonoBehaviour, IHealth, ICoinShield, INPCBehavior
    {
        #region Constants
        
        // Ngưỡng phòng thủ và rút lui
        private const float WALL_ARRIVAL_DISTANCE = 0.5f;
        private const float RETREAT_COMPLETE_DISTANCE = 2.0f;
        
        // Thời gian chiến đấu
        private const float ATTACK_READY_BUFFER = 0.1f;
        private const float TARGET_CHECK_INTERVAL = 0.2f;

        // Dữ liệu Săn bắn (được lấy từ PrefabData)
        private const float HUNTING_PATROL_INTERVAL = 5.0f;
        
        #endregion

        // References
        private DenizenStatus status;
        private Script.Game.Other.CircleDetectHelper circleDetectHelper;
        private Animator animator;
        
        // Managers
        private GameTimeManager timeManager;
        private WallAndStumpManager wallManager;
        private GuardManager guardManager;

        // State Tracking
        private bool isSubscribedToTimeEvents = false;
        private GameObject pendingTarget = null;
        private float lastTargetCheckTime = 0f;

        // Dữ liệu săn bắn
        private Vector3? currentHuntingTargetPos = null;
        private float lastHuntingMoveTime = 0f;

        private void OnEnable()
        {
            // Disable all detectives first
            var controller = GetComponent<DenizenRoleController>();
            if (controller != null)
            {
                controller.DisableAllDetectives();
            }

            // Enable CircleDetectHelper
            if (circleDetectHelper == null) circleDetectHelper = GetComponent<Script.Game.Other.CircleDetectHelper>();
            if (circleDetectHelper != null)
            {
                circleDetectHelper.enabled = true;
                circleDetectHelper.isShowGizmo = true; // System control
            }
            
            // Re-subscribe logic from old OnEnable
            if (guardManager == null) guardManager = GuardManager.Instance;
            SubscribeToTimeEvents();
        }

        private void OnDisable()
        {
             // Cleanup if needed
             if (circleDetectHelper != null)
             {
                 circleDetectHelper.enabled = false;
                 circleDetectHelper.isShowGizmo = false;
             }
             
             UnsubscribeFromTimeEvents();
        }
        
        // Bộ nhớ đệm (Cache)
        private GameObject cachedRetreatTarget = null;
        
        [Header("Debug")]
        [SerializeField] private float attackCooldownTimer; // Hiển thị trong Inspector
        [SerializeField] private bool isDebugMovePos = false;
        [SerializeField] private bool isDebugAttackRange = true;




        [ContextMenu("Debug Status (Print to Console)")]
        public void DebugStatus()
        {
            string timeStatus = timeManager != null ? (timeManager.IsDaytime ? "Day" : "Night") : "Null";
            string guardStatus = guardManager != null ? (guardManager.isEnemyAttack ? "ALARM" : "Peace") : "Null";
            string enemiesDetected = guardManager != null ? guardManager.detectedEnemies.Count.ToString() : "N/A";
            
            string info = $"--- ARCHER STATUS [{gameObject.name}] ---\n";
            info += $"Role: {status.Role}\n";
            info += $"State: {status.archerState}\n";
            info += $"Flank: {(status.isLeftFlank ? "LEFT" : "RIGHT")}\n";
            info += $"Assigned Wall: {(status.assignedWall != null ? status.assignedWall.name : "NULL")}\n";
            info += $"Current Target: {(status.currentTarget != null ? status.currentTarget.name : "NULL")}\n";
            info += $"Time: {timeStatus}\n";
            info += $"Guard Mode: {guardStatus} (Enemies: {enemiesDetected})\n";
            
            Debug.Log(info);
        }

        [ContextMenu("Force State Update")]
        public void ForceStateUpdate()
        {
            Debug.Log($"[Archer] Forcing state update for {gameObject.name}...");
            
            // Đánh giá lại trạng thái dựa trên điều kiện hiện tại
            if (guardManager != null && guardManager.isEnemyAttack)
            {
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
            else if (timeManager != null)
            {
                if (timeManager.IsDaytime)
                {
                    TransitionToState(DenizenStatus.ArcherState.Hunting);
                }
                else
                {
                    TransitionToState(DenizenStatus.ArcherState.ReturningToBase); // Hoặc HoldingWall tùy logic
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Gizmo range đã được vẽ bởi CircleDetectHelper hoặc RayCastHelper
            // Chỉ giữ lại các gizmo debug khác nếu có
        }

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

            circleDetectHelper = GetComponent<Script.Game.Other.CircleDetectHelper>();
            // Note: CircleDetectHelper is managed by DenizenRoleController usually, but we get reference here.
            
            animator = GetComponent<Animator>();
            
            InitializeDetector();
        }
        
        private void InitializeDetector()
        {
            if (circleDetectHelper == null) return;
            circleDetectHelper.SetDetectRadius(status.archerDetectRange);
        }
        


        private void Start()
        {
            // Lấy các Manager
            timeManager = GameTimeManager.Instance;
            wallManager = WallAndStumpManager.Instance;
            guardManager = GuardManager.Instance;
            
            // Kiểm tra trạng thái báo động
            if (guardManager != null && guardManager.isEnemyAttack)
            {
                status.archerState = (DenizenStatus.ArcherState)DenizenStatus.ArcherState.ReturningToBase;
            }
            
            // Đăng ký sự kiện thời gian
            SubscribeToTimeEvents();
            
            // Kiểm tra trạng thái ban đầu (nếu chưa được thiết lập)
            if (timeManager != null && !guardManager.isEnemyAttack)
            {
                status.archerState = timeManager.IsDaytime ? DenizenStatus.ArcherState.Hunting : DenizenStatus.ArcherState.HoldingWall;
            }
        }
        



        
        private void SubscribeToTimeEvents()
        {
            if (isSubscribedToTimeEvents || timeManager == null) return;
            
            try
            {
                // Thời gian Debug: Sáng (7), Tối (19)
                timeManager.SubscribeToHourlyEvent(19, OnNightFalls);
                timeManager.SubscribeToHourlyEvent(7, OnDayBreaks);
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
            // Việc hủy đăng ký được xử lý bởi Controller
            CleanupReferences();
        }

        private void UnsubscribeFromTimeEvents()
        {
            if (isSubscribedToTimeEvents && timeManager != null)
            {
                try
                {
                    timeManager.UnsubscribeFromHourlyEvent(19, OnNightFalls);
                    timeManager.UnsubscribeFromHourlyEvent(7, OnDayBreaks);
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
            //tính toán thời gian tấn công
            if (status.attackSpeed > 0)
            {
                attackCooldownTimer -= Time.deltaTime;
                if (attackCooldownTimer < 0)
                {
                    attackCooldownTimer = 0; // Giữ ở 0 khi sẵn sàng tấn công
                }
            }

            if (IsDead || status == null || animator == null)
            {
                if (!IsDead && status == null)
                    Debug.LogError("[Archer] UpdateBehavior called with null status!");
                return;
            }

            // CircleDetectHelper tự handle việc vẽ Gizmo trong OnDrawGizmos của nó.
            
            // Luôn kiểm tra mục tiêu xung quanh và đăng ký với GuardManager
            CheckForTargets();
            
            try
            {
                // Kiểm tra trạng thái báo động từ GuardManager
                // Ưu tiên rõ ràng: Nếu báo động -> Returning/Holding
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
        
        // --- PHƯƠNG THỨC CÔNG KHAI để GuardManager gọi ---
        public void OnGuardManagerStatusChanged(bool isEnemyAttack)
        {
            if (isEnemyAttack)
            {
                // Bật báo động
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
            else
            {
                // Tắt báo động
                if (timeManager != null && timeManager.IsDaytime)
                {
                    TransitionToState(DenizenStatus.ArcherState.Hunting);
                }
                // Nếu ban đêm, giữ trạng thái Phòng thủ (ngầm định)
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
            

             // LOGIC DEBUG: Nếu có target từ SelectTargetAndLog, dừng và quay mặt về phía đó.
            if (status.currentTarget != null)
            {
                status.currentSpeed = 0f;
                FaceTarget(status.currentTarget.transform.position);
                // Tấn công được xử lý bởi timer trong SelectTargetAndLog
                return;
            }

            // Không tìm thấy mục tiêu -> Đi tuần
            status.currentTarget = null;
            pendingTarget = null;

            if (currentHuntingTargetPos == null || 
                Vector3.Distance(transform.position, currentHuntingTargetPos.Value) < 0.5f ||
                Time.time - lastHuntingMoveTime > HUNTING_PATROL_INTERVAL)
            {
                if (guardManager == null) guardManager = GuardManager.Instance;

                // Hỏi GuardManager vị trí bắt đầu săn bắn
                Vector3? huntingStartPos = guardManager.GetHuntingRangeStartPosition(status.isLeftFlank);

                if (huntingStartPos.HasValue)
                {
                    // Có điểm xuất phát (Wall hoặc TownCenter) -> Đi săn trong bãi săn
                    CalculateHuntingPatrolPosition(huntingStartPos.Value, true);
                }
                else
                {
                    // Không có điểm xuất phát -> Random xung quanh vị trí hiện tại
                    currentHuntingTargetPos = Script.Utils.WorldUtility.GetRandomPositionNearby(transform.position, 10f);
                }
                
                lastHuntingMoveTime = Time.time;
            }

            MoveTowardsPosition(currentHuntingTargetPos.Value, status.walkSpeed); // Dùng tốc độ đi bộ khi tuần tra
            if (animator != null) animator.Play("archerMove");
        }

        private void CalculateHuntingPatrolPosition(Vector3 origin, bool directional)
        {
            if (guardManager == null) guardManager = GuardManager.Instance;
            float huntingRange = guardManager.archerHuntingRange;

            // Logic đi săn có hướng (khi có base)
            float randomX = UnityEngine.Random.Range(10f, huntingRange); 
            // Cánh trái (X âm) -> Hướng -1 (Ra ngoài trái)
            // Cánh phải (X dương) -> Hướng +1 (Ra ngoài phải)
            float direction = status.isLeftFlank ? -1f : 1f;
            
            currentHuntingTargetPos = new Vector3(
                origin.x + (direction * randomX), 
                origin.y, 
                origin.z
            );
        }
        
        private void HandleHoldingWallState()
        {
            // Note: KHÔNG kiểm tra thông tin Wall ở đây. GuardManager xử lý cảnh báo.
            // Note: Logic chiến đấu được tích hợp ở đây.
            
            // 1. Kiểm tra kẻ thù 
            
            // LOGIC DEBUG: Nếu có target từ SelectTargetAndLog, dừng và quay mặt về phía đó.
            if (status.currentTarget != null)
            {
                status.currentSpeed = 0f;
                FaceTarget(status.currentTarget.transform.position);
                 // Tấn công được xử lý bởi timer trong SelectTargetAndLog
                return;
            }
            
            // 2. Không có kẻ thù -> Tư thế phòng thủ
            // Luôn lấy vị trí phòng thủ từ GuardManager (bao gồm cả fallback về TownCenter)
            Vector3 targetPosition = GetDefensivePosition(status.assignedWall);
            float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
            
            // Nếu vị trí đích khác vị trí hiện tại -> Di chuyển
            if (distanceToTarget > WALL_ARRIVAL_DISTANCE)
            {
                 MoveTowardsPosition(targetPosition, status.runSpeed);
                 if (animator != null) animator.Play("archerMove");
                 return;
            }
            
            // Idling at post
            status.currentSpeed = 0f;
            status.currentTarget = null;
            FaceOutward();
            if (animator != null) animator.Play("archerIdle");
        }
        
        private void HandleRetreatingToInnerWallState()
        {
            // Được kích hoạt thông qua ForceRetreat hoặc khi Wall sắp sập
            if (cachedRetreatTarget == null)
            {
                 // Dự phòng nếu mất mục tiêu
                 TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
                 return;
            }

            Vector3 retreatPos = GetDefensivePosition(cachedRetreatTarget);
            float distanceToWall = Vector2.Distance(transform.position, retreatPos);
            
            if (distanceToWall <= RETREAT_COMPLETE_DISTANCE)
            {
                // Debug.Log($"[Archer] Rút lui hoàn tất, giờ bảo vệ {cachedRetreatTarget.name}");
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
            // 1. Cập nhật/Lấy mục tiêu tường phòng thủ từ GuardManager
            if (guardManager == null) guardManager = GuardManager.Instance;
            
            // Lấy lại tường được gán nếu null hoặc để cập nhật
            if (guardManager != null)
            {
                GameObject targetWall = guardManager.GetCurrentDefensiveWall(status.isLeftFlank);
                status.assignedWall = targetWall; 
            }
            
            // 2. Nếu không có tường (phá hủy hoàn toàn), vẫn lấy vị trí fallback từ GuardManager
            // Không return sớm nữa
            // if (status.assignedWall == null) ...
            
            // 3. Di chuyển về tường
            Vector3 targetPosition = GetDefensivePosition(status.assignedWall);
            float distance = Vector2.Distance(transform.position, targetPosition);
            
            if (distance > WALL_ARRIVAL_DISTANCE)
            {
                MoveTowardsPosition(targetPosition, status.runSpeed);
                if (animator != null) animator.Play("archerMove");
            }
            else
            {
                // Đã đến nơi
                TransitionToState(DenizenStatus.ArcherState.HoldingWall);
            }
        }
        
        #endregion

        #region Combat & Movement Helpers
        
        private bool CheckForTargets()
        {
            // Giới hạn kiểm tra
            if (Time.time - lastTargetCheckTime < TARGET_CHECK_INTERVAL) return false;
            lastTargetCheckTime = Time.time;

            if (TryAcquireTarget(out GameObject newTarget))
            {
                pendingTarget = newTarget;
                
                // Register with GuardManager if it is an Enemy
                if (guardManager != null && newTarget.CompareTag("Enemy"))
                {
                    guardManager.RegisterEnemy(newTarget);
                }
                
                return true;
            }
            pendingTarget = null;
            return false;
        }



        private bool TryAcquireTarget(out GameObject target)
        {
            target = null;
            if (circleDetectHelper == null) return false;

            // Sync debug state
            circleDetectHelper.isShowGizmo = isDebugAttackRange;

            // Sử dụng Helper để quét
            List<GameObject> detected = circleDetectHelper.ScanAndGetDetectedObjects(status.archerDetectRange);
            
            if (detected.Count == 0) return false;

            GameObject priorityTarget = GetPriorityTarget(detected);
            if (priorityTarget == null || !priorityTarget.activeInHierarchy) return false;

            // Kiểm tra khoảng cách lần nữa (để chắc chắn)
            float distance = Vector3.Distance(transform.position, priorityTarget.transform.position);
            if (distance > status.archerDetectRange) return false;

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
                // Kiểm tra thẻ săn bắn (hunting tags)
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
            // Ưu tiên Enemy hơn Animal
            return enemyInfo ?? animalInfo;
        }

        private void PerformAttack()
        {
            if (status.currentTarget != null) 
            {
                 // Debug.Log($"[Archer] Attack {status.currentTarget.name}");
            }
                 
            // Bắt đầu chuỗi animation: archerDraw -> Event(OnDrawComplete) -> archerRelease -> Event(OnArrowRelease)
            // Lưu ý: Nếu mục tiêu là Animal, animation cũng tương tự.
            if (animator != null) animator.Play("archerDraw");
            status.lastAttackTime = Time.time;
        }
        
        public void OnDrawComplete()
        {
            if (animator != null) animator.Play("archerRelease");
        }
        
        public void OnArrowRelease()
        {
            // Bắn tên (Placeholder)
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
        
        private Vector3 GetDefensivePosition(GameObject wall)
        {
            if (guardManager == null)
                guardManager = GuardManager.Instance;

            if (guardManager != null)
            {
                return guardManager.GetDefensePositionForArcher(gameObject);
            }

            return transform.position;
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
                Debug.LogError($"[Archer] Lỗi cache mục tiêu rút lui: {e.Message}");
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

        private void SelectTargetAndLog()
        {
            // 1. Thu thập & Sắp xếp Mục tiêu (dùng OverlapCircle thay vì Raycast)
            List<GameObject> allHits = DetectTargetsInRadius();
            List<GameObject> enemies = new List<GameObject>();
            List<GameObject> animals = new List<GameObject>();

            foreach(var obj in allHits)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (obj.CompareTag("Enemy")) enemies.Add(obj);
                else 
                {
                    // Kiểm tra tag Animal từ PrefabData hoặc fallback
                    // Kiểm tra an toàn: dùng tag rõ ràng
                     if (PrefabData.Instance != null && PrefabData.Instance.huntingTargetTags != null)
                     {
                         foreach(var tag in PrefabData.Instance.huntingTargetTags)
                         {
                             if(obj.CompareTag(tag)) { animals.Add(obj); break; }
                         }
                     }
                     else if (obj.CompareTag("Animal") || obj.CompareTag("Rabbit")) // Fallback
                     {
                         animals.Add(obj);
                     }
                }
            }

            // Sắp xếp theo khoảng cách
            enemies.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
            animals.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

            GameObject target = null;
            string action = "IDLE";
            string reason = "";


            // 2. Quyết định Mục tiêu & Hành động

            // --- Ưu tiên 1: Kẻ thù ---
            if (enemies.Count > 0)
            {
                target = enemies[0];

                // Đăng ký kẻ thù vào GuardManager
                if (guardManager != null)
                {
                    guardManager.RegisterEnemy(target);
                }
                
                bool isAtWall = IsAtDefensivePosition();
                
                // Kiểm tra nếu TẤT CẢ tường bị phá (Tử chiến)
                GameObject currentWall = status.assignedWall;
                if (currentWall == null && guardManager != null)
                {
                    currentWall = guardManager.GetCurrentDefensiveWall(status.isLeftFlank);
                    if (currentWall != null) status.assignedWall = currentWall; // Gán nếu tìm thấy
                }

                bool noWallsRemain = (currentWall == null);

                if (isAtWall || noWallsRemain)
                {
                    // Chỉ tấn công nếu cooldown sẵn sàng
                    if (attackCooldownTimer <= 0)
                    {
                        action = "ATTACKING";
                        status.currentTarget = target; // KHÓA MỤC TIÊU
                        PerformAttack();
                        attackCooldownTimer = status.attackSpeed; // Reset cooldown sau khi bắn
                        if (noWallsRemain) reason = "(Tử chiến)";
                        
                        // Log chỉ khi thực sự tấn công
                        string targetInfo = $"{target.name} [{target.tag}] Pos:{target.transform.position}";
                        string candidatesLog = "";
                        foreach(var e in enemies) candidatesLog += $"[E]{e.name}({Math.Round(Vector3.Distance(transform.position, e.transform.position),1)}) ";
                        foreach(var a in animals) candidatesLog += $"[A]{a.name}({Math.Round(Vector3.Distance(transform.position, a.transform.position),1)}) ";
                        Debug.Log($"[Archer {transform.position}] ACTION: {action} {reason} | TARGET: {targetInfo} | CANDIDATES: {candidatesLog}");
                    }
                    else
                    {
                        action = "COOLDOWN";
                        status.currentTarget = target; // Vẫn khóa mục tiêu
                        reason = $"({attackCooldownTimer:F1}s)";
                    }
                }
                else
                {
                    action = "RETREATING";
                    status.currentTarget = null; // Xóa mục tiêu khi rút lui
                    reason = "(Không ở Wall)";
                    if (status.archerState != DenizenStatus.ArcherState.ReturningToBase 
                        && status.archerState != DenizenStatus.ArcherState.RetreatingToInnerWall)
                    {
                        TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
                    }
                }
            }
            // --- Ưu tiên 2: Động vật ---
            else if (animals.Count > 0)
            {
                target = animals[0];

                bool isDay = timeManager != null && timeManager.IsDaytime;
                bool isHunting = status.archerState == DenizenStatus.ArcherState.Hunting;

                // Chỉ tấn công động vật nếu Ban ngày VÀ Săn bắn
                if (isDay && isHunting)
                {
                    // Chỉ tấn công nếu cooldown sẵn sàng
                    if (attackCooldownTimer <= 0)
                    {
                        action = "ATTACKING";
                        status.currentTarget = target; // KHÓA MỤC TIÊU
                        status.currentSpeed = 0f; // Dừng để bắn
                        PerformAttack();
                        attackCooldownTimer = status.attackSpeed; // Reset cooldown sau khi bắn
                        
                        // Log chỉ khi thực sự tấn công
                        string targetInfo = $"{target.name} [{target.tag}] Pos:{target.transform.position}";
                        string candidatesLog = "";
                        foreach(var e in enemies) candidatesLog += $"[E]{e.name}({Math.Round(Vector3.Distance(transform.position, e.transform.position),1)}) ";
                        foreach(var a in animals) candidatesLog += $"[A]{a.name}({Math.Round(Vector3.Distance(transform.position, a.transform.position),1)}) ";
                        Debug.Log($"[Archer {transform.position}] ACTION: {action} | TARGET: {targetInfo} | CANDIDATES: {candidatesLog}");
                    }
                    else
                    {
                        action = "COOLDOWN";
                        status.currentTarget = target; // Vẫn khóa mục tiêu
                        reason = $"({attackCooldownTimer:F1}s)";
                    }
                }
                else
                {
                    action = "IGNORING";
                    status.currentTarget = null; // Bỏ qua
                    reason = $"(Ngày:{isDay}, Trạng thái:{status.archerState})";
                }
            }
            else
            {
                status.currentTarget = null; // Không có mục tiêu
            }
        }

        // Phát hiện mục tiêu trong bán kính (hình tròn) - hỗ trợ cả mục tiêu bay
        private List<GameObject> DetectTargetsInRadius()
        {
            List<GameObject> detectedTargets = new List<GameObject>();
            
            // Sử dụng OverlapCircleAll để phát hiện tất cả collider trong bán kính
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, status.archerDetectRange);
            
            foreach (Collider2D hit in hits)
            {
                GameObject obj = hit.gameObject;
                
                // Bỏ qua chính mình
                if (obj == gameObject) continue;
                
                // Chỉ lấy Enemy hoặc Animal
                if (obj.CompareTag("Enemy"))
                {
                    detectedTargets.Add(obj);
                }
                else if (PrefabData.Instance != null && PrefabData.Instance.huntingTargetTags != null)
                {
                    foreach (var tag in PrefabData.Instance.huntingTargetTags)
                    {
                        if (obj.CompareTag(tag))
                        {
                            detectedTargets.Add(obj);
                            break;
                        }
                    }
                }
                else if (obj.CompareTag("Animal") || obj.CompareTag("Rabbit"))
                {
                    detectedTargets.Add(obj);
                }
            }
            
            return detectedTargets;
        }

        private bool IsAtDefensivePosition()
        {
            Vector3 targetPos = GetDefensivePosition(status.assignedWall);
            return Vector2.Distance(transform.position, targetPos) <= WALL_ARRIVAL_DISTANCE + 0.5f; // Slight buffer
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
        
        // Đã xóa impl ISaveable rõ ràng vì được DenizenRoleController thực hiện qua DenizenStatus

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
