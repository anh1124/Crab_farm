using System;
using System.Collections;
using Script.Controller.PlayerController;
using Script.Data_Model.Interfaces;
using Script.Manager;
using Script.Save_Controller;
using UnityEngine;

namespace Script.Game.CoinCloud
{
    /// <summary>
    /// Điều khiển đám mây tiền.
    /// Có thể kích hoạt qua Start hoặc gọi public method.
    /// </summary>
    [RequireComponent(typeof(CoinCloudMovement))]
    [RequireComponent(typeof(CoinCloudDropper))]
    public class CoinCloudController : MonoBehaviour, ISaveable
    {
        public enum CloudState { Inactive, Delaying, Moving, Dropping, Finished }
        public enum TargetType { Transform, Vector }

        [Header("Spawn Settings")]
        [Tooltip("Nếu true, giữ nguyên vị trí hiện tại khi khởi tạo. Nếu false, sẽ đặt vị trí theo biến 'spawn'.")]
        public bool useCurrentPositionAsSpawn = false;
        
        [Tooltip("Vị trí spawn mục tiêu (chỉ dùng nếu useCurrentPositionAsSpawn = false).")]
        public Vector3 spawn = new Vector3(0, 40, 0);
        
        [Tooltip("Độ lệch ngẫu nhiên theo trục Y khi khởi tạo vị trí spawn.")]
        public float randomSpawnRadiusY = 5f;

        [Header("Target Settings")]
        [Tooltip("Chọn cách xác định điểm đến: theo một Transform (GameObject) hoặc theo một Vector3 cố định.")]
        public TargetType targetType = TargetType.Transform;
        
        [Tooltip("Đối tượng mục tiêu để đám mây bay đến (dùng nếu targetType = Transform).")]
        public GameObject targetObject;
        
        [Tooltip("Vị trí tọa độ mục tiêu (dùng nếu targetType = Vector).")]
        public Vector3 targetVector;

        [Tooltip("Vector cộng thêm vào vị trí đích cuối cùng.")]
        public Vector3 targetOffset;
        
        [Tooltip("Bán kính ngẫu nhiên theo trục X quanh điểm đích.")]
        public float randomTargetRadiusX = 10f;

        [Header("Lifecycle Settings")]
        [Tooltip("Thời gian chờ (giây) trước khi bắt đầu di chuyển.")]
        public float delay = 3f;
        
        [Tooltip("Nếu true, tự động kích hoạt vòng đời (đếm ngược delay -> bay) ngay khi Start.")]
        public bool activeOnStart = true;
        
        [Tooltip("Thời gian chờ sau khi nhả hết tiền trước khi biến mất hoàn toàn.")]
        public float destroyDelay = 1f;

        // State
        [Header("Runtime State (Read Only)")]
        [SerializeField] private CloudState currentState = CloudState.Inactive;
        private float remainingDelay = 0f;
        private float currentDestroyTimer = 0f;

        // Components
        private CoinCloudMovement movement;
        private CoinCloudDropper dropper;
        private Coroutine lifecycleCoroutine;

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
            
            // Nếu là mới hoàn toàn (Inactive), khởi tạo vị trí
            if (currentState == CloudState.Inactive)
            {
                InitializeSpawnPosition();
                
                if (activeOnStart)
                {
                    ActivateCloud();
                }
            }
            else
            {
                // Nếu load từ save, tiếp tục trạng thái cũ
                ResumeBehavior();
            }
        }

        /// <summary>
        /// Tìm TownCenter và kích hoạt đám mây (thường dùng khi bắt đầu world mới).
        /// </summary>
        public void ActivateWithTownCenterSearch()
        {
            if (lifecycleCoroutine != null) StopCoroutine(lifecycleCoroutine);
            lifecycleCoroutine = StartCoroutine(SearchTownCenterAndActivate());
        }

