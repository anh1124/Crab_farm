using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Script.Game.Other;
using Script.Data_Model;
using Script.Interfaces;
using Script.Game.Item;
using Script.Game.Item.Coin;
using Script.Game.Mobs.MobsManager;
using Script.Manager;
using Script.Manager.WorldManager;
using Script.Manager.GamePlayManager;
using Script.Utils; // For Utility helper

namespace Script.Game.Mobs.Subjects
{
    // Renamed from VillagerController to VillagerBehavior to reflect Behavior + Status architecture
    public class VillagerBehavior : MonoBehaviour, ICoinShield, INPCBehavior, IItemHandle, IDamageable
    {
        #region Variables and Properties
        private DenizenStatus status;

        [Header("State")]
        // status.villagerState is the source of truth now
        
        public Vector3 targetPosition;
   
        public bool isMoving;
        public bool isInDanger;
        public float fearTimer;
        public const float FEAR_DURATION = 3f; // Giảm xuống 3 giây

        [Header("Tool")]
        [Tooltip("Công cụ được giao để đi lấy")]
        public PhysicalItem currentTool;//dùng để xác nhận công cụ này có còn không hoặc lấy tọa độ chạy đến
        
        [Header("Debug")]
        [SerializeField] private bool isDebugGoPlace = false;

        // Component References
        private Animator animator;
        private RayCastHelper rayCastHelper;
        private Coroutine fearRoutine;

        [Header("Coin Drop Settings")]
        [Tooltip("Thời gian tối thiểu giữa các lần drop coin khi gặp player (giây)")]
        public float COIN_DROP_COOLDOWN = 0.7f;
        private float lastCoinDropTime = 0f;

        // Kiểm tra tool định kỳ khi Idle
        
        [Header("Idle tool check")]
        public float _lastToolCheckTime = -999f;
        private const float TOOL_CHECK_INTERVAL = 2f;

        // Properties
        public int CurrentCoins => status.CurrentCoins;
        public int CurrentCoin { get => status.CurrentCoins; set => status.CurrentCoins = value; }
        public int MaxCoins => status.MaxCoins;
        
        // --- ICoinShield Implementation ---
        public bool HasCoinShield => status.CurrentCoins > 0;

        public bool ConsumeCoinShield()
        {
            if (HasCoinShield)
            {
                OnCoinDamage(true); // Drop coin
                return true;
            }
            return false;
        }

        // --- IDamageable Interface Implementation ---
        public void ReceiveDamage(int damage, UnityEngine.GameObject instigator)
        {
            if (damage <= 0) return;

            // Dân làng dùng Coin đở đòn trước
            if (HasCoinShield)
            {
                if (ConsumeCoinShield()) return;
            }

            // Hết xu thì để DenizenRoleController.OnCoinChangedHandler xử lý (đổi về Vagrant)
            status.CurrentCoins = Mathf.Max(0, status.CurrentCoins - damage);
        }

        [ContextMenu("Debug / Simulate Damage")]
        private void SimulateDamage()
        {
            Debug.Log($"<color=orange>[VillagerBehavior] Simulating 1 damage on {gameObject.name}. coins={status?.CurrentCoins}</color>");
            Script.Utils.DamageHandler.ApplyDamage(gameObject, 1, gameObject);
        }

        #endregion

        #region Unity Lifecycle Methods
        private void Awake()
        {
            gameObject.tag = "Villager";
            status = GetComponent<DenizenStatus>();
            if (status == null)
            {
                // Debug.LogWarning($"{gameObject.name} have no script DenizenStatus, has auto add");
                status = gameObject.AddComponent<DenizenStatus>();
            }
            
            
            rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper == null)
            {
                rayCastHelper = gameObject.AddComponent<RayCastHelper>();
            }
        }
    
