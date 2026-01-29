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

namespace Script.Game.Mobs.Subjects
{
    // Renamed from VagrantController to VagrantBehavior to reflect Behavior + Status architecture
    public class VagrantBehavior : MonoBehaviour, ISaveable, INPCBehavior, ICoinable
    {
        private VagrantStatus status;
        
        // Component References
        private Animator animator;
        private Coroutine checkRoutine;
        private Coroutine flipRoutine;
        private Coroutine idleWalkRoutine;
        private RayCastHelper rayCastHelper;

        private void Awake()
        {
            gameObject.tag = "Vagrant";
            status = GetComponent<VagrantStatus>();
            if (status == null)
            {
                status = gameObject.AddComponent<VagrantStatus>();
            }
            status.StartPosition = transform.position;
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
                Debug.LogWarning(gameObject.name + " chưa có RayCastHelper, đã thêm 1 cái vào " + gameObject.name);
            }
        }

        private void InitializeVagrant()
        {
            status.RoadToTarget = 0f;
            status.CurrentState = VagrantStatus.VagrantState.Idle;
            animator = GetComponent<Animator>();
            status.StartPosition = transform.position;
            StartCheckRoutine();
        }

        private void OnEnable()
        {
            GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleTimeChange);
        }

        private void OnDisable()
        {
            GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, HandleTimeChange);
        }

        public void UpdateBehavior()
        {
            UpdateStateHandler();
        }

        private void UpdateStateHandler()
        {
            switch (status.CurrentState)
            {
                case VagrantStatus.VagrantState.Idle:
                    HandleIdleState();
                    break;
                case VagrantStatus.VagrantState.Fear:
                    HandleFearState();
                    break;
                case VagrantStatus.VagrantState.GetCoin:
                    HandleGetCoinState();
                    break;
            }
        }

        private void UpdateStateBasedOnDetection(List<GameObject> gameObjectsToCheck)
        {
            foreach (var obj in gameObjectsToCheck)
            {
                if (obj.CompareTag("Enemy"))
                {
                    status.CurrentState = VagrantStatus.VagrantState.Fear;
                    break;
                }
        
                if (obj.CompareTag("Coin"))
                {
                    Debug.Log($"[VagrantBehavior] Coin detected: {obj.name} at {obj.transform.position}, current state: {status.CurrentState}");
                    
                    if ((status.CurrentState == VagrantStatus.VagrantState.Idle || 
                         status.CurrentState == VagrantStatus.VagrantState.GetCoin) && 
                        status.CurrentState != VagrantStatus.VagrantState.Fear)
                    {
                        Debug.Log($"[VagrantBehavior] Changing state to GetCoin for coin: {obj.name}");
                        status.CurrentState = VagrantStatus.VagrantState.GetCoin;
                        status.Coin = obj;
                    }
                    else
                    {
                        Debug.Log($"[VagrantBehavior] Not changing state - current state: {status.CurrentState}");
                    }
                    break;
                }
            }
        }
        
        #region Handle States

        private void HandleIdleState()
        {
            if (animator == null)
            {
               // Try to get animator if null
               animator = GetComponent<Animator>();
               if (animator == null) return;
            }

            if (idleWalkRoutine == null)
            {
                idleWalkRoutine = StartCoroutine(IdleWalkRoutine());
               
            }
            
            if (status.IsWalking)
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
            status.RoadToTarget = 0;
            StopCheckRoutine();
            StopAllMovementRoutines();
        }
        
        private void HandleGetCoinState()
        {
            if (status.Coin == null || status.Coin.Equals(null))
            {
                status.CurrentState = VagrantStatus.VagrantState.Idle;
                return;
            }

            StopAllMovementRoutines();
            
            status.RoadToTarget = Vector2.Distance(transform.position, status.Coin.transform.position);
            MoveTowardsTarget(status.Coin.transform.position, "run");
        }

        #endregion

        #region Trigger and Pickup

        private void OnTriggerEnter2D(Collider2D other)
        {
           // Debug.Log($"[VagrantBehavior] OnTriggerEnter2D with {other.name}, tag: {other.tag}");
            
            if (other.CompareTag("Coin"))
            {
              //  Debug.Log($"[VagrantBehavior] Coin trigger detected, calling PickUpCoinInternal");
                PickUpCoinInternal(other.gameObject);
            }
        }

        private void PickUpCoinInternal(GameObject coinObject)
        {
            Debug.Log($"[VagrantBehavior] PickUpCoin called for {coinObject.name}");
            
            Script.Utility.ProcessCoinPickup(coinObject,
                condition: () => 
                {
                    Debug.Log($"[VagrantBehavior] ProcessCoinPickup condition check - always true for Vagrant");
                    return true; // Vagrant always picks up coin
                },
                onPickupSuccess: () =>
                {
                    Debug.Log($"[VagrantBehavior] Coin pickup successful, transforming to Villager");
                    StartCoroutine(TransformToVillagerNextFrame());
                }
            );
        }

        private IEnumerator TransformToVillagerNextFrame()
        {
            yield return null;
            TransformToVillager();
        }

        private void TransformToVillager()
        {
            var controller = GetComponent<DenizenController>();
            if (controller != null)
            {
                controller.ChangeRole(NPCRole.Villager);
                // Đảm bảo có ít nhất 1 coin sau khi đổi role
                var vs = GetComponent<VillagerStatus>();
                if (vs != null && vs.CurrentCoins < 1) vs.CurrentCoins = 1;
                return;
            }
            Debug.LogError("[VagrantBehavior] DenizenController missing; expected all NPCs to be Denizen. Skipping legacy fallback.");
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
            float distanceToTarget = Vector3.Distance(transform.position, status.CurrentIdleTarget);
            if (distanceToTarget < 0.1f)
            {
               
                status.IsWalking = false;
                return;
            }
            
            FlipDirection(status.CurrentIdleTarget);
            
            Vector2 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, 0),
                new Vector2(status.CurrentIdleTarget.x, 0),
                status.IdleSpeed * Time.deltaTime
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
            if (GameTimeManager.Instance == null || EnemyWaveManager.Instance == null) return;

            if (GameTimeManager.Instance.IsDaytime && EnemyWaveManager.Instance.EnemyRemaining() <= 0)
            {
                StartCheckRoutine();
                status.CurrentState = VagrantStatus.VagrantState.Idle;
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

        private IEnumerator RandomFlipRoutine()
        {
            while (status.CurrentState == VagrantStatus.VagrantState.Idle && !status.IsWalking)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));

                if (status.CurrentState == VagrantStatus.VagrantState.Idle && !status.IsWalking)
                {
                    FlipDirection(transform.position + new Vector3(transform.localScale.x * -1, 0, 0));
                }
            }

            flipRoutine = null;
        }
        
        private IEnumerator IdleWalkRoutine()
        {
            while (status.CurrentState == VagrantStatus.VagrantState.Idle)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 8f));
                
                if (status.CurrentState != VagrantStatus.VagrantState.Idle) break;
                
                if (UnityEngine.Random.value < 0.7f)
                {
                    if (flipRoutine != null)
                    {
                        StopCoroutine(flipRoutine);
                        flipRoutine = null;
                    }
                    
                    float randomOffset = UnityEngine.Random.Range(-status.MaxIdleWalkDistance, status.MaxIdleWalkDistance);
                    float targetX = Mathf.Clamp(
                        status.StartPosition.x + randomOffset, 
                        status.StartPosition.x - status.MaxIdleWalkDistance, 
                        status.StartPosition.x + status.MaxIdleWalkDistance
                    );
                    
                    status.CurrentIdleTarget = new Vector3(targetX, transform.position.y, transform.position.z);
                    
                   
                    status.IsWalking = true;
                    
                    float walkStartTime = Time.time;
                    while (status.IsWalking && 
                           Time.time - walkStartTime < 5f && 
                           status.CurrentState == VagrantStatus.VagrantState.Idle)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, status.CurrentIdleTarget);
                        if (distanceToTarget < 0.1f)
                        {
                           
                            status.IsWalking = false;
                            break;
                        }
                        yield return null;
                    }

                    if (status.IsWalking)
                    {
                        
                        status.IsWalking = false;
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
            
            status.IsWalking = false;
        }

        #endregion
        
        #region Save System Implementation

        public string GetPrefabID()
        {
            return "vagrant";
        }
        
        public object CaptureData()
        {
            return status.CaptureData(transform);
        }

        public void RestoreData(object data)
        {
            var vagrantData = (VagrantStatus.VagrantData)data;
            status.RestoreData(vagrantData, transform);
            
            if (animator == null)
            {
                InitComponent();
                animator = GetComponent<Animator>();
            }
            
            if (status.CurrentState != VagrantStatus.VagrantState.Fear)
            {
                StartCheckRoutine();
            }
        }

        #endregion
        
        #region ICoinable Implementation

        public int Coin 
        { 
            get => status.CurrentCoins; 
            set => status.CurrentCoins = value; 
        }

        public void AddCoin(int amount)
        {
            status.CurrentCoins += amount;
            Debug.Log($"[VagrantBehavior] Added {amount} coins. Total: {status.CurrentCoins}");
        }

        public void SubtractCoin(int amount)
        {
            status.CurrentCoins -= amount;
            Debug.Log($"[VagrantBehavior] Subtracted {amount} coins. Total: {status.CurrentCoins}");
        }

        public void PickUpCoin(GameObject coinGameObject)
        {
            Debug.Log($"[VagrantBehavior] PickUpCoin (ICoinable) called for {coinGameObject.name}");
            PickUpCoinInternal(coinGameObject);
        }

        #endregion
    }
}


