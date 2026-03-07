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

        private float fearCooldownTimer = 0f;

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
             StopAllMovementRoutines();
             if (rayCastHelper != null) rayCastHelper.isActive = false;
        }

        private void Start()
        {
            InitComponent();
            InitializeVagrant();
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

        public void UpdateBehavior()
        {
            // Detection Logic - check for Enemy and Coin
            if (rayCastHelper != null)
            {
                var detectedObjects = rayCastHelper.GetListHitObjects(status.detectRangeRaycast);
                
                foreach (var obj in detectedObjects)
                {
                    if (obj.CompareTag("Enemy"))
                    {
                        if (status.vagrantState != DenizenStatus.VagrantState.Fear)
                        {
                            status.vagrantState = DenizenStatus.VagrantState.Fear;
                        }
                        // Reset timer mỗi khi quét trúng enemy
                        fearCooldownTimer = 30f;
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

            UpdateStateHandler();

            // Debug Visualization
            if (isDebugGoPlace)
            {
                if (status.vagrantState == DenizenStatus.VagrantState.Idle && status.isWalking)
                {
                    Debug.DrawLine(transform.position, status.currentIdleWalkTarget, Color.yellow);
                }
                else if (status.vagrantState == DenizenStatus.VagrantState.GetCoin && status.coinTarget != null)
                {
                    Debug.DrawLine(transform.position, status.coinTarget.transform.position, Color.yellow);
                }
            }
        }

        private void UpdateStateHandler()
        {
            if (status.vagrantState == DenizenStatus.VagrantState.Fear)
            {
                if (fearCooldownTimer > 0)
                {
                    fearCooldownTimer -= Time.deltaTime;
                    if (fearCooldownTimer <= 0)
                    {
                        status.vagrantState = DenizenStatus.VagrantState.Idle; // Hết sợ, quay về Idle
                    }
                }
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

            if (idleWalkRoutine == null)
            {
                idleWalkRoutine = StartCoroutine(IdleWalkRoutine());
            }
            
            if (status.isWalking)
            {
                MoveTowardsIdleTarget();
                animator.Play("walk");
            }
            else
            {
                animator.Play("idle");
                
                if (flipRoutine == null)
                {
                    flipRoutine = StartCoroutine(RandomFlipRoutine());
                }
            }
        }
        
        private void HandleFearState()
        {
            animator.Play("fear");
            status.roadToTarget = 0;
    
            StopAllMovementRoutines();
        }
        
        private void HandleGetCoinState()
        {
            if (status.coinTarget == null || status.coinTarget.Equals(null))
            {
                status.vagrantState = DenizenStatus.VagrantState.Idle;
                return;
            }

            StopAllMovementRoutines();
            
            status.roadToTarget = Vector2.Distance(transform.position, status.coinTarget.transform.position);
            MoveTowardsTarget(status.coinTarget.transform.position, "run");
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

        private void MoveTowardsTarget(Vector2 targetPosition, string animationName)
        {
            FlipDirection(new Vector3(targetPosition.x, transform.position.y, 0));
            
            Vector2 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, 0),
                new Vector2(targetPosition.x, 0),
                status.RunSpeed * Time.deltaTime
            );
            transform.position = new Vector3(newPosition.x, 0, transform.position.z);
            animator.Play(animationName);
        }
        
        private void MoveTowardsIdleTarget()
        {
            float distanceToTarget = Vector3.Distance(transform.position, status.currentIdleWalkTarget);
            if (distanceToTarget < 0.1f)
            {
                status.isWalking = false;
                return;
            }
            
            FlipDirection(status.currentIdleWalkTarget);
            
            Vector2 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, 0),
                new Vector2(status.currentIdleWalkTarget.x, 0),
                status.WalkSpeed * Time.deltaTime
            );
            transform.position = new Vector3(newPosition.x, transform.position.y, transform.position.z);
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

        #region Routines



        // Removed HandleTimeChange that checks EnemyRemaining
        // Coroutine FearCooldownRoutine removed. Handled via fearCooldownTimer in UpdateStateHandler.

        private IEnumerator RandomFlipRoutine()
        {
            while (status.vagrantState == DenizenStatus.VagrantState.Idle && !status.isWalking)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));

                if (status.vagrantState == DenizenStatus.VagrantState.Idle && !status.isWalking)
                {
                    FlipDirection(transform.position + new Vector3(transform.localScale.x * -1, 0, 0));
                }
            }

            flipRoutine = null;
        }
        
        private IEnumerator IdleWalkRoutine()
        {
            while (status.vagrantState == DenizenStatus.VagrantState.Idle)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 8f));
                
                if (status.vagrantState != DenizenStatus.VagrantState.Idle) break;
                
                if (UnityEngine.Random.value < 0.7f)
                {
                    if (flipRoutine != null)
                    {
                        StopCoroutine(flipRoutine);
                        flipRoutine = null;
                    }
                    
                    float randomOffset = UnityEngine.Random.Range(-status.maxIdleWalkDistance, status.maxIdleWalkDistance);
                    float targetX = Mathf.Clamp(
                        status.startPosition.x + randomOffset, 
                        status.startPosition.x - status.maxIdleWalkDistance, 
                        status.startPosition.x + status.maxIdleWalkDistance
                    );
                    
                    status.currentIdleWalkTarget = new Vector3(targetX, transform.position.y, transform.position.z);
                    
                    status.isWalking = true;
                    
                    float walkStartTime = Time.time;
                    while (status.isWalking && 
                           Time.time - walkStartTime < 5f && 
                           status.vagrantState == DenizenStatus.VagrantState.Idle)
                    {
                         // Re-check distance in loop
                         float distanceToTarget = Vector3.Distance(transform.position, status.currentIdleWalkTarget);
                         if (distanceToTarget < 0.1f)
                         {
                             status.isWalking = false;
                             break;
                         }
                        yield return null;
                    }

                    if (status.isWalking)
                    {
                        status.isWalking = false;
                    }
                }
            }
            
            idleWalkRoutine = null;
        }
        
        private void StopAllMovementRoutines()
        {
            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }
            
            if (idleWalkRoutine != null)
            {
                StopCoroutine(idleWalkRoutine);
                idleWalkRoutine = null;
            }
            
            status.isWalking = false;
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


