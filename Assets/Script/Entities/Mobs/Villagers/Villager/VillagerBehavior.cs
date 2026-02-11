using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Script.Game.Other;
using Script.Data_Model;
using Script.Game.Enemy.Interfaces;
using Script.Game.Item;
using Script.Game.Item.Coin;
using Script.Game.Mobs.MobsManager;
using Script.Manager;
using Script.Manager.WorldManager;
using Script.Utils; // For Utility helper

namespace Script.Game.Mobs.Subjects
{
    // Renamed from VillagerController to VillagerBehavior to reflect Behavior + Status architecture
    public class VillagerBehavior : MonoBehaviour, IHealth, INPCBehavior, ICoinable
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
        public ToolItem currentTool;//dùng để xác nhận công cụ này có còn không hoặc lấy tọa độ chạy đến
        
        [Header("Debug")]
        [SerializeField] private bool isDebugGoPlace = false;

        // Component References
        private Animator animator;
        private RayCastHelper rayCastHelper;
        private Coroutine fearRoutine;
        private Coroutine checkRoutine;

        [Header("Coin Drop Settings")]
        [Tooltip("Thời gian tối thiểu giữa các lần drop coin khi gặp player (giây)")]
        public float COIN_DROP_COOLDOWN = 0.7f;
        private float lastCoinDropTime = 0f;

        // Properties
        public int CurrentCoins => status.CurrentCoins;
        public int MaxCoins => status.MaxCoins;
        
        // --- IHealth Implementation ---
        public int MaxHp 
        { 
            get => 1; 
            set { /* Villagers don't have scalable HP yet, strictly coin-based */ } 
        }
        
        public int CurrentHp 
        { 
            get => 1; 
            set { /* Ignored */ } 
        }

        public void TakeDamage(int damage)
        {
            OnCoinDamage(true);
        }

        public void Die()
        {
            ConvertToVagrant();
        }

        public void Heal(int amount)
        {
            // Villagers heal by getting coins? Check logic. For now, empty or add coin?
            // Usually Heal() implies HP. Villagers don't use HP.
        }

        // --- ICoinable Implementation ---
        public int Coin 
        { 
            get => status.CurrentCoins; 
            set => status.CurrentCoins = value; 
        }

        public void AddCoin(int amount)
        {
            status.CurrentCoins += amount;
        }

        public void SubtractCoin(int amount)
        {
            status.CurrentCoins -= amount; // Just data modification, use DropCoins for visual
        }

