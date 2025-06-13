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
using Script.Game.Item.Coin;
using Script.Game.Mobs.MobsManager;
using Script.Game.Item;
namespace Script.Game.Mobs.Subjects
{
    public class VillagerController : MonoBehaviour, ISaveable,IDamageable
    {
        #region Variables and Properties
        private VillageStatus status;

        [Header("State")]
        [Tooltip("Trạng thái hiện tại của villager")]
        [SerializeField] private VillageStatus.VillageState currentState;
        
        public Vector3 targetPosition;
   
        public bool isMoving;
        public bool isInDanger;
        public float fearTimer;
        public const float FEAR_DURATION = 3f; // Giảm xuống 3 giây

        [Header("Tool")]
        [Tooltip("Công cụ được giao để đi lấy")]
        public GameObject currentTool;
        
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
            status = GetComponent<VillageStatus>();
            if (status == null)
            {
                Debug.LogWarning($"{gameObject.name} have no script VillageStatus, has auto add");
                status = gameObject.AddComponent<VillageStatus>();
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

        private void Start()
        {
            InitComponent();
            InitializeVillager();
            
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.RegisterVillager(gameObject);
            }
            else
            {
                Debug.LogError("[VillagerController] VillagerManager instance not found!");
            }
        }

        private void Update()
        {
            // Update fear timer
            if (fearTimer > 0)
            {
                fearTimer -= Time.deltaTime;
                if (fearTimer <= 0 && currentState == VillageStatus.VillageState.Fear)
                {
                    isInDanger = false;
                    // Kiểm tra thời gian để quyết định trạng thái tiếp theo
                    if (GameTimeManager.Instance != null)
                    {
                        int currentHour = GameTimeManager.Instance.GameHour;
                        if (currentHour >= 19 || currentHour < 6)
                        {
                            currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                        }
                        else
                        {
                            currentState = VillageStatus.VillageState.Idle;
                        }
                    }
                    else
                    {
                        currentState = VillageStatus.VillageState.Idle;
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
                    currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                }
                else
                {
                    currentState = VillageStatus.VillageState.Idle;
                }
            }
            else
            {
                currentState = VillageStatus.VillageState.Idle;
            }
            
            isMoving = false;
            fearTimer = 0f;
            StartCheckRoutine();
        }
        #endregion

        #region Time Management
        private void HandleNightTime()
        {
            if (currentState != VillageStatus.VillageState.RunToGetTool)
            {
                currentState = VillageStatus.VillageState.MoveBackToTownCenter;
            }
        }

        private void HandleDayTime()
        {
            if (currentState == VillageStatus.VillageState.MoveBackToTownCenter)
            {
                currentState = VillageStatus.VillageState.Idle;
            }
        }
        #endregion

        #region State Management
        private void UpdateStateHandler()
        {
            // Fear state has highest priority when enemy is detected
            if (isInDanger)
            {
                if (currentState != VillageStatus.VillageState.Fear)
                {
                    Debug.Log($"[VillagerController] {gameObject.name} chuyển sang FEAR do isInDanger = true (UpdateStateHandler)");
                    currentState = VillageStatus.VillageState.Fear;
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
                    currentState != VillageStatus.VillageState.MoveBackToTownCenter &&
                    currentState != VillageStatus.VillageState.DropCoinToPlayer) // Don't interrupt coin dropping
                {
                    currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                    return;
                }
            }

            // Check for player and handle coin drop state (third priority)
            if (currentState != VillageStatus.VillageState.DropCoinToPlayer) // Only check if not already dropping coins
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
                                Debug.Log($"[VillagerController] {gameObject.name} chuyển sang DropCoinToPlayer với {status.CurrentCoins} coins");
                                currentState = VillageStatus.VillageState.DropCoinToPlayer;
                                return;
                            }
                        }
                    }
                }
            }

            // Tool collection has fourth priority
            if (currentTool != null && !isInDanger && currentState != VillageStatus.VillageState.RunToGetTool)
            {
                currentState = VillageStatus.VillageState.RunToGetTool;
                return;
            }

            // Default to Idle during day if no other conditions are met
            if (!isInDanger && currentTool == null && currentState == VillageStatus.VillageState.MoveBackToTownCenter)
            {
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 6 && currentHour < 19)
                    {
                        currentState = VillageStatus.VillageState.Idle;
                    }
                }
            }

            // Handle current state
            switch (currentState)
            {
                case VillageStatus.VillageState.Idle:
                    HandleIdleState();
                    break;
                case VillageStatus.VillageState.MoveBackToTownCenter:
                    HandleMoveToTownCenterState(false);
                    break;
                case VillageStatus.VillageState.RunToGetTool:
                    HandleRunToGetToolState();
                    break;
                case VillageStatus.VillageState.Fear:
                    HandleFearState();
                    break;
                case VillageStatus.VillageState.DropCoinToPlayer:
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
                currentState = VillageStatus.VillageState.MoveBackToTownCenter;
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
                currentState = VillageStatus.VillageState.Idle;
                return;
            }

            // Kiểm tra xem tool có bị enemy lấy không
            var toolStats = currentTool.GetComponent<ToolStats>();
            if (toolStats != null && toolStats.isPickedByEnemy)
            {
                Debug.Log($"[VillagerController] {gameObject.name}: Tool đã bị enemy lấy mất, hủy nhiệm vụ");
                currentTool = null;
                currentState = VillageStatus.VillageState.Idle;
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
                Debug.Log($"[VillagerController] {gameObject.name} rời khỏi DropCoinToPlayer vì chỉ còn {status.CurrentCoins} coin");
                if (GameTimeManager.Instance != null)
                {
                    int currentHour = GameTimeManager.Instance.GameHour;
                    if (currentHour >= 19 || currentHour < 6)
                    {
                        currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                    }
                    else
                    {
                        currentState = VillageStatus.VillageState.Idle;
                    }
                }
                else
                {
                    currentState = VillageStatus.VillageState.Idle;
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
                    currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                }
                else
                {
                    currentState = VillageStatus.VillageState.Idle;
                }
            }
            else
            {
                currentState = VillageStatus.VillageState.Idle;
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
                
                if (isScared && currentState != VillageStatus.VillageState.RunToGetTool)
                {
                    Debug.Log($"[VillagerController] {gameObject.name} bị mất coin, chuyển sang FEAR (OnCoinDamage)");
                    isInDanger = true;
                    fearTimer = FEAR_DURATION;
                    currentState = VillageStatus.VillageState.Fear;
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
            Debug.Log($"[VillagerController] {gameObject.name} chuẩn bị drop {amount} coin(s). CurrentCoins trước drop: {status.CurrentCoins}, isAttacked: {isAttacked}");
            
            status.CurrentCoins -= amount;
            Debug.Log($"[VillagerController] {gameObject.name} đã drop {amount} coin(s). CurrentCoins sau drop: {status.CurrentCoins}");
            
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
            
            if (PrefabData.Instance != null && PrefabData.Instance.vagrant != null)
            {
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;
                Vector3 scale = transform.localScale;
                
                if (Spawner.Instance != null)
                {
                    GameObject vagrant = Spawner.Instance.SpawnMob(position, PrefabData.Instance.vagrant);
                    if (vagrant != null)
                    {
                        vagrant.transform.rotation = rotation;
                        vagrant.transform.localScale = scale;
                    }
                }
                else
                {
                    Debug.LogError("[VillagerController] Spawner instance not found!");
                    return; // Don't destroy if spawn failed
                }
                
                // Stop all coroutines before destroying
                StopAllCoroutines();
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("[VillagerController] Vagrant prefab not found in PrefabData!");
            }
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
            var villageData = (VillageStatus.VillageData)data;
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
            if (currentState == VillageStatus.VillageState.RunToGetTool && 
                currentTool != null && 
                other.gameObject == currentTool)
            {
                var toolStats = currentTool.GetComponent<ToolStats>();
                if (toolStats != null && !toolStats.isPickedByEnemy)
                {
                    // Đánh dấu tool đã được villager nhặt
                    toolStats.SetPickedUp(gameObject);
                    // Xóa tool
                    Destroy(currentTool);
                }
                else
                {
                    Debug.Log($"[VillagerController] {gameObject.name} không thể nhặt tool (toolStats null hoặc tool đã bị enemy lấy)");
                }
                
                currentTool = null;
                currentState = VillageStatus.VillageState.Idle;
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
            if (status.CurrentCoins < status.MaxCoins)
            {
                status.CurrentCoins++;
                Destroy(coinObject);
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
                            currentState = VillageStatus.VillageState.Fear;
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
                                currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                            }
                            else
                            {
                                currentState = VillageStatus.VillageState.Idle;
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
        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && CurrentCoin > 0)
            {
                InternalDropCoins(1, true);
                if (CurrentCoin <= 0)
                {
                    StartCoroutine(ConvertToVagrantAfterCoins());
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