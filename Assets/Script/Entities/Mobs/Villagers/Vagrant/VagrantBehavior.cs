using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using UnityEngine;
using Script.Data_Model;
using Script.Game.Other;
using Script.Manager.WorldManager;
using Script.Manager.GamePlayManager;
using Script.Game.Item.Coin;

namespace Script.Game.Mobs.Subjects
{
    // Renamed from VagrantController to VagrantBehavior to reflect Behavior + Status architecture
    public class VagrantBehavior : MonoBehaviour, INPCBehavior, IItemHandle
    {
        private DenizenStatus status;
        
        // Component References
        private Animator animator;
        private Coroutine flipRoutine;
        private Coroutine idleWalkRoutine;
        private RayCastHelper rayCastHelper;

        private float fearEndTime = 0f;
        
        // Movement smoothing
        private Vector3 _moveTarget;
        private float _moveSpeed;
        private bool _isMovingToTarget;

        [Header("Debug")]
        public bool isDebugGoPlace;

        private void Awake()
        {
            gameObject.tag = "Vagrant";
            status = GetComponent<DenizenStatus>();
            if (status == null)
            {
                status = gameObject.AddComponent<DenizenStatus>();
            }
            // Ensure StartPosition is set if it's zero (first init)
            if (status.startPosition == Vector3.zero)
            {
                status.startPosition = transform.position;
            }
        }
    
        private void OnEnable()
        {
            // Disable all detectives first
            var controller = GetComponent<DenizenRoleController>();
            if (controller != null)
            {
                controller.DisableAllDetectives();
            }

            // Re-initialize when role switches back to Vagrant or upon creation
            InitializeVagrant();
            
            if (rayCastHelper == null) rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper != null)
            {
                rayCastHelper.isActive = true;
            }
        }

        private void OnDisable()
        {
             StopAllCoroutines();
            status.isWalking = false;
            _isMovingToTarget = false;
            if (animator != null)
            {
                animator.Play("idle"); // Reset animation
            }
        }

        private void Start()
        {
            InitComponent();
            InitializeVagrant();
            StartCoroutine(RandomFlipRoutine());
        }

