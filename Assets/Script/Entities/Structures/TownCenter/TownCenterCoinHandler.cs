using System;
using System.Collections.Generic;
using UnityEngine;
using Script.Game.Item.Coin;
using Script.Game.Item;
using Script.Game.Player;
using Script.Interfaces;
using Script.Manager.SaveManager;
using Script.Data_Model;
using Script.Data_Model.Status;

namespace Script.Game.Structures.TownCenter
{
    
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TownCenterCoinHandler : MonoBehaviour, IPlayerInteractable
    {
        private TownCenterBehavior townCenterBehavior;
        private TownCenterStatus status;
        private Collider2D triggerCollider;
        private Rigidbody2D rb;

        [Header("Player Trigger Settings")]
        private const float PLAYER_TRIGGER_THRESHOLD = 5f;
        private float playerTriggerTimer = 0f;
        private bool isPlayerInRange = false;

        [Header("Debug")]
        [SerializeField] private bool debugIsPlayerInRange = false;
        [SerializeField] private float debugPlayerTriggerTimer = 0f;
        [SerializeField] private float debugTimeUntilDrop = 0f;

        [Header("Coin Drop Settings")]
        private const float COIN_DROP_INTERVAL = 0.5f; // 2 coins per second
        private const float COIN_SPAWN_OFFSET_Y = 2f;
        private const float TOWNCENTER_DROP_COOLDOWN = 15f; // 10s + 5s extra wait time
        private float coinDropTimer = 0f;

        [Header("Coin Tracking")]
        private Dictionary<PhysicalItem, bool> trackedCoins = new Dictionary<PhysicalItem, bool>();

        // Suck-back system: track coins dropped by TownCenter
        [SerializeField] private List<PhysicalItem> droppedCoins = new List<PhysicalItem>();
        private float suckBackCheckTimer = 0f;
        private const float SUCK_BACK_CHECK_INTERVAL = 1f;

        private void Awake()
        {
            townCenterBehavior = GetComponent<TownCenterBehavior>();
            status = GetComponent<TownCenterStatus>();
            triggerCollider = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
            else
            {
                Debug.LogError("[TownCenterCoinHandler] Collider2D is missing!");
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
            }
            else
            {
                Debug.LogError("[TownCenterCoinHandler] Rigidbody2D is missing!");
            }
        }

        private void Update()
        {
            // Update debug fields
            debugIsPlayerInRange = isPlayerInRange;
            debugPlayerTriggerTimer = playerTriggerTimer;
            debugTimeUntilDrop = PLAYER_TRIGGER_THRESHOLD - playerTriggerTimer;

            // Handle player trigger timer
            if (isPlayerInRange)
            {
                playerTriggerTimer += Time.deltaTime;

                if (playerTriggerTimer >= PLAYER_TRIGGER_THRESHOLD)
                {
                    // Player has been in range for 5 seconds, start dropping coins
                    coinDropTimer += Time.deltaTime;

                    if (coinDropTimer >= COIN_DROP_INTERVAL)
                    {
                        DropCoinToPlayer();
                        coinDropTimer = 0f;
                    }
                }
            }
            else
            {
                playerTriggerTimer = 0f;
                coinDropTimer = 0f;
            }

            // Handle suck-back timer
            suckBackCheckTimer += Time.deltaTime;
            if (suckBackCheckTimer >= SUCK_BACK_CHECK_INTERVAL)
            {
                suckBackCheckTimer = 0f;
                ProcessSuckBack();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var physicalItem = other.GetComponent<PhysicalItem>();
            if (physicalItem == null) return;

            // If coin is on ground (floating = true) and not in cooldown
            if (physicalItem.floating && physicalItem.CanTownCenterPickup())
            {
                PickUpCoin(physicalItem);
            }
            // If coin is still falling (floating = false) and not tracked yet
            else if (!physicalItem.floating && !trackedCoins.ContainsKey(physicalItem))
            {
                SubscribeToCoinReady(physicalItem);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var physicalItem = other.GetComponent<PhysicalItem>();
            if (physicalItem == null) return;

            UnsubscribeFromCoinReady(physicalItem);
        }

        private void SubscribeToCoinReady(PhysicalItem coin)
        {
            if (coin == null) return;

            coin.OnReadyForTownCenterPickup += OnCoinReadyForPickup;
            trackedCoins[coin] = true;
        }

        private void UnsubscribeFromCoinReady(PhysicalItem coin)
        {
            if (coin == null || !trackedCoins.ContainsKey(coin)) return;

            coin.OnReadyForTownCenterPickup -= OnCoinReadyForPickup;
            trackedCoins.Remove(coin);
        }

        private void OnCoinReadyForPickup(PhysicalItem coin)
        {
            if (coin == null) return;

            // Check cooldown before picking up
            if (coin.CanTownCenterPickup())
            {
                UnsubscribeFromCoinReady(coin);
                PickUpCoin(coin);
            }
        }

        private void PickUpCoin(PhysicalItem coin)
        {
            if (coin == null) return;

            // Unsubscribe from event if subscribed
            UnsubscribeFromCoinReady(coin);

            // Remove from dropped coins list if present
            droppedCoins.Remove(coin);

            // Return coin to pool instead of destroying
            var coinPool = FindObjectOfType<Script.Game.Item.Coin.CoinPool>();
            if (coinPool != null)
            {
                coinPool.ReturnCoin(coin.gameObject);
            }
            else
            {
                Destroy(coin.gameObject);
            }

            // Remove from tracking
            trackedCoins.Remove(coin);

            // Increment stored coin count
            status.storedCoinCount++;
        }

        private void DropCoinToPlayer()
        {
            if (status.storedCoinCount <= 0) return;

            status.storedCoinCount--;

            Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + COIN_SPAWN_OFFSET_Y);
            Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;

            if (dropDirection.sqrMagnitude < 0.01f) dropDirection = Vector2.right;

            if (Spawner.Instance != null)
            {
                GameObject coin = Spawner.Instance.DropByTownCenter(spawnPosition, dropDirection);

                if (coin != null)
                {
                    PhysicalItem coinScript = coin.GetComponent<PhysicalItem>();
                    if (coinScript != null)
                    {
                        coinScript.SetTownCenterDropCooldown(TOWNCENTER_DROP_COOLDOWN);
                        droppedCoins.Add(coinScript);
                    }
                }
            }
        }

        /// <summary>
        /// Process suck-back: decrease TownCenter pickup cooldown for dropped coins every 1s.
        /// If cooldown <= 0 and coin is in range, suck it back.
        /// </summary>
        private void ProcessSuckBack()
        {
            // Iterate backwards to safely remove items
            for (int i = droppedCoins.Count - 1; i >= 0; i--)
            {
                PhysicalItem coin = droppedCoins[i];

                // Remove if coin is null or destroyed
                if (coin == null)
                {
                    droppedCoins.RemoveAt(i);
                    continue;
                }

                // Decrease TownCenter pickup cooldown
                var cooldownBehavior = coin.GetComponent<ItemCooldownBehavior>();
                if (cooldownBehavior != null)
                {
                    // Using reflection to get current cooldown value
                    var cooldownField = cooldownBehavior.GetType().GetField("_townCenterPickupCooldown",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (cooldownField != null)
                    {
                        float currentCooldown = (float)cooldownField.GetValue(cooldownBehavior);
                        if (currentCooldown > 0f)
                        {
                            float newCooldown = Mathf.Max(0f, currentCooldown - SUCK_BACK_CHECK_INTERVAL);
                            cooldownBehavior.SetTownCenterDropCooldown(newCooldown);
                        }
                        else
                        {
                            // Cooldown expired, check if coin is in range and suck back
                            float distance = Vector2.Distance(coin.transform.position, transform.position);
                            if (distance <= 5f && coin.floating && coin.CanTownCenterPickup())
                            {
                                PickUpCoin(coin);
                                // Don't call RemoveAt here - PickUpCoin already removes it
                            }
                        }
                    }
                }
            }
        }

        // --- IPlayerInteractable Implementation ---

        public void OnPlayerEnter(PlayerBehavior player)
        {
            isPlayerInRange = true;
            playerTriggerTimer = 0f;
        }

        public void OnPlayerExit(PlayerBehavior player)
        {
            isPlayerInRange = false;
            playerTriggerTimer = 0f;
            coinDropTimer = 0f;
        }

        public bool CanInteract()
        {
            return status != null && status.storedCoinCount > 0;
        }

        private void OnDestroy()
        {
            // Unsubscribe all tracked coin events (null-safe)
            var coinsCopy = new List<PhysicalItem>(trackedCoins.Keys);
            foreach (var coin in coinsCopy)
            {
                if (coin != null)
                {
                    coin.OnReadyForTownCenterPickup -= OnCoinReadyForPickup;
                }
            }
            trackedCoins.Clear();
        }
    }
}