        private IEnumerator SearchTownCenterAndActivate()
        {
            currentState = CloudState.Delaying; // Đánh dấu là đang bắt đầu quy trình
            
            // Chờ một vài frame để đảm bảo BuildingManager và TownCenter đã được khởi tạo
            float timeout = 5f;
            while (timeout > 0)
            {
                var buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>();
                if (buildingManager != null && buildingManager.currentTownCenter != null)
                {
                    targetType = TargetType.Transform;
                    targetObject = buildingManager.currentTownCenter.gameObject;
                    Debug.Log("[CoinCloudController] Đã tìm thấy TownCenter, bắt đầu bay.");
                    break;
                }
                
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Nếu không tìm thấy TownCenter sau timeout, thử tìm Player làm fallback
            if (targetObject == null)
            {
                var pc = Controller.PlayerController.PlayerController.Instance;
                if (pc?.player != null)
                {
                    targetType = TargetType.Transform;
                    targetObject = pc.player;
                    Debug.Log("[CoinCloudController] Không tìm thấy TownCenter, fallback về vị trí Player.");
                }
            }

            // Bắt đầu vòng đời chính (không cần gọi ActivateCloud() vì nó sẽ tạo thêm coroutine chồng lấn)
            yield return StartCoroutine(CloudLifecycleCoroutine());
        }

        public void ActivateCloud()
        {
            if (lifecycleCoroutine != null) StopCoroutine(lifecycleCoroutine);
            lifecycleCoroutine = StartCoroutine(CloudLifecycleCoroutine());
        }

        private IEnumerator CloudLifecycleCoroutine()
        {
            // 1. Delay
            currentState = CloudState.Delaying;
            remainingDelay = delay;
            while (remainingDelay > 0)
            {
                remainingDelay -= Time.deltaTime;
                yield return null;
            }
            remainingDelay = 0;

            // 2. Moving
            currentState = CloudState.Moving;
            Vector3 finalTarget = CalculateTargetPosition();
            movement.StartMoving(finalTarget);

            while (!movement.HasReachedTarget)
            {
                yield return null;
            }

            // 3. Dropping
            currentState = CloudState.Dropping;
            dropper.StartDropping();

            while (!dropper.IsFinished)
            {
                yield return null;
            }

            // 4. Finished
            currentState = CloudState.Finished;
            currentDestroyTimer = destroyDelay;
            while (currentDestroyTimer > 0)
            {
                currentDestroyTimer -= Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void ResumeBehavior()
        {
            // Nếu đã ở trạng thái dở dang, khởi chạy lại lifecycle từ state đó
            if (lifecycleCoroutine != null) StopCoroutine(lifecycleCoroutine);
            lifecycleCoroutine = StartCoroutine(ResumeLifecycleCoroutine());
        }

        private IEnumerator ResumeLifecycleCoroutine()
        {
            switch (currentState)
            {
                case CloudState.Delaying:
                    while (remainingDelay > 0)
                    {
                        remainingDelay -= Time.deltaTime;
                        yield return null;
                    }
                    goto case CloudState.Moving;

                case CloudState.Moving:
                    currentState = CloudState.Moving;
                    Vector3 finalTarget = CalculateTargetPosition();
                    movement.StartMoving(finalTarget);
                    while (!movement.HasReachedTarget)
                    {
                        yield return null;
                    }
                    goto case CloudState.Dropping;

                case CloudState.Dropping:
                    currentState = CloudState.Dropping;
                    dropper.StartDropping();
                    while (!dropper.IsFinished)
                    {
                        yield return null;
                    }
                    goto case CloudState.Finished;

                case CloudState.Finished:
                    currentState = CloudState.Finished;
                    while (currentDestroyTimer > 0)
                    {
                        currentDestroyTimer -= Time.deltaTime;
                        yield return null;
                    }
                    Destroy(gameObject);
                    break;
            }
        }

        private void InitializeSpawnPosition()
        {
            if (useCurrentPositionAsSpawn) return;

            Vector3 pos = spawn;
            pos.y += UnityEngine.Random.Range(-randomSpawnRadiusY, randomSpawnRadiusY);
            transform.position = pos;
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 basePos;
            if (targetType == TargetType.Transform && targetObject != null)
            {
                basePos = targetObject.transform.position;
            }
            else
            {
                basePos = targetVector;
            }

            basePos.x += UnityEngine.Random.Range(-randomTargetRadiusX, randomTargetRadiusX);
            return basePos + targetOffset;
        }

        private void MoveToOtherContainer()
        {
            if (EntitySaveManager.Instance == null) return;
            try { EntitySaveManager.Instance.MoveObjectToContainer(gameObject); }
            catch { }
        }

        #region ISaveable Implementation

        [Serializable]
        private class CoinCloudData
        {
            public float px, py, pz;
            public int coinsRemaining;
            public int coinsDropped;
            public float moveSpeed;
            public float dropInterval;
            public int coinAmount;
            public int state;
            public float remainingDelay;
            public float destroyTimer;
            public bool hasReachedTarget;
        }

        public string GetPrefabID() => PREFAB_ID;

        public object CaptureData()
        {
            return new CoinCloudData
            {
                px = transform.position.x,
                py = transform.position.y,
                pz = transform.position.z,
                coinsRemaining = dropper.CoinsRemaining,
                coinsDropped = dropper.CoinsDropped,
                moveSpeed = movement.moveSpeed,
                dropInterval = dropper.DropInterval,
                coinAmount = dropper.CoinAmount,
                state = (int)currentState,
                remainingDelay = remainingDelay,
                destroyTimer = currentDestroyTimer,
                hasReachedTarget = movement.HasReachedTarget
            };
        }

        public void RestoreData(object data)
        {
            if (data is CoinCloudData d)
            {
                transform.position = new Vector3(d.px, d.py, d.pz);
                currentState = (CloudState)d.state;
                remainingDelay = d.remainingDelay;
                currentDestroyTimer = d.destroyTimer;

                movement.moveSpeed = d.moveSpeed;
                if (d.hasReachedTarget) movement.ForceReachTarget();

                dropper.SetCoinsRemaining(d.coinsRemaining);
                dropper.SetCoinsDropped(d.coinsDropped);
                dropper.dropInterval = d.dropInterval;
                dropper.coinAmount = d.coinAmount;
            }
        }

        #endregion
    }
}
