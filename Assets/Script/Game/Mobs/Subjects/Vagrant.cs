using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Game.Item.Coin;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Script.Game.Mobs.Subjects
{
    public class Vagrant : MonoBehaviour, ISaveable
    {
        // Enum Declaration
        public enum VagrantState
        {
            Fear,   // Sit down when seeing enemy
            Idle,   // stand
            GetCoin // Run quickly to coin position
        }
        
        
        // Movement Settings
        [Header("Movement Settings")]
        public float idleSpeed = 1.0f;       // Movement speed during idle
        public float runSpeed = 3.5f;        // Movement speed when seeing coin
        [SerializeField] private float maxIdleWalkDistance = 3.0f; // Khoảng cách tối đa có thể đi khi idle
        
        // Detection Settings
        [Header("Detection Settings")]
        [SerializeField] private float roadToTarget = 0f;//taget ở đây là coin
        [SerializeField] private Vector2 targetPosition;
        public GameObject coin;
        
        // State and Tracking
        [Header("State")]
        [SerializeField] private VagrantState currentState;
        
        // Private Fields
        private Animator animator;
        private Coroutine checkRoutine;
        private Coroutine flipRoutine;
        private Coroutine idleWalkRoutine;
        private RayCastHelper rayCastHelper;
        private Vector3 startPosition; // Vị trí ban đầu để kiểm soát khoảng di chuyển idle
        private Vector3 currentIdleTarget; // Vị trí mục tiêu hiện tại khi idle
        private bool isWalking = false; // Đang di chuyển trong trạng thái idle hay không

        private void Awake()
        {
            // Set tag to "Vagrant" in Awake instead of Start
            gameObject.tag = "Vagrant";
            Debug.Log("Vagrant tag set to: " + gameObject.tag);
            startPosition = transform.position;
        }
    
        private void Start()
        {
            InitComponent();
            InitializeVagrant();
        }
        private void InitComponent()
        {
            // Kiểm tra và thêm RayCastHelper nếu chưa có
            rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper == null)
            {
                rayCastHelper = gameObject.AddComponent<RayCastHelper>();
                Debug.LogWarning(gameObject.name + "  chưa có RayCastHelper, đã thêm 1 cái vào "+ gameObject.name);
            }
            
        }
        private void InitializeVagrant()
        {
            roadToTarget = 0f;
            currentState = VagrantState.Idle;//khởi tạo idle ban đầu
            animator = GetComponent<Animator>();
            startPosition = transform.position;
            StartCheckRoutine();//bắt đầu hàm kiểm tra mỗi 0.5f
        }
        private void OnEnable()
        {
            GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleTimeChange);
        }

        private void OnDisable()
        {
            GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, HandleTimeChange);
        }

        private void Update()
        {
            UpdateStateHandler();
        }
        private void UpdateStateHandler()
        {
            switch (currentState)
            {
                case VagrantState.Idle:
                    HandleIdleState();
                    break;
                case VagrantState.Fear:
                    HandleFearState();
                    break;
                case VagrantState.GetCoin:
                    HandleGetCoinState();
                    break;
            }
        }
        //hàm kiểm tra list gameobject đã nhìn thấy được và chuyển state theo điều kiện
        private void UpdateStateBasedOnDetection(List<GameObject> gameObjectsToCheck)
        {
            // Lặp qua từng GameObject trong danh sách
            foreach (var obj in gameObjectsToCheck)
            {
                // Kiểm tra nếu đối tượng có tag "Enemy"
                if (obj.CompareTag("Enemy"))
                {
                    currentState = VagrantState.Fear;
                    break; // Nếu phát hiện Enemy, thoát khỏi vòng lặp
                }
        
                // Kiểm tra nếu đối tượng có tag "Coin"
                if (obj.CompareTag("Coin"))
                {
                    if ((currentState == VagrantState.Idle || currentState == VagrantState.GetCoin) 
                        && currentState != VagrantState.Fear)
                    {
                        currentState = VagrantState.GetCoin;
                        coin = obj;
                    }
                    break; // Nếu phát hiện Coin, thoát khỏi vòng lặp
                }
            }
        }
        
        #region Hand State

        private void HandleIdleState()
        {
            // Start the idle walking routine if not already running
            if (idleWalkRoutine == null)
            {
                idleWalkRoutine = StartCoroutine(IdleWalkRoutine());
            }
            
            // If currently walking, move towards the target
            if (isWalking)
            {
                MoveTowardsIdleTarget();
                animator.Play("walk"); // Đảm bảo có animation "walk" hoặc thay bằng animation phù hợp
            }
            else
            {
                animator.Play("idle");
                
                // Start the random flip routine if not already running and not walking
                if (flipRoutine == null)
                {
                    flipRoutine = StartCoroutine(RandomFlipRoutine());
                }
            }
        }
        
        private void HandleFearState()
        {
            animator.Play("fear");
            roadToTarget = 0;
            StopCheckRoutine();
            // Stop all movement routines
            StopAllMovementRoutines();
        }
        
        private void HandleGetCoinState()
        {
            // First, check if the coin has been destroyed
            if (coin == null || coin.Equals(null))
            {
                currentState = VagrantState.Idle;
                return;
            }

            // Stop all movement routines
            StopAllMovementRoutines();
            
            roadToTarget = Vector2.Distance(transform.position, coin.transform.position);
            MoveTowardsTarget(coin.transform.position, "run");
        }

        #endregion
        // Helper Methods
        #region Trigger and pickup,coin

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Coin"))
            {
               // Debug.LogError(gameObject.name+ " trigger Coin");
                PickUpCoin(other.gameObject);
            }
        }
        private void PickUpCoin(GameObject coinObject)
        {
            Destroy(coinObject);
            currentState = VagrantState.Idle;
        }

        
        #endregion
        
        #region move and setaget,distance

        private void MoveTowardsTarget(Vector2 targetPosition, string animationName)
        {
            FlipDirection(new Vector3(targetPosition.x, transform.position.y, 0));
            
            Vector2 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, 0),  // Không thay đổi y, chỉ thay đổi x
                new Vector2(targetPosition.x,0),  // Mục tiêu di chuyển tới targetPosition.x, giữ targetPosition.y
                runSpeed * Time.deltaTime
            );
            transform.position = new Vector3(newPosition.x, 0, transform.position.z);
            animator.Play(animationName);
        }
        
        private void MoveTowardsIdleTarget()
        {
            // Kiểm tra xem đã đến gần đích chưa
            if (Vector3.Distance(transform.position, currentIdleTarget) < 0.1f)
            {
                isWalking = false;
                return;
            }
            
            FlipDirection(currentIdleTarget);
            
            Vector2 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, 0),
                new Vector2(currentIdleTarget.x, 0),
                idleSpeed * Time.deltaTime
            );
            transform.position = new Vector3(newPosition.x, transform.position.y, transform.position.z);
        }
        
        public void GetDistanceToTarget()
        {
            // Current position of the GameObject
            Vector2 currentPosition = transform.position;
            // Calculate the distance to the target
            roadToTarget = Vector2.Distance(currentPosition, targetPosition);
        }
        private void FlipDirection(Vector3 targetPosition)
        {
            transform.localScale = new Vector3(
                targetPosition.x < transform.position.x ? -1 : 1,
                1,
                1
            );
        }
        #endregion

        #region Routine and time

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
                UpdateStateBasedOnDetection(rayCastHelper.DrawRayAndReturnListHitObjects());
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void HandleTimeChange()
        {
            if (GameTimeManager.Instance.IsDaytime && EnemyWaveSystem.Instance.EnemyRemaining() <= 0)
            {
                StartCheckRoutine();
                currentState = VagrantState.Idle;
            }
        }
        private void StartCheckRoutine()
        {
            if (checkRoutine != null)
            {
                StopCoroutine(checkRoutine);
            }
            checkRoutine = StartCoroutine(CheckRoutine());
        }
        #endregion
       
        #region Random Flip and Walk Routines

        private IEnumerator RandomFlipRoutine()
        {
            while (currentState == VagrantState.Idle && !isWalking)
            {
                // Wait for a random time between 2 and 5 seconds
                yield return new WaitForSeconds(Random.Range(2f, 5f));

                // Only flip if still in Idle state and not walking
                if (currentState == VagrantState.Idle && !isWalking)
                {
                    // Flip the direction
                    FlipDirection(transform.position + new Vector3(transform.localScale.x * -1, 0, 0));
                }
            }

            // Reset flipRoutine when exiting
            flipRoutine = null;
        }
        
        private IEnumerator IdleWalkRoutine()
        {
            while (currentState == VagrantState.Idle)
            {
                // Đợi 3-8 giây trước khi quyết định di chuyển
                yield return new WaitForSeconds(Random.Range(3f, 8f));
                
                if (currentState != VagrantState.Idle) break;
                
                // 70% cơ hội di chuyển, 30% cơ hội đứng yên
                if (Random.value < 0.7f)
                {
                    // Dừng routine flip khi bắt đầu đi
                    if (flipRoutine != null)
                    {
                        StopCoroutine(flipRoutine);
                        flipRoutine = null;
                    }
                    
                    // Chọn một điểm đích ngẫu nhiên trong phạm vi cho phép
                    float randomOffset = Random.Range(-maxIdleWalkDistance, maxIdleWalkDistance);
                    float targetX = Mathf.Clamp(
                        startPosition.x + randomOffset, 
                        startPosition.x - maxIdleWalkDistance, 
                        startPosition.x + maxIdleWalkDistance
                    );
                    
                    currentIdleTarget = new Vector3(targetX, transform.position.y, transform.position.z);
                    isWalking = true;
                    
                    // Đợi cho đến khi hoàn thành di chuyển hoặc tối đa 5 giây
                    float walkStartTime = Time.time;
                    while (isWalking && Time.time - walkStartTime < 5f && currentState == VagrantState.Idle)
                    {
                        if (Vector3.Distance(transform.position, currentIdleTarget) < 0.1f)
                        {
                            isWalking = false;
                        }
                        yield return null;
                    }
                    
                    isWalking = false;
                }
            }
            
            idleWalkRoutine = null;
        }
        
        private void StopAllMovementRoutines()
        {
            // Stop the random flip routine
            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }
            
            // Stop the idle walk routine
            if (idleWalkRoutine != null)
            {
                StopCoroutine(idleWalkRoutine);
                idleWalkRoutine = null;
            }
            
            isWalking = false;
        }

        #endregion
        
        #region Save System Implementation
        [System.Serializable]
        public class VagrantData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int stateValue;
            public float startPositionX;
        }
        
        public string GetPrefabID()
        {
            return "vagrant";
        }
        
        public object CaptureData()
        {
            return new VagrantData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                stateValue = (int)currentState,
                startPositionX = startPosition.x
            };
        }

        public void RestoreData(object data)
        {
            VagrantData vagrantData = (VagrantData)data;
            transform.position = new Vector3(vagrantData.positionX, vagrantData.positionY, 0);
            transform.localScale = new Vector3(vagrantData.scaleX, 1, 1);
            currentState = (VagrantState)vagrantData.stateValue;
            startPosition = new Vector3(vagrantData.startPositionX, vagrantData.positionY, 0);
            
            // Ensure we initialize components if we're not already initialized
            if (animator == null)
            {
                InitComponent();
                animator = GetComponent<Animator>();
            }
            
            // Start check routine if we're not in fear state
            if (currentState != VagrantState.Fear)
            {
                StartCheckRoutine();
            }
        }
        #endregion
    }
}