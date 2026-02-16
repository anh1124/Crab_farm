using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Player;
using Script.Interfaces;
using Script.Manager.Building;
using Script.Game.Other;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class CoinUser : MonoBehaviour, IPlayerInteractable
    {
        /// <summary>
        /// khi player va chạm với collider của gameobject chứa coinUser thì se kiểm tra :
        /// -có đang va chạm với player không?
        /// -script CoinUser.isReadyToUpgrade có false không thì mới hiện các coin slot
        /// </summary>
        
        [Header("Coins Acceptance")]
        public bool canAcceptCoins = true; // kiểm soát việc nhận xu
        
        [Header("OnUpgradeComplete Settings")]
        public int requiredCoins; // tổng số coin cần để thực hiện hành động upgrade
        public Action onUpgradeComplete; // cái này sẽ được gọi khi nhét đủ coin

        [Header("Coin Collection")]
        public int collectedCoins ; // số coin hiện tại đã lấy được
        public float coinCollectionTimeout ; // Time before coin collection resets
        public float coinCollectionTimer; //  biến đếm ngược
        [SerializeField] private GameObject coinSlotParent; // Parent object for coin slots

        [Header("OnUpgradeComplete Status")]
        public bool isReadyToUpgrade; // Indicates if enough coins have been collected for upgrade
        
        [Header("Building Info")]
        [SerializeField] private BuildingType buildingType; // Type of building this CoinUser is attached to
        [SerializeField] private int currentTier; // Current tier/tier of the building
        
        // Public properties for external access
        public int CurrentCoin => collectedCoins;
        public int RequiredCoin => requiredCoins;
        public bool CanAcceptCoins => canAcceptCoins;
        public bool IsReadyToUpgrade => isReadyToUpgrade;

        [Header("Coin Prefabs")]
        [SerializeField] private GameObject coinPrefab;

        [SerializeField] private List<GameObject> coinSlots = new List<GameObject>(); // List of coin slot positions
        [SerializeField] private List<GameObject> coinsInTransit = new List<GameObject>(); // List of coins being moved
         private List<Coroutine> coinMoveCoroutines = new List<Coroutine>();

        private float coinMoveSpeed;
        private int currentSlotIndex ;
        private bool isCollectingCoins ;

        private IUpgradeable upgradeableComponent;

        public bool playerTrigger;
        private void Start()
        {
            InitializeCoinUser();
        }

        private void InitializeCoinUser()
        {
            coinPrefab = PrefabData.Instance.coinPrefab;
            coinMoveSpeed = PrefabData.Instance.coinMoveSpeed;
            coinCollectionTimeout = PrefabData.Instance.timeMaxFloatCoin;

            if (coinSlotParent == null)
            {
                // Prioritize finding "CoinSlotLocation" (standard for some prefabs)
                var child = transform.Find("CoinSlotLocation");

                if (child != null)
                {
                    coinSlotParent = child.gameObject;
                }
                else
                {
                    // Create new if not found -> Default to "CoinSlotLocation"
                    var newObj = new GameObject("CoinSlotLocation");
                    newObj.transform.SetParent(transform, false);
                    coinSlotParent = newObj;
                }
            }
            coinSlotParent.SetActive(false);
            
            // Check if this building has an IUpgradeable component
            upgradeableComponent = GetComponent<IUpgradeable>();
        }
        
        // For backwards compatibility, keep this method
        public void SetBuildingInfo(BuildingType type, int tier)
        {
            buildingType = type;
            currentTier = tier;
        }

        /// <summary>
        /// Debug method để tạo string thông tin CoinUser trigger
        /// </summary>
        public string GetTriggerDebugInfo(out bool shouldShowSlots)
        {
            shouldShowSlots = false;
            string result = $"CoinUser[{gameObject.name}]: AcceptCoins={canAcceptCoins}, ReadyToUpgrade={isReadyToUpgrade}, PlayerTrigger={playerTrigger}";

            // If component implements IUpgradeable, check if it's upgradeable based on owner's canAcceptCoins value
            var upgradeableComponent = GetComponent<IUpgradeable>();
            if (upgradeableComponent != null)
            {
                result += " | Has IUpgradeable";
                // Ready to upgrade will be determined by parent component (Wall, TownCenter, etc.)
                if (canAcceptCoins && !isReadyToUpgrade)
                {
                    result += " | Conditions met - WILL SHOW SLOTS";
                    shouldShowSlots = true;
                }
                else if (!canAcceptCoins)
                {
                    result += " | Cannot accept coins";
                }
                else
                {
                    result += " | Already ready or other condition";
                }
            }
            else
            {
                result += " | No IUpgradeable - Fallback";
                // Fallback for non-IUpgradeable components
                if (canAcceptCoins && !isReadyToUpgrade)
                {
                    result += " | Fallback: Conditions met - WILL SHOW SLOTS";
                    shouldShowSlots = true;
                }
                else
                {
                    result += " | Fallback: Cannot setup slots";
                }
            }

            return result;
        }

        public void PlayerTriggerOn()
        {
            bool shouldShowSlots = false;

            // If component implements IUpgradeable, check if it's upgradeable based on owner's canAcceptCoins value
            if (upgradeableComponent != null)
            {
                // Ready to upgrade will be determined by parent component (Wall, TownCenter, etc.)
                if (canAcceptCoins && !isReadyToUpgrade)
                {
                    shouldShowSlots = true;
                }
            }
            else
            {
                // Fallback for non-IUpgradeable components
                if (canAcceptCoins && !isReadyToUpgrade)
                {
                    shouldShowSlots = true;
                }
            }

            // Debug.Log($"[CoinUser-Debug] {gameObject.name}: PlayerTriggerOn. CanAccept={canAcceptCoins}, Ready={isReadyToUpgrade}, Required={requiredCoins}, SlotParent={coinSlotParent?.name}, Active={coinSlotParent?.activeSelf}");

            if (shouldShowSlots)
            {
                SetupCoinSlots();
                playerTrigger = true;
            }
        }
        
        public void PlayerTriggerOff()
        {
            ClearCoinSlots();
            playerTrigger = false;
        }
      

        private void SetupCoinSlots()
        {
            if (coinSlotParent != null)
            {
                // Debug.Log($"[CoinUser-Debug] {gameObject.name}: SetupCoinSlots. Parent={coinSlotParent.name}, ChildCount={coinSlotParent.transform.childCount}, Required={requiredCoins}");
                coinSlotParent.SetActive(true);
                
                // Check existing slots
                int currentSlotCount = coinSlotParent.transform.childCount;
                
                // If we have enough children, reuse them
                if (currentSlotCount >= requiredCoins && requiredCoins > 0)
                {
                    // Ensure coinSlots list is populated with these children
                    if (coinSlots.Count != currentSlotCount)
                    {
                        coinSlots.Clear();
                        foreach (Transform child in coinSlotParent.transform)
                        {
                            coinSlots.Add(child.gameObject);
                        }
                    }
                }
                else
                {
                    // Not enough slots or none at all, recreate them
                    // This handles the case where CoinSlotLocation exists but is empty
                    ClearCoinSlots();
                    if (InstanceCoinSlot.Instance != null)
                    {
                         coinSlots = InstanceCoinSlot.Instance.CreateCoinSlot(requiredCoins, coinSlotParent);
                    }
                    else
                    {
                        Debug.LogError("InstanceCoinSlot.Instance is null");
                    }
                }
            }
        }

        private void ClearCoinSlots()
        {
            foreach (Transform child in coinSlotParent.transform)
            {
                Destroy(child.gameObject);
            }
            coinSlots.Clear();
            currentSlotIndex = 0;
        }

        public void InsertCoin(int availableCoins, GameObject player)
        {
            // Kiểm tra điều kiện nhận xu
            if (!canAcceptCoins || availableCoins <= 0 || isReadyToUpgrade)
            {
                return;
            }
            StartCoinTransfer(player);
            isCollectingCoins = true;
            coinCollectionTimer = coinCollectionTimeout;
        }
        
        // Public methods for external access
        public bool ReceiveCoin(int amount, GameObject player)
        {
            if (!canAcceptCoins || amount <= 0 || isReadyToUpgrade)
            {
                return false;
            }
            
            InsertCoin(amount, player);
            return true;
        }
        
        public void PerformUpgrade()
        {
            // Fix: Check if already upgraded OR not enough coins
            if (isReadyToUpgrade || collectedCoins < requiredCoins) return;
            // Finalize coin collection and trigger upgrade
            foreach (GameObject coin in coinsInTransit)
            {
                if (coin != null)
                {
                    Destroy(coin);
                }
            }
            ClearTransitCoins();
            if (coinSlotParent != null) coinSlotParent.SetActive(false);
            currentSlotIndex = 0;
            isReadyToUpgrade = true;
            onUpgradeComplete?.Invoke();
        }
        
        // Restore CoinUser state so it can accept coins again (used by walls, sellers, etc.)
        public void ResetUpgradeState()
        {
            isReadyToUpgrade = false;
            collectedCoins = 0;
            // Clear any coins in transit visual state if needed
            StopAllCoinCoroutines();
            ClearTransitCoins();
            ClearCoinSlots();
            // Only show coin slots if player is still in trigger area and this object can accept coins
            if (playerTrigger && canAcceptCoins)
            {
                SetupCoinSlots();
            }
            else
            {
                if (coinSlotParent != null)
                {
                    coinSlotParent.SetActive(false);
                }
            }
        }
        
        
        private void Update()
        {
            // Kiểm tra và giảm dần giá trị đếm ngược
            if (isCollectingCoins && canAcceptCoins && !isReadyToUpgrade)
            {
                coinCollectionTimer -= Time.deltaTime;
                if (coinCollectionTimer <= 0)
                {
                    ReleaseCoins();
                }
            }
        }
        private void StartCoinTransfer(GameObject player)
        {
            if (coinSlots.Count == 0 || currentSlotIndex >= coinSlots.Count)
            {
                Debug.LogWarning("No available coin slots.");
                return;
            }

            if (coinSlots[currentSlotIndex] == null)
            {
                 // Handle stale slot case
                 // Debug.LogWarning("Target Coin Slot is null/destroyed.");
                 return;
            }

            Vector3 playerPosition = player.transform.position;
            Vector3 targetPosition = coinSlots[currentSlotIndex].transform.position;

            GameObject coin = Instantiate(coinPrefab, playerPosition, Quaternion.identity);
            SetCoinProperties(coin);
            
            coinsInTransit.Add(coin);
            Coroutine coroutine = StartCoroutine(MoveCoinToSlot(coin, targetPosition));
            coinMoveCoroutines.Add(coroutine);

            currentSlotIndex++;
        }

        private void SetCoinProperties(GameObject coin)
        {
            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.playerPickupAble = false;
                coinScript.denizensPickupAble = false;
                coinScript.floating = true;
            }
        }

        private IEnumerator MoveCoinToSlot(GameObject coin, Vector3 targetPosition)
        {
            Vector3 startPosition = coin.transform.position;
            float elapsedTime = 0f;

            while (elapsedTime < coinMoveSpeed)
            {
                if (coin == null) yield break; // Safety check
                coin.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / coinMoveSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (coin != null) coin.transform.position = targetPosition;
            collectedCoins++;
            CheckUpgradeStatus();
        }

        private void CheckUpgradeStatus()
        {
            if (collectedCoins >= requiredCoins)
            {
                PerformUpgrade();
            }
        }

     
        private void ReleaseCoins()
        {   
            
            //    Debug.LogWarning("đã gọi hàm ReleaseCoins");
            
            
            foreach (GameObject coin in coinsInTransit)
            {
                if (coin != null)
                {
                    ReleaseCoin(coin);
                }
            }
            StopAllCoinCoroutines();
            ClearTransitCoins();
            isCollectingCoins = false;
            currentSlotIndex = 0;
        }
        
        private void ReleaseCoin(GameObject coin)
        {
            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.floating = false;
                coinScript.playerPickupAble = true;
                coinScript.denizensPickupAble = true;
            }
        }
        
        private void StopAllCoinCoroutines()
        {
            foreach (Coroutine coroutine in coinMoveCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            coinMoveCoroutines.Clear();
        }

        private void ClearTransitCoins()
        {
            coinsInTransit.Clear();
            collectedCoins = 0;
        }

        public void OnPlayerEnter(PlayerBehavior player)
        {
            PlayerTriggerOn();
        }

        public void OnPlayerExit(PlayerBehavior player)
        {
            PlayerTriggerOff();
        }

        public bool CanInteract()
        {
            return canAcceptCoins && !isReadyToUpgrade;
        }

    }
}
