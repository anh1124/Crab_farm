using System;
using Script.Controller.PlayerController;
using Script.Data_Model.Interfaces;
using Script.Manager;
using Script.Save_Controller;
using UnityEngine;

namespace Script.Game.CoinCloud
{
    /// <summary>
    /// Điều khiển đám mây tiền - sử dụng Update với deltaTime thay vì Coroutine
    /// Implements ISaveable để lưu/load trạng thái
    /// </summary>
    [RequireComponent(typeof(CoinCloudMovement))]
    [RequireComponent(typeof(CoinCloudDropper))]
    public class CoinCloudController : MonoBehaviour, ISaveable
    {
        public enum CloudState { Delaying, Moving, Dropping, Finished }

        [Header("Default Spawn")]
        [Tooltip("Vị trí khởi tạo mặc định khi được Instantiate (được các script khác sử dụng để tham chiếu)")]
        public Vector3 defaultSpawnPosition = new Vector3(200f, 200f, 0f);

        [Header("Spawn Config")]
        [Tooltip("Phạm vi spawn X (random -spawnXRange đến +spawnXRange)")]
        public float spawnXRange = 50f;

        [Tooltip("Vị trí Y khi spawn")]
        public float spawnY = 40f;

        [Header("Target Config")]
        [Tooltip("Vị trí Y khi bay đến target")]
        public float targetY = 25f;

        [Tooltip("Phạm vi X offset từ TownCenter (random -targetXRange đến +targetXRange)")]
        public float targetXRange = 20f;

        [Header("Timing")]
        [Tooltip("Thời gian delay ban đầu trước khi bắt đầu bay (giây)")]
        public float initialDelay = 10f;

        [Header("Cleanup")]
        [Tooltip("Thời gian chờ trước khi hủy object sau khi hoàn thành thả tiền")]
        [SerializeField] private float destroyDelay = 1f;

        // Component references
        private CoinCloudMovement movement;
        private CoinCloudDropper dropper;

        // State
        private Vector3 townCenterPosition;
        private CloudState currentState = CloudState.Delaying;
        private float remainingDelay = 0f;          // Thời gian delay còn lại (để save)
        private float destroyTimer = 0f;            // Timer chờ destroy sau khi drop xong

        // ID cho hệ thống save (constant)
        private const string PREFAB_ID = "coin_cloud";
        
        private void Awake()
        {
            movement = GetComponent<CoinCloudMovement>();
            dropper = GetComponent<CoinCloudDropper>();
        }
        
        private void Start()
        {
            MoveToOtherContainer();
            FindTownCenter();
            
            // Nếu là spawn mới (chưa được restore từ save), khởi tạo
            if (currentState == CloudState.Delaying && remainingDelay == 0f)
            {
                Debug.Log("[CoinCloudController] ===> SPAWN MỚI <=== Đám mây mới được tạo (chưa có trong save)");
                InitializeSpawnPosition();
                remainingDelay = initialDelay;
            }
            else
            {
                Debug.Log("[CoinCloudController] ===> CONTINUE TỪ SAVE <=== Đám mây đang tiếp tục lifecycle từ save");
            }
            
            // Tiếp tục trạng thái từ save hoặc bắt đầu mới
            switch (currentState)
            {
                case CloudState.Delaying:
                    // Đang delay, timer sẽ chạy trong Update
                    break;

                case CloudState.Moving:
                    // Nếu đã đến đích rồi thì chuyển sang Dropping ngay
                    if (movement.HasReachedTarget)
                    {
                        currentState = CloudState.Dropping;
                        dropper.StartDropping();
                    }
                    else
                    {
                        // Chưa đến đích -> tính lại target và tiếp tục di chuyển
                        Vector3 targetPos = movement.CalculateTargetPosition(townCenterPosition, targetY, targetXRange);
                        movement.StartMoving(targetPos);
                    }
                    break;

                case CloudState.Dropping:
                    // Nếu dropper đã kết thúc thì chuyển sang Finished
                    if (dropper.IsFinished)
                    {
                        currentState = CloudState.Finished;
                        destroyTimer = destroyDelay;
                    }
                    else
                    {
                        // Tiếp tục drop
                        dropper.StartDropping();
                    }
                    break;

                case CloudState.Finished:
                    destroyTimer = Mathf.Max(destroyTimer, 0.1f); // đảm bảo có thời gian chờ
                    break;
            }
        }
        
        private void Update()
        {
            switch (currentState)
            {
                case CloudState.Delaying:
                    UpdateDelaying();
                    break;
                case CloudState.Moving:
                    UpdateMoving();
                    break;
                case CloudState.Dropping:
                    UpdateDropping();
                    break;
                case CloudState.Finished:
                    UpdateFinished();
                    break;
            }
        }
        