        public void PickUpCoin(GameObject coinObject)
        {
            PickUpCoinInternal(coinObject);
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

            // Subscribe to time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SubscribeToHourlyEvent(19, HandleNightTime);
                GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleDayTime);
            }
        }



        private void OnDisable()
        {
            // Unsubscribe from time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(19, HandleNightTime);
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, HandleDayTime);
            }
            StopCheckRoutine();
            
            // Note: VillagerManager removal means no central tool tracking via Manager cleanup here.
            // Tools should be handled by the DroppedItem system or WorldManager if needed.
            if (currentTool != null && !currentTool.picked)
            {
                // Logic to handle unpicked tool if needed, or just let it stay in world.
                // Releasing reference.
                currentTool = null;
            }
        }

        private void OnDestroy()
        {
            // Cleanup handled in OnDisable
        }



        public void UpdateBehavior()
        {
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

        public void AssignTool(ToolItem tool)
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
            // Immediate tool check on initialization
            // Allow check if Idle OR MovingBackToTownCenter (just spawned/loaded)
            if (currentTool == null && !isInDanger && 
                (status.villagerState == DenizenStatus.VillagerState.Idle || 
                 status.villagerState == DenizenStatus.VillagerState.MoveBackToTownCenter))
            {
                 if (Script.Manager.MobsManager.ToolManager.Instance != null)
                 {
                     var tool = Script.Manager.MobsManager.ToolManager.Instance.GetNearestTool(transform.position);
                     Debug.Log($"[VillagerBehavior] Initialize Check: {gameObject.name} at {transform.position}. Found tool: {(tool != null ? tool.name : "null")}");
                     
                     if (tool != null)
                     {
                         Debug.Log($"[VillagerBehavior] Assigning tool {tool.name} to {gameObject.name}");
                         AssignTool(tool);
                     }
                 }
                 else
                 {
                     Debug.LogError("[VillagerBehavior] ToolManager Instance is null!");
                 }
            }

            StartCheckRoutine();
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
            // Kiểm tra nếu không còn tool cần lấy nữa
            // hoặc tool đã bị hủy thì chuyển về trạng thái Idle
            if (currentTool == null)
            {
                status.villagerState = DenizenStatus.VillagerState.Idle;
                return;
            }

            // Kiểm tra xem tool có bị picked trước không
            if (currentTool.picked)
            {
                // Debug.Log($"[VillagerBehavior] {gameObject.name}: Tool đã bị nhặt trước, hủy nhiệm vụ");
                currentTool = null;
                status.villagerState = DenizenStatus.VillagerState.Idle;
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
                        status.villagerState = DenizenStatus.VillagerState.Idle;
                    }
                }
                else
                {
                    status.villagerState = DenizenStatus.VillagerState.Idle;
                }
                return;
            }

            // Kiểm tra player còn trong vùng quét không
            bool playerInRange = false;
            foreach (var hitObject in rayCastHelper.DrawRayAndReturnListHitObjects())
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
                    status.villagerState = DenizenStatus.VillagerState.Idle;
                }
            }
            else
            {
                status.villagerState = DenizenStatus.VillagerState.Idle;
            }
        }
        #endregion

        #region Movement
        private void MoveTowardsTarget(Vector3 target, string animationName)
        {
            float speed = animationName == "run" ? status.RunSpeed : status.WalkSpeed;
            
            bool hasReachedTarget = Utility.MoveTowardsTarget(gameObject, target, speed);
            
            animator.Play(animationName);

            if (hasReachedTarget)
            {
                isMoving = false;
                animator.Play("idle");
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

            // Chỉ chuyển thành vagrant khi bị tấn công và hết xu
            if (isAttacked && status.CurrentCoins <= 0)
            {
                StartCoroutine(ConvertToVagrantAfterCoins());
            }
        }

        private IEnumerator DropCoinsCoroutine(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + 2f);
                
                GameObject coin = Instantiate(Spawner.Instance.coinPrefab, spawnPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);
                
                Coin coinScript = coin.GetComponent<Coin>();
                if (coinScript != null)
                {
                    coinScript.playerPickupAble = true;
                    coinScript.denizensPickupAble = false;
                    coinScript.floating = false;
                    coinScript.CallFunctionCoolDown("Denizen");
                    
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
        // For now, assuming currentTool is not persisted or handled elsewhere (VillagerManager handles tool assignment).
        #endregion

        #region Trigger and Pickup
        /// <summary>
        /// Xử lý va chạm:
        /// 1. Nhặt Tool: Nếu gặp ToolItem -> Kiểm tra quyền sở hữu -> Đổi nghề (ChangeRole) -> Hủy tool.
        /// 2. Nhặt Coin: Nếu gặp Coin -> Gọi hàm nhặt.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
             // Only active if enabled
             if (!this.enabled) return;

            // 1. Logic nhặt Tool để đổi nghề
            var item = other.GetComponent<ToolItem>();
            if (item != null)
            {
                // Kiểm tra xem tool này chưa ai nhặt (Atomic check)
                if (!item.picked && item.pickedBy == null)
                {
                    // Đánh dấu đã nhặt ngay lập tức
                    item.pickedBy = gameObject;
                    item.picked = true;

                    // Đổi nghề dựa trên loại tool (Vd: Bow -> Archer)
                    ChangeRoleByTool(item.type);

                    // Xóa vật phẩm tool khỏi map
                    Destroy(other.gameObject);

                    // Reset trạng thái
                    currentTool = null;
                    status.villagerState = DenizenStatus.VillagerState.Idle;
                }
                else
                {
                    // Tool đã bị người khác nhanh tay nhặt trước
                    Debug.Log($"[VillagerBehavior] {gameObject.name} không thể nhặt tool (đã bị nhặt bởi {item.pickedBy?.name ?? "unknown"})");
                    currentTool = null;
                    status.villagerState = DenizenStatus.VillagerState.Idle;
                }
            }
            // 2. Logic nhặt Coin
            else if (other.CompareTag("Coin"))
            {
                // ... logic coin ...
                var coinScript = other.GetComponent<Coin>();
                if (coinScript == null || coinScript.denizensPickupAble)
                {
                    PickUpCoinInternal(other.gameObject);
                }
            }
        }
        
        
        private void PickUpCoinInternal(GameObject coinObject)
        {
            // Debug.Log($"[VillagerBehavior] PickUpCoinInternal called for {coinObject.name}");
            
            Script.Utility.ProcessCoinPickup(coinObject,
                condition: () => 
                {
                    // Debug.Log($"[VillagerBehavior] ProcessCoinPickup condition check - CurrentCoins: {status.CurrentCoins}/{status.MaxCoins}");
                    return status.CurrentCoins < status.MaxCoins;
                },
                onPickupSuccess: () =>
                {
                    // Debug.Log($"[VillagerBehavior] Coin pickup successful, adding coin");
                    status.CurrentCoins++;
                }
            );
        }

        /// <summary>
        /// Change villager role based on picked tool type
        /// </summary>
        private void ChangeRoleByTool(ToolType type)
        {
            var ctrl = GetComponent<DenizenRoleController>();
            if (ctrl == null)
            {
                Debug.LogError($"[VillagerBehavior] {gameObject.name} missing DenizenRoleController!");
                return;
            }

            switch (type)
            {
                case ToolType.Bow:
                    ctrl.ChangeRole(NPCRole.Archer);
                    break;

                case ToolType.Hammer:
                    ctrl.ChangeRole(NPCRole.Worker);
                    break;

                case ToolType.Scythe:
                    ctrl.ChangeRole(NPCRole.Farmer);
                    break;

                default:
                    Debug.LogWarning($"[VillagerBehavior] Unknown tool type: {type}");
                    break;
            }
        }
        #endregion

        #region Coroutine Management
        private void StartCheckRoutine()
        {
            StopCheckRoutine();
            checkRoutine = StartCoroutine(CheckRoutine());
        }

        private void StopCheckRoutine()
        {
            if (checkRoutine != null)
            {
                StopCoroutine(checkRoutine);
                checkRoutine = null;
            }
        }

        /// <summary>
        /// [QUAN TRỌNG] Vòng lặp kiểm tra môi trường mỗi 0.5s.
        /// - Quét xung quanh để tìm Kẻ Thù (Enemy) -> Kích hoạt trạng thái Sợ Hãi (Fear).
        /// - Cập nhật timer sợ hãi: Nếu hết giờ và an toàn -> Quay lại Idle hoặc về nhà.
        /// </summary>
        private IEnumerator CheckRoutine()
        {
            while (true)
            {
                // 1. Quét tìm mối nguy hiểm
                foreach (var hitObject in rayCastHelper.DrawRayAndReturnListHitObjects())
                {
                    if (hitObject != null)
                    {
                        if (hitObject.CompareTag("Enemy"))
                        {
                            isInDanger = true;
                            fearTimer = FEAR_DURATION;
                            status.villagerState = DenizenStatus.VillagerState.Fear;
                            break;
                        }
                    }
                }

                // 2. Đếm ngược thời gian sợ hãi
                if (fearTimer > 0)
                {
                    fearTimer -= Time.deltaTime;
                    // Hết sợ -> Quay lại trạng thái bình thường tùy theo giờ giấc
                    if (fearTimer <= 0)
                    {
                        isInDanger = false;
                        if (GameTimeManager.Instance != null)
                        {
                            int currentHour = GameTimeManager.Instance.GameHour;
                            // Đêm (19h-6h): Về nhà
                            if (currentHour >= 19 || currentHour < 6)
                            {
                                status.villagerState = DenizenStatus.VillagerState.MoveBackToTownCenter;
                            }
                            // Ngày: Đi lang thang
                            else
                            {
                                status.villagerState = DenizenStatus.VillagerState.Idle;
                            }
                        }
                    }
                }

                // 3. Tìm kiếm Tool nếu đang rảnh (Idle hoặc đang về nhà) và chưa có tool
                if (!isInDanger && currentTool == null && 
                    (status.villagerState == DenizenStatus.VillagerState.Idle || 
                     status.villagerState == DenizenStatus.VillagerState.MoveBackToTownCenter))
                {
                    if (Script.Manager.MobsManager.ToolManager.Instance != null)
                    {
                        var nearestTool = Script.Manager.MobsManager.ToolManager.Instance.GetNearestTool(transform.position);
                        if (nearestTool != null)
                        {
                            currentTool = nearestTool;
                            status.villagerState = DenizenStatus.VillagerState.RunToGetTool;
                            // Debug.Log($"[VillagerBehavior] {gameObject.name} found tool {nearestTool.name} via ToolManager");
                        }
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion
    }
}
