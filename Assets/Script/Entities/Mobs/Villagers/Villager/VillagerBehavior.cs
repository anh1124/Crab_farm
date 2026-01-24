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

namespace Script.Game.Mobs.Subjects
{
    // Renamed from VillagerController to VillagerBehavior to reflect Behavior + Status architecture
    public class VillagerBehavior : MonoBehaviour, ISaveable, IHealth, INPCBehavior
    {
        #region Variables and Properties
        private VillagerStatus status;

        [Header("State")]
        [Tooltip("Trạng thái hiện tại của villager")]
        [SerializeField] private VillagerStatus.VillagerState currentState;
        
        public Vector3 targetPosition;
   
        public bool isMoving;
        public bool isInDanger;
        public float fearTimer;
        public const float FEAR_DURATION = 3f; // Giảm xuống 3 giây

        [Header("Tool")]
        [Tooltip("Công cụ được giao để đi lấy")]
        public ToolItem currentTool;
        
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
        public int MaxCoin { get => status.MaxCoins; set { /* Không hỗ trợ set MaxCoins */ } }
        public int CurrentHp { get => 0; set { /* Không có HP, không làm gì */ } }
        #endregion

        #region Unity Lifecycle Methods
        private void Awake()
        {
            gameObject.tag = "Villager";
            status = GetComponent<VillagerStatus>();
            if (status == null)
            {
                Debug.LogWarning($"{gameObject.name} have no script VillageStatus, has auto add");
                status = gameObject.AddComponent<VillagerStatus>();
            }
        }
    