        private void OnEnable()
        {
            InitializeVillager();
            
            if (NPCUpdateManager.Instance != null)
                NPCUpdateManager.Instance.Register(this);

            if (rayCastHelper != null)
            {
                rayCastHelper.isActive = true;
                rayCastHelper.OnDetectedObjectsChanged -= OnObjectsDetected; // Prev double sub
                rayCastHelper.OnDetectedObjectsChanged += OnObjectsDetected;
            }

            // Subscribe to time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SubscribeToHourlyEvent(19, HandleNightTime);
                GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleDayTime);
            }
        }

        private void Start()
        {
            CheckForToolsOnSpawn();
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null)
                NPCUpdateManager.Instance.Unregister(this);
            
            if (rayCastHelper != null)
            {
                rayCastHelper.OnDetectedObjectsChanged -= OnObjectsDetected;
                rayCastHelper.isActive = false;
            }

            // Unsubscribe from time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(19, HandleNightTime);
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, HandleDayTime);
            }
            // StopCheckRoutine(); // Removed
            
            if (rayCastHelper != null) rayCastHelper.isActive = false;
            
            if (currentTool != null && currentTool.pickedBy == null)
            {
                currentTool.AssignedCarrier = null;
            }
            currentTool = null;
        }

        private void OnDestroy()
        {
            // Cleanup handled in OnDisable
        }



        public void OnObjectsDetected(List<GameObject> detectedObjects)
        {
            // Check for enemies (Priority 1)
            foreach (var hitObject in detectedObjects)
            {
                if (hitObject != null && hitObject.CompareTag("Enemy"))
                {
                    isInDanger = true;
                    fearTimer = FEAR_DURATION;
                    status.villagerState = DenizenStatus.VillagerState.Fear;
                    return; // Found danger, prioritize it over anything else
                }
            }

            // Check for Player (Priority 2 - Drop Coin if rich)
            foreach (var hitObject in detectedObjects)
            {
                if (hitObject != null && hitObject.CompareTag("Player"))
                {
                    // Nếu gặp player và có trên 1 coin, đứng lại xin tiền
                    if (!isInDanger && status.CurrentCoins > 1 && status.villagerState == DenizenStatus.VillagerState.Idle)
                    {
                        status.villagerState = DenizenStatus.VillagerState.DropCoinToPlayer;
                    }
                    return;
                }
            }
        }

        public void UpdateBehavior()
        {
            // [SCAN] Chủ động quét
            if (rayCastHelper != null && rayCastHelper.isActive)
            {
                rayCastHelper.DrawRay();
            }

            // Kiểm tra tool định kỳ khi đang Idle (mỗi 2 giây)
            if (currentTool == null &&
                status.villagerState == DenizenStatus.VillagerState.Idle &&
                Time.time - _lastToolCheckTime >= TOOL_CHECK_INTERVAL)
            {
                _lastToolCheckTime = Time.time;
                CheckForToolsOnSpawn();
            }

            // Debug Visualization
            if (isDebugGoPlace && isMoving)
            {
                Debug.DrawLine(transform.position, targetPosition, Color.yellow);
            }

            // Update fear timer
            if (fearTimer > 0)
            {
                fearTimer -= Time.deltaTime;
                if (fearTimer <= 0 && status.villagerState == DenizenStatus.VillagerState.Fear)
                {
                    isInDanger = false;
                    // Kiểm tra thời gian để quyết định trạng thái tiếp theo
                    if (GameTimeManager.Instance != null)
                    {
                        int currentHour = GameTimeManager.Instance.GameHour;
                        if (currentHour >= 19 || currentHour < 6)
                        {
                            status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                        }
                        else
                        {
                            status.villagerState = DenizenStatus.VillagerState.Idle;
                        }
                    }
                    else
                    {
                        status.villagerState = DenizenStatus.VillagerState.Idle;
                    }
                }
            }

            UpdateStateHandler();
        }

        public void AssignTool(PhysicalItem tool)
        {
            if (tool == null || currentTool != null || isInDanger) return;
            
            if (status.villagerState == DenizenStatus.VillagerState.Idle || 
                status.villagerState == DenizenStatus.VillagerState.MoveBackToTownCenter)
            {
                currentTool = tool;
                status.villagerState = DenizenStatus.VillagerState.RunToGetTool;
                // Debug.Log($"[VillagerBehavior] Tool assigned by Manager: {tool.name}");
            }
        }


        #endregion

        #region Initialization
     
        private void InitializeVillager()
        {
            if (status == null) status = GetComponent<DenizenStatus>();
            if (rayCastHelper == null) rayCastHelper = GetComponent<RayCastHelper>();
            
            if (rayCastHelper != null)
            {
                rayCastHelper.SetRayLength(status.detectRangeRaycast);
                // [SYNC SCAN SETTINGS]
                rayCastHelper.tagsToDetect = new List<string> { "Enemy", "Player" };
            }

            animator = GetComponent<Animator>();

            // Kiểm tra thời gian ngay khi khởi tạo
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                // Nếu là ban đêm (19h-6h), set state về MoveBackToTownCenter
                if (currentHour >= 19 || currentHour < 6)
                {
                    status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                }
                else
                {
                    // Force Idle state during day for initialization to ensure tool check runs
                    status.villagerState = DenizenStatus.VillagerState.Idle;
                }
            }
            else
            {
                // Default to Idle if time manager missing
                status.villagerState = DenizenStatus.VillagerState.Idle;
            }
            
            isMoving = false;
            fearTimer = 0f;
        }

        /// <summary>
        /// Check for available tools when Villager spawns.
        /// Called from Start() to ensure ToolManager is initialized.
        /// </summary>
        private void CheckForToolsOnSpawn()
        {
            // Only check if we don't have a tool and aren't in danger
            if (currentTool != null || isInDanger) return;

            // Allow tool check for Idle or MoveBackToTownCenter states
            if (status.villagerState != DenizenStatus.VillagerState.Idle && 
                status.villagerState != DenizenStatus.VillagerState.MoveBackToTownCenter)
            {
                return;
            }

            if (Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                var tool = Script.Manager.MobsManager.ToolManager.Instance.GetNearestTool(transform.position);
                //Debug.Log($"[VillagerBehavior] CheckForToolsOnSpawn: {gameObject.name} at {transform.position}. Found tool: {(tool != null ? tool.name : "null")}");
                
                if (tool != null)
                {
                    Debug.Log($"[VillagerBehavior] Assigning tool {tool.name} to {gameObject.name}");
                    AssignTool(tool);
                }
            }
            else
            {
                Debug.LogWarning("[VillagerBehavior] ToolManager Instance is null during CheckForToolsOnSpawn!");
            }
        }
        #endregion

        #region Time Management
        private void HandleNightTime()
        {
            if (status.villagerState != DenizenStatus.VillagerState.RunToGetTool)
            {
                status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
            }
        }

        private void HandleDayTime()
        {
            if (status.villagerState == DenizenStatus.VillagerState.MoveBackToTownCenter)
            {
                status.villagerState = DenizenStatus.VillagerState.Idle;
            }
        }
        #endregion

        #region State Management
        /// <summary>
        /// Điều phối trạng thái của Villager dựa trên độ ưu tiên (Priority):
        /// 1. Nguy hiểm (Fear) - Cao nhất.
        /// 2. Ban đêm (Về nhà) - Nếu không nguy hiểm.
        /// 3. Gặp Player (Xin tiền) - Nếu có nhiều coin.
        /// 4. Lấy Tool (Làm việc) - Nếu được giao việc.
        /// 5. Idle (Lang thang) - Mặc định ban ngày.
        /// </summary>
        private void UpdateStateHandler()
        {
            // 1. Ưu tiên cao nhất: Gặp nguy hiểm -> Chạy trốn ngay
            if (isInDanger)
            {
                if (status.villagerState != DenizenStatus.VillagerState.Fear)
                {
                    // Debug.Log($"[VillagerBehavior] {gameObject.name} chuyển sang FEAR do isInDanger = true (UpdateStateHandler)");
                    status.villagerState = DenizenStatus.VillagerState.Fear;
                    return;
                }
            }

            // 2. Ưu tiên nhì: Được giao đi lấy Tool -> Chạy đi lấy
            if (currentTool != null && !isInDanger && status.villagerState != DenizenStatus.VillagerState.RunToGetTool)
            {
                status.villagerState = DenizenStatus.VillagerState.RunToGetTool;
                return;
            }

            // 3. Ưu tiên ba: Ban đêm (19h - 6h) -> Về nhà an toàn (CHỈ KHI KHÔNG CÓ TOOL)
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                if ((currentHour >= 19 || currentHour < 6) && 
                    !isInDanger && 
                    currentTool == null && 
                    status.villagerState != DenizenStatus.VillagerState.MoveBackToTownCenter &&
                    status.villagerState != DenizenStatus.VillagerState.DropCoinToPlayer) // Don't interrupt coin dropping
                {
                    status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                    return;
                }
            }

            // 4. Ưu tiên bốn: Gặp Player và có nhiều tiền -> Đứng lại thả tiền

            // 5. Mặc định: Ban ngày ko có việc gì thì đi lang thang (Idle)
            if (!isInDanger && currentTool == null && status.villagerState == DenizenStatus.VillagerState.MoveBackToTownCenter)
            {
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 6 && currentHour < 19)
                    {
                        status.villagerState = DenizenStatus.VillagerState.Idle;
                    }
                }
            }

            // Thực thi logic của từng trạng thái cụ thể
            switch (status.villagerState)
            {
                case DenizenStatus.VillagerState.Idle:
                    HandleIdleState();
                    break;
                case DenizenStatus.VillagerState.MoveBackToTownCenter:
                    HandleMoveToTownCenterState(false);
                    break;
                case DenizenStatus.VillagerState.RunToGetTool:
                    HandleRunToGetToolState();
                    break;
                case DenizenStatus.VillagerState.Fear:
                    HandleFearState();
                    break;
                case DenizenStatus.VillagerState.DropCoinToPlayer:
                    HandleDropCoinToPlayerState();
                    break;
            }
        }
        #endregion

        #region State Handlers
        private void HandleIdleState()
        {
            if (!isMoving)
            {
                // Get random position in plaza area
                float randomX = Utility.GetRandomPlazaPos();
                targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
                isMoving = true;
            }
            
            if (isMoving)
            {
                MoveTowardsTarget(targetPosition, "walk");
            }

            // Check if it's night time
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.GameHour >= 19)
            {
                status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
            }
        }

        private void HandleMoveToTownCenterState(bool isRunning)
        {
            if (!isMoving || Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                // Get random position in plaza area
                float randomX = Utility.GetRandomPlazaPos();
                targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
                isMoving = true;
            }

            if (isMoving)
            {
                MoveTowardsTarget(targetPosition, isRunning ? "run" : "walk");
            }
        }

        private void HandleRunToGetToolState()
        {
            if (currentTool == null)
            {
                TransitionToIdle();
                return;
            }

            // Kiểm tra xem tool có bị picked trước không
            if (currentTool.pickedBy != null)
            {
                currentTool = null;
                TransitionToIdle();
                return;
            }

            // Nếu đang trong trạng thái di chuyển
            // Di chuyển đến vị trí của tool với animation chạy
            if (isMoving)
            {
                MoveTowardsTarget(currentTool.transform.position, "run");
            }
            else
            {
                // Force move start if somehow stopped
                isMoving = true; 
                MoveTowardsTarget(currentTool.transform.position, "run");
            }
        }

        public void TransitionToIdle()
        {
            status.villagerState = DenizenStatus.VillagerState.Idle;
            Script.Manager.MobsManager.ToolManager.Instance?.OnVillagerBecameIdle(this);
        }

        private void HandleFearState()
        {
            if (!isMoving || Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                float randomX = Utility.GetRandomPlazaPos();
                targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
                isMoving = true;
            }

            if (isMoving)
            {
                MoveTowardsTarget(targetPosition, "run");
            }
        }

        private void HandleDropCoinToPlayerState()
        {
            // Kiểm tra số coin, nếu chỉ còn 1 thì chuyển sang trạng thái khác
            if (status.CurrentCoins <= 1)
            {
                // Debug.Log($"[VillagerBehavior] {gameObject.name} rời khỏi DropCoinToPlayer vì chỉ còn {status.CurrentCoins} coin");
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 19 || currentHour < 6)
                    {
                        status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                    }
                    else
                    {
                        TransitionToIdle();
                    }
                }
                else
                {
                    TransitionToIdle();
                }
                return;
            }

            // Kiểm tra player còn trong vùng quét không
            bool playerInRange = false;
            foreach (var hitObject in rayCastHelper.GetListHitObjects())
            {
                if (hitObject != null && hitObject.CompareTag("Player"))
                {
                    playerInRange = true;
                    break;
                }
            }

            float currentTime = Time.time;
            if (playerInRange)
            {
                // Nếu cooldown đã hết và còn coin thì drop coin
                if (currentTime - lastCoinDropTime >= COIN_DROP_COOLDOWN && status.CurrentCoins > 1) // Đảm bảo luôn còn ít nhất 1 coin
                {
                    InternalDropCoins(1, false); // Rõ ràng là đang thả cho player
                    lastCoinDropTime = currentTime;
                }
                // Chuyển animation sang idle khi đứng chờ player
                if (animator != null)
                {
                    animator.Play("idle");
                }
                // Giữ nguyên state DropCoinToPlayer cho đến khi player rời khỏi vùng quét
                return;
            }
            // Nếu player không còn trong vùng quét, chuyển về state phù hợp
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                if (currentHour >= 19 || currentHour < 6)
                {
                    status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                }
                else
                {
                    TransitionToIdle();
                }
            }
            else
            {
                TransitionToIdle();
            }
        }
        #endregion

        #region Movement
        private float _currentMoveSpeed = 0f;

        private void MoveTowardsTarget(Vector3 target, string animationName)
        {
            _currentMoveSpeed = animationName == "run" ? status.RunSpeed : status.WalkSpeed;
            targetPosition = target;
            isMoving = true;
            
            if (animator != null)
            {
                animator.Play(animationName);
            }
        }

        private void Update()
        {
            if (!isMoving) return;

            bool hasReachedTarget = Utility.MoveTowardsTarget(gameObject, targetPosition, _currentMoveSpeed);

            if (hasReachedTarget)
            {
                isMoving = false;
                if (animator != null) animator.Play("idle");
            }
        }
        #endregion

        #region Coin System
        public void OnCoinDamage()
        {
            OnCoinDamage(true);
        }

        public void OnCoinDamage(bool isScared = true)
        {
            if (status.CurrentCoins > 0)
            {
                InternalDropCoins(1, true); // Rõ ràng là đang bị tấn công
                
                if (isScared && status.villagerState != DenizenStatus.VillagerState.RunToGetTool)
                {
                    // Debug.Log($"[VillagerBehavior] {gameObject.name} bị mất coin, chuyển sang FEAR (OnCoinDamage)");
                    isInDanger = true;
                    fearTimer = FEAR_DURATION;
                    status.villagerState = DenizenStatus.VillagerState.Fear;
                }
            }
        }

        // Interface implementation - chỉ dùng khi bị tấn công
        public void DropCoins(int amount)
        {
            InternalDropCoins(amount, true);
        }

        // Phương thức nội bộ để xử lý logic drop coin
        private void InternalDropCoins(int amount, bool isAttacked)
        {
            // Kiểm tra điều kiện giữ lại coin
            int minCoinsToKeep = isAttacked ? 0 : 1;
            
            // Nếu số coin hiện tại <= số coin cần giữ lại, không làm gì cả
            if (status.CurrentCoins <= minCoinsToKeep) return;
            
            // Luôn chỉ drop 1 coin
            amount = 1;
            // Debug.Log($"[VillagerBehavior] {gameObject.name} chuẩn bị drop {amount} coin(s). CurrentCoins trước drop: {status.CurrentCoins}, isAttacked: {isAttacked}");
            
            status.CurrentCoins -= amount;
            // Debug.Log($"[VillagerBehavior] {gameObject.name} đã drop {amount} coin(s). CurrentCoins sau drop: {status.CurrentCoins}");
            
            if (Spawner.Instance != null)
            {
                StartCoroutine(DropCoinsCoroutine(amount));
            }
            // Việc mất role sẽ được event OnCoinChangedHandler đảm nhận.
        }

        private IEnumerator DropCoinsCoroutine(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + 2f);
                
                GameObject coin = Instantiate(Spawner.Instance.coinPrefab, spawnPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);
                
                PhysicalItem coinScript = coin.GetComponent<PhysicalItem>();
                if (coinScript != null)
                {
                    coinScript.playerPickupAble = true;
                    coinScript.denizensPickupAble = false;
                    coinScript.floating = false;
                    coinScript.CallFunctionCoolDown("Denizens");
                    
                    Spawner.Instance.InitializeCoinThrow(coin, dropDirection);
                }

                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator ConvertToVagrantAfterCoins()
        {
            // Wait for any ongoing coin drops to complete
            yield return new WaitForSeconds(0.5f);
            
            // Double check to prevent duplicate conversion
            if (this == null || !gameObject || !gameObject.activeInHierarchy) yield break;
            if (status.CurrentCoins > 0) yield break;
            
            ConvertToVagrant();
        }

        private void ConvertToVagrant()
        {
            if (this == null || !gameObject || !gameObject.activeInHierarchy) return;

            var controller = GetComponent<DenizenRoleController>();
            if (controller != null)
            {
                controller.ChangeRole(NPCRole.Vagrant);
                return;
            }
            Debug.LogError("[VillagerBehavior] DenizenRoleController missing!");
        }
        #endregion

        #region Save System
        // Removed explicit ISaveable implementation as DenizenRoleController handles it via DenizenStatus
        // However, if there are transient fields like currentTool that need saving, they should be moved to DenizenStatus.
        // For now, assuming currentTool is not persisted or handled elsewhere (ToolManager handles tool assignment).
        #endregion

        #region INPCBehavior Implementation

        public bool HandleItem(GameObject other)
        {
            // Only active if enabled
            if (!this.enabled) return false;

            var item = other.GetComponent<PhysicalItem>();
            if (item == null) return false;

            // 1. Logic nhặt Tool để đổi nghề
            if (item.itemType != ItemType.Coin)
            {
                // LootPickupTrigger đã đảm bảo an toàn cho pickedBy, ở đây ta chỉ cần check logic
                if (item.AssignedCarrier != null && item.AssignedCarrier != gameObject)
                {
                    item.OnPickedByStranger(gameObject);
                }

                // Xử lý tool bị bỏ rơi TRƯỚC KHI đổi nghề
                if (currentTool != null && currentTool != item)
                {
                    var abandonedTool = currentTool;
                    currentTool = null;
                    abandonedTool.AssignedCarrier = null;
                    abandonedTool.OnAssigneeAbandoned(); 
                }
                else
                {
                    currentTool = null;
                }
                
                // Đổi nghề
                ChangeRoleByTool(item.itemType);
                return true;
            }
            // 2. Logic nhặt Coin
            else
            {
                if (!item.denizensPickupAble) return false;

                var coinStorage = GetComponent<ICoinable>();
                if (coinStorage != null && coinStorage.Coin < coinStorage.MaxCoins)
                {
                    coinStorage.AddCoin(1);
                    return true;
                }
                return false;
            }
        }
        #endregion
        
        
        // Đã xóa PickUpCoinInternal vì logic cộng tiền đã được gộp trực tiếp vào HandleItem.
        // Hủy bỏ việc sử dụng ProcessCoinPickup vì LootPickupTrigger sẽ lo phần tiêu hủy.

        private void ChangeRoleByTool(ItemType type)
        {
            var ctrl = GetComponent<DenizenRoleController>();
            if (ctrl == null)
            {
                Debug.LogError($"[VillagerBehavior] {gameObject.name} missing DenizenRoleController!");
                return;
            }

            switch (type)
            {
                case ItemType.Tool_Bow:
                    ctrl.ChangeRole(NPCRole.Archer);
                    break;

                case ItemType.Tool_Hammer:
                    ctrl.ChangeRole(NPCRole.Worker);
                    break;

                case ItemType.Tool_Scythe:
                    ctrl.ChangeRole(NPCRole.Farmer);
                    break;

                default:
                    Debug.LogWarning($"[VillagerBehavior] Unknown tool type: {type}");
                    break;
            }
        }

    }
}