        private void InitComponent()
        {
            rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper == null)
            {
                rayCastHelper = gameObject.AddComponent<RayCastHelper>();
                // Debug.LogWarning(gameObject.name + " chưa có RayCastHelper, đã thêm 1 cái vào " + gameObject.name);
            }
        }

        public void InitializeVagrant()
        {
            if (status == null) status = GetComponent<DenizenStatus>();
            
            if (rayCastHelper != null)
            {
                rayCastHelper.SetRayLength(status.detectRangeRaycast);
            }

            status.roadToTarget = 0f;
            // Only reset state if it's undefined or we are restarting logic? 
            // Better to keep existing state if reloading, but for safety:
            if (status.vagrantState == DenizenStatus.VagrantState.Idle) 
            {
                 // Keep as Idle
            }
            
            animator = GetComponent<Animator>();
            if (status.startPosition == Vector3.zero) status.startPosition = transform.position;
            
            // StartCheckRoutine(); // Removed
        }

        public void OnObjectsDetected(List<GameObject> detectedObjects)
        {
            foreach (var obj in detectedObjects)
            {
                if (obj.CompareTag("Enemy"))
                {
                    if (status.vagrantState != DenizenStatus.VagrantState.Fear)
                    {
                        status.vagrantState = DenizenStatus.VagrantState.Fear;
                    }
                    // Reset timer mỗi khi quét trúng enemy
                    fearEndTime = Time.time + 30f;
                    break;
                }
        
                if (obj.CompareTag("Coin"))
                {
                    // Sửa logic: Không được chủ động đi lượm nếu đang sợ
                    if ((status.vagrantState == DenizenStatus.VagrantState.Idle || 
                         status.vagrantState == DenizenStatus.VagrantState.GetCoin) && 
                        status.vagrantState != DenizenStatus.VagrantState.Fear)
                    {
                        status.vagrantState = DenizenStatus.VagrantState.GetCoin;
                        status.coinTarget = obj;
                    }
                    break;
                }
            }
        }

        public void UpdateBehavior()
        {
            UpdateStateHandler();

            // Debug Visualization
            if (isDebugGoPlace)
            {
                if (status.vagrantState == DenizenStatus.VagrantState.Idle && status.isWalking)
                {
                    Debug.DrawLine(transform.position, _moveTarget, Color.yellow);
                }
                else if (status.vagrantState == DenizenStatus.VagrantState.GetCoin && status.coinTarget != null)
                {
                    Debug.DrawLine(transform.position, status.coinTarget.transform.position, Color.yellow);
                }
            }
        }

        private void UpdateStateHandler()
        {
            // Nếu đang sợ, kiểm tra xem đã hết thời gian sợ chưa
            if (status.vagrantState == DenizenStatus.VagrantState.Fear && Time.time >= fearEndTime)
            {
                status.vagrantState = DenizenStatus.VagrantState.Idle; // Trở lại trạng thái Idle sau khi hết sợ
            }

            switch (status.vagrantState)
            {
                case DenizenStatus.VagrantState.Idle:
                    HandleIdleState();
                    break;
                case DenizenStatus.VagrantState.Fear:
                    HandleFearState();
                    break;
                case DenizenStatus.VagrantState.GetCoin:
                    HandleGetCoinState();
                    break;
            }
        }


        
        #region Handle States

        private void HandleIdleState()
        {
            if (animator == null)
            {
               animator = GetComponent<Animator>();
               if (animator == null) return;
            }

            if (!_isMovingToTarget)
            {
                // Chọn một điểm đến ngẫu nhiên xung quanh khu vực đi dạo
                if (UnityEngine.Random.value < 0.7f) // 70% bắt đầu đi dạo lại
                {
                    float randomOffset = UnityEngine.Random.Range(-status.maxIdleWalkDistance, status.maxIdleWalkDistance);
                    float targetX = Mathf.Clamp(status.startPosition.x + randomOffset, status.startPosition.x - status.maxIdleWalkDistance, status.startPosition.x + status.maxIdleWalkDistance);

                    _moveTarget = new Vector3(targetX, transform.position.y, transform.position.z);
                    _moveSpeed = status.WalkSpeed;
                    _isMovingToTarget = true;
                    status.isWalking = true;
                }
                else
                {
                    status.isWalking = false;
                    _isMovingToTarget = false;
                }
            }

            if (animator != null)
            {
                if (status.isWalking)
                {
                    animator.Play("walk");
                }
                else
                {
                    animator.Play("idle");
                }
            }

            // Tìm đồng xu trên đường đi
            // SearchForCoins(); // This method doesn't exist in the original code, assuming it was a placeholder.
        }
        
        private void HandleFearState()
        {
            animator.Play("fear");
            status.roadToTarget = 0;
            _isMovingToTarget = false; // Stop any current movement
            status.isWalking = false;
        }
        
        private void HandleGetCoinState()
        {
            if (status.coinTarget == null || status.coinTarget.Equals(null))
            {
                status.vagrantState = DenizenStatus.VagrantState.Idle;
                _isMovingToTarget = false;
                return;
            }

            _moveTarget = status.coinTarget.transform.position;
            _moveSpeed = status.RunSpeed;
            _isMovingToTarget = true;

            if (animator != null)
            {
                animator.Play("run");
            }
        }

        #endregion

        #region Trigger and Pickup

        // OnTriggerEnter2D handled by CoinCollector component
        // PickUpCoinInternal moved to OnCoinPickedUp

        private IEnumerator TransformToVillagerNextFrame()
        {
            yield return null;
            TransformToVillager();
        }

        private void TransformToVillager()
        {
            var controller = GetComponent<DenizenRoleController>();
            if (controller != null)
            {
                // Set initial coin for Villager
                if (status != null && status.CurrentCoins < 1) status.CurrentCoins = 1;
                
                controller.ChangeRole(NPCRole.Villager);
                return;
            }
            Debug.LogError("[VagrantBehavior] DenizenRoleController missing!");
        }

        #endregion
        
        #region Movement

        // This method is now handled by the Update() loop
        // private void MoveTowardsTarget(Vector2 targetPosition, string animationName)
        // {
        //     FlipDirection(new Vector3(targetPosition.x, transform.position.y, 0));
            
        //     Vector2 currentPosition = transform.position;
        //     Vector2 newPosition = Vector2.MoveTowards(
        //         new Vector2(currentPosition.x, 0),
        //         new Vector2(targetPosition.x, 0),
        //         status.RunSpeed * Time.deltaTime
        //     );
        //     transform.position = new Vector3(newPosition.x, 0, transform.position.z);
        //     animator.Play(animationName);
        // }
        
        // This method is now handled by the Update() loop
        // private void MoveTowardsIdleTarget()
        // {
        //     float distanceToTarget = Vector3.Distance(transform.position, status.currentIdleWalkTarget);
        //     if (distanceToTarget < 0.1f)
        //     {
        //         status.isWalking = false;
        //         return;
        //     }
            
        //     FlipDirection(status.currentIdleWalkTarget);
            
        //     Vector2 currentPosition = transform.position;
        //     Vector2 newPosition = Vector2.MoveTowards(
        //         new Vector2(currentPosition.x, 0),
        //         new Vector2(status.currentIdleWalkTarget.x, 0),
        //         status.WalkSpeed * Time.deltaTime
        //     );
        //     transform.position = new Vector3(newPosition.x, transform.position.y, transform.position.z);
        // }

        private void FlipDirection(Vector3 targetPosition)
        {
            Vector3 scale = transform.localScale;
            scale.x = (targetPosition.x > transform.position.x) ? 1 : -1;
            transform.localScale = scale;
        }

        #endregion

        #region Routines

        private void Update()
        {
            if (!_isMovingToTarget || status.vagrantState == DenizenStatus.VagrantState.Fear) return;

            float distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, 0), new Vector2(_moveTarget.x, 0));
            if (distanceToTarget < 0.1f)
            {
                _isMovingToTarget = false;
                status.isWalking = false;
                return;
            }

            FlipDirection(_moveTarget);
            Vector2 newPos = Vector2.MoveTowards(
                new Vector2(transform.position.x, 0),
                new Vector2(_moveTarget.x, 0),
                _moveSpeed * Time.deltaTime
            );
            transform.position = new Vector3(newPos.x, transform.position.y, transform.position.z);
        }

        // Removed HandleTimeChange that checks EnemyRemaining
        
        private System.Collections.IEnumerator RandomFlipRoutine()
        {
            while (true) // Chạy ngầm liên tục
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));

                // Chỉ xoay khi đang đứng im (Idle + không di chuyển)
                if (status.vagrantState == DenizenStatus.VagrantState.Idle && !status.isWalking && !_isMovingToTarget)
                {
                    // Lật ngẫu nhiên
                    Vector3 scale = transform.localScale;
                    scale.x *= -1;
                    transform.localScale = scale;
                }
            }
        }

        #endregion
        
        #region INPCBehavior Implementation

        public void HandleItem(GameObject item)
        {
            PhysicalItem coin = item.GetComponent<PhysicalItem>();
            if (coin != null && (coin.prefabID == "coin" || coin.itemCategory == ItemCategory.Coin))
            {
                // Only transform if enabled (double check, though CoinCollector checks this usually)
                if (!this.enabled) return;
                
                // Tiêu hủy coin ngay lập tức vì Vagrant đã sử dụng nó để đổi nghề
                Destroy(item);
                
                // Debug.Log($"[VagrantBehavior] Coin pickup successful, transforming to Villager");
                StartCoroutine(TransformToVillagerNextFrame());
            }
        }

        #endregion
    }
}


