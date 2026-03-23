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
using Script.Manager.GamePlayManager;
using Script.Manager; // For GuardManager access via GuardManager.Instance if needed

namespace Script.Game.Mobs.Subjects
{
    [RequireComponent(typeof(DenizenStatus))]
    [RequireComponent(typeof(RayCastHelper))] // Vẫn giữ require vì RoleController có thể add
    [RequireComponent(typeof(Script.Game.Other.CircleDetectHelper))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(ArcherShooting))]
    [RequireComponent(typeof(DenizenCoinSharing))]
    public class ArcherBehavior : MonoBehaviour, ICoinShield, IToolShield, INPCBehavior, IItemHandle, IAttacker
    {
        // --- IAttacker Interface ---
        public float AttackDame { get => status.attackDame; set => status.attackDame = value; }
        public float AttackSpeed { get => status.attackSpeed; set => status.attackSpeed = value; }
        
        public void Attack()
        {
            PerformAttack();
        }
        
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
        
        [Header("Detection Settings")]
        [Tooltip("Danh sách các Tag mục tiêu. Kế thừa từ GameTags.TargetLists")]
        [SerializeField] private List<string> targetTags;

        // References
        private DenizenStatus status;
        private Script.Game.Other.CircleDetectHelper circleDetectHelper;
        private Animator animator;
        private ArcherShooting archerShooting;
        
        // Managers
        private GameTimeManager timeManager;
        private WallAndStumpManager wallManager;
        private GuardManager guardManager;
        private DenizenCoinSharing coinSharing;

        // State Tracking
        private string _currentAnimState = "";

        private void SetAnimState(string newState)
        {
            if (animator == null) return;
            if (_currentAnimState == newState) return; // ✅ skip nếu không đổi
            _currentAnimState = newState;
            animator.Play(newState);
        }

   

        // Dữ liệu săn bắn
        private Vector3? currentHuntingTargetPos = null;
        private float lastHuntingMoveTime = 0f;

        private void OnEnable()
        {
            _currentAnimState = ""; // ✅ Reset cache để ép Animator update khi đổi nghề
            
            // Ensure detector is initialized
            InitializeDetector();

            if (circleDetectHelper != null)
            {
                circleDetectHelper.isActive = true;
                circleDetectHelper.OnDetectedObjectsChanged -= OnObjectsDetected; // Prev double sub
                circleDetectHelper.OnDetectedObjectsChanged += OnObjectsDetected;
            }
            
            // GuardManager initialization
            if (guardManager == null) guardManager = GuardManager.Instance;
        }

        private void OnDisable()
        {
             // Cleanup if needed
             if (circleDetectHelper != null)
             {
                 circleDetectHelper.OnDetectedObjectsChanged -= OnObjectsDetected;
                 circleDetectHelper.isActive = false;
             }
             
             UnsubscribeFromTimeEvents();
        }
        
        // Bộ nhớ đệm (Cache)
        private GameObject cachedRetreatTarget = null;
        
        [Header("Debug")]
        private float lastAttackTime = -100f; // -100 để cho phép bắn ngay lúc đầu
        [SerializeField] private bool isDebugShotTarget = true;
        [SerializeField] private bool isDebugMovePos = false;
        [SerializeField] private bool isDebugAttackRange = true;
        
        // Movement Smoothing Variables
        private Vector3 _moveTarget;
        private float _moveSpeed;
        private bool _isMovingToTarget;




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
           // Debug.Log($"[Archer] Forcing state update for {gameObject.name}...");
            
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

        [ContextMenu("Debug / Simulate Damage")]
        private void SimulateDamage()
        {
            Debug.Log($"<color=orange>[ArcherBehavior] Simulating 1 damage on {gameObject.name}. coins={status?.CurrentCoins}</color>");
            Script.Utils.DamageHandler.ApplyDamage(gameObject, 1, gameObject);
        }

        // --- IToolShield Interface Properties ---
        public bool HasToolShield => status.CurrentCoins <= 1 && status.CurrentCoins > 0;
        
        public bool ConsumeToolShield()
        {
            if (!HasToolShield) return false;

            Spawner.Instance?.DropItem(Data_Model.PrefabData.Instance?.bow, transform.position,
                playerPickupAble: false, denizensPickupAble: true);

            var ctrl = GetComponent<DenizenRoleController>();
            if (ctrl != null) 
            {
                // ✅ Set cờ + prime timer trước khi đổi role.
                // VillagerBehavior.OnEnable sẽ đọc cờ này và vào Fear ngay lập tức.
                if (status != null)
                {
                    status.isFearPending = true;
                    status.fearTimer = VillagerBehavior.FEAR_DURATION;
                    status.isInDanger = true;
                }
                
                ctrl.ChangeRole(NPCRole.Villager);
            }

            return true;
        }

        // --- ICoinShield Interface Properties ---
        public bool HasCoinShield => status.CurrentCoins > 1; // Wait! ICoinShield expects normal behavior, but we restrict it so that ToolShield is checked or Coin>1
        
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

        // --- IDamageable logic moved to DenizenRoleController ---

        private void Awake()
        {
            gameObject.tag = Script.Utils.GameTags.Archer;
            status = GetComponent<DenizenStatus>();
            if (status == null)
            {
                status = gameObject.AddComponent<DenizenStatus>();
            }

            circleDetectHelper = GetComponent<Script.Game.Other.CircleDetectHelper>();
            // Note: CircleDetectHelper is managed by DenizenRoleController usually, but we get reference here.
            
            animator = GetComponent<Animator>();
            archerShooting = GetComponent<ArcherShooting>();
            coinSharing = GetComponent<DenizenCoinSharing>();
            
            InitializeDetector();
        }
        
        private void InitializeDetector()
        {
            if (circleDetectHelper == null) circleDetectHelper = GetComponent<Script.Game.Other.CircleDetectHelper>();
            
            if (circleDetectHelper != null)
            {
                circleDetectHelper.SetDetectRadius(status.archerDetectRange);
                
                // [COMBAT TAGS] targetTags chỉ chứa các tag được định nghĩa trong ArcherTargets (Enemy, EnemyPortal, Animal)
                // Player KHOONG được thêm ở đây — tránh Archer coi Player là mục tiêu tấn công
                targetTags = new List<string>(Script.Utils.TargetLists.ArcherTargets);

                // [DETECT TAGS] Detector cần thấy cả Player để lògic DropCoin hoạt động
                // Nhưng đây là list riêng, TACH BIỆT với targetTags chiến đấu
                var detectorTags = new List<string>(targetTags);
                if (!detectorTags.Contains(Script.Utils.GameTags.Player))
                    detectorTags.Add(Script.Utils.GameTags.Player);
                    
                circleDetectHelper.tagsToDetect = detectorTags;
            }
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
            
            // Tự động được handle time events bởi DenizenRoleController

            // Kiểm tra trạng thái ban đầu (nếu chưa được thiết lập)
            if (timeManager != null && !guardManager.isEnemyAttack)
            {
                status.archerState = timeManager.IsDaytime ? DenizenStatus.ArcherState.Hunting : DenizenStatus.ArcherState.HoldingWall;
            }
        }
        


        // Time events được manage bởi DenizenRoleController - không subscribe ở đây

        private void OnDestroy()
        {
            // if (guardManager != null) 
            // {
            //     guardManager.UnregisterArcher(gameObject);
            // }
            UnsubscribeFromTimeEvents();
            // Việc hủy đăng ký được xử lý bởi Controller, gọi 2 lần sẽ bị double call sinh ra Warning
            CleanupReferences();
        }

        private void UnsubscribeFromTimeEvents()
        {
            // Time events managed by DenizenRoleController
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

        }

        // --- INPCBehavior implementation ---
        public bool HandleItem(GameObject item)
        {
            if (!this.enabled) return false;

            var coinScript = item.GetComponent<PhysicalItem>();
            if (coinScript == null) return false;

            if (coinScript.itemType == ItemType.Coin && coinScript.denizensPickupAble)
            {
                var coinStorage = GetComponent<ICoinable>();
                if (coinStorage != null && coinStorage.Coin < coinStorage.MaxCoins)
                {
                    coinStorage.AddCoin(1);
                    return true;
                }
            }
            return false;
        }

        public void UpdateBehavior()
        {
            // [SCAN] Cho phép Detector quét chủ động (CircleDetect)
            if (circleDetectHelper != null && circleDetectHelper.isActive)
            {
                circleDetectHelper.ScanCircle();
            }

            if (status == null || animator == null)
            {
                if (status == null)
                    Debug.LogError("[Archer] UpdateBehavior called with null status!");
                return;
            }

            // Sync debug range
            if (circleDetectHelper != null)
            {
                circleDetectHelper.isShowGizmo = isDebugAttackRange;
            }
            
            try
            {
                // Kiểm tra trạng thái báo động từ GuardManager
                // Ưu tiên rõ ràng: Nếu báo động -> Returning/Holding (Nhưng NOT khi đang nhả coin - player ưu tiên hơn)
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
                        
                    case DenizenStatus.ArcherState.DropCoinToPlayer:
                        HandleDropCoinToPlayerState();
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
            // Note: Scan logic is handled in UpdateBehavior -> PerformTargetScan()
            // status.currentTarget is already up to date.

            // 3. Visual Debug (Vẽ ngay nếu có target)
            if (status.currentTarget != null && isDebugShotTarget)
            {
                Script.Utils.DebugUtility.DrawTargetIndicator(status.currentTarget.transform.position, Color.red, 5f);
            }

            // 4. Logic Tấn công / Di chuyển
            if (status.currentTarget != null)
            {
                status.currentSpeed = 0f;
                // Quay mặt về phía mục tiêu
                FaceTarget(status.currentTarget.transform.position);

                // Chỉ tấn công khi cooldown đã xong
                if (Time.time >= lastAttackTime + status.attackSpeed)
                {
                    PerformAttack();
                }
                return;
            }

            // Không tìm thấy mục tiêu -> Đi tuần


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
            SetAnimState("archerMove");
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
                // Visual Debug
                if (isDebugShotTarget)
                {
                 //   Debug.Log($"[Archer] Drawing target indicator at {status.currentTarget.transform.position}, IsDebugEnabled: {Script.Utils.DebugUtility.IsDebugEnabled}");
                    Script.Utils.DebugUtility.DrawTargetIndicator(status.currentTarget.transform.position, Color.red, 1f);
                }

                status.currentSpeed = 0f;
                FaceTarget(status.currentTarget.transform.position);
                
                // FIX: Thêm logic tấn công vào đây (trước đó bị thiếu)
                if (Time.time >= lastAttackTime + status.attackSpeed)
                {
                    PerformAttack();
                }
                else
                {
                    // Nếu đang chờ cooldown mà chưa Attack, chuyển về Idle để tránh kẹt animation "Run"
                    // Chỉ play Idle nếu không đang play Draw/Release
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("archerDraw") && !stateInfo.IsName("archerRelease"))
                    {
                         SetAnimState("archerIdle");
                    }
                }
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
                 SetAnimState("archerMove");
                 return;
            }
            
            // Idling at post
            status.currentSpeed = 0f;
            status.currentTarget = null;
            FaceOutward();
            SetAnimState("archerIdle");
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
                SetAnimState("archerMove");
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
                SetAnimState("archerMove");
            }
            else
            {
                // Đã đến nơi
                TransitionToState(DenizenStatus.ArcherState.HoldingWall);
            }
        }  // ← đóng HandleReturningToBaseState

        private void HandleDropCoinToPlayerState()
        {
            if (coinSharing == null || !coinSharing.CanDropCoin())
            {
                // Hết coin hoặc mất component -> quay về state cũ
                TransitionToState(status.previousArcherState);
                return;
            }

            // Kiểm tra xem player còn trong vùng radar không
            bool playerInRange = false;
            if (circleDetectHelper != null)
            {
                foreach (var hitObject in circleDetectHelper.GetListDetectedObjects())
                {
                    if (hitObject != null && hitObject.CompareTag(Script.Utils.GameTags.Player))
                    {
                        playerInRange = true;
                        break;
                    }
                }
            }

            if (playerInRange)
            {
                coinSharing.TryDropCoin();
                // Luôn giữ idle khi thả coin
                SetAnimState("archerIdle");
                status.currentSpeed = 0f;
            }
            else
            {
                // Player đi mất -> Hồi phục state cũ
                TransitionToState(status.previousArcherState);
            }
        }
        
        #endregion

        #region Combat & Movement Helpers
        
        public void OnObjectsDetected(List<GameObject> detectedObjects)
        {
            // Nếu đang trong DropCoinToPlayer, HandleDropCoinToPlayerState() tự xử lý
            // Không để detection gọi tiếp, tránh xung đột state
            if (status.archerState == DenizenStatus.ArcherState.DropCoinToPlayer) return;

            // --- Ưu tiên 1: Tìm Enemy/Target chiến đấu trước ---
            GameObject combatTarget = SelectTargetAndReportThreats(detectedObjects);
            
            if (combatTarget != null)
            {
                // Có enemy → gán target, hủy DropCoin nếu đang chờ
                status.currentTarget = combatTarget;
                return;
            }

            // --- Ưu tiên 2: Không có enemy → Kiểm tra Player để DropCoin ---
            status.currentTarget = null;
            
            // Nếu Archer đang trong quá trình thực hiện animation tấn công, không chuyển state để tránh ngắt quãng
            if (IsPlayingAttackAnimation()) return;
            
            foreach (var hitObject in detectedObjects)
            {
                if (hitObject != null && hitObject.CompareTag(Script.Utils.GameTags.Player))
                {
                    if (coinSharing != null && coinSharing.CanDropCoin())
                    {
                        status.previousArcherState = status.archerState;
                        TransitionToState(DenizenStatus.ArcherState.DropCoinToPlayer);
                        return;
                    }
                    break; // Tìm thấy player nhưng không thể drop → thôi
                }
            }
        }





        private void PerformAttack()
        {
            if (Time.time < lastAttackTime + status.attackSpeed) return;

            if (status.currentTarget != null) 
            {
              //  Debug.Log($"[Archer] Attacking {status.currentTarget.name}");
            }
            
            // Check if attackSpeed is valid
            if (status.attackSpeed <= 0)
            {
                Debug.LogWarning($"[Archer] Attack Speed is {status.attackSpeed}! Cooldown will be instant.");
            }
            else
            {
               //  Debug.Log($"[Archer] Performing Attack. Cooldown will reset to {status.attackSpeed}s");
            }

            // Bắt đầu chuỗi animation
            SetAnimState("archerDraw");
            
            lastAttackTime = Time.time;
        }
        
        public void HandleDrawComplete()
        {
           // Debug.Log("[Archer] OnDrawComplete event received");
            SetAnimState("archerRelease");
        }
        
        private bool IsPlayingAttackAnimation()
        {
            if (animator == null) return false;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName("archerDraw") || stateInfo.IsName("archerRelease");
        }
        
        public void HandleArrowRelease()
        {
            // Bắn tên (Placeholder)
            if (isDebugShotTarget && status.currentTarget != null)
            {
               // Debug.Log($"[Archer] Shot at {status.currentTarget.name} at {status.currentTarget.transform.position}");
            }
            // Logic bắn thực tế
            if (status.currentTarget != null && archerShooting != null)
            {
                archerShooting.ShootAt(status.currentTarget.transform.position);
            }
            
            // CRITICAL: Force play Idle to prevent Animator from auto-looping back to Draw
            // Only PerformAttack (when cooldown <= 0) should trigger Draw again
            SetAnimState("archerIdle");
        }

        private void MoveTowardsPosition(Vector3 pos, float speed)
        {
            if (isDebugMovePos)
            {
                Debug.DrawLine(transform.position, pos, Color.yellow);
            }

            _moveTarget = pos;
            _moveSpeed = speed;
            _isMovingToTarget = true;
            
            Vector3 direction = (pos - transform.position).normalized;
            if (direction.x != 0)
            {
                float scaleX = direction.x > 0 ? 1f : -1f;
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
        }
        
        private void Update()
        {
            if (!_isMovingToTarget) return;

            Vector3 direction = (_moveTarget - transform.position).normalized;
            transform.position += direction * _moveSpeed * Time.deltaTime;
            status.currentSpeed = _moveSpeed;

            if (Vector2.Distance(transform.position, _moveTarget) < 0.1f)
            {
                _isMovingToTarget = false;
                status.currentSpeed = 0f;
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
                case DenizenStatus.ArcherState.DropCoinToPlayer:
                    status.currentSpeed = 0f;
                    SetAnimState("archerIdle");
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
                cachedRetreatTarget = wallManager.GetWallBehind(status.previousWall);
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

        /// <summary>Gọi bởi DenizenRoleController khi đồng hồ chạm giờ 19.</summary>
        public void OnNightFalls()
        {
            if (status == null) return;
            if (status.archerState == DenizenStatus.ArcherState.Hunting)
            {
                TransitionToState(DenizenStatus.ArcherState.ReturningToBase);
            }
        }

        /// <summary>
        /// SelectTargetAndReportThreats: Vừa chọn mục tiêu ưu tiên, vừa báo cáo Enemy cho GuardManager
        /// </summary>
        private GameObject SelectTargetAndReportThreats(List<GameObject> allHits)
        {
            // [SYNC] Đảm bảo targetTags luôn được khởi tạo từ GameTags.ArcherTargets
            if (targetTags == null || targetTags.Count == 0) 
            {
                InitializeDetector();
                if (targetTags == null || targetTags.Count == 0) return null;
            }

            // 1. Phân loại mục tiêu dựa trên danh sách ưu tiên
            string priorityTag = targetTags[0]; // Ưu tiên cao nhất (thường là Enemy)
            
            List<GameObject> enemies = new List<GameObject>();
            List<GameObject> secondaryTargets = new List<GameObject>();

            foreach(var obj in allHits)
            {
                if (obj == null || !obj.activeInHierarchy) continue;

                // Bỏ qua Ally (Player, Villager, Archer...) — không bao giờ tấn công Ally
                bool isAlly = false;
                foreach (var allyTag in Script.Utils.TargetLists.AllyTags)
                {
                    if (!string.IsNullOrEmpty(allyTag) && obj.CompareTag(allyTag))
                    {
                        isAlly = true;
                        break;
                    }
                }
                if (isAlly) continue;

                // Kiểm tra Priority Tag (thường là Enemy)
                if (!string.IsNullOrEmpty(priorityTag) && obj.CompareTag(priorityTag))
                {
                    enemies.Add(obj);
                }
                else 
                {
                    // Kiểm tra các tag còn lại (Secondary Targets)
                    for (int i = 1; i < targetTags.Count; i++)
                    {
                        string t = targetTags[i];
                        if (string.IsNullOrEmpty(t)) continue;

                        if (obj.CompareTag(t))
                        {
                            secondaryTargets.Add(obj);
                            break;
                        }
                    }
                }
            }

            // Sắp xếp theo khoảng cách
            enemies.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
            secondaryTargets.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

            // 2. Chọn mục tiêu ưu tiên
            
            // Ưu tiên 1: Kẻ thù (Luôn tấn công nếu có)
            // Ưu tiên 1: Kẻ thù chính (Priority Tag)
            if (enemies.Count > 0)
            {
                // Register primary threats with GuardManager
                if (guardManager != null)
                {
                    foreach (var e in enemies) guardManager.RegisterEnemy(e);
                }
                return enemies[0];
            }

            // Ưu tiên 2: Các mục tiêu phụ (Hunting Targets) - Chỉ săn vào ban ngày
            if (timeManager != null && timeManager.IsDaytime && secondaryTargets.Count > 0)
            {
                return secondaryTargets[0];
            }

            return null;
        }

        private bool IsAtDefensivePosition()
        {
            Vector3 targetPos = GetDefensivePosition(status.assignedWall);
            return Vector2.Distance(transform.position, targetPos) <= WALL_ARRIVAL_DISTANCE + 0.5f; // Slight buffer
        }

        /// <summary>Gọi bởi DenizenRoleController khi đồng hồ chạm giờ 7.</summary>
        public void OnDayBreaks()
        {
            if (status == null) return;
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
            // DropCoins(1); // Gỡ bỏ việc drop coin theo yêu cầu: mất luôn không rơi ra
            return true;
        }

        public void DropCoins(int amount)
        {
            if (Spawner.Instance != null)
            {
                // ✅ Dùng pool thay Instantiate trực tiếp — tránh heap allocation mỗi lần drop
                Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + 2f);
                Spawner.Instance.DropByDenizen(spawnPosition, amount, dropDirection);
            }
        }
    }
}
