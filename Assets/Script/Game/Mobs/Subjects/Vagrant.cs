using System;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Item.Coin;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Script.Game.Mobs.Subjects
{
    public class Vagrant : MonoBehaviour
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
        private RayCastHelper rayCastHelper;
    
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
            GameTimeManager.Instance.Hour6 += HandleTimeChange;//đăng ký múi giờ tự động chuyển về idle khi trời sáng
            roadToTarget = 0f;
            currentState = VagrantState.Idle;//khởi tạo idle ban đầu
            animator = GetComponent<Animator>();
            StartCheckRoutine();//bắt đầu hàm kiểm tra mỗi 0.5f
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
            //go not thinjk
            animator.Play("idle");
            roadToTarget = 0;
            
            // Start the random flip routine if not already running
            if (flipRoutine == null)
            {
                flipRoutine = StartCoroutine(RandomFlipRoutine());
            }
        }
        private void HandleFearState()
        {
            animator.Play("fear");
            roadToTarget = 0;
            StopCheckRoutine();
            // Stop the random flip routine
            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }
        }
        private void HandleGetCoinState()
        {
            // First, check if the coin has been destroyed
            if (coin == null || coin.Equals(null))
            {
                currentState = VagrantState.Idle;
                return;
            }

            // Stop the random flip routine
            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }
            
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
            if (GameTimeManager.Instance.IsDaytime() && EnemyWaveSystem.Instance.EnemyRemaining() <= 0)
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
       
        #region Random Flip Routine

        private IEnumerator RandomFlipRoutine()
        {
            while (currentState == VagrantState.Idle)
            {
                // Wait for a random time between 2 and 5 seconds
                yield return new WaitForSeconds(Random.Range(2f, 5f));

                // Only flip if still in Idle state
                if (currentState == VagrantState.Idle)
                {
                    // Flip the direction
                    FlipDirection(transform.position + new Vector3(transform.localScale.x * -1, 0, 0));
                }
            }

            // Reset flipRoutine when exiting
            flipRoutine = null;
        }

        #endregion
     
    }
}