        private void OnEnable()
        {
            // Subscribe to time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SubscribeToHourlyEvent(19, HandleNightTime);
                GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleDayTime);
            }
            StartCheckRoutine();
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
        }

        private void OnDestroy()
        {
            // If villager is destroyed while having an assigned tool that hasn't been picked up yet,
            // re-register the tool so another villager can pick it up
            if (currentTool != null && !currentTool.picked && VillagerManager.Instance != null)
            {
                Debug.Log($"[VillagerBehavior] {gameObject.name} destroyed with unpicked tool {currentTool.type}, re-registering tool");
                VillagerManager.Instance.RegisterTool(currentTool);
                currentTool = null;
            }
        }

        private void Start()
        {
            InitComponent();
            InitializeVillager();
            
            // Registration is now handled by DenizenController when role is applied
        }

        public void UpdateBehavior()
        {
            // Update fear timer
            if (fearTimer > 0)
            {
                fearTimer -= Time.deltaTime;
                if (fearTimer <= 0 && currentState == VillagerStatus.VillagerState.Fear)
                {
                    isInDanger = false;
                    // Kiểm tra thời gian để quyết định trạng thái tiếp theo
                    if (GameTimeManager.Instance != null)
                    {
                        int currentHour = GameTimeManager.Instance.GameHour;
                        if (currentHour >= 19 || currentHour < 6)
                        {
                            currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                        }
                        else
                        {
                            currentState = VillagerStatus.VillagerState.Idle;
                        }
                    }
                    else
                    {
                        currentState = VillagerStatus.VillagerState.Idle;
                    }
                }
            }

            UpdateStateHandler();
        }
        #endregion

        #region Initialization
        private void InitComponent()
        {
            rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper == null)
            {
                rayCastHelper = gameObject.AddComponent<RayCastHelper>();
                Debug.LogWarning($"{gameObject.name} missing RayCastHelper, adding one");
            }
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"{gameObject.name} missing Animator component!");
            }
        }

        private void InitializeVillager()
        {
            // Kiểm tra thời gian ngay khi khởi tạo
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                // Nếu là ban đêm (19h-6h), set state về MoveBackToTownCenter
                if (currentHour >= 19 || currentHour < 6)
                {
                    currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                }
                else
                {
                    currentState = VillagerStatus.VillagerState.Idle;
                }
            }
            else
            {
                currentState = VillagerStatus.VillagerState.Idle;
            }
            
            isMoving = false;
            fearTimer = 0f;
            StartCheckRoutine();
        }
        #endregion

        #region Time Management
        private void HandleNightTime()
        {
            if (currentState != VillagerStatus.VillagerState.RunToGetTool)
            {
                currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
            }
        }

        private void HandleDayTime()
        {
            if (currentState == VillagerStatus.VillagerState.MoveBackToTownCenter)
            {
                currentState = VillagerStatus.VillagerState.Idle;
            }
        }
        #endregion

        #region State Management
        private void UpdateStateHandler()
        {
            // Fear state has highest priority when enemy is detected
            if (isInDanger)
            {
                if (currentState != VillagerStatus.VillagerState.Fear)
                {
                    Debug.Log($"[VillagerBehavior] {gameObject.name} chuyển sang FEAR do isInDanger = true (UpdateStateHandler)");
                    currentState = VillagerStatus.VillagerState.Fear;
                    return;
                }
            }

            // Night time state check (19:00 - 6:00) has second priority
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                if ((currentHour >= 19 || currentHour < 6) && 
                    !isInDanger && 
                    currentTool == null && 
                    currentState != VillagerStatus.VillagerState.MoveBackToTownCenter &&
                    currentState != VillagerStatus.VillagerState.DropCoinToPlayer) // Don't interrupt coin dropping
                {
                    currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                    return;
                }
            }

            // Check for player and handle coin drop state (third priority)
            if (currentState != VillagerStatus.VillagerState.DropCoinToPlayer) // Only check if not already dropping coins
            {
                // Chỉ xét vào trạng thái DropCoinToPlayer nếu có nhiều hơn 1 coin
                if (status.CurrentCoins > 1)
                {
                    foreach (var hitObject in rayCastHelper.DrawRayAndReturnListHitObjects())
                    {
                        if (hitObject != null && hitObject.CompareTag("Player"))
                        {
                            float currentTime = Time.time;
                            if (currentTime - lastCoinDropTime >= COIN_DROP_COOLDOWN && !isInDanger)
                            {
                                Debug.Log($"[VillagerBehavior] {gameObject.name} chuyển sang DropCoinToPlayer với {status.CurrentCoins} coins");
                                currentState = VillagerStatus.VillagerState.DropCoinToPlayer;
                                return;
                            }
                        }
                    }
                }
            }

            // Tool collection has fourth priority
            if (currentTool != null && !isInDanger && currentState != VillagerStatus.VillagerState.RunToGetTool)
            {
                currentState = VillagerStatus.VillagerState.RunToGetTool;
                return;
            }

            // Default to Idle during day if no other conditions are met
            if (!isInDanger && currentTool == null && currentState == VillagerStatus.VillagerState.MoveBackToTownCenter)
            {
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 6 && currentHour < 19)
                    {
                        currentState = VillagerStatus.VillagerState.Idle;
                    }
                }
            }

            // Handle current state
            switch (currentState)
            {
                case VillagerStatus.VillagerState.Idle:
                    HandleIdleState();
                    break;
                case VillagerStatus.VillagerState.MoveBackToTownCenter:
                    HandleMoveToTownCenterState(false);
                    break;
                case VillagerStatus.VillagerState.RunToGetTool:
                    HandleRunToGetToolState();
                    break;
                case VillagerStatus.VillagerState.Fear:
                    HandleFearState();
                    break;
                case VillagerStatus.VillagerState.DropCoinToPlayer:
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
                // Get random position in kingdom safe area
                float randomX = Utility.GetRandomKingdomSafePos();
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
                currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
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
                currentState = VillagerStatus.VillagerState.Idle;
                return;
            }

            // Kiểm tra xem tool có bị picked trước không
            if (currentTool.picked)
            {
                Debug.Log($"[VillagerBehavior] {gameObject.name}: Tool đã bị nhặt trước, hủy nhiệm vụ");
                currentTool = null;
                currentState = VillagerStatus.VillagerState.Idle;
                return;
            }

            // Nếu đang trong trạng thái di chuyển
            // Di chuyển đến vị trí của tool với animation chạy
            if (isMoving)
            {
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
                Debug.Log($"[VillagerBehavior] {gameObject.name} rời khỏi DropCoinToPlayer vì chỉ còn {status.CurrentCoins} coin");
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 19 || currentHour < 6)
                    {
                        currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                    }
                    else
                    {
                        currentState = VillagerStatus.VillagerState.Idle;
                    }
                }
                else
                {
                    currentState = VillagerStatus.VillagerState.Idle;
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
                    currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                }
                else
                {
                    currentState = VillagerStatus.VillagerState.Idle;
                }
            }
            else
            {
                currentState = VillagerStatus.VillagerState.Idle;
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
                
                if (isScared && currentState != VillagerStatus.VillagerState.RunToGetTool)
                {
                    Debug.Log($"[VillagerBehavior] {gameObject.name} bị mất coin, chuyển sang FEAR (OnCoinDamage)");
                    isInDanger = true;
                    fearTimer = FEAR_DURATION;
                    currentState = VillagerStatus.VillagerState.Fear;
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
            Debug.Log($"[VillagerBehavior] {gameObject.name} chuẩn bị drop {amount} coin(s). CurrentCoins trước drop: {status.CurrentCoins}, isAttacked: {isAttacked}");
            
            status.CurrentCoins -= amount;
            Debug.Log($"[VillagerBehavior] {gameObject.name} đã drop {amount} coin(s). CurrentCoins sau drop: {status.CurrentCoins}");
            
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

            var controller = GetComponent<DenizenController>();
            if (controller != null)
            {
                controller.ChangeRole(NPCRole.Vagrant);
                return;
            }
            Debug.LogError("[VillagerBehavior] DenizenController missing; expected all NPCs to be Denizen. Skipping legacy fallback.");
        }
        #endregion

        #region Save System
        public string GetPrefabID()
        {
            return "villager";
        }
        
        public object CaptureData()
        {
            return status.CaptureData(transform);
        }

        public void RestoreData(object data)
        {
            var villageData = (VillagerStatus.VillageData)data;
            status.RestoreData(villageData, transform);
            
            if (animator == null)
            {
                InitComponent();
            }
        }
        #endregion

        #region Trigger and Pickup
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if villager is picking up assigned tool
            var item = other.GetComponent<ToolItem>();
            if (item != null && currentTool == item && currentState == VillagerStatus.VillagerState.RunToGetTool)
            {
                // Atomic ownership claim to prevent race condition
                if (!item.picked && item.pickedBy == null)
                {
                    // Claim ownership immediately
                    item.pickedBy = gameObject;
                    item.picked = true;

                    // Change role based on tool type
                    ChangeRoleByTool(item.type);

                    // Destroy tool
                    Destroy(other.gameObject);

                    // Reset state
                    currentTool = null;
                    currentState = VillagerStatus.VillagerState.Idle;
                }
                else
                {
                    Debug.Log($"[VillagerBehavior] {gameObject.name} không thể nhặt tool (đã bị nhặt bởi {item.pickedBy?.name ?? "unknown"})");
                    currentTool = null;
                    currentState = VillagerStatus.VillagerState.Idle;
                }
            }
            else if (other.CompareTag("Coin"))
            {
                // Kiểm tra thuộc tính denizensPickupAble nếu có
                var coinScript = other.GetComponent<Coin>();
                if (coinScript == null || coinScript.denizensPickupAble)
                {
                    PickUpCoin(other.gameObject);
                }
            }
        }
        
        private void PickUpCoin(GameObject coinObject)
        {
            Script.Utility.ProcessCoinPickup(coinObject,
                condition: () => status.CurrentCoins < status.MaxCoins,
                onPickupSuccess: () =>
                {
                    status.CurrentCoins++;
                }
            );
        }

        /// <summary>
        /// Change villager role based on picked tool type
        /// </summary>
        private void ChangeRoleByTool(ToolType type)
        {
            var ctrl = GetComponent<DenizenController>();
            if (ctrl == null)
            {
                Debug.LogError($"[VillagerBehavior] {gameObject.name} missing DenizenController!");
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

        private IEnumerator CheckRoutine()
        {
            while (true)
            {
                foreach (var hitObject in rayCastHelper.DrawRayAndReturnListHitObjects())
                {
                    if (hitObject != null)
                    {
                        // Kiểm tra Enemy
                        if (hitObject.CompareTag("Enemy"))
                        {
                            isInDanger = true;
                            fearTimer = FEAR_DURATION;
                            currentState = VillagerStatus.VillagerState.Fear;
                            break;
                        }
                    }
                }

                if (fearTimer > 0)
                {
                    fearTimer -= Time.deltaTime;
                    if (fearTimer <= 0)
                    {
                        isInDanger = false;
                        if (GameTimeManager.Instance != null)
                        {
                            int currentHour = GameTimeManager.Instance.GameHour;
                            if (currentHour >= 19 || currentHour < 6)
                            {
                                currentState = VillagerStatus.VillagerState.MoveBackToTownCenter;
                            }
                            else
                            {
                                currentState = VillagerStatus.VillagerState.Idle;
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        #region IDamageable Implementation
        public bool HasCoinShield => status.CurrentCoins > 0;
        public bool HasHp => false;
        public int CurrentCoin { get => status.CurrentCoins; set => status.CurrentCoins = value; }
        public int MaxHp { get => 0; set { } } // Không có HP
        public void TakeDamage(int amount)
        {
            throw new NotImplementedException();
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public void Heal(int amount)
        {
            throw new NotImplementedException();
        }

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && CurrentCoin > 0)
            {
                InternalDropCoins(1, true);
                if (CurrentCoin <= 0)
                {
                    StartCoroutine(ConvertToVagrantAfterCoins());
                    Destroy(gameObject);
                    return false;
                    
                }
                return true;
            }
            // Không có HP nên luôn return true nếu không còn coin
            return true;
        }
        public void Die(GameObject whoDoDame)
        {
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            Destroy(gameObject);
        }
        #endregion
    }
}