        private void UpdateDelaying()
        {
            remainingDelay -= Time.deltaTime;
            
            if (remainingDelay <= 0f)
            {
                remainingDelay = 0f;
                currentState = CloudState.Moving;
                
                // Bắt đầu bay
                Vector3 targetPos = movement.CalculateTargetPosition(townCenterPosition, targetY, targetXRange);
                movement.StartMoving(targetPos);
            }
        }
        
        private void UpdateMoving()
        {
            if (movement.HasReachedTarget)
            {
                currentState = CloudState.Dropping;
                dropper.StartDropping();
            }
        }
        
        private void UpdateDropping()
        {
            if (dropper.IsFinished)
            {
                currentState = CloudState.Finished;
                destroyTimer = destroyDelay;
            }
        }
        
        private void UpdateFinished()
        {
            destroyTimer -= Time.deltaTime;
            
            if (destroyTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
        
        private void FindTownCenter()
        {
            var buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>();
            if (buildingManager != null && buildingManager.currentTownCenter != null)
            {
                townCenterPosition = buildingManager.currentTownCenter.transform.position;
                return;
            }

            var playerTransform = PlayerController.Instance?.player?.transform;
            if (playerTransform != null)
            {
                townCenterPosition = playerTransform.position;
                return;
            }

            // Fallback: sử dụng vị trí hiện tại của đám mây và log cảnh báo
            townCenterPosition = transform.position;
            Debug.LogWarning("[CoinCloudController] Không tìm thấy TownCenter hoặc Player, sử dụng vị trí hiện tại làm tâm.");
        }
        
        private void MoveToOtherContainer()
        {
            if (EntitySaveManager.Instance == null) return;

            try
            {
                EntitySaveManager.Instance.MoveObjectToContainer(gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CoinCloudController] Failed to move to otherContainer: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Khởi tạo vị trí spawn ngẫu nhiên theo config
        /// </summary>
        private void InitializeSpawnPosition()
        {
            float randomX = UnityEngine.Random.Range(-spawnXRange, spawnXRange);
            transform.position = new Vector3(randomX, spawnY, 0f);
        }
        
        #region ISaveable Implementation

        [Serializable]
        private class CoinCloudData
        {
            public float positionX;
            public float positionY;
            public float positionZ;
            public int coinsRemaining;
            public int coinsDropped;
            public float moveSpeed;
            public float dropInterval;
            public int coinAmount;
            public int currentState;          // CloudState as int
            public float remainingDelay;       // Thời gian delay còn lại
            public float destroyTimer;         // Timer chờ destroy
            public bool hasReachedTarget;      // Trạng thái đã tới đích của movement
        }

        public string GetPrefabID()
        {
            return PREFAB_ID;
        }
        
        public object CaptureData()
        {
            return new CoinCloudData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                positionZ = transform.position.z,
                coinsRemaining = dropper.CoinsRemaining,
                coinsDropped = dropper.CoinsDropped,
                moveSpeed = movement.moveSpeed,
                dropInterval = dropper.DropInterval,
                coinAmount = dropper.CoinAmount,
                currentState = (int)currentState,
                remainingDelay = remainingDelay,
                destroyTimer = destroyTimer,
                hasReachedTarget = movement.HasReachedTarget
            };
        }
        
        public void RestoreData(object data)
        {
            if (data is CoinCloudData cloudData)
            {
                Debug.Log("[CoinCloudController] ===> RESTORE TỪ SAVE <=== Đám mây đang được load từ entity save");
                
                // Restore position
                transform.position = new Vector3(cloudData.positionX, cloudData.positionY, cloudData.positionZ);

                // Restore state
                currentState = (CloudState)cloudData.currentState;
                remainingDelay = cloudData.remainingDelay;
                destroyTimer = cloudData.destroyTimer;

                // Restore movement state
                movement.moveSpeed = cloudData.moveSpeed;
                // Đánh dấu trạng thái đã tới đích nếu cần
                if (cloudData.hasReachedTarget)
                {
                    movement.ForceReachTarget();
                }

                // Restore dropper state
                dropper.SetCoinsRemaining(cloudData.coinsRemaining);
                dropper.SetCoinsDropped(cloudData.coinsDropped);
                dropper.dropInterval = cloudData.dropInterval;
                dropper.coinAmount = cloudData.coinAmount;

                Debug.Log($"[CoinCloudController] Restored từ save - State: {currentState}, Delay: {remainingDelay:F2}s, Coins: {cloudData.coinsRemaining}, HasReachedTarget: {cloudData.hasReachedTarget}");
            }
        }

        #endregion
    }
}
