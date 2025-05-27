using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Script.Game.Other;
using Script.Data_Model;
using Script.Game.Mobs.MobsManager;

namespace Script.Game.Mobs.Subjects
{
    public class VillagerController : MonoBehaviour, ISaveable, ICoinTakeDamage
    {
        private VillageStatus status;

        [Header("State")]
        [Tooltip("Trạng thái hiện tại của villager")]
        [SerializeField] private VillageStatus.VillageState currentState;
        
        [Header("Movement")]
        [Tooltip("Vị trí đích đến")]
        public Vector3 targetPosition;
        [Tooltip("Đang di chuyển hay không")]
        public bool isMoving;
        
        [Header("Fear Status")]
        [Tooltip("Villager đang trong tình trạng nguy hiểm")]
        public bool isInDanger;
        [Tooltip("Thời gian sợ hãi còn lại")]
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

        // Coin Drop Settings
        private const int MIN_COINS_DROP = 1;
        private const int MAX_COINS_DROP = 2;

        // Property to get current number of coins the villager has
        public int CurrentCoins => status.CurrentCoins;

        // Property to get maximum number of coins the villager can carry
        public int MaxCoins => status.MaxCoins;

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

        private void UpdateStateHandler()
        {
            // Fear state has highest priority when enemy is detected
            if (isInDanger)
            {
                if (currentState != VillageStatus.VillageState.Fear)
                {
                    currentState = VillageStatus.VillageState.Fear;
                    return;
                }
            }

            // Tool collection has second priority
            if (currentTool != null && !isInDanger && currentState != VillageStatus.VillageState.RunToGetTool)
            {
                currentState = VillageStatus.VillageState.RunToGetTool;
                return;
            }

            // Night time state check (19:00 - 6:00)
            if (GameTimeManager.Instance != null)
            {
                int currentHour = GameTimeManager.Instance.GameHour;
                if ((currentHour >= 19 || currentHour < 6) && 
                    !isInDanger && 
                    currentTool == null && 
                    currentState != VillageStatus.VillageState.MoveBackToTownCenter)
                {
                    currentState = VillageStatus.VillageState.MoveBackToTownCenter;
                    return;
                }
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
            }
        }

        private IEnumerator CheckRoutine()
        {
            while (true)
            {
                foreach (var hitObject in rayCastHelper.DrawRayAndReturnListHitObjects())
                {
                    if (hitObject != null && hitObject.CompareTag("Enemy"))
                    {
                        isInDanger = true;
                        fearTimer = FEAR_DURATION;
                        // Chuyển sang trạng thái Fear ngay lập tức
                        currentState = VillageStatus.VillageState.Fear;
                        break;
                    }
                }

                // Update fear timer
                if (fearTimer > 0)
                {
                    fearTimer -= Time.deltaTime;
                    if (fearTimer <= 0)
                    {
                        isInDanger = false;
                        // Sau khi hết sợ, kiểm tra thời gian để quyết định trạng thái
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

            // Nếu đang trong trạng thái di chuyển
            // Di chuyển đến vị trí của tool với animation chạy
            if (isMoving)
            {
                MoveTowardsTarget(currentTool.transform.position, "run");
            }
        }

        private void HandleFearState()
        {
            // Kiểm tra 2 điều kiện:
            // 1. Villager chưa di chuyển (isMoving = false)
            // 2. Hoặc villager đã đến gần điểm đích (khoảng cách < 0.1f)
            if (!isMoving || Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                // Lấy một vị trí ngẫu nhiên trong khu vực plaza để chạy đến
                // Plaza là khu vực an toàn trong game
                float randomX = Utility.GetRandomPlazaPos();
                targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
                isMoving = true; // Đánh dấu bắt đầu di chuyển
            }

            // Nếu đang trong trạng thái di chuyển
            // Di chuyển đến vị trí đã chọn với animation chạy
            if (isMoving)
            {
                MoveTowardsTarget(targetPosition, "run");
            }
        }

        #endregion

        #region Movement

        private void MoveTowardsTarget(Vector3 target, string animationName)
        {
            float speed = animationName == "run" ? status.RunSpeed : status.WalkSpeed;
            
            // Sử dụng Utility.MoveTowardsTarget để di chuyển
            bool hasReachedTarget = Utility.MoveTowardsTarget(gameObject, target, speed);
            
            // Cập nhật animation
            animator.Play(animationName);

            // Kiểm tra nếu đã đến đích
            if (hasReachedTarget)
            {
                isMoving = false;
                animator.Play("idle");
            }
        }

        #endregion

        #region ICoinTakeDamage Implementation

        public void OnCoinDamage()
        {
            if (status.CurrentCoins > 0)
            {
                // Random số coin sẽ rơi (1-2 coins)
                int coinsToLose = UnityEngine.Random.Range(MIN_COINS_DROP, MAX_COINS_DROP);
                DropCoins(coinsToLose);

                // Enter fear state if not getting tool
                if (currentState != VillageStatus.VillageState.RunToGetTool)
                {
                    isInDanger = true;
                    fearTimer = FEAR_DURATION;
                    currentState = VillageStatus.VillageState.Fear;
                }
            }
        }

        public void DropCoins(int amount)
        {
            // Đảm bảo số coins rơi không vượt quá số coins hiện có
            // Ví dụ: nếu random ra 2 coins mà chỉ còn 1 coin, sẽ chỉ rơi 1 coin
            amount = Mathf.Min(amount, status.CurrentCoins);
            
            if (amount > 0)
            {
                status.CurrentCoins -= amount;
                
                // Drop coins using Spawner
                if (Spawner.Instance != null)
                {
                    Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                    Spawner.Instance.DropByDenizen(transform.position, amount, dropDirection);
                }

                // Nếu hết coin thì biến thành vagrant
                if (status.CurrentCoins <= 0)
                {
                    TransformToVagrant();
                }
            }
        }

        private void TransformToVagrant()
        {
            if (PrefabData.Instance != null && PrefabData.Instance.vagrant != null)
            {
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;
                Vector3 scale = transform.localScale;
                
                // Use Spawner to create vagrant
                if (Spawner.Instance != null)
                {
                    GameObject vagrant = Spawner.Instance.SpawnMob(position, PrefabData.Instance.vagrant);
                    if (vagrant != null)
                    {
                        // Maintain the same orientation and scale
                        vagrant.transform.rotation = rotation;
                        vagrant.transform.localScale = scale;
                    }
                }
                else
                {
                    Debug.LogError("[VillagerController] Spawner instance not found!");
                }
                
                // Destroy the villager
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("[VillagerController] Vagrant prefab not found in PrefabData!");
            }
        }

        #endregion
        
        #region Save System Implementation

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
            if (other.CompareTag("Coin"))
            {
                PickUpCoin(other.gameObject);
            }
        }

        private void PickUpCoin(GameObject coinObject)
        {
            // Kiểm tra xem villager có thể nhặt thêm xu không
            if (status.CurrentCoins < status.MaxCoins)
            {
                // Tăng số xu của villager
                status.CurrentCoins++;
                
                // Hủy đối tượng xu
                Destroy(coinObject);
            }
        }

        #endregion

        // Thêm hàm StartCheckRoutine
        private void StartCheckRoutine()
        {
            StopCheckRoutine(); // Dừng routine cũ nếu có
            checkRoutine = StartCoroutine(CheckRoutine());
        }

        // Thêm hàm StopCheckRoutine
        private void StopCheckRoutine()
        {
            if (checkRoutine != null)
            {
                StopCoroutine(checkRoutine);
                checkRoutine = null;
            }
        }
    }
} 